using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using SkilmeAI.GameOS.Capabilities.Unit;
using SkilmeAI.GameOS.Runtime.Data;
using SkilmeAI.GameOS.Runtime.Entity;
using SkilmeAI.GameOS.Runtime.Schedule;

namespace BrotatoLike.Game;

/// <summary>
/// BrotatoLike 正式启动代码消费 DataOS runtime snapshot 的轻量入口。
/// </summary>
public sealed class BrotatoLikeDataOSBootstrap
{
    private const string EnemyTableId = "unit.enemy";
    private const string DefaultSpawnConfigId = "default";

    private readonly RuntimeDataSnapshot snapshot;

    private BrotatoLikeDataOSBootstrap(RuntimeDataSnapshot snapshot)
    {
        this.snapshot = snapshot;
    }

    /// <summary>
    /// 从 Godot res:// 路径读取 DataOS snapshot。
    /// </summary>
    /// <param name="snapshotPath">DataOS 生成的 runtime snapshot 路径。</param>
    public static BrotatoLikeDataOSBootstrap LoadFromResource(string snapshotPath = "res://DataOS/Snapshots/runtime_snapshot.json")
    {
        using var file = FileAccess.Open(snapshotPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            throw new InvalidOperationException($"DataOS snapshot not found: {snapshotPath}");
        }

        return new BrotatoLikeDataOSBootstrap(RuntimeDataSnapshot.FromJson(file.GetAsText()));
    }

    /// <summary>
    /// 从 JSON 文本创建启动入口，供测试或工具复用。
    /// </summary>
    /// <param name="json">Runtime snapshot JSON 文本。</param>
    public static BrotatoLikeDataOSBootstrap FromJson(string json)
    {
        return new BrotatoLikeDataOSBootstrap(RuntimeDataSnapshot.FromJson(json));
    }

    /// <summary>
    /// 把 snapshot 中的资源路径注册进 ResourceCatalog。
    /// </summary>
    public int RegisterResources()
    {
        return snapshot.RegisterResources();
    }

    /// <summary>
    /// 按 DataOS table 和 record 生成 Runtime Entity，并把 snapshot 字段写入 Entity.Data。
    /// </summary>
    /// <param name="tableId">DataOS 表 Id。</param>
    /// <param name="recordIdOrName">记录 Id 或显示名。</param>
    /// <param name="entityId">生成的 Runtime EntityId。</param>
    public RuntimeEntity SpawnEntityFromRecord(string tableId, string recordIdOrName, string entityId)
    {
        if (!snapshot.TryFindRecord(tableId, recordIdOrName, out var record))
        {
            throw new InvalidOperationException($"DataOS record not found: {tableId}/{recordIdOrName}");
        }

        var entity = EntityManager.Spawn(new EntitySpawnConfig { EntityId = entityId });
        snapshot.ApplyRecord(entity.Data, record);
        return entity;
    }

    /// <summary>
    /// 按 DataOS 生成当前波次可用的敌人生成规则。
    /// </summary>
    /// <param name="wave">当前波次，1 起始。</param>
    /// <param name="enabledOnly">是否只返回启用规则。</param>
    public BrotatoLikeSpawnCatalog BuildEnemySpawnCatalog(int wave = 1, bool enabledOnly = true)
    {
        var spawnConfig = new Data();
        ApplyRecordToData("spawn.config", DefaultSpawnConfigId, spawnConfig);

        var rules = new List<BrotatoLikeSpawnRule>();
        for (var i = 0; i < snapshot.Records.Count; i++)
        {
            var record = snapshot.Records[i];
            if (record.Table != EnemyTableId)
            {
                continue;
            }

            var data = new Data();
            snapshot.ApplyRecord(data, record);

            var enabled = data.Get<bool>(ScheduleDataKeys.SpawnRuleEnabled);
            var minWave = data.Get<int>(ScheduleDataKeys.SpawnMinWave);
            var maxWave = data.Get<int>(ScheduleDataKeys.SpawnMaxWave);
            if ((enabledOnly && !enabled) || !IsWaveAllowed(wave, minWave, maxWave))
            {
                continue;
            }

            rules.Add(new BrotatoLikeSpawnRule(
                record.Table,
                record.Id,
                record.Name,
                enabled,
                data.Get<string>(UnitDataKeys.VisualScenePath),
                data.Get<string>(ScheduleDataKeys.SpawnPositionStrategy),
                minWave,
                maxWave,
                data.Get<float>(ScheduleDataKeys.SpawnInterval),
                data.Get<int>(ScheduleDataKeys.SpawnMaxCountPerWave),
                data.Get<int>(ScheduleDataKeys.SpawnSingleCount),
                data.Get<int>(ScheduleDataKeys.SpawnSingleVariance),
                data.Get<float>(ScheduleDataKeys.SpawnStartDelay),
                data.Get<int>(ScheduleDataKeys.SpawnWeight)));
        }

        rules.Sort(CompareSpawnRules);
        return new BrotatoLikeSpawnCatalog(
            wave,
            spawnConfig.Get<float>(ScheduleDataKeys.WaveDuration),
            spawnConfig.Get<int>(ScheduleDataKeys.MaxWaves),
            spawnConfig.Get<float>(ScheduleDataKeys.WaveBreakTime),
            rules);
    }

    /// <summary>
    /// 从生成规则创建 Runtime Entity，供后续 SpawnSystem 复用。
    /// </summary>
    /// <param name="rule">DataOS 生成规则。</param>
    /// <param name="entityId">生成的 Runtime EntityId。</param>
    public RuntimeEntity SpawnEnemyFromRule(BrotatoLikeSpawnRule rule, string entityId)
    {
        ArgumentNullException.ThrowIfNull(rule);
        return SpawnEntityFromRecord(rule.TableId, rule.RecordId, entityId);
    }

    /// <summary>
    /// 从 DataOS system.config 生成 RuntimeSchedule 可消费的 SpawnSystem 配置。
    /// </summary>
    public SystemConfig BuildSpawnSystemScheduleConfig()
    {
        return BuildSystemScheduleConfig("SpawnSystem");
    }

    /// <summary>
    /// 从 DataOS system.config 生成 RuntimeSchedule 配置。
    /// </summary>
    /// <param name="systemId">系统 Id。</param>
    public SystemConfig BuildSystemScheduleConfig(string systemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(systemId);

        var data = new Data();
        if (ApplyRecordToData("system.config", systemId, data) == 0)
        {
            throw new InvalidOperationException($"DataOS system.config not found: {systemId}");
        }

        return new SystemConfig
        {
            SystemId = data.Get<string>(ScheduleDataKeys.SystemId, systemId),
            Group = data.Get<SystemGroup>(ScheduleDataKeys.MountGroup, SystemGroup.Else),
            Tags = ParseFlags(data.Get<string>(ScheduleDataKeys.Tags, string.Empty), SystemTag.None),
            Required = data.Get<bool>(ScheduleDataKeys.Required),
            StartEnabled = data.Get<bool>(ScheduleDataKeys.StartEnabled, true),
            Priority = data.Get<int>(ScheduleDataKeys.Priority),
            Dependencies = SplitList(data.Get<string>(ScheduleDataKeys.Dependencies, string.Empty)),
            RunCondition = new SystemRunCondition
            {
                AllowedFlowStates = ParseFlags(
                    data.Get<string>(ScheduleDataKeys.AllowedFlowStates, string.Empty),
                    GameFlowState.None),
                RequiredOverlays = ParseFlags(
                    data.Get<string>(ScheduleDataKeys.RequiredOverlays, string.Empty),
                    OverlayFlags.None),
                BlockedOverlays = ParseFlags(
                    data.Get<string>(ScheduleDataKeys.BlockedOverlays, string.Empty),
                    OverlayFlags.None),
                AllowedSimulationStates = ParseFlags(
                    data.Get<string>(ScheduleDataKeys.AllowedSimulationStates, string.Empty),
                    SimulationState.None)
            }
        };
    }

    /// <summary>
    /// 把 snapshot 记录写入已有 Runtime Data。
    /// </summary>
    /// <param name="tableId">DataOS 表 Id。</param>
    /// <param name="recordIdOrName">记录 Id 或显示名。</param>
    /// <param name="data">目标 Data 容器。</param>
    public int ApplyRecordToData(string tableId, string recordIdOrName, Data data)
    {
        if (!snapshot.TryFindRecord(tableId, recordIdOrName, out var record))
        {
            return 0;
        }

        return snapshot.ApplyRecord(data, record);
    }

    private static int CompareSpawnRules(BrotatoLikeSpawnRule left, BrotatoLikeSpawnRule right)
    {
        var result = left.MinWave.CompareTo(right.MinWave);
        if (result != 0) return result;

        result = right.Weight.CompareTo(left.Weight);
        if (result != 0) return result;

        return string.Compare(left.RecordId, right.RecordId, StringComparison.Ordinal);
    }

    private static bool IsWaveAllowed(int wave, int minWave, int maxWave)
    {
        return wave >= minWave && (maxWave < 0 || wave <= maxWave);
    }

    private static string[] SplitList(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    private static TEnum ParseFlags<TEnum>(string value, TEnum fallback)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        long result = 0;
        var parts = value.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            if (!Enum.TryParse<TEnum>(parts[i], ignoreCase: false, out var parsed))
            {
                return fallback;
            }

            result |= Convert.ToInt64(parsed, CultureInfo.InvariantCulture);
        }

        return (TEnum)Enum.ToObject(typeof(TEnum), result);
    }
}

/// <summary>
/// BrotatoLike 生成系统消费的 DataOS 敌人生成目录。
/// </summary>
public sealed record BrotatoLikeSpawnCatalog(
    int Wave,
    float WaveDuration,
    int MaxWaves,
    float WaveBreakTime,
    IReadOnlyList<BrotatoLikeSpawnRule> EnemyRules);

/// <summary>
/// 单个敌人生成规则，来自 DataOS unit.enemy 记录。
/// </summary>
public sealed record BrotatoLikeSpawnRule(
    string TableId,
    string RecordId,
    string DisplayName,
    bool IsEnabled,
    string VisualScenePath,
    string PositionStrategy,
    int MinWave,
    int MaxWave,
    float Interval,
    int MaxCountPerWave,
    int SingleCount,
    int SingleVariance,
    float StartDelay,
    int Weight);
