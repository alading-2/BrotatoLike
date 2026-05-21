using System;
using System.Collections.Generic;
using BrotatoLike.Game.Items;
using SlimeAI.GameOS.Capabilities.Attack;
using SlimeAI.GameOS.Capabilities.Damage;
using SlimeAI.GameOS.Capabilities.Movement;
using SlimeAI.GameOS.Runtime.Entity;

namespace BrotatoLike.Game.Shop;

/// <summary>
/// 道具效果应用器。第一版只允许白名单 Runtime Data target，未知 target 必须失败。
/// </summary>
public static class BrotatoLikeItemEffectApplicator
{
    private static readonly HashSet<string> SupportedTargets = new(StringComparer.Ordinal)
    {
        DamageDataKeys.MaxHp.StableKey,
        MovementDataKeys.MoveSpeed.StableKey,
        AttackDataKeys.Damage.StableKey
    };

    /// <summary>
    /// 检查 effect target 是否被第一版支持。
    /// </summary>
    public static bool IsSupportedTarget(string target)
    {
        return SupportedTargets.Contains(target);
    }

    /// <summary>
    /// 应用道具效果，并返回 before/after。
    /// </summary>
    public static bool TryApply(
        IEntity player,
        BrotatoLikeItemDefinition item,
        out string beforeValue,
        out string afterValue,
        out string message)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(item);

        beforeValue = string.Empty;
        afterValue = string.Empty;
        message = string.Empty;
        if (!string.Equals(item.EffectOperation, "Add", StringComparison.Ordinal))
        {
            message = $"unsupported_operation:{item.EffectOperation}";
            return false;
        }

        if (string.Equals(item.EffectTarget, DamageDataKeys.MaxHp.StableKey, StringComparison.Ordinal))
        {
            var before = player.Data.Get<float>(DamageDataKeys.MaxHp, 0f);
            var currentHp = player.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
            var after = before + item.EffectValue;
            player.Data.Set(DamageDataKeys.MaxHp, after);
            player.Data.Set(DamageDataKeys.CurrentHp, currentHp + item.EffectValue);
            beforeValue = Format(before);
            afterValue = Format(after);
            message = "applied:Damage.MaxHp";
            return true;
        }

        if (string.Equals(item.EffectTarget, MovementDataKeys.MoveSpeed.StableKey, StringComparison.Ordinal))
        {
            var before = player.Data.Get<float>(MovementDataKeys.MoveSpeed, 0f);
            var after = before + item.EffectValue;
            player.Data.Set(MovementDataKeys.MoveSpeed, after);
            beforeValue = Format(before);
            afterValue = Format(after);
            message = "applied:Movement.MoveSpeed";
            return true;
        }

        if (string.Equals(item.EffectTarget, AttackDataKeys.Damage.StableKey, StringComparison.Ordinal))
        {
            var before = player.Data.Get<float>(AttackDataKeys.Damage, 0f);
            var after = before + item.EffectValue;
            player.Data.Set(AttackDataKeys.Damage, after);
            beforeValue = Format(before);
            afterValue = Format(after);
            message = "applied:Attack.Damage";
            return true;
        }

        message = $"unknown_effect_target:{item.EffectTarget}";
        return false;
    }

    private static string Format(float value)
    {
        return value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
    }
}
