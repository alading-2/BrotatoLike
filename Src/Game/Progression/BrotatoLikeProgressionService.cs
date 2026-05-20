using System;
using System.Collections.Generic;
using Godot;
using SlimeAI.GameOS.Capabilities.Collision;
using SlimeAI.GameOS.Capabilities.Damage;
using SlimeAI.GameOS.Capabilities.Damage.Events;
using SlimeAI.GameOS.Capabilities.Movement;
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

    /// <summary>
    /// 当前波次。
    /// </summary>
    public int WaveIndex => runtime?.SpawnCatalog?.Wave ?? 1;

    /// <summary>
    /// 波次状态节点。
    /// </summary>
    public Node WaveRuntimeState { get; private set; } = null!;

    /// <summary>
    /// 暂停菜单节点。
    /// </summary>
    public Control PauseMenu { get; private set; } = null!;

    /// <summary>
    /// 拾取层节点。
    /// </summary>
    public Node2D ExperiencePickupLayer { get; private set; } = null!;

    /// <summary>
    /// 升级反馈节点。
    /// </summary>
    public Label LevelUpFeedback { get; private set; } = null!;

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
        WaveRuntimeState = new Node { Name = "WaveRuntimeState" };
        AddChild(WaveRuntimeState);

        var recoveryNode = new Node { Name = "RecoveryTickService" };
        AddChild(recoveryNode);

        ExperiencePickupLayer = new Node2D { Name = "ExperiencePickupLayer" };
        AddChild(ExperiencePickupLayer);

        PauseMenu = new Control
        {
            Name = "BrotatoLikePauseMenu",
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        PauseMenu.SetMeta("Paused", false);
        AddChild(PauseMenu);

        var pauseLabel = new Label
        {
            Name = "PauseTitle",
            Text = "Paused",
            Position = new Vector2(16f, 16f)
        };
        PauseMenu.AddChild(pauseLabel);

        LevelUpFeedback = new Label
        {
            Name = "LevelUpFeedback",
            Text = string.Empty,
            Visible = false,
            Position = new Vector2(16f, 120f)
        };
        AddChild(LevelUpFeedback);

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
        elapsedSeconds += (float)delta;
        if (Input.IsActionJustPressed("PauseGame"))
        {
            if (PauseMenu.Visible)
            {
                runtime?.ClosePauseMenu();
            }
            else
            {
                runtime?.OpenPauseMenu();
            }
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
        PauseMenu.Visible = paused;
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
        LevelUpFeedback.SetMeta("Level", level);
        LevelUpFeedback.SetMeta("Feedback", LevelUpFeedback.Text);
    }

    private void UpdateWaveState()
    {
        var totalSpawned = runtime?.LastSpawnTickResult.Value.TotalSpawned ?? 0;
        var remainingEnemies = CountAliveEnemies();
        var waveDuration = runtime?.SpawnCatalog?.WaveDuration ?? 60f;
        var completed = forceWaveComplete
            || (totalSpawned > 0 && remainingEnemies == 0)
            || elapsedSeconds >= waveDuration;
        WaveRuntimeState.SetMeta("WaveIndex", WaveIndex);
        WaveRuntimeState.SetMeta("ElapsedTime", elapsedSeconds);
        WaveRuntimeState.SetMeta("SpawnedCount", totalSpawned);
        WaveRuntimeState.SetMeta("RemainingEnemies", remainingEnemies);
        WaveRuntimeState.SetMeta("Completed", completed);
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
}
