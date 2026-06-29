using System.Windows;

namespace Spinix.Wheels;

/// <summary>
/// 方向计算引擎：将光标相对轮盘中心的位移映射到具体扇区。
/// 核心思想：用 atan2(dy, dx) 求极角 → 按扇区均分 → 加中心死区判定。
/// </summary>
public static class SectorHitTest
{
    /// <summary>轮盘“上方”（屏幕 -Y 方向）对应的角度起始位置。</summary>
    public const double StartAngleDeg = -90.0;

    /// <summary>
    /// 计算鼠标位置命中的扇区索引。
    /// </summary>
    /// <param name="center">轮盘中心（屏幕坐标）</param>
    /// <param name="mouse">当前光标位置</param>
    /// <param name="sectorCount">扇区数</param>
    /// <param name="deadZoneRadius">中心死区半径（命中死区返回 -1）</param>
    /// <returns>扇区索引 [0, sectorCount-1]；死区或越界返回 -1</returns>
    public static int HitTest(Point center, Point mouse, int sectorCount, double deadZoneRadius)
    {
        // 防御：NaN/Infinity 输入（DPI 转换异常或极端坐标）直接返回死区，避免后续计算产生垃圾值
        if (double.IsNaN(mouse.X) || double.IsNaN(mouse.Y) ||
            double.IsInfinity(mouse.X) || double.IsInfinity(mouse.Y) ||
            double.IsNaN(deadZoneRadius) || double.IsInfinity(deadZoneRadius))
            return -1;

        double dx = mouse.X - center.X;
        double dy = mouse.Y - center.Y;
        double dist = Math.Sqrt(dx * dx + dy * dy);

        if (dist < deadZoneRadius) return -1;
        if (sectorCount <= 0) return -1;

        // 屏幕坐标 y 向下为正；数学上取 atan2(dy, dx)，
        // 屏幕坐标下角度随光标顺时针递增，与轮盘扇区顺时针展开一致。
        double angleDeg = Math.Atan2(dy, dx) * 180.0 / Math.PI;
        // 把角度统一归一到 [StartAngleDeg, StartAngleDeg + 360)
        double relative = angleDeg - StartAngleDeg;
        relative = ((relative % 360.0) + 360.0) % 360.0;

        double sectorSize = 360.0 / sectorCount;
        int index = (int)(relative / sectorSize);
        if (index >= sectorCount) index = sectorCount - 1;
        return index;
    }

    /// <summary>给定扇区索引和总数，返回该扇区中心角度（度，屏幕坐标系，-90 为正上）。</summary>
    public static double SectorCenterAngle(int index, int sectorCount)
    {
        double sectorSize = 360.0 / sectorCount;
        return StartAngleDeg + index * sectorSize + sectorSize / 2.0;
    }

    /// <summary>扇区中心点（屏幕/绘图坐标）。</summary>
    public static Point SectorCenterPoint(Point wheelCenter, int index, int sectorCount, double radius)
    {
        double angle = SectorCenterAngle(index, sectorCount) * Math.PI / 180.0;
        return new Point(
            wheelCenter.X + radius * Math.Cos(angle),
            wheelCenter.Y + radius * Math.Sin(angle));
    }
}
