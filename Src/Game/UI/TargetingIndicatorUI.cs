using Godot;

namespace BrotatoLike.Game.UI;

/// <summary>
/// 点选目标指示器 UI，由 BrotatoLikeTargetingController 实例化。
/// </summary>
public partial class TargetingIndicatorUI : Node2D
{
    private Sprite2D? sprite;

    /// <inheritdoc />
    public override void _Ready()
    {
        sprite = GetNode<Sprite2D>("Sprite");
        Visible = false;
    }

    /// <summary>
    /// 显示指示器在世界坐标位置。
    /// </summary>
    public void ShowAt(Vector2 worldPosition)
    {
        GlobalPosition = worldPosition;
        Visible = true;
    }

    /// <summary>
    /// 隐藏指示器。
    /// </summary>
    public void HideIndicator()
    {
        Visible = false;
    }
}
