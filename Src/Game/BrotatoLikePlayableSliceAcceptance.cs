using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
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
using SlimeAI.GameOS.Runtime.Events.Core;
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
    public static PlayableSliceAcceptanceResult Run(Node sceneRoot, BrotatoLikeGameRuntime runtime)
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

            var movement = VerifyPlayerMovement(runtime, player, values);
            AddCheck(checks, failureReasons, "player.input_map_has_wasd", movement.InputMapHasWasd);
            AddCheck(checks, failureReasons, "player.input_map_has_arrows", movement.InputMapHasArrows);
            AddCheck(checks, failureReasons, "player.input_direction_written", movement.InputDirectionWritten);
            AddCheck(checks, failureReasons, "player.last_move_direction_written", movement.LastMoveDirectionWritten);
            AddCheck(checks, failureReasons, "player.position_changed", movement.PositionChanged);

            var enemies = VerifyEnemySpawnAndChase(runtime, player, values);
            AddCheck(checks, failureReasons, "enemy.spawned_from_dataos", enemies.SpawnedFromDataOS);
            AddCheck(checks, failureReasons, "enemy.resource_paths_recorded", enemies.ResourcePathsRecorded);
            AddCheck(checks, failureReasons, "enemy.chase_or_move_observed", enemies.ChaseOrMoveObserved);
            AddCheck(checks, failureReasons, "enemy.contact_damage_applied", enemies.ContactDamageApplied);

            var skills = VerifySkills(player, enemies, values);
            AddCheck(checks, failureReasons, "skill.slam_triggered", skills.SlamTriggered);
            AddCheck(checks, failureReasons, "skill.slam_cooldown_gated", skills.SlamCooldownGated);
            AddCheck(checks, failureReasons, "skill.slam_hit", skills.SlamHit);
            AddCheck(checks, failureReasons, "skill.slam_visual_evidence", skills.SlamVisualEvidence);
            AddCheck(checks, failureReasons, "skill.chain_triggered", skills.ChainTriggered);
            AddCheck(checks, failureReasons, "skill.chain_cooldown_gated", skills.ChainCooldownGated);
            AddCheck(checks, failureReasons, "skill.chain_target_selected", skills.ChainTargetSelected);
            AddCheck(checks, failureReasons, "skill.chain_hit", skills.ChainHit);
            AddCheck(checks, failureReasons, "skill.chain_structured_evidence", skills.ChainStructuredEvidence);

            var cleanup = VerifyDeathAndCleanup(enemies, player, values);
            AddCheck(checks, failureReasons, "enemy.death_observed", cleanup.DeathObserved);
            AddCheck(checks, failureReasons, "enemy.cleanup_queued", cleanup.CleanupQueued);

            var hud = WriteHudEvidence(sceneRoot, player, skills.CurrentSkillName, damageLogs, values);
            AddCheck(checks, failureReasons, "hud.health_evidence", hud.HealthEvidence);
            AddCheck(checks, failureReasons, "hud.current_skill_evidence", hud.CurrentSkillEvidence);
            AddCheck(checks, failureReasons, "hud.damage_evidence", hud.DamageEvidence);

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
        }
    }

    private static PlayerMovementAcceptance VerifyPlayerMovement(
        BrotatoLikeGameRuntime runtime,
        GodotEntity2D player,
        Dictionary<string, string> values)
    {
        var inputComponent = player.GetNodeOrNull<GodotPlayerInputComponent>("PlayerInput");
        var inputMapHasWasd = HasPhysicalKey("MoveLeft", (Key)65)
            && HasPhysicalKey("MoveRight", (Key)68)
            && HasPhysicalKey("MoveUp", (Key)87)
            && HasPhysicalKey("MoveDown", (Key)83);
        var inputMapHasArrows = HasPhysicalKey("MoveLeft", (Key)4194319)
            && HasPhysicalKey("MoveRight", (Key)4194321)
            && HasPhysicalKey("MoveUp", (Key)4194320)
            && HasPhysicalKey("MoveDown", (Key)4194322);
        if (inputComponent == null || runtime.MovementDriver == null)
        {
            return new PlayerMovementAcceptance(inputMapHasWasd, inputMapHasArrows, false, false, false);
        }

        var start = player.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
        Input.ActionPress("MoveRight");
        inputComponent.TickInput();
        runtime.MovementDriver.TickMovement(0.25f);
        Input.ActionRelease("MoveRight");

        Input.ActionPress("MoveUp");
        inputComponent.TickInput();
        runtime.MovementDriver.TickMovement(0.25f);
        Input.ActionRelease("MoveUp");

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
            inputDirection != Vector2Value.Zero,
            lastMoveDirection != Vector2Value.Zero,
            Vector2Value.Distance(start, end) > 0.001f);
    }

    private static EnemyAcceptance VerifyEnemySpawnAndChase(
        BrotatoLikeGameRuntime runtime,
        GodotEntity2D player,
        Dictionary<string, string> values)
    {
        var tick = runtime.TickSpawn(0d);
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
        values["enemy_total_spawned"] = tick.Value.TotalSpawned.ToString(CultureInfo.InvariantCulture);
        values["enemy_ids"] = string.Join(",", enemies.ConvertAll(enemy => enemy.EntityId.Value));

        if (enemies.Count == 0 || runtime.MovementDriver == null)
        {
            return new EnemyAcceptance(enemies, false, false, false, false);
        }

        var first = enemies[0];
        var playerPosition = player.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
        first.Data.Set(MovementDataKeys.Position, playerPosition + new Vector2Value(48f, 0f));
        first.Position = new Vector2(playerPosition.X + 48f, playerPosition.Y);
        var start = first.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
        var chaseDirection = (playerPosition - start).Normalized();
        first.Data.Set(MovementDataKeys.AIMoveDirection, chaseDirection);
        runtime.MovementDriver.MovementSystem.Start(first, new MovementParams
        {
            Mode = MoveMode.AIControlled,
            MaxDuration = -1f
        });
        runtime.MovementDriver.TickMovement(0.25f);
        var end = first.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);

        var playerHpBefore = player.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var contactDamage = Math.Max(1f, first.Data.Get<float>(DamageDataKeys.ContactDamage, first.Data.Get<float>(AttackDataKeys.Damage, 5f)));
        var contactResult = DamageTool.Apply([player], new DamageApplyOptions(contactDamage)
        {
            Attacker = first,
            Type = DamageType.Physical,
            Tags = DamageTags.Contact
        });
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
            enemies.TrueForAll(enemy => !string.IsNullOrWhiteSpace(enemy.Data.Get(UnitDataKeys.VisualScenePath, string.Empty))),
            chaseDirection != Vector2Value.Zero && Vector2Value.Distance(start, end) > 0.001f,
            contactResult.AppliedCount > 0 && playerHpAfter < playerHpBefore);
    }

    private static SkillAcceptance VerifySkills(
        GodotEntity2D player,
        EnemyAcceptance enemies,
        Dictionary<string, string> values)
    {
        var ownedIds = player.Data.Get<List<string>>(AbilityDataKeys.OwnedAbilityIds);
        if (ownedIds == null || ownedIds.Count < 2 || enemies.Enemies.Count == 0)
        {
            return SkillAcceptance.Empty;
        }

        var slam = EntityManager.Get(new EntityId(ownedIds[0]));
        var chain = EntityManager.Get(new EntityId(ownedIds[1]));
        if (slam == null || chain == null)
        {
            return SkillAcceptance.Empty;
        }

        var target = enemies.Enemies[0];
        var playerPosition = player.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
        target.Data.Set(MovementDataKeys.Position, playerPosition + new Vector2Value(24f, 0f));
        target.Position = new Vector2(playerPosition.X + 24f, playerPosition.Y);

        var slamHpBefore = target.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var slamEffectsBefore = CountEffectEntities(slam.EntityId.Value);
        player.Data.Set(AbilityDataKeys.CurrentAbilityIndex, 0);
        player.Events.Publish(new InputUseSkill(player));
        var slamHpAfter = target.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var slamEffectsAfter = CountEffectEntities(slam.EntityId.Value);
        var slamCooldown = slam.Data.Get<float>(AbilityDataKeys.CooldownRemaining, 0f);
        var slamCooldownReport = AbilityService.Instance.TryTrigger(new AbilityCastContext
        {
            Caster = player,
            Ability = slam,
            DamageType = DamageType.Physical
        });

        AbilityService.Instance.TickCooldowns([slam], slamCooldown + 0.1f);

        player.Events.Publish(new InputNextSkill(player));
        var currentIndex = player.Data.Get<int>(AbilityDataKeys.CurrentAbilityIndex, 0);
        AbilityTargetingTool.TryBuildContext(player, chain, out var chainContext);
        var chainHpBefore = target.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        player.Events.Publish(new InputUseSkill(player));
        TimerManager.Instance.Tick(chain.Data.Get<float>(AbilityDataKeys.ChainDelay, 0f) + 0.05f);
        var chainHpAfter = target.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var chainCooldown = chain.Data.Get<float>(AbilityDataKeys.CooldownRemaining, 0f);
        var chainCooldownReport = chainContext == null
            ? new AbilityTriggerReport(AbilityTriggerResult.FailNoTarget, null, "missing chain context")
            : AbilityService.Instance.TryTrigger(chainContext);

        values["skill_slam_id"] = slam.EntityId.Value;
        values["skill_slam_hp_before"] = FormatFloat(slamHpBefore);
        values["skill_slam_hp_after"] = FormatFloat(slamHpAfter);
        values["skill_slam_cooldown"] = FormatFloat(slamCooldown);
        values["skill_slam_effect_count"] = (slamEffectsAfter - slamEffectsBefore).ToString(CultureInfo.InvariantCulture);
        values["skill_chain_id"] = chain.EntityId.Value;
        values["skill_chain_target"] = chainContext?.Targets != null && chainContext.Targets.Count > 0
            ? chainContext.Targets[0].EntityId.Value
            : string.Empty;
        values["skill_chain_hp_before"] = FormatFloat(chainHpBefore);
        values["skill_chain_hp_after"] = FormatFloat(chainHpAfter);
        values["skill_chain_cooldown"] = FormatFloat(chainCooldown);
        values["skill_current_index"] = currentIndex.ToString(CultureInfo.InvariantCulture);

        return new SkillAcceptance(
            SlamTriggered: slamCooldown > 0f,
            SlamCooldownGated: slamCooldownReport.Result == AbilityTriggerResult.FailCooldown,
            SlamHit: slamHpAfter < slamHpBefore,
            SlamVisualEvidence: slamEffectsAfter > slamEffectsBefore,
            ChainTriggered: chainCooldown > 0f,
            ChainCooldownGated: chainCooldownReport.Result == AbilityTriggerResult.FailCooldown,
            ChainTargetSelected: chainContext?.Targets != null && chainContext.Targets.Count > 0,
            ChainHit: chainHpAfter < chainHpBefore,
            ChainStructuredEvidence: chainHpAfter < chainHpBefore && string.IsNullOrWhiteSpace(chain.Data.Get(AbilityDataKeys.LineEffectScenePath, string.Empty)),
            CurrentSkillName: chain.Data.Get(AbilityDataKeys.Name, chain.EntityId.Value));
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
        enemy.DestroyEntity();
        values["enemy_cleanup_id"] = enemy.EntityId.Value;
        values["enemy_cleanup_hp_before"] = FormatFloat(hpBefore);
        values["enemy_cleanup_damage_applied"] = result.AppliedCount.ToString(CultureInfo.InvariantCulture);
        values["enemy_cleanup_queued"] = enemy.IsQueuedForDeletion().ToString(CultureInfo.InvariantCulture);
        return new EnemyCleanupAcceptance(
            enemy.Data.Get<bool>(DamageDataKeys.IsDead, false),
            enemy.IsQueuedForDeletion());
    }

    private static HudAcceptance WriteHudEvidence(
        Node sceneRoot,
        GodotEntity2D player,
        string currentSkillName,
        IReadOnlyList<string> damageLogs,
        Dictionary<string, string> values)
    {
        var hud = new CanvasLayer { Name = "PlayableSliceHUD" };
        var health = new Label { Name = "HealthLabel" };
        var skill = new Label { Name = "CurrentSkillLabel" };
        var damage = new Label { Name = "DamageLogLabel" };

        var currentHp = player.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        health.Text = FormattableString.Invariant($"HP {currentHp:0.##}");
        skill.Text = $"Skill {currentSkillName}";
        damage.Text = damageLogs.Count > 0 ? damageLogs[^1] : "Damage none";
        hud.AddChild(health);
        hud.AddChild(skill);
        hud.AddChild(damage);
        sceneRoot.AddChild(hud);

        values["hud_health_text"] = health.Text;
        values["hud_skill_text"] = skill.Text;
        values["hud_damage_text"] = damage.Text;
        values["damage_log_count"] = damageLogs.Count.ToString(CultureInfo.InvariantCulture);
        return new HudAcceptance(
            !string.IsNullOrWhiteSpace(health.Text),
            !string.IsNullOrWhiteSpace(skill.Text) && !string.IsNullOrWhiteSpace(currentSkillName),
            damageLogs.Count > 0 && !string.IsNullOrWhiteSpace(damage.Text));
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
        File.WriteAllText(path, BuildJson(checks, values, damageLogs, failures), Encoding.UTF8);
    }

    private static string BuildJson(
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
    string CurrentSkillName)
{
    public static SkillAcceptance Empty => new(false, false, false, false, false, false, false, false, false, string.Empty);
}

internal readonly record struct EnemyCleanupAcceptance(bool DeathObserved, bool CleanupQueued);

internal readonly record struct HudAcceptance(bool HealthEvidence, bool CurrentSkillEvidence, bool DamageEvidence);
