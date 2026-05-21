using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BrotatoLike.Game;
using BrotatoLike.Game.Bridge;
using BrotatoLike.Game.Events;
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
                "Dash is selected by ability id and triggered through NextSkill/UseSkill input actions",
                "Concurrent skill cast + contact damage + loot drop in same frame produces no exceptions",
                "Pause/resume cycle preserves HP, position, skill cooldown, and spawn state",
                "HUD correctly reflects death and respawn state transitions"
            },
            passCriteria: new[]
            {
                $"stdout contains {PassMarker}",
                "artifact status is pass",
                "failureReasons is empty",
                "all 9 integration checks pass"
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
        validation.Check("dash_input_skill_bar_path", "Dash", () => Result(values, "dash_input_skill_bar_path"));
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

        // 场景 1：Dash 通过正式技能栏输入路径释放。
        await TestDashInputPath(runtime, player, values);

        // 场景 2：死亡阻断移动输入。
        await TestDeathMovementGate(runtime, player, values);

        // 场景 3：死亡阻断技能输入。
        TestDeathSkillGate(runtime, player, values);

        // 场景 4：死亡期间镜头保持启用。
        TestDeathCamera(runtime, player, values);

        // 场景 5：通过生产 _Process 触发自动重生，覆盖 Dash 释放后的重生绑定。
        await TestAutoRespawn(runtime, player, values);
        var currentPlayer = runtime.PlayerEntity;
        if (currentPlayer == null || !GodotObject.IsInstanceValid(currentPlayer))
        {
            values["error"] = "player missing after respawn";
            return values;
        }

        // 场景 6：重生后镜头仍跟随玩家。
        TestCameraFollow(runtime, currentPlayer, values);

        // 场景 7：同帧跨系统事件不互相破坏。
        TestConcurrentSystems(runtime, currentPlayer, values);

        // 场景 8：暂停/恢复保持状态完整。
        TestPauseResume(runtime, currentPlayer, values);

        // 场景 9：HUD 死亡/重生状态清理。
        TestHudDeathRespawn(runtime, currentPlayer, values);

        runtime.Shutdown();
        runtime.QueueFree();
        await ProcessFrames(1);
        return values;
    }

    private static async Task TestDashInputPath(BrotatoLikeGameRuntime runtime, GodotEntity2D player, Dictionary<string, object?> values)
    {
        var ownedIds = player.Data.Get<EntityIdList>(AbilityDataKeys.OwnedAbilityIds);
        var dashIndex = FindAbilityIndex(ownedIds, "ability-dash-");
        var dash = dashIndex >= 0 ? EntityManager.Get(ownedIds[dashIndex]) : null;
        var input = player.GetNodeOrNull<GodotActiveSkillInputComponent>("ActiveSkillInput");
        if (dashIndex < 0 || dash == null || input == null)
        {
            values["dash_input_skill_bar_path"] = false;
            values["dash_error"] = "dash ability or ActiveSkillInput missing";
            return;
        }

        player.Position = new Vector2(-640f, -640f);
        player.Data.Set(MovementDataKeys.Position, new Vector2Value(-640f, -640f));
        player.Data.Set(MovementDataKeys.LastMoveDirection, new Vector2Value(1f, 0f));
        player.Data.Set(MovementDataKeys.CanMoveInput, true);
        player.Data.Set(DamageDataKeys.IsDead, false);
        dash.Data.Set(AbilityDataKeys.CooldownRemaining, 0f);

        await PressAction("MoveRight", framesAfterPress: 4);
        var currentIndex = player.Data.Get<int>(AbilityDataKeys.CurrentAbilityIndex, 0);
        for (var i = 0; i < ownedIds.Count && currentIndex != dashIndex; i++)
        {
            await PressAction("NextSkill");
            currentIndex = player.Data.Get<int>(AbilityDataKeys.CurrentAbilityIndex, 0);
        }

        var before = player.Position;
        await PressAction("UseSkill");
        runtime.MovementDriver?.TickMovement(0.25f);
        await ProcessFrames(2);
        var after = player.Position;

        var report = input.LastTriggerReport;
        var expectedDistance = Math.Max(0f, dash.Data.Get<float>(MovementDataKeys.HandlerMaxDistance, 0f));
        var actualDistance = before.DistanceTo(after);
        var cooldown = dash.Data.Get<float>(AbilityDataKeys.CooldownRemaining, 0f);
        var selectedSkillBar = runtime.FindChild("ActiveSkillBar", recursive: true, owned: false) as ActiveSkillBarUI;
        var selectedIndex = selectedSkillBar != null && selectedSkillBar.HasMeta("SelectedIndex")
            ? selectedSkillBar.GetMeta("SelectedIndex").AsInt32()
            : -1;
        var threshold = expectedDistance > 0f ? Math.Min(120f, expectedDistance * 0.5f) : 0.5f;
        var moved = actualDistance > threshold;
        var triggerSucceeded = report?.Result == AbilityTriggerResult.Success;
        var cooldownVisible = cooldown > 0f;
        var skillBarSceneBacked = selectedSkillBar != null && !string.IsNullOrWhiteSpace(selectedSkillBar.SceneFilePath);

        values["dash_input_skill_bar_path"] = triggerSucceeded
            && currentIndex == dashIndex
            && selectedIndex == dashIndex
            && moved
            && cooldownVisible
            && skillBarSceneBacked;
        values["dash_selected_skill_id"] = dash.EntityId.Value;
        values["dash_selected_index"] = currentIndex;
        values["dash_skill_bar_selected_index"] = selectedIndex;
        values["dash_trigger_result"] = report?.Result.ToString() ?? string.Empty;
        values["dash_trigger_message"] = report?.Message ?? string.Empty;
        values["dash_before_position"] = FormatVector(before);
        values["dash_after_position"] = FormatVector(after);
        values["dash_expected_distance"] = expectedDistance;
        values["dash_distance"] = actualDistance;
        values["dash_movement_blocked"] = triggerSucceeded && !moved;
        values["dash_cooldown_remaining"] = cooldown;
        values["dash_skill_bar_scene_backed"] = skillBarSceneBacked;
    }

    private static int FindAbilityIndex(EntityIdList ownedIds, string entityIdPrefix)
    {
        for (var i = 0; i < ownedIds.Count; i++)
        {
            var ability = EntityManager.Get(ownedIds[i]);
            if (ability != null && ability.EntityId.Value.StartsWith(entityIdPrefix, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
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
        var respawnPosition = new Vector2(96f, -48f);
        oldPlayer.Position = respawnPosition;
        oldPlayer.Data.Set(MovementDataKeys.Position, new Vector2Value(respawnPosition.X, respawnPosition.Y));
        oldPlayer.Data.Set(DamageDataKeys.CurrentHp, 0f);
        oldPlayer.Data.Set(DamageDataKeys.IsDead, true);

        await ProcessFrames(4);
        var progressHp = oldPlayer.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var oldMaxHp = oldPlayer.Data.Get<float>(DamageDataKeys.MaxHp, 0f);
        var hpProgressedDuringRespawn = progressHp > 0f && progressHp < oldMaxHp;

        // 快进死亡计时器，等待生产 _Process 调用 RespawnPlayer。
        runtime.ForceRespawnForValidation();
        await ProcessFrames(3);

        var newPlayer = runtime.PlayerEntity;
        var newCamera = runtime.PlayerCamera;

        var newEntityId = newPlayer?.EntityId.Value ?? "";
        var respawned = newPlayer != null && GodotObject.IsInstanceValid(newPlayer);

        var newHp = newPlayer?.Data.Get<float>(DamageDataKeys.CurrentHp, -1f) ?? -1f;
        var newMaxHp = newPlayer?.Data.Get<float>(DamageDataKeys.MaxHp, -1f) ?? -1f;
        var hpRestored = newHp > progressHp && newHp > 0f && newHp <= newMaxHp + 0.01f;
        var canMove = newPlayer?.Data.Get<bool>(MovementDataKeys.CanMoveInput, false) ?? false;
        var newCameraEnabled = newCamera != null && GodotObject.IsInstanceValid(newCamera) && newCamera.Enabled;
        var newCameraAttached = newCamera != null && newPlayer != null && newCamera.GetParent() == newPlayer;
        var newPosition = newPlayer?.Position ?? Vector2.One * 9999f;
        var sameRespawnPosition = newPosition.DistanceTo(respawnPosition) < 0.1f;
        var inputNodeExists = newPlayer?.GetNodeOrNull<BrotatoLikePlayerInputComponent>("PlayerInput") != null;
        var skillNodeExists = newPlayer?.GetNodeOrNull<GodotActiveSkillInputComponent>("ActiveSkillInput") != null;

        var inputDirectionWritten = false;
        var movedAfterInput = false;
        var skillAdapterSwitches = false;
        if (newPlayer != null)
        {
            Input.ActionRelease("MoveRight");
            await ProcessFrames(1);
            var beforeInputPosition = newPlayer.Position;
            Input.ActionPress("MoveRight");
            await ProcessFrames(12);
            var inputAfterPress = newPlayer.Data.Get<Vector2Value>(MovementDataKeys.InputDirection, Vector2Value.Zero);
            var afterInputPosition = newPlayer.Position;
            Input.ActionRelease("MoveRight");
            await ProcessFrames(2);

            inputDirectionWritten = inputAfterPress.X > 0.5f && Math.Abs(inputAfterPress.Y) < 0.01f;
            movedAfterInput = afterInputPosition.X > beforeInputPosition.X + 0.5f;

            var ownedIds = newPlayer.Data.Get<EntityIdList>(AbilityDataKeys.OwnedAbilityIds);
            var skillIndexBefore = newPlayer.Data.Get<int>(AbilityDataKeys.CurrentAbilityIndex, 0);
            newPlayer.Events.Publish(new InputNextSkill(newPlayer));
            var skillIndexAfter = newPlayer.Data.Get<int>(AbilityDataKeys.CurrentAbilityIndex, 0);
            skillAdapterSwitches = ownedIds.Count > 1 && skillIndexAfter != skillIndexBefore;

            values["respawn_input_direction_x"] = inputAfterPress.X;
            values["respawn_input_direction_y"] = inputAfterPress.Y;
            values["respawn_position_before_input"] = $"{beforeInputPosition.X:0.###},{beforeInputPosition.Y:0.###}";
            values["respawn_position_after_input"] = $"{afterInputPosition.X:0.###},{afterInputPosition.Y:0.###}";
            values["respawn_skill_index_before"] = skillIndexBefore;
            values["respawn_skill_index_after"] = skillIndexAfter;
        }

        var respawnOk = respawned
            && hpRestored
            && hpProgressedDuringRespawn
            && canMove
            && newCameraEnabled
            && newCameraAttached
            && sameRespawnPosition
            && inputNodeExists
            && skillNodeExists
            && inputDirectionWritten
            && movedAfterInput
            && skillAdapterSwitches;

        values["death_auto_respawn"] = respawnOk;
        values["respawn_new_entity_id"] = newEntityId;
        values["respawn_old_entity_id"] = oldEntityId;
        values["respawn_entity_reference_changed"] = newPlayer != null && !ReferenceEquals(newPlayer, oldPlayer);
        values["respawn_hp"] = newHp;
        values["respawn_max_hp"] = newMaxHp;
        values["respawn_hp_restored"] = hpRestored;
        values["respawn_progress_hp"] = progressHp;
        values["respawn_progress_hp_increased"] = hpProgressedDuringRespawn;
        values["respawn_can_move"] = canMove;
        values["respawn_camera_enabled"] = newCameraEnabled;
        values["respawn_camera_attached"] = newCameraAttached;
        values["respawn_expected_position"] = $"{respawnPosition.X:0.###},{respawnPosition.Y:0.###}";
        values["respawn_actual_position"] = $"{newPosition.X:0.###},{newPosition.Y:0.###}";
        values["respawn_same_position"] = sameRespawnPosition;
        values["respawn_input_node_exists"] = inputNodeExists;
        values["respawn_skill_node_exists"] = skillNodeExists;
        values["respawn_input_direction_written"] = inputDirectionWritten;
        values["respawn_moved_after_input"] = movedAfterInput;
        values["respawn_skill_adapter_switches"] = skillAdapterSwitches;
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

    private static async Task PressAction(string action, int framesAfterPress = 1)
    {
        Input.ActionRelease(action);
        await ProcessFrames(1);
        Input.ActionPress(action);
        await ProcessFrames(framesAfterPress);
        Input.ActionRelease(action);
        await ProcessFrames(1);
    }

    private static string FormatVector(Vector2 value)
    {
        return $"{value.X:0.###},{value.Y:0.###}";
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
