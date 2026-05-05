using Godot;
using slime.data.Abilities;
using slime.data.Units;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 可供 UI / 编辑器选择的资源目录条目。
/// </summary>
/// <param name="ResourceKey">ResourcePaths 中的资源键。</param>
/// <param name="Category">ResourceManagement 加载所需的资源分类。</param>
/// <param name="Path">资源的 res:// 路径。</param>
/// <param name="DisplayName">用户可读名称。</param>
/// <param name="CatalogPath">由资源目录推导出的点分分类路径。</param>
/// <param name="ResourceType">推荐加载类型。</param>
public readonly record struct ResourceCatalogEntry(
    string ResourceKey,
    ResourceCategory Category,
    string Path,
    string DisplayName,
    string CatalogPath,
    Type ResourceType
);

/// <summary>
/// 同一资源分组下的目录条目集合。
/// </summary>
/// <param name="CatalogPath">点分分类路径。</param>
/// <param name="Items">该分组下的目录条目。</param>
public readonly record struct ResourceCatalogGroup(
    string CatalogPath,
    IReadOnlyList<ResourceCatalogEntry> Items
);

/// <summary>
/// 通用资源目录服务。
/// <para>
/// DataNew 表数据直接构建目录条目；场景、特效等资产仍从 ResourcePaths 生成索引构建。
/// </para>
/// </summary>
public static class ResourceCatalog
{
    // 目录推导只处理资产资源；运行时数据目录由 DataNew 静态表直接提供。
    private const string EffectRoot = "assets/Effect";
    private const string AssetUnitEnemyRoot = "assets/Unit/Enemy";
    private const string AssetUnitPlayerRoot = "assets/Unit/Player";
    // 资源目录里的 Resource 只是存放目录，不参与分类名。
    private const string ResourceFolderName = "Resource";
    // 极端情况下用于兜底显示，避免空分类进入 UI。
    private const string UnknownCatalogPath = "未分类";

    // 目录内容会在一次加载后缓存，避免选择器每次刷新都重复遍历整张资源表。
    private static readonly object CacheLock = new();
    private static List<ResourceCatalogEntry>? _cachedEntries;

    /// <summary>
    /// 获取指定目录前缀下的资源目录条目。
    /// </summary>
    /// <param name="catalogPathPrefixes">目录前缀过滤；为空时返回全部支持目录。</param>
    public static IReadOnlyList<ResourceCatalogEntry> GetEntries(params string[] catalogPathPrefixes)
    {
        var entries = EnsureEntries();
        var prefixes = NormalizeCatalogPrefixes(catalogPathPrefixes);
        if (prefixes.Count == 0)
        {
            return entries.ToArray();
        }

        return entries
            .Where(entry => MatchesCatalogPrefixes(entry.CatalogPath, prefixes))
            .OrderBy(entry => entry.CatalogPath, StringComparer.Ordinal)
            .ThenBy(entry => entry.DisplayName, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// 获取按 CatalogPath 分组后的目录条目。
    /// </summary>
    /// <param name="catalogPathPrefixes">目录前缀过滤；为空时返回全部支持目录。</param>
    public static IReadOnlyList<ResourceCatalogGroup> GetGroups(params string[] catalogPathPrefixes)
    {
        var entries = GetEntries(catalogPathPrefixes);
        return entries
            .GroupBy(entry => entry.CatalogPath, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new ResourceCatalogGroup(
                group.Key, // 目录分类路径
                group.OrderBy(entry => entry.DisplayName, StringComparer.Ordinal).ToArray() // 分组条目
            ))
            .ToArray();
    }

    /// <summary>
    /// 按资源键和目录前缀查找目录条目。
    /// </summary>
    /// <param name="resourceKey">ResourcePaths 中的资源键。</param>
    /// <param name="catalogPathPrefix">目录前缀；为空时只按资源键查找。</param>
    /// <param name="entry">查找到的资源目录条目。</param>
    public static bool TryGetEntry(string resourceKey, string catalogPathPrefix, out ResourceCatalogEntry entry)
    {
        var normalizedPrefix = NormalizeCatalogPath(catalogPathPrefix);
        foreach (var candidate in EnsureEntries())
        {
            if (!string.Equals(candidate.ResourceKey, resourceKey, StringComparison.Ordinal))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(normalizedPrefix)
                && !MatchesCatalogPrefix(candidate.CatalogPath, normalizedPrefix))
            {
                continue;
            }

            entry = candidate;
            return true;
        }

        entry = default;
        return false;
    }

    /// <summary>
    /// 清理目录缓存，供资源生成器运行后或测试场景手动刷新时使用。
    /// </summary>
    public static void ClearCache()
    {
        lock (CacheLock)
        {
            _cachedEntries = null;
        }
    }

    /// <summary>
    /// 获取缓存目录；首次访问时构建并缓存。
    /// </summary>
    private static IReadOnlyList<ResourceCatalogEntry> EnsureEntries()
    {
        if (_cachedEntries != null)
        {
            return _cachedEntries;
        }

        lock (CacheLock)
        {
            _cachedEntries ??= BuildEntries();
            return _cachedEntries;
        }
    }

    /// <summary>
    /// 从 <see cref="ResourcePaths"/> 扫描结果构建完整目录列表。
    /// </summary>
    private static List<ResourceCatalogEntry> BuildEntries()
    {
        var entries = new List<ResourceCatalogEntry>();

        AddDataNewEntries(entries);

        // 资产资源仍直接从 ResourcePaths.Resources 构建目录，不额外扫描 res://。
        foreach (var (_, resources) in ResourcePaths.Resources)
        {
            foreach (var (resourceKey, data) in resources)
            {
                if (TryCreateEntry(resourceKey, data, out var entry))
                {
                    entries.Add(entry);
                }
            }
        }

        entries.Sort((left, right) =>
        {
            // 先按目录，再按显示名，保证选择器和分组输出稳定。
            var groupCompare = string.Compare(left.CatalogPath, right.CatalogPath, StringComparison.Ordinal);
            return groupCompare != 0
                ? groupCompare
                : string.Compare(left.DisplayName, right.DisplayName, StringComparison.Ordinal);
        });

        return entries;
    }

    /// <summary>
    /// 尝试把单个资源转换成目录条目。
    /// </summary>
    /// <param name="resourceKey">ResourcePaths 中的资源键。</param>
    /// <param name="data">资源元数据。</param>
    /// <param name="entry">转换后的目录条目。</param>
    private static bool TryCreateEntry(string resourceKey, ResourceData data, out ResourceCatalogEntry entry)
    {
        if (TryCreateEffectEntry(resourceKey, data, out entry))
        {
            return true;
        }

        if (TryCreateAssetUnitEntry(resourceKey, data, out entry))
        {
            return true;
        }

        entry = default;
        return false;
    }

    /// <summary>
    /// 从 DataNew 纯 C# 表构建目录条目。
    /// </summary>
    /// <param name="entries">待写入的目录条目列表。</param>
    private static void AddDataNewEntries(List<ResourceCatalogEntry> entries)
    {
        foreach (var data in EnemyData.All)
        {
            if (string.IsNullOrWhiteSpace(data.Name))
            {
                continue;
            }

            entries.Add(new ResourceCatalogEntry(
                data.Name, // DataNew 名称键
                ResourceCategory.DataUnit, // 保持选择器分类兼容
                "Data/DataNew/Unit/Enemy/EnemyData.cs", // 源数据文件
                data.Name, // UI 显示名
                "Unit.Enemy", // 目录分类路径
                typeof(EnemyData) // 推荐数据类型
            ));
        }

        foreach (var data in PlayerData.All)
        {
            if (string.IsNullOrWhiteSpace(data.Name))
            {
                continue;
            }

            entries.Add(new ResourceCatalogEntry(
                data.Name, // DataNew 名称键
                ResourceCategory.DataUnit, // 保持选择器分类兼容
                "Data/DataNew/Unit/Player/PlayerData.cs", // 源数据文件
                data.Name, // UI 显示名
                "Unit.Player", // 目录分类路径
                typeof(PlayerData) // 推荐数据类型
            ));
        }

        foreach (var data in TargetingIndicatorData.All)
        {
            if (string.IsNullOrWhiteSpace(data.Name))
            {
                continue;
            }

            entries.Add(new ResourceCatalogEntry(
                data.Name, // DataNew 名称键
                ResourceCategory.DataUnit, // 保持选择器分类兼容
                "Data/DataNew/Unit/Targeting/TargetingIndicatorData.cs", // 源数据文件
                data.Name, // UI 显示名
                "Unit.Targeting", // 目录分类路径
                typeof(TargetingIndicatorData) // 推荐数据类型
            ));
        }

        foreach (var data in AbilityData.All)
        {
            if (string.IsNullOrWhiteSpace(data.Name))
            {
                continue;
            }

            var group = string.IsNullOrWhiteSpace(data.FeatureGroupId)
                ? "Ability"
                : $"Ability.{NormalizeCatalogPath(data.FeatureGroupId) ?? "未分类"}";
            entries.Add(new ResourceCatalogEntry(
                data.Name, // DataNew 名称键
                ResourceCategory.DataAbility, // 保持选择器分类兼容
                "Data/DataNew/Ability/AbilityData.cs", // 源数据文件
                data.Name, // UI 显示名
                group, // 目录分类路径
                typeof(AbilityData) // 推荐数据类型
            ));
        }
    }

    /// <summary>
    /// 尝试从特效资源构建目录条目。
    /// </summary>
    /// <param name="resourceKey">ResourcePaths 中的资源键。</param>
    /// <param name="data">资源元数据。</param>
    /// <param name="entry">转换后的目录条目。</param>
    private static bool TryCreateEffectEntry(string resourceKey, ResourceData data, out ResourceCatalogEntry entry)
    {
        // 特效资源使用 assets/Effect 下的子目录作为分类名。
        var catalogPath = ResolveCatalogPath(data.Path, EffectRoot, "Effect");
        if (string.IsNullOrWhiteSpace(catalogPath))
        {
            entry = default;
            return false;
        }

        entry = new ResourceCatalogEntry(
            resourceKey, // ResourcePaths 资源键
            data.Category, // ResourceManagement 分类
            data.Path, // res:// 路径
            resourceKey, // UI 显示名
            catalogPath, // 路径推导分类
            typeof(PackedScene) // 特效场景推荐加载类型
        );
        return true;
    }

    /// <summary>
    /// 尝试从单位 Asset 场景构建目录条目。
    /// </summary>
    /// <param name="resourceKey">ResourcePaths 中的资源键。</param>
    /// <param name="data">资源元数据。</param>
    /// <param name="entry">转换后的目录条目。</param>
    private static bool TryCreateAssetUnitEntry(string resourceKey, ResourceData data, out ResourceCatalogEntry entry)
    {
        string rootPath;
        string rootCatalogName;
        switch (data.Category)
        {
            case ResourceCategory.AssetUnitEnemy:
                rootPath = AssetUnitEnemyRoot;
                rootCatalogName = "AssetUnit.Enemy";
                break;
            case ResourceCategory.AssetUnitPlayer:
                rootPath = AssetUnitPlayerRoot;
                rootCatalogName = "AssetUnit.Player";
                break;
            default:
                entry = default;
                return false;
        }

        var catalogPath = ResolveCatalogPath(data.Path, rootPath, rootCatalogName);
        if (string.IsNullOrWhiteSpace(catalogPath))
        {
            entry = default;
            return false;
        }

        entry = new ResourceCatalogEntry(
            resourceKey, // ResourcePaths 资源键
            data.Category, // ResourceManagement 分类
            data.Path, // res:// 路径
            ResolveAssetDisplayName(data.Path), // UI 显示名
            catalogPath, // 点分分类路径
            typeof(PackedScene) // 单位 Asset 使用 PackedScene 加载
        );
        return true;
    }

    /// <summary>
    /// 解析 Asset 资源在选择器里显示的名称。
    /// </summary>
    /// <param name="path">完整资源路径。</param>
    private static string ResolveAssetDisplayName(string path)
    {
        var relativePath = ToRelativePath(path);
        var parts = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0
            ? path
            : RemoveExtension(parts[^1]);
    }

    /// <summary>
    /// 从资源路径推导点分分类路径。
    /// </summary>
    /// <param name="path">完整资源路径。</param>
    /// <param name="rootPath">用于分类的根目录。</param>
    /// <param name="rootCatalogName">可选的根分类名，用于把根目录名并入分类层级。</param>
    private static string ResolveCatalogPath(string path, string rootPath, string? rootCatalogName = null)
    {
        var relativePath = ToRelativePath(path);
        var normalizedRoot = rootPath.Trim('/').Replace('\\', '/');
        if (!relativePath.StartsWith(normalizedRoot + "/", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var tail = relativePath[(normalizedRoot.Length + 1)..];
        var parts = tail.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (parts.Count == 0)
        {
            return NormalizeCatalogPath(rootCatalogName);
        }

        // 最后一段通常是资源文件名；中间段保留为分类路径。
        parts.RemoveAt(parts.Count - 1); // 最后一段是资源文件名，不参与分类
        if (!string.IsNullOrWhiteSpace(rootCatalogName))
        {
            parts.Insert(0, rootCatalogName);
        }

        var meaningfulParts = parts
            .Where(part => !string.Equals(part, ResourceFolderName, StringComparison.OrdinalIgnoreCase))
            .Select(RemoveExtension)
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToArray();

        return meaningfulParts.Length == 0
            ? NormalizeCatalogPath(rootCatalogName) ?? UnknownCatalogPath
            : NormalizeCatalogPath(string.Join('.', meaningfulParts)) ?? UnknownCatalogPath;
    }

    /// <summary>
    /// 将资源路径标准化为项目相对路径。
    /// </summary>
    /// <param name="path">原始资源路径，支持 res:// 和文件系统路径风格。</param>
    private static string ToRelativePath(string path)
    {
        // 统一为项目相对路径，便于和 ResourcePaths 生成结果对齐。
        var normalizedPath = path.Replace('\\', '/');
        return normalizedPath.StartsWith("res://", StringComparison.OrdinalIgnoreCase)
            ? normalizedPath[6..]
            : normalizedPath;
    }

    /// <summary>
    /// 去掉单个路径段的扩展名。
    /// </summary>
    /// <param name="segment">路径段或文件名。</param>
    private static string RemoveExtension(string segment)
    {
        var extensionIndex = segment.LastIndexOf('.');
        return extensionIndex <= 0 ? segment : segment[..extensionIndex];
    }

    /// <summary>
    /// 标准化外部传入的分类前缀列表。
    /// </summary>
    /// <param name="catalogPathPrefixes">调用方传入的分类前缀。</param>
    private static List<string> NormalizeCatalogPrefixes(string[]? catalogPathPrefixes)
    {
        var prefixes = new List<string>();
        if (catalogPathPrefixes == null)
        {
            return prefixes;
        }

        foreach (var prefix in catalogPathPrefixes)
        {
            var normalized = NormalizeCatalogPath(prefix);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                prefixes.Add(normalized);
            }
        }

        return prefixes;
    }

    /// <summary>
    /// 将分类路径统一为点分格式。
    /// </summary>
    /// <param name="catalogPath">待标准化的分类路径。</param>
    private static string? NormalizeCatalogPath(string? catalogPath)
    {
        if (string.IsNullOrWhiteSpace(catalogPath))
        {
            return null;
        }

        // 允许调用方传入 /、\、. 混用的路径写法，统一成点分目录。
        var parts = catalogPath
            .Trim()
            .Replace('\\', '.')
            .Replace('/', '.')
            .Split('.', StringSplitOptions.RemoveEmptyEntries);

        return parts.Length == 0 ? null : string.Join('.', parts);
    }

    /// <summary>
    /// 判断一个分类路径是否匹配任意前缀。
    /// </summary>
    /// <param name="catalogPath">目录分类路径。</param>
    /// <param name="prefixes">要匹配的前缀集合。</param>
    private static bool MatchesCatalogPrefixes(string catalogPath, IReadOnlyList<string> prefixes)
    {
        foreach (var prefix in prefixes)
        {
            if (MatchesCatalogPrefix(catalogPath, prefix))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 判断分类路径是否命中单个前缀。
    /// </summary>
    /// <param name="catalogPath">目录分类路径。</param>
    /// <param name="prefix">待匹配前缀。</param>
    private static bool MatchesCatalogPrefix(string catalogPath, string prefix)
    {
        return string.Equals(catalogPath, prefix, StringComparison.Ordinal)
            || catalogPath.StartsWith(prefix + ".", StringComparison.Ordinal);
    }
}
