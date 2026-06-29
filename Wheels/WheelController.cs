using System.Windows;
using System.Windows.Threading;
using Spinix.Actions;
using Spinix.Config;
using Spinix.Diagnostics;
using Spinix.Native;

namespace Spinix.Wheels;

/// <summary>
/// 轮盘协调器：串联钩子 → 轮盘 UI → 动作执行。
///
/// 状态机：
///   Idle ── TriggerDown ──▶ Active(在鼠标位置显示主轮盘)
///   Active ── MouseMove ──▶ 更新 HoverIndex（按当前光标角度）
///   Active ── TriggerUp ──▶ 执行高亮动作 / 取消（死区）/ 进入子轮盘 ──▶ Idle 或 Active(子轮盘)
///
/// 二级轮盘交互（原神手感）：
///   按住侧键期间，光标移到 SubWheel 扇区并停留一定时间 → 自动进入子轮盘，
///   无需松开；在子轮盘内同样按方向选择，松开执行。
///   死区回退：在子轮盘内将光标移回中心死区停留 → 返回父轮盘。
/// </summary>
public sealed class WheelController : IDisposable
{
    private readonly LowLevelMouseHook _hook;       // 始终安装：负责 MouseMove（方向追踪）
    private readonly LowLevelKeyboardHook _kbdHook; // 始终安装：键盘触发时负责 TriggerDown/Up
    private readonly WheelWindow _window;
    private readonly ActionExecutor _executor;
    private readonly Dispatcher _dispatcher;

    // 配置：对外只读引用，通过 UpdateConfig 热替换
    private SpinixConfig _config;

    private Point _center;              // 轮盘中心（逻辑像素）
    private Wheel? _currentWheel;       // 当前打开的轮盘（主或二级）
    private bool _active;               // 是否处于显示轮盘状态

    // 子轮盘进入/回退状态机 + 轮询计时器（鼠标静止时也能推进判定）
    private readonly SubWheelStateMachine _subWheelState = new();
    private DispatcherTimer? _pollTimer;
    private Point _lastCursorLogical;   // 上次光标位置（逻辑像素），供轮询复算命中

    public SpinixConfig Config => _config;

    /// <summary>全局钩子是否已成功安装（供自检/托盘诊断）。鼠标钩子或键盘钩子任一在用即视为可用。</summary>
    public bool IsHookInstalled => _hook.IsInstalled || _kbdHook.IsInstalled;

    /// <summary>主轮盘的条目数（供自检；空则触发键无内容可显示）。</summary>
    public int MainWheelItemCount => _config.GetMainWheel()?.Items.Count ?? 0;

    public WheelController(SpinixConfig config)
    {
        _config = config;
        _hook = new LowLevelMouseHook
        {
            Button = ParseButton(config.Trigger),
            SuppressTriggerEvents = config.SuppressTriggerEvents,
        };
        _kbdHook = new LowLevelKeyboardHook
        {
            SuppressTriggerEvents = config.SuppressTriggerEvents,
        };
        ApplyTriggerMode(config.Trigger);
        _window = new WheelWindow();
        _executor = new ActionExecutor();
        _dispatcher = Application.Current.Dispatcher;
    }

    /// <summary>
    /// 按触发值配置两个钩子的角色：
    ///  - 鼠标键（X1/X2/Middle）：鼠标钩子负责 TriggerDown/Up，键盘钩子 Combo 留空
    ///  - 键盘组合：键盘钩子 Combo = trigger，鼠标钩子 Button 保留但不再作为触发源
    /// 两个钩子都始终安装（鼠标钩子始终负责 MouseMove 用于方向追踪）。
    /// </summary>
    private void ApplyTriggerMode(string trigger)
    {
        if (SpinixConfig.IsKeyboardTrigger(trigger))
        {
            _kbdHook.Combo = trigger;
            // 鼠标键触发源关闭：Button 设为默认 X1 但不会订阅其 TriggerDown/Up
        }
        else
        {
            _kbdHook.Combo = null;
            _hook.Button = ParseButton(trigger);
        }
    }

    /// <summary>带托盘占位的构造重载（兼容旧调用）。</summary>
    public WheelController(SpinixConfig config, object tray) : this(config) { }

    public void Start()
    {
        // 钩子必须在带消息循环的线程安装；WPF 主线程即满足。两个钩子都装。
        _dispatcher.BeginInvoke(new Action(() =>
        {
            bool mouseOk = _hook.Install();
            bool kbdOk = _kbdHook.Install();
            if (!mouseOk && !kbdOk)
                SpinixLogger.Warn("Controller", "钩子未安装成功——触发键将无响应，请查看日志诊断");
        }), DispatcherPriority.Send);

        // MouseMove 始终由鼠标钩子提供（键盘触发也需要追踪光标方向选择扇区）
        _hook.MouseMove += OnMouseMove;
        // TriggerDown/Up 按当前触发模式接驳到对应钩子
        WireTriggerEvents();

        SpinixLogger.Info("Controller",
            $"轮盘控制器启动：trigger={_config.Trigger}，主轮盘={_config.GetMainWheel()?.Items.Count ?? 0} 项，半径={_config.WheelRadius}");
    }

    /// <summary>
    /// 按当前触发模式接驳 TriggerDown/Up 事件：先退订两个钩子，再只订阅激活的那个。
    /// 鼠标键模式 → 订阅 _hook；键盘组合模式 → 订阅 _kbdHook。
    /// 在 Start 和 UpdateConfig（热切换触发模式）时调用。
    /// </summary>
    private void WireTriggerEvents()
    {
        _hook.TriggerDown -= OnTriggerDown;
        _hook.TriggerUp -= OnTriggerUp;
        _kbdHook.TriggerDown -= OnTriggerDown;
        _kbdHook.TriggerUp -= OnTriggerUp;

        if (SpinixConfig.IsKeyboardTrigger(_config.Trigger))
        {
            _kbdHook.TriggerDown += OnTriggerDown;
            _kbdHook.TriggerUp += OnTriggerUp;
        }
        else
        {
            _hook.TriggerDown += OnTriggerDown;
            _hook.TriggerUp += OnTriggerUp;
        }
    }

    /// <summary>热生效：替换配置（设置窗口保存后调用）。</summary>
    public void UpdateConfig(SpinixConfig newConfig)
    {
        _config = newConfig;
        _hook.SuppressTriggerEvents = newConfig.SuppressTriggerEvents;
        _kbdHook.SuppressTriggerEvents = newConfig.SuppressTriggerEvents;
        // 重新配置触发模式并接驳事件（支持鼠标键↔键盘组合热切换）
        ApplyTriggerMode(newConfig.Trigger);
        WireTriggerEvents();

        // 若当前正显示轮盘，刷新显示
        if (_active && _currentWheel != null)
        {
            // 重置节流状态：新配置可能改变了扇区数/半径，确保首次移动必定刷新 UI
            _lastDisplayedIndex = int.MinValue;
            _subWheelState.Reset();
            _dispatcher.BeginInvoke(new Action(() =>
            {
                _window.ShowAt(_center, _currentWheel!.Items, newConfig.WheelRadius, newConfig.DeadZoneRadius);
            }), DispatcherPriority.Send);
        }
    }

    // ---- 状态机 ----
    private void OnTriggerDown(object? sender, HookMouseEventArgs e)
    {
        if (_active) return;

        // 全屏模式检查
        if (_config.DisableInFullScreen && FullScreenHelper.IsFullScreen())
        {
            SpinixLogger.Info("Controller", "全屏模式已检测到且配置为禁用，拦截唤起请求");
            return;
        }

        var main = _config.GetMainWheel();
        if (main == null || main.Items.Count == 0)
        {
            SpinixLogger.Warn("Controller", "按下触发键，但主轮盘为空——无内容可显示");
            return;
        }

        // 钩子坐标是物理像素，转逻辑像素
        _center = DpiHelper.PhysicalToLogical(new Point(e.X, e.Y));
        SpinixLogger.Info("Controller", $"触发键按下 @ ({e.X},{e.Y}) → 逻辑 ({_center.X:F0},{_center.Y:F0})，打开轮盘「{main.Name}」");
        OpenWheel(main);
    }

    private void OpenWheel(Wheel wheel)
    {
        _currentWheel = wheel;
        _active = true;
        _subWheelState.Reset();
        _lastCursorLogical = _center;
        _lastDisplayedIndex = int.MinValue; // 重置节流，确保首次移动必定刷新 UI

        _dispatcher.BeginInvoke(new Action(() =>
        {
            _window.ShowAt(_center, wheel.Items, _config.WheelRadius, _config.DeadZoneRadius);
        }), DispatcherPriority.Send);

        // 启动轮询计时器（鼠标静止时也能推进进入/回退判定）
        StartOrResetPollTimer();
    }

    private void OnMouseMove(object? sender, HookMouseEventArgs e)
    {
        if (!_active || _currentWheel == null) return;
        var logical = DpiHelper.PhysicalToLogical(new Point(e.X, e.Y));
        UpdateHoverFromCursor(logical);
    }

    private int _lastDisplayedIndex = int.MinValue; // 节流：上次显示的扇区索引

    private void UpdateHoverFromCursor(Point logicalCursor)
    {
        if (_currentWheel == null) return;
        _lastCursorLogical = logicalCursor;
        int idx = SectorHitTest.HitTest(_center, logicalCursor, _currentWheel.Items.Count, _config.DeadZoneRadius);

        // 节流：仅在扇区索引变化时才更新 UI，避免高频鼠标移动堆积 Dispatcher 调用。
        // （鼠标在同一扇区内移动时 idx 不变，无需重绘；状态机仍每次推进以准确计时）
        if (WheelTiming.ShouldUpdateDisplay(idx, _lastDisplayedIndex))
        {
            _lastDisplayedIndex = idx;
            _dispatcher.BeginInvoke(new Action(() => _window.UpdateHover(idx, logicalCursor)), DispatcherPriority.Send);
        }

        // 子轮盘状态机推进（不受节流影响，保证悬停计时准确）
        EvaluateSubWheelState(idx);
    }

    /// <summary>把当前命中索引喂给状态机，按决策执行（进入/回退/无操作）。</summary>
    private void EvaluateSubWheelState(int idx)
    {
        if (_currentWheel == null) return;
        bool isInSubWheel = _wheelStack.Count > 0;

        var decision = _subWheelState.ProcessHover(
            hitIndex: idx,
            isInSubWheel: isInSubWheel,
            isSubWheelSector: i => i >= 0 && i < _currentWheel.Items.Count
                                   && _currentWheel.Items[i].ActionType == WheelActionType.SubWheel,
            nowTicks: DateTime.UtcNow.Ticks,
            enterDelayMs: _config.SubWheelEnterDelayMs,
            retreatDelayMs: _config.SubWheelRetreatDelayMs);

        switch (decision.Kind)
        {
            case SubWheelDecisionKind.EnterSubWheel:
                TryEnterSubWheel(decision.TargetIndex);
                break;
            case SubWheelDecisionKind.RetreatToParent:
                RetreatToParentWheel();
                break;
        }
    }

    /// <summary>轮询计时器：鼠标静止时仍按固定周期推进状态机，使进入/回退能及时触发。</summary>
    private void StartOrResetPollTimer()
    {
        _pollTimer?.Stop();
        // 轮询周期取进入/回退阈值的 1/3，但不少于 16ms（~60fps）且不超过 60ms
        int enter = WheelTiming.NormalizeEnterDelay(_config.SubWheelEnterDelayMs);
        int retreat = WheelTiming.NormalizeEnterDelay(_config.SubWheelRetreatDelayMs);
        int minDelay = Math.Min(enter, retreat);
        int interval = Math.Clamp(minDelay / 3, 16, 60);
        _pollTimer = new DispatcherTimer(DispatcherPriority.Send)
        {
            Interval = TimeSpan.FromMilliseconds(interval),
        };
        _pollTimer.Tick += (s, e) =>
        {
            if (!_active || _currentWheel == null) return;
            // 复算当前命中并推进状态机（鼠标静止时也能进入/回退）
            int idx = SectorHitTest.HitTest(_center, _lastCursorLogical,
                _currentWheel.Items.Count, _config.DeadZoneRadius);
            EvaluateSubWheelState(idx);
        };
        _pollTimer.Start();
    }

    private void TryEnterSubWheel(int targetIndex)
    {
        if (_currentWheel == null) return;
        if (targetIndex < 0 || targetIndex >= _currentWheel.Items.Count) return;
        var item = _currentWheel.Items[targetIndex];
        if (item.ActionType != WheelActionType.SubWheel) return;
        var sub = _config.FindWheel(item.Argument);
        if (sub == null || sub.Items.Count == 0) return;

        _wheelStack.Push(_currentWheel.Id);
        OpenWheel(sub);
    }

    private void RetreatToParentWheel()
    {
        if (_wheelStack.Count == 0) return;
        var parentId = _wheelStack.Pop();
        var parent = _config.FindWheel(parentId);
        if (parent == null) return;
        OpenWheel(parent);
    }

    private readonly Stack<string> _wheelStack = new();

    private void OnTriggerUp(object? sender, HookMouseEventArgs e)
    {
        if (!_active || _currentWheel == null) { _wheelStack.Clear(); return; }
        var logical = DpiHelper.PhysicalToLogical(new Point(e.X, e.Y));
        int idx = SectorHitTest.HitTest(_center, logical, _currentWheel.Items.Count, _config.DeadZoneRadius);

        // 关闭轮盘
        _active = false;
        _pollTimer?.Stop();
        _subWheelState.Reset();
        _dispatcher.BeginInvoke(new Action(() => _window.Hide()), DispatcherPriority.Send);

        if (idx < 0)
        {
            // 死区：取消，清空轮盘栈
            _wheelStack.Clear();
            _currentWheel = null;
            return;
        }

        var item = _currentWheel.Items[idx];
        _currentWheel = null;
        _wheelStack.Clear();

        // SubWheel 的进入由悬停计时处理；松开时若仍在 SubWheel 扇区 = 取消（不重新打开）
        if (item.ActionType == WheelActionType.SubWheel)
        {
            SpinixLogger.Info("Controller", $"松开在 SubWheel 扇区 [{item.Name}]——取消（不执行）");
            return;
        }

        // 普通动作：异步执行，避免阻塞钩子线程
        var toExec = item;
        SpinixLogger.Info("Controller", $"松开 → 执行动作「{item.Name}」({item.ActionType}: {item.Argument})");
        _dispatcher.BeginInvoke(new Action(() => _executor.Execute(toExec)), DispatcherPriority.Normal);
    }

    private static LowLevelMouseHook.TriggerButton ParseButton(string? trigger)
        => LowLevelMouseHook.ParseTriggerButton(trigger);

    public void Dispose()
    {
        _hook.TriggerDown -= OnTriggerDown;
        _hook.TriggerUp -= OnTriggerUp;
        _kbdHook.TriggerDown -= OnTriggerDown;
        _kbdHook.TriggerUp -= OnTriggerUp;
        _hook.MouseMove -= OnMouseMove;
        _pollTimer?.Stop();
        _hook.Dispose();
        _kbdHook.Dispose();
        _dispatcher.BeginInvoke(new Action(() => _window.Close()), DispatcherPriority.Send);
    }
}
