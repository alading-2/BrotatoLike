using System;
using System.Collections.Generic;
using Godot;
using SlimeAI.GameOS.Capabilities.Ability;
using SlimeAI.GameOS.Capabilities.Collision;
using SlimeAI.GameOS.Capabilities.Damage;
using SlimeAI.GameOS.Capabilities.Movement;
using SlimeAI.GameOS.Capabilities.Unit;
using SlimeAI.GameOS.GodotBridge;
using SlimeAI.GameOS.Runtime.Entity;

namespace BrotatoLike.Game.UI;

/// <summary>
/// BrotatoLike 正式运行 HUD，使用 PackedScene 实例化复合 UI，代码只负责数据绑定。
/// </summary>
public partial class BrotatoLikeHud : CanvasLayer
{
    private const float DefaultHeadHealthBarHeight = 36f;
    private const float DamageNumberWorldYOffset = 36f;

    private readonly Dictionary<EntityId, HealthBarUI> headHealthBars = new();
    private readonly Dictionary<EntityId, float> lastHpByEntity = new();
    private readonly List<DamageNumberUI> activeDamageNumbers = new();

    private BrotatoLikeGameRuntime? runtime;
    private ActiveSkillBarUI? activeSkillBar;
    private Control? headHealthBarLayer;
    private Control? damageNumberLayer;
    private Label? playerHealthLabel;
    private Label? progressionSummary;

    private PackedScene? healthBarScene;
    private PackedScene? damageNumberScene;
    private PackedScene? activeSkillBarScene;

    /// <summary>
    /// 绑定游戏运行时。
    /// </summary>
    public void Bind(BrotatoLikeGameRuntime runtime)
    {
        this.runtime = runtime;
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        Name = "BrotatoLikeHUD";
        Layer = 20;

        healthBarScene = GD.Load<PackedScene>("res://Scenes/UI/HealthBarUI.tscn");
        damageNumberScene = GD.Load<PackedScene>("res://Scenes/UI/DamageNumberUI.tscn");
        activeSkillBarScene = GD.Load<PackedScene>("res://Scenes/UI/ActiveSkillBarUI.tscn");

        BuildTree();
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        if (runtime == null)
        {
            return;
        }

        UpdatePlayerHud();
        UpdateSkillBar();
        UpdateHeadHealthBars();
        UpdateDamageNumbers((float)delta);
    }

    private void BuildTree()
    {
        // scene-first exception: root 是简单布局容器，非复合 UI
        var root = new Control
        {
            Name = "HudRoot",
            AnchorRight = 1f,
            AnchorBottom = 1f,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        AddChild(root);

        // scene-first exception: 简单 Label，后续迁移到 HUD root scene
        playerHealthLabel = new Label
        {
            Name = "PlayerHealthLabel",
            Text = "HP 0/0",
            Position = new Vector2(16f, 14f)
        };
        root.AddChild(playerHealthLabel);

        // scene-first exception: 简单 Label，后续迁移到 HUD root scene
        progressionSummary = new Label
        {
            Name = "ProgressionSummary",
            Text = "Lv 1  XP 0/5  Wave 1",
            Position = new Vector2(16f, 40f)
        };
        root.AddChild(progressionSummary);

        // scene-backed: 技能栏来自 ActiveSkillBarUI.tscn
        activeSkillBar = activeSkillBarScene!.Instantiate<ActiveSkillBarUI>();
        activeSkillBar.Name = "ActiveSkillBar";
        activeSkillBar.Position = new Vector2(16f, 72f);
        root.AddChild(activeSkillBar);

        // scene-first exception: 简单容器层，非复合 UI
        headHealthBarLayer = new Control
        {
            Name = "HeadHealthBarLayer",
            AnchorRight = 1f,
            AnchorBottom = 1f,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        root.AddChild(headHealthBarLayer);

        // scene-first exception: 简单容器层，非复合 UI
        damageNumberLayer = new Control
        {
            Name = "DamageNumberLayer",
            AnchorRight = 1f,
            AnchorBottom = 1f,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        root.AddChild(damageNumberLayer);
    }

    private void UpdatePlayerHud()
    {
        var player = runtime?.PlayerEntity;
        if (player == null || playerHealthLabel == null || progressionSummary == null)
        {
            return;
        }

        var currentHp = player.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var maxHp = player.Data.Get<float>(DamageDataKeys.MaxHp, 0f);
        var isDead = player.Data.Get<bool>(DamageDataKeys.IsDead, false) || currentHp <= 0f;
        playerHealthLabel.Text = isDead ? "DEAD" : FormattableString.Invariant($"HP {currentHp:0}/{maxHp:0}");
        playerHealthLabel.SetMeta("CurrentHp", currentHp);
        playerHealthLabel.SetMeta("MaxHp", maxHp);
        playerHealthLabel.SetMeta("Dead", isDead);

        var level = ReadIntMeta(player, "Level", 1);
        var experience = ReadIntMeta(player, "Experience", 0);
        var nextLevelExperience = ReadIntMeta(player, "NextLevelExperience", 5);
        var waveIndex = runtime?.ProgressionService?.WaveIndex ?? 1;
        progressionSummary.Text = FormattableString.Invariant($"Lv {level}  XP {experience}/{nextLevelExperience}  Wave {waveIndex}");
        progressionSummary.SetMeta("Level", level);
        progressionSummary.SetMeta("Experience", experience);
        progressionSummary.SetMeta("NextLevelExperience", nextLevelExperience);
        progressionSummary.SetMeta("WaveIndex", waveIndex);
    }

    private void UpdateSkillBar()
    {
        if (activeSkillBar == null)
        {
            return;
        }

        var player = runtime?.PlayerEntity;
        if (player == null)
        {
            activeSkillBar.ClearAll();
            return;
        }

        var ownedIds = player.Data.Get<EntityIdList>(AbilityDataKeys.OwnedAbilityIds);
        var selectedIndex = player.Data.Get<int>(AbilityDataKeys.CurrentAbilityIndex, 0);
        for (var i = 0; i < 4; i++)
        {
            var slot = activeSkillBar.GetSlot(i);
            if (slot == null)
            {
                continue;
            }

            if (i >= ownedIds.Count)
            {
                slot.Clear();
                continue;
            }

            var ability = EntityManager.Get(ownedIds[i]);
            if (ability == null)
            {
                slot.Bind("-", $"{i + 1}", 0f, 0, 0, i == selectedIndex);
                continue;
            }

            var name = ability.Data.Get(AbilityDataKeys.Name, ability.EntityId.Value);
            if (string.IsNullOrWhiteSpace(name))
            {
                name = ability.EntityId.Value;
            }

            var cooldown = ability.Data.Get<float>(AbilityDataKeys.CooldownRemaining, 0f);
            var cooldownMax = ability.Data.Get<float>(AbilityDataKeys.Cooldown, 1f);
            var cooldownFraction = cooldownMax > 0f ? Mathf.Clamp(cooldown / cooldownMax, 0f, 1f) : 0f;
            var charges = ability.Data.Get<int>(AbilityDataKeys.CurrentCharges, 0);
            var maxCharges = ability.Data.Get<int>(AbilityDataKeys.MaxCharges, 0);
            slot.Bind(name, $"{i + 1}", cooldownFraction, charges, maxCharges, i == selectedIndex);
        }

        activeSkillBar.SetMeta("SelectedIndex", selectedIndex);
        activeSkillBar.SetMeta("SlotCount", Math.Min(4, ownedIds.Count));
    }

    private void UpdateHeadHealthBars()
    {
        if (headHealthBarLayer == null)
        {
            return;
        }

        var liveEnemyIds = new HashSet<EntityId>();
        var entities = EntityManager.GetAll();
        for (var i = 0; i < entities.Count; i++)
        {
            if (entities[i] is not GodotEntity2D node)
            {
                continue;
            }

            if (node.Data.Get<int>(CollisionDataKeys.Team, 0) != 2)
            {
                TrackHpChange(node);
                continue;
            }

            var maxHp = node.Data.Get<float>(DamageDataKeys.MaxHp, 0f);
            var currentHp = node.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
            var dead = node.Data.Get<bool>(DamageDataKeys.IsDead, false) || currentHp <= 0f || node.IsQueuedForDeletion();
            if (dead)
            {
                RemoveHeadBar(node.EntityId);
                TrackHpChange(node);
                continue;
            }

            var showHealthBar = node.Data.Get<bool>(UnitDataKeys.IsShowHealthBar, true)
                || node.Data.Get<float>(UnitDataKeys.HealthBarHeight, 0f) > 0f;
            if (!showHealthBar || maxHp <= 0f)
            {
                TrackHpChange(node);
                continue;
            }

            liveEnemyIds.Add(node.EntityId);
            var bar = EnsureHeadHealthBar(node);
            var healthBarHeight = ResolveHeadHealthBarHeight(node.Data.Get<float>(UnitDataKeys.HealthBarHeight, 0f));
            var headWorldPosition = node.GlobalPosition + new Vector2(0f, -healthBarHeight);
            var canvasPosition = WorldToCanvasPosition(headWorldPosition);
            bar.BindHealth(currentHp, maxHp);
            bar.SetCanvasPosition(canvasPosition);
            bar.SetMeta("EntityId", node.EntityId.Value);
            bar.SetMeta("CurrentHp", currentHp);
            bar.SetMeta("MaxHp", maxHp);
            bar.SetMeta("WorldPosition", $"{node.GlobalPosition.X:0.###},{node.GlobalPosition.Y:0.###}");
            bar.SetMeta("HeadWorldPosition", $"{headWorldPosition.X:0.###},{headWorldPosition.Y:0.###}");
            bar.SetMeta("CanvasPosition", $"{canvasPosition.X:0.###},{canvasPosition.Y:0.###}");
            bar.SetMeta("HealthBarHeight", healthBarHeight);

            TrackHpChange(node);
        }

        var stale = new List<EntityId>();
        foreach (var entry in headHealthBars)
        {
            if (!liveEnemyIds.Contains(entry.Key))
            {
                stale.Add(entry.Key);
            }
        }

        for (var i = 0; i < stale.Count; i++)
        {
            RemoveHeadBar(stale[i]);
        }
    }

    private HealthBarUI EnsureHeadHealthBar(GodotEntity2D enemy)
    {
        if (headHealthBars.TryGetValue(enemy.EntityId, out var existing)
            && GodotObject.IsInstanceValid(existing))
        {
            return existing;
        }

        var bar = healthBarScene!.Instantiate<HealthBarUI>();
        bar.Name = $"HeadHealthBar_{enemy.EntityId.Value}";
        headHealthBarLayer!.AddChild(bar);
        headHealthBars[enemy.EntityId] = bar;
        return bar;
    }

    private void RemoveHeadBar(EntityId entityId)
    {
        if (!headHealthBars.Remove(entityId, out var bar))
        {
            return;
        }

        if (GodotObject.IsInstanceValid(bar))
        {
            bar.QueueFree();
        }
    }

    private void TrackHpChange(IEntity entity)
    {
        var currentHp = entity.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        if (!lastHpByEntity.TryGetValue(entity.EntityId, out var previousHp))
        {
            lastHpByEntity[entity.EntityId] = currentHp;
            return;
        }

        if (Math.Abs(currentHp - previousHp) < 0.001f)
        {
            return;
        }

        var delta = currentHp - previousHp;
        lastHpByEntity[entity.EntityId] = currentHp;
        SpawnDamageNumber(entity, delta);
    }

    private void SpawnDamageNumber(IEntity entity, float hpDelta)
    {
        if (damageNumberLayer == null || damageNumberScene == null || Math.Abs(hpDelta) < 0.001f)
        {
            return;
        }

        var position = entity.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
        var worldPosition = new Vector2(position.X, position.Y - DamageNumberWorldYOffset);
        var canvasPosition = WorldToCanvasPosition(worldPosition);
        var damageNumber = damageNumberScene.Instantiate<DamageNumberUI>();
        var isHeal = hpDelta > 0f;
        damageNumber.Name = isHeal ? $"HealNumber_Player_{activeDamageNumbers.Count}" : $"DamageNumber_Enemy_{activeDamageNumbers.Count}";
        damageNumber.TreeExiting += () =>
        {
            activeDamageNumbers.Remove(damageNumber);
        };
        damageNumberLayer.AddChild(damageNumber);
        damageNumber.ShowDamage(hpDelta, canvasPosition);
        damageNumber.SetMeta("WorldPosition", $"{worldPosition.X:0.###},{worldPosition.Y:0.###}");
        damageNumber.SetMeta("CanvasPosition", $"{canvasPosition.X:0.###},{canvasPosition.Y:0.###}");
        activeDamageNumbers.Add(damageNumber);
    }

    private void UpdateDamageNumbers(float deltaSeconds)
    {
        // DamageNumberUI 自带动画生命周期，由 AnimationPlayer 控制自动释放。
        // 保留此方法以支持未来的非动画 fallback 清理。
        for (var i = activeDamageNumbers.Count - 1; i >= 0; i--)
        {
            if (!GodotObject.IsInstanceValid(activeDamageNumbers[i]))
            {
                activeDamageNumbers.RemoveAt(i);
            }
        }
    }

    private static int ReadIntMeta(Node node, string key, int fallback)
    {
        if (!node.HasMeta(key))
        {
            return fallback;
        }

        var value = node.GetMeta(key);
        return value.VariantType switch
        {
            Variant.Type.Int => value.AsInt32(),
            Variant.Type.Float => Mathf.RoundToInt(value.AsSingle()),
            Variant.Type.String => int.TryParse(value.AsString(), out var parsed) ? parsed : fallback,
            _ => fallback
        };
    }

    private Vector2 WorldToCanvasPosition(Vector2 worldPosition)
    {
        return GetViewport().GetCanvasTransform() * worldPosition;
    }

    private static float ResolveHeadHealthBarHeight(float configuredHeight)
    {
        return configuredHeight > 0f ? configuredHeight : DefaultHeadHealthBarHeight;
    }
}
