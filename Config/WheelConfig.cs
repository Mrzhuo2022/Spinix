namespace Spinix.Config;

/// <summary>轮盘中的单个扇区动作。</summary>
public sealed class WheelItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>显示名。</summary>
    public string Name { get; set; } = "New Item";

    /// <summary>图标：内置图标 key、或符号/emoji。</summary>
    public string Icon { get; set; } = "circle";

    /// <summary>动作类型。</summary>
    public WheelActionType ActionType { get; set; } = WheelActionType.LaunchApp;

    /// <summary>
    /// 动作参数（类型相关）：
    /// - LaunchApp: 可执行文件完整路径
    /// - OpenUrl/ OpenFolder: URL 或路径
    /// - RunScript: 命令行或脚本路径
    /// - SystemAction: <see cref="SystemActionKind"/> 枚举名
    /// - SubWheel: 二级轮盘的 wheel id
    /// </summary>
    public string Argument { get; set; } = "";

    /// <summary>仅 LaunchApp：传给可执行文件的参数。</summary>
    public string Arguments { get; set; } = "";

    /// <summary>仅 LaunchApp：工作目录。</summary>
    public string WorkingDirectory { get; set; } = "";

    /// <summary>以管理员身份运行（仅 LaunchApp）。</summary>
    public bool RunAsAdmin { get; set; }

    /// <summary>是否为二级轮盘入口。</summary>
    public bool IsSubWheel => ActionType == WheelActionType.SubWheel;
}

public enum WheelActionType
{
    LaunchApp,
    OpenUrl,
    OpenFolder,
    RunScript,
    SystemAction,
    SubWheel,
    Shortcut,
}

public enum SystemActionKind
{
    VolumeUp,
    VolumeDown,
    VolumeMute,
    MediaPlayPause,
    MediaNext,
    MediaPrevious,
    MediaStop,
    LockScreen,
    Screenshot,    // Win+Shift+S
    ShowDesktop,   // Win+D
    TaskView,      // Win+Tab
    ClipboardHistory, // Win+V
}
