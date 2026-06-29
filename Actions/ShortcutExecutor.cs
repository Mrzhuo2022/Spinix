using static Spinix.Native.NativeMethods;

namespace Spinix.Actions;

/// <summary>
/// 快捷键执行器：解析字符串（如 "Ctrl+Shift+S"）并模拟按键。
/// </summary>
public static class ShortcutExecutor
{
    /// <summary>
    /// 执行快捷键组合。
    /// </summary>
    /// <param name="shortcut">快捷键字符串，如 "Ctrl+C" 或 "Win+Shift+S"。</param>
    public static void Execute(string shortcut)
    {
        if (string.IsNullOrWhiteSpace(shortcut)) return;

        // 解析委托给共享的 ShortcutParser（键盘触发键匹配也复用同一套解析）
        if (!ShortcutParser.TryParseCombo(shortcut, out byte mainKey, out var modifiers))
            return;

        if (modifiers.Count == 0 && mainKey == 0) return;

        // 按下修饰键
        foreach (var vk in modifiers)
        {
            keybd_event(vk, 0, 0, IntPtr.Zero);
        }

        // 按下并松开主键
        if (mainKey != 0)
        {
            keybd_event(mainKey, 0, 0, IntPtr.Zero);
            keybd_event(mainKey, 0, KEYEVENTF_KEYUP, IntPtr.Zero);
        }

        // 逆序松开修饰键
        for (int i = modifiers.Count - 1; i >= 0; i--)
        {
            keybd_event(modifiers[i], 0, KEYEVENTF_KEYUP, IntPtr.Zero);
        }
    }
}
