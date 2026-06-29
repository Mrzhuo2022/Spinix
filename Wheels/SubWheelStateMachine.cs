using Spinix.Config;

namespace Spinix.Wheels;

/// <summary>
/// 子轮盘进入/回退状态机的决策类型。
/// </summary>
public enum SubWheelDecisionKind
{
    /// <summary>无操作（继续悬停、或未达到阈值、或未命中 SubWheel 扇区）。</summary>
    None,
    /// <summary>应进入子轮盘。<see cref="SubWheelDecision.TargetIndex"/> 指示命中的 SubWheel 扇区。</summary>
    EnterSubWheel,
    /// <summary>应回退到父轮盘（光标在子轮盘死区停留达到阈值）。</summary>
    RetreatToParent,
}

/// <summary>
/// 一次悬停事件的状态机决策结果。
/// </summary>
public readonly struct SubWheelDecision
{
    public SubWheelDecisionKind Kind { get; init; }
    /// <summary>对 EnterSubWheel：命中的 SubWheel 扇区索引。</summary>
    public int TargetIndex { get; init; }

    public static SubWheelDecision NoneResult => new() { Kind = SubWheelDecisionKind.None };
    public static SubWheelDecision Enter(int index) => new() { Kind = SubWheelDecisionKind.EnterSubWheel, TargetIndex = index };
    public static SubWheelDecision Retreat => new() { Kind = SubWheelDecisionKind.RetreatToParent };

    public override bool Equals(object? obj) => obj is SubWheelDecision d && d.Kind == Kind && d.TargetIndex == TargetIndex;
    public override int GetHashCode() => HashCode.Combine(Kind, TargetIndex);
    public static bool operator ==(SubWheelDecision a, SubWheelDecision b) => a.Equals(b);
    public static bool operator !=(SubWheelDecision a, SubWheelDecision b) => !a.Equals(b);
}

/// <summary>
/// 子轮盘进入/回退的纯状态机：把 WheelController 中"悬停计时与死区回退判定"逻辑提取为
/// 可单元测试的形式。时间通过 <see cref="ProcessHover"/> 的 now 参数注入，不依赖系统时钟，
/// 使测试确定可重复。
///
/// 语义（与 WheelController 状态机一致）：
///  - 命中一个 SubWheel 扇区并停留达到进入阈值 → <see cref="SubWheelDecisionKind.EnterSubWheel"/>
///  - 切换到另一个 SubWheel 扇区 → 重置悬停计时（防止"划过累积"）
///  - 离开 SubWheel 扇区 → 清除悬停
///  - 在子轮盘内、命中死区并停留达到回退阈值 → <see cref="SubWheelDecisionKind.RetreatToParent"/>
///  - 在主轮盘死区 → 无操作（主轮盘死区仅用于取消，不回退）
/// </summary>
public sealed class SubWheelStateMachine
{
    private int _hoveredSubWheelIndex = -1;
    private long _hoverStartTicks;
    private long _deadZoneStartTicks;
    private bool _trackingDeadZone;

    /// <summary>
    /// 处理一次悬停（鼠标移动）事件，返回应采取的决策。
    /// </summary>
    /// <param name="hitIndex">当前命中扇区索引；&lt;0 表示在死区。</param>
    /// <param name="isInSubWheel">当前是否处于子轮盘（决定死区是否触发回退）。</param>
    /// <param name="isSubWheelSector">命中的扇区是否为 SubWheel 类型扇区。</param>
    /// <param name="nowTicks">当前时间（Ticks，由调用方注入，便于测试）。</param>
    /// <param name="enterDelayMs">进入阈值（ms，会经 WheelTiming 归一化）。</param>
    /// <param name="retreatDelayMs">回退阈值（ms，会经 WheelTiming 归一化）。</param>
    public SubWheelDecision ProcessHover(
        int hitIndex, bool isInSubWheel, Func<int, bool> isSubWheelSector,
        long nowTicks, int enterDelayMs, int retreatDelayMs)
    {
        bool inDeadZone = hitIndex < 0;

        // ---- 死区处理 ----
        // 仅在子轮盘内，死区停留才触发回退（主轮盘死区只用于松开取消）
        if (inDeadZone && isInSubWheel)
        {
            if (!_trackingDeadZone)
            {
                _trackingDeadZone = true;
                _deadZoneStartTicks = nowTicks;
                return SubWheelDecision.NoneResult; // 本次仅记录起点
            }
            // 检查是否达到回退阈值
            long elapsedMs = (nowTicks - _deadZoneStartTicks) / TimeSpan.TicksPerMillisecond;
            if (WheelTiming.ShouldRetreatFromDeadZone(elapsedMs, retreatDelayMs))
            {
                Reset();
                return SubWheelDecision.Retreat;
            }
            return SubWheelDecision.NoneResult;
        }
        _trackingDeadZone = false;

        // ---- SubWheel 扇区悬停 ----
        bool isSubWheelEntry = hitIndex >= 0 && isSubWheelSector(hitIndex);

        if (isSubWheelEntry)
        {
            if (hitIndex != _hoveredSubWheelIndex)
            {
                // 切换到新的 SubWheel 扇区 → 重置计时
                _hoveredSubWheelIndex = hitIndex;
                _hoverStartTicks = nowTicks;
                return SubWheelDecision.NoneResult; // 本次仅记录起点
            }
            // 同一扇区持续悬停 → 检查是否达到进入阈值
            long elapsedMs = (nowTicks - _hoverStartTicks) / TimeSpan.TicksPerMillisecond;
            if (WheelTiming.ShouldEnterSubWheel(elapsedMs, enterDelayMs))
            {
                Reset();
                return SubWheelDecision.Enter(hitIndex);
            }
            return SubWheelDecision.NoneResult;
        }

        // 命中普通扇区（非 SubWheel、非死区）→ 清除悬停追踪
        _hoveredSubWheelIndex = -1;
        return SubWheelDecision.NoneResult;
    }

    /// <summary>重置状态（进入子轮盘/回退后应调用）。</summary>
    public void Reset()
    {
        _hoveredSubWheelIndex = -1;
        _trackingDeadZone = false;
    }
}
