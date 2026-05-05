using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 回旋镖技能执行器 - 验证 Boomerang 运动模式
/// 在施法者周围圆环内随机选择一个去程目标点，投掷回旋镖后自动返回，过程中碰撞造成伤害
/// </summary>
internal class BoomerangThrowExecutor : AbilityFeatureHandler
{
    private static readonly Log _log = new(nameof(BoomerangThrowExecutor));
    private const float DefaultFallbackRange = 800f;
    private const float InnerRingRatio = 0.45f;
    private const float FallbackForwardDistance = 320f;

    [ModuleInitializer]
    public static void Initialize()
    {
        FeatureHandlerRegistry.Register(new BoomerangThrowExecutor());
    }

    public override string FeatureId => global::FeatureId.Ability.Projectile.BoomerangThrow;

    protected override AbilityExecutedResult ExecuteAbility(CastContext context)
    {
        var caster = context.Caster!;
        var ability = context.Ability!;
        var casterNode = (Node2D)caster;

        var damage = ability.Data.Get<float>(DataKey.FinalAbilityDamage); // 最终技能伤害

        var throwTarget = GetThrowTarget(caster, casterNode, ability.Data.Get<float>(DataKey.AbilityCastRange));
        var projectileScenePath = ability.Data.Get<string>(DataKey.ProjectileScene); // 投射物场景路径

        var projectile = ProjectileTool.Spawn(
            caster, // 投射物归属者
            casterNode.GlobalPosition, // 生成位置
            projectileScenePath, // 投射物视觉路径
            "BoomerangThrowProjectile" // 投射物名称
        );
        if (projectile == null) return new AbilityExecutedResult { TargetsHit = 0 };

        projectile.Events.Emit(
            GameEventType.Unit.MovementStarted,
            new GameEventType.Unit.MovementStartedEventData(
                MoveMode.Boomerang,
                new MovementParams
                {
                    Mode = MoveMode.Boomerang,
                    TargetPoint = throwTarget, // 去程随机目标点
                    TargetNode = casterNode, // 显式指定返程宿主，避免策略退回到不可靠的祖先回溯
                    ActionSpeed = 460f, // 去程基础速度
                    BoomerangArcHeight = 160f, // 更大的轨迹弧高
                    BoomerangPauseTime = 0.05f, // 顶点轻微停顿
                    BoomerangIsClockwise = true, // 去程默认顺时针鼓包
                    BoomerangReturnSpeedMultiplier = 1.35f, // 回程更快，强调收回感
                    DestroyOnComplete = true, // 完成后回收
                    CollisionParams = new MovementCollisionParams
                    {
                        TeamFilter = TeamFilter.Enemy, //阵营过滤
                        EntityTypeFilter = EntityType.Unit, //实体类型过滤
                        StopAfterCollisionCount = -1, //只通知不停止
                        DestroyOnStop = false, //不因碰撞销毁
                        OnCollision = collisionCtx => OnHit(collisionCtx, caster, casterNode, damage) //命中回调
                    },
                    RotateToVelocity = false, // root 最终旋转改由朝向组件接管
                    Orientation = new OrientationParams
                    {
                        Mode = OrientationMode.FollowMovementAndSpin, // 跟随轨迹切线并叠加自转
                        AngularSpeed = 540f, // 自转角速度
                        AngularAcceleration = 0f, // 匀速自转
                        TotalAngle = -1f, // 不限制总自转角
                        InitialAngle = 0f, // 初始偏移角
                        IsClockwise = true, // 顺时针自转
                    },
                }
            )
        );

        _log.Info($"回旋镖投掷: 目标={throwTarget}");
        return new AbilityExecutedResult { TargetsHit = 1 };
    }

    private static Vector2 GetThrowTarget(IEntity caster, Node2D casterNode, float castRange)
    {
        _ = caster; // 当前随机落点逻辑只依赖施法者位置，保留参数以兼容既有测试签名
        float effectiveRange = castRange > 0f ? castRange : DefaultFallbackRange; // 外半径
        float innerRange = effectiveRange * InnerRingRatio; // 内半径，避免目标点离自己过近
        var targets = PositionTargetSelector.Query(new TargetSelectorQuery
        {
            Geometry = GeometryType.Ring, //圆环
            Origin = casterNode.GlobalPosition, //查询中心
            InnerRange = innerRange, //内半径
            Range = effectiveRange, //外半径
            MaxTargets = 1 //最大目标数
        });
        if (targets.Count > 0)
            return targets[0];
        return casterNode.GlobalPosition + Vector2.Right * FallbackForwardDistance;
    }

    private static void OnHit(MovementCollisionContext collisionCtx,
        IEntity caster,
        Node2D casterNode,
        float damage)
    {
        if (collisionCtx.TargetEntity == null) return;
        var targetEntity = collisionCtx.TargetEntity;
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
}
