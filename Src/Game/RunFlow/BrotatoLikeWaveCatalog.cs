using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace BrotatoLike.Game.RunFlow;

/// <summary>
/// BrotatoLike wave authoring 导出快照。
/// </summary>
public sealed class BrotatoLikeWaveAuthoringSnapshot
{
    /// <summary>
    /// 快照 schema 版本。
    /// </summary>
    public int SchemaVersion { get; init; }

    /// <summary>
    /// 生成时间 UTC 字符串。
    /// </summary>
    public string GeneratedAtUtc { get; init; } = string.Empty;

    /// <summary>
    /// 快照来源。
    /// </summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>
    /// 已授权波次定义。
    /// </summary>
    public List<BrotatoLikeWaveDefinition> Waves { get; init; } = new();

    /// <summary>
    /// 已授权波次敌人条目。
    /// </summary>
    public List<BrotatoLikeWaveEnemyEntry> Entries { get; init; } = new();
}

/// <summary>
/// 单个波次定义。
/// </summary>
public sealed class BrotatoLikeWaveDefinition
{
    /// <summary>
    /// 波次编号，1 起始。
    /// </summary>
    public int WaveId { get; init; }

    /// <summary>
    /// 波次显示名。
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// 波次持续秒数。
    /// </summary>
    public float WaveDuration { get; init; }

    /// <summary>
    /// 波间奖励阶段建议秒数。
    /// </summary>
    public float RewardPhaseSeconds { get; init; }

    /// <summary>
    /// 完成模式。
    /// </summary>
    public string CompletionMode { get; init; } = string.Empty;

    /// <summary>
    /// 下一个波次编号；为空表示 run 结束。
    /// </summary>
    public int? NextWaveId { get; init; }

    /// <summary>
    /// 完成后的下一阶段。
    /// </summary>
    public string NextPhase { get; init; } = string.Empty;

    /// <summary>
    /// 奖励系统 hook 标识。
    /// </summary>
    public string RewardHook { get; init; } = string.Empty;

    /// <summary>
    /// 商店 offer set 标识。
    /// </summary>
    public string ShopOfferSetId { get; init; } = string.Empty;

    /// <summary>
    /// 设计说明。
    /// </summary>
    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// 单个波次中的敌人生成条目。
/// </summary>
public sealed class BrotatoLikeWaveEnemyEntry
{
    /// <summary>
    /// 所属波次编号。
    /// </summary>
    public int WaveId { get; init; }

    /// <summary>
    /// 波次内排序槽位。
    /// </summary>
    public int SlotIndex { get; init; }

    /// <summary>
    /// DataOS 敌人记录 Id。
    /// </summary>
    public string EnemyId { get; init; } = string.Empty;

    /// <summary>
    /// 敌人显示名。
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// 敌人视觉场景路径。
    /// </summary>
    public string VisualScenePath { get; init; } = string.Empty;

    /// <summary>
    /// 生成位置策略。
    /// </summary>
    public string PositionStrategy { get; init; } = string.Empty;

    /// <summary>
    /// 生成间隔秒数。
    /// </summary>
    public float Interval { get; init; }

    /// <summary>
    /// 本波最多生成数量。
    /// </summary>
    public int MaxCount { get; init; }

    /// <summary>
    /// 单次生成数量。
    /// </summary>
    public int SingleCount { get; init; }

    /// <summary>
    /// 单次生成数量浮动，当前仅保留 authoring 字段。
    /// </summary>
    public int SingleVariance { get; init; }

    /// <summary>
    /// 首次生成延迟秒数。
    /// </summary>
    public float StartDelay { get; init; }

    /// <summary>
    /// 生成权重。
    /// </summary>
    public int Weight { get; init; }
}

/// <summary>
/// BrotatoLike 多波 run flow authoring catalog。
/// </summary>
public sealed class BrotatoLikeWaveCatalog
{
    private readonly Dictionary<int, BrotatoLikeWaveDefinition> wavesById;
    private readonly Dictionary<int, List<BrotatoLikeWaveEnemyEntry>> entriesByWave;

    private BrotatoLikeWaveCatalog(
        BrotatoLikeWaveAuthoringSnapshot snapshot,
        Dictionary<int, BrotatoLikeWaveDefinition> wavesById,
        Dictionary<int, List<BrotatoLikeWaveEnemyEntry>> entriesByWave)
    {
        Snapshot = snapshot;
        this.wavesById = wavesById;
        this.entriesByWave = entriesByWave;
    }

    /// <summary>
    /// DataOS 导出的原始 wave 快照。
    /// </summary>
    public BrotatoLikeWaveAuthoringSnapshot Snapshot { get; }

    /// <summary>
    /// 所有波次定义。
    /// </summary>
    public IReadOnlyCollection<BrotatoLikeWaveDefinition> Waves => wavesById.Values;

    /// <summary>
    /// 从 Godot res:// 路径读取 wave authoring。
    /// </summary>
    public static BrotatoLikeWaveCatalog LoadFromResource(string path = "res://DataOS/Snapshots/wave_authoring.json")
    {
        using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
        if (file == null)
        {
            throw new InvalidOperationException($"BrotatoLike wave authoring not found: {path}");
        }

        return FromJson(file.GetAsText());
    }

    /// <summary>
    /// 从 JSON 文本读取 wave authoring。
    /// </summary>
    public static BrotatoLikeWaveCatalog FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var snapshot = JsonSerializer.Deserialize<BrotatoLikeWaveAuthoringSnapshot>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("BrotatoLike wave authoring JSON 解析失败。");

        var wavesById = new Dictionary<int, BrotatoLikeWaveDefinition>();
        for (var i = 0; i < snapshot.Waves.Count; i++)
        {
            var wave = snapshot.Waves[i];
            if (wave.WaveId <= 0)
            {
                throw new InvalidOperationException("BrotatoLike wave id must be positive.");
            }

            if (!wavesById.TryAdd(wave.WaveId, wave))
            {
                throw new InvalidOperationException($"Duplicate BrotatoLike wave id: {wave.WaveId}");
            }

            if (wave.WaveDuration <= 0f)
            {
                throw new InvalidOperationException($"BrotatoLike wave duration must be positive: {wave.WaveId}");
            }

            if (!string.Equals(wave.CompletionMode, "AllEnemiesDefeated", StringComparison.Ordinal)
                && !string.Equals(wave.CompletionMode, "DurationOrClear", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Unsupported BrotatoLike wave completion mode: {wave.WaveId}/{wave.CompletionMode}");
            }
        }

        var entriesByWave = new Dictionary<int, List<BrotatoLikeWaveEnemyEntry>>();
        for (var i = 0; i < snapshot.Entries.Count; i++)
        {
            var entry = snapshot.Entries[i];
            if (!wavesById.ContainsKey(entry.WaveId))
            {
                throw new InvalidOperationException($"Wave entry references missing wave id: {entry.WaveId}/{entry.EnemyId}");
            }

            if (string.IsNullOrWhiteSpace(entry.EnemyId))
            {
                throw new InvalidOperationException($"Wave entry enemy id is empty: wave {entry.WaveId}");
            }

            if (entry.MaxCount < -1 || entry.SingleCount <= 0 || entry.Interval <= 0f)
            {
                throw new InvalidOperationException($"Invalid wave enemy spawn numbers: wave {entry.WaveId}/{entry.EnemyId}");
            }

            if (!entriesByWave.TryGetValue(entry.WaveId, out var entries))
            {
                entries = new List<BrotatoLikeWaveEnemyEntry>();
                entriesByWave[entry.WaveId] = entries;
            }

            entries.Add(entry);
        }

        foreach (var entries in entriesByWave.Values)
        {
            entries.Sort(static (left, right) => left.SlotIndex.CompareTo(right.SlotIndex));
        }

        return new BrotatoLikeWaveCatalog(snapshot, wavesById, entriesByWave);
    }

    /// <summary>
    /// 使用 DataOS bootstrap 和 ResourceLoader 校验 wave authoring。
    /// </summary>
    public void Validate(BrotatoLikeDataOSBootstrap bootstrap)
    {
        ArgumentNullException.ThrowIfNull(bootstrap);
        foreach (var entry in Snapshot.Entries)
        {
            if (!bootstrap.HasRecord("unit.enemy", entry.EnemyId))
            {
                throw new InvalidOperationException($"Wave entry references missing DataOS enemy id: {entry.WaveId}/{entry.EnemyId}");
            }

            if (string.IsNullOrWhiteSpace(entry.VisualScenePath) || !ResourceLoader.Exists(entry.VisualScenePath))
            {
                throw new InvalidOperationException($"Wave entry enemy visual resource is missing: {entry.WaveId}/{entry.EnemyId}/{entry.VisualScenePath}");
            }
        }
    }

    /// <summary>
    /// 查找波次定义。
    /// </summary>
    public bool TryGetWave(int waveId, out BrotatoLikeWaveDefinition definition)
    {
        return wavesById.TryGetValue(waveId, out definition!);
    }

    /// <summary>
    /// 查找下一个波次。
    /// </summary>
    public bool TryGetNextWaveId(int waveId, out int nextWaveId)
    {
        nextWaveId = 0;
        if (!wavesById.TryGetValue(waveId, out var wave) || wave.NextWaveId is not { } next)
        {
            return false;
        }

        nextWaveId = next;
        return wavesById.ContainsKey(nextWaveId);
    }

    /// <summary>
    /// 构造 SpawnSystem 消费的当前波次 catalog。
    /// </summary>
    public BrotatoLikeSpawnCatalog BuildSpawnCatalog(int waveId)
    {
        if (!wavesById.TryGetValue(waveId, out var wave))
        {
            throw new InvalidOperationException($"BrotatoLike wave definition not found: {waveId}");
        }

        if (!entriesByWave.TryGetValue(waveId, out var entries) || entries.Count == 0)
        {
            throw new InvalidOperationException($"BrotatoLike wave has no enemy entries: {waveId}");
        }

        var rules = new List<BrotatoLikeSpawnRule>(entries.Count);
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            rules.Add(new BrotatoLikeSpawnRule(
                "unit.enemy",
                entry.EnemyId,
                entry.DisplayName,
                true,
                entry.VisualScenePath,
                entry.PositionStrategy,
                waveId,
                waveId,
                entry.Interval,
                entry.MaxCount,
                entry.SingleCount,
                entry.SingleVariance,
                entry.StartDelay,
                entry.Weight));
        }

        return new BrotatoLikeSpawnCatalog(
            waveId,
            wave.WaveDuration,
            MaxWaveId(),
            wave.RewardPhaseSeconds,
            rules);
    }

    private int MaxWaveId()
    {
        var result = 0;
        foreach (var waveId in wavesById.Keys)
        {
            result = Math.Max(result, waveId);
        }

        return result;
    }
}
