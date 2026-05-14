using SlimeAI.GameOS.Capabilities.Collision;
using SlimeAI.GameOS.Capabilities.Collision.Events;
using SlimeAI.GameOS.Capabilities.Movement;
using SlimeAI.GameOS.Capabilities.Movement.Events;
using SlimeAI.GameOS.Runtime;
using SlimeAI.GameOS.Runtime.Data;
using SlimeAI.GameOS.Runtime.Entity;
using SlimeAI.GameOS.Runtime.Events.Core;
using SlimeAI.GameOS.Runtime.Pool;
using SlimeAI.GameOS.Runtime.Relationship;
using SlimeAI.GameOS.Runtime.Resource;
using SlimeAI.GameOS.Runtime.Schedule;
using SlimeAI.GameOS.Runtime.Timer;

namespace BrotatoLike.Game;

/// <summary>
/// 游戏仓库消费 SlimeAI.GameOS Runtime API 的最小验证入口。
/// </summary>
public static class GameBootstrap
{
    private static readonly DataKey<int> SmokeValueKey = DataKey.Create<int>("BrotatoLike.SmokeValue", 0);

    /// <summary>
    /// 当前游戏构建使用的框架包 Id。
    /// </summary>
    public static string FrameworkPackageId => GameOSInfo.FrameworkId;

    /// <summary>
    /// 当前游戏构建使用的框架版本。
    /// </summary>
    public static string FrameworkVersion => GameOSInfo.Version;

    /// <summary>
    /// 运行不触碰游戏资产的框架 smoke probe。
    /// </summary>
    public static FrameworkSmokeProbe RunFrameworkSmokeProbe()
    {
        EntityManager.Clear();
        var entity = EntityManager.Spawn(new EntitySpawnConfig { EntityId = new EntityId("brotato-like-smoke") });
        var child = EntityManager.Spawn(new EntitySpawnConfig
        {
            EntityId = new EntityId("brotato-like-smoke-child"),
            ParentEntityId = entity.EntityId,
            AutoAddParentRelation = true,
            ParentDestroyPolicy = ParentDestroyPolicy.Detach
        });

        var dataEvents = 0;
        entity.Events.Subscribe<DataPropertyChanged>(data =>
        {
            if (data.Change.StableKey == SmokeValueKey.StableKey)
            {
                dataEvents++;
            }
        });
        entity.Data.Set(SmokeValueKey, 1);

        var pool = new ObjectPool<SmokeToken>(
            static () => new SmokeToken(),
            new ObjectPoolConfig
            {
                Name = "brotato-like-smoke-pool",
                InitialSize = 1,
                MaxSize = 2
            });
        var token = pool.Get();
        pool.Release(token);
        var poolStats = pool.GetStats();
        pool.Destroy();

        var timerManager = new TimerManager("brotato-like-smoke-timers");
        var timerCompleted = false;
        timerManager.Delay(0.1f).OnComplete(() => timerCompleted = true);
        timerManager.Tick(0.1f);
        timerManager.Clear();

        entity.Data.Set(MovementDataKeys.Position, Vector2Value.Zero);
        var movementSystem = new MovementSystem();
        var movementStopped = false;
        entity.Events.Subscribe<Stopped>(_ => movementStopped = true);
        movementSystem.Start(entity, new MovementParams
        {
            Mode = MoveMode.Charge,
            TargetPosition = new Vector2Value(12f, 0f),
            Speed = 24f,
            ReachDistance = 0f
        });
        movementSystem.Tick(0.5f);
        var movementPosition = entity.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);

        entity.Data.Set(CollisionDataKeys.CollisionLayer, CollisionLayers.Projectile);
        entity.Data.Set(CollisionDataKeys.CollisionMask, CollisionLayers.EnemyHurtbox);
        var collisionTarget = EntityManager.Spawn(new EntitySpawnConfig { EntityId = new EntityId("brotato-like-smoke-collision-target") });
        collisionTarget.Data.Set(CollisionDataKeys.CollisionLayer, CollisionLayers.EnemyHurtbox);
        var collisionEntered = false;
        entity.Events.Subscribe<Entered>(data => collisionEntered = data.Contact.Target.EntityId == collisionTarget.EntityId);
        var collisionSystem = new CollisionSystem();
        collisionSystem.EmitEntered(entity, collisionTarget);

        var movingProjectile = EntityManager.Spawn(new EntitySpawnConfig { EntityId = new EntityId("brotato-like-smoke-moving-projectile") });
        movingProjectile.Data.Set(MovementDataKeys.Position, Vector2Value.Zero);
        movingProjectile.Data.Set(CollisionDataKeys.CollisionLayer, CollisionLayers.Projectile);
        movingProjectile.Data.Set(CollisionDataKeys.CollisionMask, CollisionLayers.EnemyHurtbox);
        movingProjectile.Data.Set(CollisionDataKeys.CollisionRadius, 1f);
        collisionTarget.Data.Set(MovementDataKeys.Position, new Vector2Value(10f, 0f));
        collisionTarget.Data.Set(CollisionDataKeys.CollisionRadius, 1f);

        var movementCollision = false;
        movingProjectile.Events.Subscribe<Collision>(data => movementCollision = data.Context.Target.EntityId == collisionTarget.EntityId);
        movementSystem.Start(movingProjectile, new MovementParams
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
        movementSystem.Tick(1f);
        var movementCollisionPosition = movingProjectile.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);

        var schedule = new RuntimeSchedule();
        schedule.Register(
            new SystemDescriptor("brotato-like-smoke-system", static () => new SmokeSystem()),
            new SystemConfig
            {
                SystemId = "brotato-like-smoke-system",
                Group = SystemGroup.Gameplay,
                Tags = SystemTag.Gameplay | SystemTag.Runtime,
                RunCondition = SystemRunCondition.GameplayRunning()
            });
        schedule.Bootstrap();
        schedule.ProjectState.BeginGameplaySession();
        var scheduleRunning = schedule.GetRuntimeInfo()[0].IsRunning;
        schedule.Clear();

        ResourceCatalog.Clear();
        ResourceCatalog.Register("Main", ResourceCategory.Entity, "res://Scenes/Main.tscn");
        var mainScenePath = ResourceManagement.GetPath("Main", ResourceCategory.Entity) ?? string.Empty;
        var relationshipBound = RelationshipManager.HasRelationship(entity.EntityId.Value, child.EntityId.Value, RelationshipType.Parent);

        EntityManager.Clear();

        return new FrameworkSmokeProbe(
            EntityId: entity.EntityId.Value,
            DataEventCount: dataEvents,
            RelationshipBound: relationshipBound,
            PoolCreated: poolStats.TotalCreated,
            TimerCompleted: timerCompleted,
            ScheduleRunning: scheduleRunning,
            MainScenePath: mainScenePath,
            MovementCompleted: movementStopped,
            MovementPositionX: movementPosition.X,
            CollisionEntered: collisionEntered,
            MovementCollision: movementCollision && System.Math.Abs(movementCollisionPosition.X - 8f) < 0.001f);
    }
}

/// <summary>
/// 最小框架 smoke probe 结果。
/// </summary>
public readonly record struct FrameworkSmokeProbe(
    string EntityId,
    int DataEventCount,
    bool RelationshipBound,
    int PoolCreated,
    bool TimerCompleted,
    bool ScheduleRunning,
    string MainScenePath,
    bool MovementCompleted,
    float MovementPositionX,
    bool CollisionEntered,
    bool MovementCollision);

internal sealed class SmokeToken
{
}

internal sealed class SmokeSystem : IRuntimeSystem
{
}
