using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 环绕护盾技能执行器 - 验证 Orbit 运动模式
/// 生成多个投射物围绕玩家旋转，碰撞敌人时造成伤害
/// </summary>
internal class OrbitSkillExecutor : AbilityFeatureHandler
{
    private static readonly Log _log = new(nameof(OrbitSkillExecutor));

    [ModuleInitializer]
    public static void Initialize()
    {
        FeatureHandlerRegistry.Register(new OrbitSkillExecutor());
    }

    public override string FeatureId => global::FeatureId.Ability.Passive.OrbitSkill;

    protected override AbilityExecutedResult ExecuteAbility(CastContext context)
    {
        var caster = context.Caster!;
        var ability = context.Ability!;
        if (caster is not Node2D casterNode)
        {
            _log.Warn("环绕护盾施法失败：施法者不是 Node2D");
            return new AbilityExecutedResult();
        }

        var damage = ability.Data.Get<float>(DataKey.FinalAbilityDamage); // 最终技能伤害
        var orbitCount = 3;
        var orbitRadius = 100f;
        var orbitDuration = 6f;
        var projectileScenePath = ability.Data.Get<string>(DataKey.ProjectileScene); // 投射物场景路径

        for (int i = 0; i < orbitCount; i++)
        {
            float initAngle = i * (360f / orbitCount);
            var projectile = ProjectileTool.Spawn(
                caster, // 投射物归属者
                casterNode.GlobalPosition, // 生成位置
                projectileScenePath, // 投射物视觉路径
                "OrbitSkillProjectile" // 投射物名称
            );
            if (projectile == null) continue;

            projectile.Events.Emit(
                GameEventType.Unit.MovementStarted,
                new GameEventType.Unit.MovementStartedEventData(
                    MoveMode.Orbit,
                    new MovementParams
                    {
                        Mode = MoveMode.Orbit,
                        TargetNode = casterNode, //环绕中心：施法者
                        OrbitRadius = orbitRadius, //环绕半径
                        OrbitInitAngle = initAngle, //初始角度（度）
                        OrbitAngularSpeed = 180f, //角速度（度/秒）
                        IsOrbitClockwise = true, //顺时针环绕
                        MaxDuration = orbitDuration, //最大持续时间
                        DestroyOnComplete = true, //到期后销毁投射物
                        CollisionParams = new MovementCollisionParams
                        {
                            TeamFilter = TeamFilter.Enemy, //阵营过滤
                            EntityTypeFilter = EntityType.Unit, //实体类型过滤
                            StopAfterCollisionCount = -1, //不限制碰撞次数，只通知不停止
                            DestroyOnStop = false, //不因碰撞销毁
                            OnCollision = collisionCtx => OnHit(collisionCtx, caster, casterNode, damage) //命中回调
                        },
                        RotateToVelocity = false, //不朝向运动方向旋转
                    }
                )
            );
        }

        _log.Info($"环绕护盾: 生成 {orbitCount} 个轨道投射物");
        return new AbilityExecutedResult { TargetsHit = orbitCount };
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
                Type = DamageType.Magical, // 伤害类型
                Tags = DamageTags.Area | DamageTags.Ability, // 伤害标签
                Attacker = casterNode // 伤害来源
            }
        });
    }
}
