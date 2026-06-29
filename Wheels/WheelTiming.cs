namespace Spinix.Wheels;

/// <summary>
/// 子轮盘时序判定：纯静态逻辑，不依赖 WPF/Dispatcher，便于单元测试。
///
/// 语义：用户按住触发键，将光标悬停在某个 SubWheel 扇区上。我们不希望"划过即进入"，
/// 也不希望"等太久"。给定悬停时长与配置阈值，判定是否到了进入/回退的时刻。
/// </summary>
public static class WheelTiming
{
    /// <summary>进入延迟的合法下限（ms）。低于此值过于灵敏，极易误触。</summary>
    public const int MinEnterDelayMs = 50;

    /// <summary>进入延迟的合法上限（ms）。高于此值手感迟钝。</summary>
    public const int MaxEnterDelayMs = 2000;

    /// <summary>非法或越界值回退到的默认延迟。</summary>
    public const int DefaultEnterDelayMs = 220;

    /// <summary>
    /// 归一化进入延迟：夹取到 [MinEnterDelayMs, MaxEnterDelayMs]，
    /// 非法值（负数/0/NaN 等）回退到默认值。
    /// </summary>
    public static int NormalizeEnterDelay(int delayMs)
    {
        if (delayMs <= 0) return DefaultEnterDelayMs;
        if (delayMs < MinEnterDelayMs) return MinEnterDelayMs;
        if (delayMs > MaxEnterDelayMs) return MaxEnterDelayMs;
        return delayMs;
    }

    /// <summary>
    /// 判定悬停是否已达到进入子轮盘的阈值。
    /// </summary>
    /// <param name="hoverDurationMs">光标在该 SubWheel 扇区上已停留的毫秒数（&gt;=0）</param>
    /// <param name="configuredDelayMs">配置的阈值（会先归一化）</param>
    /// <returns>是否应当触发进入</returns>
    public static bool ShouldEnterSubWheel(double hoverDurationMs, int configuredDelayMs)
    {
        if (hoverDurationMs < 0) return false;
        int threshold = NormalizeEnterDelay(configuredDelayMs);
        return hoverDurationMs >= threshold;
    }

    /// <summary>
    /// 判定死区停留是否已达到回退父轮盘的阈值。
    /// 复用进入阈值的归一化逻辑，但允许独立的回退延迟值。
    /// </summary>
    public static bool ShouldRetreatFromDeadZone(double deadZoneDurationMs, int configuredRetreatMs)
    {
        if (deadZoneDurationMs < 0) return false;
        int threshold = NormalizeEnterDelay(configuredRetreatMs);
        return deadZoneDurationMs >= threshold;
    }

    /// <summary>
    /// 节流判定：是否需要更新 UI 显示。仅在扇区索引变化时返回 true，
    /// 避免高频鼠标移动（鼠标在同一扇区内移动）堆积 Dispatcher 调用。
    /// 纯函数，可单元测试。
    /// </summary>
    /// <param name="newIndex">当前命中的扇区索引</param>
    /// <param name="lastDisplayedIndex">上次显示的扇区索引（初始为 int.MinValue）</param>
    public static bool ShouldUpdateDisplay(int newIndex, int lastDisplayedIndex)
        => newIndex != lastDisplayedIndex;
}
