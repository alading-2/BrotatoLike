using System;
using System.Collections.Generic;
using System.Text.Json;
using BrotatoLike.Game;
using Godot;

namespace BrotatoLike.Game.Characters;

/// <summary>
/// BrotatoLike character authoring 快照。
/// </summary>
public sealed class BrotatoLikeCharacterAuthoringSnapshot
{
    public int SchemaVersion { get; init; }

    public string GeneratedAtUtc { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;

    public List<BrotatoLikeCharacterDefinition> Characters { get; init; } = new();

    public List<BrotatoLikeCharacterLoadoutEntry> LoadoutEntries { get; init; } = new();
}

/// <summary>
/// 可选角色定义。
/// </summary>
public sealed class BrotatoLikeCharacterDefinition
{
    public string Id { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string PlayerRecordId { get; init; } = string.Empty;

    public string VisualScenePath { get; init; } = string.Empty;

    public string StartingLoadoutId { get; init; } = string.Empty;

    public int SortOrder { get; init; }

    public bool IsDefault { get; init; }

    public string UnlockState { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public float MaxHp { get; init; }

    public float MoveSpeed { get; init; }

    public float AttackDamage { get; init; }
}

/// <summary>
/// 角色起始技能槽位。
/// </summary>
public sealed class BrotatoLikeCharacterLoadoutEntry
{
    public string LoadoutId { get; init; } = string.Empty;

    public int SlotIndex { get; init; }

    public string AbilityId { get; init; } = string.Empty;

    public bool IsVisible { get; init; }
}

/// <summary>
/// BrotatoLike 可选角色目录。
/// </summary>
public sealed class BrotatoLikeCharacterCatalog
{
    public const string DefaultCharacterId = "deluyi";

    private readonly Dictionary<string, BrotatoLikeCharacterDefinition> charactersById;
    private readonly Dictionary<string, List<BrotatoLikeCharacterLoadoutEntry>> loadoutEntriesById;

    private BrotatoLikeCharacterCatalog(
        BrotatoLikeCharacterAuthoringSnapshot snapshot,
        Dictionary<string, BrotatoLikeCharacterDefinition> charactersById,
        Dictionary<string, List<BrotatoLikeCharacterLoadoutEntry>> loadoutEntriesById)
    {
        Snapshot = snapshot;
        this.charactersById = charactersById;
        this.loadoutEntriesById = loadoutEntriesById;
    }

    /// <summary>
    /// 原始 authoring 快照。
    /// </summary>
    public BrotatoLikeCharacterAuthoringSnapshot Snapshot { get; }

    /// <summary>
    /// 按 authoring 排序的角色列表。
    /// </summary>
    public IReadOnlyList<BrotatoLikeCharacterDefinition> Characters => Snapshot.Characters;

    /// <summary>
    /// 从 Godot res:// 路径读取角色 authoring。
    /// </summary>
    public static BrotatoLikeCharacterCatalog LoadFromResource(string path = "res://DataOS/Snapshots/character_authoring.json")
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            throw new InvalidOperationException($"BrotatoLike character authoring not found: {path}");
        }

        return FromJson(file.GetAsText());
    }

    /// <summary>
    /// 从 JSON 文本创建目录。
    /// </summary>
    public static BrotatoLikeCharacterCatalog FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var snapshot = JsonSerializer.Deserialize<BrotatoLikeCharacterAuthoringSnapshot>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("BrotatoLike character authoring JSON 解析失败。");

        var charactersById = new Dictionary<string, BrotatoLikeCharacterDefinition>(StringComparer.Ordinal);
        for (var i = 0; i < snapshot.Characters.Count; i++)
        {
            var character = snapshot.Characters[i];
            if (string.IsNullOrWhiteSpace(character.Id)
                || string.IsNullOrWhiteSpace(character.PlayerRecordId)
                || string.IsNullOrWhiteSpace(character.VisualScenePath)
                || string.IsNullOrWhiteSpace(character.StartingLoadoutId))
            {
                throw new InvalidOperationException("BrotatoLike character authoring contains an incomplete character row.");
            }

            if (!charactersById.TryAdd(character.Id, character))
            {
                throw new InvalidOperationException($"Duplicate BrotatoLike character id: {character.Id}");
            }
        }

        var loadoutEntriesById = new Dictionary<string, List<BrotatoLikeCharacterLoadoutEntry>>(StringComparer.Ordinal);
        for (var i = 0; i < snapshot.LoadoutEntries.Count; i++)
        {
            var entry = snapshot.LoadoutEntries[i];
            if (string.IsNullOrWhiteSpace(entry.LoadoutId) || string.IsNullOrWhiteSpace(entry.AbilityId))
            {
                throw new InvalidOperationException("BrotatoLike character loadout contains an incomplete row.");
            }

            if (!loadoutEntriesById.TryGetValue(entry.LoadoutId, out var entries))
            {
                entries = new List<BrotatoLikeCharacterLoadoutEntry>();
                loadoutEntriesById.Add(entry.LoadoutId, entries);
            }

            entries.Add(entry);
        }

        foreach (var entries in loadoutEntriesById.Values)
        {
            entries.Sort((left, right) => left.SlotIndex.CompareTo(right.SlotIndex));
        }

        snapshot.Characters.Sort(CompareCharacters);
        return new BrotatoLikeCharacterCatalog(snapshot, charactersById, loadoutEntriesById);
    }

    /// <summary>
    /// 验证角色引用的 DataOS record、视觉资源和技能 id。
    /// </summary>
    public void Validate(BrotatoLikeDataOSBootstrap bootstrap)
    {
        ArgumentNullException.ThrowIfNull(bootstrap);
        if (Snapshot.Characters.Count < 2)
        {
            throw new InvalidOperationException("BrotatoLike character catalog must contain at least two selectable characters.");
        }

        for (var i = 0; i < Snapshot.Characters.Count; i++)
        {
            var character = Snapshot.Characters[i];
            if (!bootstrap.HasRecord("unit.player", character.PlayerRecordId))
            {
                throw new InvalidOperationException($"Character references missing player record: {character.Id}/{character.PlayerRecordId}");
            }

            if (!ResourceLoader.Exists(character.VisualScenePath))
            {
                throw new InvalidOperationException($"Character references missing visual scene: {character.Id}/{character.VisualScenePath}");
            }

            var entries = GetLoadoutEntries(character.StartingLoadoutId);
            if (entries.Count == 0)
            {
                throw new InvalidOperationException($"Character references empty loadout: {character.Id}/{character.StartingLoadoutId}");
            }

            for (var j = 0; j < entries.Count; j++)
            {
                if (!bootstrap.HasRecord("ability", entries[j].AbilityId))
                {
                    throw new InvalidOperationException($"Character loadout references missing ability: {character.Id}/{entries[j].AbilityId}");
                }
            }
        }
    }

    public bool TryGetCharacter(string characterId, out BrotatoLikeCharacterDefinition character)
    {
        return charactersById.TryGetValue(characterId, out character!);
    }

    public BrotatoLikeCharacterDefinition GetDefaultCharacter()
    {
        for (var i = 0; i < Snapshot.Characters.Count; i++)
        {
            if (Snapshot.Characters[i].IsDefault)
            {
                return Snapshot.Characters[i];
            }
        }

        if (charactersById.TryGetValue(DefaultCharacterId, out var fallback))
        {
            return fallback;
        }

        return Snapshot.Characters[0];
    }

    public IReadOnlyList<BrotatoLikeCharacterLoadoutEntry> GetLoadoutEntries(string loadoutId)
    {
        return loadoutEntriesById.TryGetValue(loadoutId, out var entries)
            ? entries
            : Array.Empty<BrotatoLikeCharacterLoadoutEntry>();
    }

    internal BrotatoLikeSkillLoadout BuildLoadout(BrotatoLikeCharacterDefinition character)
    {
        ArgumentNullException.ThrowIfNull(character);
        var entries = GetLoadoutEntries(character.StartingLoadoutId);
        var allIds = new List<string>();
        var visibleIds = new List<string>();
        for (var i = 0; i < entries.Count; i++)
        {
            allIds.Add(entries[i].AbilityId);
            if (entries[i].IsVisible)
            {
                visibleIds.Add(entries[i].AbilityId);
            }
        }

        return BrotatoLikeSkillLoadoutAuthoring.CreateForCharacter(character.Id, allIds, visibleIds);
    }

    public string JoinVisibleAbilityIds(BrotatoLikeCharacterDefinition character)
    {
        var entries = GetLoadoutEntries(character.StartingLoadoutId);
        var ids = new List<string>();
        for (var i = 0; i < entries.Count; i++)
        {
            if (entries[i].IsVisible)
            {
                ids.Add(entries[i].AbilityId);
            }
        }

        return string.Join(",", ids);
    }

    private static int CompareCharacters(BrotatoLikeCharacterDefinition left, BrotatoLikeCharacterDefinition right)
    {
        var result = left.SortOrder.CompareTo(right.SortOrder);
        return result != 0 ? result : string.Compare(left.Id, right.Id, StringComparison.Ordinal);
    }
}
