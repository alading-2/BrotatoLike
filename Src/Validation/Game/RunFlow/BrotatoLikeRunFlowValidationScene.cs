using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BrotatoLike.Game;
using BrotatoLike.Game.RunFlow;
using BrotatoLike.Game.UI;
using Godot;
using SlimeAI.GameOS.Capabilities.Collision;
using SlimeAI.GameOS.Capabilities.Damage;
using SlimeAI.GameOS.Capabilities.Movement;
using SlimeAI.GameOS.GodotBridge;
using SlimeAI.GameOS.Observation;
using SlimeAI.GameOS.Runtime.Entity;

namespace BrotatoLike.Validation.Game.RunFlow;

/// <summary>
/// BrotatoLike 多波 run flow、波间阶段、生命周期门禁和 cleanup 的验证场景。
/// </summary>
public partial class BrotatoLikeRunFlowValidationScene : Node
{
    private const string ScenePath = "res://Src/Validation/Game/RunFlow/BrotatoLikeRunFlowValidation.tscn";
    private const string ArtifactFileName = "brotatolike-run-flow-validation.json";
    private const string PassMarker = "BrotatoLike Run Flow validation PASS";
    private const string FailMarker = "BrotatoLike Run Flow validation FAIL";

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
            "BrotatoLikeRunFlowValidation",
            "Game/RunFlow",
            ArtifactFileName,
            dependencies: new[]
            {
                "BrotatoLike.Game.BrotatoLikeGameRuntime",
                "BrotatoLike.Game.RunFlow.BrotatoLikeWaveCatalog",
                "BrotatoLike.Game.Progression.BrotatoLikeProgressionService",
                "BrotatoLike.Game.UI.ExperienceBarUI",
                "SlimeAI.GameOS.Runtime.Schedule"
            },
            notes: new[]
            {
                "Validation uses DataOS generated wave_authoring.json.",
                "The two authored waves are deterministic and finite for accelerated headless evidence.",
                "Reward/shop integration is validated as hook metadata, not full economy balancing."
            },
            expectedInputs: new[]
            {
                "BrotatoLikeGameRuntime initialized from the BrotatoLike DataOS snapshot",
                "DataOS generated wave_definition and wave_enemy_entry authoring",
                "Wave 1 and Wave 2 with finite deterministic enemy entries and next-wave behavior",
                "Production progression, spawn, pause, respawn, shop hook and HUD systems"
            },
            expectedObservations: new[]
            {
                "Wave authoring includes wave ids, entries, spawn timing, completion mode and reference validation",
                "Wave 1 starts in Running, spawns authored enemies, completes, and enters RewardShop",
                "Reward hook metadata records shop_offer.validation and validation offer set",
                "Wave 2 starts through runtime state machine and spawns authored enemy entries",
                "Pause blocks schedule-gated spawn ticks and resume restores them",
                "Death/respawn keeps player HP, IsDead and Movement.CanMoveInput valid",
                "Cleanup metadata records runtime entity, enemy, pickup and projectile/effect counts",
                "Scene-backed ExperienceBarUI records wave index and wave phase"
            },
            passCriteria: new[]
            {
                $"stdout contains {PassMarker}",
                "artifact status is pass",
                "all run-flow checks pass and standard-answer fields are non-empty"
            },
            failCriteria: new[]
            {
                $"stdout contains {FailMarker}",
                "wave authoring, first/second wave transition, pause, respawn, cleanup, or scene-backed UI evidence is missing",
                "artifact status is fail with feature-level failureReasons"
            });

        validation.Info("validation start");
        Dictionary<string, object?> values;
        try
        {
            values = await RunFlowProbe();
        }
        catch (Exception ex)
        {
            values = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["probe_exception_type"] = ex.GetType().FullName,
                ["probe_exception_message"] = ex.Message
            };
        }

        validation.Check("wave_authoring_loaded_and_validates_refs", "Authoring", () => Result(values, "wave_authoring_loaded_and_validates_refs"));
        validation.Check("first_wave_starts_and_spawns", "Wave", () => Result(values, "first_wave_starts_and_spawns"));
        validation.Check("pause_gate_and_respawn_preserved", "Lifecycle", () => Result(values, "pause_gate_and_respawn_preserved"));
        validation.Check("first_wave_completion_reward_phase", "Wave", () => Result(values, "first_wave_completion_reward_phase"));
        validation.Check("second_wave_starts", "Wave", () => Result(values, "second_wave_starts"));
        validation.Check("wave_cleanup_counts", "Cleanup", () => Result(values, "wave_cleanup_counts"));
        validation.Check("wave_ui_phase_scene_backed", "UI", () => Result(values, "wave_ui_phase_scene_backed"));

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
            GD.Print($"BrotatoLike Run Flow failures: {string.Join("; ", validation.FailureReasons)}");
        }

        GetTree().Quit(success ? 0 : 1);
    }

    private async Task<Dictionary<string, object?>> RunFlowProbe()
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        var bootstrap = BrotatoLikeDataOSBootstrap.LoadFromResource();
        var waveJson = ReadResourceText("res://DataOS/Snapshots/wave_authoring.json");
        var catalog = BrotatoLikeWaveCatalog.FromJson(waveJson);
        catalog.Validate(bootstrap);
        var missingEnemyRejected = ThrowsWaveValidation(
            waveJson.Replace("\"enemyId\":\"chailangren\"", "\"enemyId\":\"missing_enemy\""),
            bootstrap,
            "missing DataOS enemy id");
        var missingResourceRejected = ThrowsWaveValidation(
            waveJson.Replace("res://assets/Unit/Enemy/yuren/AnimatedSprite2D/yuren.tscn", "res://missing/run-flow-validation.tscn"),
            bootstrap,
            "visual resource is missing");

        _ = catalog.TryGetWave(1, out var wave1);
        _ = catalog.TryGetWave(2, out var wave2);
        var wave1Spawn = catalog.BuildSpawnCatalog(1);
        var wave2Spawn = catalog.BuildSpawnCatalog(2);
        values["wave_catalog_source"] = catalog.Snapshot.Source;
        values["wave_count"] = CountWaves(catalog);
        values["wave1_expected_spawn_count"] = wave1Spawn.ExpectedSpawnCount;
        values["wave2_expected_spawn_count"] = wave2Spawn.ExpectedSpawnCount;
        values["wave1_authoring_kind"] = wave1Spawn.HasFiniteSpawnLimit ? "finite" : "open";
        values["wave2_authoring_kind"] = wave2Spawn.HasFiniteSpawnLimit ? "finite" : "open";
        values["wave1_completion_mode"] = wave1.CompletionMode;
        values["wave1_next_wave_id"] = wave1.NextWaveId ?? 0;
        values["wave1_reward_hook"] = wave1.RewardHook;
        values["wave1_shop_offer_set_id"] = wave1.ShopOfferSetId;
        values["wave2_next_phase"] = wave2.NextPhase;
        values["missing_enemy_rejected"] = missingEnemyRejected;
        values["missing_resource_rejected"] = missingResourceRejected;

        var runtime = new BrotatoLikeGameRuntime
        {
            Name = "GameRuntime",
            AutoInitialize = false,
            AutoTick = true
        };
        AddChild(runtime);
        runtime.Initialize(bootstrap, 1, runtime);
        runtime.BeginGameplay();
        var player = runtime.SpawnPlayer("deluyi", Vector2.Zero);
        await ProcessFrames(12);
        runtime.AutoTick = false;

        var waveState = FindDescendant(this, "WaveRuntimeState");
        var firstWaveEnemies = FindWaveEnemies(1);
        var firstWaveRuleIds = JoinEnemyMeta(firstWaveEnemies, "SpawnRuleId");
        var firstWaveDisplayNames = JoinEnemyMeta(firstWaveEnemies, "SpawnRuleDisplayName");
        values["player_entity"] = player.EntityId.Value;
        values["first_wave_phase"] = runtime.ProgressionService?.WavePhaseName ?? string.Empty;
        values["first_wave_runtime_wave"] = runtime.CurrentWave;
        values["first_wave_state_wave_index"] = ReadIntMeta(waveState, "WaveIndex");
        values["first_wave_spawn_count"] = firstWaveEnemies.Count;
        values["first_wave_spawn_rule_ids"] = firstWaveRuleIds;
        values["first_wave_spawn_rule_display_names"] = firstWaveDisplayNames;

        var tickBeforePause = runtime.TickSpawn(0.1d);
        runtime.OpenPauseMenu();
        var tickDuringPause = runtime.TickSpawn(0.1d);
        await ProcessFrames(2);
        var pauseMenu = FindDescendant(this, "BrotatoLikePauseMenu") as PauseMenuUI;
        var pauseVisibleDuring = pauseMenu?.IsMenuVisible ?? false;
        runtime.ClosePauseMenu();
        await ProcessFrames(2);
        var pauseVisibleAfterClose = pauseMenu?.IsMenuVisible ?? true;
        var tickAfterResume = runtime.TickSpawn(0.1d);

        player.Data.Set(DamageDataKeys.CurrentHp, 0f);
        player.Data.Set(DamageDataKeys.IsDead, true);
        runtime.ForceRespawnForValidation();
        var respawnGateObserved = runtime.IsPlayerRespawning;
        runtime.AutoTick = true;
        await ProcessFrames(5);
        runtime.AutoTick = false;
        var respawnedPlayer = runtime.PlayerEntity;
        var respawnedPlayerReady = respawnedPlayer != null
            && GodotObject.IsInstanceValid(respawnedPlayer)
            && !respawnedPlayer.Data.Get<bool>(DamageDataKeys.IsDead, false)
            && respawnedPlayer.Data.Get<float>(DamageDataKeys.CurrentHp, 0f) > 0f
            && respawnedPlayer.Data.Get<bool>(MovementDataKeys.CanMoveInput, false);

        values["tick_before_pause_success"] = tickBeforePause.Success;
        values["tick_during_pause_success"] = tickDuringPause.Success;
        values["tick_after_resume_success"] = tickAfterResume.Success;
        values["pause_menu_visible_during"] = pauseVisibleDuring;
        values["pause_menu_visible_after_close"] = pauseVisibleAfterClose;
        values["pause_menu_scene_path"] = pauseMenu?.SceneFilePath ?? string.Empty;
        values["respawn_gate_observed"] = respawnGateObserved;
        values["respawned_player_entity"] = respawnedPlayer?.EntityId.Value ?? string.Empty;
        values["respawned_player_ready"] = respawnedPlayerReady;
        values["respawned_player_hp"] = respawnedPlayer?.Data.Get<float>(DamageDataKeys.CurrentHp, 0f) ?? 0f;
        values["respawned_player_can_move_input"] = respawnedPlayer?.Data.Get<bool>(MovementDataKeys.CanMoveInput, false) ?? false;

        KillWaveEnemies(1);
        runtime.ProgressionService?.CompleteCurrentWaveForValidation();
        await ProcessFrames(5);
        var phaseAfterCompletion = ReadStringMeta(waveState, "WavePhase");
        var completedMeta = ReadBoolMeta(waveState, "Completed");
        var defeatedCount = ReadIntMeta(waveState, "DefeatedCount");
        var remainingEnemies = ReadIntMeta(waveState, "RemainingEnemies");
        var cleanupRuntimeBefore = ReadIntMeta(waveState, "CleanupRuntimeEntityCountBefore");
        var cleanupRuntimeAfter = ReadIntMeta(waveState, "CleanupRuntimeEntityCountAfter");
        var cleanupEnemyBefore = ReadIntMeta(waveState, "CleanupEnemyCountBefore");
        var cleanupEnemyAfter = ReadIntMeta(waveState, "CleanupEnemyCountAfter");
        var cleanupPickupBefore = ReadIntMeta(waveState, "CleanupPickupCountBefore");
        var cleanupPickupAfter = ReadIntMeta(waveState, "CleanupPickupCountAfter");
        var cleanupProjectileEffectBefore = ReadIntMeta(waveState, "CleanupProjectileEffectCountBefore");
        var cleanupProjectileEffectAfter = ReadIntMeta(waveState, "CleanupProjectileEffectCountAfter");

        runtime.ProgressionService?.EnterRewardPhaseForValidation();
        await ProcessFrames(2);
        var rewardPhase = ReadStringMeta(waveState, "WavePhase");
        var rewardHook = ReadStringMeta(waveState, "RewardHook");
        var shopOfferSetId = ReadStringMeta(waveState, "ShopOfferSetId");
        var shopHookAvailable = ReadBoolMeta(waveState, "ShopHookAvailable");
        var levelUpHookAvailable = ReadBoolMeta(waveState, "LevelUpHookAvailable");

        var nextStarted = runtime.ProgressionService?.StartNextWaveForValidation() == true;
        runtime.AutoTick = true;
        await ProcessFrames(24);
        runtime.AutoTick = false;
        var secondWaveEnemies = FindWaveEnemies(2);
        var secondWaveRuleIds = JoinEnemyMeta(secondWaveEnemies, "SpawnRuleId");
        var secondWavePhase = runtime.ProgressionService?.WavePhaseName ?? string.Empty;
        var experienceBar = FindDescendant(this, "ExperienceBarUI") as ExperienceBarUI;
        var progressionSummary = FindDescendant(this, "ProgressionSummary");

        values["phase_after_completion"] = phaseAfterCompletion;
        values["first_wave_completed_meta"] = completedMeta;
        values["first_wave_defeated_count"] = defeatedCount;
        values["first_wave_remaining_enemies"] = remainingEnemies;
        values["reward_phase"] = rewardPhase;
        values["reward_hook"] = rewardHook;
        values["reward_shop_offer_set_id"] = shopOfferSetId;
        values["shop_hook_available"] = shopHookAvailable;
        values["level_up_hook_available"] = levelUpHookAvailable;
        values["cleanup_runtime_before"] = cleanupRuntimeBefore;
        values["cleanup_runtime_after"] = cleanupRuntimeAfter;
        values["cleanup_enemy_before"] = cleanupEnemyBefore;
        values["cleanup_enemy_after"] = cleanupEnemyAfter;
        values["cleanup_pickup_before"] = cleanupPickupBefore;
        values["cleanup_pickup_after"] = cleanupPickupAfter;
        values["cleanup_projectile_effect_before"] = cleanupProjectileEffectBefore;
        values["cleanup_projectile_effect_after"] = cleanupProjectileEffectAfter;
        values["second_wave_start_result"] = nextStarted;
        values["second_wave_runtime_wave"] = runtime.CurrentWave;
        values["second_wave_phase"] = secondWavePhase;
        values["second_wave_spawn_count"] = secondWaveEnemies.Count;
        values["second_wave_spawn_rule_ids"] = secondWaveRuleIds;
        values["second_wave_expected_spawn_count"] = wave2Spawn.ExpectedSpawnCount;
        values["second_wave_actual_spawned_count"] = secondWaveEnemies.Count;
        values["experience_bar_scene_path"] = experienceBar?.SceneFilePath ?? string.Empty;
        values["experience_bar_wave_index"] = ReadIntMeta(experienceBar, "WaveIndex");
        values["experience_bar_wave_phase"] = ReadStringMeta(experienceBar, "WavePhase");
        values["progression_summary_wave_index"] = ReadIntMeta(progressionSummary, "WaveIndex");
        values["progression_summary_wave_phase"] = ReadStringMeta(progressionSummary, "WavePhase");

        values["wave_authoring_loaded_and_validates_refs"] = CountWaves(catalog) >= 2
            && wave1Spawn.EnemyRules.Count == 2
            && wave2Spawn.EnemyRules.Count == 2
            && wave1Spawn.ExpectedSpawnCount == 5
            && wave2Spawn.ExpectedSpawnCount == 5
            && wave1.NextWaveId == 2
            && wave1.RewardHook == "shop_offer.validation"
            && wave1.ShopOfferSetId == "validation"
            && wave2.NextWaveId == null
            && missingEnemyRejected
            && missingResourceRejected;
        values["first_wave_starts_and_spawns"] = runtime.CurrentWave >= 1
            && values["first_wave_phase"] as string == "Running"
            && firstWaveEnemies.Count >= wave1Spawn.ExpectedSpawnCount
            && ContainsText(firstWaveRuleIds, "chailangren")
            && ContainsText(firstWaveRuleIds, "yuren");
        values["pause_gate_and_respawn_preserved"] = tickBeforePause.Success
            && !tickDuringPause.Success
            && tickAfterResume.Success
            && pauseVisibleDuring
            && !pauseVisibleAfterClose
            && respawnGateObserved
            && respawnedPlayerReady;
        values["first_wave_completion_reward_phase"] = phaseAfterCompletion == BrotatoLikeWavePhase.Completed.ToString()
            && completedMeta
            && defeatedCount >= wave1Spawn.ExpectedSpawnCount
            && remainingEnemies == 0
            && rewardPhase == BrotatoLikeWavePhase.RewardShop.ToString()
            && rewardHook == "shop_offer.validation"
            && shopOfferSetId == "validation"
            && shopHookAvailable
            && levelUpHookAvailable;
        values["second_wave_starts"] = nextStarted
            && runtime.CurrentWave == 2
            && secondWavePhase == BrotatoLikeWavePhase.Running.ToString()
            && secondWaveEnemies.Count >= wave2Spawn.ExpectedSpawnCount
            && ContainsText(secondWaveRuleIds, "chailangren")
            && ContainsText(secondWaveRuleIds, "yuren");
        values["wave_cleanup_counts"] = cleanupRuntimeBefore > 0
            && cleanupRuntimeAfter <= cleanupRuntimeBefore
            && cleanupEnemyBefore >= wave1Spawn.ExpectedSpawnCount
            && cleanupEnemyAfter == 0
            && cleanupPickupAfter <= cleanupPickupBefore
            && cleanupProjectileEffectAfter <= cleanupProjectileEffectBefore;
        values["wave_ui_phase_scene_backed"] = IsSceneBacked(experienceBar)
            && ReadIntMeta(experienceBar, "WaveIndex") == 2
            && ReadStringMeta(experienceBar, "WavePhase") == BrotatoLikeWavePhase.Running.ToString()
            && ReadIntMeta(progressionSummary, "WaveIndex") == 2
            && ReadStringMeta(progressionSummary, "WavePhase") == BrotatoLikeWavePhase.Running.ToString();

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

    private static string ReadResourceText(string path)
    {
        using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
        if (file == null)
        {
            throw new InvalidOperationException($"Resource not found: {path}");
        }

        return file.GetAsText();
    }

    private static bool ThrowsWaveValidation(string json, BrotatoLikeDataOSBootstrap bootstrap, string expectedMessage)
    {
        try
        {
            BrotatoLikeWaveCatalog.FromJson(json).Validate(bootstrap);
            return false;
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message.Contains(expectedMessage, StringComparison.Ordinal);
        }
    }

    private static int CountWaves(BrotatoLikeWaveCatalog catalog)
    {
        var count = 0;
        foreach (var _ in catalog.Waves)
        {
            count++;
        }

        return count;
    }

    private static List<GodotEntity2D> FindWaveEnemies(int wave)
    {
        var result = new List<GodotEntity2D>();
        var entities = EntityManager.GetAll();
        for (var i = 0; i < entities.Count; i++)
        {
            if (entities[i] is GodotEntity2D node
                && !node.IsQueuedForDeletion()
                && node.Data.Get<int>(CollisionDataKeys.Team, 0) == 2
                && ReadIntMeta(node, "WaveIndex") == wave)
            {
                result.Add(node);
            }
        }

        return result;
    }

    private static void KillWaveEnemies(int wave)
    {
        var enemies = FindWaveEnemies(wave);
        for (var i = 0; i < enemies.Count; i++)
        {
            enemies[i].Data.Set(DamageDataKeys.CurrentHp, 0f);
            enemies[i].Data.Set(DamageDataKeys.IsDead, true);
        }
    }

    private static string JoinEnemyMeta(IReadOnlyList<GodotEntity2D> enemies, string key)
    {
        var values = new string[enemies.Count];
        for (var i = 0; i < enemies.Count; i++)
        {
            values[i] = ReadStringMeta(enemies[i], key);
        }

        return string.Join(",", values);
    }

    private static bool ContainsText(string value, string expected)
    {
        return value.Contains(expected, StringComparison.Ordinal);
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
