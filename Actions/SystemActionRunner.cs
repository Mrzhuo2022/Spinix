using Spinix.Config;
using static Spinix.Native.NativeMethods;

namespace Spinix.Actions;

/// <summary>系统动作执行器：音量、媒体、锁屏、截图、任务视图等。</summary>
public sealed class SystemActionRunner
{
    /// <summary>执行指定 <see cref="SystemActionKind"/>（按枚举名匹配）。</summary>
    public bool Execute(string actionName)
    {
        if (!TryParseAction(actionName, out var kind))
            return false;
        return Execute(kind);
    }

    public bool Execute(SystemActionKind kind)
    {
        switch (kind)
        {
            case SystemActionKind.VolumeUp:        PressKey(VK_VOLUME_UP); return true;
            case SystemActionKind.VolumeDown:      PressKey(VK_VOLUME_DOWN); return true;
            case SystemActionKind.VolumeMute:      PressKey(VK_VOLUME_MUTE); return true;
            case SystemActionKind.MediaPlayPause:  PressKey(VK_MEDIA_PLAY_PAUSE); return true;
            case SystemActionKind.MediaNext:       PressKey(VK_MEDIA_NEXT_TRACK); return true;
            case SystemActionKind.MediaPrevious:   PressKey(VK_MEDIA_PREV_TRACK); return true;
            case SystemActionKind.MediaStop:       PressKey(VK_MEDIA_STOP); return true;
            case SystemActionKind.LockScreen:      return LockWorkStation();
            case SystemActionKind.Screenshot:      SendWinShiftS(); return true;
            case SystemActionKind.ShowDesktop:     SendWinD(); return true;
            case SystemActionKind.TaskView:        SendWinTab(); return true;
            case SystemActionKind.ClipboardHistory:SendWinV(); return true;
            default: return false;
        }
    }

    /// <summary>
    /// 纯函数：将动作名（不区分大小写、忽略前后空白）解析为 <see cref="SystemActionKind"/>。
    /// 可单元测试，不触发任何系统动作。
    /// </summary>
    public static bool TryParseAction(string? actionName, out SystemActionKind kind)
    {
        kind = default;
        if (string.IsNullOrWhiteSpace(actionName)) return false;
        return Enum.TryParse(actionName.Trim(), ignoreCase: true, out kind);
    }

    private static void PressKey(byte vk)
    {
        keybd_event(vk, 0, 0, IntPtr.Zero);
        keybd_event(vk, 0, KEYEVENTF_KEYUP, IntPtr.Zero);
    }

    private static void SendWinKey(byte modifierVk, byte vk)
    {
        keybd_event(modifierVk, 0, 0, IntPtr.Zero);
        keybd_event(vk, 0, 0, IntPtr.Zero);
        keybd_event(vk, 0, KEYEVENTF_KEYUP, IntPtr.Zero);
        keybd_event(modifierVk, 0, KEYEVENTF_KEYUP, IntPtr.Zero);
    }

    private const byte VK_LWIN = 0x5B;
    private const byte VK_SHIFT = 0x10;
    private const byte VK_TAB = 0x09;
    private const byte VK_KEY_D = 0x44;
    private const byte VK_KEY_V = 0x56;
    private const byte VK_KEY_S = 0x53;

    private static void SendWinTab()      => SendWinKey(VK_LWIN, VK_TAB);
    private static void SendWinD()        => SendWinKey(VK_LWIN, VK_KEY_D);
    private static void SendWinV()        => SendWinKey(VK_LWIN, VK_KEY_V);

    private static void SendWinShiftS()
    {
        keybd_event(VK_LWIN, 0, 0, IntPtr.Zero);
        keybd_event(VK_SHIFT, 0, 0, IntPtr.Zero);
        keybd_event(VK_KEY_S, 0, 0, IntPtr.Zero);
        keybd_event(VK_KEY_S, 0, KEYEVENTF_KEYUP, IntPtr.Zero);
        keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, IntPtr.Zero);
        keybd_event(VK_LWIN, 0, KEYEVENTF_KEYUP, IntPtr.Zero);
    }
}
