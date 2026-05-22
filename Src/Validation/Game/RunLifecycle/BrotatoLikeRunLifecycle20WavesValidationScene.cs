using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using BrotatoLike.Game;
using BrotatoLike.Game.RunFlow;
using Godot;
using SlimeAI.GameOS.Capabilities.Collision;
using SlimeAI.GameOS.Capabilities.Damage;
using SlimeAI.GameOS.Capabilities.Effect;
using SlimeAI.GameOS.Capabilities.Projectile;
using SlimeAI.GameOS.GodotBridge;
using SlimeAI.GameOS.Observation;
using SlimeAI.GameOS.Runtime.Entity;

namespace BrotatoLike.Validation.Game.RunLifecycle;

/// <summary>
/// BrotatoLike 20 波单局生命周期、胜负终态和 restart 清理验证场景。
/// </summary>
public partial class BrotatoLikeRunLifecycle20WavesValidationScene : Node
{
    private const string ScenePath = "res://Src/Validation/Game/RunLifecycle/BrotatoLikeRunLifecycle20WavesValidation.tscn";
    private const string ArtifactFileName = "brotatolike-run-lifecycle-20-waves-validation.json";
    private const string PassMarker = "BrotatoLike Run Lifecycle 20 Waves validation PASS";
    private const string FailMarker = "BrotatoLike Run Lifecycle 20 Waves validation FAIL";

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
            "BrotatoLikeRunLifecycle20WavesValidation",
            "Game/RunLifecycle",
            ArtifactFileName,
            dependencies: new[]
            {
                "BrotatoLike.Game.BrotatoLikeGameRuntime",
                "BrotatoLike.Game.RunFlow.BrotatoLikeRunCatalog",
                "BrotatoLike.Game.Progression.BrotatoLikeProgressionService",
                "SlimeAI.GameOS.Runtime.Schedule",
                "SlimeAI.GameOS.Runtime.Entity"
            },
            notes: new[]
            {
                "Validation fast-forwards wave completion; it is lifecycle evidence, not balance evidence.",
                "Run summary UI details are out of scope; this scene only verifies summary payload handoff metadata.",
                "Death loss is enabled by validation metadata so legacy respawn-specific scenes can remain explicit."
            },
            expectedInputs: new[]
            {
                "BrotatoLikeGameRuntime initialized from the BrotatoLike DataOS snapshot",
                "Game-side run lifecycle authoring with a deterministic 20-wave run definition",
                "DataOS generated run_definition and run_enemy_entry authoring for waves 1 through 20",
                "Validation fast-forward commands for wave completion, death loss and restart"
            },
            expectedObservations: new[]
            {
                "Runtime evidence exposes configured wave count, current wave index and end-condition metadata",
                "Fast-forwarded wave completion records explicit phase sequence through reward/shop intermission and RunWon",
                "Player death in death-ends-run mode records RunLost with a stable loss reason",
                "Restart clears old enemies, pickups, transient UI, scheduled wave state and stale runtime entities",
                "Terminal states create a minimal summary payload for the later run summary UI change"
            },
            passCriteria: new[]
            {
                $"stdout contains {PassMarker}",
                "artifact status is pass",
                "all run lifecycle checks pass and standard-answer fields are non-empty"
            },
            failCriteria: new[]
            {
                $"stdout contains {FailMarker}",
                "20-wave authoring, terminal phase, death loss, restart cleanup, or summary payload evidence is missing",
                "artifact status is fail with feature-level failureReasons"
            });

        validation.Info("validation start");
        Dictionary<string, object?> values;
        try
        {
            values = await RunLifecycleProbe();
        }
        catch (Exception ex)
        {
            values = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["probe_exception_type"] = ex.GetType().FullName,
                ["probe_exception_message"] = ex.Message
            };
        }

        validation.Check("twenty_wave_run_authoring_loaded", "Authoring", () => Result(values, "twenty_wave_run_authoring_loaded"));
        validation.Check("phase_sequence_reaches_run_won", "Phase", () => Result(values, "phase_sequence_reaches_run_won"));
        validation.Check("death_enters_run_lost", "Terminal", () => Result(values, "death_enters_run_lost"));
        validation.Check("restart_clears_run_state", "Restart", () => Result(values, "restart_clears_run_state"));
        validation.Check("summary_payload_created", "Observation", () => Result(values, "summary_payload_created"));

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
            GD.Print($"BrotatoLike Run Lifecycle failures: {string.Join("; ", validation.FailureReasons)}");
        }

        GetTree().Quit(success ? 0 : 1);
    }

    private async Task<Dictionary<string, object?>> RunLifecycleProbe()
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        var bootstrap = BrotatoLikeDataOSBootstrap.LoadFromResource();
        var runJson = ReadResourceText("res://DataOS/Snapshots/run_authoring.json");
        var catalog = BrotatoLikeRunCatalog.FromJson(runJson);
        catalog.Validate(bootstrap);

        var waveCount = CountWaves(catalog);
        var maxWaveId = MaxWaveId(catalog);
        var hasWave20 = catalog.TryGetWave(20, out var wave20);
        values["wave_count"] = waveCount;
        values["max_wave_id"] = maxWaveId;
        values["has_wave_20"] = hasWave20;
        values["wave20_next_wave_id"] = hasWave20 ? wave20.NextWaveId ?? 0 : 0;
        values["wave20_next_phase"] = hasWave20 ? wave20.NextPhase : string.Empty;

        var winRuntime = CreateRuntime("RunLifecycleWinRuntime", bootstrap, deathEndsRun: false);
        AddChild(winRuntime);
        winRuntime.BeginGameplay();
        winRuntime.SpawnPlayer("deluyi", Vector2.Zero);
        await ProcessFrames(4);

        var phaseSequence = new List<string>();
        var won = await FastForwardToWin(winRuntime, Math.Min(20, Math.Max(1, waveCount)), phaseSequence);
        var winState = FindDescendant(winRuntime, "WaveRuntimeState");
        values["phase_sequence"] = string.Join(">", phaseSequence);
        var winFinalPhase = winRuntime.ProgressionService?.WavePhaseName ?? string.Empty;
        var winReason = ReadStringMeta(winState, "RunTerminalReason");
        var winSummaryFinalWave = ReadIntMeta(winState, "SummaryFinalWave");
        values["win_final_phase"] = winFinalPhase;
        values["win_reason"] = winReason;
        var winSummaryPayloadCreated = ReadBoolMeta(winState, "SummaryPayloadCreated");
        var winSummaryResult = ReadStringMeta(winState, "SummaryResult");
        values["win_summary_payload_created"] = winSummaryPayloadCreated;
        values["win_summary_final_wave"] = winSummaryFinalWave;
        values["win_summary_result"] = winSummaryResult;
        values["win_current_wave"] = winRuntime.CurrentWave;
        values["win_result"] = won;
        winRuntime.Shutdown();
        winRuntime.QueueFree();
        await ProcessFrames(1);

        var lossRuntime = CreateRuntime("RunLifecycleLossRuntime", bootstrap, deathEndsRun: true);
        AddChild(lossRuntime);
        lossRuntime.BeginGameplay();
        var lossPlayer = lossRuntime.SpawnPlayer("deluyi", new Vector2(96f, 0f));
        var lossPlayerId = lossPlayer.EntityId;
        await ProcessFrames(4);
        lossPlayer.Data.Set(DamageDataKeys.CurrentHp, 0f);
        lossPlayer.Data.Set(DamageDataKeys.IsDead, true);
        await ProcessFrames(4);
        var lossState = FindDescendant(lossRuntime, "WaveRuntimeState");
        var lossFinalPhase = lossRuntime.ProgressionService?.WavePhaseName ?? string.Empty;
        var lossReason = ReadStringMeta(lossState, "RunTerminalReason");
        var lossSummaryPayloadCreated = ReadBoolMeta(lossState, "SummaryPayloadCreated");
        var lossSummaryResult = ReadStringMeta(lossState, "SummaryResult");
        values["loss_final_phase"] = lossFinalPhase;
        values["loss_reason"] = lossReason;
        values["loss_summary_payload_created"] = lossSummaryPayloadCreated;
        values["loss_summary_result"] = lossSummaryResult;

        var enemyEntitiesBeforeRestart = CaptureRuntimeEnemies(lossRuntime);
        var projectileEffectEntitiesBeforeRestart = CaptureProjectileAndEffects(lossPlayerId);
        var enemyBeforeRestart = enemyEntitiesBeforeRestart.Count;
        var projectileEffectBeforeRestart = projectileEffectEntitiesBeforeRestart.Count;
        var runtimeBeforeRestart = EntityManager.GetAll().Count;
        var restartResult = InvokeRestart(lossRuntime);
        await ProcessFrames(4);
        var restartState = lossRuntime.ProgressionService?.WaveRuntimeState;
        values["restart_result"] = restartResult;
        values["restart_phase"] = lossRuntime.ProgressionService?.WavePhaseName ?? string.Empty;
        values["restart_count"] = ReadIntMeta(restartState, "RestartCount");
        values["restart_old_runtime_count"] = ReadIntMeta(restartState, "RestartRuntimeEntityCountBefore");
        values["restart_new_runtime_count"] = ReadIntMeta(restartState, "RestartRuntimeEntityCountAfter");
        values["restart_enemy_count_before"] = enemyBeforeRestart;
        values["restart_enemy_count_after"] = CountRuntimeEnemies(lossRuntime);
        values["restart_stale_enemy_count_after"] = CountCurrentEntityInstances(enemyEntitiesBeforeRestart);
        values["restart_projectile_effect_before"] = projectileEffectBeforeRestart;
        values["restart_projectile_effect_after"] = CountProjectileAndEffectEntitiesForSource(lossPlayerId);
        values["restart_stale_projectile_effect_count_after"] = CountCurrentEntityInstances(projectileEffectEntitiesBeforeRestart);
        values["restart_runtime_before"] = runtimeBeforeRestart;
        values["restart_runtime_after"] = EntityManager.GetAll().Count;
        values["restart_current_wave"] = lossRuntime.CurrentWave;

        values["twenty_wave_run_authoring_loaded"] = waveCount == 20
            && maxWaveId == 20
            && hasWave20
            && wave20.NextWaveId == null
            && string.Equals(wave20.NextPhase, "RunWon", StringComparison.Ordinal);
        values["phase_sequence_reaches_run_won"] = won
            && string.Equals(winFinalPhase, "RunWon", StringComparison.Ordinal)
            && ContainsText(values["phase_sequence"], "RewardShop")
            && ContainsText(values["phase_sequence"], "RunWon")
            && string.Equals(winReason, "final_wave_completed", StringComparison.Ordinal)
            && winSummaryFinalWave == 20;
        values["death_enters_run_lost"] = string.Equals(lossFinalPhase, "RunLost", StringComparison.Ordinal)
            && string.Equals(lossReason, "player_death", StringComparison.Ordinal)
            && string.Equals(lossSummaryResult, "loss", StringComparison.Ordinal);
        values["restart_clears_run_state"] = restartResult
            && string.Equals(lossRuntime.ProgressionService?.WavePhaseName, "Running", StringComparison.Ordinal)
            && lossRuntime.CurrentWave == 1
            && CountCurrentEntityInstances(enemyEntitiesBeforeRestart) == 0
            && CountCurrentEntityInstances(projectileEffectEntitiesBeforeRestart) == 0
            && ReadIntMeta(restartState, "RestartCount") > 0
            && ReadIntMeta(restartState, "RestartRuntimeEntityCountAfter") <= ReadIntMeta(restartState, "RestartRuntimeEntityCountBefore");
        values["summary_payload_created"] = winSummaryPayloadCreated
            && lossSummaryPayloadCreated
            && string.Equals(winSummaryResult, "win", StringComparison.Ordinal)
            && string.Equals(lossSummaryResult, "loss", StringComparison.Ordinal);

        lossRuntime.Shutdown();
        lossRuntime.QueueFree();
        await ProcessFrames(1);
        return values;
    }

    private static BrotatoLikeGameRuntime CreateRuntime(string name, BrotatoLikeDataOSBootstrap bootstrap, bool deathEndsRun)
    {
        var runtime = new BrotatoLikeGameRuntime
        {
            Name = name,
            AutoInitialize = false,
            AutoTick = true
        };
        runtime.SetMeta("DeathEndsRun", deathEndsRun);
        runtime.SetMeta("RunAuthoringPath", "res://DataOS/Snapshots/run_authoring.json");
        runtime.SetMeta("RunId", BrotatoLikeRunCatalog.DefaultRunId);
        runtime.Initialize(bootstrap, 1, runtime);
        return runtime;
    }

    private async Task<bool> FastForwardToWin(BrotatoLikeGameRuntime runtime, int waveCount, List<string> phaseSequence)
    {
        for (var wave = 1; wave <= waveCount; wave++)
        {
            phaseSequence.Add(runtime.ProgressionService?.WavePhaseName ?? string.Empty);
            runtime.ProgressionService?.CompleteCurrentWaveForValidation();
            await ProcessFrames(2);
            phaseSequence.Add(runtime.ProgressionService?.WavePhaseName ?? string.Empty);
            if (wave == waveCount)
            {
                break;
            }

            runtime.ProgressionService?.EnterRewardPhaseForValidation();
            await ProcessFrames(1);
            phaseSequence.Add(runtime.ProgressionService?.WavePhaseName ?? string.Empty);
            runtime.ProgressionService?.StartNextWaveForValidation();
            await ProcessFrames(1);
        }

        phaseSequence.Add(runtime.ProgressionService?.WavePhaseName ?? string.Empty);
        return string.Equals(runtime.ProgressionService?.WavePhaseName, "RunWon", StringComparison.Ordinal);
    }

    private static bool InvokeRestart(BrotatoLikeGameRuntime runtime)
    {
        var method = typeof(BrotatoLikeGameRuntime).GetMethod(
            "RestartRunForValidation",
            BindingFlags.Public | BindingFlags.Instance);
        if (method == null)
        {
            return false;
        }

        return method.Invoke(runtime, Array.Empty<object>()) is bool result && result;
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

    private static int CountWaves(BrotatoLikeRunCatalog catalog)
    {
        var count = 0;
        foreach (var _ in catalog.Waves)
        {
            count++;
        }

        return count;
    }

    private static int MaxWaveId(BrotatoLikeRunCatalog catalog)
    {
        var max = 0;
        foreach (var wave in catalog.Waves)
        {
            max = Math.Max(max, wave.WaveId);
        }

        return max;
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

    private static int CountEnemies()
    {
        var count = 0;
        var entities = EntityManager.GetAll();
        for (var i = 0; i < entities.Count; i++)
        {
            if (entities[i].Data.Get<int>(CollisionDataKeys.Team, 0) == 2)
            {
                count++;
            }
        }

        return count;
    }

    private static List<IEntity> CaptureRuntimeEnemies(Node runtimeRoot)
    {
        var result = new List<IEntity>();
        var entities = EntityManager.GetAll();
        for (var i = 0; i < entities.Count; i++)
        {
            if (entities[i] is not GodotEntity2D node
                || entities[i].Data.Get<int>(CollisionDataKeys.Team, 0) != 2
                || !IsDescendantOf(node, runtimeRoot))
            {
                continue;
            }

            result.Add(entities[i]);
        }

        return result;
    }

    private static int CountRuntimeEnemies(Node runtimeRoot)
    {
        var count = 0;
        var entities = EntityManager.GetAll();
        for (var i = 0; i < entities.Count; i++)
        {
            if (entities[i] is GodotEntity2D node
                && entities[i].Data.Get<int>(CollisionDataKeys.Team, 0) == 2
                && IsDescendantOf(node, runtimeRoot))
            {
                count++;
            }
        }

        return count;
    }

    private static bool IsDescendantOf(Node node, Node ancestor)
    {
        var current = node;
        while (current != null)
        {
            if (current == ancestor)
            {
                return true;
            }

            current = current.GetParent();
        }

        return false;
    }

    private static int CountProjectileAndEffectEntities()
    {
        var count = 0;
        var entities = EntityManager.GetAll();
        for (var i = 0; i < entities.Count; i++)
        {
            if (entities[i].Data.Has(ProjectileDataKeys.ScenePath)
                || entities[i].Data.Has(EffectDataKeys.ScenePath))
            {
                count++;
            }
        }

        return count;
    }

    private static List<IEntity> CaptureProjectileAndEffects(EntityId sourceId)
    {
        var result = new List<IEntity>();
        var entities = EntityManager.GetAll();
        for (var i = 0; i < entities.Count; i++)
        {
            if (IsProjectileOrEffectFromSource(entities[i], sourceId))
            {
                result.Add(entities[i]);
            }
        }

        return result;
    }

    private static int CountProjectileAndEffectEntitiesForSource(EntityId sourceId)
    {
        var count = 0;
        var entities = EntityManager.GetAll();
        for (var i = 0; i < entities.Count; i++)
        {
            if (IsProjectileOrEffectFromSource(entities[i], sourceId))
            {
                count++;
            }
        }

        return count;
    }

    private static bool IsProjectileOrEffectFromSource(IEntity entity, EntityId sourceId)
    {
        if (entity.Data.Has(ProjectileDataKeys.ScenePath))
        {
            return entity.Data.Get<EntityId?>(ProjectileDataKeys.SourceEntity, null) == sourceId;
        }

        return entity.Data.Has(EffectDataKeys.ScenePath)
            && entity.Data.Get<EntityId?>(EffectDataKeys.SourceEntity, null) == sourceId;
    }

    private static int CountCurrentEntityInstances(IEnumerable<IEntity> entities)
    {
        var count = 0;
        foreach (var entity in entities)
        {
            if (ReferenceEquals(EntityManager.Get(entity.EntityId), entity))
            {
                count++;
            }
        }

        return count;
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

    private static bool ContainsText(object? value, string expected)
    {
        return value?.ToString()?.Contains(expected, StringComparison.Ordinal) == true;
    }
}
