using System;
using System.Collections.Generic;
using SkilmeAI.GameOS.Capabilities.Ability;
using SkilmeAI.GameOS.Capabilities.Collision;
using SkilmeAI.GameOS.Capabilities.Damage;
using SkilmeAI.GameOS.Capabilities.Effect;
using SkilmeAI.GameOS.Capabilities.Feature;
using SkilmeAI.GameOS.Capabilities.Movement;
using SkilmeAI.GameOS.Capabilities.Projectile;
using SkilmeAI.GameOS.Runtime.Entity;
using SkilmeAI.GameOS.Runtime.Timer;

namespace BrotatoLike.Game;

/// <summary>
/// BrotatoLike 游戏侧 Ability / Feature handler 注册入口。
/// </summary>
public static class BrotatoLikeAbilityHandlers
{
    /// <summary>
    /// 注册当前已迁入的游戏侧 handler。重复注册会覆盖同 Id handler。
    /// </summary>
    public static void RegisterAll()
    {
        FeatureHandlerRegistry.Register(new BrotatoLikeAreaDamageAbilityHandler("技能.主动.猛击", DamageType.Physical));
        FeatureHandlerRegistry.Register(new BrotatoLikeAreaDamageAbilityHandler("技能.主动.位置目标", DamageType.Physical));
        FeatureHandlerRegistry.Register(new BrotatoLikeProjectileAbilityHandler("技能.投射物.正弦波射击"));
        FeatureHandlerRegistry.Register(new BrotatoLikeProjectileAbilityHandler("技能.投射物.回旋镖投掷"));
        FeatureHandlerRegistry.Register(new BrotatoLikeProjectileAbilityHandler("技能.投射物.贝塞尔射击"));
        FeatureHandlerRegistry.Register(new BrotatoLikeProjectileAbilityHandler("技能.投射物.定点抛炸弹"));
        FeatureHandlerRegistry.Register(new BrotatoLikeProjectileAbilityHandler("技能.投射物.圆弧射击"));
        FeatureHandlerRegistry.Register(new BrotatoLikeDashAbilityHandler("技能.位移.冲刺"));
        FeatureHandlerRegistry.Register(new BrotatoLikeProjectileAbilityHandler("技能.被动.环绕技能"));
        FeatureHandlerRegistry.Register(new BrotatoLikeAreaDamageAbilityHandler("技能.被动.圆环伤害", DamageType.Magical));
        FeatureHandlerRegistry.Register(new BrotatoLikeProjectileAbilityHandler("技能.被动.光环护盾"));
        FeatureHandlerRegistry.Register(new BrotatoLikeChainLightningHandler("技能.主动.连锁闪电"));
    }
}

/// <summary>
/// 从 DataOS 位移参数执行冲刺移动。
/// </summary>
public sealed class BrotatoLikeDashAbilityHandler : IFeatureHandler
{
    private readonly MovementSystem movement = new();

    /// <inheritdoc />
    public string FeatureId { get; }

    /// <summary>
    /// 创建冲刺 handler。
    /// </summary>
    /// <param name="featureId">完整 Feature handler Id。</param>
    public BrotatoLikeDashAbilityHandler(string featureId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureId);
        FeatureId = featureId;
    }

    /// <inheritdoc />
    public object? OnExecute(FeatureContext context)
    {
        if (context.ActivationData is not AbilityCastContext cast)
        {
            return new AbilityExecutedResult();
        }

        var origin = cast.Caster.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
        var direction = ResolveDashDirection(cast, origin);
        if (direction == Vector2Value.Zero)
        {
            return new AbilityExecutedResult();
        }

        var maxDistance = ResolveDashDistance(cast.Ability);
        var targetPosition = origin + (direction * maxDistance);
        var movementParams = BuildMovementParams(cast.Ability, direction, targetPosition, maxDistance);
        var started = movement.Start(cast.Caster, movementParams);
        if (started)
        {
            ApplyMovementAuthoringData(cast.Caster, in movementParams, cast.Ability);
            SpawnDashEffect(cast, origin);
        }

        return new AbilityExecutedResult
        {
            TargetsHit = started ? 1 : 0
        };
    }

    /// <summary>
    /// 推进当前 handler 持有的冲刺移动。
    /// </summary>
    /// <param name="delta">经过秒数。</param>
    public void Tick(float delta)
    {
        movement.Tick(delta);
    }

    private static Vector2Value ResolveDashDirection(AbilityCastContext cast, Vector2Value origin)
    {
        var targetPosition = cast.TargetPosition ?? ResolveTargetPosition(cast);
        if (targetPosition.HasValue)
        {
            var direction = (targetPosition.Value - origin).Normalized();
            if (direction != Vector2Value.Zero)
            {
                return direction;
            }
        }

        return cast.Caster.Data.Get<Vector2Value>(MovementDataKeys.LastMoveDirection, new Vector2Value(1f, 0f)).Normalized();
    }

    private static Vector2Value? ResolveTargetPosition(AbilityCastContext cast)
    {
        return cast.Targets != null && cast.Targets.Count > 0
            ? cast.Targets[0].Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero)
            : null;
    }

    private static float ResolveDashDistance(IEntity ability)
    {
        var maxDistance = ability.Data.Get<float>(MovementDataKeys.HandlerMaxDistance, -1f);
        return maxDistance > 0f
            ? maxDistance
            : Math.Max(0f, ability.Data.Get<float>(AbilityDataKeys.CastRange, 0f));
    }

    private static MovementParams BuildMovementParams(
        IEntity ability,
        Vector2Value direction,
        Vector2Value targetPosition,
        float maxDistance)
    {
        return new MovementParams
        {
            Mode = ability.Data.Get<MoveMode>(MovementDataKeys.HandlerMoveMode, MoveMode.Charge),
            Direction = direction,
            Speed = ability.Data.Get<float>(MovementDataKeys.MoveSpeed, 0f),
            MaxDistance = maxDistance,
            MaxDuration = ability.Data.Get<float>(MovementDataKeys.HandlerMaxTravelDuration, -1f),
            TargetPosition = targetPosition,
            StopAtTarget = true
        };
    }

    private static void ApplyMovementAuthoringData(IEntity caster, in MovementParams movementParams, IEntity ability)
    {
        caster.Data.Set(MovementDataKeys.HandlerMoveMode, movementParams.Mode);
        caster.Data.Set(MovementDataKeys.HandlerMaxDistance, movementParams.MaxDistance);
        caster.Data.Set(MovementDataKeys.HandlerMaxTravelDuration, movementParams.MaxDuration);
        caster.Data.Set(MovementDataKeys.MoveSpeed, ability.Data.Get<float>(MovementDataKeys.MoveSpeed, movementParams.Speed));
    }

    private static void SpawnDashEffect(AbilityCastContext cast, Vector2Value position)
    {
        var scenePath = cast.Ability.Data.Get(EffectDataKeys.ScenePath, string.Empty);
        if (string.IsNullOrWhiteSpace(scenePath))
        {
            return;
        }

        EffectTool.Spawn(new EffectSpawnOptions
        {
            Source = cast.Caster,
            Ability = cast.Ability,
            EntityId = $"{cast.Ability.EntityId}.dash-effect.{Guid.NewGuid():N}",
            ScenePath = scenePath,
            Name = cast.Ability.Data.Get(EffectDataKeys.Name, string.Empty),
            AnimationName = cast.Ability.Data.Get(EffectDataKeys.AnimationName, string.Empty),
            Position = position,
            Duration = cast.Ability.Data.Get<float>(EffectDataKeys.Duration, -1f)
        });
    }
}

/// <summary>
/// 从 DataOS 范围、伤害和特效参数执行范围伤害技能。
/// </summary>
public sealed class BrotatoLikeAreaDamageAbilityHandler : IFeatureHandler
{
    private readonly DamageType damageType;

    /// <inheritdoc />
    public string FeatureId { get; }

    /// <summary>
    /// 创建范围伤害 handler。
    /// </summary>
    /// <param name="featureId">完整 Feature handler Id。</param>
    /// <param name="damageType">伤害类型。</param>
    public BrotatoLikeAreaDamageAbilityHandler(string featureId, DamageType damageType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureId);
        FeatureId = featureId;
        this.damageType = damageType;
    }

    /// <inheritdoc />
    public object? OnExecute(FeatureContext context)
    {
        if (context.ActivationData is not AbilityCastContext cast)
        {
            return new AbilityExecutedResult();
        }

        var origin = ResolveImpactPosition(cast);
        var radius = cast.Ability.Data.Get<float>(AbilityDataKeys.EffectRadius, 0f);
        var targets = FindEnemiesInRadius(cast.Caster, origin, radius);
        var damageResult = ApplyAreaDamage(cast, targets);
        SpawnImpactEffect(cast, origin);

        return new AbilityExecutedResult
        {
            TargetsHit = damageResult.TargetCount,
            TotalDamage = SumImmediateDamage(damageResult),
            DamageResult = damageResult
        };
    }

    private static Vector2Value ResolveImpactPosition(AbilityCastContext cast)
    {
        return cast.TargetPosition
            ?? cast.Caster.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
    }

    private static List<IEntity> FindEnemiesInRadius(IEntity caster, Vector2Value origin, float radius)
    {
        var targets = new List<IEntity>();
        if (radius <= 0f)
        {
            return targets;
        }

        var casterTeam = caster.Data.Get<int>(CollisionDataKeys.Team, 0);
        var entities = EntityManager.GetAll();
        for (var i = 0; i < entities.Count; i++)
        {
            var candidate = entities[i];
            if (candidate.EntityId == caster.EntityId)
            {
                continue;
            }

            if (candidate.Data.Get<bool>(DamageDataKeys.IsDead, false)
                || candidate.Data.Get<float>(DamageDataKeys.CurrentHp, 0f) <= 0f)
            {
                continue;
            }

            var team = candidate.Data.Get<int>(CollisionDataKeys.Team, casterTeam);
            if (team == casterTeam)
            {
                continue;
            }

            var position = candidate.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
            if (Vector2Value.Distance(origin, position) <= radius)
            {
                targets.Add(candidate);
            }
        }

        return targets;
    }

    private DamageApplyResult ApplyAreaDamage(AbilityCastContext cast, IReadOnlyList<IEntity> targets)
    {
        var damage = cast.Ability.Data.Get<float>(AbilityDataKeys.Damage, 0f);
        if (damage <= 0f || targets.Count <= 0)
        {
            return new DamageApplyResult();
        }

        var options = new DamageApplyOptions(damage)
        {
            Attacker = cast.Caster,
            Type = damageType,
            Tags = DamageTags.Ability
        };
        var interval = cast.Ability.Data.Get<float>(AbilityDataKeys.DamageInterval, 0f);
        var repeatCount = cast.Ability.Data.Get<int>(AbilityDataKeys.DamageRepeatCount, 1);
        var immediate = cast.Ability.Data.Get<bool>(AbilityDataKeys.ApplyImmediateDamage, true);
        return interval > 0f && repeatCount != 1
            ? DamageTool.ApplyPeriodic(
                targets,
                options,
                new DamageRepeatOptions(interval)
                {
                    RepeatCount = repeatCount,
                    ApplyImmediately = immediate,
                    TimerTag = $"{cast.Ability.EntityId}.area-damage"
                })
            : DamageTool.Apply(targets, options);
    }

    private static void SpawnImpactEffect(AbilityCastContext cast, Vector2Value position)
    {
        var scenePath = cast.Ability.Data.Get(EffectDataKeys.ScenePath, string.Empty);
        if (string.IsNullOrWhiteSpace(scenePath))
        {
            return;
        }

        EffectTool.Spawn(new EffectSpawnOptions
        {
            Source = cast.Caster,
            Ability = cast.Ability,
            EntityId = $"{cast.Ability.EntityId}.area-effect.{Guid.NewGuid():N}",
            ScenePath = scenePath,
            Name = cast.Ability.Data.Get(EffectDataKeys.Name, string.Empty),
            AnimationName = cast.Ability.Data.Get(EffectDataKeys.AnimationName, string.Empty),
            Position = position,
            Duration = cast.Ability.Data.Get<float>(EffectDataKeys.Duration, -1f)
        });
    }

    private static float SumImmediateDamage(DamageApplyResult result)
    {
        var total = 0f;
        for (var i = 0; i < result.Results.Count; i++)
        {
            total += result.Results[i].Info.FinalDamage;
        }

        return total;
    }
}

/// <summary>
/// 从 DataOS 写入的 Ability / Projectile / Movement DataKey 组装投射物移动参数。
/// </summary>
public sealed class BrotatoLikeProjectileAbilityHandler : IFeatureHandler
{
    /// <inheritdoc />
    public string FeatureId { get; }

    /// <summary>
    /// 创建投射物 handler。
    /// </summary>
    /// <param name="featureId">完整 Feature handler Id。</param>
    public BrotatoLikeProjectileAbilityHandler(string featureId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureId);
        FeatureId = featureId;
    }

    /// <inheritdoc />
    public object? OnExecute(FeatureContext context)
    {
        if (context.ActivationData is not AbilityCastContext cast)
        {
            return new AbilityExecutedResult();
        }

        var startedCount = 0;
        var projectileCount = ResolveProjectileCount(cast.Ability);
        var movement = new MovementSystem();
        for (var i = 0; i < projectileCount; i++)
        {
            var projectile = SpawnProjectile(cast, i, projectileCount);
            if (!projectile.Created)
            {
                continue;
            }

            ApplyProjectileCollisionDefaults(projectile.Projectile, cast.Caster);
            var movementParams = BuildMovementParams(cast, i, projectileCount);
            ApplyMovementAuthoringData(projectile.Projectile, in movementParams, cast.Ability);
            if (movement.Start(projectile.Projectile, movementParams))
            {
                startedCount++;
            }
        }

        return new AbilityExecutedResult
        {
            TargetsHit = startedCount
        };
    }

    private static ProjectileSpawnResult SpawnProjectile(AbilityCastContext cast, int projectileIndex, int projectileCount)
    {
        var ability = cast.Ability;
        var spawnPosition = cast.Caster.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
        var targetPosition = cast.TargetPosition
            ?? ResolveTargetPosition(cast)
            ?? spawnPosition + new Vector2Value(1f, 0f);
        var direction = (targetPosition - spawnPosition).Normalized();
        if (direction == Vector2Value.Zero)
        {
            direction = new Vector2Value(1f, 0f);
        }

        return ProjectileTool.Spawn(new ProjectileSpawnOptions
        {
            Source = cast.Caster,
            Ability = ability,
            Target = cast.Targets != null && cast.Targets.Count > 0 ? cast.Targets[0] : null,
            EntityId = $"{ability.EntityId}.projectile.{projectileIndex}.{Guid.NewGuid():N}",
            ScenePath = ability.Data.Get(ProjectileDataKeys.ScenePath, string.Empty),
            SpawnPosition = ResolveSpawnPosition(ability, spawnPosition, direction, projectileIndex, projectileCount),
            TargetPosition = targetPosition,
            Direction = direction,
            Speed = ability.Data.Get<float>(ProjectileDataKeys.Speed, 0f),
            MaxHitCount = ability.Data.Get<int>(ProjectileDataKeys.MaxHitCount, 1),
            MaxLifeTime = ability.Data.Get<float>(ProjectileDataKeys.MaxLifeTime, -1f),
            Damage = ability.Data.Get<float>(ProjectileDataKeys.Damage, ability.Data.Get<float>(AbilityDataKeys.Damage, 0f)),
            DamageTags = DamageTags.Projectile | DamageTags.Ability
        });
    }

    private static MovementParams BuildMovementParams(AbilityCastContext cast, int projectileIndex, int projectileCount)
    {
        var ability = cast.Ability;
        var mode = ability.Data.Get<MoveMode>(MovementDataKeys.HandlerMoveMode, MoveMode.Charge);
        var speed = ability.Data.Get<float>(
            ProjectileDataKeys.Speed,
            ability.Data.Get<float>(MovementDataKeys.MoveSpeed, 0f));
        var direction = ResolveDirection(cast);
        var maxDuration = ability.Data.Get<float>(
            ProjectileDataKeys.MaxLifeTime,
            ability.Data.Get<float>(MovementDataKeys.HandlerMaxTravelDuration, -1f));
        var origin = cast.Caster.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
        var targetPosition = cast.TargetPosition ?? ResolveTargetPosition(cast) ?? (origin + (direction * ResolveDefaultTravelDistance(ability, speed)));
        var maxTravelDuration = ability.Data.Get<float>(MovementDataKeys.HandlerMaxTravelDuration, maxDuration);
        if (maxTravelDuration >= 0f)
        {
            maxDuration = maxTravelDuration;
        }
        maxDuration = ApplyMinTravelDuration(ability, maxDuration);

        return new MovementParams
        {
            Mode = mode,
            Direction = direction,
            Speed = speed,
            MaxDuration = maxDuration,
            MaxDistance = ability.Data.Get<float>(MovementDataKeys.HandlerMaxDistance, -1f),
            TargetPosition = ResolveMovementTargetPosition(mode, targetPosition),
            TargetEntityId = mode == MoveMode.Boomerang || mode == MoveMode.AttachToHost ? cast.Caster.EntityId : null,
            AttachOffset = ResolveAttachOffset(ability, direction, projectileIndex, projectileCount),
            WaveAmplitude = ability.Data.Get<float>(MovementDataKeys.WaveAmplitude, 50f),
            WaveFrequency = ability.Data.Get<float>(MovementDataKeys.WaveFrequency, 2f),
            WavePhase = ability.Data.Get<float>(MovementDataKeys.WavePhase, 0f),
            BoomerangPauseTime = ability.Data.Get<float>(MovementDataKeys.BoomerangPauseTime, 0f),
            BoomerangReturnSpeedMultiplier = ability.Data.Get<float>(MovementDataKeys.BoomerangReturnSpeedMultiplier, 1f),
            BoomerangArcHeight = ability.Data.Get<float>(MovementDataKeys.BoomerangArcHeight, 0f),
            BoomerangIsClockwise = ability.Data.Get<bool>(MovementDataKeys.BoomerangIsClockwise, false),
            OrbitCenter = origin,
            OrbitInitAngle = ResolveOrbitInitAngle(projectileIndex, projectileCount),
            OrbitRadius = ability.Data.Get<float>(MovementDataKeys.OrbitRadius, 0f),
            OrbitAngularSpeed = ability.Data.Get<float>(MovementDataKeys.OrbitAngularSpeed, 0f),
            OrbitAngularAcceleration = ability.Data.Get<float>(MovementDataKeys.OrbitAngularAcceleration, 0f),
            OrbitTotalAngle = ability.Data.Get<float>(MovementDataKeys.OrbitTotalAngle, -1f),
            IsOrbitClockwise = ability.Data.Get<bool>(MovementDataKeys.IsOrbitClockwise, true),
            BezierPoints = ResolveBezierPoints(ability, origin, targetPosition),
            BowWorldUp = ability.Data.Get<bool>(MovementDataKeys.BowWorldUp, false),
            ParabolaApexHeight = ability.Data.Get<float>(MovementDataKeys.ParabolaApexHeight, 0f),
            CircularArcRadius = ResolveCircularArcRadius(ability, origin, targetPosition),
            CircularArcClockwise = ability.Data.Get<bool>(MovementDataKeys.CircularArcClockwise, false),
            StopAtTarget = false,
            CollisionParams = new MovementCollisionParams
            {
                FilterPolicy = new CollisionFilterPolicy(IgnoreSameTeam: true),
                StopAfterCollisionCount = ability.Data.Get<int>(ProjectileDataKeys.MaxHitCount, 1),
                DestroyOnStop = true
            }
        };
    }

    private static Vector2Value ResolveDirection(AbilityCastContext cast)
    {
        var origin = cast.Caster.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
        var targetPosition = cast.TargetPosition ?? ResolveTargetPosition(cast);
        if (targetPosition.HasValue)
        {
            var direction = (targetPosition.Value - origin).Normalized();
            if (direction != Vector2Value.Zero)
            {
                return direction;
            }
        }

        return new Vector2Value(1f, 0f);
    }

    private static Vector2Value? ResolveTargetPosition(AbilityCastContext cast)
    {
        return cast.Targets != null && cast.Targets.Count > 0
            ? cast.Targets[0].Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero)
            : null;
    }

    private static void ApplyProjectileCollisionDefaults(IEntity projectile, IEntity caster)
    {
        projectile.Data.Set(CollisionDataKeys.CollisionLayer, CollisionLayers.Projectile);
        projectile.Data.Set(CollisionDataKeys.CollisionMask, CollisionLayers.EnemyHurtbox);
        projectile.Data.Set(CollisionDataKeys.CollisionRadius, 1f);
        projectile.Data.Set(CollisionDataKeys.Team, caster.Data.Get<int>(CollisionDataKeys.Team, 0));
    }

    private static void ApplyMovementAuthoringData(IEntity projectile, in MovementParams movementParams, IEntity ability)
    {
        projectile.Data.Set(MovementDataKeys.HandlerMoveMode, movementParams.Mode);
        projectile.Data.Set(MovementDataKeys.HandlerMaxDistance, movementParams.MaxDistance);
        projectile.Data.Set(MovementDataKeys.HandlerMinTravelDuration, ability.Data.Get<float>(MovementDataKeys.HandlerMinTravelDuration, 0f));
        projectile.Data.Set(MovementDataKeys.HandlerMaxTravelDuration, movementParams.MaxDuration);
        projectile.Data.Set(MovementDataKeys.HandlerProjectileCount, ability.Data.Get<int>(MovementDataKeys.HandlerProjectileCount, 1));
        projectile.Data.Set(MovementDataKeys.WaveAmplitude, movementParams.WaveAmplitude);
        projectile.Data.Set(MovementDataKeys.WaveFrequency, movementParams.WaveFrequency);
        projectile.Data.Set(MovementDataKeys.WavePhase, movementParams.WavePhase);
        projectile.Data.Set(MovementDataKeys.OrbitRadius, movementParams.OrbitRadius);
        projectile.Data.Set(MovementDataKeys.OrbitAngularSpeed, movementParams.OrbitAngularSpeed);
        projectile.Data.Set(MovementDataKeys.OrbitAngularAcceleration, movementParams.OrbitAngularAcceleration);
        projectile.Data.Set(MovementDataKeys.OrbitTotalAngle, movementParams.OrbitTotalAngle);
        projectile.Data.Set(MovementDataKeys.IsOrbitClockwise, movementParams.IsOrbitClockwise);
        projectile.Data.Set(MovementDataKeys.BoomerangArcHeight, movementParams.BoomerangArcHeight);
        projectile.Data.Set(MovementDataKeys.BoomerangPauseTime, movementParams.BoomerangPauseTime);
        projectile.Data.Set(MovementDataKeys.BoomerangReturnSpeedMultiplier, movementParams.BoomerangReturnSpeedMultiplier);
        projectile.Data.Set(MovementDataKeys.BoomerangIsClockwise, movementParams.BoomerangIsClockwise);
        projectile.Data.Set(MovementDataKeys.BezierDegree, ability.Data.Get<int>(MovementDataKeys.BezierDegree, 2));
        projectile.Data.Set(MovementDataKeys.BezierPattern, ability.Data.Get(MovementDataKeys.BezierPattern, string.Empty));
        projectile.Data.Set(MovementDataKeys.ParabolaApexHeight, movementParams.ParabolaApexHeight);
        projectile.Data.Set(MovementDataKeys.CircularArcRadiusScale, ability.Data.Get<float>(MovementDataKeys.CircularArcRadiusScale, 0f));
        projectile.Data.Set(MovementDataKeys.CircularArcRadiusMinOffset, ability.Data.Get<float>(MovementDataKeys.CircularArcRadiusMinOffset, 0f));
        projectile.Data.Set(MovementDataKeys.CircularArcClockwise, movementParams.CircularArcClockwise);
        projectile.Data.Set(MovementDataKeys.BowWorldUp, movementParams.BowWorldUp);
    }

    private static int ResolveProjectileCount(IEntity ability)
    {
        var mode = ability.Data.Get<MoveMode>(MovementDataKeys.HandlerMoveMode, MoveMode.Charge);
        if (mode != MoveMode.Orbit && mode != MoveMode.BezierCurve)
        {
            return 1;
        }

        return Math.Max(1, ability.Data.Get<int>(MovementDataKeys.HandlerProjectileCount, 1));
    }

    private static Vector2Value ResolveSpawnPosition(
        IEntity ability,
        Vector2Value origin,
        Vector2Value direction,
        int projectileIndex,
        int projectileCount)
    {
        var mode = ability.Data.Get<MoveMode>(MovementDataKeys.HandlerMoveMode, MoveMode.Charge);
        if (mode == MoveMode.AttachToHost)
        {
            return origin + ResolveAttachOffset(ability, direction, projectileIndex, projectileCount);
        }

        if (mode != MoveMode.Orbit)
        {
            return origin;
        }

        var angle = ResolveOrbitInitAngle(projectileIndex, projectileCount) ?? 0f;
        var radius = ability.Data.Get<float>(MovementDataKeys.OrbitRadius, 0f);
        return origin + (DirectionFromAngle(angle, direction) * radius);
    }

    private static Vector2Value ResolveAttachOffset(
        IEntity ability,
        Vector2Value direction,
        int projectileIndex,
        int projectileCount)
    {
        if (ability.Data.Get<MoveMode>(MovementDataKeys.HandlerMoveMode, MoveMode.Charge) != MoveMode.AttachToHost)
        {
            return Vector2Value.Zero;
        }

        var radius = ability.Data.Get<float>(MovementDataKeys.HandlerMaxDistance, 64f);
        if (radius <= 0f)
        {
            radius = 64f;
        }

        var angle = 360f * projectileIndex / Math.Max(1, projectileCount);
        return DirectionFromAngle(angle, direction) * radius;
    }

    private static Vector2Value? ResolveMovementTargetPosition(MoveMode mode, Vector2Value targetPosition)
    {
        return mode == MoveMode.BezierCurve || mode == MoveMode.Boomerang || mode == MoveMode.Parabola || mode == MoveMode.CircularArc
            ? targetPosition
            : null;
    }

    private static float ApplyMinTravelDuration(IEntity ability, float maxDuration)
    {
        var minDuration = ability.Data.Get<float>(MovementDataKeys.HandlerMinTravelDuration, 0f);
        if (minDuration <= 0f)
        {
            return maxDuration;
        }

        return maxDuration < 0f ? minDuration : Math.Max(maxDuration, minDuration);
    }

    private static float ResolveDefaultTravelDistance(IEntity ability, float speed)
    {
        var maxDistance = ability.Data.Get<float>(MovementDataKeys.HandlerMaxDistance, -1f);
        if (maxDistance > 0f)
        {
            return maxDistance;
        }

        var maxDuration = ability.Data.Get<float>(
            MovementDataKeys.HandlerMaxTravelDuration,
            ability.Data.Get<float>(ProjectileDataKeys.MaxLifeTime, -1f));
        if (speed > 0f && maxDuration > 0f)
        {
            return speed * maxDuration;
        }

        return 600f;
    }

    private static float? ResolveOrbitInitAngle(int projectileIndex, int projectileCount)
    {
        if (projectileCount <= 0)
        {
            return null;
        }

        return 360f * projectileIndex / projectileCount;
    }

    private static IReadOnlyList<Vector2Value>? ResolveBezierPoints(IEntity ability, Vector2Value origin, Vector2Value targetPosition)
    {
        if (ability.Data.Get<MoveMode>(MovementDataKeys.HandlerMoveMode, MoveMode.Charge) != MoveMode.BezierCurve)
        {
            return null;
        }

        var degree = Math.Max(2, ability.Data.Get<int>(MovementDataKeys.BezierDegree, 2));
        var points = new List<Vector2Value>(degree);
        var chord = targetPosition - origin;
        var normal = new Vector2Value(-chord.Y, chord.X).Normalized();
        var pattern = ability.Data.Get(MovementDataKeys.BezierPattern, string.Empty);
        for (var i = 1; i < degree; i++)
        {
            var t = i / (float)degree;
            var arch = MathF.Sin(t * MathF.PI) * MathF.Min(180f, MathF.Max(48f, chord.Length * 0.22f));
            var sideSign = string.Equals(pattern, "Converge", StringComparison.OrdinalIgnoreCase) ? (i % 2 == 0 ? -1f : 1f) : 1f;
            points.Add(origin + (chord * t) + (normal * arch * sideSign));
        }

        points.Add(targetPosition);
        return points;
    }

    private static float ResolveCircularArcRadius(IEntity ability, Vector2Value origin, Vector2Value targetPosition)
    {
        if (ability.Data.Get<MoveMode>(MovementDataKeys.HandlerMoveMode, MoveMode.Charge) != MoveMode.CircularArc)
        {
            return 0f;
        }

        var travelDistance = Vector2Value.Distance(origin, targetPosition);
        var scale = ability.Data.Get<float>(MovementDataKeys.CircularArcRadiusScale, 0f);
        var minOffset = ability.Data.Get<float>(MovementDataKeys.CircularArcRadiusMinOffset, 0f);
        return Math.Max(travelDistance * scale, (travelDistance * 0.5f) + minOffset);
    }

    private static Vector2Value DirectionFromAngle(float degrees, Vector2Value fallbackDirection)
    {
        var radians = degrees * (MathF.PI / 180f);
        var direction = new Vector2Value(MathF.Cos(radians), MathF.Sin(radians));
        return direction == Vector2Value.Zero ? fallbackDirection : direction;
    }
}

/// <summary>
/// 从 DataOS 链式技能参数执行连锁闪电伤害。
/// </summary>
public sealed class BrotatoLikeChainLightningHandler : IFeatureHandler
{
    /// <inheritdoc />
    public string FeatureId { get; }

    /// <summary>
    /// 创建连锁闪电 handler。
    /// </summary>
    /// <param name="featureId">完整 Feature handler Id。</param>
    public BrotatoLikeChainLightningHandler(string featureId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureId);
        FeatureId = featureId;
    }

    /// <inheritdoc />
    public object? OnExecute(FeatureContext context)
    {
        if (context.ActivationData is not AbilityCastContext cast)
        {
            return new AbilityExecutedResult();
        }

        var firstTarget = ResolveInitialTarget(cast);
        if (firstTarget == null)
        {
            return new AbilityExecutedResult();
        }

        var chainCount = Math.Max(1, cast.Ability.Data.Get<int>(AbilityDataKeys.ChainCount, 1));
        var damage = cast.Ability.Data.Get<float>(AbilityDataKeys.Damage, 0f);
        var hitTargets = new HashSet<string>(StringComparer.Ordinal);
        ExecuteBounce(cast, cast.Caster, firstTarget, damage, chainCount, hitTargets);

        return new AbilityExecutedResult
        {
            TargetsHit = 1
        };
    }

    private static IEntity? ResolveInitialTarget(AbilityCastContext cast)
    {
        if (cast.Targets != null && cast.Targets.Count > 0)
        {
            return cast.Targets[0];
        }

        var origin = cast.Caster.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
        var range = cast.Ability.Data.Get<float>(AbilityDataKeys.CastRange, cast.Ability.Data.Get<float>(AbilityDataKeys.AutoTargetRange, 0f));
        return FindNearestEnemy(cast.Caster, origin, range, []);
    }

    private static void ExecuteBounce(
        AbilityCastContext cast,
        IEntity fromEntity,
        IEntity currentTarget,
        float currentDamage,
        int remainingBounces,
        HashSet<string> hitTargets)
    {
        ApplyBounceDamage(cast, currentTarget, currentDamage);
        SpawnLineEffect(cast, fromEntity, currentTarget);
        hitTargets.Add(currentTarget.EntityId);

        if (remainingBounces <= 1)
        {
            return;
        }

        var delay = Math.Max(0f, cast.Ability.Data.Get<float>(AbilityDataKeys.ChainDelay, 0f));
        var decay = cast.Ability.Data.Get<float>(AbilityDataKeys.ChainDamageDecay, 100f) / 100f;
        TimerManager.Instance.Delay(delay).OnComplete(() =>
        {
            var origin = currentTarget.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
            var range = cast.Ability.Data.Get<float>(AbilityDataKeys.ChainRange, 0f);
            var nextTarget = FindNearestEnemy(cast.Caster, origin, range, hitTargets);
            if (nextTarget == null)
            {
                return;
            }

            ExecuteBounce(cast, currentTarget, nextTarget, currentDamage * decay, remainingBounces - 1, hitTargets);
        });
    }

    private static void ApplyBounceDamage(AbilityCastContext cast, IEntity target, float damage)
    {
        DamageTool.Apply([target], new DamageApplyOptions(damage)
        {
            Attacker = cast.Caster,
            Type = DamageType.Magical,
            Tags = DamageTags.Ability
        });
    }

    private static void SpawnLineEffect(AbilityCastContext cast, IEntity fromEntity, IEntity target)
    {
        var scenePath = cast.Ability.Data.Get(AbilityDataKeys.LineEffectScenePath, string.Empty);
        if (string.IsNullOrWhiteSpace(scenePath))
        {
            return;
        }

        EffectTool.Spawn(new EffectSpawnOptions
        {
            Source = fromEntity,
            Ability = cast.Ability,
            Target = target,
            EntityId = $"{cast.Ability.EntityId}.chain-effect.{Guid.NewGuid():N}",
            ScenePath = scenePath,
            Position = target.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero),
            Duration = -1f
        });
    }

    private static IEntity? FindNearestEnemy(IEntity caster, Vector2Value origin, float range, HashSet<string> excludedIds)
    {
        if (range <= 0f)
        {
            return null;
        }

        var casterTeam = caster.Data.Get<int>(CollisionDataKeys.Team, 0);
        IEntity? nearest = null;
        var nearestDistance = float.MaxValue;
        var entities = EntityManager.GetAll();
        for (var i = 0; i < entities.Count; i++)
        {
            var candidate = entities[i];
            if (candidate.EntityId == caster.EntityId || excludedIds.Contains(candidate.EntityId))
            {
                continue;
            }

            if (candidate.Data.Get<bool>(DamageDataKeys.IsDead, false)
                || candidate.Data.Get<float>(DamageDataKeys.CurrentHp, 0f) <= 0f)
            {
                continue;
            }

            var team = candidate.Data.Get<int>(CollisionDataKeys.Team, casterTeam);
            if (team == casterTeam)
            {
                continue;
            }

            var position = candidate.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
            var distance = Vector2Value.Distance(origin, position);
            if (distance > range || distance >= nearestDistance)
            {
                continue;
            }

            nearest = candidate;
            nearestDistance = distance;
        }

        return nearest;
    }
}
