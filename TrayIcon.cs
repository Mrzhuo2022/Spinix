using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Spinix.Native;
using Spinix.Resources;
using Spinix.Wheels;

namespace Spinix;

/// <summary>系统托盘图标与右键菜单。</summary>
public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _notify;
    private readonly ContextMenuStrip _menu;
    private IntPtr _iconHandle = IntPtr.Zero; // GetHicon 创建的 GDI 句柄，Dispose 时需 DestroyIcon 释放

    public event EventHandler? OpenSettingsRequested;
    public event EventHandler? ExitRequested;
    public event EventHandler? OpenLogRequested;
    public event EventHandler? SelfTestRequested;

    public TrayIcon()
    {
        _notify = new NotifyIcon
        {
            Icon = BuildIcon(out _iconHandle),
            Visible = true,
            Text = Localization.T("TrayTooltip"),
        };

        _menu = new ContextMenuStrip();
        _settingsItem = _menu.Items.Add(Localization.T("TraySettings"), null, (s, e) => OpenSettingsRequested?.Invoke(this, EventArgs.Empty));
        _menu.Items.Add(new ToolStripSeparator());
        _openLogItem = _menu.Items.Add(Localization.T("TrayOpenLog"), null, (s, e) => OpenLogRequested?.Invoke(this, EventArgs.Empty));
        _selfTestItem = _menu.Items.Add(Localization.T("TraySelfTest"), null, (s, e) => SelfTestRequested?.Invoke(this, EventArgs.Empty));
        _menu.Items.Add(new ToolStripSeparator());
        _aboutItem = _menu.Items.Add(Localization.T("TrayAbout"), null, (s, e) =>
            MessageBox.Show(Localization.T("AboutContent"), Localization.T("TrayAbout"),
                MessageBoxButtons.OK, MessageBoxIcon.Information));
        _menu.Items.Add(new ToolStripSeparator());
        _exitItem = _menu.Items.Add(Localization.T("TrayExit"), null, (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty));

        _notify.ContextMenuStrip = _menu;

        // 双击托盘 → 打开设置
        _notify.DoubleClick += (s, e) => OpenSettingsRequested?.Invoke(this, EventArgs.Empty);

        // 订阅语言变更事件，运行时刷新菜单文本（无需重启应用）
        Localization.Instance.CultureChanged += OnCultureChanged;
    }

    private readonly ToolStripItem _settingsItem;
    private readonly ToolStripItem _openLogItem;
    private readonly ToolStripItem _selfTestItem;
    private readonly ToolStripItem _aboutItem;
    private readonly ToolStripItem _exitItem;

    /// <summary>语言变更时刷新托盘菜单文本与 Tooltip。</summary>
    private void OnCultureChanged(object? sender, EventArgs e)
    {
        _settingsItem.Text = Localization.T("TraySettings");
        _openLogItem.Text = Localization.T("TrayOpenLog");
        _selfTestItem.Text = Localization.T("TraySelfTest");
        _aboutItem.Text = Localization.T("TrayAbout");
        _exitItem.Text = Localization.T("TrayExit");
        _notify.Text = Localization.T("TrayTooltip");
    }

    /// <summary>运行时用 GDI+ 生成一个简洁的轮盘风格图标，无需外部资源。</summary>
    /// <param name="hicon">输出的 GDI 图标句柄（Icon.FromHandle 不拥有所有权，调用方需 DestroyIcon）。</param>
    private static Icon BuildIcon(out IntPtr hicon)
    {
        hicon = IntPtr.Zero;
        try
        {
            using var bmp = new Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.FromArgb(0, 0, 0, 0));

                // 外圈
                using var ring = new Pen(Color.FromArgb(91, 91, 255), 3);
                g.DrawEllipse(ring, 3, 3, 26, 26);

                // 内圆死区
                using var fill = new SolidBrush(Color.FromArgb(26, 26, 36));
                g.FillEllipse(fill, 11, 11, 10, 10);

                // 六条分隔线（暗示扇区）—— 端点坐标委托给 ComputeSeparatorLines（可单元测试）
                using var sep = new Pen(Color.FromArgb(120, 120, 160), 1);
                foreach (var (x1, y1, x2, y2) in ComputeSeparatorLines(size: 32, sectorCount: 6))
                {
                    g.DrawLine(sep, x1, y1, x2, y2);
                }
            }
            hicon = bmp.GetHicon();
            return Icon.FromHandle(hicon);
        }
        catch
        {
            // 兜底：用 SystemIcons（无需释放，系统拥有所有权）
            hicon = IntPtr.Zero;
            return SystemIcons.Application;
        }
    }

    public void Dispose()
    {
        Localization.Instance.CultureChanged -= OnCultureChanged;
        _notify.Visible = false;
        _notify.Dispose();
        // 释放 BuildIcon 创建的 GDI 图标句柄（Icon.FromHandle 不拥有所有权）
        if (_iconHandle != IntPtr.Zero)
        {
            NativeMethods.DestroyIcon(_iconHandle);
            _iconHandle = IntPtr.Zero;
        }
    }

    /// <summary>
    /// 计算托盘图标上 N 条扇区分隔线的像素端点。纯函数，可单元测试几何正确性。
    /// 每条线从内半径指向外半径，按 360°/count 均匀分布，第一条指向正上方（-90°）。
    /// </summary>
    /// <param name="size">图标边长（像素，正方形）。</param>
    /// <param name="sectorCount">分隔线/扇区数。</param>
    /// <returns>每条线的 (x1,y1,x2,y2) 端点列表。</returns>
    public static IReadOnlyList<(int X1, int Y1, int X2, int Y2)> ComputeSeparatorLines(int size, int sectorCount)
    {
        // 防御：非法参数返回空列表（必须在构造 List 前检查，避免负 capacity 抛异常）
        if (sectorCount <= 0 || size <= 0) return Array.Empty<(int, int, int, int)>();

        var lines = new List<(int, int, int, int)>(sectorCount);
        int center = size / 2;
        // 内/外半径与 BuildIcon 中 32px 下的 11/15 同比例
        double innerR = size * (11.0 / 32.0);
        double outerR = size * (15.0 / 32.0);

        for (int i = 0; i < sectorCount; i++)
        {
            double ang = (-90 + i * (360.0 / sectorCount)) * Math.PI / 180.0;
            int x1 = center + (int)(innerR * Math.Cos(ang));
            int y1 = center + (int)(innerR * Math.Sin(ang));
            int x2 = center + (int)(outerR * Math.Cos(ang));
            int y2 = center + (int)(outerR * Math.Sin(ang));
            lines.Add((x1, y1, x2, y2));
        }
        return lines;
    }
}
