using System.Diagnostics;
using System.Runtime.InteropServices;
using Spinix.Actions;
using Spinix.Diagnostics;
using static Spinix.Native.NativeMethods;

namespace Spinix.Native;

/// <summary>
/// 全局底层键盘钩子（WH_KEYBOARD_LL）。用于把任意键盘键或组合键（如 Ctrl+Q、F8）
/// 作为轮盘触发键——与 <see cref="LowLevelMouseHook"/> 的鼠标键触发二选一。
///
/// 工作方式：设置 <see cref="Combo"/> 为组合字符串（如 "Ctrl+Q"），钩子在每次按键
/// 回调里用 <see cref="ShortcutParser"/> 解析并与 KBDLLHOOKSTRUCT.vkCode 匹配；
/// 修饰键通过 GetAsyncKeyState 实时校验是否同时按下。
///
/// 触发事件复用 <see cref="HookMouseEventArgs"/>——X/Y 填当前鼠标位置（GetCursorPos），
/// 使 WheelController 的 OnTriggerDown/Up 坐标逻辑无需区分触发来源。
/// </summary>
public sealed class LowLevelKeyboardHook : IDisposable
{
    private readonly LowLevelMouseProc _proc; // 委托签名与键盘钩子一致，复用类型
    private IntPtr _hookId = IntPtr.Zero;
    private bool _disposed;

    // 当前生效的组合（解析缓存）。Combo setter 更新时重新解析。
    private string? _combo;
    private byte _mainVk;                    // 主键虚拟键码
    private IReadOnlyList<byte> _modifierVks = Array.Empty<byte>();

    /// <summary>触发键被按下时发生（X/Y 为当前鼠标位置）。</summary>
    public event EventHandler<HookMouseEventArgs>? TriggerDown;

    /// <summary>触发键被松开时发生（X/Y 为当前鼠标位置）。</summary>
    public event EventHandler<HookMouseEventArgs>? TriggerUp;

    /// <summary>
    /// 触发组合字符串（如 "Ctrl+Q"）。设为 null/空则不匹配任何键（禁用键盘触发）。
    /// setter 重新解析并缓存虚拟键码，避免每次回调重复解析。
    /// </summary>
    public string? Combo
    {
        get => _combo;
        set
        {
            _combo = value;
            if (ShortcutParser.TryParseCombo(value, out var main, out var mods))
            {
                _mainVk = main;
                _modifierVks = mods;
            }
            else
            {
                _mainVk = 0;
                _modifierVks = Array.Empty<byte>();
            }
        }
    }

    /// <summary>
    /// 是否在按下/松开触发键时“吃掉”该事件（返回非零，阻止传递给前台应用）。
    /// 默认 true，避免触发组合被前台应用误响应。复用鼠标钩子的同一配置项。
    /// </summary>
    public bool SuppressTriggerEvents { get; set; } = true;

    public LowLevelKeyboardHook()
    {
        _proc = HookCallback;
    }

    /// <summary>安装钩子。必须在拥有消息循环的线程上调用（通常通过 Dispatcher）。</summary>
    /// <returns>安装是否成功。失败原因记录到日志。</returns>
    public bool Install()
    {
        if (_hookId != IntPtr.Zero) return true;
        try
        {
            using var process = Process.GetCurrentProcess();
            using var module = process.MainModule!;
            _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(module.ModuleName!), 0);

            if (_hookId == IntPtr.Zero)
            {
                int err = Marshal.GetLastWin32Error();
                SpinixLogger.Error("KbdHook",
                    $"SetWindowsHookEx(WH_KEYBOARD_LL) 失败，Win32 错误码={err}");
                return false;
            }

            SpinixLogger.Info("KbdHook", $"WH_KEYBOARD_LL 安装成功，combo={_combo ?? "(未设置)"}，suppress={SuppressTriggerEvents}");
            return true;
        }
        catch (Exception ex)
        {
            SpinixLogger.Error("KbdHook", "安装键盘钩子异常", ex);
            return false;
        }
    }

    /// <summary>钩子当前是否已成功安装。</summary>
    public bool IsInstalled => _hookId != IntPtr.Zero;

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        // 未配置组合时不参与匹配，直接透传（鼠标钩子仍独立工作）
        if (nCode >= 0 && _mainVk != 0)
        {
            int msg = wParam.ToInt32();
            var kbd = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

            // WM_SYSKEYDOWN/UP 是带 Alt 的按键，也要处理（Alt+X 组合会走 SYS 路径）
            bool isDown = msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN;
            bool isUp = msg == WM_KEYUP || msg == WM_SYSKEYUP;

            if ((isDown || isUp) && kbd.vkCode == _mainVk)
            {
                // 校验所有修饰键当前是否同时按下
                if (AllModifiersDown())
                {
                    var args = CurrentCursorArgs();
                    if (isDown)
                    {
                        TriggerDown?.Invoke(this, args);
                        if (SuppressTriggerEvents) return new IntPtr(1); // 吃掉主键按下
                    }
                    else
                    {
                        TriggerUp?.Invoke(this, args);
                        if (SuppressTriggerEvents) return new IntPtr(1); // 吃掉主键松开
                    }
                }
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    /// <summary>检查所有配置的修饰键当前是否同时处于按下状态。</summary>
    private bool AllModifiersDown()
    {
        foreach (var vk in _modifierVks)
        {
            // GetAsyncKeyState 返回值最高位（0x8000）为 1 表示当前按下
            if ((GetAsyncKeyState(vk) & 0x8000) == 0)
                return false;
        }
        return true;
    }

    /// <summary>取当前鼠标位置（物理像素），填充到事件参数——键盘触发无固有坐标。</summary>
    private static HookMouseEventArgs CurrentCursorArgs()
    {
        GetCursorPos(out var pt);
        return new HookMouseEventArgs(pt.X, pt.Y);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }
}
