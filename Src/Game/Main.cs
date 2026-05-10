using System;
using System.Collections.Generic;
using SkilmeAI.GameOS.Capabilities.AI;
using Godot;
using SkilmeAI.GameOS.Capabilities.Ability;
using SkilmeAI.GameOS.Capabilities.Attack;
using SkilmeAI.GameOS.Capabilities.Collision;
using SkilmeAI.GameOS.Capabilities.Damage;
using SkilmeAI.GameOS.Capabilities.Effect;
using SkilmeAI.GameOS.Capabilities.Feature;
using SkilmeAI.GameOS.Capabilities.Movement;
using SkilmeAI.GameOS.Capabilities.Projectile;
using SkilmeAI.GameOS.Capabilities.Unit;
using SkilmeAI.GameOS.GodotBridge;
using SkilmeAI.GameOS.Observation;
using SkilmeAI.GameOS.Runtime.Entity;
using SkilmeAI.GameOS.Runtime.Event;
using SkilmeAI.GameOS.Runtime.Relationship;
using SkilmeAI.GameOS.Runtime.Resource;
using SkilmeAI.GameOS.Runtime.Schedule;
using SkilmeAI.GameOS.Runtime.Timer;

namespace BrotatoLike.Game;

/// <summary>
/// BrotatoLike 仓库第一个启动场景节点。
/// </summary>
public partial class Main : Node
{
    private static readonly GameOSContextLog Log = GameOSLog.For("BrotatoLike.Main");

    /// <inheritdoc />
    public override void _Ready()
    {
        if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--gameos-smoke-exit") < 0)
        {
            var runtime = StartGameRuntime();
            if (BrotatoLikePlayableSliceAcceptance.ShouldRun())
            {
                var result = BrotatoLikePlayableSliceAcceptance.Run(this, runtime);
                if (result.Success)
                {
                    Log.Pass("BrotatoLike playable slice acceptance");
                }
                else
                {
                    Log.Fail("BrotatoLike playable slice acceptance", new Dictionary<string, object?>
                    {
                        ["failures"] = string.Join(";", result.Failures)
                    });
                }

                GD.Print(result.Success ? "BrotatoLike playable slice PASS" : "BrotatoLike playable slice FAIL");
                if (!result.Success)
                {
                    Log.Fail($"BrotatoLike playable slice failures: {string.Join("; ", result.Failures)}");
                }

                GetTree().Quit(result.Success ? 0 : 1);
            }

            return;
        }

        var probe = GameBootstrap.RunFrameworkSmokeProbe();
        var bridgeProbe = RunGodotBridgeProbe();
        var dataOsProbe = RunDataOSSnapshotProbe();
        var mainEntryProbe = RunMainEntryProbe();
        Log.Info($"BrotatoLike GameOS smoke: {probe.EntityId} {probe.MainScenePath} bridge:{bridgeProbe.ComponentBound} pool:{bridgeProbe.NodePoolReused} dataos:{dataOsProbe.SnapshotApplied} main:{mainEntryProbe.GameStartedEventEmitted}");

        var success = probe.DataEventCount == 1
            && probe.RelationshipBound
            && probe.PoolCreated == 1
            && probe.TimerCompleted
            && probe.ScheduleRunning
            && probe.MainScenePath == "res://Scenes/Main.tscn"
            && probe.MovementCompleted
            && Math.Abs(probe.MovementPositionX - 12f) < 0.001f
            && probe.CollisionEntered
            && probe.MovementCollision
            && bridgeProbe.EntityRegistered
            && bridgeProbe.ComponentBound
            && bridgeProbe.ComponentCallback
            && bridgeProbe.CollisionBridgeEntered
            && bridgeProbe.HurtboxBridgeEntered
            && bridgeProbe.NodePoolReused
            && bridgeProbe.MovementNodeSynced
            && bridgeProbe.OrbitNodeSynced
            && bridgeProbe.SineNodeSynced
            && bridgeProbe.BezierNodeSynced
            && bridgeProbe.BoomerangNodeSynced
            && bridgeProbe.AttachNodeSynced
            && bridgeProbe.PlayerInputNodeSynced
            && bridgeProbe.AIControlledNodeSynced
            && bridgeProbe.ParabolaNodeSynced
            && bridgeProbe.CircularArcNodeSynced
            && bridgeProbe.MovementCollisionNodeSynced
            && bridgeProbe.OrientationNodeSynced
            && bridgeProbe.OrientationSpinSynced
            && bridgeProbe.ContactDamageSynced
            && bridgeProbe.AttackBridgeSynced
            && bridgeProbe.AttackAnimationSynced
            && bridgeProbe.AIBridgeSynced
            && bridgeProbe.AbilityPointSynced
            && bridgeProbe.AbilityAutoTargetSynced
            && bridgeProbe.AbilityDataOSHandlerSynced
            && bridgeProbe.ProjectileRuntimeSynced
            && bridgeProbe.EffectRuntimeSynced
            && bridgeProbe.PlayerInputBridgeSynced
            && bridgeProbe.ActiveSkillInputSynced
            && dataOsProbe.SnapshotApplied
            && dataOsProbe.AbilityApplied
            && dataOsProbe.ResourcesRegistered
            && dataOsProbe.SpawnSystemSynced
            && mainEntryProbe.GameStartedEventEmitted
            && mainEntryProbe.SmokeEntryKeptSeparate
            && mainEntryProbe.CameraMounted;
        GD.Print(success ? "BrotatoLike GameOS smoke PASS" : "BrotatoLike GameOS smoke FAIL");
        GetTree().Quit(success ? 0 : 1);
    }

    private BrotatoLikeGameRuntime StartGameRuntime()
    {
        var runtime = GetNodeOrNull<BrotatoLikeGameRuntime>("GameRuntime");
        if (runtime == null)
        {
            runtime = new BrotatoLikeGameRuntime
            {
                Name = "GameRuntime",
                AutoInitialize = false,
                AutoStartGameplay = true,
                AutoTick = true
            };
            AddChild(runtime);
        }

        if (!runtime.IsInitialized)
        {
            runtime.InitializeFromDataOS();
            runtime.SpawnPlayer();
            runtime.BeginGameplay();
            GlobalEventBus.Global.Emit(
                BrotatoLikeGameEventType.Game.Started,
                new BrotatoLikeGameEventType.Game.StartedEventData(runtime, this, runtime.InitialWave));
            Log.Info("BrotatoLike main scene initialized");
        }

        return runtime;
    }

    private MainEntryProbe RunMainEntryProbe()
    {
        var emittedAfterExplicitStart = false;
        Action<BrotatoLikeGameEventType.Game.StartedEventData> startedHandler = data =>
        {
            emittedAfterExplicitStart = data.Runtime.IsInitialized
                && data.EntryNode == this
                && data.Wave == data.Runtime.InitialWave;
        };
        GlobalEventBus.Global.On(BrotatoLikeGameEventType.Game.Started, startedHandler);

        var runtimeBeforeExplicitStart = GetNodeOrNull<BrotatoLikeGameRuntime>("GameRuntime");
        var runtimeInitializedBeforeExplicitStart = runtimeBeforeExplicitStart?.IsInitialized == true;
        StartGameRuntime();
        GlobalEventBus.Global.Off(BrotatoLikeGameEventType.Game.Started, startedHandler);
        var cameraMounted = GetNodeOrNull<Camera2D>("Camera2D") != null;
        return new MainEntryProbe(emittedAfterExplicitStart, !runtimeInitializedBeforeExplicitStart, cameraMounted);
    }

    private DataOSProbe RunDataOSSnapshotProbe()
    {
        var bootstrap = BrotatoLikeDataOSBootstrap.LoadFromResource();
        var enemy = bootstrap.SpawnEntityFromRecord("unit.enemy", "yuren", "dataos-smoke-enemy-yuren");
        var targetingIndicator = bootstrap.SpawnEntityFromRecord(
            "unit.targeting_indicator",
            "default",
            "dataos-smoke-targeting-indicator");
        var ability = bootstrap.SpawnEntityFromRecord("ability", "chain_lightning", "dataos-smoke-ability-chain-lightning");
        var targetPointAbility = bootstrap.SpawnEntityFromRecord("ability", "target_point_skill", "dataos-smoke-ability-target-point");
        var projectileAbility = bootstrap.SpawnEntityFromRecord("ability", "parabola_shot", "dataos-smoke-ability-parabola-shot");
        var sineWaveAbility = bootstrap.SpawnEntityFromRecord("ability", "sine_wave_shot", "dataos-smoke-ability-sine-wave-shot");
        var boomerangAbility = bootstrap.SpawnEntityFromRecord("ability", "boomerang_throw", "dataos-smoke-ability-boomerang-throw");
        var orbitAbility = bootstrap.SpawnEntityFromRecord("ability", "orbit_skill", "dataos-smoke-ability-orbit-skill");
        var bezierAbility = bootstrap.SpawnEntityFromRecord("ability", "bezier_shot", "dataos-smoke-ability-bezier-shot");
        var arcShotAbility = bootstrap.SpawnEntityFromRecord("ability", "arc_shot", "dataos-smoke-ability-arc-shot");
        var dashAbility = bootstrap.SpawnEntityFromRecord("ability", "dash", "dataos-smoke-ability-dash");
        var circleDamageAbility = bootstrap.SpawnEntityFromRecord("ability", "circle_damage", "dataos-smoke-ability-circle-damage");
        var auraShieldAbility = bootstrap.SpawnEntityFromRecord("ability", "aura_shield", "dataos-smoke-ability-aura-shield");
        var systemConfig = bootstrap.SpawnEntityFromRecord("system.config", "SpawnSystem", "dataos-smoke-system-spawn");
        var systemPreset = bootstrap.SpawnEntityFromRecord("system.preset", "Default", "dataos-smoke-system-preset");
        var spawnConfig = bootstrap.SpawnEntityFromRecord("spawn.config", "default", "dataos-smoke-spawn-config");
        var spawnCatalog = bootstrap.BuildEnemySpawnCatalog(wave: 1);
        var spawnSystemProbe = RunDataOSSpawnSystemProbe(bootstrap, spawnCatalog);

        var resourceCount = bootstrap.RegisterResources();
        var enemyApplied = Math.Abs(enemy.Data.Get<float>(DamageDataKeys.MaxHp) - 150f) < 0.001f
            && Math.Abs(enemy.Data.Get<float>(MovementDataKeys.MoveSpeed) - 150f) < 0.001f
            && enemy.Data.Get<string>(UnitDataKeys.VisualScenePath) == "res://assets/Unit/Enemy/yuren/AnimatedSprite2D/yuren.tscn"
            && enemy.Data.Get<int>(ScheduleDataKeys.SpawnSingleCount) == 3;
        var abilityApplied = ability.Data.Get<AbilityTriggerMode>(AbilityDataKeys.TriggerMode) == AbilityTriggerMode.Manual
            && ability.Data.Get<AbilityTargetSelection>(AbilityDataKeys.TargetSelection) == AbilityTargetSelection.Entity
            && Math.Abs(ability.Data.Get<float>(AbilityDataKeys.Damage) - 50f) < 0.001f
            && Math.Abs(ability.Data.Get<float>(AbilityDataKeys.CastRange) - 600f) < 0.001f
            && ability.Data.Get<int>(AbilityDataKeys.ChainCount) == 3
            && Math.Abs(ability.Data.Get<float>(AbilityDataKeys.ChainRange) - 300f) < 0.001f
            && Math.Abs(ability.Data.Get<float>(AbilityDataKeys.ChainDelay) - 0.2f) < 0.001f
            && Math.Abs(ability.Data.Get<float>(AbilityDataKeys.ChainDamageDecay) - 100f) < 0.001f
            && ability.Data.Get<string>(AbilityDataKeys.LineEffectScenePath) == string.Empty
            && ability.Data.Get<int>(AbilityDataKeys.AutoTargetMaxTargets) == 1
            && ability.Data.Get<bool>(AbilityDataKeys.AutoTargetIgnoreSameTeam)
            && ability.Data.Get<bool>(AbilityDataKeys.AutoTargetRequiresDamageable)
            && Math.Abs(ability.Data.Get<float>(AbilityDataKeys.DamageInterval)) < 0.001f
            && ability.Data.Get<int>(AbilityDataKeys.DamageRepeatCount) == 1
            && ability.Data.Get<bool>(AbilityDataKeys.ApplyImmediateDamage)
            && ability.Data.Get<int>(AbilityDataKeys.MaxLevel) == 10
            && !ability.Data.Get<bool>(AbilityDataKeys.UsesCharges)
            && Math.Abs(projectileAbility.Data.Get<float>(AbilityDataKeys.EffectRadius) - 250f) < 0.001f
            && targetPointAbility.Data.Get<AbilityTargetSelection>(AbilityDataKeys.TargetSelection) == AbilityTargetSelection.Point
            && targetPointAbility.Data.Get<string>(AbilityDataKeys.FeatureHandlerId) == "技能.主动.位置目标"
            && Math.Abs(targetPointAbility.Data.Get<float>(AbilityDataKeys.EffectRadius) - 200f) < 0.001f
            && Math.Abs(targetPointAbility.Data.Get<float>(AbilityDataKeys.Damage) - 10f) < 0.001f
            && targetPointAbility.Data.Get<string>(EffectDataKeys.Name) == "位置目标爆炸特效"
            && projectileAbility.Data.Get<int>(AbilityDataKeys.AutoTargetMaxTargets) == 1
            && projectileAbility.Data.Get<bool>(AbilityDataKeys.ApplyImmediateDamage)
            && projectileAbility.Data.Get<string>(AbilityDataKeys.FeatureGroupId) == "技能.投射物"
            && Math.Abs(projectileAbility.Data.Get<float>(ProjectileDataKeys.Speed) - 380f) < 0.001f
            && projectileAbility.Data.Get<int>(ProjectileDataKeys.MaxHitCount) == 1
            && Math.Abs(projectileAbility.Data.Get<float>(ProjectileDataKeys.MaxLifeTime) - 1.35f) < 0.001f
            && Math.Abs(projectileAbility.Data.Get<float>(ProjectileDataKeys.Damage) - 9f) < 0.001f
            && projectileAbility.Data.Get<string>(EffectDataKeys.Name) == "定点抛炸弹爆炸特效"
            && Math.Abs(sineWaveAbility.Data.Get<float>(ProjectileDataKeys.Speed) - 350f) < 0.001f
            && sineWaveAbility.Data.Get<int>(ProjectileDataKeys.MaxHitCount) == 1
            && Math.Abs(sineWaveAbility.Data.Get<float>(ProjectileDataKeys.Damage) - 25f) < 0.001f
            && sineWaveAbility.Data.Get<MoveMode>(MovementDataKeys.HandlerMoveMode) == MoveMode.SineWave
            && Math.Abs(sineWaveAbility.Data.Get<float>(MovementDataKeys.WaveAmplitude) - 60f) < 0.001f
            && Math.Abs(sineWaveAbility.Data.Get<float>(MovementDataKeys.WaveFrequency) - 2f) < 0.001f
            && Math.Abs(sineWaveAbility.Data.Get<float>(MovementDataKeys.HandlerMaxDistance) - 1800f) < 0.001f
            && Math.Abs(boomerangAbility.Data.Get<float>(ProjectileDataKeys.Speed) - 460f) < 0.001f
            && boomerangAbility.Data.Get<int>(ProjectileDataKeys.MaxHitCount) == -1
            && Math.Abs(boomerangAbility.Data.Get<float>(ProjectileDataKeys.Damage) - 22f) < 0.001f
            && boomerangAbility.Data.Get<MoveMode>(MovementDataKeys.HandlerMoveMode) == MoveMode.Boomerang
            && Math.Abs(boomerangAbility.Data.Get<float>(MovementDataKeys.BoomerangArcHeight) - 160f) < 0.001f
            && Math.Abs(boomerangAbility.Data.Get<float>(MovementDataKeys.BoomerangPauseTime) - 0.05f) < 0.001f
            && Math.Abs(boomerangAbility.Data.Get<float>(MovementDataKeys.BoomerangReturnSpeedMultiplier) - 1.35f) < 0.001f
            && boomerangAbility.Data.Get<bool>(MovementDataKeys.BoomerangIsClockwise)
            && orbitAbility.Data.Get<MoveMode>(MovementDataKeys.HandlerMoveMode) == MoveMode.Orbit
            && orbitAbility.Data.Get<int>(MovementDataKeys.HandlerProjectileCount) == 3
            && Math.Abs(orbitAbility.Data.Get<float>(MovementDataKeys.OrbitRadius) - 100f) < 0.001f
            && Math.Abs(orbitAbility.Data.Get<float>(MovementDataKeys.OrbitAngularSpeed) - 180f) < 0.001f
            && Math.Abs(orbitAbility.Data.Get<float>(MovementDataKeys.HandlerMaxTravelDuration) - 6f) < 0.001f
            && orbitAbility.Data.Get<bool>(MovementDataKeys.IsOrbitClockwise)
            && bezierAbility.Data.Get<MoveMode>(MovementDataKeys.HandlerMoveMode) == MoveMode.BezierCurve
            && bezierAbility.Data.Get<int>(MovementDataKeys.HandlerProjectileCount) == 5
            && bezierAbility.Data.Get<int>(MovementDataKeys.BezierDegree) == 5
            && bezierAbility.Data.Get<string>(MovementDataKeys.BezierPattern) == "Converge"
            && Math.Abs(bezierAbility.Data.Get<float>(MovementDataKeys.HandlerMinTravelDuration) - 0.85f) < 0.001f
            && Math.Abs(bezierAbility.Data.Get<float>(MovementDataKeys.HandlerMaxTravelDuration) - 1.45f) < 0.001f
            && projectileAbility.Data.Get<MoveMode>(MovementDataKeys.HandlerMoveMode) == MoveMode.CircularArc
            && Math.Abs(projectileAbility.Data.Get<float>(MovementDataKeys.HandlerMinTravelDuration) - 0.75f) < 0.001f
            && Math.Abs(projectileAbility.Data.Get<float>(MovementDataKeys.CircularArcRadiusScale) - 0.72f) < 0.001f
            && Math.Abs(projectileAbility.Data.Get<float>(MovementDataKeys.CircularArcRadiusMinOffset) - 32f) < 0.001f
            && !projectileAbility.Data.Get<bool>(MovementDataKeys.CircularArcClockwise)
            && projectileAbility.Data.Get<bool>(MovementDataKeys.BowWorldUp)
            && Math.Abs(arcShotAbility.Data.Get<float>(ProjectileDataKeys.Speed) - 390f) < 0.001f
            && arcShotAbility.Data.Get<MoveMode>(MovementDataKeys.HandlerMoveMode) == MoveMode.CircularArc
            && Math.Abs(arcShotAbility.Data.Get<float>(MovementDataKeys.HandlerMinTravelDuration) - 0.65f) < 0.001f
            && Math.Abs(arcShotAbility.Data.Get<float>(MovementDataKeys.HandlerMaxTravelDuration) - 1.5f) < 0.001f
            && Math.Abs(arcShotAbility.Data.Get<float>(MovementDataKeys.CircularArcRadiusScale) - 0.68f) < 0.001f
            && Math.Abs(arcShotAbility.Data.Get<float>(MovementDataKeys.CircularArcRadiusMinOffset) - 24f) < 0.001f
            && arcShotAbility.Data.Get<bool>(MovementDataKeys.CircularArcClockwise)
            && !arcShotAbility.Data.Get<bool>(MovementDataKeys.BowWorldUp)
            && dashAbility.Data.Get<MoveMode>(MovementDataKeys.HandlerMoveMode) == MoveMode.Charge
            && Math.Abs(dashAbility.Data.Get<float>(MovementDataKeys.MoveSpeed) - 1200f) < 0.001f
            && Math.Abs(dashAbility.Data.Get<float>(MovementDataKeys.HandlerMaxDistance) - 300f) < 0.001f
            && Math.Abs(dashAbility.Data.Get<float>(MovementDataKeys.HandlerMaxTravelDuration) - 0.25f) < 0.001f
            && circleDamageAbility.Data.Get<string>(EffectDataKeys.Name) == "烈焰光环特效"
            && Math.Abs(circleDamageAbility.Data.Get<float>(EffectDataKeys.Duration) + 1f) < 0.001f
            && auraShieldAbility.Data.Get<string>(AbilityDataKeys.FeatureHandlerId) == "技能.被动.光环护盾"
            && auraShieldAbility.Data.Get<MoveMode>(MovementDataKeys.HandlerMoveMode) == MoveMode.AttachToHost
            && auraShieldAbility.Data.Get<int>(MovementDataKeys.HandlerProjectileCount) == 1
            && Math.Abs(auraShieldAbility.Data.Get<float>(MovementDataKeys.HandlerMaxDistance) - 64f) < 0.001f
            && Math.Abs(auraShieldAbility.Data.Get<float>(MovementDataKeys.HandlerMaxTravelDuration) - 6f) < 0.001f
            && Math.Abs(auraShieldAbility.Data.Get<float>(ProjectileDataKeys.Damage) - 15f) < 0.001f
            && auraShieldAbility.Data.Get<string>(ProjectileDataKeys.ScenePath) == "res://assets/Projectile/Projectile/Polygon2D/BulletDiamond.tscn";
        var expandedDataApplied = targetingIndicator.Data.Get<bool>(DamageDataKeys.IsInvulnerable)
            && Math.Abs(targetingIndicator.Data.Get<float>(MovementDataKeys.MoveSpeed) - 400f) < 0.001f
            && systemConfig.Data.Get<SystemGroup>(ScheduleDataKeys.MountGroup) == SystemGroup.Gameplay
            && systemConfig.Data.Get<string>(ScheduleDataKeys.BlockedOverlays) == "Blocking"
            && systemPreset.Data.Get<bool>(ScheduleDataKeys.PresetIsActive)
            && Math.Abs(spawnConfig.Data.Get<float>(ScheduleDataKeys.WaveDuration) - 60f) < 0.001f
            && spawnConfig.Data.Get<int>(ScheduleDataKeys.MaxWaves) == 20
            && spawnCatalog.EnemyRules.Count == 2
            && Math.Abs(spawnCatalog.WaveDuration - 60f) < 0.001f
            && spawnCatalog.MaxWaves == 20
            && spawnCatalog.EnemyRules[0].RecordId == "chailangren"
            && spawnCatalog.EnemyRules[0].SingleCount == 2
            && spawnCatalog.EnemyRules[0].PositionStrategy == "Circle"
            && spawnCatalog.EnemyRules[1].RecordId == "yuren"
            && spawnCatalog.EnemyRules[1].SingleCount == 3
            && spawnCatalog.EnemyRules[1].VisualScenePath == "res://assets/Unit/Enemy/yuren/AnimatedSprite2D/yuren.tscn";
        var resourcesRegistered = resourceCount > 0
            && ResourceManagement.GetPath("Unit.Enemy.Yuren.Visual", ResourceCategory.Asset) == "res://assets/Unit/Enemy/yuren/AnimatedSprite2D/yuren.tscn"
            && ResourceManagement.GetPath("TargetingIndicatorEntity", ResourceCategory.Entity) == "res://Src/ECS/Base/Entity/Unit/TargetingIndicator/TargetingIndicatorEntity.tscn"
            && ResourceManagement.GetPath("System.DefaultPreset", ResourceCategory.Config) == "res://Data/Config/System/Preset/Resource/DefaultPreset.tres";

        return new DataOSProbe(
            enemyApplied && expandedDataApplied,
            abilityApplied,
            resourcesRegistered,
            spawnSystemProbe);
    }

    private static IEntity? GetFirstDataOSHandlerProjectile(Dictionary<string, List<string>> projectileIdsByAbilityId, IEntity ability)
    {
        if (!projectileIdsByAbilityId.TryGetValue(ability.EntityId, out var projectileIds) || projectileIds.Count == 0)
        {
            return null;
        }

        return EntityManager.Get(projectileIds[0]);
    }

    private static IEntity CreateAbilityRuntimeTarget(string entityId, Vector2Value position)
    {
        var target = EntityManager.Spawn(new EntitySpawnConfig
        {
            EntityId = entityId
        });
        target.Data.Set(CollisionDataKeys.Team, 2);
        target.Data.Set(DamageDataKeys.MaxHp, 100f);
        target.Data.Set(DamageDataKeys.CurrentHp, 100f);
        target.Data.Set(MovementDataKeys.Position, position);
        return target;
    }

    private bool RunDataOSSpawnSystemProbe(BrotatoLikeDataOSBootstrap bootstrap, BrotatoLikeSpawnCatalog spawnCatalog)
    {
        var runtime = new BrotatoLikeGameRuntime
        {
            Name = "BrotatoLikeGameRuntimeProbe",
            AutoInitialize = false,
            AutoStartGameplay = false,
            AutoTick = false
        };
        AddChild(runtime);
        runtime.Initialize(bootstrap, spawnCatalog.Wave, this);

        var blockedBeforeGameplay = runtime.TickSpawn(0d);
        runtime.BeginGameplay();
        var tickResult = runtime.TickSpawn(0d);
        runtime.OpenPauseMenu();
        var blockedWhilePaused = runtime.TickSpawn(0d);

        var chailangren = EntityManager.Get("spawn-chailangren-1") as GodotEntity2D;
        var yuren = EntityManager.Get("spawn-yuren-3") as GodotEntity2D;
        var chailangrenNode = GameOSGodotBridge.GetEntityNode("spawn-chailangren-1") as GodotEntity2D;
        var yurenNode = GameOSGodotBridge.GetEntityNode("spawn-yuren-3") as GodotEntity2D;
        var chailangrenPosition = chailangren?.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero) ?? Vector2Value.Zero;
        var yurenPosition = yuren?.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero) ?? Vector2Value.Zero;
        var scheduleConfig = runtime.SpawnScheduleConfig;
        var scheduleInfo = runtime.GetSpawnSystemRuntimeInfo();
        runtime.Shutdown();

        return !blockedBeforeGameplay.Success
            && !blockedWhilePaused.Success
            && tickResult.Success
            && tickResult.Value.SpawnedThisTick == 5
            && tickResult.Value.TotalSpawned == 5
            && runtime.IsInitialized == false
            && scheduleConfig?.Group == SystemGroup.Gameplay
            && scheduleConfig.Tags == (SystemTag.Gameplay | SystemTag.Runtime)
            && scheduleConfig.Priority == 13
            && scheduleConfig.RunCondition.AllowedFlowStates == GameFlowState.Gameplay
            && scheduleConfig.RunCondition.BlockedOverlays == OverlayFlags.Blocking
            && scheduleConfig.RunCondition.AllowedSimulationStates == SimulationState.Running
            && scheduleInfo?.IsRunning == false
            && chailangren != null
            && yuren != null
            && ReferenceEquals(chailangren, chailangrenNode)
            && ReferenceEquals(yuren, yurenNode)
            && chailangren.GetNodeOrNull<Node>("VisualRoot") != null
            && yuren.GetNodeOrNull<Node>("VisualRoot") != null
            && Math.Abs(chailangren.Data.Get<float>(DamageDataKeys.MaxHp) - 100f) < 0.001f
            && Math.Abs(yuren.Data.Get<float>(DamageDataKeys.MaxHp) - 150f) < 0.001f
            && Math.Abs(chailangrenPosition.X - 240f) < 0.001f
            && Math.Abs(chailangrenPosition.Y) < 0.001f
            && Math.Abs(yurenPosition.X + 320f) < 0.001f
            && Math.Abs(yurenPosition.Y + 180f) < 0.001f;
    }

    private GodotBridgeProbe RunGodotBridgeProbe()
    {
        var entity = new GodotEntity
        {
            Name = "GameOSBridgeProbe",
            EntityIdOverride = "brotato-like-bridge-probe"
        };

        var component = new SmokeGodotComponent
        {
            Name = "SmokeComponent"
        };

        entity.AddChild(component);
        AddChild(entity);

        var timerDriver = new GameOSTimerDriver
        {
            Name = "GameOSTimerDriver"
        };
        AddChild(timerDriver);

        var componentId = GodotNodeRegistry.GetNodeInstanceId(component);
        var collisionProbe = RunGodotCollisionBridgeProbe();
        var nodePoolProbe = RunGodotNodePoolProbe();
        var movementProbe = RunGodotMovementProbe();
        var orientationProbe = RunGodotOrientationProbe();
        var contactDamageProbe = RunGodotContactDamageProbe();
        var attackProbe = RunGodotAttackProbe();
        var aiProbe = RunGodotAIProbe();
        var abilityRuntimeProbe = RunAbilityProjectileEffectProbe();
        var playerInputProbe = RunPlayerInputProbe();
        var activeSkillInputProbe = RunActiveSkillInputProbe();
        return new GodotBridgeProbe(
            EntityRegistered: GameOSGodotBridge.GetEntityNode(entity.EntityId) == entity,
            ComponentBound: RelationshipManager.HasRelationship(
                entity.EntityId,
                componentId,
            RelationshipType.EntityToComponent),
            ComponentCallback: component.Registered,
            CollisionBridgeEntered: collisionProbe.CollisionEntered && collisionProbe.CollisionExited,
            HurtboxBridgeEntered: collisionProbe.HurtboxEntered && collisionProbe.HurtboxExited,
            NodePoolReused: nodePoolProbe.Reused && nodePoolProbe.Returned && nodePoolProbe.DetachedWhileIdle,
            MovementNodeSynced: movementProbe.ChargeSynced,
            OrbitNodeSynced: movementProbe.OrbitSynced,
            SineNodeSynced: movementProbe.SineSynced,
            BezierNodeSynced: movementProbe.BezierSynced,
            BoomerangNodeSynced: movementProbe.BoomerangSynced,
            AttachNodeSynced: movementProbe.AttachSynced,
            PlayerInputNodeSynced: movementProbe.PlayerInputSynced,
            AIControlledNodeSynced: movementProbe.AIControlledSynced,
            ParabolaNodeSynced: movementProbe.ParabolaSynced,
            CircularArcNodeSynced: movementProbe.CircularArcSynced,
            MovementCollisionNodeSynced: movementProbe.CollisionSynced,
            OrientationNodeSynced: orientationProbe.RootRotationSynced,
            OrientationSpinSynced: orientationProbe.SpinSynced,
            ContactDamageSynced: contactDamageProbe.DamageApplied && contactDamageProbe.SameTeamIgnored,
            AttackBridgeSynced: attackProbe.ExportedDataApplied && attackProbe.DamageApplied && attackProbe.Finished,
            AttackAnimationSynced: attackProbe.AnimationPlayed
                && attackProbe.CancelReturnedIdle
                && attackProbe.AnimationFinishedEvent
                && attackProbe.AnimationFinishedReturnedIdle
                && attackProbe.AvailableAnimationsCached
                && attackProbe.LegacyWrapperSynced,
            AIBridgeSynced: aiProbe.ExportedDataApplied
                && aiProbe.MoveIntentWritten
                && aiProbe.MovementSynced,
            AbilityPointSynced: abilityRuntimeProbe.AbilityPointSynced,
            AbilityAutoTargetSynced: abilityRuntimeProbe.AbilityAutoTargetSynced,
            AbilityDataOSHandlerSynced: abilityRuntimeProbe.AbilityDataOSHandlerSynced,
            ProjectileRuntimeSynced: abilityRuntimeProbe.ProjectileRuntimeSynced,
            EffectRuntimeSynced: abilityRuntimeProbe.EffectRuntimeSynced,
            PlayerInputBridgeSynced: playerInputProbe.ComponentRegistered && playerInputProbe.InputDirectionWritten && playerInputProbe.SmoothAcceleration && playerInputProbe.DirectVelocityFallback,
            ActiveSkillInputSynced: activeSkillInputProbe.SkillSwitched && activeSkillInputProbe.SkillTriggered);
    }

    private GodotCollisionProbe RunGodotCollisionBridgeProbe()
    {
        var source = new GodotAreaEntity2D
        {
            Name = "GameOSCollisionSourceProbe",
            EntityIdOverride = "brotato-like-collision-source-probe"
        };
        source.CollisionLayer = CollisionLayers.Projectile;
        source.CollisionMask = CollisionLayers.EnemyHurtbox;
        var collisionComponent = new GodotCollisionComponent
        {
            Name = "GodotCollisionComponent"
        };
        source.AddChild(collisionComponent);
        AddChild(source);

        var target = new GodotAreaEntity2D
        {
            Name = "GameOSCollisionTargetProbe",
            EntityIdOverride = "brotato-like-collision-target-probe"
        };
        target.CollisionLayer = CollisionLayers.EnemyHurtbox;
        target.CollisionMask = CollisionLayers.Projectile;
        AddChild(target);

        var collisionEntered = false;
        var collisionExited = false;
        source.Events.On<GameEventType.Collision.EnteredEventData>(
            GameEventType.Collision.Entered,
            data => collisionEntered = data.Contact.Target.EntityId == target.EntityId);
        source.Events.On<GameEventType.Collision.ExitedEventData>(
            GameEventType.Collision.Exited,
            data => collisionExited = data.Contact.Target.EntityId == target.EntityId);
        collisionComponent.EmitEntered(target);
        collisionComponent.EmitExited(target);

        var hurtbox = new GodotHurtboxComponent
        {
            Name = "GameOSHurtboxProbe",
            CollisionLayer = CollisionLayers.PlayerHurtbox,
            CollisionMask = CollisionLayers.Enemy
        };
        source.AddChild(hurtbox);
        GameOSGodotBridge.RegisterComponents(source, source);

        var hurtboxTarget = new GodotAreaEntity2D
        {
            Name = "GameOSHurtboxTargetProbe",
            EntityIdOverride = "brotato-like-hurtbox-target-probe"
        };
        hurtboxTarget.CollisionLayer = CollisionLayers.Enemy;
        AddChild(hurtboxTarget);

        var hurtboxEntered = false;
        var hurtboxExited = false;
        source.Events.On<GameEventType.Collision.HurtboxEnteredEventData>(
            GameEventType.Collision.HurtboxEntered,
            data => hurtboxEntered = data.Contact.Target.EntityId == hurtboxTarget.EntityId);
        source.Events.On<GameEventType.Collision.HurtboxExitedEventData>(
            GameEventType.Collision.HurtboxExited,
            data => hurtboxExited = data.Contact.Target.EntityId == hurtboxTarget.EntityId);
        hurtbox.EmitEntered(hurtboxTarget);
        hurtbox.EmitExited(hurtboxTarget);

        return new GodotCollisionProbe(collisionEntered, collisionExited, hurtboxEntered, hurtboxExited);
    }

    private GodotNodePoolProbe RunGodotNodePoolProbe()
    {
        var pool = new GodotNodePool<Area2D>(
            CreatePoolProbeArea,
            new GodotNodePoolConfig
            {
                Name = "brotato-like-bridge-area-pool",
                InitialSize = 1,
                MaxSize = 2,
                ActiveParent = this
            });

        var first = pool.Get(activateNode: false);
        first.GlobalPosition = new Vector2(16f, 16f);
        pool.Activate(first);

        var returned = GodotNodePoolManager.ReturnToPool(first);
        var detachedWhileIdle = first.GetParent() == null;
        var second = pool.Get();
        var reused = ReferenceEquals(first, second);

        pool.Destroy();

        return new GodotNodePoolProbe(returned, detachedWhileIdle, reused);
    }

    private GodotOrientationProbe RunGodotOrientationProbe()
    {
        var followEntity = new GodotEntity2D
        {
            Name = "GameOSOrientationFollowProbe",
            EntityIdOverride = "brotato-like-orientation-follow-probe"
        };
        var followComponent = new GodotOrientationComponent
        {
            Name = "GameOSOrientationFollowComponent"
        };
        followEntity.AddChild(followComponent);
        AddChild(followEntity);
        followEntity.Data.Set(MovementDataKeys.FacingDirection, new Vector2Value(0f, 1f));
        followComponent._Process(0.016);
        var rootRotationSynced = Math.Abs(followEntity.RotationDegrees - 90f) < 0.001f;

        var spinEntity = new GodotEntity2D
        {
            Name = "GameOSOrientationSpinProbe",
            EntityIdOverride = "brotato-like-orientation-spin-probe"
        };
        var spinComponent = new GodotOrientationComponent
        {
            Name = "GameOSOrientationSpinComponent"
        };
        spinEntity.AddChild(spinComponent);
        AddChild(spinEntity);
        var movement = new MovementSystem();
        movement.Start(spinEntity, new MovementParams
        {
            Mode = MoveMode.Charge,
            Direction = new Vector2Value(1f, 0f),
            Speed = 1f,
            Orientation = new OrientationParams
            {
                Mode = OrientationMode.SpinOnly,
                AngularSpeed = 90f,
                TotalAngle = -1f
            }
        });
        spinComponent._Process(1.0);
        var spinSynced = Math.Abs(spinEntity.RotationDegrees - 90f) < 0.001f;

        return new GodotOrientationProbe(rootRotationSynced, spinSynced);
    }

    private GodotContactDamageProbe RunGodotContactDamageProbe()
    {
        var victim = new GodotAreaEntity2D
        {
            Name = "GameOSContactDamageVictimProbe",
            EntityIdOverride = "brotato-like-contact-damage-victim-probe",
            CollisionLayer = CollisionLayers.PlayerHurtbox,
            CollisionMask = CollisionLayers.Enemy
        };
        victim.Data.Set(CollisionDataKeys.Team, 1);
        victim.Data.Set(DamageDataKeys.CurrentHp, 20f);
        victim.Data.Set(DamageDataKeys.MaxHp, 20f);
        var hurtbox = new GodotHurtboxComponent
        {
            Name = "GameOSContactDamageHurtboxProbe",
            CollisionLayer = CollisionLayers.PlayerHurtbox,
            CollisionMask = CollisionLayers.Enemy
        };
        var contactDamage = new GodotContactDamageComponent
        {
            Name = "GameOSContactDamageComponentProbe",
            RepeatWhileContact = false
        };
        victim.AddChild(hurtbox);
        victim.AddChild(contactDamage);
        AddChild(victim);

        var attacker = new GodotAreaEntity2D
        {
            Name = "GameOSContactDamageAttackerProbe",
            EntityIdOverride = "brotato-like-contact-damage-attacker-probe",
            CollisionLayer = CollisionLayers.Enemy,
            CollisionMask = CollisionLayers.PlayerHurtbox
        };
        attacker.Data.Set(CollisionDataKeys.Team, 2);
        attacker.Data.Set(DamageDataKeys.ContactDamage, 6f);
        AddChild(attacker);

        var damaged = false;
        victim.Events.On<GameEventType.Damage.DamagedEventData>(
            GameEventType.Damage.Damaged,
            data => damaged = data.Info.Attacker?.EntityId == attacker.EntityId);
        hurtbox.EmitEntered(attacker);
        var damageApplied = damaged
            && Math.Abs(victim.Data.Get<float>(DamageDataKeys.CurrentHp) - 14f) < 0.001f;

        var sameTeamAttacker = new GodotAreaEntity2D
        {
            Name = "GameOSContactDamageSameTeamProbe",
            EntityIdOverride = "brotato-like-contact-damage-same-team-probe",
            CollisionLayer = CollisionLayers.Enemy,
            CollisionMask = CollisionLayers.PlayerHurtbox
        };
        sameTeamAttacker.Data.Set(CollisionDataKeys.Team, 1);
        sameTeamAttacker.Data.Set(DamageDataKeys.ContactDamage, 6f);
        AddChild(sameTeamAttacker);

        hurtbox.EmitEntered(sameTeamAttacker);
        var sameTeamIgnored = Math.Abs(victim.Data.Get<float>(DamageDataKeys.CurrentHp) - 14f) < 0.001f;

        return new GodotContactDamageProbe(damageApplied, sameTeamIgnored);
    }

    private GodotAttackProbe RunGodotAttackProbe()
    {
        var attacker = new GodotEntity2D
        {
            Name = "GameOSAttackAttackerProbe",
            EntityIdOverride = "brotato-like-attack-attacker-probe",
            Position = Vector2.Zero
        };
        var attack = new GodotAttackComponent
        {
            Name = "GameOSAttackComponentProbe",
            Damage = 7f,
            Range = 20f,
            Interval = 0f,
            WindUpTime = 0.1f,
            AttackAnimation = "attack1"
        };
        var sprite = new AnimatedSprite2D
        {
            Name = "VisualRoot",
            SpriteFrames = CreateAttackProbeSpriteFrames()
        };
        var animation = new GodotUnitAnimationComponent
        {
            Name = "GameOSUnitAnimationComponentProbe"
        };
        attacker.AddChild(sprite);
        attacker.AddChild(animation);
        attacker.AddChild(attack);
        AddChild(attacker);

        var target = new GodotEntity2D
        {
            Name = "GameOSAttackTargetProbe",
            EntityIdOverride = "brotato-like-attack-target-probe",
            Position = new Vector2(6f, 0f)
        };
        target.Data.Set(DamageDataKeys.CurrentHp, 20f);
        target.Data.Set(DamageDataKeys.MaxHp, 20f);
        AddChild(target);

        var finished = false;
        var animationFinishedEvent = false;
        attacker.Events.On<GameEventType.Attack.FinishedEventData>(
            GameEventType.Attack.Finished,
            data => finished = data.Target?.EntityId == target.EntityId && data.DidHit);
        attacker.Events.On<GameEventType.Unit.AnimationFinishedEventData>(
            GameEventType.Unit.AnimationFinished,
            data => animationFinishedEvent = data.Entity.EntityId == attacker.EntityId && data.AnimationName == "attack2");

        var exportedDataApplied = Math.Abs(attacker.Data.Get<float>(AttackDataKeys.Damage) - 7f) < 0.001f
            && Math.Abs(attacker.Data.Get<float>(AttackDataKeys.Range) - 20f) < 0.001f;
        var availableAnimations = attacker.Data.Get<System.Collections.Generic.List<string>>(UnitDataKeys.AvailableAnimations);
        var availableAnimationsCached = availableAnimations.Contains("idle") && availableAnimations.Contains("attack2");
        var report = attack.RequestAttackNode(target);
        var animationPlayed = sprite.Animation == "attack2" && sprite.IsPlaying();
        attack.CancelAttack();
        var cancelReturnedIdle = sprite.Animation == "idle" && sprite.IsPlaying();
        attack.RequestAttackNode(target);
        var damageApplied = report.Result == AttackTriggerResult.Success;
        TimerManager.Instance.Tick(0.1f);
        damageApplied = damageApplied
            && Math.Abs(target.Data.Get<float>(DamageDataKeys.CurrentHp) - 13f) < 0.001f;
        sprite.EmitSignal(AnimatedSprite2D.SignalName.AnimationFinished);
        var animationFinishedReturnedIdle = sprite.Animation == "idle" && sprite.IsPlaying();
        var legacyWrapperSynced = RunLegacyAttackWrapperProbe();

        return new GodotAttackProbe(
            exportedDataApplied,
            damageApplied,
            finished,
            animationPlayed,
            cancelReturnedIdle,
            animationFinishedEvent,
            animationFinishedReturnedIdle,
            availableAnimationsCached,
            legacyWrapperSynced);
    }

    private bool RunLegacyAttackWrapperProbe()
    {
        var attacker = new GodotEntity2D
        {
            Name = "LegacyAttackComponentAttackerProbe",
            EntityIdOverride = "brotato-like-legacy-attack-attacker-probe",
            Position = Vector2.Zero
        };
        attacker.Data.Set(AttackDataKeys.Damage, 11f);
        attacker.Data.Set(AttackDataKeys.Range, 30f);
        attacker.Data.Set(AttackDataKeys.Interval, 0f);

        var attack = new AttackComponent
        {
            Name = "AttackComponent",
            Damage = 1f,
            Range = 1f,
            Interval = 1f,
            RequestAnimationEvents = false
        };
        attacker.AddChild(attack);
        AddChild(attacker);

        var target = new GodotEntity2D
        {
            Name = "LegacyAttackComponentTargetProbe",
            EntityIdOverride = "brotato-like-legacy-attack-target-probe",
            Position = new Vector2(10f, 0f)
        };
        target.Data.Set(DamageDataKeys.CurrentHp, 20f);
        target.Data.Set(DamageDataKeys.MaxHp, 20f);
        AddChild(target);

        var preservedData = Math.Abs(attacker.Data.Get<float>(AttackDataKeys.Damage) - 11f) < 0.001f
            && Math.Abs(attacker.Data.Get<float>(AttackDataKeys.Range) - 30f) < 0.001f
            && Math.Abs(attacker.Data.Get<float>(AttackDataKeys.Interval)) < 0.001f;
        var report = attack.RequestAttackNode(target);
        var damageApplied = report.Result == AttackTriggerResult.Success
            && Math.Abs(target.Data.Get<float>(DamageDataKeys.CurrentHp) - 9f) < 0.001f;

        return preservedData && damageApplied;
    }

    private GodotAIProbe RunGodotAIProbe()
    {
        var movementDriver = new GodotMovementDriver
        {
            Name = "GameOSAIProbeMovementDriver",
            AutoTick = false
        };
        AddChild(movementDriver);

        var agent = new GodotEntity2D
        {
            Name = "GameOSAIProbeAgent",
            EntityIdOverride = "brotato-like-ai-probe-agent",
            Position = Vector2.Zero
        };
        var ai = new GodotAIComponent
        {
            Name = "GameOSAIComponentProbe",
            AutoTick = false,
            BehaviorTreeKind = GodotAIBehaviorTreeKind.PatrolOnly,
            TargetSearchRange = 20f,
            AttackRange = 1f,
            ChaseSpeedMultiplier = 0.5f,
            PatrolRadius = 4f,
            PatrolWaitTime = 0.25f,
            PatrolSpeedMultiplier = 0.5f
        };
        agent.AddChild(ai);
        AddChild(agent);
        agent.Data.Set(MovementDataKeys.MoveSpeed, 20f);

        movementDriver.MovementSystem.Start(agent, new MovementParams
        {
            Mode = MoveMode.AIControlled,
            MaxDuration = 1f
        });
        var state = ai.TickAI(0.1f);
        var exportedDataApplied = agent.Data.Get<bool>(AIDataKeys.IsEnabled)
            && Math.Abs(agent.Data.Get<float>(AIDataKeys.AttackRange) - 1f) < 0.001f
            && Math.Abs(agent.Data.Get<float>(AIDataKeys.PatrolRadius) - 4f) < 0.001f;
        var moveIntentWritten = state == AIState.Running
            && agent.Data.Get<Vector2Value>(AIDataKeys.PatrolTargetPosition) == new Vector2Value(4f, 0f)
            && agent.Data.Get<Vector2Value>(MovementDataKeys.AIMoveDirection) == new Vector2Value(1f, 0f)
            && Math.Abs(agent.Data.Get<float>(MovementDataKeys.AIMoveSpeedMultiplier) - 0.5f) < 0.001f;
        movementDriver.TickMovement(0.5f);
        var movementSynced = Math.Abs(agent.Position.X - 5f) < 0.001f
            && Math.Abs(agent.Position.Y) < 0.001f;

        return new GodotAIProbe(exportedDataApplied, moveIntentWritten, movementSynced);
    }

    private AbilityRuntimeProbe RunAbilityProjectileEffectProbe()
    {
        var caster = new GodotEntity2D
        {
            Name = "GameOSAbilityRuntimeCasterProbe",
            EntityIdOverride = "brotato-like-ability-runtime-caster-probe",
            Position = Vector2.Zero
        };
        AddChild(caster);
        caster.Data.Set(CollisionDataKeys.Team, 1);

        var ability = new GodotEntity
        {
            Name = "GameOSAbilityRuntimeAbilityProbe",
            EntityIdOverride = "brotato-like-ability-runtime-ability-probe"
        };
        ability.Data.Set(AbilityDataKeys.IsEnabled, true);
        ability.Data.Set(AbilityDataKeys.TargetSelection, AbilityTargetSelection.Point);
        AddChild(ability);

        var pointReport = AbilityService.Instance.TryTrigger(new AbilityCastContext
        {
            Caster = caster,
            Ability = ability,
            TargetPosition = new Vector2Value(8f, 0f)
        });
        var abilityPointSynced = pointReport.Result == AbilityTriggerResult.Success;

        BrotatoLikeAbilityHandlers.RegisterAll();
        var dataOsBootstrap = BrotatoLikeDataOSBootstrap.LoadFromResource();
        var sineWaveDataOsAbility = dataOsBootstrap.SpawnEntityFromRecord(
            "ability",
            "sine_wave_shot",
            "brotato-like-dataos-handler-sine-wave-ability-probe");
        var boomerangDataOsAbility = dataOsBootstrap.SpawnEntityFromRecord(
            "ability",
            "boomerang_throw",
            "brotato-like-dataos-handler-boomerang-ability-probe");
        var bezierDataOsAbility = dataOsBootstrap.SpawnEntityFromRecord(
            "ability",
            "bezier_shot",
            "brotato-like-dataos-handler-bezier-ability-probe");
        var circularArcDataOsAbility = dataOsBootstrap.SpawnEntityFromRecord(
            "ability",
            "parabola_shot",
            "brotato-like-dataos-handler-circular-arc-ability-probe");
        var arcShotDataOsAbility = dataOsBootstrap.SpawnEntityFromRecord(
            "ability",
            "arc_shot",
            "brotato-like-dataos-handler-arc-shot-ability-probe");
        var orbitDataOsAbility = dataOsBootstrap.SpawnEntityFromRecord(
            "ability",
            "orbit_skill",
            "brotato-like-dataos-handler-orbit-ability-probe");
        var chainLightningDataOsAbility = dataOsBootstrap.SpawnEntityFromRecord(
            "ability",
            "chain_lightning",
            "brotato-like-dataos-handler-chain-lightning-ability-probe");
        var targetPointDataOsAbility = dataOsBootstrap.SpawnEntityFromRecord(
            "ability",
            "target_point_skill",
            "brotato-like-dataos-handler-target-point-ability-probe");
        var slamDataOsAbility = dataOsBootstrap.SpawnEntityFromRecord(
            "ability",
            "slam",
            "brotato-like-dataos-handler-slam-ability-probe");
        var dashDataOsAbility = dataOsBootstrap.SpawnEntityFromRecord(
            "ability",
            "dash",
            "brotato-like-dataos-handler-dash-ability-probe");
        var circleDamageDataOsAbility = dataOsBootstrap.SpawnEntityFromRecord(
            "ability",
            "circle_damage",
            "brotato-like-dataos-handler-circle-damage-ability-probe");
        var auraShieldDataOsAbility = dataOsBootstrap.SpawnEntityFromRecord(
            "ability",
            "aura_shield",
            "brotato-like-dataos-handler-aura-shield-ability-probe");
        var dataOsHandlerProjectileIds = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        GlobalEventBus.Global.On<GameEventType.Projectile.SpawnedEventData>(
            GameEventType.Projectile.Spawned,
            data =>
            {
                if (data.Ability == null)
                {
                    return;
                }

                if (!dataOsHandlerProjectileIds.TryGetValue(data.Ability.EntityId, out var projectileIds))
                {
                    projectileIds = [];
                    dataOsHandlerProjectileIds[data.Ability.EntityId] = projectileIds;
                }

                projectileIds.Add(data.Projectile.EntityId);
            });
        var slamEffectSpawned = false;
        var slamEffectPosition = Vector2Value.Zero;
        var targetPointEffectSpawned = false;
        var targetPointEffectPosition = Vector2Value.Zero;
        var dashEffectSpawned = false;
        var dashEffectPosition = Vector2Value.Zero;
        var circleDamageEffectSpawned = false;
        var circleDamageEffectPosition = Vector2Value.Zero;
        GlobalEventBus.Global.On<GameEventType.Effect.SpawnedEventData>(
            GameEventType.Effect.Spawned,
            data =>
            {
                if (data.Ability?.EntityId == slamDataOsAbility.EntityId)
                {
                    slamEffectSpawned = true;
                    slamEffectPosition = data.Effect.Data.Get<Vector2Value>(EffectDataKeys.Position, Vector2Value.Zero);
                    return;
                }

                if (data.Ability?.EntityId == dashDataOsAbility.EntityId)
                {
                    dashEffectSpawned = true;
                    dashEffectPosition = data.Effect.Data.Get<Vector2Value>(EffectDataKeys.Position, Vector2Value.Zero);
                    return;
                }

                if (data.Ability?.EntityId == targetPointDataOsAbility.EntityId)
                {
                    targetPointEffectSpawned = true;
                    targetPointEffectPosition = data.Effect.Data.Get<Vector2Value>(EffectDataKeys.Position, Vector2Value.Zero);
                    return;
                }

                if (data.Ability?.EntityId == circleDamageDataOsAbility.EntityId)
                {
                    circleDamageEffectSpawned = true;
                    circleDamageEffectPosition = data.Effect.Data.Get<Vector2Value>(EffectDataKeys.Position, Vector2Value.Zero);
                }
            });
        var sineWaveDataOsHandlerReport = AbilityService.Instance.TryTrigger(new AbilityCastContext
        {
            Caster = caster,
            Ability = sineWaveDataOsAbility,
            TargetPosition = new Vector2Value(10f, 0f)
        });
        var boomerangDataOsHandlerReport = AbilityService.Instance.TryTrigger(new AbilityCastContext
        {
            Caster = caster,
            Ability = boomerangDataOsAbility,
            TargetPosition = new Vector2Value(16f, 0f)
        });
        var bezierDataOsHandlerReport = AbilityService.Instance.TryTrigger(new AbilityCastContext
        {
            Caster = caster,
            Ability = bezierDataOsAbility,
            TargetPosition = new Vector2Value(18f, 0f)
        });
        var circularArcDataOsHandlerReport = AbilityService.Instance.TryTrigger(new AbilityCastContext
        {
            Caster = caster,
            Ability = circularArcDataOsAbility,
            TargetPosition = new Vector2Value(20f, 0f)
        });
        var arcShotTarget = CreateAbilityRuntimeTarget("brotato-like-arc-shot-target", new Vector2Value(2400f, 0f));
        var arcShotDataOsHandlerReport = AbilityService.Instance.TryTrigger(new AbilityCastContext
        {
            Caster = caster,
            Ability = arcShotDataOsAbility,
            Targets = [arcShotTarget],
            TargetPosition = new Vector2Value(24f, 0f)
        });
        var orbitDataOsHandlerReport = AbilityService.Instance.TryTrigger(new AbilityCastContext
        {
            Caster = caster,
            Ability = orbitDataOsAbility,
            TargetPosition = new Vector2Value(22f, 0f)
        });
        var chainTargetA = CreateAbilityRuntimeTarget("brotato-like-chain-target-a", new Vector2Value(30f, 0f));
        var chainTargetB = CreateAbilityRuntimeTarget("brotato-like-chain-target-b", new Vector2Value(50f, 0f));
        var chainTargetC = CreateAbilityRuntimeTarget("brotato-like-chain-target-c", new Vector2Value(80f, 0f));
        var chainLightningReport = AbilityService.Instance.TryTrigger(new AbilityCastContext
        {
            Caster = caster,
            Ability = chainLightningDataOsAbility,
            Targets = [chainTargetA]
        });
        TimerManager.Instance.Tick(0.2f);
        TimerManager.Instance.Tick(0.2f);

        var targetPointTargetA = CreateAbilityRuntimeTarget("brotato-like-target-point-target-a", new Vector2Value(3210f, 0f));
        var targetPointTargetB = CreateAbilityRuntimeTarget("brotato-like-target-point-target-b", new Vector2Value(3350f, 0f));
        var targetPointTargetOutside = CreateAbilityRuntimeTarget("brotato-like-target-point-target-outside", new Vector2Value(3450f, 0f));
        var targetPointReport = AbilityService.Instance.TryTrigger(new AbilityCastContext
        {
            Caster = caster,
            Ability = targetPointDataOsAbility,
            TargetPosition = new Vector2Value(3200f, 0f)
        });

        var slamTargetA = CreateAbilityRuntimeTarget("brotato-like-slam-target-a", new Vector2Value(1210f, 0f));
        var slamTargetB = CreateAbilityRuntimeTarget("brotato-like-slam-target-b", new Vector2Value(1400f, 0f));
        var slamTargetOutside = CreateAbilityRuntimeTarget("brotato-like-slam-target-outside", new Vector2Value(1550f, 0f));
        var slamSameTeam = EntityManager.Spawn(new EntitySpawnConfig
        {
            EntityId = "brotato-like-slam-same-team"
        });
        slamSameTeam.Data.Set(CollisionDataKeys.Team, 1);
        slamSameTeam.Data.Set(DamageDataKeys.MaxHp, 100f);
        slamSameTeam.Data.Set(DamageDataKeys.CurrentHp, 100f);
        slamSameTeam.Data.Set(MovementDataKeys.Position, new Vector2Value(1220f, 0f));
        var slamReport = AbilityService.Instance.TryTrigger(new AbilityCastContext
        {
            Caster = caster,
            Ability = slamDataOsAbility,
            TargetPosition = new Vector2Value(1200f, 0f)
        });
        var dashCaster = EntityManager.Spawn(new EntitySpawnConfig
        {
            EntityId = "brotato-like-dash-caster"
        });
        dashCaster.Data.Set(CollisionDataKeys.Team, 1);
        dashCaster.Data.Set(MovementDataKeys.Position, Vector2Value.Zero);
        var dashReport = AbilityService.Instance.TryTrigger(new AbilityCastContext
        {
            Caster = dashCaster,
            Ability = dashDataOsAbility,
            TargetPosition = new Vector2Value(300f, 0f)
        });
        if (FeatureHandlerRegistry.Get("技能.位移.冲刺") is BrotatoLikeDashAbilityHandler dashHandler)
        {
            dashHandler.Tick(0.25f);
        }

        var circleDamageTargetA = CreateAbilityRuntimeTarget("brotato-like-circle-damage-target-a", new Vector2Value(-2200f, 0f));
        var circleDamageTargetB = CreateAbilityRuntimeTarget("brotato-like-circle-damage-target-b", new Vector2Value(-1800f, 0f));
        var circleDamageTargetOutside = CreateAbilityRuntimeTarget("brotato-like-circle-damage-target-outside", new Vector2Value(-1400f, 0f));
        var circleDamageSameTeam = EntityManager.Spawn(new EntitySpawnConfig
        {
            EntityId = "brotato-like-circle-damage-same-team"
        });
        circleDamageSameTeam.Data.Set(CollisionDataKeys.Team, 1);
        circleDamageSameTeam.Data.Set(DamageDataKeys.MaxHp, 100f);
        circleDamageSameTeam.Data.Set(DamageDataKeys.CurrentHp, 100f);
        circleDamageSameTeam.Data.Set(MovementDataKeys.Position, new Vector2Value(-2100f, 0f));
        var circleDamageReport = AbilityService.Instance.TryTrigger(new AbilityCastContext
        {
            Caster = caster,
            Ability = circleDamageDataOsAbility,
            TargetPosition = new Vector2Value(-2000f, 0f)
        });
        var auraShieldReport = AbilityService.Instance.TryTrigger(new AbilityCastContext
        {
            Caster = caster,
            Ability = auraShieldDataOsAbility,
            TargetPosition = new Vector2Value(64f, 0f)
        });

        var sineWaveProjectile = GetFirstDataOSHandlerProjectile(dataOsHandlerProjectileIds, sineWaveDataOsAbility);
        var boomerangProjectile = GetFirstDataOSHandlerProjectile(dataOsHandlerProjectileIds, boomerangDataOsAbility);
        var bezierProjectile = GetFirstDataOSHandlerProjectile(dataOsHandlerProjectileIds, bezierDataOsAbility);
        var circularArcProjectile = GetFirstDataOSHandlerProjectile(dataOsHandlerProjectileIds, circularArcDataOsAbility);
        var arcShotProjectile = GetFirstDataOSHandlerProjectile(dataOsHandlerProjectileIds, arcShotDataOsAbility);
        var orbitProjectile = GetFirstDataOSHandlerProjectile(dataOsHandlerProjectileIds, orbitDataOsAbility);
        var auraShieldProjectile = GetFirstDataOSHandlerProjectile(dataOsHandlerProjectileIds, auraShieldDataOsAbility);
        var sineWavePosition = sineWaveProjectile?.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero) ?? Vector2Value.Zero;
        var dataOsHandlerSynced = sineWaveDataOsHandlerReport.Result == AbilityTriggerResult.Success
            && sineWaveDataOsHandlerReport.Executed?.TargetsHit == 1
            && sineWaveProjectile != null
            && sineWaveProjectile.Data.Get<bool>(MovementDataKeys.IsMoving)
            && sineWaveProjectile.Data.Get<MoveMode>(MovementDataKeys.HandlerMoveMode) == MoveMode.SineWave
            && Math.Abs(sineWaveProjectile.Data.Get<float>(ProjectileDataKeys.Speed) - 350f) < 0.001f
            && Math.Abs(sineWaveProjectile.Data.Get<float>(MovementDataKeys.WaveAmplitude) - 60f) < 0.001f
            && Math.Abs(sineWaveProjectile.Data.Get<float>(MovementDataKeys.WaveFrequency) - 2f) < 0.001f
            && Math.Abs(sineWaveProjectile.Data.Get<float>(MovementDataKeys.HandlerMaxDistance) - 1800f) < 0.001f
            && sineWaveProjectile.Data.Get<string>(ProjectileDataKeys.ScenePath) == "res://assets/Projectile/Projectile/Polygon2D/ArrowNeedle.tscn"
            && Math.Abs(sineWaveProjectile.Data.Get<float>(ProjectileDataKeys.Damage) - 25f) < 0.001f
            && sineWavePosition == Vector2Value.Zero
            && boomerangDataOsHandlerReport.Result == AbilityTriggerResult.Success
            && boomerangDataOsHandlerReport.Executed?.TargetsHit == 1
            && boomerangProjectile != null
            && boomerangProjectile.Data.Get<bool>(MovementDataKeys.IsMoving)
            && boomerangProjectile.Data.Get<MoveMode>(MovementDataKeys.HandlerMoveMode) == MoveMode.Boomerang
            && Math.Abs(boomerangProjectile.Data.Get<float>(ProjectileDataKeys.Speed) - 460f) < 0.001f
            && boomerangProjectile.Data.Get<int>(ProjectileDataKeys.MaxHitCount) == -1
            && Math.Abs(boomerangProjectile.Data.Get<float>(MovementDataKeys.BoomerangArcHeight) - 160f) < 0.001f
            && Math.Abs(boomerangProjectile.Data.Get<float>(MovementDataKeys.BoomerangPauseTime) - 0.05f) < 0.001f
            && Math.Abs(boomerangProjectile.Data.Get<float>(MovementDataKeys.BoomerangReturnSpeedMultiplier) - 1.35f) < 0.001f
            && boomerangProjectile.Data.Get<bool>(MovementDataKeys.BoomerangIsClockwise)
            && bezierDataOsHandlerReport.Result == AbilityTriggerResult.Success
            && bezierDataOsHandlerReport.Executed?.TargetsHit == 5
            && bezierProjectile != null
            && bezierProjectile.Data.Get<bool>(MovementDataKeys.IsMoving)
            && bezierProjectile.Data.Get<MoveMode>(MovementDataKeys.HandlerMoveMode) == MoveMode.BezierCurve
            && bezierProjectile.Data.Get<int>(MovementDataKeys.HandlerProjectileCount) == 5
            && bezierProjectile.Data.Get<int>(MovementDataKeys.BezierDegree) == 5
            && bezierProjectile.Data.Get<string>(MovementDataKeys.BezierPattern) == "Converge"
            && Math.Abs(bezierProjectile.Data.Get<float>(MovementDataKeys.HandlerMinTravelDuration) - 0.85f) < 0.001f
            && Math.Abs(bezierProjectile.Data.Get<float>(MovementDataKeys.HandlerMaxTravelDuration) - 1.45f) < 0.001f
            && circularArcDataOsHandlerReport.Result == AbilityTriggerResult.Success
            && circularArcDataOsHandlerReport.Executed?.TargetsHit == 1
            && circularArcProjectile != null
            && circularArcProjectile.Data.Get<bool>(MovementDataKeys.IsMoving)
            && circularArcProjectile.Data.Get<MoveMode>(MovementDataKeys.HandlerMoveMode) == MoveMode.CircularArc
            && Math.Abs(circularArcProjectile.Data.Get<float>(MovementDataKeys.HandlerMinTravelDuration) - 0.75f) < 0.001f
            && Math.Abs(circularArcProjectile.Data.Get<float>(MovementDataKeys.HandlerMaxTravelDuration) - 1.35f) < 0.001f
            && Math.Abs(circularArcProjectile.Data.Get<float>(MovementDataKeys.CircularArcRadiusScale) - 0.72f) < 0.001f
            && Math.Abs(circularArcProjectile.Data.Get<float>(MovementDataKeys.CircularArcRadiusMinOffset) - 32f) < 0.001f
            && !circularArcProjectile.Data.Get<bool>(MovementDataKeys.CircularArcClockwise)
            && circularArcProjectile.Data.Get<bool>(MovementDataKeys.BowWorldUp)
            && arcShotDataOsHandlerReport.Result == AbilityTriggerResult.Success
            && arcShotDataOsHandlerReport.Executed?.TargetsHit == 1
            && arcShotProjectile != null
            && arcShotProjectile.Data.Get<bool>(MovementDataKeys.IsMoving)
            && arcShotProjectile.Data.Get<MoveMode>(MovementDataKeys.HandlerMoveMode) == MoveMode.CircularArc
            && Math.Abs(arcShotProjectile.Data.Get<float>(ProjectileDataKeys.Speed) - 390f) < 0.001f
            && Math.Abs(arcShotProjectile.Data.Get<float>(ProjectileDataKeys.MaxLifeTime) - 1.5f) < 0.001f
            && Math.Abs(arcShotProjectile.Data.Get<float>(MovementDataKeys.HandlerMinTravelDuration) - 0.65f) < 0.001f
            && Math.Abs(arcShotProjectile.Data.Get<float>(MovementDataKeys.HandlerMaxTravelDuration) - 1.5f) < 0.001f
            && Math.Abs(arcShotProjectile.Data.Get<float>(MovementDataKeys.CircularArcRadiusScale) - 0.68f) < 0.001f
            && Math.Abs(arcShotProjectile.Data.Get<float>(MovementDataKeys.CircularArcRadiusMinOffset) - 24f) < 0.001f
            && arcShotProjectile.Data.Get<bool>(MovementDataKeys.CircularArcClockwise)
            && !arcShotProjectile.Data.Get<bool>(MovementDataKeys.BowWorldUp)
            && orbitDataOsHandlerReport.Result == AbilityTriggerResult.Success
            && orbitDataOsHandlerReport.Executed?.TargetsHit == 3
            && orbitProjectile != null
            && orbitProjectile.Data.Get<bool>(MovementDataKeys.IsMoving)
            && orbitProjectile.Data.Get<MoveMode>(MovementDataKeys.HandlerMoveMode) == MoveMode.Orbit
            && orbitProjectile.Data.Get<int>(MovementDataKeys.HandlerProjectileCount) == 3
            && Math.Abs(orbitProjectile.Data.Get<float>(MovementDataKeys.OrbitRadius) - 100f) < 0.001f
            && Math.Abs(orbitProjectile.Data.Get<float>(MovementDataKeys.OrbitAngularSpeed) - 180f) < 0.001f
            && Math.Abs(orbitProjectile.Data.Get<float>(MovementDataKeys.HandlerMaxTravelDuration) - 6f) < 0.001f
            && orbitProjectile.Data.Get<bool>(MovementDataKeys.IsOrbitClockwise)
            && chainLightningReport.Result == AbilityTriggerResult.Success
            && chainLightningReport.Executed?.TargetsHit == 1
            && Math.Abs(chainTargetA.Data.Get<float>(DamageDataKeys.CurrentHp) - 50f) < 0.001f
            && Math.Abs(chainTargetB.Data.Get<float>(DamageDataKeys.CurrentHp) - 50f) < 0.001f
            && Math.Abs(chainTargetC.Data.Get<float>(DamageDataKeys.CurrentHp) - 50f) < 0.001f
            && targetPointReport.Result == AbilityTriggerResult.Success
            && targetPointReport.Executed?.TargetsHit == 2
            && Math.Abs(targetPointReport.Executed.TotalDamage - 20f) < 0.001f
            && Math.Abs(targetPointTargetA.Data.Get<float>(DamageDataKeys.CurrentHp) - 90f) < 0.001f
            && Math.Abs(targetPointTargetB.Data.Get<float>(DamageDataKeys.CurrentHp) - 90f) < 0.001f
            && Math.Abs(targetPointTargetOutside.Data.Get<float>(DamageDataKeys.CurrentHp) - 100f) < 0.001f
            && targetPointEffectSpawned
            && targetPointEffectPosition == new Vector2Value(3200f, 0f)
            && slamReport.Result == AbilityTriggerResult.Success
            && slamReport.Executed?.TargetsHit == 2
            && Math.Abs(slamReport.Executed.TotalDamage - 60f) < 0.001f
            && Math.Abs(slamTargetA.Data.Get<float>(DamageDataKeys.CurrentHp) - 70f) < 0.001f
            && Math.Abs(slamTargetB.Data.Get<float>(DamageDataKeys.CurrentHp) - 70f) < 0.001f
            && Math.Abs(slamTargetOutside.Data.Get<float>(DamageDataKeys.CurrentHp) - 100f) < 0.001f
            && Math.Abs(slamSameTeam.Data.Get<float>(DamageDataKeys.CurrentHp) - 100f) < 0.001f
            && slamEffectSpawned
            && slamEffectPosition == new Vector2Value(1200f, 0f)
            && dashReport.Result == AbilityTriggerResult.Success
            && dashReport.Executed?.TargetsHit == 1
            && Math.Abs(dashCaster.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero).X - 300f) < 0.001f
            && Math.Abs(dashCaster.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero).Y) < 0.001f
            && !dashCaster.Data.Get<bool>(MovementDataKeys.IsMoving)
            && dashCaster.Data.Get<MoveMode>(MovementDataKeys.HandlerMoveMode) == MoveMode.Charge
            && Math.Abs(dashCaster.Data.Get<float>(MovementDataKeys.HandlerMaxDistance) - 300f) < 0.001f
            && dashEffectSpawned
            && dashEffectPosition == Vector2Value.Zero
            && circleDamageReport.Result == AbilityTriggerResult.Success
            && circleDamageReport.Executed?.TargetsHit == 2
            && Math.Abs(circleDamageReport.Executed.TotalDamage - 20f) < 0.001f
            && Math.Abs(circleDamageTargetA.Data.Get<float>(DamageDataKeys.CurrentHp) - 90f) < 0.001f
            && Math.Abs(circleDamageTargetB.Data.Get<float>(DamageDataKeys.CurrentHp) - 90f) < 0.001f
            && Math.Abs(circleDamageTargetOutside.Data.Get<float>(DamageDataKeys.CurrentHp) - 100f) < 0.001f
            && Math.Abs(circleDamageSameTeam.Data.Get<float>(DamageDataKeys.CurrentHp) - 100f) < 0.001f
            && circleDamageEffectSpawned
            && circleDamageEffectPosition == new Vector2Value(-2000f, 0f)
            && auraShieldReport.Result == AbilityTriggerResult.Success
            && auraShieldReport.Executed?.TargetsHit == 1
            && auraShieldProjectile != null
            && auraShieldProjectile.Data.Get<bool>(MovementDataKeys.IsMoving)
            && auraShieldProjectile.Data.Get<MoveMode>(MovementDataKeys.HandlerMoveMode) == MoveMode.AttachToHost
            && auraShieldProjectile.Data.Get<int>(MovementDataKeys.HandlerProjectileCount) == 1
            && Math.Abs(auraShieldProjectile.Data.Get<float>(MovementDataKeys.HandlerMaxDistance) - 64f) < 0.001f
            && Math.Abs(auraShieldProjectile.Data.Get<float>(MovementDataKeys.HandlerMaxTravelDuration) - 6f) < 0.001f
            && Math.Abs(auraShieldProjectile.Data.Get<float>(ProjectileDataKeys.Damage) - 15f) < 0.001f
            && auraShieldProjectile.Data.Get<string>(ProjectileDataKeys.ScenePath) == "res://assets/Projectile/Projectile/Polygon2D/BulletDiamond.tscn";

        var autoAbility = new GodotEntity
        {
            Name = "GameOSAbilityAutoTargetProbe",
            EntityIdOverride = "brotato-like-ability-auto-target-probe"
        };
        autoAbility.Data.Set(AbilityDataKeys.IsEnabled, true);
        autoAbility.Data.Set(AbilityDataKeys.TargetSelection, AbilityTargetSelection.Entity);
        autoAbility.Data.Set(AbilityDataKeys.AutoTargetRange, 8f);
        autoAbility.Data.Set(AbilityDataKeys.Damage, 2f);
        AddChild(autoAbility);

        var autoSameTeam = new GodotEntity2D
        {
            Name = "GameOSAbilityAutoTargetSameTeamProbe",
            EntityIdOverride = "brotato-like-ability-auto-target-same-team-probe",
            Position = new Vector2(2f, 0f)
        };
        autoSameTeam.Data.Set(CollisionDataKeys.Team, 1);
        autoSameTeam.Data.Set(DamageDataKeys.CurrentHp, 10f);
        AddChild(autoSameTeam);

        var autoTarget = new GodotEntity2D
        {
            Name = "GameOSAbilityAutoTargetEnemyProbe",
            EntityIdOverride = "brotato-like-ability-auto-target-enemy-probe",
            Position = new Vector2(4f, 0f)
        };
        autoTarget.Data.Set(CollisionDataKeys.Team, 2);
        autoTarget.Data.Set(DamageDataKeys.CurrentHp, 10f);
        AddChild(autoTarget);

        var autoTargetBuilt = AbilityTargetingTool.TryBuildContext(caster, autoAbility, out var autoContext);
        var autoTargetReport = autoContext == null
            ? new AbilityTriggerReport(AbilityTriggerResult.FailNoTarget, null, "Auto target missing.")
            : AbilityService.Instance.TryTrigger(autoContext);
        var abilityAutoTargetSynced = false;
        if (autoTargetBuilt && autoContext?.Targets?.Count == 1)
        {
            abilityAutoTargetSynced = autoContext.Targets[0].EntityId == autoTarget.EntityId
                && autoContext.TargetPosition == new Vector2Value(4f, 0f)
                && autoTargetReport.Result == AbilityTriggerResult.Success
                && Math.Abs(autoTarget.Data.Get<float>(DamageDataKeys.CurrentHp) - 8f) < 0.001f
                && Math.Abs(autoSameTeam.Data.Get<float>(DamageDataKeys.CurrentHp) - 10f) < 0.001f;
        }

        var target = new GodotEntity2D
        {
            Name = "GameOSProjectileEffectTargetProbe",
            EntityIdOverride = "brotato-like-projectile-effect-target-probe",
            Position = new Vector2(12f, 0f)
        };
        target.Data.Set(CollisionDataKeys.CollisionLayer, CollisionLayers.EnemyHurtbox);
        target.Data.Set(CollisionDataKeys.CollisionRadius, 1f);
        target.Data.Set(CollisionDataKeys.Team, 2);
        target.Data.Set(DamageDataKeys.MaxHp, 20f);
        target.Data.Set(DamageDataKeys.CurrentHp, 20f);
        AddChild(target);

        var projectileEvent = false;
        var projectileEffectSpawner = new GodotProjectileEffectSpawner
        {
            Name = "GameOSProjectileEffectSpawnerProbe"
        };
        AddChild(projectileEffectSpawner);

        GlobalEventBus.Global.On<GameEventType.Projectile.SpawnedEventData>(
            GameEventType.Projectile.Spawned,
            data => projectileEvent = data.Source.EntityId == caster.EntityId && data.Target?.EntityId == target.EntityId);
        var projectile = ProjectileTool.Spawn(new ProjectileSpawnOptions
        {
            Source = caster,
            Ability = ability,
            Target = target,
            EntityId = "brotato-like-projectile-runtime-probe",
            ScenePath = "res://assets/Projectile/Projectile/Polygon2D/BulletDiamond.tscn",
            SpawnPosition = Vector2Value.Zero,
            Speed = 16f,
            Damage = 5f,
            DamageTags = DamageTags.Projectile | DamageTags.Ability
        });
        projectile.Projectile.Data.Set(CollisionDataKeys.CollisionLayer, CollisionLayers.Projectile);
        projectile.Projectile.Data.Set(CollisionDataKeys.CollisionMask, CollisionLayers.EnemyHurtbox);
        projectile.Projectile.Data.Set(CollisionDataKeys.CollisionRadius, 1f);
        projectile.Projectile.Data.Set(CollisionDataKeys.Team, 1);
        var projectileSourceRelationshipBound = RelationshipManager.HasRelationship(
            caster.EntityId,
            projectile.Projectile.EntityId,
            RelationshipType.EntityToProjectile);
        var projectileDirectionSynced = projectile.Projectile.Data.Get<Vector2Value>(ProjectileDataKeys.Direction) == new Vector2Value(1f, 0f);
        var projectileSpeedSynced = Math.Abs(projectile.Projectile.Data.Get<float>(ProjectileDataKeys.Speed) - 16f) < 0.001f;
        var projectileNode = GodotNodeRegistry.GetNodeById(projectile.Projectile.EntityId) as Node2D;
        var projectileHitEvent = false;
        GlobalEventBus.Global.On<GameEventType.Projectile.HitEventData>(
            GameEventType.Projectile.Hit,
            data => projectileHitEvent = data.Projectile.EntityId == projectile.Projectile.EntityId
                && data.Target.EntityId == target.EntityId
                && data.Damage.Applied);
        var projectileMovementDriver = new GodotMovementDriver
        {
            Name = "GameOSProjectileMovementDriverProbe",
            AutoTick = false
        };
        AddChild(projectileMovementDriver);
        var projectileMovementStarted = ProjectileTool.StartMovement(
            projectile.Projectile,
            projectileMovementDriver.MovementSystem,
            new ProjectileMovementOptions
            {
                TargetMatchMode = MovementCollisionTargetMatchMode.TrackedTargetOnly
            });
        projectileMovementDriver.TickMovement(1f);
        var projectileVisualQueuedForDeletion = projectileNode?.IsQueuedForDeletion() == true;
        var projectileRuntimeSynced = projectile.Created
            && projectileEvent
            && projectileNode != null
            && Math.Abs(projectileNode.Position.X) < 0.001f
            && Math.Abs(projectileNode.Position.Y) < 0.001f
            && projectileMovementStarted
            && projectileHitEvent
            && Math.Abs(target.Data.Get<float>(DamageDataKeys.CurrentHp) - 15f) < 0.001f
            && EntityManager.Get(projectile.Projectile.EntityId) == null
            && GodotNodeRegistry.GetNodeById(projectile.Projectile.EntityId) == null
            && projectileVisualQueuedForDeletion
            && projectileSourceRelationshipBound
            && projectileDirectionSynced
            && projectileSpeedSynced;

        var pierceTargetA = new GodotEntity2D
        {
            Name = "GameOSProjectilePierceTargetAProbe",
            EntityIdOverride = "brotato-like-projectile-pierce-target-a-probe",
            Position = new Vector2(5f, 24f)
        };
        pierceTargetA.Data.Set(CollisionDataKeys.CollisionLayer, CollisionLayers.EnemyHurtbox);
        pierceTargetA.Data.Set(CollisionDataKeys.CollisionRadius, 1f);
        pierceTargetA.Data.Set(CollisionDataKeys.Team, 2);
        pierceTargetA.Data.Set(DamageDataKeys.MaxHp, 20f);
        pierceTargetA.Data.Set(DamageDataKeys.CurrentHp, 20f);
        AddChild(pierceTargetA);

        var pierceTargetB = new GodotEntity2D
        {
            Name = "GameOSProjectilePierceTargetBProbe",
            EntityIdOverride = "brotato-like-projectile-pierce-target-b-probe",
            Position = new Vector2(10f, 24f)
        };
        pierceTargetB.Data.Set(CollisionDataKeys.CollisionLayer, CollisionLayers.EnemyHurtbox);
        pierceTargetB.Data.Set(CollisionDataKeys.CollisionRadius, 1f);
        pierceTargetB.Data.Set(CollisionDataKeys.Team, 2);
        pierceTargetB.Data.Set(DamageDataKeys.MaxHp, 20f);
        pierceTargetB.Data.Set(DamageDataKeys.CurrentHp, 20f);
        AddChild(pierceTargetB);

        var pierceTargetC = new GodotEntity2D
        {
            Name = "GameOSProjectilePierceTargetCProbe",
            EntityIdOverride = "brotato-like-projectile-pierce-target-c-probe",
            Position = new Vector2(15f, 24f)
        };
        pierceTargetC.Data.Set(CollisionDataKeys.CollisionLayer, CollisionLayers.EnemyHurtbox);
        pierceTargetC.Data.Set(CollisionDataKeys.CollisionRadius, 1f);
        pierceTargetC.Data.Set(CollisionDataKeys.Team, 2);
        pierceTargetC.Data.Set(DamageDataKeys.MaxHp, 20f);
        pierceTargetC.Data.Set(DamageDataKeys.CurrentHp, 20f);
        AddChild(pierceTargetC);

        var pierceProjectile = ProjectileTool.Spawn(new ProjectileSpawnOptions
        {
            Source = caster,
            Ability = ability,
            EntityId = "brotato-like-projectile-pierce-runtime-probe",
            SpawnPosition = new Vector2Value(0f, 24f),
            Direction = new Vector2Value(1f, 0f),
            Speed = 20f,
            MaxHitCount = 2,
            Damage = 3f,
            DamageTags = DamageTags.Projectile | DamageTags.Ability
        });
        pierceProjectile.Projectile.Data.Set(CollisionDataKeys.CollisionLayer, CollisionLayers.Projectile);
        pierceProjectile.Projectile.Data.Set(CollisionDataKeys.CollisionMask, CollisionLayers.EnemyHurtbox);
        pierceProjectile.Projectile.Data.Set(CollisionDataKeys.CollisionRadius, 1f);
        pierceProjectile.Projectile.Data.Set(CollisionDataKeys.Team, 1);
        var pierceHitCount = 0;
        GlobalEventBus.Global.On<GameEventType.Projectile.HitEventData>(
            GameEventType.Projectile.Hit,
            data =>
            {
                if (data.Projectile.EntityId == pierceProjectile.Projectile.EntityId)
                {
                    pierceHitCount++;
                }
            });
        var pierceMovementStarted = ProjectileTool.StartMovement(
            pierceProjectile.Projectile,
            projectileMovementDriver.MovementSystem);
        projectileMovementDriver.TickMovement(1f);
        var projectilePierceSynced = pierceMovementStarted
            && pierceHitCount == 2
            && Math.Abs(pierceTargetA.Data.Get<float>(DamageDataKeys.CurrentHp) - 17f) < 0.001f
            && Math.Abs(pierceTargetB.Data.Get<float>(DamageDataKeys.CurrentHp) - 17f) < 0.001f
            && Math.Abs(pierceTargetC.Data.Get<float>(DamageDataKeys.CurrentHp) - 20f) < 0.001f
            && EntityManager.Get(pierceProjectile.Projectile.EntityId) == null;

        var lifetimeProjectile = ProjectileTool.Spawn(new ProjectileSpawnOptions
        {
            Source = caster,
            Ability = ability,
            EntityId = "brotato-like-projectile-lifetime-runtime-probe",
            SpawnPosition = new Vector2Value(0f, 48f),
            Direction = new Vector2Value(1f, 0f),
            Speed = 4f,
            MaxLifeTime = 0.25f,
            Damage = 0f
        });
        var lifetimeMovementStarted = ProjectileTool.StartMovement(
            lifetimeProjectile.Projectile,
            projectileMovementDriver.MovementSystem,
            new ProjectileMovementOptions
            {
                ApplyDamageOnHit = false
            });
        projectileMovementDriver.TickMovement(0.25f);
        var projectileLifetimeSynced = lifetimeMovementStarted
            && EntityManager.Get(lifetimeProjectile.Projectile.EntityId) == null;

        projectileRuntimeSynced = projectileRuntimeSynced
            && projectilePierceSynced
            && projectileLifetimeSynced;

        var effectEvent = false;
        GlobalEventBus.Global.On<GameEventType.Effect.SpawnedEventData>(
            GameEventType.Effect.Spawned,
            data => effectEvent = data.Source.EntityId == caster.EntityId && data.Target?.EntityId == target.EntityId);
        var effect = EffectTool.Spawn(new EffectSpawnOptions
        {
            Source = caster,
            Ability = ability,
            Target = target,
            EntityId = "brotato-like-effect-runtime-probe",
            ScenePath = "res://assets/Effect/003/AnimatedSprite2D/003.tscn",
            Name = "Impact",
            AnimationName = "Effect",
            Position = Vector2Value.Zero,
            Duration = 0.5f
        });
        var effectNode = GodotNodeRegistry.GetNodeById(effect.Effect.EntityId) as Node2D;
        var effectSprite = ResolveAnimatedSprite(effectNode);
        var effectAnimationSynced = effectSprite != null
            && effectSprite.Animation.ToString() == "Effect"
            && effectSprite.IsPlaying()
            && Math.Abs(effectSprite.SpeedScale - 2.2f) < 0.001f;
        var effectRuntimeSynced = effect.Created
            && effectEvent
            && effectNode != null
            && Math.Abs(effectNode.Position.X - 12f) < 0.001f
            && Math.Abs(effectNode.Position.Y) < 0.001f
            && effectAnimationSynced
            && RelationshipManager.HasRelationship(caster.EntityId, effect.Effect.EntityId, RelationshipType.EntityToEffect)
            && effect.Effect.Data.Get<Vector2Value>(EffectDataKeys.Position) == new Vector2Value(12f, 0f)
            && effect.Effect.Data.Get<string>(EffectDataKeys.AnimationName) == "Effect"
            && Math.Abs(effect.Effect.Data.Get<float>(EffectDataKeys.Duration) - 0.5f) < 0.001f;

        return new AbilityRuntimeProbe(
            abilityPointSynced,
            abilityAutoTargetSynced,
            dataOsHandlerSynced,
            projectileRuntimeSynced,
            effectRuntimeSynced);
    }

    private GodotPlayerInputProbe RunPlayerInputProbe()
    {
        // 1. 测试 GodotPlayerInputComponent 注册并写入 InputDirection
        var playerEntity = new GodotEntity2D
        {
            Name = "GameOSPlayerInputProbe",
            EntityIdOverride = "brotato-like-player-input-probe"
        };
        var inputComponent = new GodotPlayerInputComponent
        {
            Name = "PlayerInputProbe",
            AutoTick = false
        };
        playerEntity.AddChild(inputComponent);
        AddChild(playerEntity);

        // 手动注册组件（headless 测试不走完整 _EnterTree 异步生命周期）
        inputComponent.OnComponentRegistered(playerEntity, playerEntity);
        inputComponent.TickInput();
        var inputDirectionWritten = playerEntity.Data.Has(MovementDataKeys.InputDirection);
        var componentRegistered = inputComponent.LastInputDirection == Vector2Value.Zero;

        // 2. 测试 Acceleration > 0 时平滑移动
        var smoothEntity = new GodotEntity2D
        {
            Name = "GameOSPlayerInputSmoothProbe",
            EntityIdOverride = "brotato-like-player-input-smooth-probe"
        };
        AddChild(smoothEntity);
        smoothEntity.Data.Set(MovementDataKeys.MoveSpeed, 100f);
        smoothEntity.Data.Set(MovementDataKeys.Acceleration, 12f);

        var smoothDriver = new GodotMovementDriver { Name = "SmoothDriverProbe", AutoTick = false };
        AddChild(smoothDriver);
        smoothDriver.MovementSystem.Start(smoothEntity, new MovementParams
        {
            Mode = MoveMode.PlayerInput,
            MaxDuration = 2f
        });
        smoothEntity.Data.Set(MovementDataKeys.InputDirection, new Vector2Value(1f, 0f));
        smoothDriver.TickMovement(0.05f);
        var v1 = smoothEntity.Data.Get<Vector2Value>(MovementDataKeys.Velocity, Vector2Value.Zero);
        smoothDriver.TickMovement(0.5f);
        var v2 = smoothEntity.Data.Get<Vector2Value>(MovementDataKeys.Velocity, Vector2Value.Zero);

        // v1 应小于目标速度（未加速到最大），v2 应接近目标速度
        var smoothAcceleration = v1.Length < 90f && v2.Length > 95f && v2.Length <= 100f;

        // 3. 测试 Acceleration = 0 时直接到达目标速度
        var directEntity = new GodotEntity2D
        {
            Name = "GameOSPlayerInputDirectProbe",
            EntityIdOverride = "brotato-like-player-input-direct-probe"
        };
        AddChild(directEntity);
        directEntity.Data.Set(MovementDataKeys.MoveSpeed, 80f);
        directEntity.Data.Set(MovementDataKeys.Acceleration, 0f);

        var directDriver = new GodotMovementDriver { Name = "DirectDriverProbe", AutoTick = false };
        AddChild(directDriver);
        directDriver.MovementSystem.Start(directEntity, new MovementParams
        {
            Mode = MoveMode.PlayerInput,
            MaxDuration = 1f
        });
        directEntity.Data.Set(MovementDataKeys.InputDirection, new Vector2Value(0f, 1f));
        directDriver.TickMovement(0.016f);
        var directV = directEntity.Data.Get<Vector2Value>(MovementDataKeys.Velocity, Vector2Value.Zero);

        // 直接速度应瞬间到达目标
        var directVelocityFallback = Math.Abs(directV.Length - 80f) < 0.001f;

        return new GodotPlayerInputProbe(componentRegistered, inputDirectionWritten, smoothAcceleration, directVelocityFallback);
    }

    private GodotActiveSkillInputProbe RunActiveSkillInputProbe()
    {
        // 使用 DataOS 创建技能实体（需要 handler 注册）
        BrotatoLikeAbilityHandlers.RegisterAll();
        var bootstrap = BrotatoLikeDataOSBootstrap.LoadFromResource();

        // 创建玩家实体
        var playerEntity = new GodotEntity2D
        {
            Name = "GameOSActiveSkillInputProbe",
            EntityIdOverride = "brotato-like-active-skill-input-probe",
            Position = Vector2.Zero
        };
        playerEntity.Data.Set(MovementDataKeys.Position, new Vector2Value(0f, 0f));
        playerEntity.Data.Set(CollisionDataKeys.Team, 1);
        AddChild(playerEntity);

        // 创建两个技能实体
        var ability1 = bootstrap.SpawnEntityFromRecord("ability", "slam", "probe-ability-slam");
        var ability2 = bootstrap.SpawnEntityFromRecord("ability", "chain_lightning", "probe-ability-chain");

        // 写入玩家技能列表
        var ownedIds = new List<string> { ability1.EntityId, ability2.EntityId };
        playerEntity.Data.Set(AbilityDataKeys.OwnedAbilityIds, ownedIds);
        playerEntity.Data.Set(AbilityDataKeys.CurrentAbilityIndex, 0);

        // 挂载输入组件和技能输入组件
        var inputComponent = new GodotPlayerInputComponent
        {
            Name = "PlayerInputProbe",
            AutoTick = false
        };
        playerEntity.AddChild(inputComponent);
        inputComponent.OnComponentRegistered(playerEntity, playerEntity);

        var skillInputComponent = new GodotActiveSkillInputComponent
        {
            Name = "ActiveSkillInputProbe"
        };
        playerEntity.AddChild(skillInputComponent);
        skillInputComponent.OnComponentRegistered(playerEntity, playerEntity);

        // 1. 测试技能切换：Next 应从 0 -> 1
        playerEntity.Events.Emit(
            GodotPlayerInputComponent.NextSkillEvent,
            new GodotPlayerInputComponent.NextSkillEventData(playerEntity));
        var indexAfterNext = playerEntity.Data.Get<int>(AbilityDataKeys.CurrentAbilityIndex, 0);

        // 2. 测试技能切换：Previous 应从 1 -> 0
        playerEntity.Events.Emit(
            GodotPlayerInputComponent.PreviousSkillEvent,
            new GodotPlayerInputComponent.PreviousSkillEventData(playerEntity));
        var indexAfterPrevious = playerEntity.Data.Get<int>(AbilityDataKeys.CurrentAbilityIndex, 0);

        var skillSwitched = indexAfterNext == 1 && indexAfterPrevious == 0;

        // 3. 测试技能释放：发射 UseSkill 事件，验证 ability:activated 被触发
        var abilityActivated = false;
        ability1.Events.On<GameEventType.Ability.ActivatedEventData>(
            GameEventType.Ability.Activated,
            data => abilityActivated = data.Context.Caster.EntityId == playerEntity.EntityId
                && data.Context.Ability.EntityId == ability1.EntityId);

        playerEntity.Events.Emit(
            GodotPlayerInputComponent.UseSkillEvent,
            new GodotPlayerInputComponent.UseSkillEventData(playerEntity));

        var skillTriggered = abilityActivated;

        return new GodotActiveSkillInputProbe(skillSwitched, skillTriggered);
    }

    private static SpriteFrames CreateAttackProbeSpriteFrames()
    {
        var texture = ImageTexture.CreateFromImage(Image.CreateEmpty(2, 2, false, Image.Format.Rgba8));
        var frames = new SpriteFrames();
        frames.AddAnimation("idle");
        frames.SetAnimationLoop("idle", true);
        frames.AddFrame("idle", texture);
        frames.AddAnimation("attack2");
        frames.SetAnimationLoop("attack2", false);
        frames.AddFrame("attack2", texture);
        return frames;
    }

    private static AnimatedSprite2D? ResolveAnimatedSprite(Node? node)
    {
        if (node is AnimatedSprite2D sprite)
        {
            return sprite;
        }

        if (node == null)
        {
            return null;
        }

        if (node.GetNodeOrNull("VisualRoot") is AnimatedSprite2D directSprite)
        {
            return directSprite;
        }

        var descendants = node.FindChildren("*", nameof(AnimatedSprite2D), recursive: true, owned: false);
        return descendants.Count > 0 ? descendants[0] as AnimatedSprite2D : null;
    }

    private static Area2D CreatePoolProbeArea()
    {
        var area = new Area2D
        {
            CollisionLayer = 1,
            CollisionMask = 1
        };
        area.AddChild(new CollisionShape2D
        {
            Shape = new CircleShape2D { Radius = 4f }
        });
        return area;
    }

    private GodotMovementProbe RunGodotMovementProbe()
    {
        var movingEntity = new GodotEntity2D
        {
            Name = "GameOSMovingProbe",
            EntityIdOverride = "brotato-like-moving-probe",
            Position = Vector2.Zero
        };
        AddChild(movingEntity);

        var movementDriver = new GodotMovementDriver
        {
            Name = "GameOSMovementDriver",
            AutoTick = false
        };
        AddChild(movementDriver);

        movementDriver.MovementSystem.Start(movingEntity, new MovementParams
        {
            Mode = MoveMode.Charge,
            TargetPosition = new Vector2Value(18f, 0f),
            Speed = 36f,
            ReachDistance = 0f
        });
        movementDriver.TickMovement(0.5f);

        var runtimePosition = movingEntity.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
        var chargeSynced = Math.Abs(movingEntity.Position.X - 18f) < 0.001f
            && Math.Abs(runtimePosition.X - 18f) < 0.001f
            && !movementDriver.MovementSystem.IsMoving(movingEntity);

        var orbitEntity = new GodotEntity2D
        {
            Name = "GameOSOrbitProbe",
            EntityIdOverride = "brotato-like-orbit-probe",
            Position = new Vector2(10f, 0f)
        };
        AddChild(orbitEntity);
        movementDriver.MovementSystem.Start(orbitEntity, new MovementParams
        {
            Mode = MoveMode.Orbit,
            OrbitCenter = Vector2Value.Zero,
            OrbitRadius = 10f,
            OrbitInitAngle = 0f,
            OrbitAngularSpeed = 90f,
            OrbitTotalAngle = 90f
        });
        movementDriver.TickMovement(1f);
        var orbitSynced = Math.Abs(orbitEntity.Position.X) < 0.001f
            && Math.Abs(orbitEntity.Position.Y - 10f) < 0.001f
            && !movementDriver.MovementSystem.IsMoving(orbitEntity);

        var sineEntity = new GodotEntity2D
        {
            Name = "GameOSSineProbe",
            EntityIdOverride = "brotato-like-sine-probe",
            Position = Vector2.Zero
        };
        AddChild(sineEntity);
        movementDriver.MovementSystem.Start(sineEntity, new MovementParams
        {
            Mode = MoveMode.SineWave,
            Angle = 0f,
            Speed = 10f,
            WaveAmplitude = 5f,
            WaveFrequency = 0.25f,
            MaxDuration = 1f
        });
        movementDriver.TickMovement(1f);
        var sineSynced = Math.Abs(sineEntity.Position.X - 10f) < 0.001f
            && Math.Abs(sineEntity.Position.Y - 5f) < 0.001f
            && !movementDriver.MovementSystem.IsMoving(sineEntity);

        var bezierEntity = new GodotEntity2D
        {
            Name = "GameOSBezierProbe",
            EntityIdOverride = "brotato-like-bezier-probe",
            Position = Vector2.Zero
        };
        AddChild(bezierEntity);
        movementDriver.MovementSystem.Start(bezierEntity, new MovementParams
        {
            Mode = MoveMode.BezierCurve,
            TargetPosition = new Vector2Value(10f, 0f),
            BezierPoints = [new Vector2Value(5f, -10f), new Vector2Value(10f, 0f)],
            MaxDuration = 1f
        });
        movementDriver.TickMovement(1f);
        var bezierSynced = Math.Abs(bezierEntity.Position.X - 10f) < 0.001f
            && Math.Abs(bezierEntity.Position.Y) < 0.001f
            && !movementDriver.MovementSystem.IsMoving(bezierEntity);

        var boomerangEntity = new GodotEntity2D
        {
            Name = "GameOSBoomerangProbe",
            EntityIdOverride = "brotato-like-boomerang-probe",
            Position = Vector2.Zero
        };
        AddChild(boomerangEntity);
        movementDriver.MovementSystem.Start(boomerangEntity, new MovementParams
        {
            Mode = MoveMode.Boomerang,
            TargetPosition = new Vector2Value(10f, 0f),
            Speed = 10f,
            ReachDistance = 0.001f
        });
        movementDriver.TickMovement(1f);
        movementDriver.TickMovement(1f);
        var boomerangSynced = Math.Abs(boomerangEntity.Position.X) < 0.001f
            && Math.Abs(boomerangEntity.Position.Y) < 0.001f
            && !movementDriver.MovementSystem.IsMoving(boomerangEntity);

        var hostEntity = new GodotEntity2D
        {
            Name = "GameOSAttachHostProbe",
            EntityIdOverride = "brotato-like-attach-host-probe",
            Position = new Vector2(20f, 5f)
        };
        AddChild(hostEntity);
        var attachEntity = new GodotEntity2D
        {
            Name = "GameOSAttachProbe",
            EntityIdOverride = "brotato-like-attach-probe",
            Position = Vector2.Zero
        };
        AddChild(attachEntity);
        movementDriver.MovementSystem.Start(attachEntity, new MovementParams
        {
            Mode = MoveMode.AttachToHost,
            TargetEntityId = hostEntity.EntityId,
            AttachOffset = new Vector2Value(2f, -1f),
            MaxDuration = 1f
        });
        movementDriver.TickMovement(0.25f);
        var attachSynced = Math.Abs(attachEntity.Position.X - 22f) < 0.001f
            && Math.Abs(attachEntity.Position.Y - 4f) < 0.001f
            && movementDriver.MovementSystem.IsMoving(attachEntity);

        var playerInputEntity = new GodotEntity2D
        {
            Name = "GameOSPlayerInputProbe",
            EntityIdOverride = "brotato-like-player-input-probe",
            Position = Vector2.Zero
        };
        AddChild(playerInputEntity);
        playerInputEntity.Data.Set(MovementDataKeys.MoveSpeed, 12f);
        playerInputEntity.Data.Set(MovementDataKeys.InputDirection, new Vector2Value(1f, 0f));
        movementDriver.MovementSystem.Start(playerInputEntity, new MovementParams
        {
            Mode = MoveMode.PlayerInput,
            MaxDuration = 1f
        });
        movementDriver.TickMovement(0.5f);
        var playerInputSynced = Math.Abs(playerInputEntity.Position.X - 6f) < 0.001f
            && Math.Abs(playerInputEntity.Position.Y) < 0.001f;

        var aiEntity = new GodotEntity2D
        {
            Name = "GameOSAIControlledProbe",
            EntityIdOverride = "brotato-like-ai-controlled-probe",
            Position = Vector2.Zero
        };
        AddChild(aiEntity);
        aiEntity.Data.Set(MovementDataKeys.MoveSpeed, 20f);
        aiEntity.Data.Set(MovementDataKeys.AIMoveDirection, new Vector2Value(0f, 1f));
        aiEntity.Data.Set(MovementDataKeys.AIMoveSpeedMultiplier, 0.5f);
        movementDriver.MovementSystem.Start(aiEntity, new MovementParams
        {
            Mode = MoveMode.AIControlled,
            MaxDuration = 1f
        });
        movementDriver.TickMovement(0.5f);
        var aiControlledSynced = Math.Abs(aiEntity.Position.X) < 0.001f
            && Math.Abs(aiEntity.Position.Y - 5f) < 0.001f;

        var parabolaEntity = new GodotEntity2D
        {
            Name = "GameOSParabolaProbe",
            EntityIdOverride = "brotato-like-parabola-probe",
            Position = Vector2.Zero
        };
        AddChild(parabolaEntity);
        movementDriver.MovementSystem.Start(parabolaEntity, new MovementParams
        {
            Mode = MoveMode.Parabola,
            TargetPosition = new Vector2Value(10f, 0f),
            MaxDuration = 1f,
            ParabolaApexHeight = -5f
        });
        movementDriver.TickMovement(1f);
        var parabolaSynced = Math.Abs(parabolaEntity.Position.X - 10f) < 0.001f
            && Math.Abs(parabolaEntity.Position.Y) < 0.001f
            && !movementDriver.MovementSystem.IsMoving(parabolaEntity);

        var arcEntity = new GodotEntity2D
        {
            Name = "GameOSCircularArcProbe",
            EntityIdOverride = "brotato-like-circular-arc-probe",
            Position = new Vector2(10f, 0f)
        };
        AddChild(arcEntity);
        movementDriver.MovementSystem.Start(arcEntity, new MovementParams
        {
            Mode = MoveMode.CircularArc,
            TargetPosition = new Vector2Value(-10f, 0f),
            CircularArcRadius = 10f,
            CircularArcClockwise = true,
            MaxDuration = 1f
        });
        movementDriver.TickMovement(1f);
        var circularArcSynced = Math.Abs(arcEntity.Position.X + 10f) < 0.001f
            && Math.Abs(arcEntity.Position.Y) < 0.001f
            && !movementDriver.MovementSystem.IsMoving(arcEntity);

        var collisionMover = new GodotAreaEntity2D
        {
            Name = "GameOSMovementCollisionMoverProbe",
            EntityIdOverride = "brotato-like-movement-collision-mover-probe",
            Position = Vector2.Zero,
            CollisionLayer = CollisionLayers.Projectile,
            CollisionMask = CollisionLayers.EnemyHurtbox
        };
        collisionMover.AddChild(CreateCircleShape(1f));
        AddChild(collisionMover);

        var collisionTarget = new GodotAreaEntity2D
        {
            Name = "GameOSMovementCollisionTargetProbe",
            EntityIdOverride = "brotato-like-movement-collision-target-probe",
            Position = new Vector2(10f, 0f),
            CollisionLayer = CollisionLayers.EnemyHurtbox,
            CollisionMask = CollisionLayers.Projectile
        };
        collisionTarget.AddChild(CreateCircleShape(1f));
        AddChild(collisionTarget);

        var collisionEvent = false;
        collisionMover.Events.On<GameEventType.Movement.CollisionEventData>(
            GameEventType.Movement.Collision,
            data => collisionEvent = data.Context.Target.EntityId == collisionTarget.EntityId);
        movementDriver.MovementSystem.Start(collisionMover, new MovementParams
        {
            Mode = MoveMode.Charge,
            Direction = new Vector2Value(1f, 0f),
            Speed = 20f,
            CollisionParams = new MovementCollisionParams
            {
                TargetMatchMode = MovementCollisionTargetMatchMode.SpecificEntity,
                SpecificTargetEntityId = collisionTarget.EntityId,
                StopAfterCollisionCount = 1
            }
        });
        movementDriver.TickMovement(1f);
        var physicsQueryUsed = movementDriver.PhysicsCollisionTargetQuery.LastQueryUsedPhysics
            && movementDriver.PhysicsCollisionTargetQuery.LastPhysicsCandidateCount > 0;
        var collisionSynced = collisionEvent
            && physicsQueryUsed
            && Math.Abs(collisionMover.Position.X - 8f) < 0.001f
            && Math.Abs(collisionMover.Position.Y) < 0.001f
            && !movementDriver.MovementSystem.IsMoving(collisionMover);

        return new GodotMovementProbe(
            chargeSynced,
            orbitSynced,
            sineSynced,
            bezierSynced,
            boomerangSynced,
            attachSynced,
            playerInputSynced,
            aiControlledSynced,
            parabolaSynced,
            circularArcSynced,
            collisionSynced);
    }

    private static CollisionShape2D CreateCircleShape(float radius)
    {
        return new CollisionShape2D
        {
            Shape = new CircleShape2D { Radius = radius }
        };
    }
}

internal readonly record struct GodotBridgeProbe(
    bool EntityRegistered,
    bool ComponentBound,
    bool ComponentCallback,
    bool CollisionBridgeEntered,
    bool HurtboxBridgeEntered,
    bool NodePoolReused,
    bool MovementNodeSynced,
    bool OrbitNodeSynced,
    bool SineNodeSynced,
    bool BezierNodeSynced,
    bool BoomerangNodeSynced,
    bool AttachNodeSynced,
    bool PlayerInputNodeSynced,
    bool AIControlledNodeSynced,
    bool ParabolaNodeSynced,
    bool CircularArcNodeSynced,
    bool MovementCollisionNodeSynced,
    bool OrientationNodeSynced,
    bool OrientationSpinSynced,
    bool ContactDamageSynced,
    bool AttackBridgeSynced,
    bool AttackAnimationSynced,
    bool AIBridgeSynced,
    bool AbilityPointSynced,
    bool AbilityAutoTargetSynced,
    bool AbilityDataOSHandlerSynced,
    bool ProjectileRuntimeSynced,
    bool EffectRuntimeSynced,
    bool PlayerInputBridgeSynced,
    bool ActiveSkillInputSynced);

internal readonly record struct GodotMovementProbe(
    bool ChargeSynced,
    bool OrbitSynced,
    bool SineSynced,
    bool BezierSynced,
    bool BoomerangSynced,
    bool AttachSynced,
    bool PlayerInputSynced,
    bool AIControlledSynced,
    bool ParabolaSynced,
    bool CircularArcSynced,
    bool CollisionSynced);

internal readonly record struct GodotCollisionProbe(
    bool CollisionEntered,
    bool CollisionExited,
    bool HurtboxEntered,
    bool HurtboxExited);

internal readonly record struct GodotNodePoolProbe(bool Returned, bool DetachedWhileIdle, bool Reused);

internal readonly record struct DataOSProbe(
    bool SnapshotApplied,
    bool AbilityApplied,
    bool ResourcesRegistered,
    bool SpawnSystemSynced);

internal readonly record struct MainEntryProbe(
    bool GameStartedEventEmitted,
    bool SmokeEntryKeptSeparate,
    bool CameraMounted);

internal readonly record struct GodotOrientationProbe(bool RootRotationSynced, bool SpinSynced);

internal readonly record struct GodotContactDamageProbe(bool DamageApplied, bool SameTeamIgnored);

internal readonly record struct GodotAttackProbe(
    bool ExportedDataApplied,
    bool DamageApplied,
    bool Finished,
    bool AnimationPlayed,
    bool CancelReturnedIdle,
    bool AnimationFinishedEvent,
    bool AnimationFinishedReturnedIdle,
    bool AvailableAnimationsCached,
    bool LegacyWrapperSynced);

internal readonly record struct AbilityRuntimeProbe(
    bool AbilityPointSynced,
    bool AbilityAutoTargetSynced,
    bool AbilityDataOSHandlerSynced,
    bool ProjectileRuntimeSynced,
    bool EffectRuntimeSynced);

internal readonly record struct GodotPlayerInputProbe(
    bool ComponentRegistered,
    bool InputDirectionWritten,
    bool SmoothAcceleration,
    bool DirectVelocityFallback);

internal readonly record struct GodotActiveSkillInputProbe(
    bool SkillSwitched,
    bool SkillTriggered);

internal readonly record struct GodotAIProbe(
    bool ExportedDataApplied,
    bool MoveIntentWritten,
    bool MovementSynced);
