using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 正弦波弹技能执行器 - 验证 SineWave 运动模式
/// 在施法者周围随机取一点作为初始方向，发射一枚正弦波前进的投射物
/// </summary>
internal class SineWaveShotExecutor : AbilityFeatureHandler
{
    private static readonly Log _log = new(nameof(SineWaveShotExecutor));

    [ModuleInitializer]
    public static void Initialize()
    {
        FeatureHandlerRegistry.Register(new SineWaveShotExecutor());
    }

    public override string FeatureId => global::FeatureId.Ability.Projectile.SineWaveShot;

    /// <summary>
    /// 执行正弦波弹技能：先随机采样方向，再生成 1 枚正弦波投射物并挂接碰撞伤害。
    /// </summary>
    protected override AbilityExecutedResult ExecuteAbility(CastContext context)
    {
        var caster = context.Caster!;
        var ability = context.Ability!;
        var casterNode = (Node2D)caster;

        var damage = ability.Data.Get<float>(DataKey.FinalAbilityDamage); // 最终技能伤害

        // 正弦波弹不再朝最近敌人开火，而是在施法者周围随机取一点决定出射方向。
        Vector2 directionPoint = GetRandomDirectionPoint(casterNode); // 随机方向采样点
        Vector2 rawDirection = directionPoint - casterNode.GlobalPosition; // 原始朝向向量
        Vector2 dir = rawDirection.LengthSquared() >= 0.001f
            ? rawDirection.Normalized()
            : Vector2.Right;
        Vector2 spawnPos = AbilityTool.ResolveSpawnPosition(
            casterNode.GlobalPosition, // 原始生成点
            dir, // 初始朝向
            20f // 出生前推距离
        );
        var projectileScenePath = ability.Data.Get<string>(DataKey.ProjectileScene); // 投射物场景路径

        var projectile = ProjectileTool.Spawn(
            caster, // 投射物归属者
            spawnPos, // 生成位置
            projectileScenePath, // 投射物视觉路径
            "SineWaveShotProjectile" // 投射物名称
        );
        if (projectile == null) return new AbilityExecutedResult { TargetsHit = 0 };

        projectile.Events.Emit(
            GameEventType.Unit.MovementStarted, // 开始移动事件
            new GameEventType.Unit.MovementStartedEventData(
                MoveMode.SineWave, // 移动模式：正弦波
                new MovementParams
                {
                    Mode = MoveMode.SineWave, // 移动模式
                    Angle = Mathf.RadToDeg(dir.Angle()), // 初始朝向角度（度）
                    ActionSpeed = 350f, // 前进速度
                    WaveAmplitude = 60f, // 波浪振幅
                    WaveFrequency = 2f, // 波浪频率
                    MaxDistance = 1800f, // 最大飞行距离
                    DestroyOnComplete = true, // 正常飞完后销毁
                    CollisionParams = new MovementCollisionParams
                    {
                        TeamFilter = TeamFilter.Enemy, // 阵营过滤：只接受敌方
                        EntityTypeFilter = EntityType.Unit, // 实体类型过滤：只接受单位
                        StopAfterCollisionCount = -1, // 首个有效碰撞后停止
                        // DestroyOnStop = true, // 停止时同步销毁投射物
                        OnCollision = collisionCtx => OnHit(collisionCtx, caster, casterNode, damage) // 命中回调
                    },
                    RotateToVelocity = true, // 朝速度方向旋转视觉
                }
            )
        );

        _log.Info($"正弦波弹: 采样点={directionPoint}, 方向={dir}, 角度={Mathf.RadToDeg(dir.Angle()):F1}°");
        return new AbilityExecutedResult { TargetsHit = 1 }; // 单发正弦波弹成功生成
    }

    /// <summary>
    /// 在施法者周围圆环内随机采样一点，用作正弦波弹的初始朝向。
    /// </summary>
    private static Vector2 GetRandomDirectionPoint(
        Node2D casterNode) // 施法者节点
    {
        var points = PositionTargetSelector.Query(new TargetSelectorQuery
        {
            Geometry = GeometryType.Ring, // 查询形状
            Origin = casterNode.GlobalPosition, // 查询中心
            InnerRange = 120f, // 随机方向最小半径
            Range = 260f, // 随机方向最大半径
            MaxTargets = 1 // 最大目标数
        });

        if (points.Count > 0)
        {
            return points[0];
        }

        return casterNode.GlobalPosition + Vector2.Right * 160f;
    }

    /// <summary>
    /// 正弦波弹命中时立即结算一次伤害；运动系统负责停下和销毁投射物。
    /// </summary>
    private static void OnHit(
        MovementCollisionContext collisionCtx, // 碰撞上下文
        IEntity caster, // 技能施法者
        Node2D casterNode, // 施法者节点
        float damage) // 最终伤害值
    {
        if (collisionCtx.TargetEntity == null) return; // 没有实体目标时不结算伤害
        var targetEntity = collisionCtx.TargetEntity; // 命中的实体目标
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
    // 正弦波技能只负责发射和单次命中伤害，停止与回收统一交给 Movement 系统处理。
}
