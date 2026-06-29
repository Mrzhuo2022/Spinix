using System.Diagnostics;
using Microsoft.Win32;

namespace Spinix;

/// <summary>
/// 注册表键值存储抽象：把 AutoStartService 与真实的 Microsoft.Win32.Registry 解耦，
/// 使"开机自启"的启用/禁用/查询逻辑可单元测试（注入内存桩，不污染真实注册表）。
/// </summary>
public interface IRegistryStore
{
    /// <summary>读取指定路径下的值；不存在返回 null。</summary>
    string? GetValue(string keyPath, string name);
    /// <summary>设置指定路径下的值。</summary>
    void SetValue(string keyPath, string name, string value);
    /// <summary>删除指定路径下的值；不存在时静默返回。</summary>
    void DeleteValue(string keyPath, string name);
}

/// <summary>
/// 默认注册表存储：操作真实的 HKCU。封装 try/catch，注册表不可用（权限/策略）时静默降级。
/// </summary>
public sealed class RegistryStore : IRegistryStore
{
    public string? GetValue(string keyPath, string name)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(keyPath);
            return key?.GetValue(name) as string;
        }
        catch { return null; }
    }

    public void SetValue(string keyPath, string name, string value)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(keyPath);
            key?.SetValue(name, value);
        }
        catch { /* 权限/策略失败静默降级 */ }
    }

    public void DeleteValue(string keyPath, string name)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(keyPath);
            key?.DeleteValue(name, throwOnMissingValue: false);
        }
        catch { /* 静默降级 */ }
    }
}

/// <summary>开机自启：通过当前用户的注册表 Run 键管理（无需管理员）。</summary>
public sealed class AutoStartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Spinix";

    private readonly IRegistryStore _store;
    private readonly string _exePath;

    /// <summary>默认构造：使用真实注册表 + 当前进程 exe 路径。</summary>
    public AutoStartService() : this(new RegistryStore(), GetCurrentExePath()) { }

    /// <summary>测试用构造：注入存储与 exe 路径。</summary>
    public AutoStartService(IRegistryStore store, string exePath)
    {
        _store = store;
        _exePath = exePath;
    }

    /// <summary>查询开机自启是否已启用。</summary>
    public bool IsAutoStartEnabled() => _store.GetValue(RunKeyPath, ValueName) != null;

    /// <summary>启用或禁用开机自启。</summary>
    public void SetAutoStart(bool enabled)
    {
        if (enabled)
            _store.SetValue(RunKeyPath, ValueName, BuildRunCommand(_exePath));
        else
            _store.DeleteValue(RunKeyPath, ValueName);
    }

    /// <summary>
    /// 构造注册表 Run 值：用引号包裹 exe 路径，避免含空格时被解析为多个参数。
    /// 纯函数，可单元测试。
    /// </summary>
    public static string BuildRunCommand(string exePath) => $"\"{exePath}\"";

    /// <summary>校验 exe 路径非空（注册表 Run 值不能为空字符串）。</summary>
    public static bool IsValidExePath(string? exePath) => !string.IsNullOrWhiteSpace(exePath);

    private static string GetCurrentExePath()
    {
        try { return Process.GetCurrentProcess().MainModule!.FileName!; }
        catch { return ""; }
    }

    // ---- 静态便利方法（向后兼容旧的静态调用点）----
    private static readonly Lazy<AutoStartService> DefaultInstance =
        new(() => new AutoStartService());

    /// <summary>查询开机自启是否已启用（使用默认真实注册表实例）。</summary>
    public static bool IsEnabled() => DefaultInstance.Value.IsAutoStartEnabled();

    /// <summary>启用或禁用开机自启（使用默认真实注册表实例）。</summary>
    public static void Set(bool enabled) => DefaultInstance.Value.SetAutoStart(enabled);
}
