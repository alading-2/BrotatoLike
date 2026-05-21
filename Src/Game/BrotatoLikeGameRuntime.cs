using System;
using BrotatoLike.Game.Bridge;
using BrotatoLike.Game.Progression;
using BrotatoLike.Game.UI;
using Godot;
using SlimeAI.GameOS.Capabilities.Ability;
using SlimeAI.GameOS.Capabilities.Damage;
using SlimeAI.GameOS.Capabilities.Movement;
using SlimeAI.GameOS.Capabilities.Unit;
using SlimeAI.GameOS.GodotBridge;
using SlimeAI.GameOS.Runtime.Entity;
using SlimeAI.GameOS.Runtime.Schedule;
using SlimeAI.GameOS.Runtime.World;

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
    private GameOSTimerDriver? timerDriver;
    private GodotEntity2D? playerEntity;
    private BrotatoLikeHud? hud;
    private BrotatoLikeTargetingController? targetingController;
    private BrotatoLikeProgressionService? progressionService;
    private Camera2D? playerCamera;
    private float deathElapsedSeconds;
    private const float RespawnDelaySeconds = 2f;

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
    /// 正式 HUD。
    /// </summary>
    public BrotatoLikeHud? Hud => hud;

    /// <summary>
    /// 点选技能控制器。
    /// </summary>
    public BrotatoLikeTargetingController? TargetingController => targetingController;

    /// <summary>
    /// 游戏侧进度服务。
    /// </summary>
    public BrotatoLikeProgressionService? ProgressionService => progressionService;

    /// <summary>
    /// 最近一次 Tick 结果。
    /// </summary>
    public SystemExecuteResult<BrotatoLikeSpawnTickResult> LastSpawnTickResult { get; private set; }

    /// <summary>
    /// 当前玩家跟随镜头（供验证场景检查状态）。
    /// </summary>
    public Camera2D? PlayerCamera => playerCamera;

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
        if (!AutoTick || !IsInitialized)
        {
            return;
        }

        if (HandleDeathAndRespawn((float)delta))
        {
            return;
        }

        var worldSchedule = RuntimeWorld.Default.Schedule;
        worldSchedule.RunPhase(SchedulePhase.BeginTick);
        worldSchedule.RunPhase(SchedulePhase.BeforeSystemTick);
        LastSpawnTickResult = TickSpawn(delta);
        TickPlayerAbilityCooldowns((float)delta);
        worldSchedule.RunPhase(SchedulePhase.AfterSystemTick);
        worldSchedule.RunPhase(SchedulePhase.AfterEventDispatch);
        worldSchedule.RunPhase(SchedulePhase.EndOfFrame);
    }

    private bool HandleDeathAndRespawn(float deltaSeconds)
    {
        if (playerEntity == null || !GodotObject.IsInstanceValid(playerEntity))
        {
            return false;
        }

        var isDead = playerEntity.Data.Get<bool>(DamageDataKeys.IsDead, false)
            || playerEntity.Data.Get<float>(DamageDataKeys.CurrentHp, 0f) <= 0f;

        if (!isDead)
        {
            deathElapsedSeconds = 0f;
            return false;
        }

        // 死亡后禁止移动和输入
        playerEntity.Data.Set(MovementDataKeys.CanMoveInput, false);
        playerEntity.Data.Set(MovementDataKeys.InputDirection, Vector2Value.Zero);
        deathElapsedSeconds += deltaSeconds;

        if (deathElapsedSeconds < RespawnDelaySeconds)
        {
            return true;
        }

        // 重生
        RespawnPlayer();
        deathElapsedSeconds = 0f;
        return false;
    }

    /// <summary>
    /// 立即重生玩家（供验证场景快进使用；正规流程中 HandleDeathAndRespawn 会自动调用）。
    /// </summary>
    public void ForceRespawnForValidation()
    {
        if (playerEntity != null && GodotObject.IsInstanceValid(playerEntity))
        {
            playerEntity.Data.Set(DamageDataKeys.IsDead, true);
            playerEntity.Data.Set(DamageDataKeys.CurrentHp, 0f);
        }

        deathElapsedSeconds = RespawnDelaySeconds;
    }

    private void RespawnPlayer()
    {
        SpawnPlayer();
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
        RuntimeWorld.Default.Schedule.PrintStatus();
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
        EnsureRuntimeDrivers();
        schedule = new RuntimeSchedule();
        var parent = enemyParent ?? this;
        schedule.Register(
            new SystemDescriptor(
                spawnScheduleConfig.SystemId,
                () => new BrotatoLikeScheduledEnemySpawnSystem(bootstrap, spawnCatalog, parent, movementDriver!)),
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
        progressionService?.SetPaused(true);
    }

    /// <summary>
    /// 关闭暂停菜单并恢复模拟。
    /// </summary>
    public void ClosePauseMenu()
    {
        schedule?.ProjectState.ClosePauseMenu();
        progressionService?.SetPaused(false);
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

        EnsureRuntimeDrivers();

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
        entity.Data.Set(MovementDataKeys.CanMoveInput, true);
        entity.Data.Set(MovementDataKeys.InputDirection, Vector2Value.Zero);
        entity.Data.Set(DamageDataKeys.IsDead, false);

        var composition = GodotUnitComposer.Compose(entity, BrotatoLikeUnitProfiles.Player);
        if (!composition.Success)
        {
            throw new InvalidOperationException(composition.FailureReason);
        }

        // 从 DataOS 创建初始技能实体
        var ownedAbilityIds = EntityIdList.Empty;
        ownedAbilityIds = SpawnPlayerAbility(entity, "slam", ownedAbilityIds);
        ownedAbilityIds = SpawnPlayerAbility(entity, "chain_lightning", ownedAbilityIds);
        ownedAbilityIds = SpawnPlayerAbility(entity, "target_point_skill", ownedAbilityIds);
        ownedAbilityIds = SpawnPlayerAbility(entity, "dash", ownedAbilityIds);

        entity.Data.Set(AbilityDataKeys.OwnedAbilityIds, ownedAbilityIds);
        entity.Data.Set(AbilityDataKeys.CurrentAbilityIndex, 0);
        entity.SetMeta("Level", 1);
        entity.SetMeta("Experience", 0);
        entity.SetMeta("NextLevelExperience", 5);

        // 游戏侧输入和技能 Adapter 不属于框架 composer。
        entity.AddChild(new BrotatoLikePlayerInputComponent { Name = "PlayerInput" });
        entity.AddChild(new GodotActiveSkillInputComponent { Name = "ActiveSkillInput" });

        AddChild(entity);
        playerEntity = entity;

        // 镜头跟随玩家
        if (playerCamera == null || !GodotObject.IsInstanceValid(playerCamera))
        {
            playerCamera = new Camera2D
            {
                Name = "PlayerCamera",
                Enabled = true,
                PositionSmoothingEnabled = true,
                PositionSmoothingSpeed = 5f
            };
        }

        if (playerCamera.GetParent() == null)
        {
            entity.AddChild(playerCamera);
        }
        else if (playerCamera.GetParent() != entity)
        {
            playerCamera.Reparent(entity, false);
        }

        playerCamera.Enabled = true;

        EnsureBrotatoLikeGameServices();

        // 启动 PlayerInput 移动
        movementDriver!.MovementSystem.Start(entity, new MovementParams
        {
            Mode = MoveMode.PlayerInput,
            MaxDuration = -1f // 不限制时长，持续响应输入
        });

        return entity;
    }

    private EntityIdList SpawnPlayerAbility(GodotEntity2D player, string abilityRecordId, EntityIdList ownedIds)
    {
        var abilityEntityId = $"ability-{abilityRecordId}-{player.EntityId}";
        var ability = bootstrap!.SpawnEntityFromRecord("ability", abilityRecordId, abilityEntityId);
        return ownedIds.Add(ability.EntityId);
    }

    private void EnsureBrotatoLikeGameServices()
    {
        if (hud == null || !GodotObject.IsInstanceValid(hud))
        {
            hud = new BrotatoLikeHud { Name = "BrotatoLikeHUD" };
            hud.Bind(this);
            AddChild(hud);
        }

        if (targetingController == null || !GodotObject.IsInstanceValid(targetingController))
        {
            targetingController = new BrotatoLikeTargetingController { Name = "BrotatoLikeTargetingController" };
            targetingController.Bind(this);
            AddChild(targetingController);
        }

        if (progressionService == null || !GodotObject.IsInstanceValid(progressionService))
        {
            progressionService = new BrotatoLikeProgressionService { Name = "BrotatoLikeProgressionService" };
            progressionService.Bind(this);
            AddChild(progressionService);
        }
    }

    private void TickPlayerAbilityCooldowns(float deltaSeconds)
    {
        if (playerEntity == null || deltaSeconds <= 0f)
        {
            return;
        }

        var ownedIds = playerEntity.Data.Get<EntityIdList>(AbilityDataKeys.OwnedAbilityIds);
        if (ownedIds.Count == 0)
        {
            return;
        }

        var abilities = new System.Collections.Generic.List<IEntity>(ownedIds.Count);
        for (var i = 0; i < ownedIds.Count; i++)
        {
            var ability = EntityManager.Get(ownedIds[i]);
            if (ability != null)
            {
                abilities.Add(ability);
            }
        }

        AbilityService.Instance.TickCooldowns(abilities, deltaSeconds);
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

        if (timerDriver != null)
        {
            timerDriver.QueueFree();
            timerDriver = null;
        }

        if (hud != null)
        {
            hud.QueueFree();
            hud = null;
        }

        if (targetingController != null)
        {
            targetingController.QueueFree();
            targetingController = null;
        }

        if (progressionService != null)
        {
            progressionService.QueueFree();
            progressionService = null;
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

    private void EnsureRuntimeDrivers()
    {
        if (movementDriver == null || !GodotObject.IsInstanceValid(movementDriver))
        {
            movementDriver = new GodotMovementDriver { Name = "MovementDriver" };
            AddChild(movementDriver);
        }

        if (timerDriver == null || !GodotObject.IsInstanceValid(timerDriver))
        {
            timerDriver = new GameOSTimerDriver { Name = "TimerDriver" };
            AddChild(timerDriver);
        }
    }
}
