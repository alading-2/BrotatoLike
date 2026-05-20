using Godot;

namespace BrotatoLike.Game.UI;

/// <summary>
/// 伤害/治疗飘字 UI，由 BrotatoLikeHud 实例化，播放 float_up/float_up_crit 动画后自动释放。
/// </summary>
public partial class DamageNumberUI : Control
{
    private Label? damageLabel;
    private AnimationPlayer? animPlayer;

    /// <inheritdoc />
    public override void _Ready()
    {
        damageLabel = GetNode<Label>("DamageLabel");
        animPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
    }

    /// <summary>
    /// 绑定伤害数值、位置并播放动画。
    /// </summary>
    public void ShowDamage(float amount, Vector2 worldPosition, bool isCrit = false)
    {
        if (damageLabel == null || animPlayer == null)
        {
            return;
        }

        var isHeal = amount > 0f;
        damageLabel.Text = isHeal ? $"+{amount:0}" : $"{amount:0}";
        damageLabel.SetMeta("Value", amount);
        damageLabel.SetMeta("DamageType", isHeal ? "Heal" : "Damage");
        GlobalPosition = worldPosition;

        var animName = isCrit ? "float_up_crit" : "float_up";
        if (animPlayer.HasAnimation(animName))
        {
            animPlayer.Play(animName);
            animPlayer.AnimationFinished += OnAnimationFinished;
        }
        else
        {
            var timer = GetTree().CreateTimer(0.8f);
            timer.Timeout += QueueFree;
        }
    }

    private void OnAnimationFinished(StringName name)
    {
        if (animPlayer != null)
        {
            animPlayer.AnimationFinished -= OnAnimationFinished;
        }

        QueueFree();
    }
}
