using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 圆弧弹技能执行器 - 验证 CircularArc 运动模式
/// 在执行阶段自行选择最近敌人，并发射沿圆弧轨迹跟踪飞行的投射物。
/// </summary>
internal class ArcShotExecutor : AbilityFeatureHandler
{
    private static readonly Log _log = new(nameof(ArcShotExecutor));

    [ModuleInitializer]
    public static void Initialize()
    {
        FeatureHandlerRegistry.Register(new ArcShotExecutor());
    }

    public override string FeatureId => global::FeatureId.Ability.Projectile.ArcShot;

    protected override AbilityExecutedResult ExecuteAbility(CastContext context)
    {
        var caster = context.Caster!;
        var ability = context.Ability!;
        var casterNode = (Node2D)caster;
        var target = FindTarget(caster, ability, casterNode);
        if (target is not Node2D targetNode)
        {
            _log.Debug("圆弧弹执行跳过：未找到可用目标");
            return new AbilityExecutedResult { TargetsHit = 0 };
        }

        context.Targets = new List<IEntity>
        {
            target
        };

        var damage = ability.Data.Get<float>(DataKey.FinalAbilityDamage); // 最终技能伤害

        var projectileScenePath = ability.Data.Get<string>(DataKey.ProjectileScene); // 投射物场景路径

        var projectile = ProjectileTool.Spawn(
            caster, // 投射物归属者
            casterNode.GlobalPosition, // 生成位置
            projectileScenePath, // 投射物视觉路径
            "ArcShotProjectile" // 投射物名称
        );
        if (projectile == null) return new AbilityExecutedResult { TargetsHit = 0 };

        projectile.Events.Emit(
            GameEventType.Unit.MovementStarted,
            new GameEventType.Unit.MovementStartedEventData(
                MoveMode.CircularArc,
                new MovementParams
                {
                    Mode = MoveMode.CircularArc, // 运动模式
                    TargetNode = targetNode, // 目标节点
                    isTrackTarget = true, // 是否追踪目标
                    MaxDuration = 1.5f, // 运动时长,
                    CircularArcRadius = 220f, // 圆弧半径
                    CircularArcClockwise = true, // 圆弧方向
                    BowWorldUp = true, // 弓方向
                    DestroyOnComplete = true, // 完成销毁
                    RotateToVelocity = true, // 旋转到速度
                    ReachDistance = 20f, //到达阈值
                    OnStop = stopCtx => OnArcShotStop(stopCtx, caster, casterNode, target, damage) //停止回调
                }
            )
        );

        _log.Info($"圆弧弹: 跟踪目标={targetNode.Name}, 初始位置={targetNode.GlobalPosition}");
        return new AbilityExecutedResult { TargetsHit = 1 };
    }

    /// <summary>
    /// 在技能执行阶段选择本次追踪目标；无目标时让技能打空而不是阻断流水线。
    /// </summary>
    /// <param name="caster">施法实体。</param>
    /// <param name="ability">技能实体。</param>
    /// <param name="casterNode">施法者节点。</param>
    /// <returns>最近敌方目标，找不到则返回 null。</returns>
    private static IEntity? FindTarget(IEntity caster, AbilityEntity ability, Node2D casterNode)
    {
        float castRange = ability.Data.Get<float>(DataKey.AbilityCastRange); //索敌半径

        var targets = EntityTargetSelector.Query(new TargetSelectorQuery
        {
            Geometry = GeometryType.Circle, //查询形状
            Origin = casterNode.GlobalPosition, //查询中心
            Range = castRange, //查询半径
            CenterEntity = caster, //中心实体
            TeamFilter = TeamFilter.Enemy, //阵营过滤
            Sorting = TargetSorting.HighestThreat, //排序方式
            MaxTargets = 1 //最大目标数
        });

        return targets.Count > 0 ? targets[0] : null;
    }

    private static void OnArcShotStop(
        MovementStopContext stopCtx,
        IEntity caster,
        Node2D casterNode,
        IEntity target,
        float damage)
    {
        if (stopCtx.Reason != MovementStopReason.Completed) return;
        if (target is not Node2D targetNode) return;
        if (!GodotObject.IsInstanceValid(targetNode)) return;
        if (target.Data.Has(DataKey.IsDead) && target.Data.Get<bool>(DataKey.IsDead)) return;

        var targetEntity = target;
        if (!AbilityTool.MatchesTeamFilter(caster, targetEntity, TeamFilter.Enemy)) return;

        AbilityImpactTool.Execute(caster, new AbilityImpactOptions
        {
            Targets = new[] { targetEntity },
            Damage = new DamageApplyOptions
            {
                Damage = damage, // 伤害值
                Type = DamageType.Physical, // 伤害类型
                Tags = DamageTags.Attack, // 伤害标签
                Attacker = casterNode // 伤害来源
            }
        });
    }
}
