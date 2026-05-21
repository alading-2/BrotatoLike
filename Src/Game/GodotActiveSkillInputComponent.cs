using System;
using BrotatoLike.Game.Bridge;
using BrotatoLike.Game.Events;
using Godot;
using SlimeAI.GameOS.Capabilities.Ability;
using SlimeAI.GameOS.Capabilities.Damage;
using SlimeAI.GameOS.Capabilities.Movement;
using SlimeAI.GameOS.GodotBridge;
using SlimeAI.GameOS.Runtime.Entity;

namespace BrotatoLike.Game;

/// <summary>
/// Godot 主动技能输入组件：监听 <see cref="BrotatoLikePlayerInputComponent" /> 发射的技能输入事件，
/// 管理 <see cref="AbilityDataKeys.OwnedAbilityIds" /> 和 <see cref="AbilityDataKeys.CurrentAbilityIndex" />，
/// 并通过 <see cref="AbilityService" /> 触发当前选中技能。
/// <para>挂载到玩家 GodotEntity2D 下，作为玩家主动技能的输入控制器。</para>
/// </summary>
public partial class GodotActiveSkillInputComponent : Node, IGodotComponent
{
    private IEntity? entity;
    private IDisposable? useSkillSub;
    private IDisposable? previousSkillSub;
    private IDisposable? nextSkillSub;
    private IDisposable? selectSkillSlotSub;

    /// <summary>
    /// 最近一次主动技能触发报告。
    /// </summary>
    public AbilityTriggerReport? LastTriggerReport { get; private set; }

    /// <inheritdoc />
    public void OnComponentRegistered(IEntity entity, Node entityNode)
    {
        this.entity = entity;
        AbilityDataKeys.RegisterAll();

        useSkillSub = entity.Events.Subscribe<InputUseSkill>(OnUseSkill);
        previousSkillSub = entity.Events.Subscribe<InputPreviousSkill>(OnPreviousSkill);
        nextSkillSub = entity.Events.Subscribe<InputNextSkill>(OnNextSkill);
        selectSkillSlotSub = entity.Events.Subscribe<InputSelectSkillSlot>(OnSelectSkillSlot);
    }

    /// <inheritdoc />
    public void OnComponentUnregistered(IEntity? entity, Node? entityNode)
    {
        useSkillSub?.Dispose();
        previousSkillSub?.Dispose();
        nextSkillSub?.Dispose();
        selectSkillSlotSub?.Dispose();

        this.entity = null;
        useSkillSub = null;
        previousSkillSub = null;
        nextSkillSub = null;
        selectSkillSlotSub = null;
    }

    private void OnUseSkill(InputUseSkill data)
    {
        if (entity == null || !CanUseSkillExecution())
        {
            return;
        }

        var selectableIds = BrotatoLikeSkillLoadoutAuthoring.ResolveSelectableAbilityEntityIds(entity);
        if (selectableIds.Count == 0)
        {
            return;
        }

        var currentIndex = entity.Data.Get<int>(AbilityDataKeys.CurrentAbilityIndex, 0);
        if (currentIndex < 0 || currentIndex >= selectableIds.Count)
        {
            currentIndex = 0;
            entity.Data.Set(AbilityDataKeys.CurrentAbilityIndex, currentIndex);
        }

        var abilityId = selectableIds[currentIndex];
        var ability = EntityManager.Get(abilityId);
        if (ability == null)
        {
            return;
        }

        if (TryFindTargetingController() is { } targetingController
            && targetingController.TryHandleUseSkill(entity, ability, out var targetingReport))
        {
            LastTriggerReport = targetingReport;
            return;
        }

        // 尝试自动索敌构建施法上下文
        if (!AbilityTargetingTool.TryBuildContext(entity, ability, out var context))
        {
            LastTriggerReport = new AbilityTriggerReport(AbilityTriggerResult.FailNoTarget, null, "failed to build ability target context");
            return;
        }

        if (context == null)
        {
            LastTriggerReport = new AbilityTriggerReport(AbilityTriggerResult.FailNoTarget, null, "ability target context is null");
            return;
        }

        LastTriggerReport = AbilityService.Instance.TryTrigger(context);
    }

    private void OnPreviousSkill(InputPreviousSkill data)
    {
        if (entity == null || !CanSelectSkill())
        {
            return;
        }

        var selectableIds = BrotatoLikeSkillLoadoutAuthoring.ResolveSelectableAbilityEntityIds(entity);
        if (selectableIds.Count == 0)
        {
            return;
        }

        var currentIndex = entity.Data.Get<int>(AbilityDataKeys.CurrentAbilityIndex, 0);
        var newIndex = Mathf.PosMod(currentIndex - 1, selectableIds.Count);
        entity.Data.Set(AbilityDataKeys.CurrentAbilityIndex, newIndex);
    }

    private void OnNextSkill(InputNextSkill data)
    {
        if (entity == null || !CanSelectSkill())
        {
            return;
        }

        var selectableIds = BrotatoLikeSkillLoadoutAuthoring.ResolveSelectableAbilityEntityIds(entity);
        if (selectableIds.Count == 0)
        {
            return;
        }

        var currentIndex = entity.Data.Get<int>(AbilityDataKeys.CurrentAbilityIndex, 0);
        var newIndex = Mathf.PosMod(currentIndex + 1, selectableIds.Count);
        entity.Data.Set(AbilityDataKeys.CurrentAbilityIndex, newIndex);
    }

    private void OnSelectSkillSlot(InputSelectSkillSlot data)
    {
        if (entity == null || !CanSelectSkill())
        {
            return;
        }

        var selectableIds = BrotatoLikeSkillLoadoutAuthoring.ResolveSelectableAbilityEntityIds(entity);
        if (data.SlotIndex < 0 || data.SlotIndex >= selectableIds.Count)
        {
            return;
        }

        entity.Data.Set(AbilityDataKeys.CurrentAbilityIndex, data.SlotIndex);
    }

    private bool CanSelectSkill()
    {
        if (entity == null)
        {
            return false;
        }

        return !entity.Data.Get<bool>(DamageDataKeys.IsDead, false);
    }

    private bool CanUseSkillExecution()
    {
        return CanSelectSkill()
            && entity != null
            && entity.Data.Get<bool>(MovementDataKeys.CanMoveInput, true);
    }

    private BrotatoLikeTargetingController? TryFindTargetingController()
    {
        var current = GetParent();
        while (current != null)
        {
            if (current is BrotatoLikeGameRuntime runtime)
            {
                return runtime.TargetingController;
            }

            current = current.GetParent();
        }

        return IsInsideTree()
            ? GetTree()?.Root.FindChild("BrotatoLikeTargetingController", recursive: true, owned: false) as BrotatoLikeTargetingController
            : null;
    }
}
