using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Spinix.Wheels;

/// <summary>
/// 内置图标：用字符串 Geometry 表示，避免外部资源依赖。
/// key 取自 WheelItem.Icon，找不到则回退到圆形。
/// </summary>
public static class IconGeometries
{
    public static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["circle"]    = "M12,12 m-9,0 a9,9 0 1,0 18,0 a9,9 0 1,0 -18,0",
        ["terminal"]  = "M3,4 H21 V20 H3 Z M7,9 L10,12 L7,15 M12,15 H17",
        ["globe"]     = "M12,3 a9,9 0 1,0 0,18 a9,9 0 1,0 0,-18 M3,12 H21 M12,3 C8,7 8,17 12,21 M12,3 C16,7 16,17 12,21",
        ["folder"]    = "M3,6 H10 L12,8 H21 V19 H3 Z",
        ["file"]      = "M6,3 H15 L19,7 V21 H6 Z M15,3 V7 H19",
        ["link"]      = "M9,15 a4,4 0 0,0 5.66,0 l3,-3 a4,4 0 0,0 -5.66,-5.66 l-1.7,1.7 M15,9 a4,4 0 0,0 -5.66,0 l-3,3 a4,4 0 0,0 5.66,5.66 l1.7,-1.7",
        ["volume"]    = "M5,9 L9,9 L14,5 L14,19 L9,15 L5,15 Z M17,8 a5,5 0 0,1 0,8 M19.5,5.5 a9,9 0 0,1 0,13",
        ["lock"]      = "M6,10 V7 a6,6 0 0,1 12,0 V10 M5,10 H19 V20 H5 Z",
        ["unlock"]    = "M6,10 V7 a6,6 0 0,1 10,-2 M5,10 H19 V20 H5 Z",
        ["power"]     = "M12,3 V12 M6.5,7 a8,8 0 1,0 11,0",
        ["grid"]      = "M4,4 H10 V10 H4 Z M14,4 H20 V10 H14 Z M4,14 H10 V20 H4 Z M14,14 H20 V20 H14 Z",
        ["list"]      = "M4,6 H20 M4,12 H20 M4,18 H20",
        ["gear"]      = "M12,8 a4,4 0 1,0 0,8 a4,4 0 1,0 0,-8 M12,2 V5 M12,19 V22 M22,12 H19 M5,12 H2 M19,5 L17,7 M7,17 L5,19 M19,19 L17,17 M7,7 L5,5",
        ["star"]      = "M12,3 L14.5,9 L21,9.5 L16,14 L17.5,21 L12,17 L6.5,21 L8,14 L3,9.5 L9.5,9 Z",
        ["heart"]     = "M12,21 C12,21 4,14 4,8.5 A4.5,4.5 0 0,1 12,6 A4.5,4.5 0 0,1 20,8.5 C20,14 12,21 12,21 Z",
        ["search"]    = "M11,4 a7,7 0 1,0 0,14 a7,7 0 1,0 0,-14 M16,16 L21,21",
        ["mail"]      = "M3,5 H21 V19 H3 Z M3,6 L12,13 L21,6",
        ["calendar"]  = "M4,5 H20 V21 H4 Z M4,9 H20 M8,3 V7 M16,3 V7",
        ["clock"]     = "M12,3 a9,9 0 1,0 0,18 a9,9 0 1,0 0,-18 M12,7 V12 L15,14",
        ["camera"]    = "M4,7 H7 L9,5 H15 L17,7 H20 V19 H4 Z M12,10 a3,3 0 1,0 0,6 a3,3 0 1,0 0,-6",
        ["music"]     = "M9,18 V6 L19,4 V16 M9,18 a2,2 0 1,1 -2,-2 a2,2 0 1,1 2,2 M19,16 a2,2 0 1,1 -2,-2 a2,2 0 1,1 2,2",
        ["play"]      = "M6,4 L20,12 L6,20 Z",
        ["pause"]     = "M6,4 H10 V20 H6 Z M14,4 H18 V20 H14 Z",
        ["next"]      = "M5,4 L15,12 L5,20 Z M16,4 H19 V20 H16 Z",
        ["previous"]  = "M19,4 L9,12 L19,20 Z M5,4 H8 V20 H5 Z",
        ["screenshot"]= "M4,4 H10 V6 H6 V10 H4 Z M14,4 H20 V10 H18 V6 H14 Z M4,14 H6 V18 H10 V20 H4 Z M18,14 H20 V20 H14 V18 H18 Z",
        ["desktop"]   = "M3,4 H21 V16 H3 Z M3,16 H21 V18 H3 Z M8,18 V21 M16,18 V21 M8,21 H16",
        ["layers"]    = "M12,3 L22,8 L12,13 L2,8 Z M2,13 L12,18 L22,13 M2,18 L12,23 L22,18",
        ["code"]      = "M8,8 L3,12 L8,16 M16,8 L21,12 L16,16 M14,5 L10,19",
        ["script"]    = "M5,3 H15 L19,7 V21 H5 Z M15,3 V7 H19 M8,12 H16 M8,16 H13",
        ["command"]   = "M9,7 a3,3 0 1,1 -3,-3 a3,3 0 0,1 3,3 Z M15,7 a3,3 0 1,0 3,-3 a3,3 0 0,0 -3,3 Z M9,17 a3,3 0 1,1 -3,3 a3,3 0 0,1 3,-3 Z M15,17 a3,3 0 1,0 3,3 a3,3 0 0,0 -3,-3 Z M9,9 V15 M15,9 V15 M9,12 H15",
        ["wheel"]     = "M12,3 a9,9 0 1,0 0,18 a9,9 0 1,0 0,-18 M12,7 a5,5 0 1,0 0,10 a5,5 0 1,0 0,-10 M12,3 V7 M12,17 V21 M3,12 H7 M17,12 H21",
    };

    public static Geometry GetGeometry(string? iconKey)
    {
        if (!string.IsNullOrWhiteSpace(iconKey) &&
            Map.TryGetValue(iconKey, out var path) &&
            Geometry.Parse(path) is { } g)
        {
            g.Freeze();
            return g;
        }
        return Geometry.Parse(Map["circle"]);
    }

    /// <summary>可被 XAML 绑定的所有图标键（供设置面板选择）。</summary>
    public static IReadOnlyCollection<string> AllKeys => Map.Keys;
}
