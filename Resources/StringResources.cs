using System.Collections.Generic;

namespace Spinix.Resources;

/// <summary>
/// 本地化字符串资源：按语言（culture key 如 "zh-CN"/"en"）提供 UI 字符串。
///
/// 设计选择：用 C# 字典而非 .resx 文件——避免 XML 手写错误、csproj 卫星程序集配置复杂度，
/// 且完全可单元测试。新增语言只需添加一个字典条目。
/// </summary>
public static class StringResources
{
    // ---- 中文（默认）----
    // 注意：必须先于 Cultures 定义，否则静态字段初始化顺序导致 Cultures 引用 null
    private static readonly Dictionary<string, string> ZhCn = new()
    {
        // 应用通用
        ["AppName"] = "Spinix",
        ["SettingsTitle"] = "Spinix 设置",
        ["SettingsHelpText"] = "配置轮盘触发键、扇区动作、子轮盘与全局选项",

        // 托盘菜单
        ["TraySettings"] = "设置(&S)...",
        ["TrayAbout"] = "关于 Spinix",
        ["TrayExit"] = "退出(&X)",
        ["TrayTooltip"] = "Spinix",
        ["TrayOpenLog"] = "打开日志(&L)",
        ["TraySelfTest"] = "自检诊断(&D)",
        ["AboutContent"] = "Spinix 1.0\nWindows 快捷轮盘\n\n按下鼠标侧键 X1 唤起轮盘。",

        // 自检结果
        ["SelfTestTitle"] = "Spinix 自检诊断",
        ["SelfTestHookOk"] = "✓ 全局鼠标钩子已正常安装，可捕获 X1/X2/中键",
        ["SelfTestHookFail"] = "✗ 全局鼠标钩子未安装成功——触发键将无响应。常见原因：已有 Spinix 实例在运行，或被安全软件拦截。",
        ["SelfTestMainWheelOk"] = "✓ 主轮盘已配置（{0} 项）",
        ["SelfTestMainWheelEmpty"] = "✗ 主轮盘为空——请打开设置添加条目",
        ["SelfTestLogPath"] = "日志文件：{0}",
        ["SelfTestHint"] = "若轮盘无法弹出：① 确认无其他 Spinix 实例运行；② 确认鼠标有侧键且未被游戏占用；③ 独占全屏游戏可能遮挡覆盖层，请尝试无边框/窗口模式。",

        // 动作执行错误
        ["ActionError"] = "无法执行动作",

        // 设置窗口 - 校验消息
        ["ErrorDuplicateWheelId"] = "存在重复的轮盘 ID，请检查。",
        ["ErrorOrphanSubWheelPrefix"] = "存在指向已删除子轮盘的条目：",
        ["ErrorOrphanSubWheelSuffix"] = "\n\n请先修正后再保存。",
        ["ErrorCannotDeleteMainWheel"] = "不能删除主轮盘。",
        ["ConfirmResetConfig"] = "恢复为默认配置？当前未保存的更改将丢失。",
        ["ConfigSaved"] = "配置已保存并应用。",

        // 设置窗口 - 编辑区
        ["SectionItems"] = "轮盘与条目",
        ["ButtonAddItem"] = "＋ 条目",
        ["ButtonAddWheel"] = "＋ 子轮盘",
        ["NewItemDefaultName"] = "新条目",
        ["NewWheelDefaultName"] = "子轮盘",

        // 设置窗口 - 空提示
        ["EmptyHint"] = "选择左侧条目进行编辑，或点击「＋ 条目」添加新动作。",

        // 设置窗口 - XAML 标签与按钮
        ["HeaderSubtitle"] = "按下 X1 唤起轮盘",
        ["LabelItemProperties"] = "条目属性",
        ["LabelWheelProperties"] = "轮盘属性",
        ["LabelName"] = "名称",
        ["LabelIcon"] = "图标",
        ["LabelActionType"] = "动作类型",
        ["LabelTarget"] = "目标",
        ["LabelSystemAction"] = "系统动作",
        ["LabelSubWheelTarget"] = "目标子轮盘",
        ["LabelLaunchArgs"] = "启动参数（可选）",
        ["LabelWorkDir"] = "工作目录（可选）",
        ["LabelWheelId"] = "轮盘 ID（仅子轮盘可改；主轮盘固定为 main）",
        ["ButtonBrowse"] = "浏览…",
        ["ButtonDeleteItem"] = "删除此条目",
        ["ButtonDeleteWheel"] = "删除此轮盘",
        ["ButtonSave"] = "保存并应用",
        ["ButtonReset"] = "恢复默认",
        ["ButtonRunAsAdmin"] = "以管理员身份运行",
        ["ActionTypeShortcut"] = "键盘快捷键",
        ["LabelShortcut"] = "快捷键组合",
        ["HintShortcut"] = "示例：Ctrl+C, Win+D, Alt+F4",
        ["ButtonRecord"] = "录制",
        ["CheckBoxAutoStart"] = "开机自启",
        ["CheckBoxSuppress"] = "屏蔽触发键事件",
        ["CheckBoxDisableFullScreen"] = "全屏时禁止生效",
        ["LabelTrigger"] = "触发键：",
        ["LabelWheelRadius"] = "轮盘半径",
        ["LabelEnterDelay"] = "子轮盘进入延迟(ms)",
        ["LabelRetreatDelay"] = "子轮盘回退延迟(ms)",
        ["HintSubWheelEnter"] = "提示：按住侧键期间悬停此扇区约 0.2 秒即可进入子轮盘",
        ["LabelLanguage"] = "语言",
        ["LabelGroupTriggerLang"] = "触发与语言",
        ["LabelGroupWheelBehavior"] = "轮盘与行为",
        ["LangZhCn"] = "中文",
        ["LangEn"] = "English",
        ["LanguageChangedRestartHint"] = "语言已切换，部分界面将在重新打开设置后完全生效",
        ["HelpSettingsWindow"] = "配置轮盘触发键、扇区动作、子轮盘与全局选项",
        ["HelpAddItem"] = "向当前轮盘添加一个新的动作条目",
        ["HelpAddWheel"] = "创建一个新的子轮盘并在主轮盘添加入口",
        ["HelpMoveUp"] = "将选中条目向上移动一位",
        ["HelpMoveDown"] = "将选中条目向下移动一位",
        ["HelpWheelTree"] = "选择轮盘或条目进行编辑。使用上下箭头键导航。",
        ["HelpItemName"] = "显示在轮盘扇区上的名称",
        ["HelpItemIcon"] = "选择轮盘扇区显示的图标",
        ["HelpItemType"] = "选择条目执行的动作类型",
        ["HelpSystemAction"] = "选择要执行的系统动作",
        ["HelpSubWheelTarget"] = "选择此条目要进入的子轮盘",
        ["HelpDeleteItem"] = "从所有轮盘中删除选中的条目",
        ["HelpDeleteWheel"] = "删除当前轮盘及其所有条目",
        ["HelpRunAsAdmin"] = "勾选后启动程序时请求管理员权限",
        ["HelpSuppress"] = "勾选后阻止触发键事件传递给前台应用",
        ["HelpDisableFullScreen"] = "勾选后，若当前处于全屏游戏或应用中，则不唤起轮盘",
        ["HelpWheelRadius"] = "轮盘半径，逻辑像素，范围 80-400",
        ["HelpEnterDelay"] = "悬停子轮盘扇区多久自动进入，单位毫秒，范围 50-2000",
        ["HelpRetreatDelay"] = "子轮盘死区停留多久回退父轮盘，单位毫秒，范围 50-2000",
        ["HelpReset"] = "重置为默认配置，未保存的更改将丢失",
        ["HelpSave"] = "保存配置并立即生效",
        ["HelpTrigger"] = "选择唤起轮盘的按键",

        // 触发键显示文本
        ["TriggerX1"] = "X1（侧键1）",
        ["TriggerX2"] = "X2（侧键2）",
        ["TriggerMiddle"] = "Middle（中键）",
        ["TriggerKeyboard"] = "键盘键…",
        ["LabelKbdTrigger"] = "键盘触发键",
        ["ButtonRecordTrigger"] = "录制",
        ["HintRecordTrigger"] = "按下要作为触发键的键或组合键（Esc 取消）",
        ["HintKbdTriggerTip"] = "提示：单键（如 F8）易与其它应用冲突，建议用组合键（如 Ctrl+Q）",
    };

    // ---- 英文 ----
    private static readonly Dictionary<string, string> En = new()
    {
        ["AppName"] = "Spinix",
        ["SettingsTitle"] = "Spinix Settings",
        ["SettingsHelpText"] = "Configure wheel trigger, sector actions, sub-wheels and global options",

        ["TraySettings"] = "&Settings...",
        ["TrayAbout"] = "About Spinix",
        ["TrayExit"] = "E&xit",
        ["TrayTooltip"] = "Spinix",
        ["TrayOpenLog"] = "Open &Log",
        ["TraySelfTest"] = "Self-&Test Diagnostics",
        ["AboutContent"] = "Spinix 1.0\nWindows Shortcut Wheel\n\nHold mouse side button X1 to invoke the wheel.",

        // Self-test results
        ["SelfTestTitle"] = "Spinix Self-Test Diagnostics",
        ["SelfTestHookOk"] = "✓ Global mouse hook installed successfully (can capture X1/X2/Middle)",
        ["SelfTestHookFail"] = "✗ Global mouse hook NOT installed — the trigger key will not respond. Common causes: another Spinix instance is running, or security software is blocking it.",
        ["SelfTestMainWheelOk"] = "✓ Main wheel configured ({0} items)",
        ["SelfTestMainWheelEmpty"] = "✗ Main wheel is empty — open Settings to add items",
        ["SelfTestLogPath"] = "Log file: {0}",
        ["SelfTestHint"] = "If the wheel won't appear: ① Ensure no other Spinix instance is running; ② Ensure your mouse has side buttons not grabbed by a game; ③ Exclusive-fullscreen games may hide the overlay — try borderless/windowed mode.",

        ["ActionError"] = "Failed to execute action",

        ["ErrorDuplicateWheelId"] = "Duplicate wheel IDs found. Please check.",
        ["ErrorOrphanSubWheelPrefix"] = "Items referencing a deleted sub-wheel:",
        ["ErrorOrphanSubWheelSuffix"] = "\n\nPlease fix these before saving.",
        ["ErrorCannotDeleteMainWheel"] = "Cannot delete the main wheel.",
        ["ConfirmResetConfig"] = "Reset to default configuration? Unsaved changes will be lost.",
        ["ConfigSaved"] = "Configuration saved and applied.",

        ["SectionItems"] = "Wheels & Items",
        ["ButtonAddItem"] = "+ Item",
        ["ButtonAddWheel"] = "+ Sub-wheel",
        ["NewItemDefaultName"] = "New Item",
        ["NewWheelDefaultName"] = "Sub-wheel",

        ["EmptyHint"] = "Select an item on the left to edit, or click \"+ Item\" to add a new action.",

        // Settings window - XAML labels & buttons
        ["HeaderSubtitle"] = "Hold X1 to invoke the wheel",
        ["LabelItemProperties"] = "Item Properties",
        ["LabelWheelProperties"] = "Wheel Properties",
        ["LabelName"] = "Name",
        ["LabelIcon"] = "Icon",
        ["LabelActionType"] = "Action Type",
        ["LabelTarget"] = "Target",
        ["LabelSystemAction"] = "System Action",
        ["LabelSubWheelTarget"] = "Target Sub-wheel",
        ["LabelLaunchArgs"] = "Arguments (optional)",
        ["LabelWorkDir"] = "Working Directory (optional)",
        ["LabelWheelId"] = "Wheel ID (sub-wheels only; main is fixed)",
        ["ButtonBrowse"] = "Browse...",
        ["ButtonDeleteItem"] = "Delete Item",
        ["ButtonDeleteWheel"] = "Delete Wheel",
        ["ButtonSave"] = "Save & Apply",
        ["ButtonReset"] = "Reset",
        ["ButtonRunAsAdmin"] = "Run as administrator",
        ["ActionTypeShortcut"] = "Keyboard Shortcut",
        ["LabelShortcut"] = "Shortcut Keys",
        ["HintShortcut"] = "E.g. Ctrl+C, Win+D, Alt+F4",
        ["ButtonRecord"] = "Record",
        ["CheckBoxAutoStart"] = "Auto-start",
        ["CheckBoxSuppress"] = "Suppress trigger key events",
        ["CheckBoxDisableFullScreen"] = "Disable in full screen",
        ["LabelTrigger"] = "Trigger:",
        ["LabelWheelRadius"] = "Wheel Radius",
        ["LabelEnterDelay"] = "Sub-wheel enter delay (ms)",
        ["LabelRetreatDelay"] = "Sub-wheel retreat delay (ms)",
        ["HintSubWheelEnter"] = "Tip: Hold the trigger key and hover this sector ~0.2s to enter the sub-wheel",
        ["LabelLanguage"] = "Language",
        ["LabelGroupTriggerLang"] = "Trigger & Language",
        ["LabelGroupWheelBehavior"] = "Wheel & Behavior",
        ["LangZhCn"] = "中文",
        ["LangEn"] = "English",
        ["LanguageChangedRestartHint"] = "Language changed. Some UI will fully update after reopening settings.",
        ["HelpSettingsWindow"] = "Configure wheel trigger, sector actions, sub-wheels and global options",
        ["HelpAddItem"] = "Add a new action item to the current wheel",
        ["HelpAddWheel"] = "Create a new sub-wheel and add an entry in the main wheel",
        ["HelpMoveUp"] = "Move the selected item up",
        ["HelpMoveDown"] = "Move the selected item down",
        ["HelpWheelTree"] = "Select a wheel or item to edit. Use arrow keys to navigate.",
        ["HelpItemName"] = "Name shown on the wheel sector",
        ["HelpItemIcon"] = "Icon shown on the wheel sector",
        ["HelpItemType"] = "Action type this item performs",
        ["HelpSystemAction"] = "System action to perform",
        ["HelpSubWheelTarget"] = "Sub-wheel to enter when this item is selected",
        ["HelpDeleteItem"] = "Delete the selected item from all wheels",
        ["HelpDeleteWheel"] = "Delete this wheel and all its items",
        ["HelpRunAsAdmin"] = "Request administrator privileges when launching",
        ["HelpSuppress"] = "Block trigger key events from reaching foreground apps",
        ["HelpDisableFullScreen"] = "When checked, the wheel won't appear if a full-screen app/game is active",
        ["HelpWheelRadius"] = "Wheel radius in logical pixels, range 80-400",
        ["HelpEnterDelay"] = "Hover duration to enter sub-wheel, in ms, range 50-2000",
        ["HelpRetreatDelay"] = "Dead zone dwell to retreat from sub-wheel, in ms, range 50-2000",
        ["HelpReset"] = "Reset to default configuration; unsaved changes will be lost",
        ["HelpSave"] = "Save configuration and apply immediately",
        ["HelpTrigger"] = "Key to invoke the wheel",

        ["TriggerX1"] = "X1 (Side 1)",
        ["TriggerX2"] = "X2 (Side 2)",
        ["TriggerMiddle"] = "Middle",
        ["TriggerKeyboard"] = "Keyboard Key...",
        ["LabelKbdTrigger"] = "Keyboard Trigger",
        ["ButtonRecordTrigger"] = "Record",
        ["HintRecordTrigger"] = "Press a key or combo to use as trigger (Esc to cancel)",
        ["HintKbdTriggerTip"] = "Tip: a single key (e.g. F8) may conflict with other apps; prefer a combo (e.g. Ctrl+Q)",
    };

    /// <summary>所有支持的语言及其字符串映射。</summary>
    public static readonly Dictionary<string, Dictionary<string, string>> Cultures = new()
    {
        ["zh-CN"] = ZhCn,
        ["en"] = En,
    };

    /// <summary>默认语言（找不到匹配时回退）。</summary>
    public const string DefaultCulture = "zh-CN";

    /// <summary>
    /// 获取指定语言的字符串；找不到时回退到默认语言，再找不到返回 key 本身。
    /// </summary>
    public static string Get(string culture, string key)
    {
        if (Cultures.TryGetValue(culture, out var lang) && lang.TryGetValue(key, out var v))
            return v;
        if (Cultures.TryGetValue(DefaultCulture, out var def) && def.TryGetValue(key, out var dv))
            return dv;
        return key;
    }

    /// <summary>获取所有已知的字符串 key（用于测试验证完整性）。</summary>
    public static IEnumerable<string> AllKeys => ZhCn.Keys;
}
