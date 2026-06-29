using System.Windows;

namespace Spinix.Wheels;

/// <summary>
/// 扇区几何数学：把 WheelControl 中"扇区环形顶点 / 圆弧端点"的坐标计算提取为纯函数。
/// 不依赖 WPF PathGeometry/ArcSegment，便于单元测试数学正确性。
///
/// 扇区环形（annular sector）由 4 个顶点定义：
///   p1 = 外圆起始角顶点
///   p2 = 外圆结束角顶点
///   p3 = 内圆结束角顶点
///   p4 = 内圆起始角顶点
/// 绘制路径：p1 →(外弧顺时针)→ p2 →(直线)→ p3 →(内弧逆时针)→ p4 →(直线)→ p1 闭合
/// </summary>
public static class SectorGeometryMath
{
    /// <summary>角度（度）转弧度。</summary>
    public static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

    /// <summary>圆上某角度处的点（中心 + 半径向量）。</summary>
    public static Point PointOnCircle(Point center, double radius, double angleRadians)
        => new(center.X + radius * Math.Cos(angleRadians), center.Y + radius * Math.Sin(angleRadians));

    /// <summary>
    /// 计算扇区环形的 4 个顶点。
    /// </summary>
    /// <param name="center">轮盘中心</param>
    /// <param name="innerR">内圆半径（死区）</param>
    /// <param name="outerR">外圆半径</param>
    /// <param name="startDeg">扇区起始角（度，屏幕坐标系）</param>
    /// <param name="sweepDeg">扇区扫过角度（度，正值顺时针）</param>
    public static SectorCorners ComputeCorners(Point center, double innerR, double outerR, double startDeg, double sweepDeg)
    {
        double startRad = ToRadians(startDeg);
        double endRad = ToRadians(startDeg + sweepDeg);

        return new SectorCorners
        {
            P1 = PointOnCircle(center, outerR, startRad), // 外圆起点
            P2 = PointOnCircle(center, outerR, endRad),   // 外圆终点
            P3 = PointOnCircle(center, innerR, endRad),   // 内圆终点
            P4 = PointOnCircle(center, innerR, startRad), // 内圆起点
        };
    }

    /// <summary>
    /// 计算圆弧的两个端点（用于高亮扇区外缘描边）。
    /// </summary>
    public static ArcEndpoints ComputeArcEndpoints(Point center, double radius, double startDeg, double sweepDeg)
    {
        double startRad = ToRadians(startDeg);
        double endRad = ToRadians(startDeg + sweepDeg);
        return new ArcEndpoints
        {
            Start = PointOnCircle(center, radius, startRad),
            End = PointOnCircle(center, radius, endRad),
        };
    }

    /// <summary>
    /// 校验扇区几何参数合法：内外半径为正、外径 &gt; 内径、扫角为正。
    /// 用于绘制前的防御性检查。
    /// </summary>
    public static bool IsValidGeometry(double innerR, double outerR, double sweepDeg)
    {
        return innerR >= 0 && outerR > 0 && outerR > innerR && sweepDeg > 0 && sweepDeg <= 360;
    }
}

/// <summary>扇区环形 4 顶点。</summary>
public readonly struct SectorCorners
{
    /// <summary>外圆起始角顶点。</summary>
    public Point P1 { get; init; }
    /// <summary>外圆结束角顶点。</summary>
    public Point P2 { get; init; }
    /// <summary>内圆结束角顶点。</summary>
    public Point P3 { get; init; }
    /// <summary>内圆起始角顶点。</summary>
    public Point P4 { get; init; }
}

/// <summary>圆弧两端点。</summary>
public readonly struct ArcEndpoints
{
    public Point Start { get; init; }
    public Point End { get; init; }
}
