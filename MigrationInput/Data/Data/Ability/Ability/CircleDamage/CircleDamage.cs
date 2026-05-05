using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 烈焰光环技能执行器
/// 
/// 触发方式：Periodic（周期触发，由 TriggerComponent 每隔 AbilityCooldown 秒自动执行）
/// 目标选择：None（自动对施法者周围圆形范围内所有敌人）
/// 特效：Effect_003（每次触发在施法者位置播放一次独立特效）
/// 伤害：魔法伤害，带 Area 标签；当前未配置 TickInterval/TotalDuration，因此重复触发仅来自 TriggerComponent 的 Periodic 轴
/// </summary>
internal class CircleDamageExecutor : AbilityFeatureHandler
{
    private static readonly Log _log = new(nameof(CircleDamageExecutor));

    [ModuleInitializer]
    public static void Initialize()
    {
        FeatureHandlerRegistry.Register(new CircleDamageExecutor());
    }

    public override string FeatureId => global::FeatureId.Ability.Passive.CircleDamage;

    protected override AbilityExecutedResult ExecuteAbility(CastContext context)
    {
        var caster = context.Caster!;
        var ability = context.Ability!;
        var casterNode2D = (Node2D)caster;

        // 从技能配置中读取参数
        var range = ability.Data.Get<float>(DataKey.AbilityEffectRadius); // 伤害半径
        var effectScenePath = ability.Data.Get<string>(DataKey.EffectScene); // 特效场景路径
        var damage = ability.Data.Get<float>(DataKey.FinalAbilityDamage); // 技能最终伤害

        // 执行命中（目标查询 + 特效 + 伤害，三步合一；位置来源统一收口到 Query / Effect 参数中）
        var result = AbilityImpactTool.Execute(caster, new AbilityImpactOptions
        {
            Query = new TargetSelectorQuery
            {
                Geometry = GeometryType.Circle, // 圆形范围，以施法者为圆心
                Origin = casterNode2D.GlobalPosition, // 初始位置
                OriginProvider = () => casterNode2D.GlobalPosition, // DoT tick 时持续跟随施法者当前位置
                Range = range, // 查询半径
                CenterEntity = caster, // 阵营判断基准
                TeamFilter = TeamFilter.Enemy, // 阵营过滤
                MaxTargets = -1 // 不限命中数量
            },
            Effect = !string.IsNullOrWhiteSpace(effectScenePath)
                ? new EffectSpawnOptions(
                    effectScenePath,
                    Name: "烈焰光环特效",
                    Scale: new Vector2(2.0f, 2.0f))
                : null,
            Damage = new DamageApplyOptions
            {
                Damage = damage, // 技能最终伤害
                Type = DamageType.Magical, // 魔法伤害
                Tags = DamageTags.Area | DamageTags.Ability, // 范围技能标签
                Attacker = casterNode2D, // 伤害来源
            }
        });

        _log.Info($"[烈焰光环] 触发! 范围: {range}, 命中: {result.TargetsHit}");
        return new AbilityExecutedResult { TargetsHit = result.TargetsHit };
    }
}
