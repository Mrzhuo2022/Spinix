using System.Diagnostics;

namespace Spinix.Actions;

/// <summary>
/// 进程启动抽象：将 ActionExecutor 与真实的 <see cref="Process.Start(ProcessStartInfo)"/>
/// 解耦，使动作的"参数构造"逻辑可单元测试（无需真正启动进程）。
/// </summary>
public interface IProcessLauncher
{
    /// <summary>按指定启动信息启动进程。返回是否成功启动。</summary>
    bool Start(ProcessStartInfo startInfo);
}

/// <summary>
/// 默认进程启动器：包装 <see cref="Process.Start(ProcessStartInfo)"/>。
/// 启动失败（如文件未找到）返回 false 而非抛出，供上层统一处理。
/// </summary>
public sealed class ProcessLauncher : IProcessLauncher
{
    public bool Start(ProcessStartInfo startInfo)
    {
        try
        {
            var p = Process.Start(startInfo);
            return p != null;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// 错误处理器抽象：ActionExecutor 捕获异常时调用，默认实现弹 MessageBox。
/// 测试可注入无副作用的实现以避免 UI 弹窗。
/// </summary>
public interface IErrorReporter
{
    void ReportError(string itemName, string message);
}

/// <summary>默认错误处理器：弹出 WPF MessageBox。</summary>
public sealed class MessageBoxErrorReporter : IErrorReporter
{
    public void ReportError(string itemName, string message)
    {
        var t = Spinix.Resources.Localization.T;
        System.Windows.MessageBox.Show(
            $"{t("ActionError")}「{itemName}」：\n{message}", t("AppName"),
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
    }
}
