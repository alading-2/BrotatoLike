using System;
using System.Collections.Generic;
using Godot;
using SlimeAI.GameOS.Capabilities.Ability;
using SlimeAI.GameOS.Runtime.Entity;

namespace BrotatoLike.Game;

/// <summary>
/// BrotatoLike 玩家技能装配 authoring。当前为游戏侧集中配置，后续可迁入 DataOS 业务表。
/// </summary>
internal static class BrotatoLikeSkillLoadoutAuthoring
{
    public const int VisibleActiveSlotCapacity = 4;
    public const string SourceDefault = "default";
    public const string SourceValidationOverride = "validation-override";
    public const string SourceLevelUpChoice = "level-up-choice";
    public const string LoadoutSourceMeta = "SkillLoadoutSource";
    public const string DefaultActiveAbilityIdsMeta = "SkillDefaultActiveAbilityIds";
    public const string AvailableSkillPoolIdsMeta = "SkillAvailablePoolIds";
    public const string PassiveSkillIdsMeta = "SkillPassiveIds";
    public const string OwnedAbilityEntityIdsMeta = "SkillOwnedAbilityEntityIds";
    public const string VisibleActiveAbilityEntityIdsMeta = "SkillVisibleActiveAbilityEntityIds";
    public const string VisibleActiveAbilityRecordIdsMeta = "SkillVisibleActiveAbilityRecordIds";
    public const string TotalOwnedCountMeta = "SkillTotalOwnedCount";
    public const string VisibleSlotCountMeta = "SkillVisibleSlotCount";
    public const string HiddenOwnedCountMeta = "SkillHiddenOwnedCount";

    public static readonly string[] DefaultActiveAbilityIds =
    [
        "slam",
        "chain_lightning",
        "target_point_skill",
        "dash"
    ];

    public static readonly string[] UnlockableActiveAbilityIds =
    [
        "sine_wave_shot",
        "boomerang_throw",
        "bezier_shot",
        "parabola_shot",
        "arc_shot"
    ];

    public static readonly string[] PassiveAbilityIds =
    [
        "orbit_skill",
        "circle_damage",
        "aura_shield"
    ];

    public static readonly string[] AvailableSkillPoolAbilityIds =
    [
        "slam",
        "chain_lightning",
        "target_point_skill",
        "dash",
        "sine_wave_shot",
        "boomerang_throw",
        "bezier_shot",
        "parabola_shot",
        "arc_shot",
        "orbit_skill",
        "circle_damage",
        "aura_shield"
    ];

    public static readonly string[] ValidationAllSkillAbilityIds =
    [
        "slam",
        "chain_lightning",
        "target_point_skill",
        "dash",
        "sine_wave_shot",
        "boomerang_throw",
        "bezier_shot",
        "parabola_shot",
        "arc_shot",
        "orbit_skill",
        "circle_damage",
        "aura_shield"
    ];

    public static BrotatoLikeSkillLoadout CreateDefault()
    {
        return new BrotatoLikeSkillLoadout(SourceDefault, DefaultActiveAbilityIds, DefaultActiveAbilityIds);
    }

    public static BrotatoLikeSkillLoadout CreateValidationOverride(IReadOnlyList<string> abilityIds)
    {
        ArgumentNullException.ThrowIfNull(abilityIds);
        var allIds = DistinctKnownIds(abilityIds);
        var visibleIds = ResolveVisibleActiveIds(allIds);
        return new BrotatoLikeSkillLoadout(SourceValidationOverride, allIds, visibleIds);
    }

    public static void ValidateAvailableSkillPool(BrotatoLikeDataOSBootstrap bootstrap)
    {
        ArgumentNullException.ThrowIfNull(bootstrap);
        var missing = new List<string>();
        for (var i = 0; i < AvailableSkillPoolAbilityIds.Length; i++)
        {
            if (!bootstrap.HasRecord("ability", AvailableSkillPoolAbilityIds[i]))
            {
                missing.Add(AvailableSkillPoolAbilityIds[i]);
            }
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException($"BrotatoLike skill pool references missing DataOS ability ids: {string.Join(",", missing)}");
        }
    }

    public static EntityIdList ResolveSelectableAbilityEntityIds(IEntity owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        var ownedIds = owner.Data.Get<EntityIdList>(AbilityDataKeys.OwnedAbilityIds);
        var fromMeta = ReadEntityIdsMeta(owner, VisibleActiveAbilityEntityIdsMeta);
        if (fromMeta.Count > 0)
        {
            var selected = EntityIdList.Empty;
            for (var i = 0; i < fromMeta.Count; i++)
            {
                if (ownedIds.Contains(fromMeta[i]))
                {
                    selected = selected.Add(fromMeta[i]);
                }
            }

            if (selected.Count > 0)
            {
                return selected;
            }
        }

        var fallback = EntityIdList.Empty;
        for (var i = 0; i < ownedIds.Count && i < VisibleActiveSlotCapacity; i++)
        {
            fallback = fallback.Add(ownedIds[i]);
        }

        return fallback;
    }

    public static string JoinIds(IEnumerable<string> ids)
    {
        return string.Join(",", ids);
    }

    public static string JoinIds(EntityIdList ids)
    {
        var values = new List<string>();
        for (var i = 0; i < ids.Count; i++)
        {
            values.Add(ids[i].Value);
        }

        return string.Join(",", values);
    }

    private static string[] DistinctKnownIds(IReadOnlyList<string> ids)
    {
        var result = new List<string>();
        for (var i = 0; i < ids.Count; i++)
        {
            var id = ids[i];
            if (string.IsNullOrWhiteSpace(id) || result.Contains(id))
            {
                continue;
            }

            result.Add(id);
        }

        return result.ToArray();
    }

    private static string[] ResolveVisibleActiveIds(IReadOnlyList<string> ids)
    {
        var result = new List<string>();
        for (var i = 0; i < ids.Count && result.Count < VisibleActiveSlotCapacity; i++)
        {
            if (IsPassiveAbility(ids[i]))
            {
                continue;
            }

            result.Add(ids[i]);
        }

        return result.ToArray();
    }

    public static bool IsPassiveAbility(string abilityId)
    {
        for (var i = 0; i < PassiveAbilityIds.Length; i++)
        {
            if (string.Equals(PassiveAbilityIds[i], abilityId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static EntityIdList ReadEntityIdsMeta(IEntity owner, string key)
    {
        if (owner is not Node node || !node.HasMeta(key))
        {
            return EntityIdList.Empty;
        }

        var raw = node.GetMeta(key).AsString();
        var result = EntityIdList.Empty;
        var parts = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            result = result.Add(new EntityId(parts[i]));
        }

        return result;
    }
}

internal sealed record BrotatoLikeSkillLoadout(
    string Source,
    IReadOnlyList<string> AbilityIds,
    IReadOnlyList<string> VisibleActiveAbilityIds);
