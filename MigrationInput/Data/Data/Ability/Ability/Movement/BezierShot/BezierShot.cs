using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 贝塞尔曲线弹技能执行器 - 验证 BezierCurve 运动模式
/// 一次生成 5 发追踪单位的五阶贝塞尔投射物，形成“先散开再收束”的包夹轨迹
/// </summary>
internal class BezierShotExecutor : AbilityFeatureHandler
{
    private static readonly Log _log = new(nameof(BezierShotExecutor));
    private const float DefaultFallbackRange = 600f;
    private const int ProjectileCount = 5;
    private const int BezierDegree = 5;
    private const float ProjectileSpeed = 420f;
    private const float MinTravelDuration = 0.85f;
    private const float MaxTravelDuration = 1.45f;
    private const BezierPatternType BezierPattern = BezierPatternType.Converge;

    [ModuleInitializer]
    public static void Initialize()
    {
        FeatureHandlerRegistry.Register(new BezierShotExecutor());
    }

    public override string FeatureId => global::FeatureId.Ability.Projectile.BezierShot;

    /// <summary>
    /// 执行贝塞尔弹技能：一次生成 5 发五阶贝塞尔追踪弹，运动完成后对锁定目标直接结算伤害。
    /// </summary>
    protected override AbilityExecutedResult ExecuteAbility(CastContext context)
    {
        var caster = context.Caster!;
        var ability = context.Ability!;
        var casterNode = (Node2D)caster;

        var damage = ability.Data.Get<float>(DataKey.FinalAbilityDamage); // 最终技能伤害

        Vector2 startPos = casterNode.GlobalPosition;
        var targetInfo = GetTargetUnit(
            caster, // 技能施法者
            casterNode, // 施法者节点
            ability.Data.Get<float>(DataKey.AbilityCastRange) // 技能施法距离
        );
        if (targetInfo == null)
        {
            _log.Warn("贝塞尔弹: 未找到可追踪的敌方单位，跳过本次释放");
            return new AbilityExecutedResult { TargetsHit = 0 };
        }

        var (targetEntity, targetNode) = targetInfo.Value;
        Vector2 targetPos = targetNode.GlobalPosition;
        var projectileScenePath = ability.Data.Get<string>(DataKey.ProjectileScene); // 投射物场景路径
        int spawnedCount = 0;

        int randomSeed = Mathf.RoundToInt(startPos.X * 7f + startPos.Y * 11f + targetPos.X * 13f + targetPos.Y * 17f); // 基于当前战场位置生成稳定随机种子

        // 贝塞尔弹固定生成 5 发，统一使用更收敛的 Converge 模板，再按序号分配多发车道与扰动。
        for (int i = 0; i < ProjectileCount; i++)
        {
            BezierCurveTemplate template = BezierTemplateBuilder.CreatePattern(
                BezierDegree, // 五阶模板
                BezierPattern, // 当前模板模式
                i, // 当前投射物序号
                ProjectileCount, // 总发数
                randomSeed); // 随机种子
            Vector2[] initialPoints = BezierTemplateBuilder.CreatePatternPoints(
                startPos, // 起始位置
                targetPos, // 初始目标位置
                BezierDegree, // 五阶模板
                BezierPattern, // 当前模板模式
                i, // 当前投射物序号
                ProjectileCount, // 总发数
                randomSeed); // 随机种子
            Vector2 spawnPos = AbilityTool.ResolveSpawnPosition(
                startPos, // 原始生成点
                initialPoints[1] - initialPoints[0], // 朝首个控制点方向前推
                20f // 出生前推距离
            );
            float travelDuration = Mathf.Clamp(
                spawnPos.DistanceTo(targetPos) / ProjectileSpeed, // 用期望速度推导飞行时长
                MinTravelDuration, // 最小时长
                MaxTravelDuration); // 最大时长

            var projectile = ProjectileTool.Spawn(
                caster, // 投射物归属者
                spawnPos, // 生成位置
                projectileScenePath, // 投射物视觉路径
                $"BezierShotProjectile_{i}" // 投射物名称
            );
            if (projectile == null) continue;

            projectile.Events.Emit(
                GameEventType.Unit.MovementStarted, // 开始移动事件
                new GameEventType.Unit.MovementStartedEventData(
                    MoveMode.BezierCurve, // 移动模式：贝塞尔曲线
                    new MovementParams
                    {
                        Mode = MoveMode.BezierCurve, // 移动模式
                        MaxDuration = travelDuration, // 曲线飞行总时长
                        TargetPoint = targetPos, // 初始终点
                        isTrackTarget = true, // 终点实时追踪目标
                        TargetNode = targetNode, // 被追踪的目标节点
                        ReachDistance = 18f, // 接近目标的完成阈值
                        BezierTemplate = template, // 五阶模板控制点
                        DestroyOnComplete = true, // 到达后销毁
                        OnStop = stopContext => OnMoveCompleted(
                            stopContext, // 运动停止上下文
                            caster, // 技能施法者
                            casterNode, // 施法者节点
                            targetEntity, // 被锁定的目标实体
                            targetNode, // 被锁定的目标节点
                            damage // 最终伤害值
                        ),
                        RotateToVelocity = true, // 旋转朝向速度
                    }
                )
            );

            spawnedCount++;
        }

        _log.Info($"贝塞尔弹: 起点={startPos}, 目标={targetPos}, 成功生成={spawnedCount}");
        return new AbilityExecutedResult { TargetsHit = spawnedCount }; // 返回实际生成数量
    }

    /// <summary>
    /// 查找最近敌方单位作为贝塞尔终点，返回实体和节点，供追踪与结算复用。
    /// </summary>
    private static (IEntity Entity, Node2D Node)? GetTargetUnit(
        IEntity caster, // 技能施法者
        Node2D casterNode, // 施法者节点
        float castRange) // 技能施法距离
    {
        float effectiveRange = castRange > 0f ? castRange : DefaultFallbackRange; // 查询半径
        var targets = EntityTargetSelector.Query(new TargetSelectorQuery
        {
            Geometry = GeometryType.Circle, // 查询形状
            Origin = casterNode.GlobalPosition, // 查询中心
            Range = effectiveRange, // 查询半径
            CenterEntity = caster, // 中心实体
            TeamFilter = TeamFilter.Enemy, // 阵营过滤
            Sorting = TargetSorting.Nearest, // 排序方式：最近敌人
            MaxTargets = 1 // 最大目标数
        });
        if (targets.Count > 0 && targets[0] is Node2D targetNode)
        {
            return (targets[0], targetNode);
        }

        return null;
    }

    /// <summary>
    /// 贝塞尔弹在曲线运动自然完成时结算伤害；中途打断或目标失效时直接跳过。
    /// </summary>
    private static void OnMoveCompleted(
        MovementStopContext stopContext, // 运动停止上下文
        IEntity caster, // 技能施法者
        Node2D casterNode, // 施法者节点
        IEntity targetEntity, // 被锁定的目标实体
        Node2D targetNode, // 被锁定的目标节点
        float damage) // 最终伤害值
    {
        if (stopContext.Reason != MovementStopReason.Completed) return; // 只有自然完成才结算伤害
        if (!GodotObject.IsInstanceValid(targetNode)) return; // 目标节点已失效时不结算伤害
        if (!AbilityTool.MatchesTeamFilter(caster, targetEntity, TeamFilter.Enemy)) return;

        AbilityImpactTool.Execute(caster, new AbilityImpactOptions
        {
            Targets = new[] { targetEntity },
            Damage = new DamageApplyOptions
            {
                Damage = damage, // 伤害值
                Type = DamageType.Physical, // 伤害类型
                Tags = DamageTags.Ability | DamageTags.Ranged, // 伤害标签
                Attacker = casterNode // 伤害来源
            }
        });
    }
    // 贝塞尔技能只维护锁敌、发射和完成结算，5 发投射物的停止与销毁统一交给 Movement 系统处理。
}
