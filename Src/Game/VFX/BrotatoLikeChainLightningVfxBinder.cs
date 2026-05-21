using System;
using System.Collections.Generic;
using Godot;
using SlimeAI.GameOS.Capabilities.Effect;
using SlimeAI.GameOS.Capabilities.Movement;
using SlimeAI.GameOS.GodotBridge;
using SlimeAI.GameOS.Runtime.Entity;
using SlimeAI.GameOS.Runtime.Event;
using SlimeAI.GameOS.Runtime.Events.Core;
using SlimeAI.GameOS.Runtime.Timer;
using EffectSpawned = SlimeAI.GameOS.Capabilities.Effect.Events.Spawned;

namespace BrotatoLike.Game.VFX;

/// <summary>
/// BrotatoLike 链电专用 VFX 绑定器，把 Runtime effect source/target 端点绑定到 Line2D 场景。
/// </summary>
public partial class BrotatoLikeChainLightningVfxBinder : Node
{
    /// <summary>链电连线场景路径。</summary>
    public const string ChainLightningLineScenePath = "res://Scenes/VFX/LightningLineEffect.tscn";

    private readonly List<BrotatoLikeChainLightningLineVfxRecord> records = new();
    private IDisposable? effectSpawnedToken;
    private IDisposable? entityDestroyedToken;

    /// <summary>
    /// 本轮运行已绑定过的链电线段记录。
    /// </summary>
    public IReadOnlyList<BrotatoLikeChainLightningLineVfxRecord> Records => records;

    /// <inheritdoc />
    public override void _EnterTree()
    {
        Subscribe();
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        Unsubscribe();
    }

    /// <summary>
    /// 订阅 Effect 生成和销毁事件。
    /// </summary>
    public void Subscribe()
    {
        if (effectSpawnedToken != null)
        {
            return;
        }

        effectSpawnedToken = WorldEvents.World.Subscribe<EffectSpawned>(OnEffectSpawned);
        entityDestroyedToken = WorldEvents.World.Subscribe<EntityDestroyed>(OnEntityDestroyed);
    }

    /// <summary>
    /// 取消订阅 Runtime 事件。
    /// </summary>
    public void Unsubscribe()
    {
        effectSpawnedToken?.Dispose();
        entityDestroyedToken?.Dispose();
        effectSpawnedToken = null;
        entityDestroyedToken = null;
    }

    private void OnEffectSpawned(EffectSpawned data)
    {
        var scenePath = data.Effect.Data.Get(EffectDataKeys.ScenePath, string.Empty);
        if (!string.Equals(scenePath, ChainLightningLineScenePath, StringComparison.Ordinal))
        {
            return;
        }

        var sourceId = data.Effect.Data.Get<EntityId?>(EffectDataKeys.SourceEntity, data.Source.EntityId) ?? data.Source.EntityId;
        var targetId = data.Effect.Data.Get<EntityId?>(EffectDataKeys.TargetEntity, data.Target?.EntityId) ?? data.Target?.EntityId ?? EntityId.Empty;
        var source = EntityManager.Get(sourceId) ?? data.Source;
        var target = targetId.IsEmpty ? data.Target : EntityManager.Get(targetId) ?? data.Target;
        var start = ResolvePosition(source);
        var end = ResolvePosition(target);
        var duration = Math.Max(0.05f, data.Effect.Data.Get(EffectDataKeys.Duration, 0.2f));
        data.Effect.Data.Set(EffectDataKeys.Duration, duration);

        var record = new BrotatoLikeChainLightningLineVfxRecord
        {
            EffectEntityId = data.Effect.EntityId,
            SourceEntityId = sourceId,
            TargetEntityId = targetId,
            ScenePath = scenePath,
            StartPosition = start,
            EndPosition = end,
            DurationSeconds = duration
        };
        records.Add(record);

        BindLineNode(record);
        ScheduleCleanup(record);
    }

    private void BindLineNode(BrotatoLikeChainLightningLineVfxRecord record)
    {
        var node = GodotNodeRegistry.GetNodeById(record.EffectEntityId.Value);
        if (node is not LightningLineEffect line || !GodotObject.IsInstanceValid(line))
        {
            record.FailureReason = "LightningLineEffect node not found after effect spawn";
            return;
        }

        line.SetLine(ToGodot(record.StartPosition), ToGodot(record.EndPosition));
        line.Play(record.DurationSeconds);

        record.LocalPoints = line.Points;
        record.WorldPointStart = line.Points.Length > 0 ? line.ToGlobal(line.Points[0]) : Vector2.Zero;
        record.WorldPointEnd = line.Points.Length > 1 ? line.ToGlobal(line.Points[1]) : Vector2.Zero;
        record.Bound = line.Points.Length >= 2
            && record.WorldPointStart.DistanceTo(ToGodot(record.StartPosition)) <= 0.1f
            && record.WorldPointEnd.DistanceTo(ToGodot(record.EndPosition)) <= 0.1f
            && ToGodot(record.StartPosition).DistanceTo(ToGodot(record.EndPosition)) > 0.1f;
    }

    private static void ScheduleCleanup(BrotatoLikeChainLightningLineVfxRecord record)
    {
        TimerManager.Instance.Delay(record.DurationSeconds).OnComplete(() =>
        {
            if (EntityManager.Get(record.EffectEntityId) != null)
            {
                EntityManager.Destroy(record.EffectEntityId);
            }
        });
    }

    private void OnEntityDestroyed(EntityDestroyed data)
    {
        for (var i = 0; i < records.Count; i++)
        {
            var record = records[i];
            if (record.EffectEntityId != data.Entity.EntityId)
            {
                continue;
            }

            record.CleanupObserved = true;
            record.NodeRegisteredAfterCleanup = GodotNodeRegistry.GetNodeById(record.EffectEntityId.Value) != null;
        }
    }

    private static Vector2Value ResolvePosition(IEntity? entity)
    {
        return entity?.Data.Get(MovementDataKeys.Position, Vector2Value.Zero) ?? Vector2Value.Zero;
    }

    private static Vector2 ToGodot(Vector2Value value)
    {
        return new Vector2(value.X, value.Y);
    }
}

/// <summary>
/// 单段链电连线 VFX 的结构化验证记录。
/// </summary>
public sealed class BrotatoLikeChainLightningLineVfxRecord
{
    /// <summary>Effect Runtime EntityId。</summary>
    public EntityId EffectEntityId { get; init; }

    /// <summary>来源实体 Id。</summary>
    public EntityId SourceEntityId { get; init; }

    /// <summary>目标实体 Id。</summary>
    public EntityId TargetEntityId { get; init; }

    /// <summary>Line2D 场景路径。</summary>
    public string ScenePath { get; init; } = string.Empty;

    /// <summary>世界起点。</summary>
    public Vector2Value StartPosition { get; init; }

    /// <summary>世界终点。</summary>
    public Vector2Value EndPosition { get; init; }

    /// <summary>本地 Line2D points。</summary>
    public Vector2[] LocalPoints { get; set; } = Array.Empty<Vector2>();

    /// <summary>points[0] 转换后的世界坐标。</summary>
    public Vector2 WorldPointStart { get; set; }

    /// <summary>points[1] 转换后的世界坐标。</summary>
    public Vector2 WorldPointEnd { get; set; }

    /// <summary>持续时间。</summary>
    public float DurationSeconds { get; init; }

    /// <summary>端点是否成功绑定到非空线段。</summary>
    public bool Bound { get; set; }

    /// <summary>是否观察到对应 Runtime effect 销毁。</summary>
    public bool CleanupObserved { get; set; }

    /// <summary>清理事件后节点注册表是否仍保留该节点。</summary>
    public bool NodeRegisteredAfterCleanup { get; set; }

    /// <summary>绑定失败原因。</summary>
    public string FailureReason { get; set; } = string.Empty;
}
