using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 冲刺技能执行器（移动系统版）
/// 
/// 触发方式：Manual（手动，玩家按键）
/// 目标选择：None（直接作用于施法者自身）
/// 运动模式：Charge（向当前移动方向或面朝方向高速冲刺，完成后自动回退默认模式）
/// 特效：可在技能数据中配置落地特效
/// </summary>
internal class DashExecutor : AbilityFeatureHandler
{
    private static readonly Log _log = new(nameof(DashExecutor));

    /// <summary>
    /// 冲刺持续时间（固定值，快速完成位移）
    /// </summary>
    private const float DashDuration = 0.15f;

    [ModuleInitializer]
    internal static void Initialize()
    {
        // 注册到功能处理器中心，使框架能通过 FeatureId 路由到此执行器
        FeatureHandlerRegistry.Register(new DashExecutor());
    }

    /// <summary>
    /// 技能功能 ID：对应配置中的 技能.位移.冲刺
    /// </summary>
    public override string FeatureId => global::FeatureId.Ability.Movement.Dash;

    /// <summary>
    /// 执行冲刺逻辑的主入口
    /// </summary>
    /// <param name="context">施法上下文</param>
    /// <returns>执行结果</returns>
    protected override AbilityExecutedResult ExecuteAbility(CastContext context)
    {
        var caster = context.Caster!;
        var ability = context.Ability!;
        var casterNode2D = (Node2D)caster;

        // 1. 数据驱动：从技能动态 Data 容器中读取运行时配置
        // 冲刺距离：决定位移终点
        var dashDistance = ability.Data.Get<float>(DataKey.AbilityCastRange);
        // 落地伤害半径：决定冲刺停止时的圆形检测范围
        var damageRadius = ability.Data.Get<float>(DataKey.AbilityEffectRadius);
        // 落地特效场景：冲刺完成后的视觉表现
        var effectScenePath = ability.Data.Get<string>(DataKey.EffectScene); // 落地特效场景路径
        // 最大伤害目标数：当前固定为不限制，由技能逻辑自行决定
        const int maxTargets = -1;

        // 2. 策略逻辑：确定冲刺方向
        // 规则：优先取当前移动速度方向；若当前速度接近 0，则回退到最后一次主动移动方向缓存；再不行才读当前视觉朝向。
        Vector2 dashDir = ResolveDashDirection(caster, casterNode2D);

        // 3. 通信机制：通过 EventBus 发布位移启动事件
        // 移动系统（MovementSystem）会监听此事件并接管实体的坐标更新
        caster.Events.Emit(
            GameEventType.Unit.MovementStarted,
            new GameEventType.Unit.MovementStartedEventData(
                MoveMode.Charge,
                new MovementParams
                {
                    Mode = MoveMode.Charge, // 切换为高速冲刺模式
                    TargetPoint = casterNode2D.GlobalPosition + dashDir * dashDistance, // 计算目标航点
                    MaxDuration = DashDuration, // 设置超时限制
                    RotateToVelocity = false, // 冲刺期间保持原有朝向
                    // 回调函数：当移动系统完成位移或因碰撞停止时，触发 OnDashStop
                    OnStop =
                        stopCtx => OnDashStop(stopCtx, caster, casterNode2D, effectScenePath, damageRadius, maxTargets),
                }
            )
        );

        _log.Info($"冲刺执行: 方向={dashDir}, 位移距离={dashDistance}, 伤害半径={damageRadius}");
        return new AbilityExecutedResult { TargetsHit = 0 };
    }

    /// <summary>
    /// 解析冲刺方向。
    /// <para>优先级：当前速度 > 最后一次主动移动方向缓存 > 当前视觉朝向。</para>
    /// </summary>
    private static Vector2 ResolveDashDirection(
        IEntity caster,
        Node2D casterNode2D)
    {
        Vector2 moveDir = caster.Data.Get<Vector2>(DataKey.Velocity);
        if (moveDir.LengthSquared() > 0.01f)
        {
            return moveDir.Normalized();
        }

        Vector2 cachedMoveDir = caster.Data.Get<Vector2>(DataKey.LastMoveDirection);
        if (cachedMoveDir.LengthSquared() > 0.01f)
        {
            return cachedMoveDir.Normalized();
        }

        // 最后的兜底才读取当前视觉朝向；这样至少不会在“上一帧向左移动过”时莫名回退到向右。
        var sprite = casterNode2D.GetNodeOrNull<AnimatedSprite2D>("VisualRoot");
        return (sprite?.FlipH ?? false) ? Vector2.Left : Vector2.Right;
    }

    /// <summary>
    /// 冲刺位移停止时的回调逻辑
    /// </summary>
    /// <param name="ctx">位移停止上下文，包含是否正常完成或被中断的信息</param>
    /// <param name="caster">施法实体</param>
    /// <param name="casterNode2D">施法者的 Godot 节点引用</param>
    /// <param name="effectScenePath">待生成的落点特效路径</param>
    /// <param name="damageRadius">伤害检测半径</param>
    /// <param name="maxTargets">最大命中数</param>
    private void OnDashStop(
        MovementStopContext ctx,
        IEntity caster,
        Node2D casterNode2D,
        string effectScenePath,
        float damageRadius,
        int maxTargets)
    {
        // 仅在位移正常完成（到达终点）时执行落地效果
        if (!ctx.IsCompleted) return;
        // 确保施法者节点仍然存活（防止中途实体被销毁）
        if (!GodotObject.IsInstanceValid(casterNode2D)) return;

        var impactPosition = casterNode2D.GlobalPosition;

        // 4. 执行落地命中（目标查询 + 特效 + 伤害，三步合一）
        var result = AbilityImpactTool.Execute(caster, new AbilityImpactOptions
        {
            Query = new TargetSelectorQuery
            {
                Geometry = GeometryType.Circle, // 圆形范围
                Origin = impactPosition, // 落点为圆心
                Range = damageRadius, // 技能配置的半径
                CenterEntity = caster, // 施法者作为阵营判断基准
                TeamFilter = TeamFilter.Enemy, // 仅筛选敌方单位
                Sorting = TargetSorting.Nearest, // 优先最近目标
                MaxTargets = maxTargets // 限制命中数量
            },
            Effect = !string.IsNullOrWhiteSpace(effectScenePath)
                ? new EffectSpawnOptions(
                    effectScenePath, // 特效场景路径
                    Name: "冲刺落地特效")
                : null,
            Damage = new DamageApplyOptions
            {
                Damage = caster.Data.Get<float>(DataKey.FinalAttack), // 施法者最终攻击力
                Type = DamageType.Physical, // 物理伤害
                Tags = DamageTags.Ability | DamageTags.Area, // 技能范围伤害
                Attacker = casterNode2D // 伤害来源节点
            }
        });

        _log.Info($"冲刺结束: 落点={impactPosition}, 伤害半径={damageRadius}, 命中={result.TargetsHit}");
    }
}
