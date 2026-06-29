using System.Windows;
using Spinix.Config;
using Spinix.Diagnostics;
using Spinix.Settings;
using Spinix.Wheels;

namespace Spinix;

public partial class App : Application
{
    public static new App Current => (App)Application.Current;

    private WheelController? _controller;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        SpinixLogger.Info("App", "===== Spinix 启动 =====");

        // 加载配置
        var config = ConfigService.Load();
        SpinixLogger.Info("App",
            $"配置加载完成：trigger={config.Trigger}，语言='{config.Language}'，轮盘数={config.Wheels.Count}，半径={config.WheelRadius}");

        // 先初始化本地化资源管理器（订阅 CultureChanged 事件 + 注册初始资源），
        // 再应用配置语言——确保 ApplyCulture 触发的事件能被 ResourceManager 接收
        Spinix.Resources.LocalizationResourceManager.Initialize();

        // 应用配置中指定的 UI 语言（空=跟随系统）
        if (!string.IsNullOrWhiteSpace(config.Language))
            Spinix.Resources.Localization.Instance.ApplyCulture(config.Language);

        // 创建托盘（在应用语言之后，确保托盘菜单用正确语言）
        var tray = new TrayIcon();

        // 启动轮盘控制器：钩子 → 轮盘 UI → 动作执行闭环
        _controller = new WheelController(config, tray);
        _controller.Start();

        // 默认首次启用开机自启（仅当用户配置允许且尚未设置时）
        try
        {
            if (config.AutoStart && !AutoStartService.IsEnabled())
                AutoStartService.Set(true);
        }
        catch { /* 注册表写入失败不应阻断启动 */ }

        // 暴露给托盘「设置」入口
        tray.OpenSettingsRequested += (s, _) => SettingsWindow.ShowSingle(_controller);
        tray.OpenLogRequested += (s, _) => OpenLog();
        tray.SelfTestRequested += (s, _) => ShowSelfTest();
        tray.ExitRequested += (s, _) => ShutdownApp();
    }

    /// <summary>用记事本打开日志文件，便于用户查看/反馈"用不了"的线索。</summary>
    private void OpenLog()
    {
        try
        {
            SpinixLogger.Info("App", "用户请求打开日志文件");
            // 确保日志文件存在（首次打开时尚无日志时）
            if (!System.IO.File.Exists(SpinixLogger.LogPath))
                System.IO.File.WriteAllText(SpinixLogger.LogPath, "");
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{SpinixLogger.LogPath}\"",
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            SpinixLogger.Error("App", "打开日志失败", ex);
        }
    }

    /// <summary>显示自检诊断对话框：钩子状态 + 主轮盘状态 + 排查建议。</summary>
    private void ShowSelfTest()
    {
        try
        {
            var T = Spinix.Resources.Localization.T;
            var sb = new System.Text.StringBuilder();

            bool hookOk = _controller?.IsHookInstalled ?? false;
            sb.AppendLine(hookOk ? T("SelfTestHookOk") : T("SelfTestHookFail"));
            sb.AppendLine();

            int items = _controller?.MainWheelItemCount ?? 0;
            sb.AppendLine(items > 0
                ? string.Format(T("SelfTestMainWheelOk"), items)
                : T("SelfTestMainWheelEmpty"));
            sb.AppendLine();

            sb.AppendLine(string.Format(T("SelfTestLogPath"), SpinixLogger.LogPath));
            sb.AppendLine();
            sb.AppendLine(T("SelfTestHint"));

            SpinixLogger.Info("App", $"自检：hook={hookOk}，主轮盘项数={items}");

            System.Windows.MessageBox.Show(sb.ToString(), T("SelfTestTitle"),
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            SpinixLogger.Error("App", "自检异常", ex);
        }
    }

    public void ShutdownApp()
    {
        _controller?.Dispose();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _controller?.Dispose();
        base.OnExit(e);
    }
}
