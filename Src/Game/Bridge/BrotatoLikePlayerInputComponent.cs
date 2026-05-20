using BrotatoLike.Game.Events;
using Godot;
using SlimeAI.GameOS.Capabilities.Damage;
using SlimeAI.GameOS.Capabilities.Movement;
using SlimeAI.GameOS.GodotBridge;
using SlimeAI.GameOS.Runtime.Entity;

namespace BrotatoLike.Game.Bridge;

/// <summary>
/// BrotatoLike 玩家输入桥接组件：每帧读取 Godot Input，将移动方向写入 Entity Data 的 <see cref="MovementDataKeys.InputDirection" />。
/// <para>挂载到 GodotEntity2D 下，作为 PlayerInput 运动策略的输入来源。</para>
/// <para>也支持 AI 直接覆盖：AI 层写入 <see cref="MovementDataKeys.AIMoveDirection" /> 时，本组件可自动禁用或保持共存。</para>
/// </summary>
public partial class BrotatoLikePlayerInputComponent : Node, IGodotComponent
{
    private IEntity? entity;

    /// <summary>是否自动在 <code>_Process</code> 中读取输入。</summary>
    [Export]
    public bool AutoTick { get; set; } = true;

    /// <summary>Godot Input Map 中左移动作名称。</summary>
    [Export]
    public string MoveLeftAction { get; set; } = "MoveLeft";

    /// <summary>Godot Input Map 中右移动作名称。</summary>
    [Export]
    public string MoveRightAction { get; set; } = "MoveRight";

    /// <summary>Godot Input Map 中上移动作名称。</summary>
    [Export]
    public string MoveUpAction { get; set; } = "MoveUp";

    /// <summary>Godot Input Map 中下移动作名称。</summary>
    [Export]
    public string MoveDownAction { get; set; } = "MoveDown";

    /// <summary>当 <see cref="MovementDataKeys.CanMoveInput" /> 为 false 时是否仍读取输入（仅写入 Data，不强制门控）。</summary>
    [Export]
    public bool IgnoreCanMoveInputGate { get; set; } = false;

    /// <summary>Godot Input Map 中释放当前技能动作名称。</summary>
    [Export]
    public string UseSkillAction { get; set; } = "UseSkill";

    /// <summary>Godot Input Map 中切换到上一个技能动作名称。</summary>
    [Export]
    public string PreviousSkillAction { get; set; } = "PreviousSkill";

    /// <summary>Godot Input Map 中切换到下一个技能动作名称。</summary>
    [Export]
    public string NextSkillAction { get; set; } = "NextSkill";

    /// <summary>最近一次读取到的输入方向。</summary>
    public Vector2Value LastInputDirection { get; private set; } = Vector2Value.Zero;

    /// <summary>技能释放按钮是否在本帧刚按下。</summary>
    public bool UseSkillJustPressed { get; private set; }

    /// <summary>上一个技能按钮是否在本帧刚按下。</summary>
    public bool PreviousSkillJustPressed { get; private set; }

    /// <summary>下一个技能按钮是否在本帧刚按下。</summary>
    public bool NextSkillJustPressed { get; private set; }

    /// <inheritdoc />
    public void OnComponentRegistered(IEntity entity, Node entityNode)
    {
        this.entity = entity;
        MovementDataKeys.RegisterAll();
    }

    /// <inheritdoc />
    public void OnComponentUnregistered(IEntity? entity, Node? entityNode)
    {
        this.entity = null;
        LastInputDirection = Vector2Value.Zero;
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        if (AutoTick)
        {
            TickInput();
        }
    }

    /// <summary>
    /// 手动执行一次输入读取，供测试或外部调度入口调用。
    /// </summary>
    public void TickInput()
    {
        if (entity == null)
        {
            return;
        }

        if (!IgnoreCanMoveInputGate && !entity.Data.Get<bool>(MovementDataKeys.CanMoveInput, true))
        {
            LastInputDirection = Vector2Value.Zero;
            entity.Data.Set(MovementDataKeys.InputDirection, Vector2Value.Zero);
            return;
        }

        if (entity.Data.Get<bool>(DamageDataKeys.IsDead, false))
        {
            LastInputDirection = Vector2Value.Zero;
            entity.Data.Set(MovementDataKeys.InputDirection, Vector2Value.Zero);
            return;
        }

        var input = Input.GetVector(MoveLeftAction, MoveRightAction, MoveUpAction, MoveDownAction);
        LastInputDirection = new Vector2Value(input.X, input.Y);
        entity.Data.Set(MovementDataKeys.InputDirection, LastInputDirection);

        UseSkillJustPressed = Input.IsActionJustPressed(UseSkillAction);
        PreviousSkillJustPressed = Input.IsActionJustPressed(PreviousSkillAction);
        NextSkillJustPressed = Input.IsActionJustPressed(NextSkillAction);

        if (UseSkillJustPressed)
        {
            entity.Events.Publish(new InputUseSkill(entity));
        }

        if (PreviousSkillJustPressed)
        {
            entity.Events.Publish(new InputPreviousSkill(entity));
        }

        if (NextSkillJustPressed)
        {
            entity.Events.Publish(new InputNextSkill(entity));
        }
    }
}
