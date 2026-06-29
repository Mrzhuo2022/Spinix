using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spinix.Config;

/// <summary>配置服务：负责 config.json 的加载、保存、迁移。</summary>
public static class ConfigService
{
    private static readonly string DefaultConfigDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Spinix");

    /// <summary>默认配置目录（%APPDATA%\Spinix）。测试可临时覆盖。</summary>
    public static string ConfigDir
    {
        get => _configDir ?? DefaultConfigDir;
        set => _configDir = value;
    }

    private static string? _configDir;

    /// <summary>配置文件完整路径（基于 <see cref="ConfigDir"/>）。</summary>
    public static string ConfigPath => Path.Combine(ConfigDir, "config.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>加载配置；不存在或损坏时回退到默认。从 <see cref="ConfigPath"/> 读取。</summary>
    public static SpinixConfig Load() => LoadFrom(ConfigPath);

    /// <summary>
    /// 从指定文件路径加载配置；不存在或损坏时回退到默认。
    /// 此重载用于单元测试（可指向临时目录）。
    /// </summary>
    public static SpinixConfig LoadFrom(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                var def = SpinixConfig.CreateDefault();
                SaveTo(def, path);
                return def;
            }
            var json = File.ReadAllText(path);
            var cfg = JsonSerializer.Deserialize<SpinixConfig>(json, JsonOpts);
            // 空轮盘或反序列化为 null → 回退默认
            if (cfg == null || cfg.Wheels.Count == 0)
            {
                return SpinixConfig.CreateDefault();
            }
            // 迁移到当前版本（补全旧版缺失字段、修正非法值）
            return ConfigMigrator.Migrate(cfg);
        }
        catch
        {
            // 任何异常（IO 错误、JSON 损坏、权限）→ 回退默认，绝不抛出
            return SpinixConfig.CreateDefault();
        }
    }

    /// <summary>保存配置到 <see cref="ConfigPath"/>。</summary>
    public static void Save(SpinixConfig config) => SaveTo(config, ConfigPath);

    /// <summary>保存配置到指定路径。目录不存在则创建。</summary>
    public static void SaveTo(SpinixConfig config, string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(config, JsonOpts);
        File.WriteAllText(path, json);
    }

    public static string ResolveRelativeToAppData(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";
        return Path.IsPathRooted(path) ? path : Path.Combine(ConfigDir, path);
    }
}
