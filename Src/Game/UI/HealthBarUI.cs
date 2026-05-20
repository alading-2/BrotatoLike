using Godot;

namespace BrotatoLike.Game.UI;

/// <summary>
/// 头顶血条 UI，由 BrotatoLikeHud 实例化并绑定敌人 HP 数据。
/// </summary>
public partial class HealthBarUI : Control
{
    private ProgressBar? bar;

    /// <inheritdoc />
    public override void _Ready()
    {
        bar = GetNode<ProgressBar>("HealthBar");
    }

    /// <summary>
    /// 同步血量到 ProgressBar。
    /// </summary>
    public void BindHealth(float currentHp, float maxHp)
    {
        if (bar == null)
        {
            return;
        }

        bar.MaxValue = maxHp;
        bar.Value = Mathf.Clamp(currentHp, 0f, maxHp);
    }

    /// <summary>
    /// 设置血条世界位置，以 ProgressBar 中心对齐目标头上方。
    /// </summary>
    public void SetWorldPosition(Vector2 worldPosition)
    {
        Position = worldPosition + new Vector2(-50f, -36f);
    }
}
