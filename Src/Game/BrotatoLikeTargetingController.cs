using BrotatoLike.Game.UI;
using Godot;
using SlimeAI.GameOS.Capabilities.Ability;
using SlimeAI.GameOS.Capabilities.Damage;
using SlimeAI.GameOS.Capabilities.Movement;
using SlimeAI.GameOS.Runtime.Entity;

namespace BrotatoLike.Game;

/// <summary>
/// BrotatoLike 点选技能 UX 控制器，负责目标指示器和确认/取消。
/// </summary>
public partial class BrotatoLikeTargetingController : Node2D
{
    private BrotatoLikeGameRuntime? runtime;
    private IEntity? activeCaster;
    private IEntity? activeAbility;
    private Node? sessionNode;
    private bool justStarted;
    private Vector2 requestedTargetPosition;
    private Vector2 clampedTargetPosition;

    /// <summary>
    /// 最近一次确认施法结果。
    /// </summary>
    public AbilityTriggerReport? LastTriggerReport { get; private set; }

    /// <summary>
    /// 是否存在点选会话。
    /// </summary>
    public bool IsTargeting => activeCaster != null && activeAbility != null;

    /// <summary>
    /// 指示器节点（scene-backed: TargetingIndicatorUI.tscn）。
    /// </summary>
    public TargetingIndicatorUI Indicator { get; private set; } = null!;

    /// <summary>
    /// 绑定运行时。
    /// </summary>
    /// <param name="runtime">BrotatoLike 运行时。</param>
    public void Bind(BrotatoLikeGameRuntime runtime)
    {
        this.runtime = runtime;
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        Name = "BrotatoLikeTargetingController";

        // scene-backed: 点选指示器来自 TargetingIndicatorUI.tscn
        var indicatorScene = GD.Load<PackedScene>("res://Scenes/UI/TargetingIndicatorUI.tscn");
        Indicator = indicatorScene.Instantiate<TargetingIndicatorUI>();
        Indicator.Name = "PointTargetingIndicator";
        AddChild(Indicator);
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        if (!IsTargeting)
        {
            return;
        }

        UpdateClamp();
        if (Input.IsActionJustPressed("CancelTarget"))
        {
            Cancel();
            return;
        }

        if (Input.IsActionJustPressed("ConfirmTarget")
            || (!justStarted && Input.IsActionJustPressed("UseSkill")))
        {
            Confirm();
            return;
        }

        justStarted = false;
    }

    /// <summary>
    /// 如果技能需要点选，则接管 UseSkill 输入。
    /// </summary>
    /// <param name="caster">施法者。</param>
    /// <param name="ability">技能实体。</param>
    /// <param name="report">确认时的触发报告；仅启动会话时为空。</param>
    public bool TryHandleUseSkill(IEntity caster, IEntity ability, out AbilityTriggerReport? report)
    {
        report = null;
        var selection = ability.Data.Get<AbilityTargetSelection>(
            AbilityDataKeys.TargetSelection,
            AbilityTargetSelection.None);
        if (selection != AbilityTargetSelection.Point && selection != AbilityTargetSelection.EntityOrPoint)
        {
            return false;
        }

        if (IsTargeting && activeCaster?.EntityId == caster.EntityId && activeAbility?.EntityId == ability.EntityId)
        {
            report = Confirm();
            return true;
        }

        Start(caster, ability);
        return true;
    }

    /// <summary>
    /// 设置请求目标点，供验证或鼠标/手柄控制层使用。
    /// </summary>
    /// <param name="worldPosition">世界坐标。</param>
    public void SetRequestedTargetPosition(Vector2 worldPosition)
    {
        requestedTargetPosition = worldPosition;
        UpdateClamp();
    }

    /// <summary>
    /// 取消当前点选。
    /// </summary>
    public void Cancel()
    {
        activeCaster = null;
        activeAbility = null;
        Indicator.Visible = false;
        if (sessionNode != null && GodotObject.IsInstanceValid(sessionNode))
        {
            sessionNode.QueueFree();
        }

        sessionNode = null;
        SetMeta("IsTargeting", false);
    }

    /// <summary>
    /// 确认当前点选并触发 AbilityService。
    /// </summary>
    public AbilityTriggerReport Confirm()
    {
        if (activeCaster == null || activeAbility == null)
        {
            LastTriggerReport = new AbilityTriggerReport(AbilityTriggerResult.FailNoTarget, null, "no active targeting session");
            return LastTriggerReport.Value;
        }

        UpdateClamp();
        var context = new AbilityCastContext
        {
            Caster = activeCaster,
            Ability = activeAbility,
            TargetPosition = new Vector2Value(clampedTargetPosition.X, clampedTargetPosition.Y),
            DamageType = DamageType.Physical,
            DamageTags = DamageTags.Ability
        };
        LastTriggerReport = AbilityService.Instance.TryTrigger(context);
        SetMeta("LastTriggerResult", LastTriggerReport.Value.Result.ToString());
        SetMeta("RequestedTargetPosition", Format(requestedTargetPosition));
        SetMeta("ClampedTargetPosition", Format(clampedTargetPosition));
        Cancel();
        return LastTriggerReport.Value;
    }

    private void Start(IEntity caster, IEntity ability)
    {
        activeCaster = caster;
        activeAbility = ability;
        LastTriggerReport = null;
        justStarted = true;

        var origin = caster.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
        var range = ResolveCastRange(ability);
        requestedTargetPosition = new Vector2(origin.X + (range * 1.5f), origin.Y);
        clampedTargetPosition = new Vector2(origin.X + range, origin.Y);
        Indicator.Visible = true;
        Indicator.GlobalPosition = clampedTargetPosition;
        Indicator.SetMeta("AbilityId", ability.EntityId.Value);
        Indicator.SetMeta("RequestedTargetPosition", Format(requestedTargetPosition));
        Indicator.SetMeta("ClampedTargetPosition", Format(clampedTargetPosition));

        // scene-first exception: metadata-only runtime session node
        sessionNode = new Node { Name = "PointTargetingSession" };
        sessionNode.SetMeta("AbilityId", ability.EntityId.Value);
        sessionNode.SetMeta("CooldownBeforeConfirm", ability.Data.Get<float>(AbilityDataKeys.CooldownRemaining, 0f));
        AddChild(sessionNode);
        SetMeta("IsTargeting", true);
    }

    private void UpdateClamp()
    {
        if (activeCaster == null || activeAbility == null)
        {
            return;
        }

        var originValue = activeCaster.Data.Get<Vector2Value>(MovementDataKeys.Position, Vector2Value.Zero);
        var origin = new Vector2(originValue.X, originValue.Y);
        var range = ResolveCastRange(activeAbility);
        var offset = requestedTargetPosition - origin;
        clampedTargetPosition = offset.Length() > range && range > 0f
            ? origin + offset.Normalized() * range
            : requestedTargetPosition;
        Indicator.GlobalPosition = clampedTargetPosition;
        Indicator.SetMeta("RequestedTargetPosition", Format(requestedTargetPosition));
        Indicator.SetMeta("ClampedTargetPosition", Format(clampedTargetPosition));
        Indicator.SetMeta("CastRange", range);
        sessionNode?.SetMeta("RequestedTargetPosition", Format(requestedTargetPosition));
        sessionNode?.SetMeta("ClampedTargetPosition", Format(clampedTargetPosition));
    }

    private static float ResolveCastRange(IEntity ability)
    {
        var range = ability.Data.Get<float>(AbilityDataKeys.CastRange, -1f);
        if (range > 0f)
        {
            return range;
        }

        range = ability.Data.Get<float>(AbilityDataKeys.AutoTargetRange, -1f);
        return range > 0f ? range : 300f;
    }

    private static string Format(Vector2 value)
    {
        return $"{value.X:0.###},{value.Y:0.###}";
    }
}
