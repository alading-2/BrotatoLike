using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BrotatoLike.Game;
using Godot;
using SlimeAI.GameOS.Capabilities.Collision;
using SlimeAI.GameOS.Capabilities.Damage;
using SlimeAI.GameOS.Capabilities.Movement;
using SlimeAI.GameOS.Capabilities.Unit;
using SlimeAI.GameOS.GodotBridge;
using SlimeAI.GameOS.Observation;
using SlimeAI.GameOS.Runtime.Entity;

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
                "Level-up choices and shop systems are out of scope for this validation."
            },
            expectedInputs: new[]
            {
                "BrotatoLikeGameRuntime initialized from DataOS snapshot",
                "DataOS player and enemy entities with Unit.ExpReward data",
                "runtime pause/resume calls and deterministic frame advancement"
            },
            expectedObservations: new[]
            {
                "wave runtime state records elapsed time, spawned count, remaining enemies and completion",
                "formal pause menu opens, blocks schedule-gated gameplay, and resumes",
                "HP recovery, pickup collection, experience gain and level-up feedback are observable",
                "formal pause menu node has non-empty SceneFilePath (scene-backed, not code-created)"
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
                "wave, pause, recovery, pickup, experience or level-up evidence is missing",
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

        var enemy = FindFirstEnemy();
        var tickBeforePause = runtime.TickSpawn(0.1d);
        runtime.OpenPauseMenu();
        var tickDuringPause = runtime.TickSpawn(0.1d);
        await ProcessFrames(2);
        runtime.ClosePauseMenu();
        var tickAfterResume = runtime.TickSpawn(0.1d);

        values["player_entity"] = player.EntityId.Value;
        values["enemy_entity"] = enemy?.EntityId.Value ?? string.Empty;
        values["spawned_before_pause"] = tickBeforePause.Value.TotalSpawned;
        values["tick_during_pause_success"] = tickDuringPause.Success;
        values["tick_after_resume_success"] = tickAfterResume.Success;
        values["enemy_exp_reward"] = enemy?.Data.Get<int>(UnitDataKeys.ExpReward, 0) ?? 0;

        var waveState = FindDescendant(this, "WaveRuntimeState");
        var pauseMenu = FindDescendant(this, "BrotatoLikePauseMenu") as CanvasItem;
        var recoveryService = FindDescendant(this, "RecoveryTickService");
        var pickupLayer = FindDescendant(this, "ExperiencePickupLayer");
        var progressionSummary = FindDescendant(this, "ProgressionSummary");
        var levelUpFeedback = FindDescendant(this, "LevelUpFeedback");

        values["wave_state_found"] = waveState != null;
        values["pause_menu_found"] = pauseMenu != null;
        values["pause_menu_visible"] = pauseMenu?.Visible ?? false;
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
        if (enemy != null)
        {
            enemy.Data.Set(DamageDataKeys.CurrentHp, 0f);
            enemy.Data.Set(DamageDataKeys.IsDead, true);
            enemy.DestroyEntity();
            await ProcessFrames(5);
        }

        var pickup = FindDescendant(this, "ExperiencePickup");
        var oldExperience = ReadIntMeta(player, "Experience");
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
            && FindDescendant(this, "ExperiencePickup") == null;

        DestroyRemainingEnemies();
        runtime.ProgressionService?.CompleteCurrentWaveForValidation();
        await ProcessFrames(5);
        var waveCompleted = ReadBoolMeta(waveState, "Completed");
        var remainingEnemies = ReadIntMeta(waveState, "RemainingEnemies");
        var manaRecoveryStatus = ReadStringMeta(recoveryService, "ManaRecoveryStatus");

        values["pickup_found"] = pickup != null;
        values["old_experience"] = oldExperience;
        values["new_experience"] = newExperience;
        values["old_level"] = oldLevel;
        values["new_level"] = newLevel;
        values["last_reward"] = lastReward;
        values["pickup_cleanup_done"] = pickupCleaned;
        values["player_level"] = level;
        values["wave_completed_meta"] = waveCompleted;
        values["wave_remaining_enemies"] = remainingEnemies;
        values["mana_recovery_status"] = manaRecoveryStatus;

        values["wave_completion_state"] = waveState != null
            && waveCompleted
            && remainingEnemies == 0;
        values["pause_menu_blocks_and_resumes_tick"] = pauseMenu != null
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
            && (newExperience > oldExperience || newLevel > oldLevel)
            && pickupCleaned;
        values["level_up_feedback"] = progressionSummary != null
            && levelUpFeedback != null
            && level > 1;

        values["scene_backed_pause_menu"] = pauseMenu != null && IsSceneBacked(pauseMenu);
        values["pause_menu_scene_file_path"] = pauseMenu is Node n ? n.SceneFilePath : string.Empty;

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

    private static bool IsSceneBacked(Node? node)
    {
        return node != null && GodotObject.IsInstanceValid(node) && !string.IsNullOrEmpty(node.SceneFilePath);
    }
}
