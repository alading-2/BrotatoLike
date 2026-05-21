using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BrotatoLike.Game;
using BrotatoLike.Game.UI;
using Godot;
using SlimeAI.GameOS.Capabilities.Ability;
using SlimeAI.GameOS.Capabilities.Collision;
using SlimeAI.GameOS.Capabilities.Damage;
using SlimeAI.GameOS.Capabilities.Movement;
using SlimeAI.GameOS.Capabilities.Unit;
using SlimeAI.GameOS.GodotBridge;
using SlimeAI.GameOS.Observation;
using SlimeAI.GameOS.Runtime.Entity;
using SlimeAI.GameOS.Runtime.Schedule;

namespace BrotatoLike.Validation.Game.Progression;

/// <summary>
/// BrotatoLike 波次、暂停、恢复、拾取、经验和升级闭环的验证场景。
/// </summary>
public partial class BrotatoLikeProgressionLoopValidationScene : Node
{
    private const string ScenePath = "res://Src/Validation/Game/Progression/BrotatoLikeProgressionLoopValidation.tscn";
    private const string ArtifactFileName = "brotatolike-progression-loop-validation.json";
    private const string PassMarker = "BrotatoLike Progression Loop validation PASS";
    private const string FailMarker = "BrotatoLike Progression Loop validation FAIL";

    /// <inheritdoc />
    public override async void _Ready()
    {
        EntityManager.Clear();
        BrotatoLikeAbilityHandlers.RegisterAll();

        using var observation = GameOSObservationSession.FromEnvironment(
            ScenePath,
            "validation",
            Path.Combine(Directory.GetCurrentDirectory(), ".ai-temp", "scene-tests", "manual", "artifacts"));
        using var validation = new SceneValidationSession(
            observation,
            "BrotatoLikeProgressionLoopValidation",
            "Game/Progression",
            ArtifactFileName,
            dependencies: new[]
            {
                "BrotatoLike.Game.BrotatoLikeGameRuntime",
                "BrotatoLike.Game.Progression",
                "SlimeAI.GameOS.Runtime.Schedule",
                "SlimeAI.GameOS.Capabilities.Damage",
                "SlimeAI.GameOS.Capabilities.Unit"
            },
            notes: new[]
            {
                "Validation observes production progression nodes/state only.",
                "Level-up choice UI and experience UI must be scene-backed.",
                "Shop systems are out of scope for this validation."
            },
            expectedInputs: new[]
            {
                "BrotatoLikeGameRuntime initialized from DataOS snapshot",
                "DataOS player and enemy entities with Unit.ExpReward data",
                "runtime pause/resume calls, level-up choice selection and deterministic frame advancement"
            },
            expectedObservations: new[]
            {
                "wave runtime state records elapsed time, spawned count, remaining enemies and completion",
                "formal pause menu opens, blocks schedule-gated gameplay, and resumes",
                "HP recovery, pickup collection, experience gain, level-up choice UI and selected reward mutation are observable",
                "formal pause menu, experience bar, level-up feedback and choice panel nodes have non-empty SceneFilePath"
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
                "wave, pause, recovery, pickup, experience, level-up choice or formal experience UI evidence is missing",
                "artifact status is fail with feature-level failureReasons"
            });

        validation.Info("validation start");
        var values = await RunProgressionProbe();
        validation.Check("wave_completion_state", "Wave", () => Result(values, "wave_completion_state"));
        validation.Check("pause_menu_blocks_and_resumes_tick", "Pause", () => Result(values, "pause_menu_blocks_and_resumes_tick"));
        validation.Check("hp_recovery_tick_and_dead_skip", "Recovery", () => Result(values, "hp_recovery_tick_and_dead_skip"));
        validation.Check("enemy_death_spawns_experience_pickup", "Pickup", () => Result(values, "enemy_death_spawns_experience_pickup"));
        validation.Check("pickup_grants_experience_and_cleans", "Experience", () => Result(values, "pickup_grants_experience_and_cleans"));
        validation.Check("level_up_feedback", "LevelUp", () => Result(values, "level_up_feedback"));
        validation.Check("level_up_choice_scene_backed", "LevelUpChoice", () => Result(values, "level_up_choice_scene_backed"));
        validation.Check("level_up_choice_applies_stat_reward", "LevelUpChoice", () => Result(values, "level_up_choice_applies_stat_reward"));
        validation.Check("level_up_choice_applies_ability_reward", "LevelUpChoice", () => Result(values, "level_up_choice_applies_ability_reward"));
        validation.Check("level_up_choice_gate_blocks_and_resumes_tick", "LevelUpChoice", () => Result(values, "level_up_choice_gate_blocks_and_resumes_tick"));
        validation.Check("experience_ui_scene_backed_updates", "ExperienceUI", () => Result(values, "experience_ui_scene_backed_updates"));
        validation.Check("scene_backed_pause_menu", "SceneBacked", () => Result(values, "scene_backed_pause_menu"));

        var success = validation.Success;
        if (success)
        {
            validation.Pass("all checks passed");
        }
        else
        {
            validation.Fail($"{validation.FailureReasons.Count} checks failed");
        }

        EntityManager.Clear();
        validation.WriteArtifact();
        GD.Print(success ? PassMarker : FailMarker);
        if (!success)
        {
            GD.Print($"BrotatoLike Progression Loop failures: {string.Join("; ", validation.FailureReasons)}");
        }

        GetTree().Quit(success ? 0 : 1);
    }

    private async Task<Dictionary<string, object?>> RunProgressionProbe()
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
        await ProcessFrames(30);
        runtime.AutoTick = false;

        var enemy = FindFirstEnemy();
        var tickBeforePause = runtime.TickSpawn(0.1d);
        runtime.OpenPauseMenu();
        var tickDuringPause = runtime.TickSpawn(0.1d);
        await ProcessFrames(2);
        var pauseMenu = FindDescendant(this, "BrotatoLikePauseMenu") as PauseMenuUI;
        var pauseMenuVisibleDuringPause = pauseMenu?.IsMenuVisible ?? false;
        var pauseMenuSceneFilePath = pauseMenu?.SceneFilePath ?? string.Empty;
        runtime.ClosePauseMenu();
        await ProcessFrames(2);
        var pauseMenuVisibleAfterClose = pauseMenu?.IsMenuVisible ?? false;
        var tickAfterResume = runtime.TickSpawn(0.1d);

        values["player_entity"] = player.EntityId.Value;
        values["enemy_entity"] = enemy?.EntityId.Value ?? string.Empty;
        values["spawned_before_pause"] = tickBeforePause.Value.TotalSpawned;
        values["tick_during_pause_success"] = tickDuringPause.Success;
        values["tick_after_resume_success"] = tickAfterResume.Success;
        values["enemy_exp_reward"] = enemy?.Data.Get<int>(UnitDataKeys.ExpReward, 0) ?? 0;

        var waveState = FindDescendant(this, "WaveRuntimeState");
        var recoveryService = FindDescendant(this, "RecoveryTickService");
        var pickupLayer = FindDescendant(this, "ExperiencePickupLayer");
        var progressionSummary = FindDescendant(this, "ProgressionSummary");
        var levelUpFeedback = FindDescendant(this, "LevelUpFeedback");

        values["wave_state_found"] = waveState != null;
        values["pause_menu_found"] = pauseMenu != null;
        values["pause_menu_visible"] = pauseMenuVisibleDuringPause;
        values["pause_menu_visible_after_close"] = pauseMenuVisibleAfterClose;
        values["recovery_service_found"] = recoveryService != null;
        values["pickup_layer_found"] = pickupLayer != null;
        values["progression_summary_found"] = progressionSummary != null;
        values["level_up_feedback_found"] = levelUpFeedback != null;

        var hpBeforeRecovery = player.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        player.Data.Set(DamageDataKeys.CurrentHp, Math.Max(1f, hpBeforeRecovery - 20f));
        await ProcessFrames(20);
        var hpAfterRecovery = player.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        player.Data.Set(DamageDataKeys.IsDead, true);
        var deadHpBefore = player.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        await ProcessFrames(10);
        var deadHpAfter = player.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        player.Data.Set(DamageDataKeys.IsDead, false);

        values["hp_before_recovery"] = hpBeforeRecovery;
        values["hp_after_recovery"] = hpAfterRecovery;
        values["dead_hp_before"] = deadHpBefore;
        values["dead_hp_after"] = deadHpAfter;

        var enemyReward = enemy?.Data.Get<int>(UnitDataKeys.ExpReward, 0) ?? 0;
        var oldMaxHpBeforeChoice = player.Data.Get<float>(DamageDataKeys.MaxHp, 0f);
        var oldOwnedAbilityCount = player.Data.Get<EntityIdList>(AbilityDataKeys.OwnedAbilityIds).Count;
        if (enemy != null)
        {
            enemy.Data.Set(DamageDataKeys.CurrentHp, 0f);
            enemy.Data.Set(DamageDataKeys.IsDead, true);
            await ProcessFrames(2);
        }

        var pickup = FindExperiencePickup(pickupLayer);
        var oldExperience = ReadIntMeta(player, "Experience");
        var oldPlayerLevel = ReadIntMeta(player, "Level");
        if (pickup is Node2D pickupNode)
        {
            pickupNode.Position = player.Position;
        }

        await ProcessFrames(10);
        var newExperience = ReadIntMeta(player, "Experience");
        var level = ReadIntMeta(player, "Level");
        var oldLevel = ReadIntMeta(pickupLayer, "OldLevel");
        var newLevel = ReadIntMeta(pickupLayer, "NewLevel");
        var lastReward = ReadIntMeta(pickupLayer, "LastReward");
        var pickupCleaned = ReadBoolMeta(pickupLayer, "LastPickupCleaned")
            && FindExperiencePickup(pickupLayer) == null;

        await ProcessFrames(2);
        var choicePanel = FindDescendant(this, "LevelUpChoicePanel") as LevelUpChoicePanelUI;
        var experienceBar = FindDescendant(this, "ExperienceBarUI") as ExperienceBarUI;
        var choiceIds = ReadStringMeta(choicePanel, "ChoiceIds");
        var choiceEffectTypes = ReadStringMeta(choicePanel, "EffectTypes");
        var choicePanelSceneBacked = IsSceneBacked(choicePanel);
        var choicePanelVisibleBeforeSelection = choicePanel?.IsPanelVisible ?? false;
        var levelUpPendingBeforeSelection = runtime.ProgressionService?.IsLevelUpChoicePending ?? false;
        var gateSnapshotOpen = runtime.CurrentProjectState;
        var tickDuringLevelUpChoice = runtime.TickSpawn(0.1d);
        var choiceGateBlocked = gateSnapshotOpen.HasValue
            && gateSnapshotOpen.Value.Overlays.HasFlag(OverlayFlags.ModalUi)
            && gateSnapshotOpen.Value.SimulationState == SimulationState.Suspended
            && !tickDuringLevelUpChoice.Success;

        runtime.ProgressionService?.SelectLevelUpChoice("max_hp_plus_10");
        await ProcessFrames(3);
        var maxHpAfterStatChoice = player.Data.Get<float>(DamageDataKeys.MaxHp, 0f);
        var statChoiceApplied = maxHpAfterStatChoice > oldMaxHpBeforeChoice;
        var statChoicePanelClosed = choicePanel?.IsPanelVisible == false;
        var tickAfterStatChoice = runtime.TickSpawn(0.1d);

        var secondEnemy = FindFirstAliveEnemyExcept(enemy?.EntityId ?? EntityId.Empty);
        if (secondEnemy != null)
        {
            secondEnemy.Data.Set(DamageDataKeys.CurrentHp, 0f);
            secondEnemy.Data.Set(DamageDataKeys.IsDead, true);
            await ProcessFrames(2);
        }

        var secondPickup = FindExperiencePickup(pickupLayer);
        if (secondPickup is Node2D secondPickupNode)
        {
            secondPickupNode.SetMeta("Reward", Math.Max(10, ReadIntMeta(player, "NextLevelExperience")));
            secondPickupNode.Position = player.Position;
        }

        await ProcessFrames(10);
        var secondChoiceOpen = runtime.ProgressionService?.IsLevelUpChoicePending ?? false;
        var secondChoiceIds = ReadStringMeta(choicePanel, "ChoiceIds");
        runtime.ProgressionService?.SelectLevelUpChoice("unlock_sine_wave_shot");
        await ProcessFrames(3);
        var ownedAbilityCountAfterChoice = player.Data.Get<EntityIdList>(AbilityDataKeys.OwnedAbilityIds).Count;
        var sineWaveAbilityOwned = EntityManager.Get(new EntityId($"ability-sine_wave_shot-{player.EntityId.Value}")) != null;
        var abilityChoiceApplied = ownedAbilityCountAfterChoice > oldOwnedAbilityCount && sineWaveAbilityOwned;
        var gateSnapshotClosed = runtime.CurrentProjectState;
        var levelUpChoiceClosed = choicePanel?.IsPanelVisible == false
            && runtime.ProgressionService?.IsLevelUpChoicePending == false;

        DestroyRemainingEnemies();
        runtime.ProgressionService?.CompleteCurrentWaveForValidation();
        await ProcessFrames(5);
        var waveCompleted = ReadBoolMeta(waveState, "Completed");
        var remainingEnemies = ReadIntMeta(waveState, "RemainingEnemies");
        var manaRecoveryStatus = ReadStringMeta(recoveryService, "ManaRecoveryStatus");

        values["pickup_found"] = pickup != null;
        values["old_experience"] = oldExperience;
        values["new_experience"] = newExperience;
        values["old_player_level"] = oldPlayerLevel;
        values["old_level"] = oldLevel;
        values["new_level"] = newLevel;
        values["last_reward"] = lastReward;
        values["pickup_cleanup_done"] = pickupCleaned;
        values["player_level"] = level;
        values["choice_ids"] = choiceIds;
        values["choice_effect_types"] = choiceEffectTypes;
        values["second_choice_ids"] = secondChoiceIds;
        values["choice_panel_scene_path"] = choicePanel?.SceneFilePath ?? string.Empty;
        values["choice_panel_visible_before_selection"] = choicePanelVisibleBeforeSelection;
        values["choice_pending_before_selection"] = levelUpPendingBeforeSelection;
        values["tick_during_level_up_choice_success"] = tickDuringLevelUpChoice.Success;
        values["tick_after_stat_choice_success"] = tickAfterStatChoice.Success;
        values["level_up_gate_open_overlays"] = gateSnapshotOpen?.Overlays.ToString() ?? string.Empty;
        values["level_up_gate_open_simulation"] = gateSnapshotOpen?.SimulationState.ToString() ?? string.Empty;
        values["level_up_gate_closed_overlays"] = gateSnapshotClosed?.Overlays.ToString() ?? string.Empty;
        values["level_up_gate_closed_simulation"] = gateSnapshotClosed?.SimulationState.ToString() ?? string.Empty;
        values["max_hp_before_choice"] = oldMaxHpBeforeChoice;
        values["max_hp_after_stat_choice"] = maxHpAfterStatChoice;
        values["owned_ability_count_before_choice"] = oldOwnedAbilityCount;
        values["owned_ability_count_after_choice"] = ownedAbilityCountAfterChoice;
        values["sine_wave_ability_owned"] = sineWaveAbilityOwned;
        values["experience_bar_scene_path"] = experienceBar?.SceneFilePath ?? string.Empty;
        values["experience_bar_level"] = ReadIntMeta(experienceBar, "Level");
        values["experience_bar_experience"] = ReadIntMeta(experienceBar, "Experience");
        values["experience_bar_next"] = ReadIntMeta(experienceBar, "NextLevelExperience");
        values["experience_bar_progress_fraction"] = ReadFloatMeta(experienceBar, "ProgressFraction");
        values["wave_completed_meta"] = waveCompleted;
        values["wave_remaining_enemies"] = remainingEnemies;
        values["mana_recovery_status"] = manaRecoveryStatus;

        values["wave_completion_state"] = waveState != null
            && waveCompleted
            && remainingEnemies == 0;
        values["pause_menu_blocks_and_resumes_tick"] = pauseMenu != null
            && pauseMenuVisibleDuringPause
            && !pauseMenuVisibleAfterClose
            && tickBeforePause.Success
            && !tickDuringPause.Success
            && tickAfterResume.Success;
        values["hp_recovery_tick_and_dead_skip"] = recoveryService != null
            && hpAfterRecovery > hpBeforeRecovery - 20f
            && Math.Abs(deadHpAfter - deadHpBefore) < 0.001f
            && manaRecoveryStatus == "not-applicable";
        values["enemy_death_spawns_experience_pickup"] = enemy != null
            && enemyReward > 0
            && pickup != null;
        values["pickup_grants_experience_and_cleans"] = pickupLayer != null
            && lastReward > 0
            && (newExperience > oldExperience || level > oldPlayerLevel || newLevel > oldLevel)
            && pickupCleaned;
        values["level_up_feedback"] = progressionSummary != null
            && levelUpFeedback != null
            && level > oldPlayerLevel
            && IsSceneBacked(levelUpFeedback);
        values["level_up_choice_scene_backed"] = choicePanelSceneBacked
            && choicePanelVisibleBeforeSelection
            && choiceIds.Contains("max_hp_plus_10", StringComparison.Ordinal)
            && choiceIds.Contains("unlock_sine_wave_shot", StringComparison.Ordinal)
            && choiceEffectTypes.Contains("AddMaxHp", StringComparison.Ordinal)
            && choiceEffectTypes.Contains("GrantAbility", StringComparison.Ordinal);
        values["level_up_choice_applies_stat_reward"] = statChoiceApplied
            && statChoicePanelClosed;
        values["level_up_choice_applies_ability_reward"] = secondChoiceOpen
            && secondChoiceIds.Contains("unlock_sine_wave_shot", StringComparison.Ordinal)
            && abilityChoiceApplied
            && levelUpChoiceClosed;
        values["level_up_choice_gate_blocks_and_resumes_tick"] = choiceGateBlocked
            && tickAfterStatChoice.Success
            && gateSnapshotClosed.HasValue
            && !gateSnapshotClosed.Value.Overlays.HasFlag(OverlayFlags.ModalUi)
            && gateSnapshotClosed.Value.SimulationState == SimulationState.Running;
        values["experience_ui_scene_backed_updates"] = IsSceneBacked(experienceBar)
            && ReadIntMeta(experienceBar, "Level") >= 2
            && ReadIntMeta(experienceBar, "NextLevelExperience") > 0
            && ReadFloatMeta(experienceBar, "ProgressFraction") >= 0f;

        values["scene_backed_pause_menu"] = pauseMenu != null && IsSceneBacked(pauseMenu);
        values["pause_menu_scene_file_path"] = pauseMenuSceneFilePath;

        return values;
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
                && node.Data.Get<int>(CollisionDataKeys.Team, 0) == 2)
            {
                return node;
            }
        }

        return null;
    }

    private static GodotEntity2D? FindFirstAliveEnemyExcept(EntityId except)
    {
        var entities = EntityManager.GetAll();
        for (var i = 0; i < entities.Count; i++)
        {
            if (entities[i] is GodotEntity2D node
                && node.EntityId != except
                && node.EntityId.Value.StartsWith("spawn-", StringComparison.Ordinal)
                && node.Data.Get<int>(CollisionDataKeys.Team, 0) == 2
                && !node.Data.Get<bool>(DamageDataKeys.IsDead, false)
                && node.Data.Get<float>(DamageDataKeys.CurrentHp, 0f) > 0f)
            {
                return node;
            }
        }

        return null;
    }

    private static Node2D? FindExperiencePickup(Node? pickupLayer)
    {
        if (pickupLayer == null)
        {
            return null;
        }

        foreach (var child in pickupLayer.GetChildren())
        {
            if (child is Node2D pickup
                && pickup.Name.ToString().StartsWith("ExperiencePickup", StringComparison.Ordinal)
                && !pickup.IsQueuedForDeletion())
            {
                return pickup;
            }
        }

        return null;
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

    private static void DestroyRemainingEnemies()
    {
        var entities = EntityManager.GetAll();
        for (var i = 0; i < entities.Count; i++)
        {
            if (entities[i] is not GodotEntity2D enemy
                || enemy.Data.Get<int>(CollisionDataKeys.Team, 0) != 2
                || enemy.IsQueuedForDeletion())
            {
                continue;
            }

            enemy.Data.Set(DamageDataKeys.CurrentHp, 0f);
            enemy.Data.Set(DamageDataKeys.IsDead, true);
            enemy.DestroyEntity();
        }
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

    private static bool ReadBoolMeta(Node? node, string key)
    {
        if (node == null || !node.HasMeta(key))
        {
            return false;
        }

        var value = node.GetMeta(key);
        return value.VariantType switch
        {
            Variant.Type.Bool => value.AsBool(),
            Variant.Type.Int => value.AsInt32() != 0,
            Variant.Type.String => bool.TryParse(value.AsString(), out var parsed) && parsed,
            _ => false
        };
    }

    private static string ReadStringMeta(Node? node, string key)
    {
        return node != null && node.HasMeta(key)
            ? node.GetMeta(key).AsString()
            : string.Empty;
    }

    private static float ReadFloatMeta(Node? node, string key)
    {
        if (node == null || !node.HasMeta(key))
        {
            return 0f;
        }

        var value = node.GetMeta(key);
        return value.VariantType switch
        {
            Variant.Type.Float => value.AsSingle(),
            Variant.Type.Int => value.AsInt32(),
            Variant.Type.String => float.TryParse(
                value.AsString(),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var parsed) ? parsed : 0f,
            _ => 0f
        };
    }

    private static bool IsSceneBacked(Node? node)
    {
        return node != null && GodotObject.IsInstanceValid(node) && !string.IsNullOrEmpty(node.SceneFilePath);
    }
}
