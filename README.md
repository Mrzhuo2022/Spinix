# ⬡ Spinix

> Windows 快捷轮盘工具 —— 按住鼠标侧键，在光标位置瞬时弹出扇形轮盘，松开即执行。

轮盘交互：按下触发键 → 移动鼠标选择扇区 → 松开执行。
整个流程在一次按键中完成，无需点击，无需移动视线到固定位置。

## ✨ 功能

- **瞬时轮盘**：在鼠标当前位置即时弹出（无淡入淡出），松开即执行并关闭
- **方向选择**：用光标相对轮盘中心的角度选择扇区，中心死区松开 = 取消
- **多种动作**：启动程序、打开网址/文件夹、运行命令脚本、系统动作（音量·媒体·锁屏·截图·任务视图·剪贴板历史）、键盘按键组合
- **二级轮盘**：按住触发键期间悬停子轮盘扇区自动进入，死区停留回退父轮盘
- **可视化配置**：树形编辑界面，支持增删改条目、调整顺序、选择图标、设置子轮盘
- **可配置触发键**：鼠标侧键 X1 / X2 / 中键
- **多屏 DPI 适配**：Per-Monitor V2，在高 DPI 多屏环境下位置与尺寸正确
- **开机自启**：通过注册表 Run 键管理，可在设置中开关
- **系统托盘**：常驻后台，双击打开设置，右键菜单退出
- **多语言支持**：中文（zh-CN）与英文（en），随系统语言自动选择，运行时可切换且实时刷新
- **可访问性（a11y）**：设置窗口全控件支持 AutomationProperties、键盘 Tab 导航、屏幕阅读器

## 🎮 使用方法

1. 启动 `Spinix.exe`，程序常驻系统托盘
2. **按住触发键**唤起轮盘（默认鼠标侧键 X1；可在设置中改为 X2 / 中键，或自定义任意键盘键/组合键如 `Ctrl+Q`）
3. 在弹出的轮盘上**移动鼠标**到目标扇区（扇区实时高亮）
4. **松开触发键**执行该扇区动作
5. 想取消？松开前把光标移回**中心圆**（死区）即可

**二级轮盘**：按住侧键期间，将光标悬停在「子轮盘」扇区上约 0.2 秒自动进入子轮盘；在子轮盘内把光标移回中心死区停留可回退。

## ⚙️ 配置

配置文件位于 `%APPDATA%\Spinix\config.json`，也可通过托盘菜单的「设置」可视化编辑。

### 可配置项

| 项                         | 说明                                                                 | 默认值   |
| -------------------------- | -------------------------------------------------------------------- | -------- |
| `Trigger`                | 触发键：鼠标键 `X1` / `X2` / `Middle`，或键盘组合（如 `Ctrl+Q`、`F8`） | `X1`   |
| `WheelRadius`            | 轮盘半径（逻辑像素）                                                 | `180`  |
| `DeadZoneRadius`         | 中心死区半径（逻辑像素）                                             | `32`   |
| `SubWheelEnterDelayMs`   | 悬停子轮盘扇区多久自动进入（ms，范围 50-2000）                       | `220`  |
| `SubWheelRetreatDelayMs` | 子轮盘死区停留多久回退（ms，范围 50-2000）                           | `220`  |
| `AutoStart`              | 开机自启                                                             | `true` |
| `SuppressTriggerEvents`  | 屏蔽触发键事件（避免传给前台应用）                                   | `true` |
| `DisableInFullScreen`    | 全屏（游戏/应用）时禁止唤起轮盘                                       | `true` |

### 动作类型

| 类型             | 参数说明                                                       |
| ---------------- | -------------------------------------------------------------- |
| `LaunchApp`    | 可执行文件路径（支持环境变量、启动参数、工作目录、管理员运行） |
| `OpenUrl`      | 网址（用默认浏览器打开）                                       |
| `OpenFolder`   | 文件夹路径（用资源管理器打开）                                 |
| `RunScript`    | 命令（通过`cmd /c` 执行，无窗口）                            |
| `SystemAction` | 系统动作枚举名（见下表）                                       |
| `SubWheel`     | 目标子轮盘的 ID                                                |
| `Shortcut`     | 键盘快捷键组合（如 `Ctrl+C`、`Win+Shift+S`，自动模拟按下）     |

### 系统动作

`VolumeUp` · `VolumeDown` · `VolumeMute` · `MediaPlayPause` · `MediaNext` · `MediaPrevious` · `MediaStop` · `LockScreen` · `Screenshot`（Win+Shift+S）· `ShowDesktop`（Win+D）· `TaskView`（Win+Tab）· `ClipboardHistory`（Win+V）

### config.json 示例

```json
{
  "Version": 1,
  "Trigger": "X1",
  "AutoStart": true,
  "SuppressTriggerEvents": true,
  "DisableInFullScreen": true,
  "WheelRadius": 180,
  "DeadZoneRadius": 32,
  "SubWheelEnterDelayMs": 220,
  "SubWheelRetreatDelayMs": 220,
  "Wheels": [
    {
      "Id": "main",
      "Name": "Main Wheel",
      "Items": [
        { "Name": "Terminal", "Icon": "terminal", "ActionType": "LaunchApp", "Argument": "wt.exe" },
        { "Name": "Browser", "Icon": "globe", "ActionType": "OpenUrl", "Argument": "https://github.com" }
      ]
    }
  ]
}
```

## 🏗️ 架构

```
Spinix/
├── Native/              # Win32 P/Invoke 与底层集成
│   ├── NativeMethods.cs        # 集中托管所有 Win32 声明
│   ├── LowLevelMouseHook.cs    # WH_MOUSE_LL 全局钩子（捕获 X1/X2 按下+松开）
│   ├── DpiHelper.cs            # 物理↔逻辑像素换算（多屏 DPI）
│   └── WindowExStyleBuilder.cs # 窗口扩展样式位掩码计算
├── Wheels/              # 轮盘核心
│   ├── WheelController.cs      # 协调器：钩子→轮盘UI→动作执行 闭环
│   ├── WheelWindow.xaml(.cs)   # 透明置顶点击穿透的 WPF 窗口
│   ├── WheelControl.xaml.cs    # 扇区几何绘制（DrawingContext）
│   ├── SectorHitTest.cs        # 方向计算引擎（atan2→扇区索引）
│   ├── SectorGeometryMath.cs   # 扇区顶点坐标纯数学
│   ├── SubWheelStateMachine.cs # 子轮盘进入/回退状态机
│   ├── WheelTiming.cs          # 悬停计时判定
│   └── IconGeometries.cs       # 32 个内置矢量图标
├── Actions/             # 动作执行
│   ├── ActionExecutor.cs       # 动作分发（可注入 IProcessLauncher）
│   └── SystemActionRunner.cs   # 系统动作（音量/媒体/锁屏等）
├── Config/              # 配置系统
│   ├── ConfigModels.cs         # SpinixConfig/Wheel/WheelItem 模型
│   ├── ConfigService.cs        # JSON 读写（路径可注入）
│   ├── ConfigMigrator.cs       # 版本迁移（v0→v1）
│   └── WheelConfigEditor.cs    # 配置编辑纯逻辑（校验/移动/清理）
├── Settings/            # 可视化配置界面
│   └── SettingsWindow.xaml(.cs)
├── Themes/              # WPF 主题资源
├── Assets/              # 应用图标（多尺寸 ICO）
├── App.xaml(.cs)        # 应用入口
├── TrayIcon.cs          # 系统托盘
└── AutoStartService.cs  # 开机自启（注册表，可注入 IRegistryStore）
```

### 核心交互模型（不变量）

```
按下 X1 → 鼠标位置瞬时弹出轮盘（无淡入） → 移动鼠标扇区实时高亮 → 松开 X1 → 执行高亮扇区动作 + 立即关闭
                                        （中心死区内松开 = 取消）
```

**关键不变量**：轮盘必须瞬时出现/消失，绝不能有淡入淡出动画。

## 🔧 开发

### 环境要求

- Windows 10/11
- .NET 9 SDK
- 支持 WPF + Windows Desktop 的工作负载

### 构建

```bash
# 构建（单项目，csproj 在仓库根目录）
dotnet build Spinix.csproj -c Debug
```

### 可测试性设计

核心逻辑均提取为不依赖 WPF UI 的纯函数/纯类，便于扩展单元测试：

- 进程启动抽象为 `IProcessLauncher`（默认真实实现，可注入记录桩）
- 注册表抽象为 `IRegistryStore`（默认真实实现，可注入内存桩）
- 配置路径可注入（`ConfigService.ConfigDir` 可临时覆盖）
- 时间通过参数注入（`SubWheelStateMachine` 用 `nowTicks` 而非系统时钟）
- 几何计算、位运算、解析逻辑均为纯静态函数（`SectorHitTest` / `SectorGeometryMath` / `DpiHelper.Convert*` / `ShortcutParser` / `SpinixConfig.IsKeyboardTrigger`）
- 多语言资源是纯 C# 字典（`StringResources`/`Localization`），运行时切换通过 DynamicResource + 事件机制
- SettingsWindow 的保存逻辑提取为 `TrySave()` 核心方法（不弹 UI），支持 internal 测试构造

### 可测试性设计

核心逻辑均提取为不依赖 WPF UI 的纯函数/纯类，便于单元测试：

- 进程启动抽象为 `IProcessLauncher`（默认真实实现，测试用记录桩）
- 注册表抽象为 `IRegistryStore`（默认真实实现，测试用内存桩）
- 配置路径可注入（`ConfigService.ConfigDir` 可临时覆盖）
- 时间通过参数注入（`SubWheelStateMachine` 用 `nowTicks` 而非系统时钟）
- 几何计算、位运算、解析逻辑均为纯静态函数
- 多语言资源是纯 C# 字典（`StringResources`/`Localization`），运行时切换通过 DynamicResource + 事件机制
- SettingsWindow 的保存逻辑提取为 `TrySave()` 核心方法（不弹 UI），支持 internal 测试构造

## 📦 发布

```bash
# 框架依赖发布（目标机需装 .NET 9 运行时，体积小）
dotnet publish Spinix.csproj -c Release -r win-x64 --self-contained false

# 自包含单文件发布（内置运行时，开箱即用，约 71MB）
dotnet publish Spinix.csproj -c Release
```

自包含发布配置（已在 csproj 中）：

- `PublishSingleFile` —— 合并为单个 exe
- `EnableCompressionInSingleFile` —— 压缩内容
- `IncludeNativeLibrariesForSelfExtract` —— 原生库打入单文件

## 🌐 多语言（i18n）

Spinix 支持中文（zh-CN，默认）与英文（en），启动时随系统 UI 语言自动选择。用户可在设置窗口的语言下拉框中切换语言，**切换后所有 UI 实时更新（无需重启应用或窗口）**。

### 实现

| 组件                            | 文件                                         | 职责                                                        |
| ------------------------------- | -------------------------------------------- | ----------------------------------------------------------- |
| `StringResources`             | `Resources/StringResources.cs`             | 两语言字典 + Get 查询（带回退）                             |
| `Localization`                | `Resources/Localization.cs`                | 语言管理（单例，实现 INotifyPropertyChanged）               |
| `LocalizedStrings`            | `Resources/LocalizedStrings.cs`            | C# 代码访问的静态属性                                       |
| `LocalizationResourceManager` | `Resources/LocalizationResourceManager.cs` | 注册字符串到 Application.Resources，供 DynamicResource 绑定 |

- **C# 代码**：用 `Localization.T("key")` 获取字符串（如 MessageBox、托盘菜单、校验消息）
- **XAML**：用 `{DynamicResource key}` 绑定（如 `Text="{DynamicResource ButtonSave}"`），语言切换时自动刷新
- **回退**：不支持的语言 → 默认 zh-CN；未知 key → 返回 key 本身

### 运行时切换机制

```
用户在设置窗口选语言
  → Language_Changed → Localization.ApplyCulture("en")
  → CultureChanged 事件 → 双路刷新：
      ① LocalizationResourceManager.Refresh() → 重建 Application.Resources
         → DynamicResource 绑定自动刷新 → 设置窗口所有控件立即变英文
      ② TrayIcon 订阅事件 → 托盘菜单文本刷新
```

配置中的 `Language` 字段持久化用户选择，下次启动自动应用。

### 新增语言

1. 在 `StringResources.Cultures` 添加新字典条目（如 `["ja"] = Ja`）
2. 填充该语言的所有 key（`LocalizationTests` 会校验两种语言 key 一致）

### 设计说明

用 C# 字典而非 `.resx` 文件——避免 XML 手写错误和卫星程序集配置复杂度，且完全可单元测试。XAML 用 `DynamicResource`（非 `x:Static`）实现运行时语言切换：语言变更时 `LocalizationResourceManager` 重建资源字典，DynamicResource 绑定自动刷新所有控件，无需重启窗口。

## ♿ 可访问性（a11y）

设置窗口（`SettingsWindow`）已做无障碍优化，支持屏幕阅读器（NVDA/JAWS/讲述人）和键盘操作：

- **AutomationProperties.Name/HelpText**：全控件设置了无障碍名称与帮助文本（屏幕阅读器朗读）
- **TabIndex 逻辑顺序**：树视图(1) → 编辑区(20-30) → 全局设置(40-45) → 操作按钮(50-51)
- **键盘导航**：`KeyboardNavigation.TabNavigation=Cycle`，初始聚焦树视图
- **本地化的 a11y**：无障碍名称与帮助文本也支持中英文

## 🔄 CI/CD

GitHub Actions 配置在 `.github/workflows/`：

- **`ci.yml`**：push/PR 时自动构建（Debug + Release）；打 `v*` tag 时发布自包含单文件 exe 到 GitHub Release
- **`codeql.yml`**：CodeQL 安全扫描（C#，`security-extended, security-and-quality` 规则集），push/PR + 每周定期扫描

### 发版流程

1. 从 `main` 分支打 tag：`git tag v0.1.0 && git push origin v0.1.0`
2. CI 自动 publish 自包含单文件（约 71MB，内置 .NET 运行时），打包 `Spinix-v0.1.0-win-x64.zip`（含 exe + README）
3. 自动创建 GitHub Release（含自动生成的更新日志）；tag 含 `-` 视为预发布

## 📋 技术栈

- **C# / .NET 9 / WPF**（`net9.0-windows`）
- **WinForms**（仅用于 `NotifyIcon` 托盘和文件对话框）

## 📄 许可

MIT
