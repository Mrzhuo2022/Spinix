namespace Spinix.Resources;

/// <summary>
/// XAML 本地化字符串访问器：通过 <code>x:Static</code> markup extension 绑定。
///
/// 用法（XAML）：
///   xmlns:r="clr-namespace:Spinix.Resources"
///   Text="{x:Static r:LocalizedStrings.SettingsTitle}"
///
/// 每个属性委托给 <see cref="Localization.T"/>（按当前语言查找）。
/// 注意：x:Static 是一次性绑定（值在控件加载时读取），适合启动时确定语言。
/// 运行时切换语言需重启窗口，或后续升级为 DynamicResource 方案。
/// </summary>
public static class LocalizedStrings
{
    public static string AppName => Localization.T("AppName");
    public static string SettingsTitle => Localization.T("SettingsTitle");
    public static string SettingsHelpText => Localization.T("SettingsHelpText");
    public static string HeaderSubtitle => Localization.T("HeaderSubtitle");
    public static string EmptyHint => Localization.T("EmptyHint");
    public static string SectionItems => Localization.T("SectionItems");

    // 编辑区标签
    public static string LabelItemProperties => Localization.T("LabelItemProperties");
    public static string LabelWheelProperties => Localization.T("LabelWheelProperties");
    public static string LabelName => Localization.T("LabelName");
    public static string LabelIcon => Localization.T("LabelIcon");
    public static string LabelActionType => Localization.T("LabelActionType");
    public static string LabelTarget => Localization.T("LabelTarget");
    public static string LabelSystemAction => Localization.T("LabelSystemAction");
    public static string LabelSubWheelTarget => Localization.T("LabelSubWheelTarget");
    public static string LabelLaunchArgs => Localization.T("LabelLaunchArgs");
    public static string LabelWorkDir => Localization.T("LabelWorkDir");
    public static string LabelWheelId => Localization.T("LabelWheelId");
    public static string HintSubWheelEnter => Localization.T("HintSubWheelEnter");

    // 按钮
    public static string ButtonAddItem => Localization.T("ButtonAddItem");
    public static string ButtonAddWheel => Localization.T("ButtonAddWheel");
    public static string ButtonBrowse => Localization.T("ButtonBrowse");
    public static string ButtonDeleteItem => Localization.T("ButtonDeleteItem");
    public static string ButtonDeleteWheel => Localization.T("ButtonDeleteWheel");
    public static string ButtonSave => Localization.T("ButtonSave");
    public static string ButtonReset => Localization.T("ButtonReset");
    public static string ButtonRunAsAdmin => Localization.T("ButtonRunAsAdmin");

    // 复选框
    public static string CheckBoxAutoStart => Localization.T("CheckBoxAutoStart");
    public static string CheckBoxSuppress => Localization.T("CheckBoxSuppress");

    // 全局设置标签
    public static string LabelTrigger => Localization.T("LabelTrigger");
    public static string LabelWheelRadius => Localization.T("LabelWheelRadius");
    public static string LabelEnterDelay => Localization.T("LabelEnterDelay");
    public static string LabelRetreatDelay => Localization.T("LabelRetreatDelay");
    public static string LabelLanguage => Localization.T("LabelLanguage");
    public static string LangZhCn => Localization.T("LangZhCn");
    public static string LangEn => Localization.T("LangEn");

    // 无障碍帮助文本
    public static string HelpSettingsWindow => Localization.T("HelpSettingsWindow");
    public static string HelpAddItem => Localization.T("HelpAddItem");
    public static string HelpAddWheel => Localization.T("HelpAddWheel");
    public static string HelpMoveUp => Localization.T("HelpMoveUp");
    public static string HelpMoveDown => Localization.T("HelpMoveDown");
    public static string HelpWheelTree => Localization.T("HelpWheelTree");
    public static string HelpItemName => Localization.T("HelpItemName");
    public static string HelpItemIcon => Localization.T("HelpItemIcon");
    public static string HelpItemType => Localization.T("HelpItemType");
    public static string HelpSystemAction => Localization.T("HelpSystemAction");
    public static string HelpSubWheelTarget => Localization.T("HelpSubWheelTarget");
    public static string HelpDeleteItem => Localization.T("HelpDeleteItem");
    public static string HelpDeleteWheel => Localization.T("HelpDeleteWheel");
    public static string HelpRunAsAdmin => Localization.T("HelpRunAsAdmin");
    public static string HelpSuppress => Localization.T("HelpSuppress");
    public static string HelpWheelRadius => Localization.T("HelpWheelRadius");
    public static string HelpEnterDelay => Localization.T("HelpEnterDelay");
    public static string HelpRetreatDelay => Localization.T("HelpRetreatDelay");
    public static string HelpReset => Localization.T("HelpReset");
    public static string HelpSave => Localization.T("HelpSave");
    public static string HelpTrigger => Localization.T("HelpTrigger");
}
