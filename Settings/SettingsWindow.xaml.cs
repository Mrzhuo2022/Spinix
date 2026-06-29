using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Spinix.Config;
using Spinix.Resources;
using Spinix.Wheels;
using Spinix.Native;
using Localization = Spinix.Resources.Localization;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace Spinix.Settings;

/// <summary>可视化配置窗口。编辑的是当前运行实例的配置副本，保存时落盘并热生效。</summary>
public partial class SettingsWindow : Window
{
    private readonly WheelController _controller;
    private SpinixConfig _config;
    private bool _suppressEvents;

    /// <summary>已打开的设置窗口单例（避免重复打开）。</summary>
    private static SettingsWindow? _instance;

    private object? _selectedTag;
    private Wheel? _selectedWheel;
    private WheelItem? _selectedItem;
    private bool _isRecordingShortcut;
    private bool _isRecordingTrigger;

    private SettingsWindow(WheelController controller)
    {
        InitializeComponent();
        _controller = controller;
        _saveCallback = null; // 生产模式：保存时走 _controller.UpdateConfig

        // 深拷贝一份配置作为编辑副本
        _config = CloneConfig(controller.Config);

        InitIconCombo();
        InitTypeCombo();
        InitTriggerCombo();
        InitSystemActionCombo();
        InitLanguageCombo();

        RefreshTree();
        RefreshGlobalFields();
        ShowEmpty();

        Closing += (s, e) => _instance = null;
        PreviewKeyDown += (s, e) => { if (e.Key == Key.Escape) Close(); };
    }

    /// <summary>
    /// 测试用 internal 构造：接受配置与保存回调，绕过 WheelController。
    /// 用于 UI 自动化集成测试，验证条目增删改的端到端行为。
    /// </summary>
    internal SettingsWindow(SpinixConfig config, Action<SpinixConfig> saveCallback)
    {
        InitializeComponent();
        _controller = null!; // 测试模式不使用 controller
        _saveCallback = saveCallback;
        _config = CloneConfig(config);

        InitIconCombo();
        InitTypeCombo();
        InitTriggerCombo();
        InitSystemActionCombo();
        InitLanguageCombo();

        RefreshTree();
        RefreshGlobalFields();
        ShowEmpty();
    }

    // 保存回调：生产为 null（走 controller），测试注入以捕获保存的配置
    private readonly Action<SpinixConfig>? _saveCallback;

    public static void ShowSingle(WheelController controller)
    {
        if (_instance != null)
        {
            _instance.Activate();
            return;
        }
        _instance = new SettingsWindow(controller);
        _instance.Show();
    }

    private static SpinixConfig CloneConfig(SpinixConfig src)
    {
        // 通过 JSON 序列化做深拷贝，简单可靠
        var json = System.Text.Json.JsonSerializer.Serialize(src);
        return System.Text.Json.JsonSerializer.Deserialize<SpinixConfig>(json)!;
    }

    private void InitIconCombo()
    {
        _suppressEvents = true;
        // 用带图标的项填充：每项展示图标 + 名称
        var iconItems = IconGeometries.AllKeys
            .Select(k => new IconComboItem(k, k))
            .ToList();
        ItemIcon.ItemsSource = iconItems;
        _suppressEvents = false;
    }

    private void InitTypeCombo()
    {
        _suppressEvents = true;
        ItemType.ItemsSource = Enum.GetValues(typeof(WheelActionType));
        _suppressEvents = false;
    }

    private void InitTriggerCombo()
    {
        _suppressEvents = true;
        // 用本地化显示文本，ExtractTriggerKey 按前缀解析；最后一项是「键盘键…」
        TriggerCombo.ItemsSource = new[]
        {
            Localization.T("TriggerX1"),
            Localization.T("TriggerX2"),
            Localization.T("TriggerMiddle"),
            Localization.T("TriggerKeyboard"),
        };
        _suppressEvents = false;
    }

    private void InitLanguageCombo()
    {
        _suppressEvents = true;
        // 语言选项：用本地化名称显示，Tag 存语言代码
        LanguageCombo.Items.Clear();
        foreach (var culture in Localization.SupportedCultures)
        {
            var displayKey = culture switch { "zh-CN" => "LangZhCn", "en" => "LangEn", _ => culture };
            var item = new System.Windows.Controls.ComboBoxItem
            {
                Content = Localization.T(displayKey),
                Tag = culture,
            };
            LanguageCombo.Items.Add(item);
        }
        _suppressEvents = false;
    }

    private void InitSystemActionCombo()
    {
        _suppressEvents = true;
        ItemSystemAction.ItemsSource = Enum.GetValues(typeof(SystemActionKind));
        _suppressEvents = false;
    }

    private void RefreshGlobalFields()
    {
        _suppressEvents = true;
        // 触发键：鼠标键 → 选对应项；键盘组合 → 选「键盘键…」并填录制框
        if (SpinixConfig.IsKeyboardTrigger(_config.Trigger))
        {
            TriggerCombo.SelectedItem = Localization.T("TriggerKeyboard");
            KbdTriggerBox.Text = _config.Trigger;
            KbdTriggerPanel.Visibility = Visibility.Visible;
        }
        else
        {
            TriggerCombo.SelectedItem = _config.Trigger switch
            {
                "X2" => "X2（侧键2）",
                "Middle" => "Middle（中键）",
                _ => "X1（侧键1）",
            };
            KbdTriggerBox.Text = "";
            KbdTriggerPanel.Visibility = Visibility.Collapsed;
        }
        _isRecordingTrigger = false;
        RecordTriggerBtn.Content = Localization.T("ButtonRecordTrigger");
        AutoStartChk.IsChecked = _config.AutoStart;
        SuppressChk.IsChecked = _config.SuppressTriggerEvents;
        DisableFullScreenChk.IsChecked = _config.DisableInFullScreen;
        RadiusBox.Text = _config.WheelRadius.ToString();
        SubWheelEnterDelayBox.Text = _config.SubWheelEnterDelayMs.ToString();
        SubWheelRetreatDelayBox.Text = _config.SubWheelRetreatDelayMs.ToString();
        // 语言下拉：空=跟随系统，否则选中对应项
        var langCode = string.IsNullOrEmpty(_config.Language) ? Localization.Instance.CurrentCulture : _config.Language;
        foreach (System.Windows.Controls.ComboBoxItem item in LanguageCombo.Items)
        {
            if ((item.Tag as string) == langCode)
            {
                LanguageCombo.SelectedItem = item;
                break;
            }
        }
        _suppressEvents = false;
    }

    // ---- 左侧树 ----
    private void RefreshTree()
    {
        WheelTree.Items.Clear();
        foreach (var wheel in _config.Wheels)
        {
            var wheelNode = new TreeViewItem
            {
                Header = BuildWheelHeader(wheel),
                Tag = wheel,
                IsExpanded = true,
            };
            foreach (var item in wheel.Items)
            {
                wheelNode.Items.Add(new TreeViewItem
                {
                    Header = BuildItemHeader(item),
                    Tag = item,
                });
            }
            WheelTree.Items.Add(wheelNode);
        }
    }

    private static string GetIconGlyph(string icon) => "◆";

    private static string BuildWheelHeader(Wheel wheel) => $"🛞 {wheel.Name}  ({wheel.Id})";

    private static string BuildItemHeader(WheelItem item) => $"  {GetIconGlyph(item.Icon)}  {item.Name}";

    /// <summary>
    /// 仅刷新树节点显示文字，不重建 TreeView 结构。
    ///
    /// 重要：编辑条目时若调用 RefreshTree()（清空+重建），会销毁当前选中节点、
    /// 重建后 SelectedItem 变化触发 SelectedItemChanged → 刷新编辑面板 → 输入框焦点丢失。
    /// 改为只原地更新 Header 文字，保持选中状态与焦点不变。
    /// </summary>
    private void RefreshTreeLabelOnly()
    {
        // 更新所有轮盘节点的 Header（名称/ID 可能变化）
        foreach (TreeViewItem wheelNode in WheelTree.Items)
        {
            if (wheelNode.Tag is Wheel w)
                wheelNode.Header = BuildWheelHeader(w);
            foreach (TreeViewItem itemNode in wheelNode.Items)
            {
                if (itemNode.Tag is WheelItem it)
                    itemNode.Header = BuildItemHeader(it);
            }
        }
    }

    private void WheelTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (WheelTree.SelectedItem is TreeViewItem node && node.Tag != null)
        {
            _selectedTag = node.Tag;
            if (_selectedTag is WheelItem item)
            {
                _selectedItem = item;
                _selectedWheel = _config.Wheels.FirstOrDefault(w => w.Items.Contains(item));
                ShowItemEditor(item);
            }
            else if (_selectedTag is Wheel wheel)
            {
                _selectedWheel = wheel;
                _selectedItem = null;
                ShowWheelEditor(wheel);
            }
        }
        else
        {
            ShowEmpty();
        }
    }

    private void WheelTree_MouseDoubleClick(object sender, MouseButtonEventArgs e) { }

    // ---- 编辑面板切换 ----
    private void ShowEmpty()
    {
        EmptyHint.Visibility = Visibility.Visible;
        ItemEditor.Visibility = Visibility.Collapsed;
        WheelEditor.Visibility = Visibility.Collapsed;
    }

    private void ShowItemEditor(WheelItem item)
    {
        _suppressEvents = true;
        _isRecordingShortcut = false;
        EmptyHint.Visibility = Visibility.Collapsed;
        ItemEditor.Visibility = Visibility.Visible;
        WheelEditor.Visibility = Visibility.Collapsed;

        ItemName.Text = item.Name;
        ItemIcon.SelectedItem = ItemIcon.Items.OfType<IconComboItem>().FirstOrDefault(x => x.Key == item.Icon);
        ItemType.SelectedItem = item.ActionType;

        // 参数区初始填充
        ItemArgument.Text = item.Argument;
        ItemArguments.Text = item.Arguments;
        ItemWorkDir.Text = item.WorkingDirectory;
        ItemRunAsAdmin.IsChecked = item.RunAsAdmin;

        // 系统动作下拉当前值
        if (Enum.TryParse<SystemActionKind>(item.Argument, out var sak))
            ItemSystemAction.SelectedItem = sak;
        else
            ItemSystemAction.SelectedIndex = 0;

        // 子轮盘下拉数据源：当前所有轮盘（不含自身）
        RefreshSubWheelCombo(item);

        _suppressEvents = false;
        UpdateArgPanelVisibility(item.ActionType);
    }

    private void RefreshSubWheelCombo(WheelItem currentItem)
    {
        var candidates = _config.Wheels
            .Where(w => w.Id != "main")
            .Select(w => new SubWheelComboItem(w.Id, $"{w.Name}  ({w.Id})"))
            .ToList();
        ItemSubWheelTarget.ItemsSource = candidates;
        ItemSubWheelTarget.SelectedItem = candidates.FirstOrDefault(c => c.Id == currentItem.Argument);
    }

    private void ShowWheelEditor(Wheel wheel)
    {
        _suppressEvents = true;
        EmptyHint.Visibility = Visibility.Collapsed;
        ItemEditor.Visibility = Visibility.Collapsed;
        WheelEditor.Visibility = Visibility.Visible;
        WheelNameBox.Text = wheel.Name;
        WheelIdBox.Text = wheel.Id;
        WheelIdBox.IsEnabled = wheel.Id != "main";
        _suppressEvents = false;
    }

    /// <summary>根据动作类型切换参数输入区的可见性与标签。</summary>
    private void UpdateArgPanelVisibility(WheelActionType t)
    {
        switch (t)
        {
            case WheelActionType.LaunchApp:
                ArgTextPanel.Visibility = Visibility.Visible;
                ArgSystemPanel.Visibility = Visibility.Collapsed;
                ArgSubWheelPanel.Visibility = Visibility.Collapsed;
                ArgLabel.Text = "可执行文件路径";
                BrowseBtn.Visibility = Visibility.Visible;
                ArgHint.Text = "示例：wt.exe、notepad.exe 或 C:\\Path\\app.exe";
                LaunchArgsLabel.Visibility = Visibility.Visible;
                ItemArguments.Visibility = Visibility.Visible;
                WorkDirLabel.Visibility = Visibility.Visible;
                ItemWorkDir.Visibility = Visibility.Visible;
                ItemRunAsAdmin.Visibility = Visibility.Visible;
                break;

            case WheelActionType.OpenUrl:
                ArgTextPanel.Visibility = Visibility.Visible;
                ArgSystemPanel.Visibility = Visibility.Collapsed;
                ArgSubWheelPanel.Visibility = Visibility.Collapsed;
                ArgLabel.Text = "网址 URL";
                BrowseBtn.Visibility = Visibility.Collapsed;
                ArgHint.Text = "示例：https://github.com";
                LaunchArgsLabel.Visibility = Visibility.Collapsed;
                ItemArguments.Visibility = Visibility.Collapsed;
                WorkDirLabel.Visibility = Visibility.Collapsed;
                ItemWorkDir.Visibility = Visibility.Collapsed;
                ItemRunAsAdmin.Visibility = Visibility.Collapsed;
                break;

            case WheelActionType.OpenFolder:
                ArgTextPanel.Visibility = Visibility.Visible;
                ArgSystemPanel.Visibility = Visibility.Collapsed;
                ArgSubWheelPanel.Visibility = Visibility.Collapsed;
                ArgLabel.Text = "文件夹路径";
                BrowseBtn.Visibility = Visibility.Visible;
                BrowseBtn.Content = "选择…";
                ArgHint.Text = "示例：C:\\Users 或 %USERPROFILE%\\Desktop";
                LaunchArgsLabel.Visibility = Visibility.Collapsed;
                ItemArguments.Visibility = Visibility.Collapsed;
                WorkDirLabel.Visibility = Visibility.Collapsed;
                ItemWorkDir.Visibility = Visibility.Collapsed;
                ItemRunAsAdmin.Visibility = Visibility.Collapsed;
                break;

            case WheelActionType.RunScript:
                ArgTextPanel.Visibility = Visibility.Visible;
                ArgSystemPanel.Visibility = Visibility.Collapsed;
                ArgSubWheelPanel.Visibility = Visibility.Collapsed;
                ArgLabel.Text = "命令 / 脚本";
                BrowseBtn.Visibility = Visibility.Collapsed;
                ArgHint.Text = "通过 cmd /c 执行。示例：taskkill /im notepad.exe /f";
                LaunchArgsLabel.Visibility = Visibility.Collapsed;
                ItemArguments.Visibility = Visibility.Collapsed;
                WorkDirLabel.Visibility = Visibility.Visible;
                ItemWorkDir.Visibility = Visibility.Visible;
                ItemRunAsAdmin.Visibility = Visibility.Collapsed;
                break;

            case WheelActionType.SystemAction:
                ArgTextPanel.Visibility = Visibility.Collapsed;
                ArgSystemPanel.Visibility = Visibility.Visible;
                ArgSubWheelPanel.Visibility = Visibility.Collapsed;
                LaunchArgsLabel.Visibility = Visibility.Collapsed;
                ItemArguments.Visibility = Visibility.Collapsed;
                WorkDirLabel.Visibility = Visibility.Collapsed;
                ItemWorkDir.Visibility = Visibility.Collapsed;
                ItemRunAsAdmin.Visibility = Visibility.Collapsed;
                break;

            case WheelActionType.SubWheel:
                ArgTextPanel.Visibility = Visibility.Collapsed;
                ArgSystemPanel.Visibility = Visibility.Collapsed;
                ArgSubWheelPanel.Visibility = Visibility.Visible;
                RefreshSubWheelCombo(_selectedItem!);
                LaunchArgsLabel.Visibility = Visibility.Collapsed;
                ItemArguments.Visibility = Visibility.Collapsed;
                WorkDirLabel.Visibility = Visibility.Collapsed;
                ItemWorkDir.Visibility = Visibility.Collapsed;
                ItemRunAsAdmin.Visibility = Visibility.Collapsed;
                break;

            case WheelActionType.Shortcut:
                ArgTextPanel.Visibility = Visibility.Visible;
                ArgSystemPanel.Visibility = Visibility.Collapsed;
                ArgSubWheelPanel.Visibility = Visibility.Collapsed;
                ArgLabel.Text = Localization.T("LabelShortcut");
                BrowseBtn.Visibility = Visibility.Visible;
                BrowseBtn.Content = Localization.T("ButtonRecord");
                ArgHint.Text = Localization.T("HintShortcut");
                LaunchArgsLabel.Visibility = Visibility.Collapsed;
                ItemArguments.Visibility = Visibility.Collapsed;
                WorkDirLabel.Visibility = Visibility.Collapsed;
                ItemWorkDir.Visibility = Visibility.Collapsed;
                ItemRunAsAdmin.Visibility = Visibility.Collapsed;
                break;
        }
    }

    // ---- 条目字段变更 ----
    private void ItemField_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressEvents || _selectedItem == null) return;
        _selectedItem.Name = ItemName.Text;
        if (ItemIcon.SelectedItem is IconComboItem ici)
            _selectedItem.Icon = ici.Key;
        _selectedItem.Argument = ItemArgument.Text;
        _selectedItem.Arguments = ItemArguments.Text;
        _selectedItem.WorkingDirectory = ItemWorkDir.Text;
        _selectedItem.RunAsAdmin = ItemRunAsAdmin.IsChecked == true;
        RefreshTreeLabelOnly();
    }

    private void ItemType_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressEvents || _selectedItem == null) return;
        if (ItemType.SelectedItem is WheelActionType t)
        {
            _selectedItem.ActionType = t;
            UpdateArgPanelVisibility(t);
            // 切换类型时，对系统动作/子轮盘立即把 Argument 同步到下拉默认项
            if (t == WheelActionType.SystemAction && ItemSystemAction.SelectedItem is SystemActionKind sak)
                _selectedItem.Argument = sak.ToString();
            if (t == WheelActionType.SubWheel && ItemSubWheelTarget.SelectedItem is SubWheelComboItem swc)
                _selectedItem.Argument = swc.Id;
        }
    }

    private void ItemSystemAction_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressEvents || _selectedItem == null) return;
        if (ItemSystemAction.SelectedItem is SystemActionKind sak)
            _selectedItem.Argument = sak.ToString();
    }

    private void ItemSubWheelTarget_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressEvents || _selectedItem == null) return;
        if (ItemSubWheelTarget.SelectedItem is SubWheelComboItem swc)
            _selectedItem.Argument = swc.Id;
    }

    private void WheelField_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressEvents || _selectedWheel == null) return;
        _selectedWheel.Name = WheelNameBox.Text;
        if (_selectedWheel.Id != "main")
            _selectedWheel.Id = string.IsNullOrWhiteSpace(WheelIdBox.Text) ? Guid.NewGuid().ToString("N") : WheelIdBox.Text;
        RefreshTreeLabelOnly();
    }

    // ---- 全局字段 ----
    private void Global_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressEvents) return;
        // Trigger 仅在下拉/录制改变时由 Trigger_Changed 写入。
        // 此处若处于键盘模式，不要用 ExtractTriggerKey 覆写（会把键盘组合重置为 X1）。
        if (!SpinixConfig.IsKeyboardTrigger(_config.Trigger))
            _config.Trigger = ExtractTriggerKey(TriggerCombo.SelectedItem?.ToString());
        _config.AutoStart = AutoStartChk.IsChecked == true;
        _config.SuppressTriggerEvents = SuppressChk.IsChecked == true;
        _config.DisableInFullScreen = DisableFullScreenChk.IsChecked == true;
        if (int.TryParse(RadiusBox.Text, out var r) && r >= 80 && r <= 400)
            _config.WheelRadius = r;
        if (int.TryParse(SubWheelEnterDelayBox.Text, out var enterMs))
            _config.SubWheelEnterDelayMs = enterMs;
        if (int.TryParse(SubWheelRetreatDelayBox.Text, out var retreatMs))
            _config.SubWheelRetreatDelayMs = retreatMs;
    }

    private void Trigger_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressEvents) return;
        var selected = TriggerCombo.SelectedItem?.ToString();

        if (selected == Localization.T("TriggerKeyboard"))
        {
            // 切到键盘模式：显示录制面板；首次切换给个默认组合并立即进入录制
            KbdTriggerPanel.Visibility = Visibility.Visible;
            if (string.IsNullOrWhiteSpace(KbdTriggerBox.Text))
            {
                KbdTriggerBox.Text = "Ctrl+Q";
                _config.Trigger = "Ctrl+Q";
                StartTriggerRecording();
            }
            else
            {
                _config.Trigger = KbdTriggerBox.Text;
            }
        }
        else
        {
            // 切回鼠标键模式
            KbdTriggerPanel.Visibility = Visibility.Collapsed;
            StopTriggerRecording();
            _config.Trigger = ExtractTriggerKey(selected);
        }
    }

    /// <summary>语言下拉变更：切换 UI 语言，DynamicResource 自动刷新设置窗口，托盘菜单也实时刷新。</summary>
    private void Language_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressEvents) return;
        if (LanguageCombo.SelectedItem is not System.Windows.Controls.ComboBoxItem item) return;
        var culture = item.Tag as string;
        if (string.IsNullOrEmpty(culture)) return;

        _config.Language = culture;
        // 应用语言：LocalizationResourceManager 刷新 DynamicResource，托盘菜单通过 CultureChanged 刷新
        Localization.Instance.ApplyCulture(culture);
    }

    /// <summary>
    /// 从设置窗口触发键下拉的显示文本中提取配置用的触发键标识（"X1"/"X2"/"Middle"）。
    /// 纯函数，可单元测试。
    /// </summary>
    public static string ExtractTriggerKey(string? display)
    {
        if (string.IsNullOrEmpty(display)) return "X1";
        if (display.StartsWith("X2")) return "X2";
        if (display.StartsWith("Middle")) return "Middle";
        return "X1";
    }

    // ---- 增删改 ----
    private void AddItem_Click(object sender, RoutedEventArgs e)
    {
        var target = _selectedWheel ?? _config.GetMainWheel() ?? _config.Wheels.FirstOrDefault();
        if (target == null)
        {
            target = new Wheel { Id = "main", Name = "Main Wheel" };
            _config.Wheels.Add(target);
        }
        target.Items.Add(WheelConfigEditor.CreateNewItem());
        RefreshTree();
    }

    private void AddWheel_Click(object sender, RoutedEventArgs e)
    {
        WheelConfigEditor.CreateSubWheel(_config);
        RefreshTree();
    }

    private void DeleteItem_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedItem == null) return;
        WheelConfigEditor.DeleteItemById(_config, _selectedItem.Id);
        _selectedItem = null;
        ShowEmpty();
        RefreshTree();
    }

    private void DeleteWheel_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedWheel == null) return;
        // 删除轮盘 + 关联清理委托给 WheelConfigEditor（可单元测试）
        int cleaned = WheelConfigEditor.DeleteWheelAndCleanReferences(_config, _selectedWheel.Id);
        if (cleaned == -1)
        {
            System.Windows.MessageBox.Show(Localization.T("ErrorCannotDeleteMainWheel"), Localization.T("AppName"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _selectedWheel = null;
        ShowEmpty();
        RefreshTree();
    }

    private void MoveUp_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedItem == null || _selectedWheel == null) return;
        var list = _selectedWheel.Items;
        int i = list.IndexOf(_selectedItem);
        if (WheelConfigEditor.TrySwapItems(list, i, WheelConfigEditor.ComputeMoveUpIndex(list, i)))
            RefreshTree();
    }

    private void MoveDown_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedItem == null || _selectedWheel == null) return;
        var list = _selectedWheel.Items;
        int i = list.IndexOf(_selectedItem);
        if (WheelConfigEditor.TrySwapItems(list, i, WheelConfigEditor.ComputeMoveDownIndex(list, i)))
            RefreshTree();
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedItem == null) return;

        if (_selectedItem.ActionType == WheelActionType.Shortcut)
        {
            _isRecordingShortcut = !_isRecordingShortcut;
            BrowseBtn.Content = _isRecordingShortcut ? "⏹" : Localization.T("ButtonRecord");
            if (_isRecordingShortcut)
            {
                ItemArgument.Focus();
                ItemArgument.SelectAll();
            }
            return;
        }

        if (_selectedItem.ActionType == WheelActionType.OpenFolder)
        {
            using var fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ItemArgument.Text = fbd.SelectedPath;
                if (string.IsNullOrWhiteSpace(ItemName.Text) || ItemName.Text == "新条目")
                    ItemName.Text = Path.GetFileName(fbd.SelectedPath.TrimEnd('\\'));
                ItemField_Changed(sender, e);
            }
            return;
        }

        using var dlg = new OpenFileDialog { Filter = "程序 (*.exe)|*.exe|所有文件 (*.*)|*.*" };
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            ItemArgument.Text = dlg.FileName;
            if (string.IsNullOrWhiteSpace(ItemName.Text) || ItemName.Text == "新条目")
                ItemName.Text = Path.GetFileNameWithoutExtension(dlg.FileName);
            ItemField_Changed(sender, e);
        }
    }

    private void ItemArgument_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_isRecordingShortcut || _selectedItem == null) return;

        // 屏蔽原本的输入处理
        e.Handled = true;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        // 忽略单独按下的修饰键
        if (key == Key.LeftCtrl || key == Key.RightCtrl ||
            key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LWin || key == Key.RWin)
        {
            return;
        }

        var modifiers = Keyboard.Modifiers;
        var sb = new System.Text.StringBuilder();

        if (modifiers.HasFlag(ModifierKeys.Control)) sb.Append("Ctrl+");
        if (modifiers.HasFlag(ModifierKeys.Shift)) sb.Append("Shift+");
        if (modifiers.HasFlag(ModifierKeys.Alt)) sb.Append("Alt+");
        if (modifiers.HasFlag(ModifierKeys.Windows)) sb.Append("Win+");

        sb.Append(key.ToString());

        ItemArgument.Text = sb.ToString();

        // 录制完成
        _isRecordingShortcut = false;
        BrowseBtn.Content = Localization.T("ButtonRecord");
    }

    // ---- 键盘触发键录制 ----

    private void RecordTrigger_Click(object sender, RoutedEventArgs e)
    {
        if (_isRecordingTrigger) StopTriggerRecording();
        else StartTriggerRecording();
    }

    /// <summary>开始录制键盘触发键：聚焦输入框并切换按钮为停止态。</summary>
    private void StartTriggerRecording()
    {
        _isRecordingTrigger = true;
        RecordTriggerBtn.Content = "⏹";
        KbdTriggerBox.Focus();
        KbdTriggerBox.SelectAll();
    }

    /// <summary>停止录制，恢复按钮文字。</summary>
    private void StopTriggerRecording()
    {
        _isRecordingTrigger = false;
        RecordTriggerBtn.Content = Localization.T("ButtonRecordTrigger");
    }

    /// <summary>录制键盘触发键：捕获修饰键+主键组合。Esc 取消录制。</summary>
    private void KbdTriggerBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_isRecordingTrigger) return;
        e.Handled = true;

        // Esc 取消录制，保留原值
        if (e.Key == Key.Escape)
        {
            StopTriggerRecording();
            return;
        }

        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        // 忽略单独按下的修饰键（等用户按下主键时再读取修饰键状态）
        if (key == Key.LeftCtrl || key == Key.RightCtrl ||
            key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LWin || key == Key.RWin)
        {
            return;
        }

        var modifiers = Keyboard.Modifiers;
        var sb = new System.Text.StringBuilder();

        if (modifiers.HasFlag(ModifierKeys.Control)) sb.Append("Ctrl+");
        if (modifiers.HasFlag(ModifierKeys.Shift)) sb.Append("Shift+");
        if (modifiers.HasFlag(ModifierKeys.Alt)) sb.Append("Alt+");
        if (modifiers.HasFlag(ModifierKeys.Windows)) sb.Append("Win+");

        sb.Append(key.ToString());

        KbdTriggerBox.Text = sb.ToString();
        _config.Trigger = sb.ToString();

        StopTriggerRecording();
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        if (System.Windows.MessageBox.Show(Localization.T("ConfirmResetConfig"), Localization.T("AppName"),
            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        _config = SpinixConfig.CreateDefault();
        RefreshTree();
        RefreshGlobalFields();
        ShowEmpty();
    }

    /// <summary>
    /// 保存的核心逻辑（不弹 UI）。返回 (success, errorMessage)。
    /// 通过校验则保存配置并触发保存回调；失败返回错误描述。可单元测试。
    /// </summary>
    internal (bool Success, string? Error) TrySave()
    {
        if (!WheelConfigEditor.HasUniqueWheelIds(_config))
            return (false, Localization.T("ErrorDuplicateWheelId"));

        var orphans = WheelConfigEditor.FindOrphanSubWheelItems(_config);
        if (orphans.Count > 0)
        {
            var detail = string.Join("\n", orphans.Select(o => $"「{o.ItemName}」→ {o.TargetId}"));
            return (false, $"{Localization.T("ErrorOrphanSubWheelPrefix")}\n{detail}{Localization.T("ErrorOrphanSubWheelSuffix")}");
        }

        // 生产模式落盘；测试模式（ConfigDir 指向临时目录）也会落盘但无害
        ConfigService.Save(_config);

        // 热生效：生产走 controller，测试走 saveCallback
        var runtime = CloneConfig(_config);
        if (_saveCallback != null)
            _saveCallback(runtime);
        else
            _controller.UpdateConfig(runtime);

        // 开机自启（仅生产模式，测试不操作真实注册表）
        if (_saveCallback == null)
        {
            try { AutoStartService.Set(runtime.AutoStart); }
            catch { /* 注册表失败不阻断 */ }
        }

        return (true, null);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var (success, error) = TrySave();
        if (!success)
        {
            System.Windows.MessageBox.Show(error, Localization.T("AppName"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        System.Windows.MessageBox.Show(Localization.T("ConfigSaved"), Localization.T("AppName"), MessageBoxButton.OK, MessageBoxImage.Information);
    }
}

/// <summary>图标下拉项：展示图标矢量 + 名称。</summary>
internal sealed class IconComboItem
{
    public string Key { get; }
    public string Display { get; }
    public Geometry Icon => IconGeometries.GetGeometry(Key);
    public IconComboItem(string key, string display) { Key = key; Display = display; }
    public override string ToString() => Key;
}

/// <summary>子轮盘下拉项。</summary>
internal sealed class SubWheelComboItem
{
    public string Id { get; }
    public string Display { get; }
    public SubWheelComboItem(string id, string display) { Id = id; Display = display; }
    public override string ToString() => Display;
}
