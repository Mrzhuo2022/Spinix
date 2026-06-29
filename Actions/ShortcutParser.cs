using System.Windows.Input;

namespace Spinix.Actions;

/// <summary>
/// 快捷键字符串解析器：把 "Ctrl+Shift+S" 这样的组合字符串解析为虚拟键码集合。
///
/// 共享给两个消费方：
///  - <see cref="ShortcutExecutor"/>：解析后用 keybd_event 模拟按键
///  - <see cref="Spinix.Native.LowLevelKeyboardHook"/>：解析后与 KBDLLHOOKSTRUCT.vkCode 匹配
///
/// 字符串格式："[Modifier+]*MainKey"，修饰键名 Ctrl/Shift/Alt/Win（不区分大小写），
/// 主键为 A-Z / 0-9 单字符或 WPF Key 枚举名（如 Space、F8、D1）。解析不区分前后顺序。
/// 纯函数，无副作用，可单元测试。
/// </summary>
public static class ShortcutParser
{
    /// <summary>解析修饰键名（Ctrl/Shift/Alt/Win 等）为虚拟键码。</summary>
    public static bool TryParseModifier(string part, out byte vk)
    {
        vk = 0;
        if (string.IsNullOrEmpty(part)) return false;
        switch (part.ToLowerInvariant())
        {
            case "ctrl":
            case "control":
                vk = 0x11; // VK_CONTROL
                return true;
            case "shift":
                vk = 0x10; // VK_SHIFT
                return true;
            case "alt":
            case "menu":
                vk = 0x12; // VK_MENU
                return true;
            case "win":
            case "windows":
            case "meta":
                vk = 0x5B; // VK_LWIN
                return true;
            default:
                return false;
        }
    }

    /// <summary>解析主键（A-Z/0-9 单字符或 WPF Key 枚举名）为虚拟键码。</summary>
    public static bool TryParseKey(string part, out byte vk)
    {
        vk = 0;
        if (string.IsNullOrEmpty(part)) return false;
        // 优先尝试直接转换 A-Z, 0-9
        if (part.Length == 1)
        {
            char c = char.ToUpperInvariant(part[0]);
            if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
            {
                vk = (byte)c;
                return true;
            }
        }

        // 尝试解析为 WPF Key 枚举
        if (Enum.TryParse(part, true, out Key key))
        {
            vk = (byte)KeyInterop.VirtualKeyFromKey(key);
            return vk != 0;
        }

        return false;
    }

    /// <summary>
    /// 一次性解析整个组合字符串。
    /// </summary>
    /// <param name="combo">组合字符串，如 "Ctrl+Shift+S"。空/非法返回 false。</param>
    /// <param name="mainVk">主键虚拟键码（0 表示无主键）。</param>
    /// <param name="modifierVks">修饰键虚拟键码集合（可能为空）。</param>
    /// <returns>是否解析出至少一个键（主键或修饰键）。</returns>
    public static bool TryParseCombo(string? combo, out byte mainVk, out IReadOnlyList<byte> modifierVks)
    {
        mainVk = 0;
        var mods = new List<byte>();

        if (string.IsNullOrWhiteSpace(combo))
        {
            modifierVks = mods;
            return false;
        }

        var parts = combo.Split('+', StringSplitOptions.RemoveEmptyEntries)
                         .Select(p => p.Trim())
                         .ToList();

        foreach (var part in parts)
        {
            if (TryParseModifier(part, out var vk))
                mods.Add(vk);
            else if (TryParseKey(part, out var mvk))
                mainVk = mvk;
        }

        modifierVks = mods;
        return mainVk != 0 || mods.Count > 0;
    }
}
