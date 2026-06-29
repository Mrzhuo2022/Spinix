using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Forms;

namespace Spinix.Native;

public static class FullScreenHelper
{
    /// <summary>
    /// 检测前台窗口是否为全屏应用（游戏/视频播放器的独占或无边框全屏）。
    /// 用于 DisableInFullScreen 配置——全屏时禁止唤起轮盘避免干扰。
    ///
    /// 判定逻辑（依次）：
    ///  1. 无前台窗口 → 非全屏
    ///  2. 前台是桌面（Progman/WorkerW，或 GetDesktopWindow/GetShellWindow）→ 非全屏
    ///  3. 前台窗口矩形覆盖整个屏幕 → 全屏
    /// </summary>
    /// <remarks>
    /// 桌面窗口识别是关键修正：Windows 桌面由 Progman（ShellWindow）+ WorkerW（壁纸容器）
    /// 双层窗口组成，显示桌面时 GetForegroundWindow 可能返回 WorkerW（句柄与 ShellWindow 不同），
    /// 旧实现只比对 ShellWindow 句柄 → WorkerW 漏网 → 其矩形覆盖全屏 → 误判桌面为全屏。
    /// 改用类名识别后，无论哪个成为前台都能正确排除。
    /// </remarks>
    public static bool IsFullScreen()
    {
        IntPtr foregroundWindow = NativeMethods.GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero) return false;

        // 排除桌面窗口（含 WorkerW 壁纸容器，避免“显示桌面”时误判）
        if (IsDesktopWindow(foregroundWindow)) return false;

        if (!NativeMethods.GetWindowRect(foregroundWindow, out var rect))
            return false;

        // 获取当前窗口所在的屏幕
        var screen = Screen.FromHandle(foregroundWindow);
        var bounds = screen.Bounds;

        // 如果窗口矩形覆盖了整个屏幕，则认为是全屏
        return rect.Left <= bounds.X &&
               rect.Top <= bounds.Y &&
               rect.Right >= bounds.X + bounds.Width &&
               rect.Bottom >= bounds.Y + bounds.Height;
    }

    /// <summary>
    /// 判断窗口句柄是否为桌面相关窗口：
    ///  - GetDesktopWindow / GetShellWindow 返回的句柄
    ///  - 类名为 Progman（桌面主窗口）或 WorkerW（壁纸/桌面内容容器）
    /// </summary>
    private static bool IsDesktopWindow(IntPtr hWnd)
    {
        if (hWnd == NativeMethods.GetDesktopWindow()) return true;
        if (hWnd == NativeMethods.GetShellWindow()) return true;

        // 类名识别：覆盖 Progman 与 WorkerW（显示桌面时前台常为 WorkerW）
        var className = new StringBuilder(256);
        if (NativeMethods.GetClassName(hWnd, className, className.Capacity) > 0)
        {
            var name = className.ToString();
            if (name == "Progman" || name == "WorkerW")
                return true;
        }
        return false;
    }
}
