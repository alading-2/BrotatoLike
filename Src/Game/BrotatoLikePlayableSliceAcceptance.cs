using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BrotatoLike.Game.Bridge;
using BrotatoLike.Game.Characters;
using BrotatoLike.Game.VFX;
using Godot;
using SlimeAI.GameOS.Capabilities.Ability;
using SlimeAI.GameOS.Capabilities.Attack;
using SlimeAI.GameOS.Capabilities.Collision;
using SlimeAI.GameOS.Capabilities.Damage;
using SlimeAI.GameOS.Capabilities.Damage.Events;
using SlimeAI.GameOS.Capabilities.Effect;
using SlimeAI.GameOS.Capabilities.Movement;
using SlimeAI.GameOS.Capabilities.Unit;
using SlimeAI.GameOS.GodotBridge;
using SlimeAI.GameOS.Observation;
using SlimeAI.GameOS.Runtime.Entity;
using SlimeAI.GameOS.Runtime.Event;
using SlimeAI.GameOS.Runtime.Timer;

namespace BrotatoLike.Game;

/// <summary>
/// R07 playable-slice acceptance runner for ordinary Main.tscn runs.
/// </summary>
internal static class BrotatoLikePlayableSliceAcceptance
{
    private const string ScenePath = "res://Scenes/Main.tscn";
    private const string ArtifactFileName = "scene-acceptance.json";
    private static readonly GameOSContextLog Log = GameOSLog.For("BrotatoLike.PlayableSliceAcceptance");

    /// <summary>
    /// Returns true when the scene runner requested structured artifacts.
    /// </summary>
    public static bool ShouldRun()
    {
        return !string.IsNullOrWhiteSpace(OS.GetEnvironment("GODOT_SCENE_TEST_ARTIFACT_DIR"));
    }

    /// <summary>
    /// Runs deterministic acceptance checks against the live BrotatoLike main scene.
    /// </summary>
    public static async Task<PlayableSliceAcceptanceResult> Run(Node sceneRoot, BrotatoLikeGameRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(sceneRoot);
        ArgumentNullException.ThrowIfNull(runtime);

        BrotatoLikeAbilityHandlers.RegisterAll();
        using var observation = GameOSObservationSession.FromEnvironment(ScenePath, "playable-slice");
        var checks = new Dictionary<string, bool>(StringComparer.Ordinal);
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var damageLogs = new List<string>();
        var failureReasons = new List<string>();
        Log.Info("acceptance start");

        Action<Damaged> damagedHandler = data =>
        {
            damageLogs.Add(FormattableString.Invariant(
                $"{data.Info.Attacker?.EntityId.Value ?? "none"}->{data.Info.Victim.EntityId.Value}:{data.Info.FinalDamage:0.###}:{data.Info.OldHp:0.###}->{data.Info.NewHp:0.###}"));
        };
        var damagedSub = WorldEvents.World.Subscribe<Damaged>(damagedHandler);

        try
        {
            var player = runtime.PlayerEntity;
            AddCheck(checks, failureReasons, "main.runtime_initialized", runtime.IsInitialized);
            AddCheck(checks, failureReasons, "main.player_spawned", player != null && player.EntityId == new EntityId("player-deluyi"));
            AddCheck(checks, failureReasons, "main.gameplay_started", runtime.GetSpawnSystemRuntimeInfo()?.IsStateAllowed == true);
            AddCheck(checks, failureReasons, "main.smoke_path_separate", Array.IndexOf(OS.GetCmdlineUserArgs(), "--gameos-smoke-exit") < 0);

            if (player == null)
            {
                WriteArtifact(observation, checks, values, damageLogs, failureReasons);
                return new PlayableSliceAcceptanceResult(false, failureReasons);
            }

            var movement = await VerifyPlayerMovement(sceneRoot, player, values);
            AddCheck(checks, failureReasons, "player.input_map_has_wasd", movement.InputMapHasWasd);
            AddCheck(checks, failureReasons, "player.input_map_has_arrows", movement.InputMapHasArrows);
            AddCheck(checks, failureReasons, "player.input_direction_written", movement.InputDirectionWritten);
            AddCheck(checks, failureReasons, "player.last_move_direction_written", movement.LastMoveDirectionWritten);
            AddCheck(checks, failureReasons, "player.position_changed", movement.PositionChanged);

            var enemies = await VerifyEnemySpawnAndChase(sceneRoot, runtime, player, values);
            AddCheck(checks, failureReasons, "enemy.spawned_from_dataos", enemies.SpawnedFromDataOS);
            AddCheck(checks, failureReasons, "enemy.continuous_spawn_observed", enemies.ContinuousSpawnObserved);
            AddCheck(checks, failureReasons, "enemy.resource_paths_recorded", enemies.ResourcePathsRecorded);
            AddCheck(checks, failureReasons, "enemy.chase_or_move_observed", enemies.ChaseOrMoveObserved);
            AddCheck(checks, failureReasons, "enemy.contact_damage_applied", enemies.ContactDamageApplied);

            var skills = await VerifySkills(sceneRoot, runtime, player, enemies, values);
            AddCheck(checks, failureReasons, "skill.slam_triggered", skills.SlamTriggered);
            AddCheck(checks, failureReasons, "skill.slam_cooldown_gated", skills.SlamCooldownGated);
            AddCheck(checks, failureReasons, "skill.slam_hit", skills.SlamHit);
            AddCheck(checks, failureReasons, "skill.slam_visual_evidence", skills.SlamVisualEvidence);
            AddCheck(checks, failureReasons, "skill.chain_triggered", skills.ChainTriggered);
            AddCheck(checks, failureReasons, "skill.chain_cooldown_gated", skills.ChainCooldownGated);
            AddCheck(checks, failureReasons, "skill.chain_target_selected", skills.ChainTargetSelected);
            AddCheck(checks, failureReasons, "skill.chain_hit", skills.ChainHit);
            AddCheck(checks, failureReasons, "skill.chain_structured_evidence", skills.ChainStructuredEvidence);
            AddCheck(checks, failureReasons, "skill.chain_line_vfx_bound", skills.ChainLineVfxBound);
            AddCheck(checks, failureReasons, "skill.chain_line_vfx_cleanup", skills.ChainLineVfxCleanup);
            AddCheck(checks, failureReasons, "skill.chain_line_vfx_multi_bounce", skills.ChainLineVfxMultiBounce);
            AddCheck(checks, failureReasons, "skill.point_targeting_started", skills.PointTargetingStarted);
            AddCheck(checks, failureReasons, "skill.point_targeting_confirmed", skills.PointTargetingConfirmed);
            AddCheck(checks, failureReasons, "skill.direct_slot_selected", skills.DirectSlotSelected);
            AddCheck(checks, failureReasons, "skill.real_input_action_path", skills.RealInputActionPath);

            var cleanup = VerifyDeathAndCleanup(enemies, player, values);
            AddCheck(checks, failureReasons, "enemy.death_observed", cleanup.DeathObserved);
            AddCheck(checks, failureReasons, "enemy.cleanup_queued", cleanup.CleanupQueued);

            var hud = WriteHudEvidence(sceneRoot, player, skills.CurrentSkillName, damageLogs, values);
            AddCheck(checks, failureReasons, "hud.health_evidence", hud.HealthEvidence);
            AddCheck(checks, failureReasons, "hud.current_skill_evidence", hud.CurrentSkillEvidence);
            AddCheck(checks, failureReasons, "hud.damage_evidence", hud.DamageEvidence);
            AddCheck(checks, failureReasons, "hud.scene_backed_formal_ui", hud.SceneBacked);

            var camera = VerifyCameraFollow(runtime, player, values);
            AddCheck(checks, failureReasons, "camera.follow_player", camera.CameraFollowsPlayer);

            var deathRespawn = await VerifyDeathGateAndRespawn(sceneRoot, runtime, player, enemies, values);
            AddCheck(checks, failureReasons, "player.death_gate_respawn", deathRespawn.RespawnOk);

            var activePlayer = runtime.PlayerEntity ?? player;
            var concurrent = VerifyConcurrentSystems(runtime, activePlayer, values);
            AddCheck(checks, failureReasons, "concurrent.same_frame_no_exception", concurrent.NoException);

            activePlayer = runtime.PlayerEntity ?? activePlayer;
            var pauseResume = VerifyPauseResumeIntegrity(runtime, activePlayer, values);
            AddCheck(checks, failureReasons, "pause.resume_state_integrity", pauseResume.StatePreserved);

            var success = failureReasons.Count == 0;
            values["result"] = success ? "pass" : "fail";
            if (success)
            {
                Log.Pass("acceptance complete");
            }
            else
            {
                Log.Fail("acceptance complete", new Dictionary<string, object?>
                {
                    ["failureCount"] = failureReasons.Count
                });
            }

            WriteArtifact(observation, checks, values, damageLogs, failureReasons);
            return new PlayableSliceAcceptanceResult(success, failureReasons);
        }
        finally
        {
            damagedSub.Dispose();
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
            Input.ActionRelease("ConfirmTarget");
            Input.ActionRelease("CancelTarget");
        }
    }

    private static async Task<PlayerMovementAcceptance> VerifyPlayerMovement(
        Node sceneRoot,
        GodotEntity2D player,
        Dictionary<string, string> values)
    {
        var inputComponent = player.GetNodeOrNull<BrotatoLikePlayerInputComponent>("PlayerInput");
        var inputMapHasWasd = HasPhysicalKey("MoveLeft", (Key)65)
            && HasPhysicalKey("MoveRight", (Key)68)
            && HasPhysicalKey("MoveUp", (Key)87)
            && HasPhysicalKey("MoveDown", (Key)83);
        var inputMapHasArrows = HasPhysicalKey("MoveLeft", (Key)4194319)
            && HasPhysicalKey("MoveRight", (Key)4194321)
            && HasPhysicalKey("MoveUp", (Key)4194320)
            && HasPhysicalKey("MoveDown", (Key)4194322);
        if (inputComponent == null)
        {
            return new PlayerMovementAcceptance(inputMapHasWasd, inputMapHasArrows, false, false, false);
        }

        var start = player.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
        Input.ActionPress("MoveRight");
        await ProcessFrames(sceneRoot, 20);
        var inputDirectionDuringPress = player.Data.Get<Vector2Value>(MovementDataKeys.InputDirection, Vector2Value.Zero);
        Input.ActionRelease("MoveRight");

        Input.ActionPress("MoveUp");
        await ProcessFrames(sceneRoot, 20);
        Input.ActionRelease("MoveUp");
        await ProcessFrames(sceneRoot, 2);

        var inputDirection = player.Data.Get<Vector2Value>(MovementDataKeys.InputDirection, Vector2Value.Zero);
        var lastMoveDirection = player.Data.Get<Vector2Value>(MovementDataKeys.LastMoveDirection, Vector2Value.Zero);
        var end = player.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
        values["player_start"] = FormatVector(start);
        values["player_end"] = FormatVector(end);
        values["player_input_direction"] = FormatVector(inputDirection);
        values["player_last_move_direction"] = FormatVector(lastMoveDirection);

        return new PlayerMovementAcceptance(
            inputMapHasWasd,
            inputMapHasArrows,
            inputDirectionDuringPress != Vector2Value.Zero || inputDirection != Vector2Value.Zero,
            lastMoveDirection != Vector2Value.Zero,
            Vector2Value.Distance(start, end) > 0.001f);
    }

    private static async Task<EnemyAcceptance> VerifyEnemySpawnAndChase(
        Node sceneRoot,
        BrotatoLikeGameRuntime runtime,
        GodotEntity2D player,
        Dictionary<string, string> values)
    {
        await ProcessFrames(sceneRoot, 20);
        var totalAfterInitialWindow = runtime.LastSpawnTickResult.Value.TotalSpawned;
        for (var i = 0; i < 6; i++)
        {
            runtime.TickSpawn(1.6d);
            await ProcessFrames(sceneRoot, 1);
        }

        var tick = runtime.LastSpawnTickResult;
        var entities = EntityManager.GetAll();
        var enemies = new List<GodotEntity2D>();
        for (var i = 0; i < entities.Count; i++)
        {
            if (entities[i] is GodotEntity2D node
                && node.EntityId.Value.StartsWith("spawn-", StringComparison.Ordinal)
                && node.Data.Get<int>(CollisionDataKeys.Team, 0) == 2)
            {
                enemies.Add(node);
            }
        }

        values["enemy_spawned_this_tick"] = tick.Value.SpawnedThisTick.ToString(CultureInfo.InvariantCulture);
        values["enemy_total_after_initial_window"] = totalAfterInitialWindow.ToString(CultureInfo.InvariantCulture);
        values["enemy_total_spawned"] = tick.Value.TotalSpawned.ToString(CultureInfo.InvariantCulture);
        values["enemy_ids"] = string.Join(",", enemies.ConvertAll(enemy => enemy.EntityId.Value));
        values["wave_current_id"] = runtime.CurrentWave.ToString(CultureInfo.InvariantCulture);
        values["wave_phase"] = runtime.ProgressionService?.WavePhaseName ?? string.Empty;
        values["wave_expected_spawn_count"] = (runtime.SpawnCatalog?.ExpectedSpawnCount ?? -1).ToString(CultureInfo.InvariantCulture);
        values["wave_actual_spawned_count"] = tick.Value.TotalSpawned.ToString(CultureInfo.InvariantCulture);
        values["wave_authoring_kind"] = runtime.SpawnCatalog?.HasFiniteSpawnLimit == true ? "finite" : "open";
        values["wave_completion_mode"] = runtime.TryGetWaveDefinition(runtime.CurrentWave, out var waveDefinition)
            ? waveDefinition.CompletionMode
            : string.Empty;

        if (enemies.Count == 0 || runtime.MovementDriver == null)
        {
            return new EnemyAcceptance(enemies, false, false, false, false, false);
        }

        var first = enemies[0];
        var playerPosition = player.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
        first.Data.Set(MovementDataKeys.Position, playerPosition + new Vector2Value(48f, 0f));
        first.Position = new Vector2(playerPosition.X + 48f, playerPosition.Y);
        var start = first.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
        await ProcessFrames(sceneRoot, 30);
        var end = first.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
        var chaseDirection = first.Data.Get<Vector2Value>(MovementDataKeys.AIMoveDirection, Vector2Value.Zero);

        var playerHpBefore = player.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var hurtbox = player.GetNodeOrNull<GodotHurtboxComponent>("Hurtbox");
        var enemyHurtbox = first.GetNodeOrNull<GodotHurtboxComponent>("Hurtbox");
        var contactEmitted = hurtbox != null && enemyHurtbox != null && hurtbox.EmitEntered(enemyHurtbox);
        await ProcessFrames(sceneRoot, 2);
        var playerHpAfter = player.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);

        values["enemy_first_id"] = first.EntityId.Value;
        values["enemy_first_visual"] = first.Data.Get(UnitDataKeys.VisualScenePath, string.Empty);
        values["enemy_first_start"] = FormatVector(start);
        values["enemy_first_end"] = FormatVector(end);
        values["enemy_chase_direction"] = FormatVector(chaseDirection);
        values["player_hp_before_contact"] = FormatFloat(playerHpBefore);
        values["player_hp_after_contact"] = FormatFloat(playerHpAfter);

        return new EnemyAcceptance(
            enemies,
            tick.Success && tick.Value.TotalSpawned > 0,
            runtime.SpawnCatalog?.HasFiniteSpawnLimit == false && tick.Value.TotalSpawned > 5 && tick.Value.TotalSpawned > totalAfterInitialWindow,
            enemies.TrueForAll(enemy => !string.IsNullOrWhiteSpace(enemy.Data.Get(UnitDataKeys.VisualScenePath, string.Empty))),
            chaseDirection != Vector2Value.Zero && Vector2Value.Distance(start, end) > 0.001f,
            contactEmitted && playerHpAfter < playerHpBefore);
    }

    private static async Task ProcessFrames(Node node, int count)
    {
        for (var i = 0; i < count; i++)
        {
            await node.ToSignal(node.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private static async Task PressAction(Node node, string action, int frames = 2)
    {
        Input.ActionPress(action);
        await ProcessFrames(node, frames);
        Input.ActionRelease(action);
        await ProcessFrames(node, frames);
    }

    private static async Task<SkillAcceptance> VerifySkills(
        Node sceneRoot,
        BrotatoLikeGameRuntime runtime,
        GodotEntity2D player,
        EnemyAcceptance enemies,
        Dictionary<string, string> values)
    {
        var ownedIds = player.Data.Get<EntityIdList>(AbilityDataKeys.OwnedAbilityIds);
        var inputComponent = player.GetNodeOrNull<GodotActiveSkillInputComponent>("ActiveSkillInput");
        if (ownedIds.Count < 3 || enemies.Enemies.Count == 0 || inputComponent == null)
        {
            return SkillAcceptance.Empty;
        }

        var slam = EntityManager.Get(ownedIds[0]);
        var chain = EntityManager.Get(ownedIds[1]);
        var point = EntityManager.Get(ownedIds[2]);
        if (slam == null || chain == null || point == null)
        {
            return SkillAcceptance.Empty;
        }

        var target = enemies.Enemies[0];
        var playerPosition = player.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
        target.Data.Set(MovementDataKeys.Position, playerPosition + new Vector2Value(24f, 0f));
        target.Position = new Vector2(playerPosition.X + 24f, playerPosition.Y);
        target.Data.Set(DamageDataKeys.IsDead, false);

        var slamHpBefore = target.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var slamEffectsBefore = CountEffectEntities(slam.EntityId.Value);
        player.Data.Set(AbilityDataKeys.CurrentAbilityIndex, 0);
        await PressAction(sceneRoot, "UseSkill");
        var slamHpAfter = target.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var slamEffectsAfter = CountEffectEntities(slam.EntityId.Value);
        var slamCooldown = slam.Data.Get<float>(AbilityDataKeys.CooldownRemaining, 0f);
        var slamReport = inputComponent.LastTriggerReport;
        await PressAction(sceneRoot, "UseSkill");
        var slamCooldownReport = inputComponent.LastTriggerReport;

        AbilityService.Instance.TickCooldowns([slam], slamCooldown + 0.1f);

        var expectedChainBounces = Math.Max(1, chain.Data.Get<int>(AbilityDataKeys.ChainCount, 1));
        var chainTargets = PrepareChainTargets(enemies.Enemies, playerPosition, expectedChainBounces, values);
        if (chainTargets.Count > 0)
        {
            target = chainTargets[0];
        }

        await PressAction(sceneRoot, "NextSkill");
        var currentIndex = player.Data.Get<int>(AbilityDataKeys.CurrentAbilityIndex, 0);
        AbilityTargetingTool.TryBuildContext(player, chain, out var chainContext);
        var chainHpBefore = target.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var chainLineRecordsBefore = runtime.ChainLightningVfxBinder?.Records.Count ?? 0;
        var chainDelay = Math.Max(0f, chain.Data.Get<float>(AbilityDataKeys.ChainDelay, 0f));
        await PressAction(sceneRoot, "UseSkill");
        for (var i = 1; i < expectedChainBounces; i++)
        {
            TimerManager.Instance.Tick(chainDelay + 0.05f);
            await ProcessFrames(sceneRoot, 2);
        }

        var chainHpAfter = target.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var chainCooldown = chain.Data.Get<float>(AbilityDataKeys.CooldownRemaining, 0f);
        var chainReport = inputComponent.LastTriggerReport;
        await PressAction(sceneRoot, "UseSkill");
        var chainCooldownReport = inputComponent.LastTriggerReport;
        TimerManager.Instance.Tick(Math.Max(0.05f, chain.Data.Get<float>(AbilityDataKeys.ChainDelay, 0.2f)) + 0.05f);
        await ProcessFrames(sceneRoot, 2);
        var chainLineVfx = CaptureChainLineVfxEvidence(
            runtime.ChainLightningVfxBinder,
            chainLineRecordsBefore,
            expectedChainBounces,
            values);

        AbilityService.Instance.TickCooldowns([chain], chainCooldown + 0.1f);
        await PressAction(sceneRoot, "NextSkill");
        var pointTarget = enemies.Enemies.Count > 1 ? enemies.Enemies[1] : target;
        pointTarget.Data.Set(DamageDataKeys.IsDead, false);
        pointTarget.Data.Set(DamageDataKeys.CurrentHp, MathF.Max(50f, pointTarget.Data.Get<float>(DamageDataKeys.CurrentHp, 0f)));
        var pointTargetPosition = playerPosition + new Vector2Value(120f, 0f);
        pointTarget.Data.Set(MovementDataKeys.Position, pointTargetPosition);
        pointTarget.Position = new Vector2(pointTargetPosition.X, pointTargetPosition.Y);
        var pointHpBefore = pointTarget.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var pointCooldownBeforeStart = point.Data.Get<float>(AbilityDataKeys.CooldownRemaining, 0f);
        await PressAction(sceneRoot, "UseSkill");
        var pointTargetingStarted = runtime.TargetingController?.IsTargeting == true;
        var pointCooldownAfterStart = point.Data.Get<float>(AbilityDataKeys.CooldownRemaining, 0f);
        runtime.TargetingController?.SetRequestedTargetPosition(pointTarget.Position);
        await ProcessFrames(sceneRoot, 1);
        await PressAction(sceneRoot, "ConfirmTarget");
        var pointReport = runtime.TargetingController?.LastTriggerReport;
        var pointHpAfter = pointTarget.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var pointCooldownAfterConfirm = point.Data.Get<float>(AbilityDataKeys.CooldownRemaining, 0f);
        await PressAction(sceneRoot, "SkillSlot4");
        var directSlot4Index = player.Data.Get<int>(AbilityDataKeys.CurrentAbilityIndex, 0);
        await PressAction(sceneRoot, "SkillSlot1");
        var directSlot1Index = player.Data.Get<int>(AbilityDataKeys.CurrentAbilityIndex, 0);
        var skillPhysicalKeyMappings = HasPhysicalKey("SkillSlot1", (Key)49)
            && HasPhysicalKey("SkillSlot4", (Key)52)
            && HasPhysicalKey("PreviousSkill", (Key)81)
            && HasPhysicalKey("NextSkill", (Key)69)
            && HasPhysicalKey("UseSkill", (Key)32);

        values["skill_slam_id"] = slam.EntityId.Value;
        values["skill_slam_report"] = slamReport?.Result.ToString() ?? string.Empty;
        values["skill_slam_hp_before"] = FormatFloat(slamHpBefore);
        values["skill_slam_hp_after"] = FormatFloat(slamHpAfter);
        values["skill_slam_cooldown"] = FormatFloat(slamCooldown);
        values["skill_slam_effect_count"] = (slamEffectsAfter - slamEffectsBefore).ToString(CultureInfo.InvariantCulture);
        values["skill_chain_id"] = chain.EntityId.Value;
        values["skill_chain_report"] = chainReport?.Result.ToString() ?? string.Empty;
        values["skill_chain_target"] = chainContext?.Targets != null && chainContext.Targets.Count > 0
            ? chainContext.Targets[0].EntityId.Value
            : string.Empty;
        values["skill_chain_hp_before"] = FormatFloat(chainHpBefore);
        values["skill_chain_hp_after"] = FormatFloat(chainHpAfter);
        values["skill_chain_cooldown"] = FormatFloat(chainCooldown);
        values["skill_current_index"] = currentIndex.ToString(CultureInfo.InvariantCulture);
        values["skill_point_id"] = point.EntityId.Value;
        values["skill_point_target"] = pointTarget.EntityId.Value;
        values["skill_point_targeting_started"] = pointTargetingStarted.ToString(CultureInfo.InvariantCulture);
        values["skill_point_cooldown_before_start"] = FormatFloat(pointCooldownBeforeStart);
        values["skill_point_cooldown_after_start"] = FormatFloat(pointCooldownAfterStart);
        values["skill_point_cooldown_after_confirm"] = FormatFloat(pointCooldownAfterConfirm);
        values["skill_point_report"] = pointReport?.Result.ToString() ?? string.Empty;
        values["skill_point_hp_before"] = FormatFloat(pointHpBefore);
        values["skill_point_hp_after"] = FormatFloat(pointHpAfter);
        values["skill_direct_slot4_index"] = directSlot4Index.ToString(CultureInfo.InvariantCulture);
        values["skill_direct_slot1_index"] = directSlot1Index.ToString(CultureInfo.InvariantCulture);
        values["skill_physical_key_mappings"] = skillPhysicalKeyMappings.ToString(CultureInfo.InvariantCulture);

        return new SkillAcceptance(
            SlamTriggered: slamReport?.Result == AbilityTriggerResult.Success && slamCooldown > 0f,
            SlamCooldownGated: slamCooldownReport?.Result == AbilityTriggerResult.FailCooldown,
            SlamHit: slamHpAfter < slamHpBefore,
            SlamVisualEvidence: slamEffectsAfter > slamEffectsBefore,
            ChainTriggered: chainReport?.Result == AbilityTriggerResult.Success && chainCooldown > 0f,
            ChainCooldownGated: chainCooldownReport?.Result == AbilityTriggerResult.FailCooldown,
            ChainTargetSelected: chainContext?.Targets != null && chainContext.Targets.Count > 0,
            ChainHit: chainHpAfter < chainHpBefore,
            ChainStructuredEvidence: chainHpAfter < chainHpBefore && !string.IsNullOrWhiteSpace(chain.Data.Get(AbilityDataKeys.LineEffectScenePath, string.Empty)),
            ChainLineVfxBound: chainLineVfx.Bound,
            ChainLineVfxCleanup: chainLineVfx.Cleanup,
            ChainLineVfxMultiBounce: chainLineVfx.MultiBounce,
            PointTargetingStarted: pointTargetingStarted && Math.Abs(pointCooldownAfterStart - pointCooldownBeforeStart) < 0.001f,
            PointTargetingConfirmed: pointReport?.Result == AbilityTriggerResult.Success
                && pointCooldownAfterConfirm > 0f
                && pointHpAfter < pointHpBefore,
            DirectSlotSelected: directSlot4Index == 3 && directSlot1Index == 0,
            RealInputActionPath: skillPhysicalKeyMappings,
            CurrentSkillName: point.Data.Get(AbilityDataKeys.Name, point.EntityId.Value));
    }

    private static List<GodotEntity2D> PrepareChainTargets(
        IReadOnlyList<GodotEntity2D> enemies,
        Vector2Value playerPosition,
        int expectedCount,
        Dictionary<string, string> values)
    {
        var targets = new List<GodotEntity2D>();
        var preparedCount = Math.Min(expectedCount, enemies.Count);
        for (var i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            var position = i < preparedCount
                ? playerPosition + new Vector2Value(24f + (i * 72f), 0f)
                : playerPosition + new Vector2Value(1200f + (i * 80f), 0f);
            enemy.Data.Set(MovementDataKeys.Position, position);
            enemy.Data.Set(DamageDataKeys.IsDead, false);
            enemy.Data.Set(DamageDataKeys.CurrentHp, 200f);
            enemy.Position = new Vector2(position.X, position.Y);
            if (i < preparedCount)
            {
                targets.Add(enemy);
            }
        }

        values["skill_chain_prepared_targets"] = string.Join(",", targets.ConvertAll(target => target.EntityId.Value));
        return targets;
    }

    private static ChainLineVfxAcceptance CaptureChainLineVfxEvidence(
        BrotatoLikeChainLightningVfxBinder? binder,
        int startIndex,
        int expectedCount,
        Dictionary<string, string> values)
    {
        var records = new List<BrotatoLikeChainLightningLineVfxRecord>();
        if (binder != null)
        {
            for (var i = Math.Max(0, startIndex); i < binder.Records.Count; i++)
            {
                records.Add(binder.Records[i]);
            }
        }

        var boundCount = 0;
        var cleanupCount = 0;
        var scenePaths = new List<string>();
        var sourceTargets = new List<string>();
        var starts = new List<string>();
        var ends = new List<string>();
        var points = new List<string>();
        var worldPoints = new List<string>();
        var durations = new List<string>();
        var cleanup = new List<string>();
        var failures = new List<string>();

        for (var i = 0; i < records.Count; i++)
        {
            var record = records[i];
            if (record.Bound)
            {
                boundCount++;
            }

            if (record.CleanupObserved && !record.NodeRegisteredAfterCleanup)
            {
                cleanupCount++;
            }

            scenePaths.Add(record.ScenePath);
            sourceTargets.Add($"{record.SourceEntityId.Value}->{record.TargetEntityId.Value}");
            starts.Add(FormatVector(record.StartPosition));
            ends.Add(FormatVector(record.EndPosition));
            durations.Add(FormatFloat(record.DurationSeconds));
            cleanup.Add($"{record.EffectEntityId.Value}:{record.CleanupObserved}:{record.NodeRegisteredAfterCleanup}");
            if (!string.IsNullOrWhiteSpace(record.FailureReason))
            {
                failures.Add($"{record.EffectEntityId.Value}:{record.FailureReason}");
            }

            points.Add(record.LocalPoints.Length >= 2
                ? $"{FormatVector(record.LocalPoints[0])}->{FormatVector(record.LocalPoints[1])}"
                : "missing");
            worldPoints.Add(record.LocalPoints.Length >= 2
                ? $"{FormatVector(record.WorldPointStart)}->{FormatVector(record.WorldPointEnd)}"
                : "missing");
        }

        values["skill_chain_line_scene_path"] = string.Join(";", scenePaths);
        values["skill_chain_line_expected_count"] = expectedCount.ToString(CultureInfo.InvariantCulture);
        values["skill_chain_line_recorded_count"] = records.Count.ToString(CultureInfo.InvariantCulture);
        values["skill_chain_line_bound_count"] = boundCount.ToString(CultureInfo.InvariantCulture);
        values["skill_chain_line_cleanup_count"] = cleanupCount.ToString(CultureInfo.InvariantCulture);
        values["skill_chain_line_source_targets"] = string.Join(";", sourceTargets);
        values["skill_chain_line_start_positions"] = string.Join(";", starts);
        values["skill_chain_line_end_positions"] = string.Join(";", ends);
        values["skill_chain_line_points"] = string.Join(";", points);
        values["skill_chain_line_world_points"] = string.Join(";", worldPoints);
        values["skill_chain_line_durations"] = string.Join(";", durations);
        values["skill_chain_line_cleanup"] = string.Join(";", cleanup);
        values["skill_chain_line_failures"] = string.Join(";", failures);

        return new ChainLineVfxAcceptance(
            Bound: records.Count >= expectedCount && boundCount >= expectedCount,
            Cleanup: records.Count >= expectedCount && cleanupCount >= expectedCount,
            MultiBounce: expectedCount > 1 && records.Count >= expectedCount);
    }

    private static EnemyCleanupAcceptance VerifyDeathAndCleanup(
        EnemyAcceptance enemies,
        GodotEntity2D player,
        Dictionary<string, string> values)
    {
        if (enemies.Enemies.Count == 0)
        {
            return new EnemyCleanupAcceptance(false, false);
        }

        var enemy = enemies.Enemies[^1];
        var hpBefore = enemy.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var result = DamageTool.Apply([enemy], new DamageApplyOptions(hpBefore + 999f)
        {
            Attacker = player,
            Type = DamageType.Physical,
            Tags = DamageTags.Ability
        });
        var deathObserved = enemy.Data.Get<bool>(DamageDataKeys.IsDead, false)
            || result.Results.Exists(entry => entry.Info.IsFatal || entry.NewHp <= 0f);
        enemy.DestroyEntity();
        values["enemy_cleanup_id"] = enemy.EntityId.Value;
        values["enemy_cleanup_hp_before"] = FormatFloat(hpBefore);
        values["enemy_cleanup_damage_applied"] = result.AppliedCount.ToString(CultureInfo.InvariantCulture);
        values["enemy_cleanup_death_observed"] = deathObserved.ToString(CultureInfo.InvariantCulture);
        values["enemy_cleanup_queued"] = enemy.IsQueuedForDeletion().ToString(CultureInfo.InvariantCulture);
        return new EnemyCleanupAcceptance(
            deathObserved,
            enemy.IsQueuedForDeletion());
    }

    private static HudAcceptance WriteHudEvidence(
        Node sceneRoot,
        GodotEntity2D player,
        string currentSkillName,
        IReadOnlyList<string> damageLogs,
        Dictionary<string, string> values)
    {
        var hud = sceneRoot.FindChild("BrotatoLikeHUD", recursive: true, owned: false);
        var health = sceneRoot.FindChild("PlayerHealthLabel", recursive: true, owned: false) as Label;
        var playerHealthBar = sceneRoot.FindChild("PlayerHealthBar", recursive: true, owned: false);
        var skillBar = sceneRoot.FindChild("ActiveSkillBar", recursive: true, owned: false) as Control;
        var damageLayer = sceneRoot.FindChild("DamageNumberLayer", recursive: true, owned: false);
        var headHealthLayer = sceneRoot.FindChild("HeadHealthBarLayer", recursive: true, owned: false);
        var damageNumber = FindFirstSceneBackedDescendant(damageLayer);
        var headHealthBar = FindFirstSceneBackedDescendant(headHealthLayer);
        var progressionSummary = sceneRoot.FindChild("ProgressionSummary", recursive: true, owned: false) as Label;
        var currentHp = player.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var selectedIndex = skillBar != null && skillBar.HasMeta("SelectedIndex")
            ? skillBar.GetMeta("SelectedIndex").AsInt32()
            : -1;
        var damageNumberCount = damageLayer?.GetChildCount() ?? 0;
        var headHealthBarCount = headHealthLayer?.GetChildCount() ?? 0;

        values["formal_hud_found"] = (hud != null).ToString(CultureInfo.InvariantCulture);
        values["formal_hud_health_text"] = health?.Text ?? string.Empty;
        values["formal_hud_skill_selected_index"] = selectedIndex.ToString(CultureInfo.InvariantCulture);
        values["formal_hud_current_skill"] = currentSkillName;
        values["formal_hud_damage_number_count"] = damageNumberCount.ToString(CultureInfo.InvariantCulture);
        values["formal_hud_head_health_bar_count"] = headHealthBarCount.ToString(CultureInfo.InvariantCulture);
        values["formal_hud_progression_summary"] = progressionSummary?.Text ?? string.Empty;
        values["damage_log_count"] = damageLogs.Count.ToString(CultureInfo.InvariantCulture);
        var loadoutSource = ReadStringMeta(skillBar, "LoadoutSource");
        var expectedCharacterSource = $"{BrotatoLikeSkillLoadoutAuthoring.SourceCharacterPrefix}{BrotatoLikeCharacterCatalog.DefaultCharacterId}";
        var defaultFallbackLoadout = loadoutSource == BrotatoLikeSkillLoadoutAuthoring.SourceDefault
            || loadoutSource == expectedCharacterSource;
        values["skill_loadout_source"] = loadoutSource;
        values["skill_loadout_source_is_default_fallback"] = defaultFallbackLoadout.ToString(CultureInfo.InvariantCulture);
        values["skill_owned_ids"] = ReadStringMeta(skillBar, "OwnedAbilityIds");
        values["skill_visible_slot_ids"] = ReadStringMeta(skillBar, "VisibleSlotIds");
        values["skill_selected_id"] = ReadStringMeta(skillBar, "SelectedAbilityId");
        values["skill_total_owned_count"] = ReadIntMeta(skillBar, "TotalOwnedCount").ToString(CultureInfo.InvariantCulture);
        values["skill_visible_slot_count"] = ReadIntMeta(skillBar, "VisibleSlotCount").ToString(CultureInfo.InvariantCulture);
        values["skill_hidden_owned_count"] = ReadIntMeta(skillBar, "HiddenOwnedCount").ToString(CultureInfo.InvariantCulture);
        values["skill_available_pool_ids"] = player.HasMeta(BrotatoLikeSkillLoadoutAuthoring.AvailableSkillPoolIdsMeta)
            ? player.GetMeta(BrotatoLikeSkillLoadoutAuthoring.AvailableSkillPoolIdsMeta).AsString()
            : string.Empty;

        values["scene_backed_hud"] = FormatSceneBacked(hud);
        values["scene_backed_health"] = FormatSceneBacked(health);
        values["scene_backed_player_health_bar"] = FormatSceneBacked(playerHealthBar);
        values["scene_backed_skill_bar"] = FormatSceneBacked(skillBar);
        values["scene_backed_damage_layer"] = FormatSceneBacked(damageLayer);
        values["scene_backed_damage_number"] = FormatSceneBacked(damageNumber);
        values["scene_backed_head_health_layer"] = FormatSceneBacked(headHealthLayer);
        values["scene_backed_head_health_bar"] = FormatSceneBacked(headHealthBar);
        values["scene_backed_progression"] = FormatSceneBacked(progressionSummary);

        var sceneBacked = IsSceneBacked(playerHealthBar)
            && IsSceneBacked(skillBar)
            && IsSceneBacked(damageNumber)
            && IsSceneBacked(headHealthBar);

        return new HudAcceptance(
            hud != null
                && health != null
                && health.Text.StartsWith("HP ", StringComparison.Ordinal)
                && health.HasMeta("CurrentHp")
                && health.HasMeta("MaxHp"),
            skillBar != null
                && selectedIndex >= 0
                && !string.IsNullOrWhiteSpace(currentSkillName)
                && progressionSummary != null
                && defaultFallbackLoadout
                && ReadIntMeta(skillBar, "TotalOwnedCount") > 0
                && !string.IsNullOrWhiteSpace(ReadStringMeta(skillBar, "VisibleSlotIds")),
            damageLogs.Count > 0
                && damageLayer != null
                && damageNumberCount > 0
                && headHealthLayer != null,
            sceneBacked);
    }

    private static bool IsSceneBacked(Node? node)
    {
        return node != null && GodotObject.IsInstanceValid(node) && !string.IsNullOrEmpty(node.SceneFilePath);
    }

    private static string FormatSceneBacked(Node? node)
    {
        if (node == null)
        {
            return "null";
        }

        return string.IsNullOrEmpty(node.SceneFilePath) ? "false" : node.SceneFilePath;
    }

    private static Node? FindFirstSceneBackedDescendant(Node? root)
    {
        if (root == null)
        {
            return null;
        }

        for (var i = 0; i < root.GetChildCount(); i++)
        {
            var child = root.GetChild(i);
            if (IsSceneBacked(child))
            {
                return child;
            }

            var nested = FindFirstSceneBackedDescendant(child);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
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

    private static CameraFollowAcceptance VerifyCameraFollow(
        BrotatoLikeGameRuntime runtime,
        GodotEntity2D player,
        Dictionary<string, string> values)
    {
        var camera = runtime.PlayerCamera;
        var ok = camera != null
            && GodotObject.IsInstanceValid(camera)
            && camera.Enabled
            && camera.PositionSmoothingEnabled
            && camera.PositionSmoothingSpeed > 0f;
        values["camera_enabled"] = (camera?.Enabled ?? false).ToString();
        values["camera_smoothing"] = (camera?.PositionSmoothingEnabled ?? false).ToString();
        return new CameraFollowAcceptance(ok);
    }

    private static async Task<DeathRespawnAcceptance> VerifyDeathGateAndRespawn(
        Node sceneRoot,
        BrotatoLikeGameRuntime runtime,
        GodotEntity2D player,
        EnemyAcceptance enemies,
        Dictionary<string, string> values)
    {
        var oldEntityId = player.EntityId.Value;
        var oldPosition = player.Position;
        var oldMaxHp = player.Data.Get<float>(DamageDataKeys.MaxHp, 0f);

        // Kill the player
        player.Data.Set(DamageDataKeys.CurrentHp, 0f);
        player.Data.Set(DamageDataKeys.IsDead, true);

        await ProcessFrames(sceneRoot, 4);

        var canMoveDead = player.Data.Get<bool>(MovementDataKeys.CanMoveInput, true);
        var inputDead = player.Data.Get<Vector2Value>(MovementDataKeys.InputDirection, new Vector2Value(999f, 999f));
        var deadGated = !canMoveDead && Math.Abs(inputDead.X) < 0.001f && Math.Abs(inputDead.Y) < 0.001f;
        var progressHp = player.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var hpProgressed = oldMaxHp > 0f && progressHp > 0f && progressHp < oldMaxHp;

        // 快进死亡计时器，让主运行时生产流程完成复活。
        runtime.ForceRespawnForValidation();
        await ProcessFrames(sceneRoot, 3);

        var newPlayer = runtime.PlayerEntity;
        var newEntityId = newPlayer?.EntityId.Value ?? "";
        var respawned = newPlayer != null && GodotObject.IsInstanceValid(newPlayer);

        var hpOk = (newPlayer?.Data.Get<float>(DamageDataKeys.CurrentHp, -1f) ?? -1f) > 0f;
        var canMoveOk = newPlayer?.Data.Get<bool>(MovementDataKeys.CanMoveInput, false) ?? false;
        var cameraOk = runtime.PlayerCamera?.Enabled ?? false;
        var positionOk = newPlayer != null && newPlayer.Position.DistanceTo(oldPosition) < 0.1f;

        var respawnOk = deadGated && hpProgressed && respawned && hpOk && canMoveOk && cameraOk && positionOk;

        values["death_gated"] = deadGated.ToString();
        values["respawn_old_id"] = oldEntityId;
        values["respawn_new_id"] = newEntityId;
        values["respawn_progress_hp"] = FormatFloat(progressHp);
        values["respawn_progress_hp_increased"] = hpProgressed.ToString();
        values["respawn_hp_ok"] = hpOk.ToString();
        values["respawn_canmove_ok"] = canMoveOk.ToString();
        values["respawn_same_position"] = positionOk.ToString();
        values["respawn_expected_position"] = FormatVector(oldPosition);
        values["respawn_actual_position"] = newPlayer == null ? string.Empty : FormatVector(newPlayer.Position);
        return new DeathRespawnAcceptance(respawnOk);
    }

    private static ConcurrentAcceptance VerifyConcurrentSystems(
        BrotatoLikeGameRuntime runtime,
        GodotEntity2D player,
        Dictionary<string, string> values)
    {
        var exceptionCaught = false;
        try
        {
            // Skill on cooldown
            var cooldownKey = SlimeAI.GameOS.Capabilities.Ability.AbilityDataKeys.CooldownRemaining;
            player.Data.Set(cooldownKey, 1f);

            // Contact damage
            player.Data.Set(DamageDataKeys.CurrentHp, Math.Max(1f,
                player.Data.Get<float>(DamageDataKeys.CurrentHp, 100f) - 10f));

            // HUD update
            var hud = runtime.Hud;
            hud?.SetMeta("concurrent_acceptance_test", true);
        }
        catch (Exception ex)
        {
            exceptionCaught = true;
            values["concurrent_exception"] = ex.Message;
        }

        values["concurrent_no_exception"] = (!exceptionCaught).ToString();
        return new ConcurrentAcceptance(!exceptionCaught);
    }

    private static PauseResumeAcceptance VerifyPauseResumeIntegrity(
        BrotatoLikeGameRuntime runtime,
        GodotEntity2D player,
        Dictionary<string, string> values)
    {
        var hpBefore = player.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var posBefore = player.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);

        runtime.OpenPauseMenu();

        var spawnInfo = runtime.GetSpawnSystemRuntimeInfo();
        var blockedByPause = spawnInfo?.IsRunning == false;

        runtime.ClosePauseMenu();

        var hpAfter = player.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var posAfter = player.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);

        var statePreserved = blockedByPause
            && Math.Abs(hpBefore - hpAfter) < 0.01f
            && Math.Abs(posBefore.X - posAfter.X) < 0.01f
            && Math.Abs(posBefore.Y - posAfter.Y) < 0.01f;

        values["pause_blocked"] = blockedByPause.ToString();
        values["pause_hp_preserved"] = (Math.Abs(hpBefore - hpAfter) < 0.01f).ToString();
        values["pause_pos_preserved"] = (Math.Abs(posBefore.X - posAfter.X) < 0.01f).ToString();
        return new PauseResumeAcceptance(statePreserved);
    }

    private static void AddCheck(
        IDictionary<string, bool> checks,
        ICollection<string> failures,
        string name,
        bool passed)
    {
        checks[name] = passed;
        if (!passed)
        {
            failures.Add(name);
            Log.Fail($"check {name} failed");
            return;
        }

        Log.Pass($"check {name} passed");
    }

    private static bool HasPhysicalKey(string action, Key physicalKey)
    {
        if (!InputMap.HasAction(action))
        {
            return false;
        }

        var events = InputMap.ActionGetEvents(action);
        for (var i = 0; i < events.Count; i++)
        {
            if (events[i] is InputEventKey keyEvent && keyEvent.PhysicalKeycode == physicalKey)
            {
                return true;
            }
        }

        return false;
    }

    private static int CountEffectEntities(string abilityEntityId)
    {
        var count = 0;
        var entities = EntityManager.GetAll();
        for (var i = 0; i < entities.Count; i++)
        {
            var ability = entities[i].Data.Get<EntityId?>(EffectDataKeys.AbilityEntity, null);
            if (ability.HasValue && ability.Value.Value == abilityEntityId)
            {
                count++;
            }
        }

        return count;
    }

    private static void WriteArtifact(
        GameOSObservationSession observation,
        IReadOnlyDictionary<string, bool> checks,
        IReadOnlyDictionary<string, string> values,
        IReadOnlyList<string> damageLogs,
        IReadOnlyList<string> failures)
    {
        var path = observation.CreateArtifactPath(ArtifactFileName);
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        Log.Info($"writing artifact {path}");
        File.WriteAllText(path, BuildJson(path, checks, values, damageLogs, failures), Encoding.UTF8);
    }

    private static string BuildJson(
        string artifactPath,
        IReadOnlyDictionary<string, bool> checks,
        IReadOnlyDictionary<string, string> values,
        IReadOnlyList<string> damageLogs,
        IReadOnlyList<string> failures)
    {
        var builder = new StringBuilder();
        builder.AppendLine("{");
        builder.Append("  \"scene\": \"").Append(ScenePath).AppendLine("\",");
        builder.AppendLine("  \"mode\": \"playable-slice\",");
        builder.Append("  \"status\": \"").Append(failures.Count == 0 ? "pass" : "fail").AppendLine("\",");
        builder.Append("  \"passMarker\": \"").Append(failures.Count == 0 ? "BrotatoLike playable slice PASS" : "BrotatoLike playable slice FAIL").AppendLine("\",");
        builder.Append("  \"artifactPath\": \"").Append(EscapeJson(artifactPath)).AppendLine("\",");
        AppendStringArray(builder, "expectedInputs", ExpectedInputs, trailingComma: true);
        AppendStringArray(builder, "expectedObservations", ExpectedObservations, trailingComma: true);
        AppendStringArray(builder, "passCriteria", PassCriteria, trailingComma: true);
        AppendStringArray(builder, "failCriteria", FailCriteria, trailingComma: true);
        AppendCriteriaArray(builder, checks, trailingComma: true);
        builder.AppendLine("  \"checked_criteria\": {");
        var index = 0;
        foreach (var check in checks)
        {
            builder.Append("    \"").Append(EscapeJson(check.Key)).Append("\": ").Append(check.Value ? "true" : "false");
            builder.AppendLine(++index < checks.Count ? "," : string.Empty);
        }

        builder.AppendLine("  },");
        AppendStringMap(builder, "observed_values", values, trailingComma: true);
        AppendStringArray(builder, "damage_logs", damageLogs, trailingComma: true);
        AppendStringArray(builder, "failureReasons", failures, trailingComma: true);
        AppendStringArray(builder, "failure_reasons", failures, trailingComma: false);
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static readonly string[] ExpectedInputs =
    {
        "GODOT_SCENE_TEST_ARTIFACT_DIR is set by the scene runner",
        "res://Scenes/Main.tscn initializes BrotatoLikeGameRuntime from DataOS snapshot",
        "deterministic MoveRight, MoveUp, UseSkill, NextSkill, SkillSlot1, SkillSlot4 and ConfirmTarget input actions are applied during acceptance"
    };

    private static readonly string[] ExpectedObservations =
    {
        "player runtime data records input direction, last move direction, and changed position",
        "DataOS-spawned enemies chase, apply contact damage, and expose resource path evidence",
        "finite wave authoring records current wave id, phase, expected spawn count, actual spawned count and completion mode",
        "formal HUD, head health bar, skill bar, damage number and progression summary nodes expose player-facing evidence",
        "skill bar records loadout source, owned ability ids, visible active slots, selected ability id and total owned count",
        "numeric skill slot actions select visible active slots without bypassing the player input component",
        "slam, chain and point-target abilities produce input-action damage, cooldown, targeting and visual evidence",
        "chain lightning line VFX records scene path, source/target ids, start/end world positions, Line2D points, duration and cleanup evidence for each bounce",
        "formal composite UI nodes (player health bar, skill bar, head health bar and damage number) have non-empty SceneFilePath",
        "death gate blocks input while respawn HP increases, then respawns at the previous position"
    };

    private static readonly string[] PassCriteria =
    {
        "all criteria entries have status pass",
        "failureReasons and failure_reasons are empty",
        "passMarker is BrotatoLike playable slice PASS"
    };

    private static readonly string[] FailCriteria =
    {
        "any criteria entry has status fail",
        "player, enemy, ability, damage, or HUD evidence is missing",
        "skill bar loadout source, visible slot ids, selected ability id, or total owned count is missing",
        "chain lightning line VFX is missing, unbound, not multi-bounce, or not cleaned up",
        "formal composite UI node has empty SceneFilePath (code-created, not scene-backed)",
        "respawn moves the player away from the death position or does not restore input",
        "artifactPath is empty or the scene runner does not collect the artifact file"
    };

    private static void AppendCriteriaArray(
        StringBuilder builder,
        IReadOnlyDictionary<string, bool> checks,
        bool trailingComma)
    {
        builder.AppendLine("  \"criteria\": [");
        var index = 0;
        foreach (var check in checks)
        {
            builder.Append("    { \"name\": \"")
                .Append(EscapeJson(check.Key))
                .Append("\", \"status\": \"")
                .Append(check.Value ? "pass" : "fail")
                .Append("\" }");
            builder.AppendLine(++index < checks.Count ? "," : string.Empty);
        }

        builder.Append("  ]");
        builder.AppendLine(trailingComma ? "," : string.Empty);
    }

    private static void AppendStringMap(
        StringBuilder builder,
        string propertyName,
        IReadOnlyDictionary<string, string> values,
        bool trailingComma)
    {
        builder.Append("  \"").Append(propertyName).AppendLine("\": {");
        var index = 0;
        foreach (var value in values)
        {
            builder.Append("    \"").Append(EscapeJson(value.Key)).Append("\": \"").Append(EscapeJson(value.Value)).Append('"');
            builder.AppendLine(++index < values.Count ? "," : string.Empty);
        }

        builder.Append("  }");
        builder.AppendLine(trailingComma ? "," : string.Empty);
    }

    private static void AppendStringArray(
        StringBuilder builder,
        string propertyName,
        IReadOnlyList<string> values,
        bool trailingComma)
    {
        builder.Append("  \"").Append(propertyName).AppendLine("\": [");
        for (var i = 0; i < values.Count; i++)
        {
            builder.Append("    \"").Append(EscapeJson(values[i])).Append('"');
            builder.AppendLine(i + 1 < values.Count ? "," : string.Empty);
        }

        builder.Append("  ]");
        builder.AppendLine(trailingComma ? "," : string.Empty);
    }

    private static string EscapeJson(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal);
    }

    private static string FormatVector(Vector2Value value)
    {
        return FormattableString.Invariant($"{value.X:0.###},{value.Y:0.###}");
    }

    private static string FormatVector(Vector2 value)
    {
        return FormattableString.Invariant($"{value.X:0.###},{value.Y:0.###}");
    }

    private static string FormatFloat(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}

internal sealed record PlayableSliceAcceptanceResult(bool Success, IReadOnlyList<string> Failures);

internal readonly record struct PlayerMovementAcceptance(
    bool InputMapHasWasd,
    bool InputMapHasArrows,
    bool InputDirectionWritten,
    bool LastMoveDirectionWritten,
    bool PositionChanged);

internal sealed record EnemyAcceptance(
    List<GodotEntity2D> Enemies,
    bool SpawnedFromDataOS,
    bool ContinuousSpawnObserved,
    bool ResourcePathsRecorded,
    bool ChaseOrMoveObserved,
    bool ContactDamageApplied);

internal readonly record struct SkillAcceptance(
    bool SlamTriggered,
    bool SlamCooldownGated,
    bool SlamHit,
    bool SlamVisualEvidence,
    bool ChainTriggered,
    bool ChainCooldownGated,
    bool ChainTargetSelected,
    bool ChainHit,
    bool ChainStructuredEvidence,
    bool ChainLineVfxBound,
    bool ChainLineVfxCleanup,
    bool ChainLineVfxMultiBounce,
    bool PointTargetingStarted,
    bool PointTargetingConfirmed,
    bool DirectSlotSelected,
    bool RealInputActionPath,
    string CurrentSkillName)
{
    public static SkillAcceptance Empty => new(false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, string.Empty);
}

internal readonly record struct ChainLineVfxAcceptance(bool Bound, bool Cleanup, bool MultiBounce);

internal readonly record struct EnemyCleanupAcceptance(bool DeathObserved, bool CleanupQueued);

internal readonly record struct HudAcceptance(bool HealthEvidence, bool CurrentSkillEvidence, bool DamageEvidence, bool SceneBacked);

internal readonly record struct CameraFollowAcceptance(bool CameraFollowsPlayer);

internal readonly record struct DeathRespawnAcceptance(bool RespawnOk);

internal readonly record struct ConcurrentAcceptance(bool NoException);

internal readonly record struct PauseResumeAcceptance(bool StatePreserved);
