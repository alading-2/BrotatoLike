using System;
using System.Collections.Generic;
using BrotatoLike.Game.RunFlow;
using BrotatoLike.Game.UI;
using Godot;
using SlimeAI.GameOS.Capabilities.Ability;
using SlimeAI.GameOS.Capabilities.Collision;
using SlimeAI.GameOS.Capabilities.Damage;
using SlimeAI.GameOS.Capabilities.Damage.Events;
using SlimeAI.GameOS.Capabilities.Effect;
using SlimeAI.GameOS.Capabilities.Movement;
using SlimeAI.GameOS.Capabilities.Projectile;
using SlimeAI.GameOS.Capabilities.Unit;
using SlimeAI.GameOS.GodotBridge;
using SlimeAI.GameOS.Runtime.Entity;
using SlimeAI.GameOS.Runtime.Event;

namespace BrotatoLike.Game.Progression;

/// <summary>
/// BrotatoLike 游戏侧进度闭环服务：暂停、恢复、波次状态、经验拾取和升级反馈。
/// </summary>
public partial class BrotatoLikeProgressionService : Node
{
    private const float HpRecoveryPerSecond = 18f;
    private const int BaseNextLevelExperience = 5;

    private readonly HashSet<EntityId> processedDrops = new();
    private IDisposable? killedSub;
    private BrotatoLikeGameRuntime? runtime;
    private bool forceWaveComplete;
    private float elapsedSeconds;
    private float phaseElapsedSeconds;
    private int completedWaveIndex;
    private int defeatedCount;
    private BrotatoLikeWavePhase wavePhase = BrotatoLikeWavePhase.Preparing;
    private IReadOnlyList<BrotatoLikeLevelUpChoiceDefinition> pendingChoices = [];

    /// <summary>
    /// 当前波次。
    /// </summary>
    public int WaveIndex => runtime?.SpawnCatalog?.Wave ?? 1;

    /// <summary>
    /// 当前波次状态机阶段。
    /// </summary>
    public BrotatoLikeWavePhase WavePhase => wavePhase;

    /// <summary>
    /// 当前波次状态机阶段名称，供 UI 和验证 artifact 记录。
    /// </summary>
    public string WavePhaseName => wavePhase.ToString();

    /// <summary>
    /// 波次状态节点。
    /// </summary>
    public Node WaveRuntimeState { get; private set; } = null!;

    /// <summary>
    /// 暂停菜单节点（scene-backed: PauseMenuUI.tscn）。
    /// </summary>
    public PauseMenuUI PauseMenu { get; private set; } = null!;

    /// <summary>
    /// 拾取层节点。
    /// </summary>
    public Node2D ExperiencePickupLayer { get; private set; } = null!;

    /// <summary>
    /// 升级反馈节点。
    /// </summary>
    public Label LevelUpFeedback { get; private set; } = null!;

    /// <summary>
    /// 升级选择面板（scene-backed: LevelUpChoicePanelUI.tscn）。
    /// </summary>
    public LevelUpChoicePanelUI LevelUpChoicePanel { get; private set; } = null!;

    /// <summary>
    /// 是否有待选择的升级奖励。
    /// </summary>
    public bool IsLevelUpChoicePending { get; private set; }

    /// <summary>
    /// 绑定运行时。
    /// </summary>
    /// <param name="runtime">BrotatoLike 运行时。</param>
    public void Bind(BrotatoLikeGameRuntime runtime)
    {
        this.runtime = runtime;
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        Name = "BrotatoLikeProgressionService";

        // scene-first exception: metadata-only runtime session node
        WaveRuntimeState = new Node { Name = "WaveRuntimeState" };
        AddChild(WaveRuntimeState);

        // scene-first exception: metadata-only node
        var recoveryNode = new Node { Name = "RecoveryTickService" };
        AddChild(recoveryNode);

        // scene-first exception: 极小运行时 marker，不构成复合 UI
        ExperiencePickupLayer = new Node2D { Name = "ExperiencePickupLayer" };
        AddChild(ExperiencePickupLayer);

        // scene-backed: 暂停菜单来自 PauseMenuUI.tscn
        var pauseScene = GD.Load<PackedScene>("res://Scenes/UI/PauseMenuUI.tscn");
        PauseMenu = pauseScene.Instantiate<PauseMenuUI>();
        PauseMenu.Name = "BrotatoLikePauseMenu";
        AddChild(PauseMenu);

        // scene-backed: 升级反馈来自 LevelUpFeedbackUI.tscn
        var feedbackScene = GD.Load<PackedScene>("res://Scenes/UI/LevelUpFeedbackUI.tscn");
        LevelUpFeedback = feedbackScene.Instantiate<Label>();
        LevelUpFeedback.Name = "LevelUpFeedback";
        AddChild(LevelUpFeedback);

        // scene-backed: 升级三选一面板来自 LevelUpChoicePanelUI.tscn
        var choiceScene = GD.Load<PackedScene>("res://Scenes/UI/LevelUpChoicePanelUI.tscn");
        LevelUpChoicePanel = choiceScene.Instantiate<LevelUpChoicePanelUI>();
        LevelUpChoicePanel.Name = "LevelUpChoicePanel";
        LevelUpChoicePanel.ChoiceSelected += SelectLevelUpChoice;
        AddChild(LevelUpChoicePanel);

        killedSub = WorldEvents.World.Subscribe<Killed>(OnKilled);
        UpdateWaveState();
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        killedSub?.Dispose();
        killedSub = null;
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        var deltaSeconds = (float)delta;
        elapsedSeconds += deltaSeconds;
        phaseElapsedSeconds += deltaSeconds;
        if (Input.IsActionJustPressed("PauseGame"))
        {
            if (PauseMenu.IsMenuVisible)
            {
                runtime?.ClosePauseMenu();
            }
            else
            {
                runtime?.OpenPauseMenu();
            }
        }

        if (IsLevelUpChoicePending)
        {
            UpdateWaveState();
            return;
        }

        TickRecovery((float)delta);
        ScanDeadEnemiesForDrops();
        TickPickupCollection();
        UpdateWaveState();
    }

    /// <summary>
    /// 同步暂停菜单显示。
    /// </summary>
    /// <param name="paused">是否暂停。</param>
    public void SetPaused(bool paused)
    {
        if (paused)
        {
            PauseMenu.ShowMenu();
        }
        else
        {
            PauseMenu.HideMenu();
        }

        PauseMenu.SetMeta("Paused", paused);
    }

    /// <summary>
    /// 为验证或调试确定性完成当前波次。
    /// </summary>
    public void CompleteCurrentWaveForValidation()
    {
        forceWaveComplete = true;
        UpdateWaveState();
    }

    /// <summary>
    /// 为验证或后续奖励系统进入波间奖励/商店阶段。
    /// </summary>
    public void EnterRewardPhaseForValidation()
    {
        EnterRewardPhase("validation");
        UpdateWaveState();
    }

    /// <summary>
    /// 为验证或后续波间流程启动下一波。
    /// </summary>
    public bool StartNextWaveForValidation()
    {
        var result = StartNextWave("validation");
        UpdateWaveState();
        return result;
    }

    /// <summary>
    /// 选择并应用升级奖励。
    /// </summary>
    /// <param name="choiceId">选择 Id。</param>
    public void SelectLevelUpChoice(string choiceId)
    {
        if (!IsLevelUpChoicePending || runtime?.PlayerEntity == null)
        {
            return;
        }

        var definition = FindPendingChoice(choiceId);
        if (definition == null)
        {
            return;
        }

        var result = ApplyLevelUpChoice(runtime.PlayerEntity, definition);
        LevelUpChoicePanel.RecordSelection(result);
        LevelUpChoicePanel.HidePanel();
        IsLevelUpChoicePending = false;
        pendingChoices = [];
        runtime.CloseLevelUpChoiceGate();

        LevelUpFeedback.Visible = false;
        SetMeta("LevelUpChoicePending", false);
        LevelUpFeedback.SetMeta("LastSelectedChoiceId", result.ChoiceId);
        LevelUpFeedback.SetMeta("LastSelectedEffectType", result.EffectType);
        LevelUpFeedback.SetMeta("LastBeforeValue", result.BeforeValue);
        LevelUpFeedback.SetMeta("LastAfterValue", result.AfterValue);
        LevelUpFeedback.SetMeta("LastApplySuccess", result.Success);
        SetMeta("LastSelectedChoiceId", result.ChoiceId);
        SetMeta("LastSelectedEffectType", result.EffectType);
        SetMeta("LastBeforeValue", result.BeforeValue);
        SetMeta("LastAfterValue", result.AfterValue);
        SetMeta("LastApplySuccess", result.Success);
    }

    private void OnKilled(Killed killed)
    {
        TrySpawnExperiencePickup(killed.Victim, killed.Killer);
    }

    private void TickRecovery(float deltaSeconds)
    {
        var player = runtime?.PlayerEntity;
        if (player == null)
        {
            return;
        }

        if (player.Data.Get<bool>(DamageDataKeys.IsDead, false))
        {
            SetRecoveryMeta("SkipReason", "dead");
            return;
        }

        var currentHp = player.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
        var maxHp = player.Data.Get<float>(DamageDataKeys.MaxHp, 0f);
        SetRecoveryMeta("ManaRecoveryStatus", "not-applicable");
        if (currentHp >= maxHp || maxHp <= 0f)
        {
            SetRecoveryMeta("SkipReason", "full");
            return;
        }

        var amount = MathF.Min(maxHp - currentHp, HpRecoveryPerSecond * deltaSeconds);
        if (amount <= 0f)
        {
            return;
        }

        var result = HealService.Instance.Process(new HealInfo
        {
            Target = player,
            Healer = player,
            Amount = amount,
            Source = HealSource.Regeneration
        });
        SetRecoveryMeta("LastAmount", result.Applied ? result.Info.FinalAmount : 0f);
        SetRecoveryMeta("SkipReason", result.Applied ? string.Empty : result.Message);
    }

    private void ScanDeadEnemiesForDrops()
    {
        var entities = EntityManager.GetAll();
        for (var i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            if (entity.Data.Get<int>(CollisionDataKeys.Team, 0) != 2)
            {
                continue;
            }

            if (entity.Data.Get<bool>(DamageDataKeys.IsDead, false)
                || entity.Data.Get<float>(DamageDataKeys.CurrentHp, 0f) <= 0f)
            {
                TrySpawnExperiencePickup(entity, runtime?.PlayerEntity);
            }
        }
    }

    private void TrySpawnExperiencePickup(IEntity enemy, IEntity? collector)
    {
        if (!processedDrops.Add(enemy.EntityId))
        {
            return;
        }

        var reward = enemy.Data.Get<int>(UnitDataKeys.ExpReward, 0);
        if (reward <= 0)
        {
            reward = 1;
        }

        var position = enemy.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
        var pickup = new Node2D
        {
            Name = ExperiencePickupLayer.GetNodeOrNull("ExperiencePickup") == null
                ? "ExperiencePickup"
                : $"ExperiencePickup_{enemy.EntityId.Value}",
            Position = new Vector2(position.X, position.Y)
        };
        pickup.SetMeta("EnemyId", enemy.EntityId.Value);
        pickup.SetMeta("Reward", reward);
        pickup.SetMeta("CollectorId", collector?.EntityId.Value ?? string.Empty);
        ExperiencePickupLayer.AddChild(pickup);
    }

    private void TickPickupCollection()
    {
        var player = runtime?.PlayerEntity;
        if (player == null)
        {
            return;
        }

        EnsurePlayerProgressionMeta(player);
        var playerPosition = player.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
        var playerVector = new Vector2(playerPosition.X, playerPosition.Y);
        var pickups = ExperiencePickupLayer.GetChildren();
        for (var i = 0; i < pickups.Count; i++)
        {
            if (pickups[i] is not Node2D pickup || pickup.IsQueuedForDeletion())
            {
                continue;
            }

            if (pickup.GlobalPosition.DistanceTo(playerVector) > 48f)
            {
                continue;
            }

            var reward = ReadIntMeta(pickup, "Reward", 1);
            var oldExperience = ReadIntMeta(player, "Experience", 0);
            var oldLevel = ReadIntMeta(player, "Level", 1);
            player.SetMeta("Experience", oldExperience + reward);
            pickup.QueueFree();
            ApplyLevelUp(player);
            ExperiencePickupLayer.SetMeta("LastReward", reward);
            ExperiencePickupLayer.SetMeta("OldExperience", oldExperience);
            ExperiencePickupLayer.SetMeta("NewExperience", ReadIntMeta(player, "Experience", 0));
            ExperiencePickupLayer.SetMeta("OldLevel", oldLevel);
            ExperiencePickupLayer.SetMeta("NewLevel", ReadIntMeta(player, "Level", 1));
            ExperiencePickupLayer.SetMeta("LastPickupCleaned", true);
        }
    }

    private void ApplyLevelUp(Node player)
    {
        var experience = ReadIntMeta(player, "Experience", 0);
        var level = ReadIntMeta(player, "Level", 1);
        var nextLevelExperience = ReadIntMeta(player, "NextLevelExperience", BaseNextLevelExperience);
        var oldLevel = level;
        var leveled = false;
        while (experience >= nextLevelExperience)
        {
            experience -= nextLevelExperience;
            level++;
            nextLevelExperience = BaseNextLevelExperience * level;
            leveled = true;
        }

        player.SetMeta("Experience", experience);
        player.SetMeta("Level", level);
        player.SetMeta("NextLevelExperience", nextLevelExperience);
        if (!leveled)
        {
            return;
        }

        LevelUpFeedback.Visible = true;
        LevelUpFeedback.Text = $"Level {level}";
        LevelUpFeedback.SetMeta("OldLevel", oldLevel);
        LevelUpFeedback.SetMeta("Level", level);
        LevelUpFeedback.SetMeta("Feedback", LevelUpFeedback.Text);
        LevelUpFeedback.SetMeta("SceneBacked", !string.IsNullOrEmpty(LevelUpFeedback.SceneFilePath));
        LevelUpFeedback.SetMeta("ScenePath", LevelUpFeedback.SceneFilePath);
        OpenLevelUpChoice(player, oldLevel, level);
    }

    private void OpenLevelUpChoice(Node player, int oldLevel, int newLevel)
    {
        if (player is not IEntity playerEntity || runtime == null)
        {
            return;
        }

        pendingChoices = BrotatoLikeLevelUpChoiceAuthoring.CreateChoices(playerEntity);
        IsLevelUpChoicePending = true;
        runtime.OpenLevelUpChoiceGate();
        LevelUpChoicePanel.ShowChoices(newLevel, pendingChoices);
        LevelUpChoicePanel.SetMeta("OldLevel", oldLevel);
        LevelUpChoicePanel.SetMeta("NewLevel", newLevel);
        LevelUpChoicePanel.SetMeta("GatePolicy", BrotatoLikeLevelUpChoiceAuthoring.GatePolicy);
        SetMeta("LevelUpChoicePending", true);
        SetMeta("LevelUpChoiceIds", JoinChoiceValues(pendingChoices, choice => choice.Id));
        SetMeta("LevelUpChoiceTexts", JoinChoiceValues(pendingChoices, choice => choice.DisplayText));
        SetMeta("LevelUpChoiceGatePolicy", BrotatoLikeLevelUpChoiceAuthoring.GatePolicy);
    }

    private BrotatoLikeLevelUpChoiceDefinition? FindPendingChoice(string choiceId)
    {
        for (var i = 0; i < pendingChoices.Count; i++)
        {
            if (string.Equals(pendingChoices[i].Id, choiceId, StringComparison.Ordinal))
            {
                return pendingChoices[i];
            }
        }

        return null;
    }

    private BrotatoLikeLevelUpChoiceApplyResult ApplyLevelUpChoice(
        GodotEntity2D player,
        BrotatoLikeLevelUpChoiceDefinition definition)
    {
        switch (definition.EffectType)
        {
            case BrotatoLikeLevelUpChoiceEffectType.AddMaxHp:
            {
                var before = player.Data.Get<float>(DamageDataKeys.MaxHp, 0f);
                var currentHp = player.Data.Get<float>(DamageDataKeys.CurrentHp, 0f);
                var after = before + definition.EffectValue;
                player.Data.Set(DamageDataKeys.MaxHp, after);
                player.Data.Set(DamageDataKeys.CurrentHp, currentHp + definition.EffectValue);
                return new BrotatoLikeLevelUpChoiceApplyResult(
                    true,
                    definition.Id,
                    definition.EffectType.ToString(),
                    definition.EffectTarget,
                    FormatFloat(before),
                    FormatFloat(after),
                    "max hp increased");
            }

            case BrotatoLikeLevelUpChoiceEffectType.AddMoveSpeed:
            {
                var before = player.Data.Get<float>(MovementDataKeys.MoveSpeed, 0f);
                var after = before + definition.EffectValue;
                player.Data.Set(MovementDataKeys.MoveSpeed, after);
                return new BrotatoLikeLevelUpChoiceApplyResult(
                    true,
                    definition.Id,
                    definition.EffectType.ToString(),
                    definition.EffectTarget,
                    FormatFloat(before),
                    FormatFloat(after),
                    "move speed increased");
            }

            case BrotatoLikeLevelUpChoiceEffectType.GrantAbility:
            {
                var ownedBefore = player.Data.Get<EntityIdList>(AbilityDataKeys.OwnedAbilityIds).Count;
                var abilityEntityId = EntityId.Empty;
                var message = "runtime unavailable";
                var success = runtime != null
                    && runtime.TryGrantPlayerAbility(
                        definition.AbilityRecordId,
                        visibleActive: false,
                        out abilityEntityId,
                        out message);
                var ownedAfter = player.Data.Get<EntityIdList>(AbilityDataKeys.OwnedAbilityIds).Count;
                return new BrotatoLikeLevelUpChoiceApplyResult(
                    success,
                    definition.Id,
                    definition.EffectType.ToString(),
                    abilityEntityId.IsEmpty ? definition.EffectTarget : abilityEntityId.Value,
                    ownedBefore.ToString(),
                    ownedAfter.ToString(),
                    message);
            }

            case BrotatoLikeLevelUpChoiceEffectType.UpgradeAbilityLevel:
            {
                var abilityEntityId = EntityId.Empty;
                var beforeLevel = 0;
                var afterLevel = 0;
                var success = runtime != null
                    && runtime.TryUpgradePlayerAbilityLevel(
                        definition.AbilityRecordId,
                        Mathf.RoundToInt(definition.EffectValue),
                        out abilityEntityId,
                        out beforeLevel,
                        out afterLevel);
                return new BrotatoLikeLevelUpChoiceApplyResult(
                    success,
                    definition.Id,
                    definition.EffectType.ToString(),
                    abilityEntityId.IsEmpty ? definition.EffectTarget : abilityEntityId.Value,
                    beforeLevel.ToString(),
                    afterLevel.ToString(),
                    success ? "ability level increased" : "ability level unchanged");
            }

            default:
                return new BrotatoLikeLevelUpChoiceApplyResult(
                    false,
                    definition.Id,
                    definition.EffectType.ToString(),
                    definition.EffectTarget,
                    string.Empty,
                    string.Empty,
                    "unsupported level-up choice");
        }
    }

    private void UpdateWaveState()
    {
        if (wavePhase == BrotatoLikeWavePhase.Preparing)
        {
            SetWavePhase(BrotatoLikeWavePhase.Running, "initial_start");
        }

        var totalSpawned = runtime?.LastSpawnTickResult.Value.TotalSpawned ?? 0;
        var remainingEnemies = CountAliveEnemies();
        var spawnCatalog = runtime?.SpawnCatalog;
        var expectedSpawnCount = spawnCatalog?.ExpectedSpawnCount ?? -1;
        var hasFiniteSpawnLimit = spawnCatalog?.HasFiniteSpawnLimit == true;
        var waveDuration = spawnCatalog?.WaveDuration ?? 60f;
        defeatedCount = Math.Max(defeatedCount, Math.Max(0, totalSpawned - remainingEnemies));
        var completed = forceWaveComplete
            || (hasFiniteSpawnLimit && totalSpawned >= expectedSpawnCount && remainingEnemies == 0)
            || elapsedSeconds >= waveDuration;
        if (wavePhase == BrotatoLikeWavePhase.Running && completed)
        {
            completedWaveIndex = WaveIndex;
            CleanupExpiredWaveEntities();
            SetWavePhase(BrotatoLikeWavePhase.Completed, "completion");
        }

        WaveRuntimeState.SetMeta("WaveIndex", WaveIndex);
        WaveRuntimeState.SetMeta("WavePhase", wavePhase.ToString());
        WaveRuntimeState.SetMeta("PreviousWaveIndex", completedWaveIndex);
        WaveRuntimeState.SetMeta("ElapsedTime", elapsedSeconds);
        WaveRuntimeState.SetMeta("PhaseElapsedTime", phaseElapsedSeconds);
        WaveRuntimeState.SetMeta("SpawnedCount", totalSpawned);
        WaveRuntimeState.SetMeta("ExpectedSpawnCount", expectedSpawnCount);
        WaveRuntimeState.SetMeta("HasFiniteSpawnLimit", hasFiniteSpawnLimit);
        WaveRuntimeState.SetMeta("DefeatedCount", defeatedCount);
        WaveRuntimeState.SetMeta("RemainingEnemies", remainingEnemies);
        WaveRuntimeState.SetMeta("Completed", completed);
        WaveRuntimeState.SetMeta("CompletionMode", ResolveWaveDefinition()?.CompletionMode ?? string.Empty);
        WaveRuntimeState.SetMeta("NextWaveId", ResolveWaveDefinition()?.NextWaveId ?? 0);
        WaveRuntimeState.SetMeta("RewardHook", ResolveWaveDefinition()?.RewardHook ?? string.Empty);
        WaveRuntimeState.SetMeta("ShopOfferSetId", ResolveWaveDefinition()?.ShopOfferSetId ?? string.Empty);
    }

    private void SetWavePhase(BrotatoLikeWavePhase next, string source)
    {
        if (wavePhase == next)
        {
            return;
        }

        var previous = wavePhase;
        wavePhase = next;
        phaseElapsedSeconds = 0f;
        WaveRuntimeState?.SetMeta("PreviousWavePhase", previous.ToString());
        WaveRuntimeState?.SetMeta("WavePhase", wavePhase.ToString());
        WaveRuntimeState?.SetMeta("LastTransitionSource", source);
        WaveRuntimeState?.SetMeta("LastTransition", $"{previous}->{next}");
    }

    private void EnterRewardPhase(string source)
    {
        if (wavePhase != BrotatoLikeWavePhase.Completed && wavePhase != BrotatoLikeWavePhase.RewardShop)
        {
            return;
        }

        SetWavePhase(BrotatoLikeWavePhase.RewardShop, source);
        var wave = ResolveWaveDefinition();
        WaveRuntimeState.SetMeta("RewardHook", wave?.RewardHook ?? string.Empty);
        WaveRuntimeState.SetMeta("ShopOfferSetId", wave?.ShopOfferSetId ?? string.Empty);
        WaveRuntimeState.SetMeta("ShopHookAvailable", runtime?.ShopService != null);
        WaveRuntimeState.SetMeta("LevelUpHookAvailable", true);
    }

    private bool StartNextWave(string source)
    {
        if (runtime == null || wavePhase != BrotatoLikeWavePhase.RewardShop)
        {
            return false;
        }

        SetWavePhase(BrotatoLikeWavePhase.NextWave, source);
        if (!runtime.TryStartNextWave(out var nextWave, out var message))
        {
            WaveRuntimeState.SetMeta("NextWaveStarted", false);
            WaveRuntimeState.SetMeta("NextWaveMessage", message);
            SetWavePhase(BrotatoLikeWavePhase.Ended, source);
            return false;
        }

        elapsedSeconds = 0f;
        forceWaveComplete = false;
        defeatedCount = 0;
        processedDrops.Clear();
        WaveRuntimeState.SetMeta("NextWaveStarted", true);
        WaveRuntimeState.SetMeta("NextWaveMessage", message);
        WaveRuntimeState.SetMeta("NextWaveIndex", nextWave);
        SetWavePhase(BrotatoLikeWavePhase.Running, source);
        return true;
    }

    private BrotatoLikeWaveDefinition? ResolveWaveDefinition()
    {
        return runtime != null && runtime.TryGetWaveDefinition(WaveIndex, out var wave)
            ? wave
            : null;
    }

    private void CleanupExpiredWaveEntities()
    {
        var runtimeBefore = EntityManager.GetAll().Count;
        var enemyBefore = CountWaveEnemies();
        var projectileEffectBefore = CountProjectileAndEffectEntities();
        var pickupBefore = CountExperiencePickups();

        var entities = EntityManager.GetAll();
        for (var i = 0; i < entities.Count; i++)
        {
            if (entities[i] is not GodotEntity2D enemy
                || enemy.Data.Get<int>(CollisionDataKeys.Team, 0) != 2
                || enemy.IsQueuedForDeletion())
            {
                continue;
            }

            if (enemy.Data.Get<bool>(DamageDataKeys.IsDead, false)
                || enemy.Data.Get<float>(DamageDataKeys.CurrentHp, 0f) <= 0f)
            {
                enemy.DestroyEntity();
            }
        }

        if (ExperiencePickupLayer != null)
        {
            foreach (var child in ExperiencePickupLayer.GetChildren())
            {
                if (child is Node pickup && !pickup.IsQueuedForDeletion())
                {
                    pickup.QueueFree();
                }
            }
        }

        WaveRuntimeState.SetMeta("CleanupRuntimeEntityCountBefore", runtimeBefore);
        WaveRuntimeState.SetMeta("CleanupRuntimeEntityCountAfter", EntityManager.GetAll().Count);
        WaveRuntimeState.SetMeta("CleanupEnemyCountBefore", enemyBefore);
        WaveRuntimeState.SetMeta("CleanupEnemyCountAfter", CountWaveEnemies());
        WaveRuntimeState.SetMeta("CleanupPickupCountBefore", pickupBefore);
        WaveRuntimeState.SetMeta("CleanupPickupCountAfter", CountExperiencePickups());
        WaveRuntimeState.SetMeta("CleanupProjectileEffectCountBefore", projectileEffectBefore);
        WaveRuntimeState.SetMeta("CleanupProjectileEffectCountAfter", CountProjectileAndEffectEntities());
    }

    private static int CountAliveEnemies()
    {
        var count = 0;
        var entities = EntityManager.GetAll();
        for (var i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            if (entity.Data.Get<int>(CollisionDataKeys.Team, 0) != 2)
            {
                continue;
            }

            if (!entity.Data.Get<bool>(DamageDataKeys.IsDead, false)
                && entity.Data.Get<float>(DamageDataKeys.CurrentHp, 0f) > 0f)
            {
                count++;
            }
        }

        return count;
    }

    private static int CountWaveEnemies()
    {
        var count = 0;
        var entities = EntityManager.GetAll();
        for (var i = 0; i < entities.Count; i++)
        {
            if (entities[i].Data.Get<int>(CollisionDataKeys.Team, 0) == 2)
            {
                count++;
            }
        }

        return count;
    }

    private static int CountProjectileAndEffectEntities()
    {
        var count = 0;
        var entities = EntityManager.GetAll();
        for (var i = 0; i < entities.Count; i++)
        {
            if (entities[i].Data.Has(ProjectileDataKeys.ScenePath)
                || entities[i].Data.Has(EffectDataKeys.ScenePath))
            {
                count++;
            }
        }

        return count;
    }

    private int CountExperiencePickups()
    {
        if (ExperiencePickupLayer == null)
        {
            return 0;
        }

        var count = 0;
        foreach (var child in ExperiencePickupLayer.GetChildren())
        {
            if (child is Node node && !node.IsQueuedForDeletion())
            {
                count++;
            }
        }

        return count;
    }

    private static void EnsurePlayerProgressionMeta(Node player)
    {
        if (!player.HasMeta("Level"))
        {
            player.SetMeta("Level", 1);
        }

        if (!player.HasMeta("Experience"))
        {
            player.SetMeta("Experience", 0);
        }

        if (!player.HasMeta("NextLevelExperience"))
        {
            player.SetMeta("NextLevelExperience", BaseNextLevelExperience);
        }
    }

    private void SetRecoveryMeta(string key, Variant value)
    {
        var node = GetNodeOrNull("RecoveryTickService");
        node?.SetMeta(key, value);
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

    private static string JoinChoiceValues(
        IReadOnlyList<BrotatoLikeLevelUpChoiceDefinition> choices,
        Func<BrotatoLikeLevelUpChoiceDefinition, string> selector)
    {
        var values = new string[choices.Count];
        for (var i = 0; i < choices.Count; i++)
        {
            values[i] = selector(choices[i]);
        }

        return string.Join(",", values);
    }

    private static string FormatFloat(float value)
    {
        return value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
    }
}
