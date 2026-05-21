using Godot;

namespace BrotatoLike.Game.UI;

/// <summary>
/// 单个技能槽 UI，由 ActiveSkillBarUI 管理。
/// 节点引用通过 unique_name_in_owner (%) 解析。
/// </summary>
public partial class ActiveSkillSlotUI : Control
{
    private Panel? background;
    private TextureRect? skillIcon;
    private ColorRect? cooldownOverlay;
    private Label? chargeLabel;
    private Label? keyHintLabel;
    private Label? skillNameLabel;

    /// <inheritdoc />
    public override void _Ready()
    {
        background = GetNode<Panel>("%Background");
        skillIcon = GetNode<TextureRect>("%SkillIcon");
        cooldownOverlay = GetNode<ColorRect>("%CooldownOverlay");
        chargeLabel = GetNode<Label>("%ChargeLabel");
        keyHintLabel = GetNode<Label>("%KeyHintLabel");
        skillNameLabel = GetNode<Label>("%SkillNameLabel");
    }

    /// <summary>
    /// 绑定技能名称、按键提示、冷却和充能状态。
    /// </summary>
    public void Bind(
        string skillName,
        string keyHint,
        float cooldownFraction,
        int charges,
        int maxCharges,
        bool selected,
        string abilityEntityId = "",
        int visibleSlotIndex = -1)
    {
        if (skillNameLabel != null) skillNameLabel.Text = skillName;
        if (keyHintLabel != null) keyHintLabel.Text = keyHint;
        if (chargeLabel != null) chargeLabel.Text = maxCharges > 0 ? $"{charges}/{maxCharges}" : string.Empty;
        if (cooldownOverlay != null)
        {
            cooldownOverlay.Size = new Vector2(64f * cooldownFraction, 64f);
            cooldownOverlay.Visible = cooldownFraction > 0.01f;
        }

        if (background != null)
        {
            background.Modulate = selected ? new Color(1f, 0.9f, 0.5f, 1f) : new Color(0.3f, 0.3f, 0.35f, 1f);
        }

        SetMeta("AbilityEntityId", abilityEntityId);
        SetMeta("VisibleSlotIndex", visibleSlotIndex);
        SetMeta("Selected", selected);
    }

    /// <summary>
    /// 清空槽位显示。
    /// </summary>
    public void Clear()
    {
        if (skillNameLabel != null) skillNameLabel.Text = "-";
        if (keyHintLabel != null) keyHintLabel.Text = string.Empty;
        if (chargeLabel != null) chargeLabel.Text = string.Empty;
        if (cooldownOverlay != null) cooldownOverlay.Visible = false;
        SetMeta("AbilityEntityId", string.Empty);
        SetMeta("VisibleSlotIndex", -1);
        SetMeta("Selected", false);
    }
}
