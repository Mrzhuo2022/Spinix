using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Threading;
using Spinix.Diagnostics;
using static Spinix.Native.NativeMethods;

namespace Spinix.Native;

/// <summary>
/// 全局底层鼠标钩子（WH_MOUSE_LL）。用于可靠捕获鼠标侧键 X1/X2 的按下与松开事件——
/// RegisterHotKey 拿不到松开时刻，所以必须用底层钩子。
/// </summary>
public sealed class LowLevelMouseHook : IDisposable
{
    /// <summary>触发键类型。</summary>
    public enum TriggerButton
    {
        /// <summary>鼠标侧键 1（X1）</summary>
        X1,
        /// <summary>鼠标侧键 2（X2）</summary>
        X2,
        /// <summary>鼠标中键（滚轮按下）</summary>
        Middle,
    }

    /// <summary>
    /// 把配置中的触发键字符串解析为 <see cref="TriggerButton"/>。
    /// 解析不区分大小写、忽略前后空白；任何无法识别的值（含 null/空）回退到 <see cref="TriggerButton.X1"/>（默认）。
    /// 纯函数，可单元测试。
    /// </summary>
    public static TriggerButton ParseTriggerButton(string? trigger)
    {
        if (string.IsNullOrWhiteSpace(trigger)) return TriggerButton.X1;
        return trigger.Trim() switch
        {
            "X2" => TriggerButton.X2,
            "Middle" => TriggerButton.Middle,
            // 不区分大小写
            { } s when s.Equals("X2", StringComparison.OrdinalIgnoreCase) => TriggerButton.X2,
            { } s when s.Equals("Middle", StringComparison.OrdinalIgnoreCase) => TriggerButton.Middle,
            _ => TriggerButton.X1,
        };
    }

    private readonly LowLevelMouseProc _proc;
    private IntPtr _hookId = IntPtr.Zero;
    private bool _disposed;

    /// <summary>触发键被按下时发生。</summary>
    public event EventHandler<HookMouseEventArgs>? TriggerDown;

    /// <summary>触发键被松开时发生。</summary>
    public event EventHandler<HookMouseEventArgs>? TriggerUp;

    /// <summary>鼠标移动时发生（始终在按下/松开间隙触发，供方向追踪用）。</summary>
    public event EventHandler<HookMouseEventArgs>? MouseMove;

    public TriggerButton Button { get; set; } = TriggerButton.X1;

    /// <summary>
    /// 是否在按下/松开触发键时“吃掉”该事件（返回非零，阻止传递给前台应用）。
    /// 默认 true，避免按下 X1 时前台应用误触发后退。
    /// </summary>
    public bool SuppressTriggerEvents { get; set; } = true;

    public LowLevelMouseHook()
    {
        _proc = HookCallback;
    }

    /// <summary>安装钩子。必须在拥有消息循环的线程上调用（通常通过 Dispatcher）。</summary>
    /// <returns>安装是否成功。失败原因记录到日志（如另一实例占用、权限不足）。</returns>
    public bool Install()
    {
        if (_hookId != IntPtr.Zero) return true;
        try
        {
            using var process = Process.GetCurrentProcess();
            using var module = process.MainModule!;
            _hookId = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(module.ModuleName!), 0);

            if (_hookId == IntPtr.Zero)
            {
                // 安装失败：钩子无法捕获任何按键，应用将完全失灵（"用不了"的典型根因）。
                // 常见原因：另一实例已在运行、系统钩子配额耗尽、安全软件拦截。
                int err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                SpinixLogger.Error("Hook",
                    $"SetWindowsHookEx 失败，Win32 错误码={err}（可能已有 Spinix 实例在运行或被安全软件拦截）");
                return false;
            }

            SpinixLogger.Info("Hook", $"WH_MOUSE_LL 安装成功，监听按钮={Button}，suppress={SuppressTriggerEvents}");
            return true;
        }
        catch (Exception ex)
        {
            SpinixLogger.Error("Hook", "安装钩子异常", ex);
            return false;
        }
    }

    /// <summary>钩子当前是否已成功安装（供自检/诊断）。</summary>
    public bool IsInstalled => _hookId != IntPtr.Zero;

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();
            var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            var args = new HookMouseEventArgs(hookStruct.pt.X, hookStruct.pt.Y);

            switch (msg)
            {
                case WM_XBUTTONDOWN when Button == TriggerButton.X1 && HIWORD(hookStruct.mouseData) == XBUTTON1:
                case WM_XBUTTONDOWN when Button == TriggerButton.X2 && HIWORD(hookStruct.mouseData) == XBUTTON2:
                case WM_MBUTTONDOWN when Button == TriggerButton.Middle:
                    TriggerDown?.Invoke(this, args);
                    if (SuppressTriggerEvents) return new IntPtr(1); // 吃掉事件
                    break;

                case WM_XBUTTONUP when Button == TriggerButton.X1 && HIWORD(hookStruct.mouseData) == XBUTTON1:
                case WM_XBUTTONUP when Button == TriggerButton.X2 && HIWORD(hookStruct.mouseData) == XBUTTON2:
                case WM_MBUTTONUP when Button == TriggerButton.Middle:
                    TriggerUp?.Invoke(this, args);
                    if (SuppressTriggerEvents) return new IntPtr(1);
                    break;

                case WM_MOUSEMOVE:
                    MouseMove?.Invoke(this, args);
                    break;
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private static int HIWORD(uint value) => unchecked((short)(value >> 16));

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

public sealed class HookMouseEventArgs : EventArgs
{
    public int X { get; }
    public int Y { get; }
    public HookMouseEventArgs(int x, int y) { X = x; Y = y; }
}
