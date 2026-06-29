using System.Collections.Generic;
using System.Linq;

namespace Spinix.Config;

/// <summary>
/// 轮盘配置编辑的纯逻辑助手：把 SettingsWindow 中"轮盘 ID 唯一性校验、
/// SubWheel 孤儿条目检测、条目移动、删除轮盘时的关联清理"等可测试逻辑提取出来，
/// 不依赖 WPF UI（MessageBox/TreeView），便于单元测试。
/// </summary>
public static class WheelConfigEditor
{
    /// <summary>
    /// 校验所有轮盘 ID 唯一。存在重复返回 false。
    /// </summary>
    public static bool HasUniqueWheelIds(SpinixConfig config)
    {
        var ids = config.Wheels.Select(w => w.Id).ToList();
        return ids.Distinct().Count() == ids.Count;
    }

    /// <summary>查找所有重复的轮盘 ID（便于提示用户具体冲突）。</summary>
    public static IReadOnlyList<string> FindDuplicateWheelIds(SpinixConfig config)
    {
        return config.Wheels
            .GroupBy(w => w.Id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
    }

    /// <summary>判断给定轮盘 ID 是否存在。</summary>
    public static bool WheelExists(SpinixConfig config, string wheelId)
        => config.Wheels.Any(w => w.Id == wheelId);

    /// <summary>
    /// 检测所有指向不存在轮盘的 SubWheel 条目（孤儿引用）。
    /// 返回 (wheelId, itemName, targetId) 三元组列表。
    /// </summary>
    public static IReadOnlyList<(string WheelId, string ItemName, string TargetId)> FindOrphanSubWheelItems(SpinixConfig config)
    {
        var result = new List<(string, string, string)>();
        foreach (var wheel in config.Wheels)
        {
            foreach (var item in wheel.Items)
            {
                if (item.ActionType == WheelActionType.SubWheel &&
                    !WheelExists(config, item.Argument))
                {
                    result.Add((wheel.Id, item.Name, item.Argument));
                }
            }
        }
        return result;
    }

    /// <summary>判断配置是否通过保存前的完整性校验（ID 唯一 + 无孤儿引用）。</summary>
    public static bool IsValidForSave(SpinixConfig config)
        => HasUniqueWheelIds(config) && FindOrphanSubWheelItems(config).Count == 0;

    /// <summary>
    /// 计算条目向上移动后的新索引；若已在顶部返回原索引。
    /// </summary>
    public static int ComputeMoveUpIndex(IList<WheelItem> items, int currentIndex)
    {
        if (currentIndex <= 0 || currentIndex >= items.Count) return currentIndex;
        return currentIndex - 1;
    }

    /// <summary>
    /// 计算条目向下移动后的新索引；若已在底部返回原索引。
    /// </summary>
    public static int ComputeMoveDownIndex(IList<WheelItem> items, int currentIndex)
    {
        if (currentIndex < 0 || currentIndex >= items.Count - 1) return currentIndex;
        return currentIndex + 1;
    }

    /// <summary>
    /// 在列表中交换两个索引位置的条目（原地修改）。索引非法时返回 false 不修改。
    /// </summary>
    public static bool TrySwapItems(IList<WheelItem> items, int indexA, int indexB)
    {
        if (indexA < 0 || indexB < 0 || indexA >= items.Count || indexB >= items.Count) return false;
        (items[indexA], items[indexB]) = (items[indexB], items[indexA]);
        return true;
    }

    /// <summary>
    /// 删除指定轮盘，并清理所有指向它的 SubWheel 条目。
    /// 返回被清理的孤儿条目数量。主轮盘（Id=="main"）不可删除，返回 -1。
    /// </summary>
    public static int DeleteWheelAndCleanReferences(SpinixConfig config, string wheelId)
    {
        if (wheelId == "main") return -1;
        var target = config.Wheels.FirstOrDefault(w => w.Id == wheelId);
        if (target == null) return 0;

        int cleaned = 0;
        foreach (var wheel in config.Wheels)
        {
            cleaned += wheel.Items.RemoveAll(
                i => i.ActionType == WheelActionType.SubWheel && i.Argument == wheelId);
        }
        config.Wheels.Remove(target);
        return cleaned;
    }

    /// <summary>删除指定 ID 的条目（从所有轮盘中移除）。返回实际移除的数量。</summary>
    public static int DeleteItemById(SpinixConfig config, string itemId)
    {
        int removed = 0;
        foreach (var wheel in config.Wheels)
            removed += wheel.Items.RemoveAll(i => i.Id == itemId);
        return removed;
    }

    /// <summary>
    /// 创建一个新条目（工厂方法，保证默认值一致）。
    /// </summary>
    public static WheelItem CreateNewItem(string? name = null)
        => new()
        {
            Name = name ?? Spinix.Resources.Localization.T("NewItemDefaultName"),
            Icon = "circle",
            ActionType = WheelActionType.LaunchApp,
            Argument = "",
        };

    /// <summary>
    /// 创建一个新子轮盘，并（可选）在主轮盘中添加指向它的 SubWheel 条目。
    /// 返回新轮盘的 ID。
    /// </summary>
    public static string CreateSubWheel(SpinixConfig config, string? name = null)
    {
        var wheel = new Wheel { Id = GenerateWheelId(config), Name = name ?? Spinix.Resources.Localization.T("NewWheelDefaultName") };
        config.Wheels.Add(wheel);

        // 确保主轮盘存在——若不存在则创建，避免子轮盘成为无入口的孤立轮盘。
        // 注意 GetMainWheel() 在无 Id=="main" 时会回退返回第一个轮盘，故需检查 Id。
        var main = config.Wheels.FirstOrDefault(w => w.Id == "main");
        if (main == null)
        {
            main = new Wheel { Id = "main", Name = "Main Wheel" };
            config.Wheels.Insert(0, main);
        }
        main.Items.Add(new WheelItem
        {
            Name = wheel.Name,
            Icon = "wheel",
            ActionType = WheelActionType.SubWheel,
            Argument = wheel.Id,
        });

        return wheel.Id;
    }

    /// <summary>生成不与现有轮盘冲突的唯一 ID。</summary>
    public static string GenerateWheelId(SpinixConfig config)
    {
        string id;
        do
        {
            id = System.Guid.NewGuid().ToString("N");
        } while (WheelExists(config, id));
        return id;
    }
}
