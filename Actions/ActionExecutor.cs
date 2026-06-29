using System.Diagnostics;
using Spinix.Config;

namespace Spinix.Actions;

/// <summary>
/// 动作执行器：根据 <see cref="WheelItem"/> 执行对应动作。
///
/// 可测试性设计：
///  - 进程启动通过 <see cref="IProcessLauncher"/> 注入（默认 <see cref="ProcessLauncher"/>），
///    测试可注入桩来断言"参数构造"是否正确，而无需真正启动进程。
///  - 错误处理通过 <see cref="IErrorReporter"/> 注入（默认 <see cref="MessageBoxErrorReporter"/>），
///    测试避免弹窗。
///  - 各动作的 <see cref="ProcessStartInfo"/> 构造提取为 public static 纯函数，
///    直接单元测试参数展开/拼接逻辑。
/// </summary>
public sealed class ActionExecutor
{
    private readonly SystemActionRunner _systemRunner = new();
    private readonly IProcessLauncher _launcher;
    private readonly IErrorReporter _errorReporter;

    public ActionExecutor()
        : this(new ProcessLauncher(), new MessageBoxErrorReporter()) { }

    /// <summary>测试用构造：注入启动器与错误处理器。</summary>
    public ActionExecutor(IProcessLauncher launcher, IErrorReporter errorReporter)
    {
        _launcher = launcher;
        _errorReporter = errorReporter;
    }

    /// <summary>执行一个轮盘项；返回是否成功（仅用于日志/提示）。</summary>
    public bool Execute(WheelItem item)
    {
        try
        {
            switch (item.ActionType)
            {
                case WheelActionType.LaunchApp:
                    return LaunchApp(item);
                case WheelActionType.OpenUrl:
                    return OpenUrl(item.Argument);
                case WheelActionType.OpenFolder:
                    return OpenFolder(item.Argument);
                case WheelActionType.RunScript:
                    return RunScript(item);
                case WheelActionType.SystemAction:
                    return _systemRunner.Execute(item.Argument);
                case WheelActionType.Shortcut:
                    ShortcutExecutor.Execute(item.Argument);
                    return true;
                case WheelActionType.SubWheel:
                    // 二级轮盘由 WheelController 单独处理；这里返回 false 不视为错误
                    return false;
                default:
                    return false;
            }
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(item.Name, ex.Message);
            return false;
        }
    }

    private bool LaunchApp(WheelItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Argument))
            return false;
        var psi = BuildLaunchAppStartInfo(item);
        if (!_launcher.Start(psi))
        {
            _errorReporter.ReportError(item.Name, $"无法启动「{psi.FileName}」");
            return false;
        }
        return true;
    }

    private bool OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!_launcher.Start(BuildOpenUrlStartInfo(url)))
        {
            _errorReporter.ReportError(url, $"无法打开网址「{url}」");
            return false;
        }
        return true;
    }

    private bool OpenFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        if (!_launcher.Start(BuildOpenFolderStartInfo(path)))
        {
            _errorReporter.ReportError(path, $"无法打开文件夹「{path}」");
            return false;
        }
        return true;
    }

    private bool RunScript(WheelItem item)
    {
        var psi = BuildRunScriptStartInfo(item);
        if (psi == null) return false;
        if (!_launcher.Start(psi))
        {
            _errorReporter.ReportError(item.Name, $"无法执行命令「{item.Argument}」");
            return false;
        }
        return true;
    }

    // ---- 参数构造纯函数（可单元测试）----

    /// <summary>构造 LaunchApp 的启动信息：展开环境变量、附加参数/工作目录、RunAsAdmin。</summary>
    public static ProcessStartInfo BuildLaunchAppStartInfo(WheelItem item)
    {
        var psi = new ProcessStartInfo
        {
            FileName = Environment.ExpandEnvironmentVariables(item.Argument ?? ""),
            UseShellExecute = true,
        };

        if (!string.IsNullOrWhiteSpace(item.Arguments))
            psi.Arguments = item.Arguments;
        if (!string.IsNullOrWhiteSpace(item.WorkingDirectory))
            psi.WorkingDirectory = Environment.ExpandEnvironmentVariables(item.WorkingDirectory);

        // RunAsAdmin 只在 UseShellExecute=true 时可用
        psi.Verb = item.RunAsAdmin ? "runas" : "";

        return psi;
    }

    /// <summary>构造 OpenUrl 的启动信息（用默认浏览器）。</summary>
    public static ProcessStartInfo BuildOpenUrlStartInfo(string url)
        => new(url) { UseShellExecute = true };

    /// <summary>构造 OpenFolder 的启动信息（explorer.exe + 路径）。</summary>
    public static ProcessStartInfo BuildOpenFolderStartInfo(string path)
    {
        path = Environment.ExpandEnvironmentVariables(path ?? "");
        // 路径加引号，避免含空格时被 explorer 误解析
        return new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true };
    }

    /// <summary>
    /// 构造 RunScript 的启动信息（cmd /c）。参数为空时返回 null（表示不执行）。
    /// </summary>
    public static ProcessStartInfo? BuildRunScriptStartInfo(WheelItem item)
    {
        var arg = Environment.ExpandEnvironmentVariables(item.Argument ?? "");
        if (string.IsNullOrWhiteSpace(arg)) return null;

        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c {arg}",
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        if (!string.IsNullOrWhiteSpace(item.WorkingDirectory))
            psi.WorkingDirectory = Environment.ExpandEnvironmentVariables(item.WorkingDirectory);

        return psi;
    }
}
