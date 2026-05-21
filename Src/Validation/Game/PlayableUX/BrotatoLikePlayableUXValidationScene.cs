using BrotatoLike.Game.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BrotatoLike.Game;
using Godot;
using SlimeAI.GameOS.Capabilities.Ability;
using SlimeAI.GameOS.Capabilities.Collision;
using SlimeAI.GameOS.Capabilities.Damage;
using SlimeAI.GameOS.Capabilities.Movement;
using SlimeAI.GameOS.Capabilities.Unit;
using SlimeAI.GameOS.GodotBridge;
using SlimeAI.GameOS.Observation;
using SlimeAI.GameOS.Runtime.Entity;

namespace BrotatoLike.Validation.Game.PlayableUX;

/// <summary>
/// BrotatoLike 正式可玩 UX 的 Godot headless 验证场景。
/// </summary>
public partial class BrotatoLikePlayableUXValidationScene : Node
{
    private const string ScenePath = "res://Src/Validation/Game/PlayableUX/BrotatoLikePlayableUXValidation.tscn";
    private const string ArtifactFileName = "brotatolike-playable-ux-validation.json";
    private const string PassMarker = "BrotatoLike Playable UX validation PASS";
    private const string FailMarker = "BrotatoLike Playable UX validation FAIL";

    /// <inheritdoc />
    public override async void _Ready()
    {
        EntityManager.Clear();
        ReleaseValidationActions();
        BrotatoLikeAbilityHandlers.RegisterAll();

        using var observation = GameOSObservationSession.FromEnvironment(
            ScenePath,
            "validation",
            Path.Combine(Directory.GetCurrentDirectory(), ".ai-temp", "scene-tests", "manual", "artifacts"));
        using var validation = new SceneValidationSession(
            observation,
            "BrotatoLikePlayableUXValidation",
            "Game/PlayableUX",
            ArtifactFileName,
            dependencies: new[]
            {
                "BrotatoLike.Game.BrotatoLikeGameRuntime",
                "BrotatoLike.Game.UI",
                "BrotatoLike.Game.GodotActiveSkillInputComponent",
                "SlimeAI.GameOS.Capabilities.Ability",
                "SlimeAI.GameOS.Capabilities.Damage"
            },
            notes: new[]
            {
                "Validation does not create formal UX nodes; production gameplay must mount them.",
                "Skill input is pressed through Godot Input actions instead of direct event publishing."
            },
            expectedInputs: new[]
            {
                "BrotatoLikeGameRuntime initialized from DataOS snapshot",
                "DataOS player and DataOS-spawned enemy entities",
                "Godot input actions MoveRight, NextSkill, PreviousSkill, SkillSlot1, SkillSlot4 and UseSkill"
            },
            expectedObservations: new[]
            {
                "formal HUD host exposes player HP, skill slots, selection, cooldown and progression nodes",
                "skill bar exposes loadout source, visible active slots, selected ability id, owned ids and total count",
                "validation override loadout can own more than four abilities while exposing only four visible active slots",
                "direct numeric skill slot input selects visible active skill slots",
                "enemy head health bars update, clean up and match camera-aware canvas coordinates from runtime HP/death state",
                "real input actions drive skill UX, point targeting, camera-aware damage/heal numbers and visible movement",
                "formal UI nodes (HUD root, skill slots, head health bars, damage numbers, targeting indicator) have non-empty SceneFilePath"
            },
            passCriteria: new[]
            {
                $"stdout contains {PassMarker}",
                "artifact status is pass",
                "failureReasons is empty and standard-answer fields are non-empty"
            },
            failCriteria: new[]
            {
                $"stdout contains {FailMarker}",
                "formal HUD, health bar, skill bar, point targeting, damage number or visibility evidence is missing",
                "artifact status is fail with feature-level failureReasons"
            });

        validation.Info("validation start");
        var values = await RunPlayableUxProbe();
        validation.Check("formal_hud_host_mounted", "HUD", () => Result(values, "formal_hud_host_mounted"));
        validation.Check("player_hp_ui_updates", "HUD", () => Result(values, "player_hp_ui_updates"));
        validation.Check("enemy_head_health_bar_updates_and_cleans", "HealthBar", () => Result(values, "enemy_head_health_bar_updates_and_cleans"));
        validation.Check("enemy_head_health_bar_canvas_coordinates", "HealthBar", () => Result(values, "enemy_head_health_bar_canvas_coordinates"));
        validation.Check("skill_bar_action_input_updates", "SkillBar", () => Result(values, "skill_bar_action_input_updates"));
        validation.Check("skill_bar_direct_slot_input_updates", "SkillBar", () => Result(values, "skill_bar_direct_slot_input_updates"));
        validation.Check("validation_loadout_override_visible_slots", "SkillBar", () => Result(values, "validation_loadout_override_visible_slots"));
        validation.Check("point_targeting_indicator_session", "Targeting", () => Result(values, "point_targeting_indicator_session"));
        validation.Check("damage_and_heal_numbers_lifecycle", "CombatFeedback", () => Result(values, "damage_and_heal_numbers_lifecycle"));
        validation.Check("damage_and_heal_numbers_canvas_coordinates", "CombatFeedback", () => Result(values, "damage_and_heal_numbers_canvas_coordinates"));
        validation.Check("camera_player_visibility", "Visibility", () => Result(values, "camera_player_visibility"));
        validation.Check("scene_backed_formal_ui", "SceneBacked", () => Result(values, "scene_backed_formal_ui"));

        var success = validation.Success;
        if (success)
        {
            validation.Pass("all checks passed");
        }
        else
        {
            validation.Fail($"{validation.FailureReasons.Count} checks failed");
        }

        ReleaseValidationActions();
        EntityManager.Clear();
        validation.WriteArtifact();

        GD.Print(success ? PassMarker : FailMarker);
        if (!success)
        {
            GD.Print($"BrotatoLike Playable UX failures: {string.Join("; ", validation.FailureReasons)}");
        }

        GetTree().Quit(success ? 0 : 1);
    }

    private async Task<Dictionary<string, object?>> RunPlayableUxProbe()
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        var runtime = new BrotatoLikeGameRuntime
        {
            Name = "GameRuntime",
            AutoInitialize = false,
            AutoTick = true
        };
        AddChild(runtime);
        runtime.InitializeFromDataOS(1, runtime);
        runtime.BeginGameplay();
        var player = runtime.SpawnPlayer("deluyi", Vector2.Zero);
        await ProcessFrames(20);
        runtime.ProgressionService?.SetProcess(false);

        var enemy = FindFirstEnemy();
        values["player_entity"] = player.EntityId.Value;
        values["enemy_entity"] = enemy?.EntityId.Value ?? string.Empty;

        Input.ActionPress("MoveRight");
        await ProcessFrames(10);
        Input.ActionRelease("MoveRight");
        await ProcessFrames(2);

        var camera = new Camera2D
        {
            Name = "ValidationCamera",
            Position = new Vector2(240f, 120f),
            Enabled = true
        };
        AddChild(camera);
        camera.MakeCurrent();
        await ProcessFrames(2);
        values["camera_position"] = Format(camera.GlobalPosition);
        values["viewport_canvas_transform"] = Format(GetViewport().GetCanvasTransform());

        var hud = FindDescendant(this, "BrotatoLikeHUD");
        var playerHpText = FindDescendant(this, "PlayerHealthLabel") as Label;
        var playerHealthBar = FindDescendant(this, "PlayerHealthBar");
        var skillBar = FindDescendant(this, "ActiveSkillBar");
        var targetIndicator = FindDescendant(this, "PointTargetingIndicator") as CanvasItem;
        var damageNumberLayer = FindDescendant(this, "DamageNumberLayer");
        values["hud_found"] = hud != null;
        values["player_health_label_found"] = playerHpText != null;
        values["player_health_bar_found"] = playerHealthBar != null;
        values["skill_bar_found"] = skillBar != null;
        values["point_indicator_found"] = targetIndicator != null;
        values["damage_number_layer_found"] = damageNumberLayer != null;

        values["scene_backed_hud"] = IsSceneBacked(hud);
        values["scene_backed_player_hp"] = IsSceneBacked(playerHpText);
        values["scene_backed_progression_summary"] = IsSceneBacked(FindDescendant(this, "ProgressionSummary"));
        values["scene_backed_skill_bar"] = IsSceneBacked(FindDescendant(this, "ActiveSkillBar"));
        values["scene_backed_indicator"] = IsSceneBacked(targetIndicator);

        var hpBefore = player.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        player.Data.Set(DamageDataKeys.CurrentHp, Math.Max(0f, hpBefore - 7f));
        await ProcessFrames(2);
        var hpAfterDamage = player.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var hpTextAfterDamage = playerHpText?.Text ?? string.Empty;
        var hpLabelMetaAfterDamage = ReadFloatMeta(playerHpText, "CurrentHp");
        var playerHealthBarKind = ReadStringMeta(playerHealthBar, "HealthBarKind");
        var playerDamageNumber = FindDamageNumberByText(this, "-7")
            ?? FindDescendantByNamePrefix(this, "DamageNumber_");
        var playerDamageNumberPosition = ReadControlPosition(playerDamageNumber);
        var playerDamageWorldPosition = new Vector2(player.GlobalPosition.X, player.GlobalPosition.Y - 36f);
        var playerDamageExpectedCanvasPosition = WorldToCanvasPosition(playerDamageWorldPosition);
        var playerDamageNumberDistance = Distance(playerDamageNumberPosition, playerDamageExpectedCanvasPosition);
        var playerDamageNumberLabel = FindDescendantOrNull(playerDamageNumber, "DamageLabel") as Label;
        var hpAfterHeal = Math.Min(hpBefore, hpAfterDamage + 4f);
        player.Data.Set(DamageDataKeys.CurrentHp, hpAfterHeal);
        await ProcessFrames(2);
        var playerHealNumber = FindDamageNumberByText(this, "+4")
            ?? FindDescendantByNamePrefix(this, "HealNumber_");
        var playerHealNumberPosition = ReadControlPosition(playerHealNumber);
        var playerHealWorldPosition = new Vector2(player.GlobalPosition.X, player.GlobalPosition.Y - 36f);
        var playerHealExpectedCanvasPosition = WorldToCanvasPosition(playerHealWorldPosition);
        var playerHealNumberDistance = Distance(playerHealNumberPosition, playerHealExpectedCanvasPosition);
        var playerHealNumberLabel = FindDescendantOrNull(playerHealNumber, "DamageLabel") as Label;
        var playerDamageNumberText = playerDamageNumberLabel?.Text ?? string.Empty;
        var playerHealNumberText = playerHealNumberLabel?.Text ?? string.Empty;
        values["player_hp_before"] = hpBefore;
        values["player_hp_after"] = hpAfterDamage;
        values["player_hp_after_heal"] = hpAfterHeal;
        values["player_hp_label_text"] = hpTextAfterDamage;
        values["player_hp_label_meta"] = hpLabelMetaAfterDamage;
        values["player_health_bar_kind"] = playerHealthBarKind;
        values["damage_number_found"] = playerDamageNumber != null;
        values["damage_number_text"] = playerDamageNumberText;
        values["scene_backed_damage_number"] = IsSceneBacked(playerDamageNumber);
        values["damage_number_world_position"] = Format(playerDamageWorldPosition);
        values["damage_number_actual_canvas_position"] = Format(playerDamageNumberPosition);
        values["damage_number_expected_canvas_position"] = Format(playerDamageExpectedCanvasPosition);
        values["damage_number_canvas_distance"] = playerDamageNumberDistance;
        values["heal_number_found"] = playerHealNumber != null;
        values["heal_number_text"] = playerHealNumberText;
        values["heal_number_world_position"] = Format(playerHealWorldPosition);
        values["heal_number_actual_canvas_position"] = Format(playerHealNumberPosition);
        values["heal_number_expected_canvas_position"] = Format(playerHealExpectedCanvasPosition);
        values["heal_number_canvas_distance"] = playerHealNumberDistance;
        await ProcessFrames(90);
        values["damage_number_pool_idle_after_lifetime"] = ReadIntMeta(damageNumberLayer, "PoolIdleCount");
        values["damage_number_pool_active_after_lifetime"] = ReadIntMeta(damageNumberLayer, "PoolActiveCount");

        var enemyBar = enemy == null ? null : FindDescendant(this, $"HeadHealthBar_{enemy.EntityId.Value}");
        var enemyHpBefore = enemy?.Data.Get<float>(DamageDataKeys.CurrentHp, 0f) ?? 0f;
        values["scene_backed_head_health_bar"] = IsSceneBacked(enemyBar);
        var enemyWorldPosition = enemy?.GlobalPosition ?? Vector2.Zero;
        var enemyHealthBarHeight = enemy?.Data.Get<float>(UnitDataKeys.HealthBarHeight, 0f) ?? 0f;
        var enemyHeadWorldPosition = enemyWorldPosition + new Vector2(0f, -ResolveHeadHealthBarHeight(enemyHealthBarHeight));
        var enemyExpectedBarCanvasPosition = WorldToCanvasPosition(enemyHeadWorldPosition);
        if (enemy != null)
        {
            enemy.Data.Set(DamageDataKeys.CurrentHp, Math.Max(0f, enemyHpBefore - 5f));
            await ProcessFrames(2);
            enemyWorldPosition = enemy.GlobalPosition;
            enemyHeadWorldPosition = enemyWorldPosition + new Vector2(0f, -ResolveHeadHealthBarHeight(enemyHealthBarHeight));
            enemyExpectedBarCanvasPosition = WorldToCanvasPosition(enemyHeadWorldPosition);
        }

        var enemyHpAfterDamage = enemy?.Data.Get<float>(DamageDataKeys.CurrentHp, 0f) ?? 0f;
        var enemyBarAfterDamage = enemy == null ? null : FindDescendant(this, $"HeadHealthBar_{enemy.EntityId.Value}");
        var enemyHealthBarKind = ReadStringMeta(enemyBarAfterDamage, "HealthBarKind");
        var enemyBarValueAfterDamage = enemyBarAfterDamage is HealthBarUI healthBarUI
            && healthBarUI.GetNodeOrNull<ProgressBar>("HealthBar") is ProgressBar bar
            ? bar.Value
            : -1d;
        var enemyBarPositionAfterDamage = ReadControlPosition(enemyBarAfterDamage);
        var enemyBarCanvasDistance = Distance(enemyBarPositionAfterDamage, enemyExpectedBarCanvasPosition);
        RecordUnitHealthBarEvidence(this, values, "yuren");
        RecordUnitHealthBarEvidence(this, values, "chailangren");
        if (enemy != null)
        {
            enemy.Data.Set(DamageDataKeys.CurrentHp, 0f);
            enemy.Data.Set(DamageDataKeys.IsDead, true);
            enemy.DestroyEntity();
            await ProcessFrames(5);
        }

        values["enemy_head_bar_found"] = enemyBar != null;
        values["enemy_hp_before"] = enemyHpBefore;
        values["enemy_hp_after_damage"] = enemyHpAfterDamage;
        values["enemy_bar_value_after_damage"] = enemyBarValueAfterDamage;
        values["enemy_health_bar_kind"] = enemyHealthBarKind;
        values["enemy_world_position"] = Format(enemyWorldPosition);
        values["enemy_health_bar_height"] = enemyHealthBarHeight;
        values["enemy_head_world_position"] = Format(enemyHeadWorldPosition);
        values["enemy_bar_actual_canvas_position"] = Format(enemyBarPositionAfterDamage);
        values["enemy_bar_expected_canvas_position"] = Format(enemyExpectedBarCanvasPosition);
        values["enemy_bar_canvas_distance"] = enemyBarCanvasDistance;
        var headHealthBarLayer = FindDescendant(this, "HeadHealthBarLayer");
        var cleanedEnemyBar = enemy == null ? null : FindDescendant(this, $"HeadHealthBar_{enemy.EntityId.Value}");
        values["enemy_bar_cleanup_done"] = enemy == null
            || cleanedEnemyBar == null
            || cleanedEnemyBar is CanvasItem { Visible: false };
        values["head_health_bar_pool_idle_after_cleanup"] = ReadIntMeta(headHealthBarLayer, "PoolIdleCount");
        values["head_health_bar_pool_active_after_cleanup"] = ReadIntMeta(headHealthBarLayer, "PoolActiveCount");

        var ownedIds = player.Data.Get<EntityIdList>(AbilityDataKeys.OwnedAbilityIds);
        var oldIndex = player.Data.Get<int>(AbilityDataKeys.CurrentAbilityIndex, 0);
        Input.ActionPress("NextSkill");
        await ProcessFrames(2);
        Input.ActionRelease("NextSkill");
        await ProcessFrames(2);
        var nextIndex = player.Data.Get<int>(AbilityDataKeys.CurrentAbilityIndex, 0);
        Input.ActionPress("PreviousSkill");
        await ProcessFrames(2);
        Input.ActionRelease("PreviousSkill");
        await ProcessFrames(2);
        var previousIndex = player.Data.Get<int>(AbilityDataKeys.CurrentAbilityIndex, 0);
        Input.ActionPress("SkillSlot4");
        await ProcessFrames(2);
        Input.ActionRelease("SkillSlot4");
        await ProcessFrames(2);
        var directSlot4Index = player.Data.Get<int>(AbilityDataKeys.CurrentAbilityIndex, 0);
        Input.ActionPress("SkillSlot1");
        await ProcessFrames(2);
        Input.ActionRelease("SkillSlot1");
        await ProcessFrames(2);
        var directSlot1Index = player.Data.Get<int>(AbilityDataKeys.CurrentAbilityIndex, 0);
        values["skill_owned_count"] = ownedIds.Count;
        values["skill_old_index"] = oldIndex;
        values["skill_next_index"] = nextIndex;
        values["skill_previous_index"] = previousIndex;
        values["skill_direct_slot4_index"] = directSlot4Index;
        values["skill_direct_slot1_index"] = directSlot1Index;
        values["skill_loadout_source"] = ReadStringMeta(skillBar, "LoadoutSource");
        values["skill_owned_ids"] = ReadStringMeta(skillBar, "OwnedAbilityIds");
        values["skill_visible_slot_ids"] = ReadStringMeta(skillBar, "VisibleSlotIds");
        values["skill_selected_id"] = ReadStringMeta(skillBar, "SelectedAbilityId");
        values["skill_total_owned_count"] = ReadIntMeta(skillBar, "TotalOwnedCount");
        values["skill_visible_slot_count"] = ReadIntMeta(skillBar, "VisibleSlotCount");
        values["skill_hidden_owned_count"] = ReadIntMeta(skillBar, "HiddenOwnedCount");
        values["skill_available_pool_ids"] = player.HasMeta(BrotatoLikeSkillLoadoutAuthoring.AvailableSkillPoolIdsMeta)
            ? player.GetMeta(BrotatoLikeSkillLoadoutAuthoring.AvailableSkillPoolIdsMeta).AsString()
            : string.Empty;

        var selectedAbility = ownedIds.Count > 0 ? EntityManager.Get(ownedIds[Mathf.Clamp(directSlot1Index, 0, ownedIds.Count - 1)]) : null;
        var cooldownBeforeUse = selectedAbility?.Data.Get<float>(AbilityDataKeys.CooldownRemaining, 0f) ?? 0f;
        Input.ActionPress("UseSkill");
        await ProcessFrames(2);
        Input.ActionRelease("UseSkill");
        await ProcessFrames(2);
        var cooldownAfterUse = selectedAbility?.Data.Get<float>(AbilityDataKeys.CooldownRemaining, 0f) ?? 0f;
        values["skill_cooldown_before_use"] = cooldownBeforeUse;
        values["skill_cooldown_after_use"] = cooldownAfterUse;

        var pointAbilityIndex = FindAbilityIndexByRecordFragment(ownedIds, "target_point_skill");
        var pointAbility = pointAbilityIndex >= 0 ? EntityManager.Get(ownedIds[pointAbilityIndex]) : null;
        var pointCooldownBeforeStart = pointAbility?.Data.Get<float>(AbilityDataKeys.CooldownRemaining, 0f) ?? -1f;
        if (pointAbilityIndex >= 0)
        {
            AbilityService.Instance.TickCooldowns([pointAbility!], pointCooldownBeforeStart + 0.1f);
            player.Data.Set(AbilityDataKeys.CurrentAbilityIndex, pointAbilityIndex);
        }

        var pointTargetEnemy = FindFirstEnemy();
        if (pointTargetEnemy != null)
        {
            var targetPosition = player.GlobalPosition + new Vector2(120f, 0f);
            pointTargetEnemy.GlobalPosition = targetPosition;
            pointTargetEnemy.Data.Set(MovementDataKeys.Position, new Vector2Value(targetPosition.X, targetPosition.Y));
        }

        var pointTarget = pointTargetEnemy?.GlobalPosition ?? player.GlobalPosition + new Vector2(160f, 0f);
        Input.ActionPress("UseSkill");
        await ProcessFrames(2);
        Input.ActionRelease("UseSkill");
        await ProcessFrames(2);
        runtime.TargetingController?.SetRequestedTargetPosition(pointTarget);
        await ProcessFrames(1);
        var targetingStarted = runtime.TargetingController?.IsTargeting == true;
        var cooldownAfterTargetingStart = pointAbility?.Data.Get<float>(AbilityDataKeys.CooldownRemaining, 0f) ?? -1f;
        var indicatorVisibleAfterStart = targetIndicator?.Visible ?? false;
        var sessionFoundAfterStart = FindDescendant(this, "PointTargetingSession") != null;
        var clampedTarget = runtime.TargetingController?.Indicator.GetMeta("ClampedTargetPosition").AsString() ?? string.Empty;
        var targetHpBeforeConfirm = pointTargetEnemy?.Data.Get<float>(DamageDataKeys.CurrentHp, 0f) ?? 0f;
        Input.ActionPress("ConfirmTarget");
        await ProcessFrames(2);
        Input.ActionRelease("ConfirmTarget");
        await ProcessFrames(2);
        var pointReport = runtime.TargetingController?.LastTriggerReport;
        var pointCooldownAfterConfirm = pointAbility?.Data.Get<float>(AbilityDataKeys.CooldownRemaining, 0f) ?? -1f;
        var targetHpAfterConfirm = pointTargetEnemy?.Data.Get<float>(DamageDataKeys.CurrentHp, 0f) ?? 0f;

        if (pointAbility != null)
        {
            AbilityService.Instance.TickCooldowns([pointAbility], pointCooldownAfterConfirm + 0.1f);
            player.Data.Set(AbilityDataKeys.CurrentAbilityIndex, pointAbilityIndex);
            Input.ActionPress("UseSkill");
            await ProcessFrames(2);
            Input.ActionRelease("UseSkill");
            await ProcessFrames(2);
            Input.ActionPress("CancelTarget");
            await ProcessFrames(2);
            Input.ActionRelease("CancelTarget");
            await ProcessFrames(2);
        }

        var cancelCleared = runtime.TargetingController?.IsTargeting == false
            && FindDescendant(this, "PointTargetingSession") == null
            && targetIndicator?.Visible == false;

        values["point_ability_index"] = pointAbilityIndex;
        values["point_target_enemy"] = pointTargetEnemy?.EntityId.Value ?? string.Empty;
        values["point_cooldown_before_start"] = pointCooldownBeforeStart;
        values["point_cooldown_after_start"] = cooldownAfterTargetingStart;
        values["point_targeting_started"] = targetingStarted;
        values["point_indicator_visible_after_start"] = indicatorVisibleAfterStart;
        values["point_session_found_after_start"] = sessionFoundAfterStart;
        values["point_clamped_target"] = clampedTarget;
        values["point_report"] = pointReport?.Result.ToString() ?? string.Empty;
        values["point_cooldown_after_confirm"] = pointCooldownAfterConfirm;
        values["point_target_hp_before_confirm"] = targetHpBeforeConfirm;
        values["point_target_hp_after_confirm"] = targetHpAfterConfirm;
        values["point_cancel_cleared"] = cancelCleared;

        values["formal_hud_host_mounted"] = hud != null
            && playerHpText != null
            && playerHealthBar != null
            && skillBar != null
            && FindDescendant(this, "ProgressionSummary") != null;
        values["player_hp_ui_updates"] = playerHpText != null
            && playerHealthBar != null
            && hpBefore > hpAfterDamage
            && hpLabelMetaAfterDamage < hpBefore
            && Mathf.RoundToInt(hpLabelMetaAfterDamage) == Mathf.RoundToInt(hpAfterDamage)
            && hpTextAfterDamage.Contains(Mathf.RoundToInt(hpAfterDamage).ToString(), StringComparison.Ordinal)
            && playerHealthBarKind == "Player";
        values["enemy_head_health_bar_updates_and_cleans"] = enemy != null
            && enemyBar != null
            && enemyBarAfterDamage != null
            && enemyHealthBarKind == "Enemy"
            && enemyHpAfterDamage < enemyHpBefore
            && Math.Abs(enemyBarValueAfterDamage - enemyHpAfterDamage) < 0.01d
            && ReadBoolValue(values, "enemy_bar_cleanup_done");
        values["skill_bar_action_input_updates"] = skillBar != null
            && ownedIds.Count >= 2
            && nextIndex != oldIndex
            && previousIndex == oldIndex
            && cooldownAfterUse > cooldownBeforeUse
            && ReadStringMeta(skillBar, "LoadoutSource") == BrotatoLikeSkillLoadoutAuthoring.SourceDefault
            && ReadIntMeta(skillBar, "TotalOwnedCount") == ownedIds.Count
            && ReadIntMeta(skillBar, "VisibleSlotCount") <= BrotatoLikeSkillLoadoutAuthoring.VisibleActiveSlotCapacity
            && !string.IsNullOrWhiteSpace(ReadStringMeta(skillBar, "VisibleSlotIds"))
            && !string.IsNullOrWhiteSpace(ReadStringMeta(skillBar, "SelectedAbilityId"));
        values["skill_bar_direct_slot_input_updates"] = skillBar != null
            && ownedIds.Count >= 4
            && directSlot4Index == 3
            && directSlot1Index == 0;
        values["point_targeting_indicator_session"] = targetIndicator != null
            && pointAbility != null
            && targetingStarted
            && indicatorVisibleAfterStart
            && sessionFoundAfterStart
            && Math.Abs(cooldownAfterTargetingStart) < 0.001f
            && pointReport?.Result == AbilityTriggerResult.Success
            && pointCooldownAfterConfirm > 0f
            && targetHpAfterConfirm < targetHpBeforeConfirm
            && cancelCleared;
        values["damage_and_heal_numbers_lifecycle"] = damageNumberLayer != null
            && ReadBoolValue(values, "damage_number_found")
            && ReadBoolValue(values, "heal_number_found")
            && ReadIntValue(values, "damage_number_pool_idle_after_lifetime") >= 2
            && ReadIntValue(values, "damage_number_pool_active_after_lifetime") == 0;
        values["damage_and_heal_numbers_canvas_coordinates"] = damageNumberLayer != null
            && playerDamageNumber != null
            && playerHealNumber != null
            && playerDamageNumberDistance <= 2f
            && playerHealNumberDistance <= 2f
            && playerDamageNumberLabel != null
            && playerHealNumberLabel != null
            && playerDamageNumberText.Contains("-7", StringComparison.Ordinal)
            && playerHealNumberText.Contains("+4", StringComparison.Ordinal)
            && ReadIntValue(values, "damage_number_pool_idle_after_lifetime") >= 2
            && ReadIntValue(values, "damage_number_pool_active_after_lifetime") == 0;
        values["camera_player_visibility"] = camera.Enabled
            && player.Position.DistanceTo(camera.Position) < 4096f
            && player.Position != Vector2.Zero;
        values["enemy_head_health_bar_canvas_coordinates"] = enemyBarAfterDamage != null
            && enemyBarCanvasDistance <= 4f
            && ReadBoolValue(values, "yuren_head_bar_found")
            && ReadBoolValue(values, "chailangren_head_bar_found")
            && ReadFloatValue(values, "yuren_health_bar_height") >= 90f
            && ReadFloatValue(values, "chailangren_health_bar_height") <= 130f;

        values["scene_backed_formal_ui"] = ReadSceneBacked(values, "scene_backed_skill_bar")
            && ReadSceneBacked(values, "scene_backed_head_health_bar")
            && ReadSceneBacked(values, "scene_backed_damage_number")
            && ReadSceneBacked(values, "scene_backed_indicator");

        await RecordValidationLoadoutProbe(runtime, values);

        return values;
    }

    private async Task RecordValidationLoadoutProbe(
        BrotatoLikeGameRuntime runtime,
        Dictionary<string, object?> values)
    {
        var validationPlayer = runtime.SpawnPlayerWithValidationLoadout(
            BrotatoLikeSkillLoadoutAuthoring.ValidationAllSkillAbilityIds,
            "deluyi",
            new Vector2(320f, 0f));
        await ProcessFrames(4);

        var skillBar = FindDescendant(this, "ActiveSkillBar");
        var ownedIds = validationPlayer.Data.Get<EntityIdList>(AbilityDataKeys.OwnedAbilityIds);
        var source = ReadStringMeta(skillBar, "LoadoutSource");
        var visibleSlotIds = ReadStringMeta(skillBar, "VisibleSlotIds");
        var selectedId = ReadStringMeta(skillBar, "SelectedAbilityId");
        var totalOwnedCount = ReadIntMeta(skillBar, "TotalOwnedCount");
        var visibleSlotCount = ReadIntMeta(skillBar, "VisibleSlotCount");
        var hiddenOwnedCount = ReadIntMeta(skillBar, "HiddenOwnedCount");

        values["validation_loadout_source"] = source;
        values["validation_loadout_owned_count"] = ownedIds.Count;
        values["validation_loadout_total_owned_count"] = totalOwnedCount;
        values["validation_loadout_visible_slot_count"] = visibleSlotCount;
        values["validation_loadout_hidden_owned_count"] = hiddenOwnedCount;
        values["validation_loadout_visible_slot_ids"] = visibleSlotIds;
        values["validation_loadout_selected_id"] = selectedId;
        values["validation_loadout_available_pool_ids"] = validationPlayer.HasMeta(BrotatoLikeSkillLoadoutAuthoring.AvailableSkillPoolIdsMeta)
            ? validationPlayer.GetMeta(BrotatoLikeSkillLoadoutAuthoring.AvailableSkillPoolIdsMeta).AsString()
            : string.Empty;
        values["validation_loadout_passive_ids"] = validationPlayer.HasMeta(BrotatoLikeSkillLoadoutAuthoring.PassiveSkillIdsMeta)
            ? validationPlayer.GetMeta(BrotatoLikeSkillLoadoutAuthoring.PassiveSkillIdsMeta).AsString()
            : string.Empty;
        values["validation_loadout_override_visible_slots"] = source == BrotatoLikeSkillLoadoutAuthoring.SourceValidationOverride
            && totalOwnedCount == BrotatoLikeSkillLoadoutAuthoring.ValidationAllSkillAbilityIds.Length
            && ownedIds.Count == totalOwnedCount
            && visibleSlotCount == BrotatoLikeSkillLoadoutAuthoring.VisibleActiveSlotCapacity
            && hiddenOwnedCount == totalOwnedCount - visibleSlotCount
            && !string.IsNullOrWhiteSpace(visibleSlotIds)
            && !string.IsNullOrWhiteSpace(selectedId);
    }

    private async Task ProcessFrames(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private static CheckResult Result(IReadOnlyDictionary<string, object?> values, string key)
    {
        var success = values.TryGetValue(key, out var raw) && raw is bool value && value;
        return CheckResult.From(success, success ? $"{key} passed" : $"{key} failed", values);
    }

    private static GodotEntity2D? FindFirstEnemy()
    {
        var entities = EntityManager.GetAll();
        for (var i = 0; i < entities.Count; i++)
        {
            if (entities[i] is GodotEntity2D node
                && node.EntityId.Value.StartsWith("spawn-", StringComparison.Ordinal)
                && node.Data.Get<int>(CollisionDataKeys.Team, 0) == 2
                && !node.IsQueuedForDeletion()
                && !node.Data.Get<bool>(DamageDataKeys.IsDead, false)
                && node.Data.Get<float>(DamageDataKeys.CurrentHp, 0f) > 0f)
            {
                return node;
            }
        }

        return null;
    }

    private static GodotEntity2D? FindEnemyByRuleId(string ruleId)
    {
        var entities = EntityManager.GetAll();
        for (var i = 0; i < entities.Count; i++)
        {
            if (entities[i] is GodotEntity2D node
                && node.Data.Get<int>(CollisionDataKeys.Team, 0) == 2
                && !node.IsQueuedForDeletion()
                && ReadStringMeta(node, "SpawnRuleId") == ruleId)
            {
                return node;
            }
        }

        return null;
    }

    private static void RecordUnitHealthBarEvidence(
        Node root,
        Dictionary<string, object?> values,
        string ruleId)
    {
        var enemy = FindEnemyByRuleId(ruleId);
        var bar = enemy == null ? null : FindDescendant(root, $"HeadHealthBar_{enemy.EntityId.Value}");
        values[$"{ruleId}_head_bar_found"] = bar != null;
        values[$"{ruleId}_health_bar_height"] = enemy?.Data.Get<float>(UnitDataKeys.HealthBarHeight, 0f) ?? 0f;
        values[$"{ruleId}_bar_canvas_position"] = ReadStringMeta(bar, "CanvasPosition");
        values[$"{ruleId}_bar_health_bar_height_meta"] = ReadFloatMeta(bar, "HealthBarHeight");
    }

    private static int FindAbilityIndexByRecordFragment(EntityIdList ownedIds, string fragment)
    {
        for (var i = 0; i < ownedIds.Count; i++)
        {
            if (ownedIds[i].Value.Contains(fragment, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private static float ReadFloatMeta(Node? node, string key)
    {
        if (node == null || !node.HasMeta(key))
        {
            return float.NaN;
        }

        var value = node.GetMeta(key);
        return value.VariantType switch
        {
            Variant.Type.Float => value.AsSingle(),
            Variant.Type.Int => value.AsInt32(),
            Variant.Type.String => float.TryParse(value.AsString(), out var parsed) ? parsed : float.NaN,
            _ => float.NaN
        };
    }

    private static string ReadStringMeta(Node? node, string key)
    {
        if (node == null || !node.HasMeta(key))
        {
            return string.Empty;
        }

        return node.GetMeta(key).AsString();
    }

    private static int ReadIntMeta(Node? node, string key)
    {
        if (node == null || !node.HasMeta(key))
        {
            return 0;
        }

        var value = node.GetMeta(key);
        return value.VariantType switch
        {
            Variant.Type.Int => value.AsInt32(),
            Variant.Type.Float => Mathf.RoundToInt(value.AsSingle()),
            Variant.Type.String => int.TryParse(value.AsString(), out var parsed) ? parsed : 0,
            _ => 0
        };
    }

    private static Node? FindDescendant(Node root, string name)
    {
        if (root.Name == name)
        {
            return root;
        }

        foreach (var child in root.GetChildren())
        {
            if (child.Name == name)
            {
                return child;
            }

            var descendant = FindDescendant(child, name);
            if (descendant != null)
            {
                return descendant;
            }
        }

        return null;
    }

    private static Node? FindDescendantOrNull(Node? root, string name)
    {
        return root == null ? null : FindDescendant(root, name);
    }

    private static Node? FindDescendantByNamePrefix(Node root, string prefix)
    {
        if (root.Name.ToString().StartsWith(prefix, StringComparison.Ordinal))
        {
            return root;
        }

        foreach (var child in root.GetChildren())
        {
            var found = FindDescendantByNamePrefix(child, prefix);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static Node? FindDamageNumberByText(Node root, string text)
    {
        if (root is DamageNumberUI damageNumber
            && FindDescendantOrNull(damageNumber, "DamageLabel") is Label label
            && label.Text == text)
        {
            return damageNumber;
        }

        foreach (var child in root.GetChildren())
        {
            var found = FindDamageNumberByText(child, text);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static string Format(Vector2 value)
    {
        return $"{value.X:0.###},{value.Y:0.###}";
    }

    private static string Format(Transform2D value)
    {
        return $"x=({value.X.X:0.###},{value.X.Y:0.###});y=({value.Y.X:0.###},{value.Y.Y:0.###});origin=({value.Origin.X:0.###},{value.Origin.Y:0.###})";
    }

    private Vector2 WorldToCanvasPosition(Vector2 worldPosition)
    {
        return GetViewport().GetCanvasTransform() * worldPosition;
    }

    private static Vector2 ReadControlPosition(Node? node)
    {
        return node is Control control ? control.Position : new Vector2(float.NaN, float.NaN);
    }

    private static float Distance(Vector2 actual, Vector2 expected)
    {
        if (float.IsNaN(actual.X) || float.IsNaN(actual.Y) || float.IsNaN(expected.X) || float.IsNaN(expected.Y))
        {
            return float.PositiveInfinity;
        }

        return actual.DistanceTo(expected);
    }

    private static float ResolveHeadHealthBarHeight(float configuredHeight)
    {
        return configuredHeight > 0f ? configuredHeight : 36f;
    }

    private static bool IsSceneBacked(Node? node)
    {
        return node != null && GodotObject.IsInstanceValid(node) && !string.IsNullOrEmpty(node.SceneFilePath);
    }

    private static bool ReadSceneBacked(IReadOnlyDictionary<string, object?> values, string key)
    {
        return values.TryGetValue(key, out var raw) && raw is bool value && value;
    }

    private static bool ReadBoolValue(IReadOnlyDictionary<string, object?> values, string key)
    {
        return values.TryGetValue(key, out var raw) && raw is bool value && value;
    }

    private static float ReadFloatValue(IReadOnlyDictionary<string, object?> values, string key)
    {
        return values.TryGetValue(key, out var raw) && raw is float value ? value : float.NaN;
    }

    private static int ReadIntValue(IReadOnlyDictionary<string, object?> values, string key)
    {
        return values.TryGetValue(key, out var raw) && raw is int value ? value : -1;
    }

    private static void ReleaseValidationActions()
    {
        Input.ActionRelease("MoveLeft");
        Input.ActionRelease("MoveRight");
        Input.ActionRelease("MoveUp");
        Input.ActionRelease("MoveDown");
        Input.ActionRelease("UseSkill");
        Input.ActionRelease("PreviousSkill");
        Input.ActionRelease("NextSkill");
        Input.ActionRelease("SkillSlot1");
        Input.ActionRelease("SkillSlot2");
        Input.ActionRelease("SkillSlot3");
        Input.ActionRelease("SkillSlot4");
    }
}
