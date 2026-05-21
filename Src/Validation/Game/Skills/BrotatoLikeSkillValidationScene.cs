using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BrotatoLike.Game;
using Godot;
using SlimeAI.GameOS.Capabilities.Ability;
using SlimeAI.GameOS.Capabilities.Collision;
using SlimeAI.GameOS.Capabilities.Damage;
using SlimeAI.GameOS.Capabilities.Effect;
using SlimeAI.GameOS.Capabilities.Movement;
using SlimeAI.GameOS.Capabilities.Projectile;
using SlimeAI.GameOS.GodotBridge;
using SlimeAI.GameOS.Observation;
using SlimeAI.GameOS.Runtime.Entity;
using SlimeAI.GameOS.Runtime.Events.Core;
using SlimeAI.GameOS.Runtime.Pool;
using EffectSpawned = SlimeAI.GameOS.Capabilities.Effect.Events.Spawned;
using MovementCollision = SlimeAI.GameOS.Capabilities.Movement.Events.Collision;
using ProjectileSpawned = SlimeAI.GameOS.Capabilities.Projectile.Events.Spawned;

namespace BrotatoLike.Validation.Game.Skills;

/// <summary>
/// BrotatoLike projectile / passive 技能的逐技能 Godot headless 验证场景。
/// </summary>
public partial class BrotatoLikeSkillValidationScene : Node
{
    private const string ScenePath = "res://Src/Validation/Game/Skills/BrotatoLikeSkillValidation.tscn";
    private const string ArtifactFileName = "brotatolike-skill-validation.json";
    private const string PassMarker = "BrotatoLike Skill validation PASS";
    private const string FailMarker = "BrotatoLike Skill validation FAIL";

    /// <inheritdoc />
    public override async void _Ready()
    {
        EntityManager.Clear();

        using var observation = GameOSObservationSession.FromEnvironment(
            ScenePath,
            "validation",
            Path.Combine(Directory.GetCurrentDirectory(), ".ai-temp", "scene-tests", "manual", "artifacts"));
        using var validation = new SceneValidationSession(
            observation,
            "BrotatoLikeSkillValidation",
            "Game/Skills",
            ArtifactFileName,
            dependencies: new[]
            {
                "BrotatoLike.Game.BrotatoLikeGameRuntime",
                "BrotatoLike.Game.BrotatoLikeAbilityHandlers",
                "BrotatoLike.Game.BrotatoLikeSkillLoadoutAuthoring",
                "SlimeAI.GameOS.Capabilities.Ability",
                "SlimeAI.GameOS.Capabilities.Projectile",
                "SlimeAI.GameOS.Capabilities.Movement",
                "SlimeAI.GameOS.Capabilities.Damage",
                "SlimeAI.GameOS.GodotBridge.GodotProjectileEffectSpawner"
            },
            notes: new[]
            {
                "Validation uses the production BrotatoLike handler registry and DataOS rows.",
                "Validation does not implement skill acquisition, upgrade choices, shop flow, or balance tuning."
            },
            expectedInputs: new[]
            {
                "BrotatoLikeGameRuntime initialized from DataOS snapshot",
                "SpawnPlayerWithValidationLoadout using ValidationAllSkillAbilityIds",
                "Deterministic enemy targets with explicit team, HP, collision radius, and position",
                "Manual AbilityService.TryTrigger calls for each projectile/passive ability id"
            },
            expectedObservations: new[]
            {
                "sine_wave_shot, boomerang_throw, bezier_shot, parabola_shot, and arc_shot spawn projectile runtime entities with DataOS scene paths and movement modes",
                "projectile skills move, collide, damage a target, and clean up projectile runtime/visual entities",
                "orbit_skill and aura_shield spawn sustained projectile entities with Orbit/AttachToHost semantics, hit evidence, and lifecycle cleanup",
                "circle_damage damages only in-radius enemy targets, spawns its DataOS visual effect, and exposes effect cleanup evidence",
                "artifact checks are grouped by ability id and include loadout source plus owned ability ids"
            },
            passCriteria: new[]
            {
                $"stdout contains {PassMarker}",
                "artifact status is pass",
                "every skill check named by ability id passes",
                "expectedInputs, expectedObservations, passCriteria, failCriteria, and artifactPath are non-empty"
            },
            failCriteria: new[]
            {
                $"stdout contains {FailMarker}",
                "any skill trigger, projectile/effect spawn, movement mode, hit/damage, visual, or cleanup evidence is missing",
                "artifact status is fail with ability-id-specific failureReasons"
            });

        validation.Info("validation start");
        var values = await RunSkillProbe();
        validation.Check("sine_wave_shot", "Ability:sine_wave_shot", () => Result(values, "sine_wave_shot"));
        validation.Check("boomerang_throw", "Ability:boomerang_throw", () => Result(values, "boomerang_throw"));
        validation.Check("bezier_shot", "Ability:bezier_shot", () => Result(values, "bezier_shot"));
        validation.Check("parabola_shot", "Ability:parabola_shot", () => Result(values, "parabola_shot"));
        validation.Check("arc_shot", "Ability:arc_shot", () => Result(values, "arc_shot"));
        validation.Check("orbit_skill", "Ability:orbit_skill", () => Result(values, "orbit_skill"));
        validation.Check("circle_damage", "Ability:circle_damage", () => Result(values, "circle_damage"));
        validation.Check("aura_shield", "Ability:aura_shield", () => Result(values, "aura_shield"));
        validation.Check("ability_id_grouping", "Artifact", () => Result(values, "ability_id_grouping"));

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
            GD.Print($"BrotatoLike Skill validation failures: {string.Join("; ", validation.FailureReasons)}");
        }

        GetTree().Quit(success ? 0 : 1);
    }

    private async Task<Dictionary<string, SkillProbeEvidence>> RunSkillProbe()
    {
        var values = new Dictionary<string, SkillProbeEvidence>(StringComparer.Ordinal);
        var runtime = new BrotatoLikeGameRuntime
        {
            Name = "GameRuntime",
            AutoInitialize = false,
            AutoTick = false
        };
        AddChild(runtime);
        runtime.InitializeFromDataOS(1, runtime);
        runtime.BeginGameplay();
        runtime.MovementDriver!.AutoTick = false;

        var player = runtime.SpawnPlayerWithValidationLoadout(
            BrotatoLikeSkillLoadoutAuthoring.ValidationAllSkillAbilityIds,
            "deluyi",
            Vector2.Zero);
        await ProcessFrames(4);
        runtime.ProgressionService?.SetProcess(false);

        values["sine_wave_shot"] = await RunProjectileProbe(runtime, player, new ProjectileSkillCase
        {
            AbilityId = "sine_wave_shot",
            ExpectedMode = MoveMode.SineWave,
            ExpectedScenePath = "res://assets/Projectile/Projectile/Polygon2D/ArrowNeedle.tscn",
            TargetPosition = new Vector2Value(360f, 0f),
            TargetRadius = 90f,
            ExpectedProjectileCount = 1,
            SampleSeconds = 0.15f,
            CompletionSeconds = 1.4f,
            BehaviorExpectation = "SineWave projectile records lateral movement before collision cleanup."
        });
        values["boomerang_throw"] = await RunProjectileProbe(runtime, player, new ProjectileSkillCase
        {
            AbilityId = "boomerang_throw",
            ExpectedMode = MoveMode.Boomerang,
            ExpectedScenePath = "res://assets/Projectile/Projectile/Polygon2D/BulletDiamond.tscn",
            TargetPosition = new Vector2Value(260f, 0f),
            TargetRadius = 72f,
            ExpectedProjectileCount = 1,
            SampleSeconds = 0.25f,
            CompletionSeconds = 1.8f,
            BehaviorExpectation = "Boomerang projectile hits on outbound travel and cleans up after return."
        });
        values["bezier_shot"] = await RunProjectileProbe(runtime, player, new ProjectileSkillCase
        {
            AbilityId = "bezier_shot",
            ExpectedMode = MoveMode.BezierCurve,
            ExpectedScenePath = "res://assets/Projectile/Projectile/Polygon2D/ArrowNeedle.tscn",
            TargetPosition = new Vector2Value(340f, 120f),
            TargetRadius = 120f,
            ExpectedProjectileCount = 5,
            SampleSeconds = 0.25f,
            CompletionSeconds = 1.65f,
            BehaviorExpectation = "Bezier projectiles use DataOS converging curve parameters and at least one projectile hits."
        });
        values["parabola_shot"] = await RunProjectileProbe(runtime, player, new ProjectileSkillCase
        {
            AbilityId = "parabola_shot",
            ExpectedMode = MoveMode.CircularArc,
            ExpectedScenePath = "res://assets/Projectile/Projectile/Polygon2D/ArrowNeedle.tscn",
            TargetPosition = new Vector2Value(300f, 0f),
            TargetRadius = 96f,
            ExpectedProjectileCount = 1,
            SampleSeconds = 0.25f,
            CompletionSeconds = 1.55f,
            BehaviorExpectation = "Parabola-labeled skill uses current DataOS CircularArc/BowWorldUp landing path and damages at target point."
        });
        values["arc_shot"] = await RunProjectileProbe(runtime, player, new ProjectileSkillCase
        {
            AbilityId = "arc_shot",
            ExpectedMode = MoveMode.CircularArc,
            ExpectedScenePath = "res://assets/Projectile/Projectile/Polygon2D/BoomerangChevron.tscn",
            TargetPosition = new Vector2Value(300f, 0f),
            TargetRadius = 96f,
            ExpectedProjectileCount = 1,
            UseEntityTarget = true,
            SampleSeconds = 0.25f,
            CompletionSeconds = 1.75f,
            BehaviorExpectation = "Arc shot requires an entity target and follows CircularArc movement to hit it."
        });
        values["orbit_skill"] = await RunOrbitProbe(runtime, player);
        values["circle_damage"] = await RunCircleDamageProbe(player);
        values["aura_shield"] = await RunAuraShieldProbe(runtime, player);
        values["ability_id_grouping"] = BuildGroupingEvidence(player, values);
        return values;
    }

    private async Task<SkillProbeEvidence> RunProjectileProbe(
        BrotatoLikeGameRuntime runtime,
        GodotEntity2D player,
        ProjectileSkillCase skillCase)
    {
        var ability = RequireAbility(player, skillCase.AbilityId);
        var target = CreateTarget($"{skillCase.AbilityId}-target", skillCase.TargetPosition, skillCase.TargetRadius, 600f, 2);
        using var monitor = new SkillEventMonitor(ability.EntityId);
        var hpBefore = target.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var report = AbilityService.Instance.TryTrigger(new AbilityCastContext
        {
            Caster = player,
            Ability = ability,
            Targets = skillCase.UseEntityTarget ? new[] { target } : null,
            TargetPosition = skillCase.TargetPosition
        });

        await ProcessFrames(2);
        var initialPositions = CapturePositions(monitor.Projectiles);
        var visualBeforeCleanup = CountVisualNodes(monitor.Projectiles);
        var visualPoolNames = VisualPoolNames(monitor.Projectiles);
        var projectileScenePaths = ProjectileScenePaths(monitor.Projectiles);
        var projectileMovementModes = ProjectileMovementModes(monitor.Projectiles);
        TickMovement(runtime, skillCase.SampleSeconds);
        var samplePositions = CapturePositions(monitor.Projectiles);
        TickMovement(runtime, MathF.Max(0f, skillCase.CompletionSeconds - skillCase.SampleSeconds));
        await ProcessFrames(3);

        var hpAfter = target.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var projectileCountOk = monitor.Projectiles.Count == skillCase.ExpectedProjectileCount;
        var sceneOk = AllValuesEqual(projectileScenePaths, skillCase.ExpectedScenePath);
        var modeOk = AllValuesEqual(projectileMovementModes, skillCase.ExpectedMode.ToString());
        var movedDistance = MaxMovedDistance(initialPositions, samplePositions);
        var hitOk = monitor.CollisionCount > 0 && hpAfter < hpBefore;
        var cleanupOk = AllRuntimeEntitiesDestroyed(monitor.Projectiles);
        var poolReturnOk = AllVisualsReturnedToPools(visualPoolNames);
        var success = report.Success
            && projectileCountOk
            && sceneOk
            && modeOk
            && movedDistance > 0.001f
            && hitOk
            && cleanupOk
            && poolReturnOk
            && visualBeforeCleanup > 0;

        var details = CreateBaseDetails(player, ability, skillCase.AbilityId);
        details["triggerResult"] = report.Result.ToString();
        details["triggerSuccess"] = report.Success;
        details["targetsHitReported"] = report.Executed?.TargetsHit ?? 0;
        details["expectedProjectileCount"] = skillCase.ExpectedProjectileCount;
        details["spawnedProjectileIds"] = EntityIds(monitor.Projectiles);
        details["projectileScenePaths"] = projectileScenePaths;
        details["expectedScenePath"] = skillCase.ExpectedScenePath;
        details["movementModes"] = projectileMovementModes;
        details["expectedMovementMode"] = skillCase.ExpectedMode.ToString();
        details["targetPosition"] = Format(skillCase.TargetPosition);
        details["targetRadius"] = skillCase.TargetRadius;
        details["initialPositions"] = initialPositions;
        details["samplePositions"] = samplePositions;
        details["maxMovedDistance"] = movedDistance;
        details["collisionCount"] = monitor.CollisionCount;
        details["targetHpBefore"] = hpBefore;
        details["targetHpAfter"] = hpAfter;
        details["destroyedEntityIds"] = monitor.DestroyedIds;
        details["cleanupRuntimeDestroyed"] = cleanupOk;
        details["visualNodeCountBeforeCleanup"] = visualBeforeCleanup;
        details["visualNodeCountAfterCleanup"] = CountVisualNodes(monitor.Projectiles);
        details["visualPoolNames"] = visualPoolNames;
        details["visualPoolReleasedCounts"] = VisualPoolReleasedCounts(visualPoolNames);
        details["visualPoolReturnOk"] = poolReturnOk;
        details["behaviorExpectation"] = skillCase.BehaviorExpectation;

        await DestroyTarget(target);
        return new SkillProbeEvidence(
            skillCase.AbilityId,
            success,
            success ? "projectile skill behavior validated" : "projectile skill evidence incomplete",
            details);
    }

    private async Task<SkillProbeEvidence> RunOrbitProbe(BrotatoLikeGameRuntime runtime, GodotEntity2D player)
    {
        const string abilityId = "orbit_skill";
        var ability = RequireAbility(player, abilityId);
        var target = CreateTarget("orbit-skill-target", new Vector2Value(100f, 0f), 40f, 600f, 2);
        using var monitor = new SkillEventMonitor(ability.EntityId);
        var hpBefore = target.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var report = AbilityService.Instance.TryTrigger(new AbilityCastContext
        {
            Caster = player,
            Ability = ability,
            TargetPosition = new Vector2Value(100f, 0f)
        });

        await ProcessFrames(2);
        var initialPositions = CapturePositions(monitor.Projectiles);
        var visualBeforeCleanup = CountVisualNodes(monitor.Projectiles);
        var visualPoolNames = VisualPoolNames(monitor.Projectiles);
        var projectileScenePaths = ProjectileScenePaths(monitor.Projectiles);
        var projectileMovementModes = ProjectileMovementModes(monitor.Projectiles);
        TickMovement(runtime, 0.25f);
        var samplePositions = CapturePositions(monitor.Projectiles);
        TickMovement(runtime, 6.05f);
        await ProcessFrames(3);

        var hpAfter = target.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var modeOk = AllValuesEqual(projectileMovementModes, MoveMode.Orbit.ToString());
        var cleanupOk = AllRuntimeEntitiesDestroyed(monitor.Projectiles);
        var poolReturnOk = AllVisualsReturnedToPools(visualPoolNames);
        var movedDistance = MaxMovedDistance(initialPositions, samplePositions);
        var success = report.Success
            && monitor.Projectiles.Count == 3
            && AllValuesEqual(projectileScenePaths, "res://assets/Projectile/Projectile/Polygon2D/BulletDiamond.tscn")
            && modeOk
            && movedDistance > 0.001f
            && monitor.CollisionCount > 0
            && hpAfter < hpBefore
            && cleanupOk
            && poolReturnOk
            && visualBeforeCleanup > 0;

        var details = CreateBaseDetails(player, ability, abilityId);
        details["triggerResult"] = report.Result.ToString();
        details["triggerSuccess"] = report.Success;
        details["expectedProjectileCount"] = 3;
        details["spawnedProjectileIds"] = EntityIds(monitor.Projectiles);
        details["projectileScenePaths"] = projectileScenePaths;
        details["movementModes"] = projectileMovementModes;
        details["expectedMovementMode"] = MoveMode.Orbit.ToString();
        details["orbitRadius"] = ability.Data.Get<float>(MovementDataKeys.OrbitRadius, 0f);
        details["orbitAngularSpeed"] = ability.Data.Get<float>(MovementDataKeys.OrbitAngularSpeed, 0f);
        details["initialPositions"] = initialPositions;
        details["samplePositions"] = samplePositions;
        details["maxMovedDistance"] = movedDistance;
        details["collisionCount"] = monitor.CollisionCount;
        details["targetHpBefore"] = hpBefore;
        details["targetHpAfter"] = hpAfter;
        details["destroyedEntityIds"] = monitor.DestroyedIds;
        details["cleanupRuntimeDestroyed"] = cleanupOk;
        details["visualNodeCountBeforeCleanup"] = visualBeforeCleanup;
        details["visualNodeCountAfterCleanup"] = CountVisualNodes(monitor.Projectiles);
        details["visualPoolNames"] = visualPoolNames;
        details["visualPoolReleasedCounts"] = VisualPoolReleasedCounts(visualPoolNames);
        details["visualPoolReturnOk"] = poolReturnOk;
        details["behaviorExpectation"] = "Orbit projectiles circle the player, collide with enemies, and clean up after max duration.";

        await DestroyTarget(target);
        return new SkillProbeEvidence(
            abilityId,
            success,
            success ? "orbit passive behavior validated" : "orbit passive evidence incomplete",
            details);
    }

    private async Task<SkillProbeEvidence> RunCircleDamageProbe(GodotEntity2D player)
    {
        const string abilityId = "circle_damage";
        var ability = RequireAbility(player, abilityId);
        var inside = CreateTarget("circle-damage-inside", new Vector2Value(160f, 0f), 24f, 100f, 2);
        var outside = CreateTarget("circle-damage-outside", new Vector2Value(620f, 0f), 24f, 100f, 2);
        var ally = CreateTarget("circle-damage-ally", new Vector2Value(120f, 0f), 24f, 100f, 1);
        using var monitor = new SkillEventMonitor(ability.EntityId);
        var insideBefore = inside.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var outsideBefore = outside.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var allyBefore = ally.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var report = AbilityService.Instance.TryTrigger(new AbilityCastContext
        {
            Caster = player,
            Ability = ability
        });
        await ProcessFrames(2);

        var insideAfter = inside.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var outsideAfter = outside.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var allyAfter = ally.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var effectScenePaths = EffectScenePaths(monitor.Effects);
        var effectSceneOk = AllValuesEqual(effectScenePaths, "res://assets/Effect/003/AnimatedSprite2D/003.tscn");
        var visualBeforeCleanup = CountVisualNodes(monitor.Effects);
        var visualPoolNames = VisualPoolNames(monitor.Effects);
        DestroyRuntimeEntities(monitor.Effects);
        await ProcessFrames(3);
        var cleanupOk = AllRuntimeEntitiesDestroyed(monitor.Effects);
        var poolReturnOk = AllVisualsReturnedToPools(visualPoolNames);
        var radius = ability.Data.Get<float>(AbilityDataKeys.EffectRadius, 0f);
        var success = report.Success
            && report.Executed?.TargetsHit == 1
            && insideAfter < insideBefore
            && MathF.Abs(outsideAfter - outsideBefore) < 0.001f
            && MathF.Abs(allyAfter - allyBefore) < 0.001f
            && monitor.Effects.Count > 0
            && effectSceneOk
            && cleanupOk
            && poolReturnOk
            && visualBeforeCleanup > 0;

        var details = CreateBaseDetails(player, ability, abilityId);
        details["triggerResult"] = report.Result.ToString();
        details["triggerSuccess"] = report.Success;
        details["targetsHitReported"] = report.Executed?.TargetsHit ?? 0;
        details["effectRadius"] = radius;
        details["effectEntityIds"] = EntityIds(monitor.Effects);
        details["effectScenePaths"] = effectScenePaths;
        details["expectedEffectScenePath"] = "res://assets/Effect/003/AnimatedSprite2D/003.tscn";
        details["insideEnemyHpBefore"] = insideBefore;
        details["insideEnemyHpAfter"] = insideAfter;
        details["outsideEnemyHpBefore"] = outsideBefore;
        details["outsideEnemyHpAfter"] = outsideAfter;
        details["allyHpBefore"] = allyBefore;
        details["allyHpAfter"] = allyAfter;
        details["cleanupRuntimeDestroyed"] = cleanupOk;
        details["visualNodeCountBeforeCleanup"] = visualBeforeCleanup;
        details["visualNodeCountAfterCleanup"] = CountVisualNodes(monitor.Effects);
        details["visualPoolNames"] = visualPoolNames;
        details["visualPoolReleasedCounts"] = VisualPoolReleasedCounts(visualPoolNames);
        details["visualPoolReturnOk"] = poolReturnOk;
        details["destroyedEntityIds"] = monitor.DestroyedIds;
        details["behaviorExpectation"] = "Circle damage affects enemy targets within radius only and spawns the authored aura effect.";

        await DestroyTarget(inside);
        await DestroyTarget(outside);
        await DestroyTarget(ally);
        return new SkillProbeEvidence(
            abilityId,
            success,
            success ? "circle damage passive behavior validated" : "circle damage passive evidence incomplete",
            details);
    }

    private async Task<SkillProbeEvidence> RunAuraShieldProbe(BrotatoLikeGameRuntime runtime, GodotEntity2D player)
    {
        const string abilityId = "aura_shield";
        var ability = RequireAbility(player, abilityId);
        var target = CreateTarget("aura-shield-target", new Vector2Value(64f, 0f), 28f, 600f, 2);
        using var monitor = new SkillEventMonitor(ability.EntityId);
        var hpBefore = target.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var report = AbilityService.Instance.TryTrigger(new AbilityCastContext
        {
            Caster = player,
            Ability = ability,
            TargetPosition = new Vector2Value(64f, 0f)
        });
        await ProcessFrames(2);

        var initialPositions = CapturePositions(monitor.Projectiles);
        var visualBeforeCleanup = CountVisualNodes(monitor.Projectiles);
        var visualPoolNames = VisualPoolNames(monitor.Projectiles);
        var projectileScenePaths = ProjectileScenePaths(monitor.Projectiles);
        var projectileMovementModes = ProjectileMovementModes(monitor.Projectiles);
        TickMovement(runtime, 0.1f);
        var hitPositions = CapturePositions(monitor.Projectiles);
        SetPosition(player, new Vector2Value(32f, 0f));
        TickMovement(runtime, 0.1f);
        var followPositions = CapturePositions(monitor.Projectiles);
        TickMovement(runtime, 6.05f);
        await ProcessFrames(3);

        var hpAfter = target.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var modeOk = AllValuesEqual(projectileMovementModes, MoveMode.AttachToHost.ToString());
        var cleanupOk = AllRuntimeEntitiesDestroyed(monitor.Projectiles);
        var poolReturnOk = AllVisualsReturnedToPools(visualPoolNames);
        var followDistance = DistanceToAny(followPositions, new Vector2Value(96f, 0f));
        var success = report.Success
            && monitor.Projectiles.Count == 1
            && AllValuesEqual(projectileScenePaths, "res://assets/Projectile/Projectile/Polygon2D/BulletDiamond.tscn")
            && modeOk
            && monitor.CollisionCount > 0
            && hpAfter < hpBefore
            && followDistance <= 4f
            && cleanupOk
            && poolReturnOk
            && visualBeforeCleanup > 0;

        var details = CreateBaseDetails(player, ability, abilityId);
        details["triggerResult"] = report.Result.ToString();
        details["triggerSuccess"] = report.Success;
        details["expectedProjectileCount"] = 1;
        details["spawnedProjectileIds"] = EntityIds(monitor.Projectiles);
        details["projectileScenePaths"] = projectileScenePaths;
        details["movementModes"] = projectileMovementModes;
        details["expectedMovementMode"] = MoveMode.AttachToHost.ToString();
        details["attachOffsetDistance"] = ability.Data.Get<float>(MovementDataKeys.HandlerMaxDistance, 0f);
        details["initialPositions"] = initialPositions;
        details["hitPositions"] = hitPositions;
        details["followPositionsAfterPlayerMove"] = followPositions;
        details["expectedFollowPosition"] = Format(new Vector2Value(96f, 0f));
        details["followDistance"] = followDistance;
        details["collisionCount"] = monitor.CollisionCount;
        details["targetHpBefore"] = hpBefore;
        details["targetHpAfter"] = hpAfter;
        details["destroyedEntityIds"] = monitor.DestroyedIds;
        details["cleanupRuntimeDestroyed"] = cleanupOk;
        details["visualNodeCountBeforeCleanup"] = visualBeforeCleanup;
        details["visualNodeCountAfterCleanup"] = CountVisualNodes(monitor.Projectiles);
        details["visualPoolNames"] = visualPoolNames;
        details["visualPoolReleasedCounts"] = VisualPoolReleasedCounts(visualPoolNames);
        details["visualPoolReturnOk"] = poolReturnOk;
        details["behaviorExpectation"] = "Aura shield is an AttachToHost projectile that follows the player, damages contact targets, and cleans up after max duration.";

        await DestroyTarget(target);
        return new SkillProbeEvidence(
            abilityId,
            success,
            success ? "aura shield passive behavior validated" : "aura shield passive evidence incomplete",
            details);
    }

    private static SkillProbeEvidence BuildGroupingEvidence(
        GodotEntity2D player,
        IReadOnlyDictionary<string, SkillProbeEvidence> values)
    {
        var abilityIds = new[]
        {
            "sine_wave_shot",
            "boomerang_throw",
            "bezier_shot",
            "parabola_shot",
            "arc_shot",
            "orbit_skill",
            "circle_damage",
            "aura_shield"
        };
        var grouped = true;
        for (var i = 0; i < abilityIds.Length; i++)
        {
            grouped &= values.ContainsKey(abilityIds[i]);
        }

        var details = new Dictionary<string, object?>
        {
            ["groupedAbilityIds"] = abilityIds,
            ["loadoutSource"] = ReadMetaString(player, BrotatoLikeSkillLoadoutAuthoring.LoadoutSourceMeta),
            ["ownedAbilityEntityIds"] = ReadMetaString(player, BrotatoLikeSkillLoadoutAuthoring.OwnedAbilityEntityIdsMeta),
            ["ownedAbilityRecordIds"] = BrotatoLikeSkillLoadoutAuthoring.ValidationAllSkillAbilityIds,
            ["totalOwnedCount"] = ReadMetaInt(player, BrotatoLikeSkillLoadoutAuthoring.TotalOwnedCountMeta),
            ["visibleSlotCount"] = ReadMetaInt(player, BrotatoLikeSkillLoadoutAuthoring.VisibleSlotCountMeta),
            ["hiddenOwnedCount"] = ReadMetaInt(player, BrotatoLikeSkillLoadoutAuthoring.HiddenOwnedCountMeta)
        };
        return new SkillProbeEvidence(
            "ability_id_grouping",
            grouped
            && ReadMetaString(player, BrotatoLikeSkillLoadoutAuthoring.LoadoutSourceMeta) == BrotatoLikeSkillLoadoutAuthoring.SourceValidationOverride
            && ReadMetaInt(player, BrotatoLikeSkillLoadoutAuthoring.TotalOwnedCountMeta) >= abilityIds.Length,
            "artifact checks are grouped by ability id",
            details);
    }

    private static CheckResult Result(IReadOnlyDictionary<string, SkillProbeEvidence> values, string abilityId)
    {
        if (!values.TryGetValue(abilityId, out var evidence))
        {
            return CheckResult.Fail($"missing evidence for {abilityId}", new Dictionary<string, object?>
            {
                ["abilityId"] = abilityId
            });
        }

        return CheckResult.From(evidence.Success, evidence.Message, evidence.Details);
    }

    private GodotEntity2D CreateTarget(string id, Vector2Value position, float radius, float hp, int team)
    {
        var target = new GodotEntity2D
        {
            Name = id,
            EntityIdOverride = $"skill-validation-{id}",
            Position = new Vector2(position.X, position.Y)
        };
        target.Data.Set(MovementDataKeys.Position, position);
        target.Data.Set(CollisionDataKeys.CollisionLayer, CollisionLayers.EnemyHurtbox);
        target.Data.Set(CollisionDataKeys.CollisionMask, CollisionLayers.Projectile);
        target.Data.Set(CollisionDataKeys.CollisionRadius, radius);
        target.Data.Set(CollisionDataKeys.Team, team);
        target.Data.Set(DamageDataKeys.MaxHp, hp);
        target.Data.Set(DamageDataKeys.CurrentHp, hp);
        target.Data.Set(DamageDataKeys.IsDead, false);
        AddChild(target);
        return target;
    }

    private static IEntity RequireAbility(GodotEntity2D player, string abilityRecordId)
    {
        var abilityId = new EntityId($"ability-{abilityRecordId}-{player.EntityId.Value}");
        return EntityManager.Get(abilityId)
            ?? throw new InvalidOperationException($"Missing validation ability entity: {abilityId.Value}");
    }

    private static Dictionary<string, object?> CreateBaseDetails(GodotEntity2D player, IEntity ability, string abilityId)
    {
        return new Dictionary<string, object?>
        {
            ["abilityId"] = abilityId,
            ["abilityEntityId"] = ability.EntityId.Value,
            ["loadoutSource"] = ReadMetaString(player, BrotatoLikeSkillLoadoutAuthoring.LoadoutSourceMeta),
            ["ownedAbilityEntityIds"] = ReadMetaString(player, BrotatoLikeSkillLoadoutAuthoring.OwnedAbilityEntityIdsMeta),
            ["totalOwnedCount"] = ReadMetaInt(player, BrotatoLikeSkillLoadoutAuthoring.TotalOwnedCountMeta),
            ["visibleSlotCount"] = ReadMetaInt(player, BrotatoLikeSkillLoadoutAuthoring.VisibleSlotCountMeta),
            ["hiddenOwnedCount"] = ReadMetaInt(player, BrotatoLikeSkillLoadoutAuthoring.HiddenOwnedCountMeta),
            ["abilityTriggerMode"] = ability.Data.Get<AbilityTriggerMode>(AbilityDataKeys.TriggerMode, AbilityTriggerMode.None).ToString(),
            ["abilityType"] = ability.Data.Get<AbilityType>(AbilityDataKeys.Type, AbilityType.Active).ToString(),
            ["featureHandlerId"] = ability.Data.Get(AbilityDataKeys.FeatureHandlerId, string.Empty)
        };
    }

    private static void TickMovement(BrotatoLikeGameRuntime runtime, float seconds)
    {
        var remaining = seconds;
        while (remaining > 0f)
        {
            var step = MathF.Min(0.05f, remaining);
            runtime.MovementDriver!.TickMovement(step);
            remaining -= step;
        }
    }

    private async Task ProcessFrames(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private async Task DestroyTarget(GodotEntity2D target)
    {
        if (GodotObject.IsInstanceValid(target))
        {
            target.DestroyEntity();
        }

        await ProcessFrames(1);
    }

    private static void SetPosition(GodotEntity2D entity, Vector2Value position)
    {
        entity.Position = new Vector2(position.X, position.Y);
        entity.Data.Set(MovementDataKeys.Position, position);
    }

    private static IReadOnlyDictionary<string, string> CapturePositions(IReadOnlyList<IEntity> entities)
    {
        var positions = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < entities.Count; i++)
        {
            positions[entities[i].EntityId.Value] = Format(ReadPosition(entities[i]));
        }

        return positions;
    }

    private static float MaxMovedDistance(
        IReadOnlyDictionary<string, string> initialPositions,
        IReadOnlyDictionary<string, string> samplePositions)
    {
        var max = 0f;
        foreach (var pair in initialPositions)
        {
            if (!samplePositions.TryGetValue(pair.Key, out var sample))
            {
                continue;
            }

            max = MathF.Max(max, Distance(Parse(pair.Value), Parse(sample)));
        }

        return max;
    }

    private static float DistanceToAny(IReadOnlyDictionary<string, string> positions, Vector2Value expected)
    {
        var min = float.MaxValue;
        foreach (var pair in positions)
        {
            min = MathF.Min(min, Distance(Parse(pair.Value), expected));
        }

        return min == float.MaxValue ? -1f : min;
    }

    private static Vector2Value ReadPosition(IEntity entity)
    {
        return entity.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
    }

    private static bool AllProjectilesHaveScene(IReadOnlyList<IEntity> projectiles, string scenePath)
    {
        if (projectiles.Count == 0)
        {
            return false;
        }

        for (var i = 0; i < projectiles.Count; i++)
        {
            if (projectiles[i].Data.Get(ProjectileDataKeys.ScenePath, string.Empty) != scenePath)
            {
                return false;
            }
        }

        return true;
    }

    private static bool AllValuesEqual(IReadOnlyList<string> values, string expected)
    {
        if (values.Count == 0)
        {
            return false;
        }

        for (var i = 0; i < values.Count; i++)
        {
            if (values[i] != expected)
            {
                return false;
            }
        }

        return true;
    }

    private static bool AllEffectsHaveScene(IReadOnlyList<IEntity> effects, string scenePath)
    {
        if (effects.Count == 0)
        {
            return false;
        }

        for (var i = 0; i < effects.Count; i++)
        {
            if (effects[i].Data.Get(EffectDataKeys.ScenePath, string.Empty) != scenePath)
            {
                return false;
            }
        }

        return true;
    }

    private static bool AllProjectilesHaveMode(IReadOnlyList<IEntity> projectiles, MoveMode mode)
    {
        if (projectiles.Count == 0)
        {
            return false;
        }

        for (var i = 0; i < projectiles.Count; i++)
        {
            if (projectiles[i].Data.Get<MoveMode>(MovementDataKeys.HandlerMoveMode, MoveMode.None) != mode)
            {
                return false;
            }
        }

        return true;
    }

    private static IReadOnlyList<string> EntityIds(IReadOnlyList<IEntity> entities)
    {
        var ids = new List<string>();
        for (var i = 0; i < entities.Count; i++)
        {
            ids.Add(entities[i].EntityId.Value);
        }

        return ids;
    }

    private static IReadOnlyList<string> ProjectileScenePaths(IReadOnlyList<IEntity> projectiles)
    {
        var values = new List<string>();
        for (var i = 0; i < projectiles.Count; i++)
        {
            values.Add(projectiles[i].Data.Get(ProjectileDataKeys.ScenePath, string.Empty));
        }

        return values;
    }

    private static IReadOnlyList<string> EffectScenePaths(IReadOnlyList<IEntity> effects)
    {
        var values = new List<string>();
        for (var i = 0; i < effects.Count; i++)
        {
            values.Add(effects[i].Data.Get(EffectDataKeys.ScenePath, string.Empty));
        }

        return values;
    }

    private static IReadOnlyList<string> ProjectileMovementModes(IReadOnlyList<IEntity> projectiles)
    {
        var values = new List<string>();
        for (var i = 0; i < projectiles.Count; i++)
        {
            values.Add(projectiles[i].Data.Get<MoveMode>(MovementDataKeys.HandlerMoveMode, MoveMode.None).ToString());
        }

        return values;
    }

    private static int CountVisualNodes(IReadOnlyList<IEntity> entities)
    {
        var count = 0;
        for (var i = 0; i < entities.Count; i++)
        {
            if (GodotNodeRegistry.GetNodeById(entities[i].EntityId.Value) != null)
            {
                count++;
            }
        }

        return count;
    }

    private static IReadOnlyList<string> VisualPoolNames(IReadOnlyList<IEntity> entities)
    {
        var names = new List<string>();
        for (var i = 0; i < entities.Count; i++)
        {
            var node = GodotNodeRegistry.GetNodeById(entities[i].EntityId.Value);
            if (node != null && node.HasMeta("GameOSVisualPoolName"))
            {
                names.Add(node.GetMeta("GameOSVisualPoolName").AsString());
            }
        }

        return names;
    }

    private static bool AllVisualsReturnedToPools(IReadOnlyList<string> poolNames)
    {
        if (poolNames.Count == 0)
        {
            return false;
        }

        var required = CountPoolNames(poolNames);
        var stats = ObjectPoolManager.GetAllStats();
        foreach (var pair in required)
        {
            if (!stats.TryGetValue(pair.Key, out var poolStats) || poolStats.TotalReleased < pair.Value)
            {
                return false;
            }
        }

        return true;
    }

    private static IReadOnlyDictionary<string, int> VisualPoolReleasedCounts(IReadOnlyList<string> poolNames)
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        var stats = ObjectPoolManager.GetAllStats();
        foreach (var pair in CountPoolNames(poolNames))
        {
            result[pair.Key] = stats.TryGetValue(pair.Key, out var poolStats)
                ? poolStats.TotalReleased
                : 0;
        }

        return result;
    }

    private static Dictionary<string, int> CountPoolNames(IReadOnlyList<string> poolNames)
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < poolNames.Count; i++)
        {
            result[poolNames[i]] = result.GetValueOrDefault(poolNames[i]) + 1;
        }

        return result;
    }

    private static bool AllRuntimeEntitiesDestroyed(IReadOnlyList<IEntity> entities)
    {
        if (entities.Count == 0)
        {
            return false;
        }

        for (var i = 0; i < entities.Count; i++)
        {
            if (EntityManager.Get(entities[i].EntityId) != null)
            {
                return false;
            }
        }

        return true;
    }

    private static void DestroyRuntimeEntities(IReadOnlyList<IEntity> entities)
    {
        for (var i = 0; i < entities.Count; i++)
        {
            if (EntityManager.Get(entities[i].EntityId) != null)
            {
                EntityManager.Destroy(entities[i]);
            }
        }
    }

    private static string ReadMetaString(Node node, string key)
    {
        return node.HasMeta(key) ? node.GetMeta(key).AsString() : string.Empty;
    }

    private static int ReadMetaInt(Node node, string key)
    {
        return node.HasMeta(key) ? node.GetMeta(key).AsInt32() : 0;
    }

    private static string Format(Vector2Value value)
    {
        return $"{value.X:0.###},{value.Y:0.###}";
    }

    private static Vector2Value Parse(string value)
    {
        var parts = value.Split(',', StringSplitOptions.TrimEntries);
        return parts.Length == 2
            && float.TryParse(parts[0], out var x)
            && float.TryParse(parts[1], out var y)
            ? new Vector2Value(x, y)
            : Vector2Value.Zero;
    }

    private static float Distance(Vector2Value a, Vector2Value b)
    {
        return Vector2Value.Distance(a, b);
    }

    private sealed class SkillEventMonitor : IDisposable
    {
        private readonly EntityId abilityEntityId;
        private readonly List<IDisposable> tokens = new();

        public SkillEventMonitor(EntityId abilityEntityId)
        {
            this.abilityEntityId = abilityEntityId;
            tokens.Add(SlimeAI.GameOS.Runtime.Event.WorldEvents.World.Subscribe<ProjectileSpawned>(OnProjectileSpawned));
            tokens.Add(SlimeAI.GameOS.Runtime.Event.WorldEvents.World.Subscribe<EffectSpawned>(OnEffectSpawned));
            tokens.Add(SlimeAI.GameOS.Runtime.Event.WorldEvents.World.Subscribe<EntityDestroyed>(OnEntityDestroyed));
        }

        public List<IEntity> Projectiles { get; } = new();

        public List<IEntity> Effects { get; } = new();

        public List<string> DestroyedIds { get; } = new();

        public int CollisionCount { get; private set; }

        public void Dispose()
        {
            for (var i = 0; i < tokens.Count; i++)
            {
                tokens[i].Dispose();
            }

            tokens.Clear();
        }

        private void OnProjectileSpawned(ProjectileSpawned data)
        {
            if (data.Ability?.EntityId != abilityEntityId)
            {
                return;
            }

            Projectiles.Add(data.Projectile);
            tokens.Add(data.Projectile.Events.Subscribe<MovementCollision>(_ => CollisionCount++));
        }

        private void OnEffectSpawned(EffectSpawned data)
        {
            if (data.Ability?.EntityId == abilityEntityId)
            {
                Effects.Add(data.Effect);
            }
        }

        private void OnEntityDestroyed(EntityDestroyed data)
        {
            var id = data.Entity.EntityId.Value;
            if (Contains(Projectiles, id) || Contains(Effects, id))
            {
                DestroyedIds.Add(id);
            }
        }

        private static bool Contains(IReadOnlyList<IEntity> entities, string entityId)
        {
            for (var i = 0; i < entities.Count; i++)
            {
                if (entities[i].EntityId.Value == entityId)
                {
                    return true;
                }
            }

            return false;
        }
    }

    private sealed record ProjectileSkillCase
    {
        public required string AbilityId { get; init; }

        public required MoveMode ExpectedMode { get; init; }

        public required string ExpectedScenePath { get; init; }

        public Vector2Value TargetPosition { get; init; }

        public float TargetRadius { get; init; }

        public int ExpectedProjectileCount { get; init; } = 1;

        public bool UseEntityTarget { get; init; }

        public float SampleSeconds { get; init; } = 0.25f;

        public float CompletionSeconds { get; init; } = 1.5f;

        public string BehaviorExpectation { get; init; } = string.Empty;
    }

    private sealed record SkillProbeEvidence(
        string AbilityId,
        bool Success,
        string Message,
        IReadOnlyDictionary<string, object?> Details);
}
