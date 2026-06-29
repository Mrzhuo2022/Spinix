using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Spinix.Config;

namespace Spinix.Wheels;

/// <summary>
/// 轮盘绘制控件：在 WPF 中按 <see cref="OnRender"/> 直接绘制扇区。
/// 高亮当前扇区由 <see cref="HoverIndex"/> 驱动，每次更新触发重绘。
///
/// 视觉反馈（不破坏“瞬时出现”不变量——这些只作用于扇区高亮状态）：
///  - 高亮扇区半径略微外扩
///  - 高亮扇区文字/图标放大并提亮
///  - 中心死区在处于死区时显示“取消”并轻微高亮
/// </summary>
public class WheelControl : FrameworkElement
{
    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(nameof(Items), typeof(IList<WheelItem>), typeof(WheelControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty HoverIndexProperty =
        DependencyProperty.Register(nameof(HoverIndex), typeof(int), typeof(WheelControl),
            new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty RadiusProperty =
        DependencyProperty.Register(nameof(Radius), typeof(double), typeof(WheelControl),
            new FrameworkPropertyMetadata(180.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty DeadZoneRadiusProperty =
        DependencyProperty.Register(nameof(DeadZoneRadius), typeof(double), typeof(WheelControl),
            new FrameworkPropertyMetadata(32.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public IList<WheelItem>? Items
    {
        get => (IList<WheelItem>?)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    /// <summary>当前高亮扇区，-1 表示死区/无。</summary>
    public int HoverIndex
    {
        get => (int)GetValue(HoverIndexProperty);
        set => SetValue(HoverIndexProperty, value);
    }

    public double Radius
    {
        get => (double)GetValue(RadiusProperty);
        set => SetValue(RadiusProperty, value);
    }

    public double DeadZoneRadius
    {
        get => (double)GetValue(DeadZoneRadiusProperty);
        set => SetValue(DeadZoneRadiusProperty, value);
    }

    // 静态画刷：在 FindResource 失败时提供兜底
    private static readonly Brush SectorBrush   = GetBrush("SectorBrush",   new SolidColorBrush(Color.FromArgb(0xD9, 0x2A, 0x2A, 0x3C)));
    private static readonly Brush HoverBrush    = GetBrush("SectorHoverBrush", new SolidColorBrush(Color.FromArgb(0xE6, 0x5B, 0x5B, 0xFF)));
    private static readonly Brush BorderBrush   = GetBrush("WheelBorderBrush", new SolidColorBrush(Color.FromRgb(0x3D, 0x3D, 0x52)));
    private static readonly Brush DeadZoneBrush = GetBrush("DeadZoneBrush", new SolidColorBrush(Color.FromArgb(0x99, 0x0F, 0x0F, 0x18)));
    private static readonly Brush TextBrush     = GetBrush("SectorTextBrush", new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xF0)));
    private static readonly Brush IconBrush     = GetBrush("SectorIconBrush", new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xC0)));
    private static readonly Brush GlowBrush     = GetBrush("AccentBrush", new SolidColorBrush(Color.FromRgb(0x5B, 0x5B, 0xFF)));

    // 每帧固定不变的绘制资源：预创建并 Freeze 为单例，避免 OnRender 每帧 GC 分配。
    private static readonly Brush ShadowRingBrush  = Freeze(new RadialGradientBrush(Color.FromArgb(0x50, 0, 0, 0), Colors.Transparent));
    private static readonly Brush BodyBgBrush      = Freeze(new RadialGradientBrush(Color.FromArgb(0x60, 0x1A, 0x1A, 0x2C), Color.FromArgb(0x40, 0, 0, 0)));
    private static readonly Brush InnerGlowBrush   = Freeze(new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF)));
    private static readonly Brush IconShadowBrush  = Freeze(new SolidColorBrush(Color.FromArgb(0x40, 0, 0, 0)));
    private static readonly Brush CenterDotBrush   = Brushes.White;
    private static readonly Pen   BodyPen          = FreezePen(new Pen(BorderBrush, 1.2));
    private static readonly Pen   HoverFillPen     = FreezePen(new Pen(GlowBrush, 1.0));

    /// <summary>把渐变/纯色画刷冻结为不可变单例（可跨线程、避免渲染时修改通知开销）。</summary>
    private static Brush Freeze(Brush b)
    {
        if (b != null && b.CanFreeze) b.Freeze();
        return b!;
    }

    private static Pen FreezePen(Pen p)
    {
        if (p != null && p.CanFreeze) p.Freeze();
        return p!;
    }

    private static Brush GetBrush(string key, Brush fallback)
    {
        try
        {
            var brush = Application.Current.TryFindResource(key) as Brush ?? fallback;
            // Freeze 使 Brush 跨线程安全（静态字段在初始化线程创建，
            // Freeze 后可在任意线程的 OnRender 中使用，避免 InvalidOperationException）
            if (brush != null && brush.CanFreeze)
                brush.Freeze();
            return brush!;
        }
        catch { return fallback; }
    }

    // 字体缓存：OnRender 每帧、每扇区都曾 new FontFamily/Typeface，造成大量 GC。
    // 提取为冻结单例后零分配。FontFamily 本身可安全共享。
    private static readonly FontFamily LabelFontFamily = new("Segoe UI");
    private static readonly FontFamily DeadZoneFontFamily = new("Segoe UI Semibold");
    private static readonly Typeface LabelTypefaceNormal = new(LabelFontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
    private static readonly Typeface LabelTypefaceBold   = new(LabelFontFamily, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
    private static readonly Typeface DeadZoneTypeface    = new(DeadZoneFontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

    private double[] _expansions = Array.Empty<double>();
    private double _deadZoneExp = 0;

    public WheelControl()
    {
        CompositionTarget.Rendering += OnCompositionTargetRendering;
    }

    private void OnCompositionTargetRendering(object? sender, EventArgs e)
    {
        if (!IsVisible || Items == null || Items.Count == 0) return;

        if (_expansions.Length != Items.Count)
        {
            _expansions = new double[Items.Count];
            for (int i = 0; i < _expansions.Length; i++) _expansions[i] = 0;
        }

        bool changed = false;
        
        // 更新扇区平滑扩展
        for (int i = 0; i < _expansions.Length; i++)
        {
            double target = (i == HoverIndex) ? 1.0 : 0.0;
            double diff = target - _expansions[i];
            if (Math.Abs(diff) > 0.001)
            {
                _expansions[i] += diff * 0.22; // 稍微放慢一点点，更有质感
                changed = true;
            }
            else _expansions[i] = target;
        }

        // 更新死区平滑扩展
        bool inDeadZone = HoverIndex < 0;
        double dzTarget = inDeadZone ? 1.0 : 0.0;
        double dzDiff = dzTarget - _deadZoneExp;
        if (Math.Abs(dzDiff) > 0.001)
        {
            _deadZoneExp += dzDiff * 0.22;
            changed = true;
        }
        else _deadZoneExp = dzTarget;

        // 装饰性微弱旋转已移除以保持轻量化
        if (changed) InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var items = Items;
        if (items == null || items.Count == 0) return;

        double size = Math.Min(RenderSize.Width, RenderSize.Height);
        Point center = new(size / 2, size / 2);
        double radius = Radius;
        double innerR = DeadZoneRadius;
        int count = items.Count;
        double sectorDeg = 360.0 / count;

        // DPI 在单帧内不变，只取一次（此前每个扇区/每个 FormattedText 都调用一次）
        double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        // 1. 绘制底层多层阴影/发光
        dc.DrawEllipse(ShadowRingBrush, null, center, radius + 40, radius + 40);

        // 2. 轮盘主体背景（带微弱边框和微妙渐变）
        dc.DrawEllipse(BodyBgBrush, BodyPen, center, radius, radius);

        // 3. 逐扇区绘制
        for (int i = 0; i < count; i++)
        {
            double exp = (i < _expansions.Length) ? _expansions[i] : 0;
            
            // 扇区半径和偏移：悬停时向外偏移并扩大
            double outerR = radius + (10 * exp);
            double currentInnerR = innerR + (2 * exp);
            
            var geometry = CreateSectorGeometry(center, currentInnerR, outerR, SectorHitTest.StartAngleDeg + i * sectorDeg, sectorDeg);
            
            // 绘制基础扇区
            dc.DrawGeometry(SectorBrush, null, geometry);

            // 悬停高亮叠层
            if (exp > 0.01)
            {
                dc.PushOpacity(exp);
                dc.DrawGeometry(HoverBrush, HoverFillPen, geometry);

                // 内部边缘柔光（模拟内发光）
                var innerGlowGeo = CreateSectorGeometry(center, currentInnerR + 3, outerR - 3,
                    SectorHitTest.StartAngleDeg + i * sectorDeg + 1, sectorDeg - 2);
                dc.DrawGeometry(InnerGlowBrush, null, innerGlowGeo);
                dc.Pop();
            }

            // 悬停发光外缘弧线（笔宽随高亮变化，无法预冻结，但仍是少量分配）
            if (exp > 0.05)
            {
                var outerArc = CreateArcLine(center, outerR, SectorHitTest.StartAngleDeg + i * sectorDeg, sectorDeg);
                dc.DrawGeometry(null, new Pen(GlowBrush, 1.5 + 2.0 * exp), outerArc);
            }

            // 计算图标和文本位置
            double labelR = currentInnerR + (outerR - currentInnerR) * 0.54;
            var labelPos = SectorHitTest.SectorCenterPoint(center, i, count, labelR);
            var item = items[i];

            // 绘制图标与文字：垂直居中组合
            double iconSize = 36 + (8 * exp); // 略微增加基准尺寸并减小动态增量，使整体更平衡
            double fontSize = 13 + (1 * exp);
            bool bold = exp > 0.8;
            Brush activeBrush = exp > 0.5 ? CenterDotBrush : IconBrush;
            Brush textBrush = exp > 0.5 ? CenterDotBrush : TextBrush;

            var ft = new FormattedText(
                TruncateText(item.Name, radius - innerR - 15), // 留出更多余量
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                bold ? LabelTypefaceBold : LabelTypefaceNormal,
                fontSize,
                textBrush,
                pixelsPerDip);

            double spacing = 4; // 稍微增加间距
            double totalH = iconSize + spacing + ft.Height;
            double startY = labelPos.Y - totalH / 2;

            // 绘制图标阴影 (高亮时)
            if (exp > 0.6)
            {
                DrawIcon(dc, item.Icon, new Point(labelPos.X + 1, startY + iconSize / 2 + 1), iconSize, IconShadowBrush, true);
            }

            // 绘制图标 (居中)
            DrawIcon(dc, item.Icon, new Point(labelPos.X, startY + iconSize / 2), iconSize, activeBrush, exp > 0.5);

            // 绘制文本 (居中)
            dc.DrawText(ft, new Point(labelPos.X - ft.Width / 2, startY + iconSize + spacing));
        }

        // 4. 中心死区
        double currentDzR = innerR + (2 * _deadZoneExp);
        var dzPen = new Pen(_deadZoneExp > 0.6 ? HoverBrush : BorderBrush, 1.2 + 0.8 * _deadZoneExp);
        dc.DrawEllipse(DeadZoneBrush, dzPen, center, currentDzR, currentDzR);

        // 移除装饰性旋转环，保持轻量化

        // 中心文本/指示器
        if (_deadZoneExp > 0.05)
        {
            dc.PushOpacity(_deadZoneExp);
            var dzText = new FormattedText("取消", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                DeadZoneTypeface, 12 + 2 * _deadZoneExp,
                _deadZoneExp > 0.8 ? CenterDotBrush : HoverBrush, pixelsPerDip);
            dc.DrawText(dzText, center - new Vector(dzText.Width / 2, dzText.Height / 2));
            dc.Pop();
        }
        else
        {
            dc.DrawEllipse(TextBrush, null, center, 1.5, 1.5);
        }
    }

    /// <summary>按扇区宽度估算可容纳的文本长度，过长截断加省略号。纯函数，可单元测试。</summary>
    public static string TruncateText(string text, double availableWidth)
    {
        if (string.IsNullOrEmpty(text)) return "";
        // 粗略估算：每字符约 7px（13pt 下），扇区弧长有限
        int maxChars = Math.Max(4, (int)(availableWidth / 8));
        if (text.Length <= maxChars) return text;
        return text.Substring(0, maxChars - 1) + "…";
    }

    private static Geometry CreateSectorGeometry(Point center, double innerR, double outerR, double startDeg, double sweepDeg)
    {
        // 顶点计算委托给 SectorGeometryMath（可单元测试），这里只负责构造 PathGeometry
        var c = SectorGeometryMath.ComputeCorners(center, innerR, outerR, startDeg, sweepDeg);

        var path = new PathGeometry();
        var figure = new PathFigure { StartPoint = c.P1, IsClosed = true };
        figure.Segments.Add(new ArcSegment(c.P2, new Size(outerR, outerR), 0, false, SweepDirection.Clockwise, true));
        figure.Segments.Add(new LineSegment(c.P3, true));
        figure.Segments.Add(new ArcSegment(c.P4, new Size(innerR, innerR), 0, false, SweepDirection.Counterclockwise, true));
        figure.Segments.Add(new LineSegment(c.P1, true));
        path.Figures.Add(figure);
        return path;
    }

    /// <summary>生成一条圆弧线（仅外侧描边，用于高亮扇区外缘）。</summary>
    private static Geometry CreateArcLine(Point center, double r, double startDeg, double sweepDeg)
    {
        var arc = SectorGeometryMath.ComputeArcEndpoints(center, r, startDeg, sweepDeg);
        var path = new PathGeometry();
        var figure = new PathFigure { StartPoint = arc.Start, IsClosed = false };
        figure.Segments.Add(new ArcSegment(arc.End, new Size(r, r), 0, false, SweepDirection.Clockwise, true));
        path.Figures.Add(figure);
        return path;
    }

    private static void DrawIcon(DrawingContext dc, string? iconKey, Point center, double size, Brush brush, bool isBold)
    {
        var geo = IconGeometries.GetGeometry(iconKey);
        double scale = size / 24.0;

        // 合并三次平移/缩放为单个仿射矩阵（Translate→Scale→Translate），
        // 减少每帧 3 个 Transform + 3 个 PushTransform 分配为单个 MatrixTransform。
        var m = new Matrix();
        m.Translate(-12, -12);     // 先把 24x24 图标原点居中
        m.Scale(scale, scale);
        m.Translate(center.X, center.Y);
        dc.PushTransform(new MatrixTransform(m));

        // 模拟字体图标：填充为主，高亮时可选描边增强
        dc.DrawGeometry(brush, null, geo);
        if (isBold)
        {
            dc.DrawGeometry(null, new Pen(brush, 0.5), geo);
        }

        dc.Pop();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // 需要为高亮扇区外扩和阴影预留空间
        double s = Radius * 2 + 100;
        return new Size(s, s);
    }

    protected override Size ArrangeOverride(Size finalSize) => finalSize;
}
