using Godot;

namespace BrotatoLike.Game.UI;

/// <summary>
/// 血条语义样式。
/// </summary>
public enum HealthBarKind
{
    /// <summary>中性目标。</summary>
    Neutral,

    /// <summary>玩家。</summary>
    Player,

    /// <summary>敌人。</summary>
    Enemy
}

/// <summary>
/// 头顶血条 UI，由 BrotatoLikeHud 实例化并绑定敌人 HP 数据。
/// </summary>
public partial class HealthBarUI : Control
{
    private ProgressBar? bar;
    private HealthBarKind? appliedKind;

    /// <inheritdoc />
    public override void _Ready()
    {
        bar = GetNode<ProgressBar>("HealthBar");
    }

    /// <summary>
    /// 同步血量到 ProgressBar。
    /// </summary>
    public void BindHealth(float currentHp, float maxHp, HealthBarKind kind = HealthBarKind.Neutral)
    {
        if (bar == null)
        {
            return;
        }

        ApplyStyle(kind);
        bar.MaxValue = maxHp;
        bar.Value = Mathf.Clamp(currentHp, 0f, maxHp);
        SetMeta("HealthBarKind", kind.ToString());
    }

    /// <summary>
    /// 设置血条在 CanvasLayer 内的位置，以 ProgressBar 中心对齐目标头上方。
    /// </summary>
    public void SetCanvasPosition(Vector2 canvasPosition)
    {
        Position = canvasPosition;
    }

    /// <summary>
    /// 回到对象池前重置运行时绑定状态。
    /// </summary>
    public void ResetForPool()
    {
        CacheNodes();
        Position = Vector2.Zero;
        Visible = false;
        RemoveMeta("EntityId");
        RemoveMeta("CurrentHp");
        RemoveMeta("MaxHp");
        RemoveMeta("WorldPosition");
        RemoveMeta("HeadWorldPosition");
        RemoveMeta("CanvasPosition");
        RemoveMeta("HealthBarHeight");
        RemoveMeta("HealthBarKind");
        if (bar != null)
        {
            bar.MaxValue = 1d;
            bar.Value = 0d;
        }

        appliedKind = null;
    }

    private void ApplyStyle(HealthBarKind kind)
    {
        if (bar == null || appliedKind == kind)
        {
            return;
        }

        appliedKind = kind;
        var fillColor = kind switch
        {
            HealthBarKind.Player => new Color(0.16f, 0.86f, 0.34f, 1f),
            HealthBarKind.Enemy => new Color(0.94f, 0.22f, 0.18f, 1f),
            _ => new Color(0.86f, 0.72f, 0.28f, 1f)
        };

        var background = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.07f, 0.08f, 0.82f),
            BorderColor = new Color(0.96f, 0.96f, 0.96f, 0.28f),
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3
        };

        var fill = new StyleBoxFlat
        {
            BgColor = fillColor,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3
        };

        bar.AddThemeStyleboxOverride("background", background);
        bar.AddThemeStyleboxOverride("fill", fill);
    }

    private void CacheNodes()
    {
        bar ??= GetNodeOrNull<ProgressBar>("HealthBar");
    }
}
