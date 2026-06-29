namespace Spinix.Config;

/// <summary>单个轮盘的配置。</summary>
public sealed class Wheel
{
    public string Id { get; set; } = "main";

    public string Name { get; set; } = "Main Wheel";

    /// <summary>扇区条目列表。</summary>
    public List<WheelItem> Items { get; set; } = new();
}

/// <summary>Spinix 全局配置。</summary>
public sealed class SpinixConfig
{
    public int Version { get; set; } = 1;

    /// <summary>
    /// 触发按键：鼠标键 "X1" / "X2" / "Middle"，或键盘组合字符串（如 "Ctrl+Q"、"F8"）。
    /// 键盘组合的判定见 <see cref="IsKeyboardTrigger"/>。
    /// </summary>
    public string Trigger { get; set; } = "X1";

    /// <summary>
    /// 判断触发值是否为键盘组合（而非鼠标键）。
    /// 规则：值为 X1/X2/Middle（不区分大小写）→ 鼠标键（返回 false）；
    /// 否则（含 + 的组合、或单键名如 F8）→ 键盘触发（返回 true）。
    /// 纯函数，可单元测试。
    /// </summary>
    public static bool IsKeyboardTrigger(string? trigger)
    {
        if (string.IsNullOrWhiteSpace(trigger)) return false;
        return trigger.Trim() switch
        {
            "X1" => false,
            "X2" => false,
            "Middle" => false,
            { } s when s.Equals("X1", StringComparison.OrdinalIgnoreCase) => false,
            { } s when s.Equals("X2", StringComparison.OrdinalIgnoreCase) => false,
            { } s when s.Equals("Middle", StringComparison.OrdinalIgnoreCase) => false,
            _ => true,
        };
    }

    /// <summary>是否开机自启。</summary>
    public bool AutoStart { get; set; } = true;

    /// <summary>是否在按下触发键时“吃掉”该事件（阻止前台应用收到）。</summary>
    public bool SuppressTriggerEvents { get; set; } = true;

    /// <summary>是否在全屏界面下禁止显示轮盘。</summary>
    public bool DisableInFullScreen { get; set; } = true;

    /// <summary>轮盘半径（逻辑像素）。</summary>
    public int WheelRadius { get; set; } = 180;

    /// <summary>中心死区半径（逻辑像素）。光标进入死区松开 = 取消。</summary>
    public int DeadZoneRadius { get; set; } = 32;

    /// <summary>
    /// 按住触发键期间，光标悬停在 SubWheel 扇区多少毫秒后自动进入子轮盘。
    /// 合法范围 [50, 2000]；越短越灵敏（容易误触），越长越迟钝。默认 220ms。
    /// 实际使用前由 <see cref="Spinix.Wheels.WheelTiming.NormalizeEnterDelay"/> 校验。
    /// </summary>
    public int SubWheelEnterDelayMs { get; set; } = 220;

    /// <summary>
    /// 在子轮盘内，光标移回中心死区多少毫秒后自动回退到父轮盘。
    /// 复用与进入相同的语义；为独立可调而分开。默认 220ms。
    /// </summary>
    public int SubWheelRetreatDelayMs { get; set; } = 220;

    /// <summary>
    /// UI 语言代码（"zh-CN" / "en"）。空字符串或 null 表示跟随系统。
    /// </summary>
    public string Language { get; set; } = "";

    /// <summary>所有轮盘（含主轮盘，可能含二级轮盘）。</summary>
    public List<Wheel> Wheels { get; set; } = new();

    /// <summary>获取主轮盘；不存在则返回 null。</summary>
    public Wheel? GetMainWheel() => Wheels.FirstOrDefault(w => w.Id == "main") ?? Wheels.FirstOrDefault();

    /// <summary>按 id 查找轮盘。</summary>
    public Wheel? FindWheel(string id) => Wheels.FirstOrDefault(w => w.Id == id);

    /// <summary>生成默认配置。</summary>
    public static SpinixConfig CreateDefault()
    {
        var config = new SpinixConfig
        {
            Trigger = "X1",
            AutoStart = true,
            SuppressTriggerEvents = true,
            DisableInFullScreen = true,
            WheelRadius = 180,
            DeadZoneRadius = 32,
            SubWheelEnterDelayMs = 220,
            SubWheelRetreatDelayMs = 220,
            Language = "",
        };

        var main = new Wheel { Id = "main", Name = "Main Wheel" };
        main.Items = new List<WheelItem>
        {
            new() { Name = "Terminal",      Icon = "terminal", ActionType = WheelActionType.LaunchApp,   Argument = "wt.exe" },
            new() { Name = "Browser",       Icon = "globe",    ActionType = WheelActionType.OpenUrl,     Argument = "https://www.google.com" },
            new() { Name = "Explorer",      Icon = "folder",   ActionType = WheelActionType.OpenFolder,  Argument = @"C:\Users" },
            new() { Name = "Volume Up",     Icon = "volume",   ActionType = WheelActionType.SystemAction,Argument = nameof(SystemActionKind.VolumeUp) },
            new() { Name = "Lock",          Icon = "lock",     ActionType = WheelActionType.SystemAction,Argument = nameof(SystemActionKind.LockScreen) },
            new() { Name = "Task View",     Icon = "grid",     ActionType = WheelActionType.SystemAction,Argument = nameof(SystemActionKind.TaskView) },
        };
        config.Wheels.Add(main);
        return config;
    }
}
