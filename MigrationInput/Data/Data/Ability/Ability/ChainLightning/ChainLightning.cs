using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 链式弹跳上下文 - 减少方法传递参数数量
/// </summary>
public readonly record struct ChainBounceContext(
    IEntity Caster,
    float Range,
    float Delay,
    float DamageDecay,
    TeamFilter TeamFilter,
    TargetSorting Sorting,
    string LineScenePath
);

/// <summary>
/// 链式闪电技能执行器 - 使用 Chain 执行模式
/// 
/// 触发方式：Manual（手动，玩家按键）
/// 执行模式：Chain（延时弹跳）
/// 目标选择：Entity（Circle 几何查询初始目标）
/// 特效：在每个被命中目标位置生成独立特效
/// 伤害：魔法伤害，每次弹跳衰减
/// </summary>
internal class ChainLightningExecutor : AbilityFeatureHandler
{
    private static readonly Log _log = new(nameof(ChainLightningExecutor));

    [ModuleInitializer]
    public static void Initialize()
    {
        FeatureHandlerRegistry.Register(new ChainLightningExecutor());
    }

    /// <summary>
    /// 执行技能逻辑
    /// </summary>
    /// <param name="context">施法上下文，包含施法者、技能对象、初始目标等</param>
    /// <returns>返回执行结果，主要包含命中目标数</returns>
    public override string FeatureId => global::FeatureId.Ability.Active.ChainLightning;

    protected override AbilityExecutedResult ExecuteAbility(CastContext context)
    {
        var caster = context.Caster!;
        var ability = context.Ability!;

        var firstTarget = FindInitialTarget(caster, ability);
        if (firstTarget == null)
        {
            _log.Info("链式闪电执行跳过：没有有效目标");
            return new AbilityExecutedResult();
        }

        context.Targets = new List<IEntity>
        {
            firstTarget
        };

        var chainCount = ability.Data.Get<int>(DataKey.AbilityChainCount);
        if (chainCount <= 0) chainCount = 1;

        // 计算初始伤害：技能基础伤害 × 施法者技能伤害倍率
        var initialDamage = ability.Data.Get<float>(DataKey.FinalAbilityDamage);

        var bounceContext = new ChainBounceContext
        {
            Caster = caster, // 技能施法者
            Delay = ability.Data.Get<float>(DataKey.AbilityChainDelay), // 技能链式延迟
            Range = ability.Data.Get<float>(DataKey.AbilityChainRange), // 技能链式范围
            DamageDecay = ability.Data.Get<float>(DataKey.AbilityChainDamageDecay) / 100f, // 技能链式伤害衰减
            TeamFilter = TeamFilter.Enemy, // 技能链式目标阵营
            Sorting = TargetSorting.Nearest, // 技能链式目标排序
            LineScenePath = ability.Data.Get<string>(DataKey.AbilityChainLineEffect) // 技能链式特效路径
        };

        _log.Debug(
            $"[Execute起步] 目标数: 1, 施法者: {(caster as Node)?.Name}, 第一目标: {(firstTarget as Node)?.Name}, 第一目标类型: {firstTarget.GetType().Name}, 第一目标是否死亡: {firstTarget.Data.Get<bool>(DataKey.IsDead)}");

        // 发起第一击
        ExecuteBounce(bounceContext, caster, firstTarget, initialDamage, chainCount, new HashSet<IEntity>());

        // 立即返回（后续弹跳通过 Timer 异步执行）
        return new AbilityExecutedResult { TargetsHit = 1 };
    }

    /// <summary>
    /// 在技能执行阶段选择链式闪电的第一跳目标。
    /// </summary>
    /// <param name="caster">施法实体。</param>
    /// <param name="ability">技能实体。</param>
    /// <returns>第一跳敌方目标，找不到则返回 null。</returns>
    private static IEntity? FindInitialTarget(IEntity caster, AbilityEntity ability)
    {
        if (caster is not Node2D casterNode) return null;

        float castRange = ability.Data.Get<float>(DataKey.AbilityCastRange); //索敌半径
        if (castRange <= 0f) return null;

        var targets = EntityTargetSelector.Query(new TargetSelectorQuery
        {
            Geometry = GeometryType.Circle, //查询形状
            Origin = casterNode.GlobalPosition, //查询中心
            Range = castRange, //查询半径
            CenterEntity = caster, //中心实体
            TeamFilter = TeamFilter.Enemy, //阵营过滤
            Sorting = TargetSorting.Nearest, //排序方式
            MaxTargets = 1 //最大目标数
        });

        return targets.Count > 0 ? targets[0] : null;
    }

    /// <summary>
    /// 执行一次闪电弹跳（造成伤害、画特效，并预定下一次弹跳）
    /// </summary>
    private void ExecuteBounce(
        ChainBounceContext context,
        IEntity fromEntity,
        IEntity currentTarget,
        float currentDamage,
        int remainingBounces,
        HashSet<IEntity> hitTargets)
    {
        // 1. 获取可靠的起点坐标（防止 fromEntity 此时已经回到了对象池）
        Vector2 fromPos = Vector2.Zero;
        if (fromEntity is Node2D fNode)
        {
            fromPos = fNode.GlobalPosition;
        }

        bool isDead = currentTarget.Data.Get<bool>(DataKey.IsDead);
        _log.Debug(
            $"[弹跳准备] 起点: {(fromEntity as Node)?.Name}, 终点Entity: {(currentTarget as Node)?.Name}, 类型: {currentTarget.GetType().Name}, 死亡状态: {isDead}, 节点有效: {GodotObject.IsInstanceValid(currentTarget as GodotObject)}, 实例ID: {(currentTarget as Node)?.GetInstanceId()}");

        // 2. 造成伤害与画线
        if (currentTarget is IUnit unitVictim && currentTarget is Node2D targetNode)
        {
            Vector2 toPos = targetNode.GlobalPosition;

            var damageInfo = new DamageInfo
            {
                Attacker = context.Caster as Node,
                Victim = unitVictim,
                Damage = currentDamage,
                Type = DamageType.Magical,
                Tags = DamageTags.Ability
            };

            float dodgeChance = unitVictim.Data.Get<float>(DataKey.DodgeChance);
            _log.Debug(
                $"[伤害发送前] 期望结算伤害: {currentDamage}, 目标当前Hp: {unitVictim.Data.Get<float>(DataKey.CurrentHp)}, IsDead: {isDead}, IsInvulnerable: {unitVictim.Data.Get<bool>(DataKey.IsInvulnerable)}, LifecycleState: {unitVictim.Data.Get<LifecycleState>(DataKey.LifecycleState)}, DodgeChance: {dodgeChance}");
            SystemManager.Instance?.Execute<DamageService, DamageProcessRequest, DamageProcessResult>(
                new DamageProcessRequest(damageInfo) // 单次弹跳伤害请求
            );
            _log.Debug(
                $"[伤害发送后] 实际最终伤害: {damageInfo.FinalDamage}, 目标当前Hp: {unitVictim.Data.Get<float>(DataKey.CurrentHp)} (如果无伤害，可能是被闪避或因为目标已标记为Dead)");

            // 绘制特效
            if (!string.IsNullOrWhiteSpace(context.LineScenePath))
            {
                var lineScene = CommonTool.LoadPackedScene(
                    context.LineScenePath, // 链式闪电连线场景路径
                    "链式闪电连线特效"); // 日志用途名称
                if (lineScene != null)
                {
                    var effectNode = lineScene.Instantiate<Node2D>();
                    targetNode.GetTree().CurrentScene.AddChild(effectNode);
                    if (effectNode is ILineEffect iLine)
                    {
                        iLine.PlayChain(fromPos, toPos);
                    }
                }
            }
            else
            {
                var pool = ObjectPoolManager.GetPool<LightningLineEffect>(ObjectPoolNames.LightningLinePool);
                if (pool != null)
                {
                    var lineEffect = pool.Get();
                    lineEffect?.PlayChain(fromPos, toPos);
                }
            }

            fromPos = toPos; // 下一次搜索的起点更新为命中位置
        }
        else
        {
            _log.Warn($"[目标异常] currentTarget 并非 IUnit 或并非 Node2D，无法处理！类型: {currentTarget?.GetType().Name}");
        }

        // 3. 记录已命中目标防止无脑循环
        hitTargets.Add(currentTarget);

        // 4. 判断是否结束
        if (remainingBounces <= 1)
        {
            _log.Info($"链式闪电执行完成: 总弹跳次数 {hitTargets.Count}");
            return;
        }

        // 5. 准备下一跳（延迟进行索敌与开劈）
        var nextDamage = currentDamage * context.DamageDecay;

        TimerManager.Instance.Delay(context.Delay).OnComplete(() =>
        {
            // 延迟结束后，在这个时间点查找最合适的目标，而不是提前查找
            var nextTarget = FindNextChainTarget(context, fromPos, hitTargets);
            if (nextTarget == null)
            {
                _log.Debug($"链式闪电中断: 未找到下一个目标，已弹跳 {hitTargets.Count} 次");
                return;
            }

            ExecuteBounce(context, currentTarget, nextTarget, nextDamage, remainingBounces - 1, hitTargets);
        });
    }

    /// <summary>
    /// 在指定范围内查找下一个目标
    /// </summary>
    private IEntity? FindNextChainTarget(
        ChainBounceContext context,
        Vector2 searchOrigin,
        HashSet<IEntity> excludeTargets)
    {
        // 构建目标搜索查询请求
        var query = new TargetSelectorQuery
        {
            Geometry = GeometryType.Circle,
            Origin = searchOrigin,
            Range = context.Range,
            CenterEntity = context.Caster,
            TeamFilter = context.TeamFilter,
            Sorting = context.Sorting,
            MaxTargets = -1 // 搜索所有候选者以进行手动去重，由于 TargetSelector 不直接支持排除列表，此处先获取候选集
        };

        // 执行查询
        var candidates = EntityTargetSelector.Query(query);
        _log.Debug(
            $"[TargetQuery] 在 {searchOrigin} 半径 {context.Range} 内搜到 {candidates.Count} 个候选者, TeamFilter={context.TeamFilter}");

        // 遍历候选目标，返回第一个不在排除列表中的目标
        foreach (var candidate in candidates)
        {
            bool isExcluded = excludeTargets.Contains(candidate);
            _log.Debug(
                $"[TargetQuery - 候选] Entity: {(candidate as Node)?.Name}, 类型: {candidate.GetType().Name}, 是否死亡: {candidate.Data.Get<bool>(DataKey.IsDead)}, 是否已命中: {isExcluded}");
            if (!isExcluded)
            {
                _log.Debug($"[TargetQuery - 选中] 决定弹跳至: {(candidate as Node)?.Name}");
                return candidate;
            }
        }

        return null;
    }
}
