using System;
using System.Collections.Generic;
using SlimeAI.GameOS.Capabilities.Ability;
using SlimeAI.GameOS.Capabilities.Damage;
using SlimeAI.GameOS.Capabilities.Movement;
using SlimeAI.GameOS.Runtime.Entity;

namespace BrotatoLike.Game.Progression;

/// <summary>
/// BrotatoLike 升级选择首版 authoring。当前保持游戏侧集中配置，后续可迁入 DataOS 业务表。
/// </summary>
public static class BrotatoLikeLevelUpChoiceAuthoring
{
    /// <summary>首版选择数量。</summary>
    public const int ChoiceCount = 3;

    /// <summary>升级选择门禁策略。</summary>
    public const string GatePolicy = "ModalUi+Suspended";

    /// <summary>
    /// 生成确定性的升级选择。
    /// </summary>
    /// <param name="player">玩家实体。</param>
    public static IReadOnlyList<BrotatoLikeLevelUpChoiceDefinition> CreateChoices(IEntity player)
    {
        ArgumentNullException.ThrowIfNull(player);

        var choices = new List<BrotatoLikeLevelUpChoiceDefinition>(ChoiceCount)
        {
            new(
                "max_hp_plus_10",
                "生命上限 +10",
                "立刻提升生命上限并治疗等量生命。",
                BrotatoLikeLevelUpChoiceEffectType.AddMaxHp,
                DamageDataKeys.MaxHp.StableKey,
                10f),
            new(
                "move_speed_plus_20",
                "移动速度 +20",
                "提升玩家基础移动速度。",
                BrotatoLikeLevelUpChoiceEffectType.AddMoveSpeed,
                MovementDataKeys.MoveSpeed.StableKey,
                20f)
        };

        choices.Add(OwnsAbilityRecord(player, "sine_wave_shot")
            ? new BrotatoLikeLevelUpChoiceDefinition(
                "slam_level_plus_1",
                "猛击等级 +1",
                "提升已拥有的猛击技能等级。",
                BrotatoLikeLevelUpChoiceEffectType.UpgradeAbilityLevel,
                AbilityDataKeys.Level.StableKey,
                1f,
                "slam")
            : new BrotatoLikeLevelUpChoiceDefinition(
                "unlock_sine_wave_shot",
                "解锁正弦波射击",
                "把正弦波射击加入本局拥有技能，保留为隐藏技能供后续替换/面板流程使用。",
                BrotatoLikeLevelUpChoiceEffectType.GrantAbility,
                AbilityDataKeys.OwnedAbilityIds.StableKey,
                1f,
                "sine_wave_shot"));

        return choices;
    }

    private static bool OwnsAbilityRecord(IEntity player, string abilityRecordId)
    {
        var ownedIds = player.Data.Get<EntityIdList>(AbilityDataKeys.OwnedAbilityIds);
        for (var i = 0; i < ownedIds.Count; i++)
        {
            if (ownedIds[i].Value.Contains($"ability-{abilityRecordId}-", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// 升级选择效果类型。
/// </summary>
public enum BrotatoLikeLevelUpChoiceEffectType
{
    /// <summary>增加生命上限。</summary>
    AddMaxHp,

    /// <summary>增加移动速度。</summary>
    AddMoveSpeed,

    /// <summary>授予技能。</summary>
    GrantAbility,

    /// <summary>提升已拥有技能等级。</summary>
    UpgradeAbilityLevel
}

/// <summary>
/// 单个升级选择定义。
/// </summary>
public sealed record BrotatoLikeLevelUpChoiceDefinition(
    string Id,
    string DisplayText,
    string Description,
    BrotatoLikeLevelUpChoiceEffectType EffectType,
    string EffectTarget,
    float EffectValue,
    string AbilityRecordId = "");

/// <summary>
/// 升级选择应用结果，用于 UI 和 validation artifact 记录 before/after。
/// </summary>
public sealed record BrotatoLikeLevelUpChoiceApplyResult(
    bool Success,
    string ChoiceId,
    string EffectType,
    string EffectTarget,
    string BeforeValue,
    string AfterValue,
    string Message);
