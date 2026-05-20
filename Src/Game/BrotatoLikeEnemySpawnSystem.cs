using System;
using System.Collections.Generic;
using Godot;
using SlimeAI.GameOS.Capabilities.Movement;
using SlimeAI.GameOS.Capabilities.Unit;
using SlimeAI.GameOS.GodotBridge;
using SlimeAI.GameOS.Runtime.Entity;
using SlimeAI.GameOS.Runtime.Schedule;

namespace BrotatoLike.Game;

/// <summary>
/// BrotatoLike 游戏侧敌人生成入口，消费 DataOS 生成的 Spawn catalog。
/// </summary>
public sealed class BrotatoLikeEnemySpawnSystem
{
    private readonly List<RuleState> ruleStates = new();

    private BrotatoLikeDataOSBootstrap? bootstrap;
    private BrotatoLikeSpawnCatalog? catalog;
    private Node? activeParent;
    private GodotMovementDriver? movementDriver;
    private double elapsedSeconds;
    private int totalSpawned;

    /// <summary>
    /// 已生成敌人总数。
    /// </summary>
    public int TotalSpawned => totalSpawned;

    /// <summary>
    /// 配置本生成系统。后续 Runtime Schedule 可以直接驱动 <see cref="Tick"/>。
    /// </summary>
    /// <param name="bootstrap">DataOS bootstrap 入口。</param>
    /// <param name="spawnCatalog">当前波次敌人生成规则目录。</param>
    /// <param name="parent">实例化敌人的 Godot 父节点。</param>
    public void Configure(
        BrotatoLikeDataOSBootstrap bootstrap,
        BrotatoLikeSpawnCatalog spawnCatalog,
        Node parent,
        GodotMovementDriver? sharedMovementDriver = null)
    {
        ArgumentNullException.ThrowIfNull(bootstrap);
        ArgumentNullException.ThrowIfNull(spawnCatalog);
        ArgumentNullException.ThrowIfNull(parent);

        this.bootstrap = bootstrap;
        catalog = spawnCatalog;
        activeParent = parent;
        movementDriver = sharedMovementDriver;
        elapsedSeconds = 0d;
        totalSpawned = 0;
        ruleStates.Clear();

        for (var i = 0; i < spawnCatalog.EnemyRules.Count; i++)
        {
            var rule = spawnCatalog.EnemyRules[i];
            ruleStates.Add(new RuleState(rule, Math.Max(0f, rule.StartDelay)));
        }
    }

    /// <summary>
    /// 推进生成系统。当前切片使用规则中的 SingleCount；SingleVariance 预留给后续确定性随机策略。
    /// </summary>
    /// <param name="deltaSeconds">本次推进秒数。</param>
    public BrotatoLikeSpawnTickResult Tick(double deltaSeconds)
    {
        if (bootstrap == null || catalog == null || activeParent == null || deltaSeconds < 0d)
        {
            return new BrotatoLikeSpawnTickResult(0, totalSpawned);
        }

        elapsedSeconds += deltaSeconds;
        var spawnedThisTick = 0;
        for (var i = 0; i < ruleStates.Count; i++)
        {
            var state = ruleStates[i];
            if (state.IsExhausted || elapsedSeconds < state.NextSpawnTime)
            {
                continue;
            }

            spawnedThisTick += SpawnRuleBatch(state);
            state.NextSpawnTime += Math.Max(0.001f, state.Rule.Interval);
        }

        return new BrotatoLikeSpawnTickResult(spawnedThisTick, totalSpawned);
    }

    /// <summary>
    /// 直接按规则生成一个敌人节点，供测试、调试面板和后续 SpawnSystem 复用。
    /// </summary>
    /// <param name="rule">DataOS 敌人生成规则。</param>
    /// <param name="position">生成位置。</param>
    /// <param name="entityId">稳定 Runtime EntityId。</param>
    public GodotEntity2D SpawnEnemy(BrotatoLikeSpawnRule rule, Vector2 position, string entityId)
    {
        if (bootstrap == null || activeParent == null)
        {
            throw new InvalidOperationException("BrotatoLikeEnemySpawnSystem must be configured before spawning enemies.");
        }

        ArgumentNullException.ThrowIfNull(rule);
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("EntityId is required.", nameof(entityId));
        }

        var entity = new GodotEntity2D
        {
            Name = $"Enemy_{rule.RecordId}_{totalSpawned + 1}",
            EntityIdOverride = entityId,
            Position = position
        };
        bootstrap.ApplyRecordToData(rule.TableId, rule.RecordId, entity.Data);
        entity.Data.Set(MovementDataKeys.Position, new Vector2Value(position.X, position.Y));

        if (!entity.Data.Has(UnitDataKeys.VisualScenePath))
        {
            entity.Data.Set(UnitDataKeys.VisualScenePath, rule.VisualScenePath);
        }

        var composition = GodotUnitComposer.Compose(entity, BrotatoLikeUnitProfiles.EnemyMelee);
        if (!composition.Success)
        {
            throw new InvalidOperationException(composition.FailureReason);
        }

        activeParent.AddChild(entity);
        movementDriver?.MovementSystem.Start(entity, new MovementParams
        {
            Mode = MoveMode.AIControlled,
            MaxDuration = -1f
        });

        return entity;
    }

    private int SpawnRuleBatch(RuleState state)
    {
        var spawned = 0;
        var count = Math.Max(0, state.Rule.SingleCount);
        for (var i = 0; i < count; i++)
        {
            if (state.IsExhausted)
            {
                break;
            }

            var positionIndex = state.SpawnedCountInWave;
            var entityId = $"spawn-{state.Rule.RecordId}-{totalSpawned + 1}";
            SpawnEnemy(state.Rule, ResolvePosition(state.Rule, positionIndex), entityId);
            state.SpawnedCountInWave++;
            totalSpawned++;
            spawned++;
        }

        return spawned;
    }

    private Vector2 ResolvePosition(BrotatoLikeSpawnRule rule, int index)
    {
        return string.Equals(rule.PositionStrategy, "Circle", StringComparison.OrdinalIgnoreCase)
            ? ResolveCirclePosition(index)
            : ResolveRectanglePosition(index);
    }

    private static Vector2 ResolveCirclePosition(int index)
    {
        var angle = index * Mathf.Tau / 8f;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 240f;
    }

    private static Vector2 ResolveRectanglePosition(int index)
    {
        var side = index % 4;
        var offset = (index / 4) * 48f;
        return side switch
        {
            0 => new Vector2(-320f + offset, -180f),
            1 => new Vector2(320f, -180f + offset),
            2 => new Vector2(320f - offset, 180f),
            _ => new Vector2(-320f, 180f - offset)
        };
    }

    private sealed class RuleState
    {
        public RuleState(BrotatoLikeSpawnRule rule, double nextSpawnTime)
        {
            Rule = rule;
            NextSpawnTime = nextSpawnTime;
        }

        public BrotatoLikeSpawnRule Rule { get; }

        public double NextSpawnTime { get; set; }

        public int SpawnedCountInWave { get; set; }

        public bool IsExhausted => Rule.MaxCountPerWave >= 0 && SpawnedCountInWave >= Rule.MaxCountPerWave;
    }
}

/// <summary>
/// BrotatoLike 敌人生成 Tick 结果。
/// </summary>
public readonly record struct BrotatoLikeSpawnTickResult(int SpawnedThisTick, int TotalSpawned);

/// <summary>
/// RuntimeSchedule 命令式 Tick 请求。
/// </summary>
public readonly record struct BrotatoLikeSpawnTickRequest(double DeltaSeconds);

/// <summary>
/// 让 RuntimeSchedule 托管 BrotatoLike 敌人生成 Tick 的适配系统。
/// </summary>
public sealed class BrotatoLikeScheduledEnemySpawnSystem :
    IRuntimeSystem,
    IRuntimeCommandHandler<BrotatoLikeSpawnTickRequest, BrotatoLikeSpawnTickResult>
{
    private readonly BrotatoLikeEnemySpawnSystem innerSystem;

    /// <summary>
    /// 创建调度适配系统。
    /// </summary>
    /// <param name="bootstrap">DataOS bootstrap 入口。</param>
    /// <param name="spawnCatalog">当前波次敌人生成规则目录。</param>
    /// <param name="parent">实例化敌人的 Godot 父节点。</param>
    public BrotatoLikeScheduledEnemySpawnSystem(
        BrotatoLikeDataOSBootstrap bootstrap,
        BrotatoLikeSpawnCatalog spawnCatalog,
        Node parent,
        GodotMovementDriver? sharedMovementDriver = null)
    {
        innerSystem = new BrotatoLikeEnemySpawnSystem();
        innerSystem.Configure(bootstrap, spawnCatalog, parent, sharedMovementDriver);
    }

    /// <summary>
    /// 当前内部生成系统已生成敌人总数。
    /// </summary>
    public int TotalSpawned => innerSystem.TotalSpawned;

    /// <inheritdoc />
    public BrotatoLikeSpawnTickResult Execute(BrotatoLikeSpawnTickRequest request)
    {
        return innerSystem.Tick(request.DeltaSeconds);
    }
}
