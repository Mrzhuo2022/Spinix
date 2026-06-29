namespace Spinix.Config;

/// <summary>
/// 配置迁移器：把旧版本配置逐步升级到当前版本。
///
/// 背景：SpinixConfig 随版本演进会新增字段。当用户从旧版升级时，磁盘上的 config.json
/// 缺少新字段，System.Text.Json 反序列化时会用属性初始值——但这只对"有默认值的属性"有效。
/// 更隐蔽的问题是：某些字段的"语义默认"与"0"不同（如 SubWheelEnterDelayMs 默认 220，
/// 但旧配置反序列化后该字段若存在且为 0，或属性未初始化则为 0），会导致行为异常
/// （子轮盘进入延迟变 0 → 过于灵敏）。迁移器负责修正这类历史包袱。
///
/// 版本历史：
///   v0：初始版本（无 Version 字段，或显式为 0）。缺少 SubWheelEnterDelayMs /
///       SubWheelRetreatDelayMs，需补全为默认 220。
///   v1（当前）：完整版本，含子轮盘延迟字段。
/// </summary>
public static class ConfigMigrator
{
    /// <summary>当前配置版本号。</summary>
    public const int CurrentVersion = 1;

    /// <summary>子轮盘延迟的语义默认值（与 SpinixConfig.CreateDefault 一致）。</summary>
    private const int DefaultSubWheelDelayMs = 220;

    /// <summary>
    /// 把任意版本的配置迁移到当前版本。原地修改并返回同一对象。
    /// 对已是当前版本的配置是幂等的（无副作用）。
    /// </summary>
    public static SpinixConfig Migrate(SpinixConfig config)
    {
        // 按版本号逐步迁移：每个分支负责从该版本升到下一版本
        // v0 → v1：补全子轮盘延迟字段
        if (config.Version < 1)
        {
            MigrateV0ToV1(config);
            config.Version = 1;
        }

        // 未来版本迁移在此追加：
        // if (config.Version < 2) { MigrateV1ToV2(config); config.Version = 2; }

        // 确保 Version 不超过当前（防止手工编辑成超大值）
        if (config.Version > CurrentVersion)
            config.Version = CurrentVersion;

        // 卫生检查：无论版本号如何，修正任何残留的非法延迟值
        // （防止用户手工编辑 config.json 写入 0/负数导致行为异常）
        EnsureValidDelayFields(config);

        return config;
    }

    /// <summary>确保子轮盘延迟字段合法（>0），非法则补默认值。每次迁移都执行。</summary>
    private static void EnsureValidDelayFields(SpinixConfig config)
    {
        if (config.SubWheelEnterDelayMs <= 0)
            config.SubWheelEnterDelayMs = DefaultSubWheelDelayMs;
        if (config.SubWheelRetreatDelayMs <= 0)
            config.SubWheelRetreatDelayMs = DefaultSubWheelDelayMs;
    }

    /// <summary>判断配置是否需要迁移。</summary>
    public static bool NeedsMigration(SpinixConfig config)
        => config.Version < CurrentVersion || HasInvalidDelayFields(config);

    /// <summary>
    /// v0 → v1 迁移：补全子轮盘进入/回退延迟。
    /// 旧配置（v0）没有这俩字段或显式为 0，需补为默认 220。
    /// 实际修正由 EnsureValidDelayFields 统一完成（v0 步骤仅标记版本升级）。
    /// </summary>
    private static void MigrateV0ToV1(SpinixConfig config)
    {
        // 字段修正委托给 EnsureValidDelayFields（在 Migrate 末尾统一执行）
    }

    /// <summary>检测是否存在非法的延迟字段（0 或负数）。</summary>
    private static bool HasInvalidDelayFields(SpinixConfig config)
        => config.SubWheelEnterDelayMs <= 0 || config.SubWheelRetreatDelayMs <= 0;
}
