using System;
using System.Collections.Generic;
using BrotatoLike.Game.Bridge;
using BrotatoLike.Game.Items;
using BrotatoLike.Game.Progression;
using BrotatoLike.Game.RunFlow;
using BrotatoLike.Game.Shop;
using BrotatoLike.Game.UI;
using BrotatoLike.Game.VFX;
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
    private BrotatoLikeItemCatalog? itemCatalog;
    private BrotatoLikeWaveCatalog? waveCatalog;
    private BrotatoLikeSpawnCatalog? spawnCatalog;
    private SystemConfig? spawnScheduleConfig;
    private Node? spawnParent;
    private GodotMovementDriver? movementDriver;
    private GameOSTimerDriver? timerDriver;
    private GodotProjectileEffectSpawner? projectileEffectSpawner;
    private BrotatoLikeChainLightningVfxBinder? chainLightningVfxBinder;
    private GodotEntity2D? playerEntity;
    private BrotatoLikeHud? hud;
    private BrotatoLikeTargetingController? targetingController;
    private BrotatoLikeProgressionService? progressionService;
    private BrotatoLikeShopService? shopService;
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
    /// 当前 wave authoring catalog。
    /// </summary>
    public BrotatoLikeWaveCatalog? WaveCatalog => waveCatalog;

    /// <summary>
    /// 当前波次。
    /// </summary>
    public int CurrentWave => spawnCatalog?.Wave ?? InitialWave;

    /// <summary>
    /// 当前玩家 Godot Entity。
    /// </summary>
    public GodotEntity2D? PlayerEntity => playerEntity;

    /// <summary>
    /// 当前 Movement Driver。
    /// </summary>
    public GodotMovementDriver? MovementDriver => movementDriver;

    /// <summary>
    /// 链电 VFX 端点绑定器。
    /// </summary>
    public BrotatoLikeChainLightningVfxBinder? ChainLightningVfxBinder => chainLightningVfxBinder;

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
    /// 游戏侧商店服务。
    /// </summary>
    public BrotatoLikeShopService? ShopService => shopService;

    /// <summary>
    /// 当前 ProjectState 快照，供验证升级选择门禁使用。
    /// </summary>
    public ProjectStateSnapshot? CurrentProjectState => schedule?.ProjectState.Snapshot;

    /// <summary>
    /// 最近一次 Tick 结果。
    /// </summary>
    public SystemExecuteResult<BrotatoLikeSpawnTickResult> LastSpawnTickResult { get; private set; }

    /// <summary>
    /// 当前玩家跟随镜头（供验证场景检查状态）。
    /// </summary>
    public Camera2D? PlayerCamera => playerCamera;

    /// <summary>
    /// 玩家是否处于死亡后的复活等待阶段。
    /// </summary>
    public bool IsPlayerRespawning => playerEntity != null
        && GodotObject.IsInstanceValid(playerEntity)
        && playerEntity.Data.Get<bool>(DamageDataKeys.IsDead, false);

    /// <summary>
    /// 当前复活进度，0 到 1。
    /// </summary>
    public float RespawnProgress => IsPlayerRespawning
        ? Mathf.Clamp(deathElapsedSeconds / RespawnDelaySeconds, 0f, 1f)
        : 0f;

    /// <summary>
    /// 复活剩余秒数。
    /// </summary>
    public float RespawnRemainingSeconds => IsPlayerRespawning
        ? Mathf.Max(0f, RespawnDelaySeconds - deathElapsedSeconds)
        : 0f;

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
        playerEntity.Data.Set(DamageDataKeys.IsDead, true);
        playerEntity.Data.Set(MovementDataKeys.CanMoveInput, false);
        playerEntity.Data.Set(MovementDataKeys.InputDirection, Vector2Value.Zero);
        deathElapsedSeconds += deltaSeconds;

        var maxHp = playerEntity.Data.Get<float>(DamageDataKeys.MaxHp, 0f);
        if (maxHp > 0f)
        {
            var respawnHp = maxHp * RespawnProgress;
            playerEntity.Data.Set(DamageDataKeys.CurrentHp, respawnHp);
        }

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
        if (playerEntity == null || !GodotObject.IsInstanceValid(playerEntity))
        {
            SpawnPlayer();
            return;
        }

        var state = CaptureRespawnState(playerEntity);
        var respawned = SpawnPlayer(spawnPosition: state.Position);
        ApplyRespawnState(respawned, state);
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
        itemCatalog = BrotatoLikeItemCatalog.LoadFromResource();
        waveCatalog = BrotatoLikeWaveCatalog.LoadFromResource();
        waveCatalog.Validate(bootstrap);
        BrotatoLikeSkillLoadoutAuthoring.ValidateAvailableSkillPool(bootstrap);
        spawnCatalog = BuildSpawnCatalogForWave(wave);
        spawnScheduleConfig = bootstrap.BuildSpawnSystemScheduleConfig();
        EnsureRuntimeDrivers();
        BrotatoLikeAbilityHandlers.RegisterAll(movementDriver!.MovementSystem);
        schedule = new RuntimeSchedule();
        spawnParent = enemyParent ?? this;
        schedule.Register(
            new SystemDescriptor(
                spawnScheduleConfig.SystemId,
                () => new BrotatoLikeScheduledEnemySpawnSystem(bootstrap, spawnCatalog, spawnParent!, movementDriver!)),
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
    /// 打开升级选择门禁：使用 ModalUi 覆盖层并暂停 schedule-gated gameplay。
    /// </summary>
    public void OpenLevelUpChoiceGate()
    {
        if (schedule == null)
        {
            return;
        }

        var snapshot = schedule.ProjectState.Snapshot;
        schedule.ProjectState.Apply(snapshot with
        {
            Overlays = snapshot.Overlays | OverlayFlags.ModalUi,
            SimulationState = SimulationState.Suspended
        });
    }

    /// <summary>
    /// 关闭升级选择门禁；若仍存在其它阻塞覆盖层则保持暂停。
    /// </summary>
    public void CloseLevelUpChoiceGate()
    {
        if (schedule == null)
        {
            return;
        }

        var snapshot = schedule.ProjectState.Snapshot;
        var overlays = snapshot.Overlays & ~OverlayFlags.ModalUi;
        schedule.ProjectState.Apply(snapshot with
        {
            Overlays = overlays,
            SimulationState = (overlays & OverlayFlags.Blocking) == OverlayFlags.None
                ? SimulationState.Running
                : SimulationState.Suspended
        });
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
        return SpawnPlayerCore(recordId, spawnPosition, BrotatoLikeSkillLoadoutAuthoring.CreateDefault());
    }

    /// <summary>
    /// 使用确定性验证 loadout 生成玩家，供后续逐技能 validation 场景复用。
    /// </summary>
    /// <param name="abilityIds">要授予玩家的 ability record id 列表。</param>
    /// <param name="recordId">DataOS unit.player 记录 Id，默认 deluyi。</param>
    /// <param name="spawnPosition">玩家生成位置。</param>
    /// <returns>生成的玩家 GodotEntity2D。</returns>
    public GodotEntity2D SpawnPlayerWithValidationLoadout(
        IReadOnlyList<string> abilityIds,
        string recordId = "deluyi",
        Vector2? spawnPosition = null)
    {
        return SpawnPlayerCore(recordId, spawnPosition, BrotatoLikeSkillLoadoutAuthoring.CreateValidationOverride(abilityIds));
    }

    private GodotEntity2D SpawnPlayerCore(string recordId, Vector2? spawnPosition, BrotatoLikeSkillLoadout loadout)
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

        // 从集中 loadout authoring 创建技能实体
        var ownedAbilityIds = EntityIdList.Empty;
        var visibleActiveEntityIds = EntityIdList.Empty;
        for (var i = 0; i < loadout.AbilityIds.Count; i++)
        {
            var abilityId = loadout.AbilityIds[i];
            if (!bootstrap.HasRecord("ability", abilityId))
            {
                throw new InvalidOperationException($"BrotatoLike loadout references missing DataOS ability id: {abilityId}");
            }

            ownedAbilityIds = SpawnPlayerAbility(entity, abilityId, ownedAbilityIds);
            if (ContainsRecordId(loadout.VisibleActiveAbilityIds, abilityId))
            {
                visibleActiveEntityIds = visibleActiveEntityIds.Add(new EntityId(BuildPlayerAbilityEntityId(recordId, abilityId)));
            }
        }

        entity.Data.Set(AbilityDataKeys.OwnedAbilityIds, ownedAbilityIds);
        entity.Data.Set(AbilityDataKeys.CurrentAbilityIndex, 0);
        WriteLoadoutMetadata(entity, loadout, ownedAbilityIds, visibleActiveEntityIds);
        entity.SetMeta("Level", 1);
        entity.SetMeta("Experience", 0);
        entity.SetMeta("NextLevelExperience", 5);
        entity.Data.Set(BrotatoLikeShopDataKeys.Currency, BrotatoLikeShopService.DefaultStartingCurrency);
        entity.SetMeta("Currency", BrotatoLikeShopService.DefaultStartingCurrency);

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

    /// <summary>
    /// 打开 scene-backed 商店面板。
    /// </summary>
    public IReadOnlyList<BrotatoLikeShopOfferView> OpenShop(string offerSetId = BrotatoLikeShopService.DefaultOfferSetId)
    {
        EnsureBrotatoLikeGameServices();
        return shopService?.OpenShop(offerSetId) ?? Array.Empty<BrotatoLikeShopOfferView>();
    }

    /// <summary>
    /// 切换到指定波次的生成目录。
    /// </summary>
    public bool TryStartWave(int wave, out string message)
    {
        message = string.Empty;
        if (bootstrap == null || schedule == null || spawnScheduleConfig == null)
        {
            message = "runtime schedule is not ready";
            return false;
        }

        if (wave <= 0)
        {
            message = $"invalid wave: {wave}";
            return false;
        }

        BrotatoLikeSpawnCatalog nextCatalog;
        try
        {
            nextCatalog = BuildSpawnCatalogForWave(wave);
        }
        catch (InvalidOperationException ex)
        {
            message = ex.Message;
            return false;
        }

        EnsureRuntimeDrivers();
        var spawnSystem = schedule.Resolve<BrotatoLikeScheduledEnemySpawnSystem>();
        if (spawnSystem == null)
        {
            message = "spawn system is not loaded";
            return false;
        }

        spawnCatalog = nextCatalog;
        spawnSystem.Reconfigure(bootstrap, spawnCatalog, spawnParent ?? this, movementDriver);
        LastSpawnTickResult = default;
        message = $"wave {wave} started";
        return true;
    }

    /// <summary>
    /// 根据 wave authoring 切换到下一波。
    /// </summary>
    public bool TryStartNextWave(out int nextWave, out string message)
    {
        nextWave = 0;
        var current = CurrentWave;
        if (waveCatalog != null && waveCatalog.TryGetNextWaveId(current, out nextWave))
        {
            return TryStartWave(nextWave, out message);
        }

        var maxWaves = spawnCatalog?.MaxWaves ?? -1;
        var fallbackNext = current + 1;
        if (maxWaves > 0 && fallbackNext <= maxWaves)
        {
            nextWave = fallbackNext;
            return TryStartWave(nextWave, out message);
        }

        message = $"no next wave after {current}";
        return false;
    }

    /// <summary>
    /// 查找当前或指定波次定义。
    /// </summary>
    public bool TryGetWaveDefinition(int wave, out BrotatoLikeWaveDefinition definition)
    {
        definition = null!;
        return waveCatalog?.TryGetWave(wave, out definition) == true;
    }

    private EntityIdList SpawnPlayerAbility(GodotEntity2D player, string abilityRecordId, EntityIdList ownedIds)
    {
        var abilityEntityId = BuildPlayerAbilityEntityId(player, abilityRecordId);
        var ability = bootstrap!.SpawnEntityFromRecord("ability", abilityRecordId, abilityEntityId);
        return ownedIds.Add(ability.EntityId);
    }

    /// <summary>
    /// 向当前玩家授予一个 DataOS ability 记录。
    /// </summary>
    public bool TryGrantPlayerAbility(
        string abilityRecordId,
        bool visibleActive,
        out EntityId abilityEntityId,
        out string message)
    {
        abilityEntityId = EntityId.Empty;
        if (bootstrap == null || playerEntity == null || !GodotObject.IsInstanceValid(playerEntity))
        {
            message = "runtime player is not ready";
            return false;
        }

        if (!bootstrap.HasRecord("ability", abilityRecordId))
        {
            message = $"ability record not found: {abilityRecordId}";
            return false;
        }

        abilityEntityId = new EntityId(BuildPlayerAbilityEntityId(playerEntity, abilityRecordId));
        var ownedIds = playerEntity.Data.Get<EntityIdList>(AbilityDataKeys.OwnedAbilityIds);
        if (ownedIds.Contains(abilityEntityId))
        {
            message = $"ability already owned: {abilityRecordId}";
            return false;
        }

        var ability = bootstrap.SpawnEntityFromRecord("ability", abilityRecordId, abilityEntityId.Value);
        ownedIds = ownedIds.Add(ability.EntityId);
        playerEntity.Data.Set(AbilityDataKeys.OwnedAbilityIds, ownedIds);

        var visibleIds = ReadEntityIdsMeta(playerEntity, BrotatoLikeSkillLoadoutAuthoring.VisibleActiveAbilityEntityIdsMeta);
        if (visibleActive
            && visibleIds.Count < BrotatoLikeSkillLoadoutAuthoring.VisibleActiveSlotCapacity
            && !BrotatoLikeSkillLoadoutAuthoring.IsPassiveAbility(abilityRecordId))
        {
            visibleIds = visibleIds.Add(ability.EntityId);
            AppendStringListMeta(playerEntity, BrotatoLikeSkillLoadoutAuthoring.VisibleActiveAbilityRecordIdsMeta, abilityRecordId);
        }

        RewriteRuntimeLoadoutMetadata(playerEntity, ownedIds, visibleIds);
        message = $"granted ability: {abilityRecordId}";
        return true;
    }

    /// <summary>
    /// 提升当前玩家已拥有技能等级。
    /// </summary>
    public bool TryUpgradePlayerAbilityLevel(
        string abilityRecordId,
        int delta,
        out EntityId abilityEntityId,
        out int beforeLevel,
        out int afterLevel)
    {
        abilityEntityId = EntityId.Empty;
        beforeLevel = 0;
        afterLevel = 0;
        if (playerEntity == null || !GodotObject.IsInstanceValid(playerEntity))
        {
            return false;
        }

        var expectedId = new EntityId(BuildPlayerAbilityEntityId(playerEntity, abilityRecordId));
        var ownedIds = playerEntity.Data.Get<EntityIdList>(AbilityDataKeys.OwnedAbilityIds);
        if (!ownedIds.Contains(expectedId))
        {
            return false;
        }

        var ability = EntityManager.Get(expectedId);
        if (ability == null)
        {
            return false;
        }

        abilityEntityId = ability.EntityId;
        beforeLevel = ability.Data.Get<int>(AbilityDataKeys.Level, 1);
        var maxLevel = ability.Data.Get<int>(AbilityDataKeys.MaxLevel, 10);
        afterLevel = Math.Clamp(beforeLevel + delta, 1, Math.Max(1, maxLevel));
        ability.Data.Set(AbilityDataKeys.Level, afterLevel);
        return afterLevel != beforeLevel;
    }

    private static string BuildPlayerAbilityEntityId(GodotEntity2D player, string abilityRecordId)
    {
        return $"ability-{abilityRecordId}-{player.EntityId.Value}";
    }

    private static string BuildPlayerAbilityEntityId(string playerRecordId, string abilityRecordId)
    {
        return $"ability-{abilityRecordId}-player-{playerRecordId}";
    }

    private static void WriteLoadoutMetadata(
        GodotEntity2D player,
        BrotatoLikeSkillLoadout loadout,
        EntityIdList ownedAbilityIds,
        EntityIdList visibleActiveEntityIds)
    {
        player.SetMeta(BrotatoLikeSkillLoadoutAuthoring.LoadoutSourceMeta, loadout.Source);
        player.SetMeta(BrotatoLikeSkillLoadoutAuthoring.DefaultActiveAbilityIdsMeta,
            BrotatoLikeSkillLoadoutAuthoring.JoinIds(BrotatoLikeSkillLoadoutAuthoring.DefaultActiveAbilityIds));
        player.SetMeta(BrotatoLikeSkillLoadoutAuthoring.AvailableSkillPoolIdsMeta,
            BrotatoLikeSkillLoadoutAuthoring.JoinIds(BrotatoLikeSkillLoadoutAuthoring.AvailableSkillPoolAbilityIds));
        player.SetMeta(BrotatoLikeSkillLoadoutAuthoring.PassiveSkillIdsMeta,
            BrotatoLikeSkillLoadoutAuthoring.JoinIds(BrotatoLikeSkillLoadoutAuthoring.PassiveAbilityIds));
        player.SetMeta(BrotatoLikeSkillLoadoutAuthoring.OwnedAbilityEntityIdsMeta,
            BrotatoLikeSkillLoadoutAuthoring.JoinIds(ownedAbilityIds));
        player.SetMeta(BrotatoLikeSkillLoadoutAuthoring.VisibleActiveAbilityEntityIdsMeta,
            BrotatoLikeSkillLoadoutAuthoring.JoinIds(visibleActiveEntityIds));
        player.SetMeta(BrotatoLikeSkillLoadoutAuthoring.VisibleActiveAbilityRecordIdsMeta,
            BrotatoLikeSkillLoadoutAuthoring.JoinIds(loadout.VisibleActiveAbilityIds));
        player.SetMeta(BrotatoLikeSkillLoadoutAuthoring.TotalOwnedCountMeta, ownedAbilityIds.Count);
        player.SetMeta(BrotatoLikeSkillLoadoutAuthoring.VisibleSlotCountMeta, visibleActiveEntityIds.Count);
        player.SetMeta(BrotatoLikeSkillLoadoutAuthoring.HiddenOwnedCountMeta, Math.Max(0, ownedAbilityIds.Count - visibleActiveEntityIds.Count));
    }

    private static void RewriteRuntimeLoadoutMetadata(
        GodotEntity2D player,
        EntityIdList ownedAbilityIds,
        EntityIdList visibleActiveEntityIds)
    {
        player.SetMeta(BrotatoLikeSkillLoadoutAuthoring.LoadoutSourceMeta, BrotatoLikeSkillLoadoutAuthoring.SourceLevelUpChoice);
        player.SetMeta(BrotatoLikeSkillLoadoutAuthoring.OwnedAbilityEntityIdsMeta,
            BrotatoLikeSkillLoadoutAuthoring.JoinIds(ownedAbilityIds));
        player.SetMeta(BrotatoLikeSkillLoadoutAuthoring.VisibleActiveAbilityEntityIdsMeta,
            BrotatoLikeSkillLoadoutAuthoring.JoinIds(visibleActiveEntityIds));
        player.SetMeta(BrotatoLikeSkillLoadoutAuthoring.TotalOwnedCountMeta, ownedAbilityIds.Count);
        player.SetMeta(BrotatoLikeSkillLoadoutAuthoring.VisibleSlotCountMeta, visibleActiveEntityIds.Count);
        player.SetMeta(BrotatoLikeSkillLoadoutAuthoring.HiddenOwnedCountMeta, Math.Max(0, ownedAbilityIds.Count - visibleActiveEntityIds.Count));
    }

    private static EntityIdList ReadEntityIdsMeta(IEntity owner, string key)
    {
        if (owner is not Node node || !node.HasMeta(key))
        {
            return EntityIdList.Empty;
        }

        var raw = node.GetMeta(key).AsString();
        var result = EntityIdList.Empty;
        var parts = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            result = result.Add(new EntityId(parts[i]));
        }

        return result;
    }

    private static void AppendStringListMeta(Node node, string key, string value)
    {
        var existing = node.HasMeta(key) ? node.GetMeta(key).AsString() : string.Empty;
        if (string.IsNullOrWhiteSpace(existing))
        {
            node.SetMeta(key, value);
            return;
        }

        var parts = existing.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            if (string.Equals(parts[i], value, StringComparison.Ordinal))
            {
                return;
            }
        }

        node.SetMeta(key, $"{existing},{value}");
    }

    private static bool ContainsRecordId(IReadOnlyList<string> recordIds, string abilityRecordId)
    {
        for (var i = 0; i < recordIds.Count; i++)
        {
            if (string.Equals(recordIds[i], abilityRecordId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
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

        if (shopService == null || !GodotObject.IsInstanceValid(shopService))
        {
            shopService = new BrotatoLikeShopService { Name = "BrotatoLikeShopService" };
            shopService.Bind(this, itemCatalog ?? BrotatoLikeItemCatalog.LoadFromResource());
            AddChild(shopService);
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

    private static PlayerRespawnState CaptureRespawnState(GodotEntity2D player)
    {
        return new PlayerRespawnState(
            player.Position,
            ReadIntMeta(player, "Level", 1),
            ReadIntMeta(player, "Experience", 0),
            ReadIntMeta(player, "NextLevelExperience", 5));
    }

    private static void ApplyRespawnState(GodotEntity2D player, PlayerRespawnState state)
    {
        player.Position = state.Position;
        player.Data.Set(MovementDataKeys.Position, new Vector2Value(state.Position.X, state.Position.Y));
        player.Data.Set(MovementDataKeys.CanMoveInput, true);
        player.Data.Set(MovementDataKeys.InputDirection, Vector2Value.Zero);
        player.Data.Set(DamageDataKeys.IsDead, false);

        var maxHp = player.Data.Get<float>(DamageDataKeys.MaxHp, 0f);
        if (maxHp > 0f)
        {
            player.Data.Set(DamageDataKeys.CurrentHp, maxHp);
        }

        player.SetMeta("Level", state.Level);
        player.SetMeta("Experience", state.Experience);
        player.SetMeta("NextLevelExperience", state.NextLevelExperience);
    }

    private static int ReadIntMeta(Node node, string key, int fallback)
    {
        if (!node.HasMeta(key))
        {
            return fallback;
        }

        var value = node.GetMeta(key);
        return value.VariantType switch
        {
            Variant.Type.Int => value.AsInt32(),
            Variant.Type.Float => Mathf.RoundToInt(value.AsSingle()),
            Variant.Type.String => int.TryParse(value.AsString(), out var parsed) ? parsed : fallback,
            _ => fallback
        };
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

        if (shopService != null)
        {
            shopService.QueueFree();
            shopService = null;
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

        if (chainLightningVfxBinder != null)
        {
            chainLightningVfxBinder.Unsubscribe();
            chainLightningVfxBinder.QueueFree();
            chainLightningVfxBinder = null;
        }

        if (projectileEffectSpawner != null)
        {
            projectileEffectSpawner.Unsubscribe();
            projectileEffectSpawner.QueueFree();
            projectileEffectSpawner = null;
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
        waveCatalog = null;
        spawnCatalog = null;
        spawnScheduleConfig = null;
        spawnParent = null;
        LastSpawnTickResult = default;
    }

    private BrotatoLikeSpawnCatalog BuildSpawnCatalogForWave(int wave)
    {
        if (waveCatalog != null)
        {
            return waveCatalog.BuildSpawnCatalog(wave);
        }

        if (bootstrap == null)
        {
            throw new InvalidOperationException("BrotatoLikeGameRuntime bootstrap is not ready.");
        }

        return bootstrap.BuildEnemySpawnCatalog(wave);
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

        if (projectileEffectSpawner == null || !GodotObject.IsInstanceValid(projectileEffectSpawner))
        {
            projectileEffectSpawner = new GodotProjectileEffectSpawner { Name = "ProjectileEffectSpawner" };
            AddChild(projectileEffectSpawner);
        }

        if (chainLightningVfxBinder == null || !GodotObject.IsInstanceValid(chainLightningVfxBinder))
        {
            chainLightningVfxBinder = new BrotatoLikeChainLightningVfxBinder { Name = "ChainLightningVfxBinder" };
            AddChild(chainLightningVfxBinder);
        }
    }

    private readonly record struct PlayerRespawnState(
        Vector2 Position,
        int Level,
        int Experience,
        int NextLevelExperience);
}
