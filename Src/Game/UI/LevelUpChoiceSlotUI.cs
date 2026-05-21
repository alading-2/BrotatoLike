using System;
using BrotatoLike.Game.Progression;
using Godot;

namespace BrotatoLike.Game.UI;

/// <summary>
/// 升级选择面板中的单个选项槽。
/// </summary>
public partial class LevelUpChoiceSlotUI : PanelContainer
{
    private Button? choiceButton;
    private Label? descriptionLabel;
    private Label? effectLabel;
    private string choiceId = string.Empty;

    /// <summary>
    /// 选项被确认时触发。
    /// </summary>
    public event Action<string>? ChoicePressed;

    /// <inheritdoc />
    public override void _Ready()
    {
        choiceButton = GetNode<Button>("Margin/Content/ChoiceButton");
        descriptionLabel = GetNode<Label>("Margin/Content/DescriptionLabel");
        effectLabel = GetNode<Label>("Margin/Content/EffectLabel");
        if (choiceButton != null)
        {
            choiceButton.Pressed += () =>
            {
                if (!string.IsNullOrEmpty(choiceId))
                {
                    ChoicePressed?.Invoke(choiceId);
                }
            };
        }
    }

    /// <summary>
    /// 绑定选项定义。
    /// </summary>
    public void Bind(BrotatoLikeLevelUpChoiceDefinition definition, int index)
    {
        choiceId = definition.Id;
        if (choiceButton != null)
        {
            choiceButton.Text = $"{index + 1}. {definition.DisplayText}";
            choiceButton.Disabled = false;
        }

        if (descriptionLabel != null)
        {
            descriptionLabel.Text = definition.Description;
        }

        if (effectLabel != null)
        {
            effectLabel.Text = $"{definition.EffectType} {definition.EffectTarget} +{definition.EffectValue:0.###}";
        }

        SetMeta("ChoiceId", definition.Id);
        SetMeta("DisplayText", definition.DisplayText);
        SetMeta("Description", definition.Description);
        SetMeta("EffectType", definition.EffectType.ToString());
        SetMeta("EffectTarget", definition.EffectTarget);
        SetMeta("EffectValue", definition.EffectValue);
        SetMeta("AbilityRecordId", definition.AbilityRecordId);
        SetMeta("SlotIndex", index);
    }

    /// <summary>
    /// 清空选项槽。
    /// </summary>
    public void Clear()
    {
        choiceId = string.Empty;
        if (choiceButton != null)
        {
            choiceButton.Text = "-";
            choiceButton.Disabled = true;
        }

        if (descriptionLabel != null)
        {
            descriptionLabel.Text = string.Empty;
        }

        if (effectLabel != null)
        {
            effectLabel.Text = string.Empty;
        }

        SetMeta("ChoiceId", string.Empty);
        SetMeta("SlotIndex", -1);
    }
}
