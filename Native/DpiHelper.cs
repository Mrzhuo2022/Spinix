using System.Windows;
using System.Windows.Media;

namespace Spinix.Native;

/// <summary>
/// DPI 适配助手：底层钩子返回的 GetCursorPos 是物理像素，
/// 而 WPF Window 的 Left/Top/Width/Height 使用逻辑像素（device-independent units, 1/96 inch）。
/// 在 DPI &gt; 100%（如 150%）的显示器上，不换算会导致轮盘位置偏移、尺寸缩小。
/// </summary>
public static class DpiHelper
{
    /// <summary>
    /// 获取指定屏幕坐标处的 DPI 缩放因子（X 方向）。
    /// 优先用 Win32 <c>MonitorFromPoint</c>+<c>GetDpiForMonitor</c> 查询该物理坐标所在显示器，
    /// 不依赖任何 WPF 窗口——因此轮盘首次显示（窗口尚无 HWND）时也能取到正确 DPI。
    /// 失败时回退到 WPF 视觉树的 TransformToDevice；仍失败则 1.0。
    /// </summary>
    /// <remarks>
    /// 注意：旧实现仅用 WPF 视觉树，导致每次启动的第一次触发时轮盘窗口还未创建 HWND，
    /// PresentationSource 为 null → 误回退 1.0 → 在非 100% DPI 上首轮位置错乱。
    /// 需要测试的“物理↔逻辑像素换算”本身是纯数学，请用 <see cref="PhysicalToLogical"/> /
    /// <see cref="LogicalToPhysical"/> 的可注入 scale 重载，或 <see cref="ConvertPhysicalToLogical"/>。
    /// </remarks>
    public static double GetDpiScaleAtPoint(double physicalX, double physicalY)
    {
        // 1. 首选：Win32 按显示器查询，不依赖 WPF 窗口（首轮即可靠）
        try
        {
            double scale = QueryMonitorDpiScale(physicalX, physicalY);
            if (scale > 0) return scale;
        }
        catch { /* 降级到下面 */ }

        // 2. 回退：WPF 视觉树（窗口已有 HWND 时可用）
        try
        {
            var src = PresentationSource.FromVisual(GetAnyVisual());
            if (src?.CompositionTarget != null)
            {
                return src.CompositionTarget.TransformToDevice.M11;
            }
        }
        catch { /* 降级 */ }
        return 1.0;
    }

    /// <summary>
    /// 用 MonitorFromPoint + GetDpiForMonitor 查询指定物理坐标所在显示器的 DPI 缩放因子。
    /// 返回 0 表示查询失败（如 shcore.dll 不可用），由调用方回退。
    /// </summary>
    private static double QueryMonitorDpiScale(double physicalX, double physicalY)
    {
        var pt = new NativeMethods.POINT
        {
            X = (int)Math.Round(physicalX),
            Y = (int)Math.Round(physicalY),
        };
        IntPtr hmon = NativeMethods.MonitorFromPoint(pt, NativeMethods.MONITOR_DEFAULTTONEAREST);
        if (hmon == IntPtr.Zero) return 0;
        int hr = NativeMethods.GetDpiForMonitor(hmon, NativeMethods.MDT_EFFECTIVE_DPI, out uint dpiX, out _);
        if (hr != 0 || dpiX == 0) return 0;
        return dpiX / 96.0;
    }

    /// <summary>物理像素 → 逻辑像素（WPF 单位），使用运行时查询到的 scale。</summary>
    public static Point PhysicalToLogical(Point physical)
    {
        double scale = GetDpiScaleAtPoint(physical.X, physical.Y);
        return ConvertPhysicalToLogical(physical, scale);
    }

    /// <summary>逻辑像素 → 物理像素，使用运行时查询到的 scale。</summary>
    public static Point LogicalToPhysical(Point logical)
    {
        double scale = GetDpiScaleAtPoint(logical.X, logical.Y);
        return ConvertLogicalToPhysical(logical, scale);
    }

    /// <summary>物理像素 → 逻辑像素（纯数学，给定 scale）。可单元测试。</summary>
    public static Point ConvertPhysicalToLogical(Point physical, double scale)
    {
        double s = NormalizeScale(scale);
        return new Point(physical.X / s, physical.Y / s);
    }

    /// <summary>逻辑像素 → 物理像素（纯数学，给定 scale）。可单元测试。</summary>
    public static Point ConvertLogicalToPhysical(Point logical, double scale)
    {
        double s = NormalizeScale(scale);
        return new Point(logical.X * s, logical.Y * s);
    }

    /// <summary>归一化缩放因子：拒绝 &lt;=0，避免除零；上限保护防异常值。</summary>
    public static double NormalizeScale(double scale)
    {
        if (scale <= 0 || double.IsNaN(scale) || double.IsInfinity(scale))
            return 1.0;
        // Windows 支持的自定义 DPI 上限约 500%（5.0），超出视为异常
        if (scale > 10.0)
            return 1.0;
        return scale;
    }

    /// <summary>获取鼠标当前位置（物理像素），并换算为 WPF 逻辑像素。</summary>
    public static Point GetLogicalCursorPos()
    {
        NativeMethods.GetCursorPos(out var pt);
        return PhysicalToLogical(new Point(pt.X, pt.Y));
    }

    /// <summary>获取一个有效 Visual 用于查询 DPI（兜底）。</summary>
    private static Visual? GetAnyVisual()
    {
        var app = Application.Current;
        if (app == null) return null;
        foreach (Window w in app.Windows)
            return w;
        return null;
    }
}
