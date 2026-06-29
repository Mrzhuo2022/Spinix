using System.Windows;

namespace Spinix.Resources;

/// <summary>
/// 本地化资源管理器：把 StringResources 的所有字符串注册为 Application.Resources 中的
/// 动态资源，使 XAML 能用 <c>{DynamicResource key}</c> 绑定并在语言切换时自动刷新。
///
/// 工作原理：
///  1. <see cref="Initialize"/> 在应用启动时把当前语言的所有字符串注册为应用级资源
///  2. XAML 用 {DynamicResource "SettingsTitle"} 等绑定这些资源
///  3. 语言切换时 Localization.CultureChanged 触发 → <see cref="Refresh"/> 被调用
///     → 清除旧资源字典、重新注册新语言字符串 → DynamicResource 自动刷新所有绑定
///
/// 与 x:Static 的区别：x:Static 是一次性读取（控件加载时），DynamicResource 在资源
/// 变更时自动刷新绑定，实现不重启窗口切换语言。
/// </summary>
public static class LocalizationResourceManager
{
    /// <summary>用于标记我们注册的资源字典（便于刷新时清除）。</summary>
    private static ResourceDictionary? _dict;

    /// <summary>
    /// 初始化：注册当前语言的字符串到应用资源，并订阅语言变更事件。
    /// 在 Application 创建后调用（通常 App.OnStartup）。
    /// </summary>
    public static void Initialize()
    {
        Localization.Instance.CultureChanged += OnCultureChanged;
        Refresh();
    }

    /// <summary>语言变更时刷新资源。</summary>
    private static void OnCultureChanged(object? sender, EventArgs e) => Refresh();

    /// <summary>
    /// 重建资源字典：把当前语言的每个字符串 key 注册为应用资源。
    /// DynamicResource 绑定会自动感知变更并刷新。
    /// </summary>
    public static void Refresh()
    {
        var app = Application.Current;
        if (app == null) return;

        // 移除旧字典
        if (_dict != null)
            app.Resources.MergedDictionaries.Remove(_dict);

        // 创建新字典，填充当前语言的所有字符串
        _dict = new ResourceDictionary();
        var culture = Localization.Instance.CurrentCulture;
        if (StringResources.Cultures.TryGetValue(culture, out var strings))
        {
            foreach (var (key, value) in strings)
                _dict[key] = value;
        }
        app.Resources.MergedDictionaries.Add(_dict);
    }

    /// <summary>测试用：检查某 key 是否已注册为资源。</summary>
    public static bool IsRegistered(string key)
        => Application.Current?.Resources.Contains(key) == true;
}
