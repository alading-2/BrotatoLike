using System;
using System.Collections.Generic;
using Godot;
using SkilmeAI.GameOS.Capabilities.Ability;
using SkilmeAI.GameOS.GodotBridge;
using SkilmeAI.GameOS.Runtime.Entity;

namespace BrotatoLike.Game;

/// <summary>
/// Godot 主动技能输入组件：监听 <see cref="GodotPlayerInputComponent" /> 发射的技能输入事件，
/// 管理 <see cref="AbilityDataKeys.OwnedAbilityIds" /> 和 <see cref="AbilityDataKeys.CurrentAbilityIndex" />，
/// 并通过 <see cref="AbilityService" /> 触发当前选中技能。
/// <para>挂载到玩家 GodotEntity2D 下，作为玩家主动技能的输入控制器。</para>
/// </summary>
public partial class GodotActiveSkillInputComponent : Node, IGodotComponent
{
    private IEntity? entity;
    private Action<GameEventType.Input.UseSkillEventData>? useSkillHandler;
    private Action<GameEventType.Input.PreviousSkillEventData>? previousSkillHandler;
    private Action<GameEventType.Input.NextSkillEventData>? nextSkillHandler;

    /// <inheritdoc />
    public void OnComponentRegistered(IEntity entity, Node entityNode)
    {
        this.entity = entity;
        AbilityDataKeys.RegisterAll();

        useSkillHandler = OnUseSkill;
        previousSkillHandler = OnPreviousSkill;
        nextSkillHandler = OnNextSkill;

        entity.Events.On(GameEventType.Input.UseSkill, useSkillHandler);
        entity.Events.On(GameEventType.Input.PreviousSkill, previousSkillHandler);
        entity.Events.On(GameEventType.Input.NextSkill, nextSkillHandler);
    }

    /// <inheritdoc />
    public void OnComponentUnregistered(IEntity? entity, Node? entityNode)
    {
        if (this.entity != null && useSkillHandler != null)
        {
            this.entity.Events.Off(GameEventType.Input.UseSkill, useSkillHandler);
            this.entity.Events.Off(GameEventType.Input.PreviousSkill, previousSkillHandler!);
            this.entity.Events.Off(GameEventType.Input.NextSkill, nextSkillHandler!);
        }

        this.entity = null;
        useSkillHandler = null;
        previousSkillHandler = null;
        nextSkillHandler = null;
    }

    private void OnUseSkill(GameEventType.Input.UseSkillEventData data)
    {
        if (entity == null)
        {
            return;
        }

        var ownedIds = entity.Data.Get<List<string>>(AbilityDataKeys.OwnedAbilityIds);
        if (ownedIds == null || ownedIds.Count == 0)
        {
            return;
        }

        var currentIndex = entity.Data.Get<int>(AbilityDataKeys.CurrentAbilityIndex, 0);
        if (currentIndex < 0 || currentIndex >= ownedIds.Count)
        {
            currentIndex = 0;
        }

        var abilityId = ownedIds[currentIndex];
        var ability = EntityManager.Get(abilityId);
        if (ability == null)
        {
            return;
        }

        // 尝试自动索敌构建施法上下文
        if (!AbilityTargetingTool.TryBuildContext(entity, ability, out var context))
        {
            return;
        }

        if (context == null)
        {
            return;
        }

        AbilityService.Instance.TryTrigger(context);
    }

    private void OnPreviousSkill(GameEventType.Input.PreviousSkillEventData data)
    {
        if (entity == null)
        {
            return;
        }

        var ownedIds = entity.Data.Get<List<string>>(AbilityDataKeys.OwnedAbilityIds);
        if (ownedIds == null || ownedIds.Count == 0)
        {
            return;
        }

        var currentIndex = entity.Data.Get<int>(AbilityDataKeys.CurrentAbilityIndex, 0);
        var newIndex = Mathf.PosMod(currentIndex - 1, ownedIds.Count);
        entity.Data.Set(AbilityDataKeys.CurrentAbilityIndex, newIndex);
    }

    private void OnNextSkill(GameEventType.Input.NextSkillEventData data)
    {
        if (entity == null)
        {
            return;
        }

        var ownedIds = entity.Data.Get<List<string>>(AbilityDataKeys.OwnedAbilityIds);
        if (ownedIds == null || ownedIds.Count == 0)
        {
            return;
        }

        var currentIndex = entity.Data.Get<int>(AbilityDataKeys.CurrentAbilityIndex, 0);
        var newIndex = Mathf.PosMod(currentIndex + 1, ownedIds.Count);
        entity.Data.Set(AbilityDataKeys.CurrentAbilityIndex, newIndex);
    }
}
