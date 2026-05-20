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
/// BrotatoLike 正式运行 HUD，读取 Runtime Data 并输出玩家可见状态。
/// </summary>
public partial class BrotatoLikeHud : CanvasLayer
{
    private readonly Dictionary<EntityId, ProgressBar> headHealthBars = new();
    private readonly Dictionary<EntityId, float> lastHpByEntity = new();
    private readonly Dictionary<Label, float> floatingTextLife = new();
    private readonly List<Label> skillSlots = new();

    private BrotatoLikeGameRuntime? runtime;
    private Label? playerHealthLabel;
    private Label? progressionSummary;
    private HBoxContainer? activeSkillBar;
    private Control? headHealthBarLayer;
    private Control? damageNumberLayer;

    /// <summary>
    /// 绑定游戏运行时。
    /// </summary>
    /// <param name="runtime">BrotatoLike 运行时。</param>
    public void Bind(BrotatoLikeGameRuntime runtime)
    {
        this.runtime = runtime;
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        Name = "BrotatoLikeHUD";
        Layer = 20;
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
        UpdateFloatingText((float)delta);
    }

    private void BuildTree()
    {
        var root = new Control
        {
            Name = "HudRoot",
            AnchorRight = 1f,
            AnchorBottom = 1f,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        AddChild(root);

        playerHealthLabel = new Label
        {
            Name = "PlayerHealthLabel",
            Text = "HP 0/0",
            Position = new Vector2(16f, 14f)
        };
        root.AddChild(playerHealthLabel);

        progressionSummary = new Label
        {
            Name = "ProgressionSummary",
            Text = "Lv 1  XP 0/5  Wave 1",
            Position = new Vector2(16f, 40f)
        };
        root.AddChild(progressionSummary);

        activeSkillBar = new HBoxContainer
        {
            Name = "ActiveSkillBar",
            Position = new Vector2(16f, 72f),
            CustomMinimumSize = new Vector2(420f, 36f)
        };
        root.AddChild(activeSkillBar);

        for (var i = 0; i < 4; i++)
        {
            var slot = new Label
            {
                Name = $"SkillSlot{i}",
                Text = $"{i + 1}: -",
                CustomMinimumSize = new Vector2(100f, 30f)
            };
            slot.SetMeta("SlotIndex", i);
            slot.SetMeta("Selected", false);
            slot.SetMeta("CooldownRemaining", 0f);
            activeSkillBar.AddChild(slot);
            skillSlots.Add(slot);
        }

        headHealthBarLayer = new Control
        {
            Name = "HeadHealthBarLayer",
            AnchorRight = 1f,
            AnchorBottom = 1f,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        root.AddChild(headHealthBarLayer);

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
        playerHealthLabel.Text = FormattableString.Invariant($"HP {currentHp:0}/{maxHp:0}");
        playerHealthLabel.SetMeta("CurrentHp", currentHp);
        playerHealthLabel.SetMeta("MaxHp", maxHp);

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
        var player = runtime?.PlayerEntity;
        if (player == null)
        {
            return;
        }

        var ownedIds = player.Data.Get<EntityIdList>(AbilityDataKeys.OwnedAbilityIds);
        var selectedIndex = player.Data.Get<int>(AbilityDataKeys.CurrentAbilityIndex, 0);
        for (var i = 0; i < skillSlots.Count; i++)
        {
            var slot = skillSlots[i];
            if (i >= ownedIds.Count)
            {
                slot.Text = $"{i + 1}: -";
                slot.SetMeta("Selected", false);
                slot.SetMeta("CooldownRemaining", 0f);
                slot.SetMeta("AbilityId", string.Empty);
                continue;
            }

            var ability = EntityManager.Get(ownedIds[i]);
            if (ability == null)
            {
                slot.Text = $"{i + 1}: missing";
                slot.SetMeta("Selected", i == selectedIndex);
                slot.SetMeta("CooldownRemaining", 0f);
                slot.SetMeta("AbilityId", ownedIds[i].Value);
                continue;
            }

            var name = ability.Data.Get(AbilityDataKeys.Name, ability.EntityId.Value);
            if (string.IsNullOrWhiteSpace(name))
            {
                name = ability.EntityId.Value;
            }

            var cooldown = MathF.Max(0f, ability.Data.Get<float>(AbilityDataKeys.CooldownRemaining, 0f));
            var charges = ability.Data.Get<int>(AbilityDataKeys.CurrentCharges, 0);
            var maxCharges = ability.Data.Get<int>(AbilityDataKeys.MaxCharges, 0);
            var selected = i == selectedIndex;
            var prefix = selected ? ">" : " ";
            var cooldownText = cooldown > 0f ? $" cd {cooldown:0.0}" : " ready";
            var chargeText = maxCharges > 0 ? $" {charges}/{maxCharges}" : string.Empty;
            slot.Text = $"{prefix}{i + 1}: {name}{cooldownText}{chargeText}";
            slot.SetMeta("Selected", selected);
            slot.SetMeta("CooldownRemaining", cooldown);
            slot.SetMeta("AbilityId", ability.EntityId.Value);
            slot.SetMeta("AbilityName", name);
        }

        activeSkillBar?.SetMeta("SelectedIndex", selectedIndex);
        activeSkillBar?.SetMeta("SlotCount", Math.Min(4, ownedIds.Count));
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
            bar.MaxValue = maxHp;
            bar.Value = Mathf.Clamp(currentHp, 0f, maxHp);
            bar.Position = node.GlobalPosition + new Vector2(-32f, -MathF.Max(24f, node.Data.Get<float>(UnitDataKeys.HealthBarHeight, 48f) * 0.25f));
            bar.SetMeta("EntityId", node.EntityId.Value);
            bar.SetMeta("CurrentHp", currentHp);
            bar.SetMeta("MaxHp", maxHp);
            bar.SetMeta("WorldPosition", $"{node.GlobalPosition.X:0.###},{node.GlobalPosition.Y:0.###}");

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

    private ProgressBar EnsureHeadHealthBar(GodotEntity2D enemy)
    {
        if (headHealthBars.TryGetValue(enemy.EntityId, out var existing)
            && GodotObject.IsInstanceValid(existing))
        {
            return existing;
        }

        var bar = new ProgressBar
        {
            Name = $"HeadHealthBar_{enemy.EntityId.Value}",
            MinValue = 0,
            MaxValue = 1,
            Value = 1,
            CustomMinimumSize = new Vector2(64f, 8f),
            ShowPercentage = false,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
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
        SpawnFloatingNumber(entity, delta);
    }

    private void SpawnFloatingNumber(IEntity entity, float hpDelta)
    {
        if (damageNumberLayer == null || Math.Abs(hpDelta) < 0.001f)
        {
            return;
        }

        var isHeal = hpDelta > 0f;
        var team = entity.Data.Get<int>(CollisionDataKeys.Team, 0);
        var stableName = isHeal && team == 1 ? "HealNumber_Player" : team == 2 ? "DamageNumber_Enemy" : "DamageNumber_Player";
        if (damageNumberLayer.GetNodeOrNull<Label>(stableName) != null)
        {
            stableName = $"{stableName}_{floatingTextLife.Count + 1}";
        }

        var position = entity.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
        var label = new Label
        {
            Name = stableName,
            Text = isHeal ? $"+{hpDelta:0}" : $"{hpDelta:0}",
            Position = new Vector2(position.X, position.Y - 36f)
        };
        label.SetMeta("Value", hpDelta);
        label.SetMeta("DamageType", isHeal ? "Heal" : "Damage");
        label.SetMeta("WorldPosition", $"{position.X:0.###},{position.Y:0.###}");
        damageNumberLayer.AddChild(label);
        floatingTextLife[label] = 0.8f;
    }

    private void UpdateFloatingText(float deltaSeconds)
    {
        if (floatingTextLife.Count == 0)
        {
            return;
        }

        var expired = new List<Label>();
        var updates = new List<(Label Label, float Remaining)>();
        foreach (var entry in floatingTextLife)
        {
            var label = entry.Key;
            if (!GodotObject.IsInstanceValid(label))
            {
                expired.Add(label);
                continue;
            }

            var remaining = entry.Value - deltaSeconds;
            label.Position += new Vector2(0f, -18f * deltaSeconds);
            label.Modulate = new Color(label.Modulate.R, label.Modulate.G, label.Modulate.B, Mathf.Clamp(remaining / 0.8f, 0f, 1f));
            if (remaining <= 0f)
            {
                expired.Add(label);
            }
            else
            {
                updates.Add((label, remaining));
            }
        }

        for (var i = 0; i < updates.Count; i++)
        {
            floatingTextLife[updates[i].Label] = updates[i].Remaining;
        }

        for (var i = 0; i < expired.Count; i++)
        {
            floatingTextLife.Remove(expired[i]);
            if (GodotObject.IsInstanceValid(expired[i]))
            {
                expired[i].QueueFree();
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
}
