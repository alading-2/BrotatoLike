using System;
using System.Collections.Generic;
using Godot;
using SkilmeAI.GameOS.Capabilities.Ability;
using SkilmeAI.GameOS.Capabilities.Movement;
using SkilmeAI.GameOS.Capabilities.Unit;
using SkilmeAI.GameOS.GodotBridge;
using SkilmeAI.GameOS.Runtime.Entity;
using SkilmeAI.GameOS.Runtime.Resource;
using SkilmeAI.GameOS.Runtime.Schedule;

namespace BrotatoLike.Game;

/// <summary>
/// BrotatoLike 主运行时入口，负责装载 DataOS 并托管 RuntimeSchedule。
/// </summary>
public partial class BrotatoLikeGameRuntime : Node
{
    private RuntimeSchedule? schedule;
    private BrotatoLikeDataOSBootstrap? bootstrap;
    private BrotatoLikeSpawnCatalog? spawnCatalog;
    private SystemConfig? spawnScheduleConfig;
    private GodotMovementDriver? movementDriver;
    private GodotEntity2D? playerEntity;

    /// <summary>
    /// 进入场景树后是否自动初始化 DataOS。
    /// </summary>
    [Export]
    public bool AutoInitialize { get; set; } = true;

    /// <summary>
    /// 初始化后是否自动进入 Gameplay 状态。
    /// </summary>
    [Export]
    public bool AutoStartGameplay { get; set; } = true;

    /// <summary>
    /// 是否在 _Process 中驱动 Spawn Tick。
    /// </summary>
    [Export]
    public bool AutoTick { get; set; } = true;

    /// <summary>
    /// 初始波次，1 起始。
    /// </summary>
    [Export]
    public int InitialWave { get; set; } = 1;

    /// <summary>
    /// 敌人实例化父节点路径；为空时挂到本节点。
    /// </summary>
    [Export]
    public NodePath? EnemyParentPath { get; set; }

    /// <summary>
    /// 当前是否已完成初始化。
    /// </summary>
    public bool IsInitialized => schedule != null;

    /// <summary>
    /// DataOS 生成的 SpawnSystem 调度配置。
    /// </summary>
    public SystemConfig? SpawnScheduleConfig => spawnScheduleConfig;

    /// <summary>
    /// 当前波次 Spawn catalog。
    /// </summary>
    public BrotatoLikeSpawnCatalog? SpawnCatalog => spawnCatalog;

    /// <summary>
    /// 当前玩家 Godot Entity。
    /// </summary>
    public GodotEntity2D? PlayerEntity => playerEntity;

    /// <summary>
    /// 当前 Movement Driver。
    /// </summary>
    public GodotMovementDriver? MovementDriver => movementDriver;

    /// <summary>
    /// 最近一次 Tick 结果。
    /// </summary>
    public SystemExecuteResult<BrotatoLikeSpawnTickResult> LastSpawnTickResult { get; private set; }

    /// <inheritdoc />
    public override void _Ready()
    {
        if (!AutoInitialize)
        {
            return;
        }

        InitializeFromDataOS(InitialWave, ResolveEnemyParent());
        if (AutoStartGameplay)
        {
            BeginGameplay();
        }
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        if (AutoTick && IsInitialized)
        {
            LastSpawnTickResult = TickSpawn(delta);
        }
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        Shutdown();
    }

    /// <summary>
    /// 从默认 DataOS snapshot 初始化游戏运行时。
    /// </summary>
    /// <param name="wave">当前波次。</param>
    /// <param name="enemyParent">敌人节点挂载父节点。</param>
    public void InitializeFromDataOS(int wave = 1, Node? enemyParent = null)
    {
        Initialize(BrotatoLikeDataOSBootstrap.LoadFromResource(), wave, enemyParent);
    }

    /// <summary>
    /// 使用已有 DataOS bootstrap 初始化游戏运行时。
    /// </summary>
    /// <param name="dataBootstrap">DataOS bootstrap。</param>
    /// <param name="wave">当前波次。</param>
    /// <param name="enemyParent">敌人节点挂载父节点。</param>
    public void Initialize(BrotatoLikeDataOSBootstrap dataBootstrap, int wave = 1, Node? enemyParent = null)
    {
        ArgumentNullException.ThrowIfNull(dataBootstrap);

        Shutdown();
        bootstrap = dataBootstrap;
        bootstrap.RegisterResources();
        spawnCatalog = bootstrap.BuildEnemySpawnCatalog(wave);
        spawnScheduleConfig = bootstrap.BuildSpawnSystemScheduleConfig();
        schedule = new RuntimeSchedule();
        var parent = enemyParent ?? this;
        schedule.Register(
            new SystemDescriptor(
                spawnScheduleConfig.SystemId,
                () => new BrotatoLikeScheduledEnemySpawnSystem(bootstrap, spawnCatalog, parent)),
            spawnScheduleConfig);
        schedule.Bootstrap();
    }

    /// <summary>
    /// 进入 Gameplay 状态。
    /// </summary>
    public void BeginGameplay()
    {
        schedule?.ProjectState.BeginGameplaySession();
    }

    /// <summary>
    /// 打开暂停菜单并暂停模拟。
    /// </summary>
    public void OpenPauseMenu()
    {
        schedule?.ProjectState.OpenPauseMenu();
    }

    /// <summary>
    /// 关闭暂停菜单并恢复模拟。
    /// </summary>
    public void ClosePauseMenu()
    {
        schedule?.ProjectState.ClosePauseMenu();
    }

    /// <summary>
    /// 通过 RuntimeSchedule 门禁推进 Spawn Tick。
    /// </summary>
    /// <param name="deltaSeconds">本次推进秒数。</param>
    public SystemExecuteResult<BrotatoLikeSpawnTickResult> TickSpawn(double deltaSeconds)
    {
        if (schedule == null)
        {
            return SystemExecuteResult<BrotatoLikeSpawnTickResult>.Blocked("BrotatoLikeGameRuntime 尚未初始化");
        }

        return schedule.Execute<
            BrotatoLikeScheduledEnemySpawnSystem,
            BrotatoLikeSpawnTickRequest,
            BrotatoLikeSpawnTickResult>(new BrotatoLikeSpawnTickRequest(deltaSeconds));
    }

    /// <summary>
    /// 获取 SpawnSystem 当前运行时信息。
    /// </summary>
    public SystemRuntimeInfo? GetSpawnSystemRuntimeInfo()
    {
        if (schedule == null)
        {
            return null;
        }

        var systems = schedule.GetRuntimeInfo();
        for (var i = 0; i < systems.Count; i++)
        {
            if (systems[i].SystemId == spawnScheduleConfig?.SystemId)
            {
                return systems[i];
            }
        }

        return null;
    }

    /// <summary>
    /// 从 DataOS 生成玩家角色并挂载输入组件和 Movement。
    /// </summary>
    /// <param name="recordId">DataOS unit.player 记录 Id，默认 deluyi。</param>
    /// <param name="spawnPosition">玩家生成位置。</param>
    /// <returns>生成的玩家 GodotEntity2D。</returns>
    public GodotEntity2D SpawnPlayer(string recordId = "deluyi", Vector2? spawnPosition = null)
    {
        if (bootstrap == null)
        {
            throw new InvalidOperationException("BrotatoLikeGameRuntime 尚未初始化，无法生成玩家。");
        }

        // 清理旧玩家
        if (playerEntity != null)
        {
            playerEntity.DestroyEntity();
            playerEntity = null;
        }

        // 创建共享 MovementDriver（若不存在）
        if (movementDriver == null)
        {
            movementDriver = new GodotMovementDriver { Name = "MovementDriver" };
            AddChild(movementDriver);
        }

        var position = spawnPosition ?? Vector2.Zero;
        var entity = new GodotEntity2D
        {
            Name = $"Player_{recordId}",
            EntityIdOverride = $"player-{recordId}",
            Position = position
        };

        // 写入 DataOS 玩家数据
        bootstrap.ApplyRecordToData("unit.player", recordId, entity.Data);
        entity.Data.Set(MovementDataKeys.Position, new Vector2Value(position.X, position.Y));

        // 挂载输入组件
        var inputComponent = new GodotPlayerInputComponent { Name = "PlayerInput" };
        entity.AddChild(inputComponent);

        // 挂载主动技能输入组件
        var skillInputComponent = new GodotActiveSkillInputComponent { Name = "ActiveSkillInput" };
        entity.AddChild(skillInputComponent);

        // 从 DataOS 创建初始技能实体
        var ownedAbilityIds = new List<string>();
        SpawnPlayerAbility(entity, "slam", ownedAbilityIds);
        SpawnPlayerAbility(entity, "chain_lightning", ownedAbilityIds);

        entity.Data.Set(AbilityDataKeys.OwnedAbilityIds, ownedAbilityIds);
        entity.Data.Set(AbilityDataKeys.CurrentAbilityIndex, 0);

        // 加载视觉场景
        var visualPath = entity.Data.Get(UnitDataKeys.VisualScenePath, string.Empty);
        if (!string.IsNullOrEmpty(visualPath))
        {
            var visualScene = ResourceManagement.LoadPath<PackedScene>(visualPath);
            if (visualScene != null)
            {
                var visual = visualScene.Instantiate();
                visual.Name = "VisualRoot";
                entity.AddChild(visual);
            }
        }

        AddChild(entity);
        playerEntity = entity;

        // 启动 PlayerInput 移动
        movementDriver.MovementSystem.Start(entity, new MovementParams
        {
            Mode = MoveMode.PlayerInput,
            MaxDuration = -1f // 不限制时长，持续响应输入
        });

        return entity;
    }

    private void SpawnPlayerAbility(GodotEntity2D player, string abilityRecordId, List<string> ownedIds)
    {
        var abilityEntityId = $"ability-{abilityRecordId}-{player.EntityId}";
        var ability = bootstrap!.SpawnEntityFromRecord("ability", abilityRecordId, abilityEntityId);
        ownedIds.Add(ability.EntityId);
    }

    /// <summary>
    /// 清理当前 RuntimeSchedule 和玩家。
    /// </summary>
    public void Shutdown()
    {
        if (playerEntity != null)
        {
            playerEntity.DestroyEntity();
            playerEntity = null;
        }

        if (movementDriver != null)
        {
            movementDriver.QueueFree();
            movementDriver = null;
        }

        schedule?.Clear();
        schedule = null;
        bootstrap = null;
        spawnCatalog = null;
        spawnScheduleConfig = null;
        LastSpawnTickResult = default;
    }

    private Node ResolveEnemyParent()
    {
        if (EnemyParentPath == null || EnemyParentPath.IsEmpty)
        {
            return this;
        }

        return GetNodeOrNull<Node>(EnemyParentPath) ?? this;
    }
}
