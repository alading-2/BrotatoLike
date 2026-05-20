using Godot;

namespace BrotatoLike.Game.UI;

/// <summary>
/// 暂停菜单 UI，由 BrotatoLikeProgressionService 实例化。
/// </summary>
public partial class PauseMenuUI : CanvasLayer
{
    private Control? root;
    private Label? titleLabel;
    private Label? hintLabel;
    private Button? resumeButton;

    /// <summary>
    /// 暂停菜单是否可见。
    /// </summary>
    public bool IsMenuVisible => root?.Visible ?? false;

    /// <inheritdoc />
    public override void _Ready()
    {
        root = GetNode<Control>("Root");
        titleLabel = root.GetNode<Label>("Panel/Body/Margin/Content/TitleLabel");
        hintLabel = root.GetNode<Label>("Panel/Body/Margin/Content/HintLabel");
        resumeButton = root.GetNode<Button>("Panel/Body/Margin/Content/ResumeButton");

        if (resumeButton != null)
        {
            resumeButton.Pressed += () => HideMenu();
        }
    }

    /// <summary>
    /// 显示暂停菜单。
    /// </summary>
    public void ShowMenu()
    {
        if (root == null)
        {
            return;
        }

        root.Visible = true;
        GetTree().Paused = true;
    }

    /// <summary>
    /// 隐藏暂停菜单。
    /// </summary>
    public void HideMenu()
    {
        if (root == null)
        {
            return;
        }

        root.Visible = false;
        GetTree().Paused = false;
    }
}
