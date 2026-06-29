using static Spinix.Native.NativeMethods;

namespace Spinix.Native;

/// <summary>
/// 窗口扩展样式（WS_EX_*）位掩码的纯逻辑助手：把 WheelWindow 中
/// "在基础样式上叠加点击穿透/置顶/工具窗口等标志"的位运算提取为可单元测试的形式，
/// 不依赖任何 HWND / P/Invoke 调用。
/// </summary>
public static class WindowExStyleBuilder
{
    /// <summary>
    /// 轮盘叠加层所需的标准扩展样式标志集：
    ///  - <see cref="WS_EX_LAYERED"/>：分层窗口（透明背景渲染所必需）
    ///  - <see cref="WS_EX_TRANSPARENT"/>：点击穿透（鼠标事件不送达本窗口，由钩子全局追踪）
    ///  - <see cref="WS_EX_TOOLWINDOW"/>：不在任务栏/Alt+Tab 显示
    ///  - <see cref="WS_EX_NOACTIVATE"/>：不抢占焦点
    ///  - <see cref="WS_EX_TOPMOST"/>：始终置顶
    /// </summary>
    public const int WheelOverlayFlags =
        WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TOPMOST;

    /// <summary>
    /// 在基础扩展样式上叠加指定标志，返回合并后的样式。
    /// 用位 OR 实现：已有标志保持不变，新标志被置位。
    /// </summary>
    public static int Combine(int baseStyle, int flagsToAdd) => baseStyle | flagsToAdd;

    /// <summary>查询样式是否包含（已置位）某标志。</summary>
    public static bool HasFlag(int style, int flag) => (style & flag) == flag;

    /// <summary>从样式中移除指定标志。</summary>
    public static int Remove(int style, int flagsToRemove) => style & ~flagsToRemove;

    /// <summary>
    /// 应用轮盘叠加层的标准标志集到基础样式。
    /// 等价于 Combine(baseStyle, WheelOverlayFlags)。
    /// </summary>
    public static int ApplyWheelOverlayFlags(int baseStyle) => Combine(baseStyle, WheelOverlayFlags);

    /// <summary>判断样式是否具备轮盘点击穿透所需的全部标志（LAYERED + TRANSPARENT）。</summary>
    public static bool HasClickThroughCapability(int style)
        => HasFlag(style, WS_EX_LAYERED) && HasFlag(style, WS_EX_TRANSPARENT);
}
