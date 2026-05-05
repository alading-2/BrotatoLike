using System;
using SkilmeAI.GameOS.Capabilities.AI;
using Godot;
using SkilmeAI.GameOS.Capabilities.Ability;
using SkilmeAI.GameOS.Capabilities.Attack;
using SkilmeAI.GameOS.Capabilities.Collision;
using SkilmeAI.GameOS.Capabilities.Damage;
using SkilmeAI.GameOS.Capabilities.Effect;
using SkilmeAI.GameOS.Capabilities.Movement;
using SkilmeAI.GameOS.Capabilities.Projectile;
using SkilmeAI.GameOS.Capabilities.Unit;
using SkilmeAI.GameOS.GodotBridge;
using SkilmeAI.GameOS.Runtime.Data;
using SkilmeAI.GameOS.Runtime.Entity;
using SkilmeAI.GameOS.Runtime.Event;
using SkilmeAI.GameOS.Runtime.Relationship;
using SkilmeAI.GameOS.Runtime.Resource;
using SkilmeAI.GameOS.Runtime.Timer;

namespace BrotatoLike.Game;

/// <summary>
/// BrotatoLike 仓库第一个启动场景节点。
/// </summary>
public partial class Main : Node
{
    /// <inheritdoc />
    public override void _Ready()
    {
        var probe = GameBootstrap.RunFrameworkSmokeProbe();
        var bridgeProbe = RunGodotBridgeProbe();
        var dataOsProbe = RunDataOSSnapshotProbe();
        GD.Print($"BrotatoLike GameOS smoke: {probe.EntityId} {probe.MainScenePath} bridge:{bridgeProbe.ComponentBound} pool:{bridgeProbe.NodePoolReused} dataos:{dataOsProbe.SnapshotApplied}");

        if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--gameos-smoke-exit") >= 0)
        {
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
                && bridgeProbe.ProjectileRuntimeSynced
                && bridgeProbe.EffectRuntimeSynced
                && dataOsProbe.SnapshotApplied
                && dataOsProbe.AbilityApplied
                && dataOsProbe.ResourcesRegistered;
            GD.Print(success ? "BrotatoLike GameOS smoke PASS" : "BrotatoLike GameOS smoke FAIL");
            GetTree().Quit(success ? 0 : 1);
        }
    }

    private DataOSProbe RunDataOSSnapshotProbe()
    {
        using var file = FileAccess.Open("res://DataOS/Snapshots/runtime_snapshot.json", FileAccess.ModeFlags.Read);
        if (file == null)
        {
            return new DataOSProbe(false, false, false);
        }

        var snapshot = RuntimeDataSnapshot.FromJson(file.GetAsText());
        var foundEnemy = snapshot.TryFindRecord("unit.enemy", "yuren", out var enemyRecord);
        var foundAbility = snapshot.TryFindRecord("ability", "slam", out var abilityRecord);

        var enemyData = new SkilmeAI.GameOS.Runtime.Data.Data();
        var abilityData = new SkilmeAI.GameOS.Runtime.Data.Data();
        if (foundEnemy)
        {
            snapshot.ApplyRecord(enemyData, enemyRecord);
        }

        if (foundAbility)
        {
            snapshot.ApplyRecord(abilityData, abilityRecord);
        }

        var resourceCount = snapshot.RegisterResources();
        var enemyApplied = foundEnemy
            && Math.Abs(enemyData.Get<float>(DamageDataKeys.MaxHp) - 150f) < 0.001f
            && Math.Abs(enemyData.Get<float>(MovementDataKeys.MoveSpeed) - 150f) < 0.001f;
        var abilityApplied = foundAbility
            && abilityData.Get<AbilityTriggerMode>(AbilityDataKeys.TriggerMode) == AbilityTriggerMode.Manual
            && Math.Abs(abilityData.Get<float>(AbilityDataKeys.Damage) - 30f) < 0.001f;
        var resourcesRegistered = resourceCount > 0
            && ResourceManagement.GetPath("Unit.Enemy.Yuren.Visual", ResourceCategory.Asset) == "res://assets/Unit/Enemy/yuren/AnimatedSprite2D/yuren.tscn";

        return new DataOSProbe(enemyApplied, abilityApplied, resourcesRegistered);
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
            ProjectileRuntimeSynced: abilityRuntimeProbe.ProjectileRuntimeSynced,
            EffectRuntimeSynced: abilityRuntimeProbe.EffectRuntimeSynced);
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
            projectileRuntimeSynced,
            effectRuntimeSynced);
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
    bool ProjectileRuntimeSynced,
    bool EffectRuntimeSynced);

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

internal readonly record struct DataOSProbe(bool SnapshotApplied, bool AbilityApplied, bool ResourcesRegistered);

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
    bool ProjectileRuntimeSynced,
    bool EffectRuntimeSynced);

internal readonly record struct GodotAIProbe(
    bool ExportedDataApplied,
    bool MoveIntentWritten,
    bool MovementSynced);
