using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using Spinix.Config;
using Spinix.Native;
using static Spinix.Native.NativeMethods;

namespace Spinix.Wheels;

/// <summary>
/// 轮盘 UI 叠加层：透明、置顶、点击穿透的 WPF 窗口。
/// 不接收任何鼠标事件（WS_EX_TRANSPARENT），所有输入由全局钩子追踪。
/// 关键不变量：无淡入淡出——窗口 Show/Hide 即时切换。
///
/// 坐标系：本窗口所有坐标均为 WPF 逻辑像素（device-independent units）。
/// 控制器在传入前已用 DpiHelper 把钩子的物理像素换算为逻辑像素。
/// </summary>
    public partial class WheelWindow : Window
    {
        public WheelWindow()
        {
            InitializeComponent();
            SourceInitialized += OnSourceInitialized;
            IsVisibleChanged += (s, e) =>
            {
                if ((bool)e.NewValue) ForceTopmost();
            };
        }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var helper = new WindowInteropHelper(this);
        IntPtr hwnd = helper.Handle;

        // 1. 应用点击穿透扩展样式
        int ex = unchecked((int)(long)GetWindowLongCompat(hwnd, GWL_EXSTYLE));
        ex = WindowExStyleBuilder.ApplyWheelOverlayFlags(ex);
        SetWindowLongCompat(hwnd, GWL_EXSTYLE, (IntPtr)ex);

        // 2. 开启背景模糊 (Blur Behind)
        EnableBlur(hwnd);
    }

    private void EnableBlur(IntPtr hwnd)
    {
        var accent = new AccentPolicy();
        // 在 Win10 1803+ 建议使用 ACCENT_ENABLE_ACRYLICBLURBEHIND (4)
        // 但为了兼容性，ACCENT_ENABLE_BLURBEHIND (3) 更稳定
        accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;
        accent.GradientColor = 0x00FFFFFF; // 透明背景

        int size = Marshal.SizeOf(accent);
        IntPtr pointer = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(accent, pointer, false);
            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                Data = pointer,
                SizeOfData = size
            };
            SetWindowCompositionAttribute(hwnd, ref data);
        }
        finally { Marshal.FreeHGlobal(pointer); }
    }

    /// <summary>把轮盘以 center 为屏幕中心即时显示。所有坐标为逻辑像素。</summary>
    public void ShowAt(Point center, IList<WheelItem> items, double radius, double deadZone)
    {
        Wheel.Items = items;
        Wheel.Radius = radius;
        Wheel.DeadZoneRadius = deadZone;
        Wheel.HoverIndex = -1;

        double size = radius * 2 + 100; // 与 WheelControl.MeasureOverride 的预留一致
        Width = size;
        Height = size;

        // 左上 = center - (size/2, size/2)
        Left = center.X - size / 2;
        Top = center.Y - size / 2;

        // 立即显示，无动画
        Show();
        Topmost = true;

        // 强制把 HWND 提到 z-order 最顶。SetWindowPos(HWND_TOPMOST) 是比 WPF Topmost
        // 更底层的置顶保证，且不抢占焦点（SWP_NOACTIVATE）。
        ForceTopmost();
    }

    /// <summary>用 Win32 SetWindowPos 强制置顶，确保覆盖层在其它窗口之上可见。</summary>
    internal void ForceTopmost()
    {
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;
            const uint flags = NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOACTIVATE;
            NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0, flags);
        }
        catch { /* 置顶失败不应阻断轮盘显示 */ }
    }

    /// <summary>更新当前高亮扇区（-1 表示死区）。cursorScreen 为逻辑像素坐标。</summary>
    public void UpdateHover(int hoverIndex, Point cursorScreen)
    {
        Wheel.HoverIndex = hoverIndex;
        Wheel.InvalidateVisual();
    }

    public new void Hide()
    {
        base.Hide();
        Wheel.Items = null;
        Wheel.HoverIndex = -1;
    }
}
