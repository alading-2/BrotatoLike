using System;
using Godot;

namespace BrotatoLike.Game.UI;

/// <summary>
/// 伤害/治疗飘字 UI，由 BrotatoLikeHud 实例化，播放 float_up/float_up_crit 动画后自动释放。
/// </summary>
public partial class DamageNumberUI : Control
{
    private const float DefaultLifetimeSeconds = 0.8f;
    private const float MinimumFrameDelta = 1f / 60f;

    private Label? damageLabel;
    private AnimationPlayer? animPlayer;
    private int showVersion;
    private bool finished;
    private bool animationFinishedConnected;
    private float lifetimeRemainingSeconds = -1f;

    /// <summary>
    /// 飘字动画或兜底生命周期结束时触发，由 HUD 负责归还对象池。
    /// </summary>
    public event Action<DamageNumberUI>? Finished;

    /// <inheritdoc />
    public override void _Ready()
    {
        CacheNodes();
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        if (finished || lifetimeRemainingSeconds <= 0f)
        {
            return;
        }

        lifetimeRemainingSeconds -= Math.Max((float)delta, MinimumFrameDelta);
        if (lifetimeRemainingSeconds <= 0f)
        {
            Finish(showVersion);
        }
    }

    /// <summary>
    /// 绑定伤害数值、CanvasLayer 内位置并播放动画。
    /// </summary>
    public void ShowDamage(float amount, Vector2 canvasPosition, bool isCrit = false)
    {
        CacheNodes();
        if (damageLabel == null)
        {
            return;
        }

        showVersion++;
        finished = false;
        lifetimeRemainingSeconds = DefaultLifetimeSeconds;
        SetProcess(true);
        Visible = true;
        Modulate = new Color(1f, 1f, 1f, 1f);
        SelfModulate = new Color(1f, 1f, 1f, 1f);
        var isHeal = amount > 0f;
        damageLabel.Text = isHeal ? $"+{amount:0}" : $"{amount:0}";
        damageLabel.Position = Vector2.Zero;
        damageLabel.Scale = Vector2.One;
        damageLabel.SetMeta("Value", amount);
        damageLabel.SetMeta("DamageType", isHeal ? "Heal" : "Damage");
        SetMeta("Value", amount);
        SetMeta("DamageType", isHeal ? "Heal" : "Damage");
        Position = canvasPosition;

        var animName = isCrit ? "float_up_crit" : "float_up";
        DisconnectAnimationFinished();
        if (animPlayer != null)
        {
            animPlayer.Stop();
            if (animPlayer.HasAnimation(animName))
            {
                lifetimeRemainingSeconds = ResolveAnimationLength(animName);
                ConnectAnimationFinished();
                animPlayer.Play(animName);
            }
        }
    }

    /// <summary>
    /// 回到对象池前重置文本、动画和元数据。
    /// </summary>
    public void ResetForPool()
    {
        CacheNodes();
        showVersion++;
        finished = true;
        lifetimeRemainingSeconds = -1f;
        SetProcess(false);
        DisconnectAnimationFinished();
        animPlayer?.Stop();

        if (damageLabel != null)
        {
            damageLabel.Text = string.Empty;
            damageLabel.Position = Vector2.Zero;
            damageLabel.Scale = Vector2.One;
            damageLabel.RemoveMeta("Value");
            damageLabel.RemoveMeta("DamageType");
        }

        RemoveMeta("Value");
        RemoveMeta("DamageType");
        RemoveMeta("WorldPosition");
        RemoveMeta("CanvasPosition");
        Position = Vector2.Zero;
        Visible = false;
        Modulate = new Color(1f, 1f, 1f, 1f);
        SelfModulate = new Color(1f, 1f, 1f, 1f);
    }

    private void OnAnimationFinished(StringName name)
    {
        Finish(showVersion);
    }

    private void Finish(int version)
    {
        if (finished || version != showVersion)
        {
            return;
        }

        finished = true;
        lifetimeRemainingSeconds = -1f;
        SetProcess(false);
        DisconnectAnimationFinished();

        Finished?.Invoke(this);
    }

    private void ConnectAnimationFinished()
    {
        if (animPlayer == null || animationFinishedConnected)
        {
            return;
        }

        animPlayer.AnimationFinished += OnAnimationFinished;
        animationFinishedConnected = true;
    }

    private void DisconnectAnimationFinished()
    {
        if (animPlayer == null || !animationFinishedConnected)
        {
            return;
        }

        animPlayer.AnimationFinished -= OnAnimationFinished;
        animationFinishedConnected = false;
    }

    private float ResolveAnimationLength(string animName)
    {
        if (animPlayer == null || !animPlayer.HasAnimation(animName))
        {
            return DefaultLifetimeSeconds;
        }

        var animation = animPlayer.GetAnimation(animName);
        return animation == null ? DefaultLifetimeSeconds : Math.Max((float)animation.Length, MinimumFrameDelta);
    }

    private void CacheNodes()
    {
        damageLabel ??= GetNodeOrNull<Label>("DamageLabel");
        animPlayer ??= GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
    }
}
