using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 定点抛炸弹技能执行器 - 使用 Arc 轨迹模拟 2D 抛投效果
/// 周期性向固定落点抛出一枚炸弹，炸弹落地后造成范围伤害
/// </summary>
internal class ParabolaBombardmentExecutor : AbilityFeatureHandler
{
    private static readonly Log _log = new(nameof(ParabolaBombardmentExecutor));
    private const float DefaultFallbackRange = 700f;
    private const float ProjectileSpeed = 380f;
    private const float MinTravelDuration = 0.75f;
    private const float MaxTravelDuration = 1.35f;

    [ModuleInitializer]
    public static void Initialize()
    {
        FeatureHandlerRegistry.Register(new ParabolaBombardmentExecutor());
    }

    public override string FeatureId => global::FeatureId.Ability.Projectile.ParabolaBombardment;

    /// <summary>
    /// 执行定点抛炸弹技能：在施法者周围圆形范围内随机选择落点，生成 1 枚沿圆弧飞行的炸弹，并在落地时造成范围伤害与特效。
    /// </summary>
    protected override AbilityExecutedResult ExecuteAbility(CastContext context)
    {
        var caster = context.Caster!;
        var ability = context.Ability!;
        var casterNode = (Node2D)caster;

        var damage = ability.Data.Get<float>(DataKey.FinalAbilityDamage); // 最终技能伤害
        var targetPoint = GetBombardTargetPoint(
            caster, // 技能施法者
            casterNode, // 施法者节点
            ability.Data.Get<float>(DataKey.AbilityCastRange) // 技能施法距离
        );
        float effectRadius = ability.Data.Get<float>(DataKey.AbilityEffectRadius); // 爆炸半径
        var effectScenePath = ability.Data.Get<string>(DataKey.EffectScene); // 爆炸特效路径
        Vector2 rawDirection = targetPoint - casterNode.GlobalPosition; // 起点指向落点的向量
        Vector2 travelDirection = rawDirection.LengthSquared() >= 0.001f
            ? rawDirection.Normalized()
            : Vector2.Right;
        Vector2 spawnPos = AbilityTool.ResolveSpawnPosition(
            casterNode.GlobalPosition, // 原始生成点
            travelDirection, // 初始朝向
            20f // 出生前推距离
        );
        float travelDistance = spawnPos.DistanceTo(targetPoint); // 飞行距离
        float travelDuration = Mathf.Clamp(
            travelDistance / ProjectileSpeed, // 用期望速度推导飞行时长
            MinTravelDuration, // 最小时长
            MaxTravelDuration); // 最大时长
        float arcRadius = Mathf.Max(
            travelDistance * 0.72f, // 常规弧线半径
            travelDistance * 0.5f + 32f); // 保证半径始终大于半弦长
        var projectileScenePath = ability.Data.Get<string>(DataKey.ProjectileScene); // 投射物场景路径

        var projectile = ProjectileTool.Spawn(
            caster, // 投射物归属者
            spawnPos, // 生成位置
            projectileScenePath, // 投射物视觉路径
            "ParabolaShotProjectile" // 投射物名称
        );
        if (projectile == null) return new AbilityExecutedResult { TargetsHit = 0 };

        projectile.Events.Emit(
            GameEventType.Unit.MovementStarted, // 开始移动事件
            new GameEventType.Unit.MovementStartedEventData(
                MoveMode.CircularArc, // 移动模式：圆弧
                new MovementParams
                {
                    Mode = MoveMode.CircularArc, // 移动模式
                    TargetPoint = targetPoint, // 固定落点
                    ActionSpeed = ProjectileSpeed, // 沿弧线推进速度
                    MaxDuration = travelDuration, // 飞行总时长
                    CircularArcRadius = arcRadius, // 圆弧半径
                    CircularArcClockwise = false, // 默认方向，BowWorldUp 开启后仅作平手回退
                    BowWorldUp = true, // 固定朝世界上方拱起
                    ReachDistance = 5f, // 接近落点的完成阈值
                    DestroyOnComplete = true, // 正常到点后销毁
                    OnStop = stopContext => OnMoveCompleted(
                        stopContext, // 运动停止上下文
                        caster, // 技能施法者
                        casterNode, // 施法者节点
                        targetPoint, // 固定落点
                        effectRadius, // 爆炸半径
                        effectScenePath, // 爆炸特效路径
                        damage // 最终伤害值
                    ),
                    RotateToVelocity = true, // 朝速度方向旋转视觉
                }
            )
        );

        _log.Info($"定点抛炸弹: 生成点={spawnPos}, 落点={targetPoint}, 圆弧半径={arcRadius:F1}, 时长={travelDuration:F2}s");
        return new AbilityExecutedResult { TargetsHit = 1 }; // 单发炸弹成功生成
    }

    /// <summary>
    /// 解析炸弹落点：以施法者为圆心，在施法范围圆内随机选择一个固定落点。
    /// </summary>
    private static Vector2 GetBombardTargetPoint(
        IEntity caster, // 技能施法者
        Node2D casterNode, // 施法者节点
        float castRange) // 技能施法距离
    {
        _ = caster; // 当前随机选点逻辑只依赖施法者位置，保留参数以维持既有方法签名。
        float effectiveRange = castRange > 0f ? castRange : DefaultFallbackRange; // 随机选点半径
        return GeometryCalculator.GetRandomPointInCircle(
            casterNode.GlobalPosition, // 圆心
            effectiveRange // 半径
        );
    }

    /// <summary>
    /// 炸弹在落地完成时结算范围伤害，并在落点生成一次独立特效。
    /// </summary>
    private static void OnMoveCompleted(
        MovementStopContext stopContext, // 运动停止上下文
        IEntity caster, // 技能施法者
        Node2D casterNode, // 施法者节点
        Vector2 targetPoint, // 固定落点
        float effectRadius, // 爆炸半径
        string effectScenePath, // 爆炸特效路径
        float damage) // 最终伤害值
    {
        if (stopContext.Reason != MovementStopReason.Completed) return; // 只有自然完成才结算伤害

        AbilityImpactTool.Execute(caster, new AbilityImpactOptions
        {
            Query = new TargetSelectorQuery
            {
                Geometry = GeometryType.Circle, // 查询形状
                Origin = targetPoint, // 爆炸中心
                Range = effectRadius, // 爆炸半径
                CenterEntity = caster, // 阵营判断基准
                TeamFilter = TeamFilter.Enemy, // 阵营过滤
                MaxTargets = -1 // 不限命中数量
            },
            Effect = !string.IsNullOrWhiteSpace(effectScenePath)
                ? new EffectSpawnOptions(
                    effectScenePath, // 特效场景路径
                    Name: "定点抛炸弹爆炸特效", // 特效名称
                    Owner: caster, // 特效归属者
                    EffectPosition: targetPoint) // 特效位置
                : null,
            Damage = new DamageApplyOptions
            {
                Damage = damage, // 伤害值
                Type = DamageType.Physical, // 伤害类型
                Tags = DamageTags.Ability | DamageTags.Ranged | DamageTags.Area, // 伤害标签
                Attacker = casterNode // 伤害来源
            }
        });
    }

    // 定点抛炸弹技能只负责取落点、发射和落地结算；投射物回收统一交给 Movement 系统处理。
}
