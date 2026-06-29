using System.ComponentModel;
using System.Globalization;

namespace Spinix.Resources;

/// <summary>
/// 本地化服务：管理当前 UI 语言，提供字符串查找。
/// 默认跟随系统语言（中文系统=zh-CN，其他=en），可在运行时切换。
///
/// 运行时切换机制：
///  - 实现 <see cref="INotifyPropertyChanged"/>，CurrentCulture 变更时触发事件
///  - <see cref="CultureChanged"/> 事件供 UI 订阅后刷新绑定
///  - <see cref="ApplyCulture(string)"/> 一步完成切换 + 通知
/// </summary>
public sealed class Localization : INotifyPropertyChanged
{
    /// <summary>单例实例（供 XAML 绑定与事件订阅）。</summary>
    public static Localization Instance { get; } = new();

    private string _currentCulture = DetectSystemCulture();

    private Localization() { }

    /// <summary>当前 UI 语言（如 "zh-CN"/"en"）。</summary>
    public string CurrentCulture
    {
        get => _currentCulture;
        set
        {
            if (value != null && StringResources.Cultures.ContainsKey(value) && value != _currentCulture)
            {
                _currentCulture = value;
                OnPropertyChanged(nameof(CurrentCulture));
                CultureChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>当前语言变更时触发（运行时切换语言的通知机制）。</summary>
    public event EventHandler? CultureChanged;

    /// <summary>属性变更通知（用于 INotifyPropertyChanged 数据绑定）。</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>
    /// 一步切换语言并触发通知：设置 CurrentCulture + 引发 CultureChanged 事件。
    /// 若新语言与当前相同或不支持，静默不操作。
    /// </summary>
    public void ApplyCulture(string culture)
    {
        CurrentCulture = culture; // setter 内部处理事件触发
    }

    /// <summary>
    /// 获取当前语言下指定 key 的本地化字符串。
    /// 找不到 key 时回退到默认语言，再找不到返回 key 本身。
    /// </summary>
    public static string T(string key) => StringResources.Get(Instance._currentCulture, key);

    /// <summary>检测系统语言，映射到支持的文化。中文系统→zh-CN，其他→en。</summary>
    public static string DetectSystemCulture()
    {
        var name = CultureInfo.CurrentUICulture.Name;
        if (name.StartsWith("zh", System.StringComparison.OrdinalIgnoreCase))
            return "zh-CN";
        return "en";
    }

    /// <summary>所有支持的语言代码。</summary>
    public static System.Collections.Generic.IEnumerable<string> SupportedCultures
        => StringResources.Cultures.Keys;

    // ---- 静态便利成员（向后兼容）----

    /// <summary>重置为系统语言（用于测试清理）。</summary>
    internal static void ResetToSystemCulture() => Instance.ResetToSystemCultureCore();

    private void ResetToSystemCultureCore()
    {
        _currentCulture = DetectSystemCulture();
        CultureChanged?.Invoke(this, EventArgs.Empty);
        OnPropertyChanged(nameof(CurrentCulture));
    }
}
