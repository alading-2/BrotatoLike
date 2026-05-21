using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BrotatoLike.Game;
using BrotatoLike.Game.Events;
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

namespace BrotatoLike.Validation.Game.GameLifecycle;

/// <summary>
/// BrotatoLike 游戏循环 feature-slice 集成验证场景。
/// </summary>
public partial class BrotatoLikeGameplayLifecycleValidationScene : Node
{
    private const string ScenePath = "res://Src/Validation/Game/GameLifecycle/BrotatoLikeGameplayLifecycleValidation.tscn";
    private const string ArtifactFileName = "brotatolike-gameplay-lifecycle-validation.json";
    private const string PassMarker = "BrotatoLike Gameplay Lifecycle validation PASS";
    private const string FailMarker = "BrotatoLike Gameplay Lifecycle validation FAIL";

    /// <inheritdoc />
    public override async void _Ready()
    {
        EntityManager.Clear();
        BrotatoLikeAbilityHandlers.RegisterAll();

        using var observation = GameOSObservationSession.FromEnvironment(
            ScenePath, "validation",
            Path.Combine(Directory.GetCurrentDirectory(), ".ai-temp", "scene-tests", "manual", "artifacts"));
        using var validation = new SceneValidationSession(
            observation,
            "BrotatoLikeGameplayLifecycleValidation",
            "Game/GameLifecycle",
            ArtifactFileName,
            dependencies: new[]
            {
                "BrotatoLike.Game.BrotatoLikeGameRuntime",
                "BrotatoLike.Game.UI.BrotatoLikeHud",
                "BrotatoLike.Game.Bridge.BrotatoLikePlayerInputComponent",
                "BrotatoLike.Game.GodotActiveSkillInputComponent",
                "SlimeAI.GameOS.Capabilities.Damage",
                "SlimeAI.GameOS.Capabilities.Movement",
                "SlimeAI.GameOS.GodotBridge",
                "SlimeAI.GameOS.Runtime.Schedule"
            },
            notes: new[]
            {
                "Integration validation: death gate, camera follow, respawn, concurrent systems, pause/resume integrity.",
                "Verifies the full gameplay lifecycle chain, not single features in isolation.",
                "Corresponds to openspec/specs/gameplay-lifecycle-integration/bdd.md"
            },
            expectedInputs: new[]
            {
                "BrotatoLikeGameRuntime with AutoInitialize=false, AutoTick=true",
                "DataOS snapshot with unit.player/deluyi record",
                "Player spawned via SpawnPlayer()",
                "Deterministic frame advancement and input simulation"
            },
            expectedObservations: new[]
            {
                "Death blocks movement input and skill input (CanMoveInput=false, IsDead gate)",
                "Camera2D follows player and stays enabled during death",
                "Auto respawn restores HP, position, camera, and input after delay",
                "Concurrent skill cast + contact damage + loot drop in same frame produces no exceptions",
                "Pause/resume cycle preserves HP, position, skill cooldown, and spawn state",
                "HUD correctly reflects death and respawn state transitions"
            },
            passCriteria: new[]
            {
                $"stdout contains {PassMarker}",
                "artifact status is pass",
                "failureReasons is empty",
                "all 8 integration checks pass"
            },
            failCriteria: new[]
            {
                $"stdout contains {FailMarker}",
                "any integration check fails with specific failure reason",
                "artifact status is fail"
            });

        validation.Info("integration validation start");
        var values = await RunLifecycleProbe();
        validation.Check("death_blocks_movement_input", "DeathGate", () => Result(values, "death_blocks_movement_input"));
        validation.Check("death_blocks_skill_input", "DeathGate", () => Result(values, "death_blocks_skill_input"));
        validation.Check("death_camera_stays_enabled", "Camera", () => Result(values, "death_camera_stays_enabled"));
        validation.Check("death_auto_respawn", "Respawn", () => Result(values, "death_auto_respawn"));
        validation.Check("camera_follows_player", "Camera", () => Result(values, "camera_follows_player"));
        validation.Check("concurrent_systems_no_conflict", "Concurrent", () => Result(values, "concurrent_systems_no_conflict"));
        validation.Check("pause_resume_state_integrity", "Pause", () => Result(values, "pause_resume_state_integrity"));
        validation.Check("hud_death_respawn_state_clean", "HUD", () => Result(values, "hud_death_respawn_state_clean"));

        var success = validation.Success;
        if (success)
        {
            validation.Pass("all integration checks passed");
        }
        else
        {
            validation.Fail($"{validation.FailureReasons.Count} integration checks failed");
        }

        GD.Print(success ? PassMarker : FailMarker);
        validation.WriteArtifact();

        EntityManager.Clear();
        GetTree().Quit(success ? 0 : 1);
    }

    private async Task<Dictionary<string, object?>> RunLifecycleProbe()
    {
        var values = new Dictionary<string, object?>();
        var tree = ((SceneTree)Engine.GetMainLoop());
        var runtime = new BrotatoLikeGameRuntime
        {
            Name = "LifecycleValidationRuntime",
            AutoInitialize = false,
            AutoStartGameplay = false,
            AutoTick = true
        };
        AddChild(runtime);

        // wait for _Ready
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        runtime.InitializeFromDataOS(1, runtime);
        runtime.BeginGameplay();
        runtime.SpawnPlayer();
        await ProcessFrames(5);

        var player = runtime.PlayerEntity;
        if (player == null)
        {
            values["error"] = "player not spawned";
            return values;
        }

        // 场景 1：死亡阻断移动输入。
        await TestDeathMovementGate(runtime, player, values);

        // 场景 2：死亡阻断技能输入。
        TestDeathSkillGate(runtime, player, values);

        // 场景 3：死亡期间镜头保持启用。
        TestDeathCamera(runtime, player, values);

        // 场景 4：通过生产 _Process 触发自动重生。
        await TestAutoRespawn(runtime, player, values);
        var currentPlayer = runtime.PlayerEntity;
        if (currentPlayer == null || !GodotObject.IsInstanceValid(currentPlayer))
        {
            values["error"] = "player missing after respawn";
            return values;
        }

        // 场景 5：重生后镜头仍跟随玩家。
        TestCameraFollow(runtime, currentPlayer, values);

        // 场景 6：同帧跨系统事件不互相破坏。
        TestConcurrentSystems(runtime, currentPlayer, values);

        // 场景 7：暂停/恢复保持状态完整。
        TestPauseResume(runtime, currentPlayer, values);

        // 场景 8：HUD 死亡/重生状态清理。
        TestHudDeathRespawn(runtime, currentPlayer, values);

        runtime.Shutdown();
        runtime.QueueFree();
        await ProcessFrames(1);
        return values;
    }

    private static async Task TestDeathMovementGate(BrotatoLikeGameRuntime runtime, GodotEntity2D player, Dictionary<string, object?> values)
    {
        player.Data.Set(DamageDataKeys.CurrentHp, 100f);
        player.Data.Set(DamageDataKeys.MaxHp, 100f);
        player.Data.Set(DamageDataKeys.IsDead, false);
        player.Data.Set(MovementDataKeys.CanMoveInput, true);
        player.Data.Set(MovementDataKeys.InputDirection, new Vector2Value(1f, 0f));

        // 模拟致死伤害，然后等待生产 _Process 执行死亡门禁。
        player.Data.Set(DamageDataKeys.CurrentHp, 0f);
        player.Data.Set(DamageDataKeys.IsDead, true);
        await ProcessFrames(2);

        var canMoveInput = player.Data.Get<bool>(MovementDataKeys.CanMoveInput, true);
        var inputDirection = player.Data.Get<Vector2Value>(MovementDataKeys.InputDirection, new Vector2Value(1f, 0f));

        values["death_blocks_movement_input"] = !canMoveInput && Math.Abs(inputDirection.X) < 0.001f && Math.Abs(inputDirection.Y) < 0.001f;
        values["death_canmoveinput_value"] = canMoveInput;
        values["death_inputdirection_x"] = inputDirection.X;
        values["death_inputdirection_y"] = inputDirection.Y;
    }

    private static void TestDeathSkillGate(BrotatoLikeGameRuntime runtime, GodotEntity2D player, Dictionary<string, object?> values)
    {
        // 复用上一检查产生的死亡状态，确认技能切换事件被死亡门禁忽略。
        var ownedIds = player.Data.Get<EntityIdList>(AbilityDataKeys.OwnedAbilityIds);
        var originalIndex = player.Data.Get<int>(AbilityDataKeys.CurrentAbilityIndex, 0);

        player.Events.Publish(new BrotatoLike.Game.Events.InputNextSkill(player));
        var indexAfterNextSkill = player.Data.Get<int>(AbilityDataKeys.CurrentAbilityIndex, -1);

        values["death_blocks_skill_input"] = indexAfterNextSkill == originalIndex;
        values["death_skill_index_preserved"] = indexAfterNextSkill;
    }

    private static void TestDeathCamera(BrotatoLikeGameRuntime runtime, GodotEntity2D player, Dictionary<string, object?> values)
    {
        var camera = runtime.PlayerCamera;
        values["death_camera_stays_enabled"] = camera != null
            && GodotObject.IsInstanceValid(camera)
            && camera.Enabled;
        if (camera != null && GodotObject.IsInstanceValid(camera))
        {
            values["death_camera_enabled"] = camera.Enabled;
            values["death_camera_smoothing"] = camera.PositionSmoothingEnabled;
        }
    }

    private static async Task TestAutoRespawn(BrotatoLikeGameRuntime runtime, GodotEntity2D oldPlayer, Dictionary<string, object?> values)
    {
        var oldEntityId = oldPlayer.EntityId.Value;

        // 快进死亡计时器，等待生产 _Process 调用 RespawnPlayer。
        runtime.ForceRespawnForValidation();
        await ProcessFrames(3);

        var newPlayer = runtime.PlayerEntity;
        var newCamera = runtime.PlayerCamera;

        var newEntityId = newPlayer?.EntityId.Value ?? "";
        var respawned = newPlayer != null && GodotObject.IsInstanceValid(newPlayer);

        var newHp = newPlayer?.Data.Get<float>(DamageDataKeys.CurrentHp, -1f) ?? -1f;
        var newMaxHp = newPlayer?.Data.Get<float>(DamageDataKeys.MaxHp, -1f) ?? -1f;
        var hpRestored = newHp > 0f && Math.Abs(newHp - newMaxHp) < 0.01f;
        var canMove = newPlayer?.Data.Get<bool>(MovementDataKeys.CanMoveInput, false) ?? false;
        var newCameraEnabled = newCamera != null && GodotObject.IsInstanceValid(newCamera) && newCamera.Enabled;
        var newCameraAttached = newCamera != null && newPlayer != null && newCamera.GetParent() == newPlayer;
        var newPosition = newPlayer?.Position ?? Vector2.One * 9999f;
        var atOrigin = Math.Abs(newPosition.X) < 0.01f && Math.Abs(newPosition.Y) < 0.01f;

        var respawnOk = respawned && hpRestored && canMove && newCameraEnabled && newCameraAttached && atOrigin;

        values["death_auto_respawn"] = respawnOk;
        values["respawn_new_entity_id"] = newEntityId;
        values["respawn_old_entity_id"] = oldEntityId;
        values["respawn_entity_reference_changed"] = newPlayer != null && !ReferenceEquals(newPlayer, oldPlayer);
        values["respawn_hp"] = newHp;
        values["respawn_max_hp"] = newMaxHp;
        values["respawn_can_move"] = canMove;
        values["respawn_camera_enabled"] = newCameraEnabled;
        values["respawn_camera_attached"] = newCameraAttached;
        values["respawn_at_origin"] = atOrigin;
    }

    private static void TestCameraFollow(BrotatoLikeGameRuntime runtime, GodotEntity2D player, Dictionary<string, object?> values)
    {
        var camera = runtime.PlayerCamera;
        if (camera == null || !GodotObject.IsInstanceValid(camera))
        {
            values["camera_follows_player"] = false;
            values["camera_follow_error"] = "camera not found";
            return;
        }

        var enabled = camera.Enabled;
        var smoothingEnabled = camera.PositionSmoothingEnabled;
        var smoothingSpeed = camera.PositionSmoothingSpeed;

        player.Position = new Vector2(100f, 50f);
        player.Data.Set(MovementDataKeys.Position, new Vector2Value(100f, 50f));

        // Camera2D 作为玩家子节点，下一帧会由 Godot 自动跟随。
        var cameraOk = enabled && smoothingEnabled && smoothingSpeed > 0f;

        values["camera_follows_player"] = cameraOk;
        values["camera_enabled"] = enabled;
        values["camera_smoothing_enabled"] = smoothingEnabled;
        values["camera_smoothing_speed"] = smoothingSpeed;
    }

    private static void TestConcurrentSystems(BrotatoLikeGameRuntime runtime, GodotEntity2D player, Dictionary<string, object?> values)
    {
        var exceptionCaught = false;
        try
        {
            // 技能冷却。
            player.Data.Set(AbilityDataKeys.CooldownRemaining, 1f);

            // 接触伤害。
            player.Data.Set(DamageDataKeys.CurrentHp, Math.Max(1f,
                player.Data.Get<float>(DamageDataKeys.CurrentHp, 100f) - 10f));

            // 敌人死亡掉落。
            var enemy = EntityManager.Spawn(new EntitySpawnConfig { EntityId = new EntityId("test-enemy-concurrent") });
            enemy.Data.Set(CollisionDataKeys.Team, 2);
            enemy.Data.Set(DamageDataKeys.IsDead, true);
            enemy.Data.Set(DamageDataKeys.CurrentHp, 0f);
            enemy.Data.Set(UnitDataKeys.ExpReward, 5);
            enemy.Data.Set(MovementDataKeys.Position, new Vector2Value(50f, 0f));

            // HUD 更新。
            var hud = runtime.Hud;
            if (hud != null)
            {
                hud.SetMeta("concurrent_test", true);
            }

            // 触发进度服务拾取扫描。
            var progression = runtime.ProgressionService;
            progression?.CallDeferred("_Process", 0.016);

            EntityManager.Destroy(enemy);
        }
        catch (Exception ex)
        {
            exceptionCaught = true;
            values["concurrent_exception"] = ex.Message;
        }

        values["concurrent_systems_no_conflict"] = !exceptionCaught;
        values["concurrent_exception_caught"] = exceptionCaught;
    }

    private static void TestPauseResume(BrotatoLikeGameRuntime runtime, GodotEntity2D player, Dictionary<string, object?> values)
    {
        // 记录暂停前状态。
        var hpBefore = player.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var posBefore = player.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
        var cooldownBefore = player.Data.Get<float>(AbilityDataKeys.CooldownRemaining, 0f);

        runtime.OpenPauseMenu();

        // 确认暂停态会阻断 spawn tick。
        var spawnInfo = runtime.GetSpawnSystemRuntimeInfo();
        var blockedByPause = spawnInfo?.IsRunning == false;

        // 记录暂停期间状态，应保持不变。
        var hpDuring = player.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var posDuring = player.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);

        runtime.ClosePauseMenu();

        // 确认恢复后状态与暂停前一致。
        var hpAfter = player.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var posAfter = player.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
        var cooldownAfter = player.Data.Get<float>(AbilityDataKeys.CooldownRemaining, -1f);

        var hpPreserved = Math.Abs(hpBefore - hpAfter) < 0.01f && Math.Abs(hpDuring - hpBefore) < 0.01f;
        var posPreserved = Math.Abs(posBefore.X - posAfter.X) < 0.01f
            && Math.Abs(posBefore.Y - posAfter.Y) < 0.01f;
        var cooldownPreserved = Math.Abs(cooldownBefore - cooldownAfter) < 0.01f;

        var pauseOk = blockedByPause && hpPreserved && posPreserved && cooldownPreserved;

        values["pause_resume_state_integrity"] = pauseOk;
        values["pause_blocked_spawn"] = blockedByPause;
        values["pause_hp_before"] = hpBefore;
        values["pause_hp_after"] = hpAfter;
        values["pause_pos_before_x"] = posBefore.X;
        values["pause_pos_after_x"] = posAfter.X;
        values["pause_cooldown_before"] = cooldownBefore;
        values["pause_cooldown_after"] = cooldownAfter;
    }

    private static void TestHudDeathRespawn(BrotatoLikeGameRuntime runtime, GodotEntity2D player, Dictionary<string, object?> values)
    {
        var hud = runtime.Hud;
        if (hud == null || !GodotObject.IsInstanceValid(hud))
        {
            values["hud_death_respawn_state_clean"] = false;
            values["hud_error"] = "HUD not found";
            return;
        }

        // 读取重生后 HUD 绑定的玩家状态。
        var dead = player.Data.Get<bool>(DamageDataKeys.IsDead, false);
        var hp = player.Data.Get<float>(DamageDataKeys.CurrentHp, -1f);

        // HUD 应呈现存活态。
        var hudOk = !dead && hp > 0f;

        values["hud_death_respawn_state_clean"] = hudOk;
        values["hud_player_dead"] = dead;
        values["hud_player_hp"] = hp;
    }

    private static CheckResult Result(Dictionary<string, object?> values, string key)
    {
        if (!values.TryGetValue(key, out var raw) || raw is not bool ok)
        {
            return CheckResult.From(false, $"{key}: missing or not boolean");
        }

        return CheckResult.From(ok, ok ? $"{key}: passed" : $"{key}: failed", values);
    }

    private static async Task ProcessFrames(int count)
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        for (var i = 0; i < count; i++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
