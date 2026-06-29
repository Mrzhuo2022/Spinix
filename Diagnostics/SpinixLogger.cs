using System.IO;
using System.Text;

namespace Spinix.Diagnostics;

/// <summary>
/// 轻量运行时日志：写入 %APPDATA%\Spinix\spinix.log。
///
/// 设计目标：当用户反馈"用不了"时，提供可观测的故障线索——
///  - 应用启动/配置加载/语言应用
///  - 钩子安装成功或失败（含 Win32 错误码）
///  - 触发键按下/松开、轮盘显示/隐藏、动作执行
///
/// 不变量：日志永不抛出异常。任何 I/O 错误都被静默吞掉——
/// 诊断工具本身绝不能成为新的故障源。
/// </summary>
public static class SpinixLogger
{
    private static readonly object _lock = new();
    private static string _logDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Spinix");
    private static string _logPath = Path.Combine(_logDir, "spinix.log");

    /// <summary>日志文件所在目录（%APPDATA%\Spinix）。</summary>
    public static string LogDir => _logDir;

    /// <summary>日志文件完整路径。</summary>
    public static string LogPath => _logPath;

    /// <summary>单文件大小上限（字节）。超过则滚动重置，避免无限增长。</summary>
    private const long MaxFileSize = 1 * 1024 * 1024; // 1 MB

    /// <summary>
    /// 覆盖日志路径（仅供单元测试注入临时目录）。同时推导目录。
    /// </summary>
    internal static void SetLogPath(string path)
    {
        _logPath = path;
        _logDir = Path.GetDirectoryName(path) ?? "";
    }

    /// <summary>格式化一行日志（纯函数，可单元测试）。</summary>
    public static string FormatLine(string timestamp, string category, string message)
    {
        var sb = new StringBuilder();
        sb.Append(timestamp).Append(" [").Append(category).Append("] ").Append(message);
        return sb.ToString();
    }

    /// <summary>写入一条 INFO 级别日志。</summary>
    public static void Info(string category, string message) => Write("INFO", category, message);

    /// <summary>写入一条 WARN 级别日志。</summary>
    public static void Warn(string category, string message) => Write("WARN", category, message);

    /// <summary>写入一条 ERROR 级别日志（带异常详情）。</summary>
    public static void Error(string category, string message, Exception? ex = null)
    {
        var detail = ex == null ? message : $"{message} :: {ex.GetType().Name}: {ex.Message}";
        Write("ERROR", category, detail);
    }

    private static void Write(string level, string category, string message)
    {
        try
        {
            var line = FormatLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), $"{level}|{category}", message)
                       + Environment.NewLine;
            lock (_lock)
            {
                Directory.CreateDirectory(_logDir);
                // 简单滚动：超过上限则截断为空，从头开始记录最新事件
                if (File.Exists(_logPath) && new FileInfo(_logPath).Length > MaxFileSize)
                    File.WriteAllText(_logPath, "");
                File.AppendAllText(_logPath, line, Encoding.UTF8);
            }
        }
        catch
        {
            // 诊断工具永不抛出
        }
    }
}
