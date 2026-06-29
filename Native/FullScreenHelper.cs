using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;

namespace Spinix.Native;

public static class FullScreenHelper
{
    public static bool IsFullScreen()
    {
        IntPtr foregroundWindow = NativeMethods.GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero) return false;

        IntPtr desktopWindow = NativeMethods.GetDesktopWindow();
        IntPtr shellWindow = NativeMethods.GetShellWindow();

        if (foregroundWindow == desktopWindow || foregroundWindow == shellWindow)
            return false;

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
}
