using System;
using System.Collections.Generic;
using BrotatoLike.Game.Progression;
using Godot;

namespace BrotatoLike.Game.UI;

/// <summary>
/// 升级三选一面板，由 BrotatoLikeProgressionService 实例化和绑定。
/// </summary>
public partial class LevelUpChoicePanelUI : CanvasLayer
{
    private Control? root;
    private Label? titleLabel;
    private Label? subtitleLabel;
    private readonly List<LevelUpChoiceSlotUI> slots = new();

    /// <summary>
    /// 选项被确认时触发。
    /// </summary>
    public event Action<string>? ChoiceSelected;

    /// <summary>
    /// 面板是否可见。
    /// </summary>
    public bool IsPanelVisible => root?.Visible ?? false;

    /// <inheritdoc />
    public override void _Ready()
    {
        root = GetNode<Control>("Root");
        titleLabel = root.GetNode<Label>("Panel/Body/Margin/Content/TitleLabel");
        subtitleLabel = root.GetNode<Label>("Panel/Body/Margin/Content/SubtitleLabel");
        var slotContainer = root.GetNode<VBoxContainer>("Panel/Body/Margin/Content/ChoiceSlots");
        slots.Clear();
        foreach (var child in slotContainer.GetChildren())
        {
            if (child is LevelUpChoiceSlotUI slot)
            {
                slot.ChoicePressed += id => ChoiceSelected?.Invoke(id);
                slots.Add(slot);
            }
        }

        HidePanel();
    }

    /// <summary>
    /// 显示并绑定升级选择。
    /// </summary>
    public void ShowChoices(int level, IReadOnlyList<BrotatoLikeLevelUpChoiceDefinition> choices)
    {
        if (root == null)
        {
            return;
        }

        root.Visible = true;
        if (titleLabel != null)
        {
            titleLabel.Text = $"Level {level}";
        }

        if (subtitleLabel != null)
        {
            subtitleLabel.Text = "选择一项本局成长";
        }

        for (var i = 0; i < slots.Count; i++)
        {
            if (i < choices.Count)
            {
                slots[i].Bind(choices[i], i);
            }
            else
            {
                slots[i].Clear();
            }
        }

        SetMeta("Shown", true);
        SetMeta("Closed", false);
        SetMeta("Level", level);
        SetMeta("ChoiceIds", JoinChoices(choices, value => value.Id));
        SetMeta("ChoiceTexts", JoinChoices(choices, value => value.DisplayText));
        SetMeta("EffectTypes", JoinChoices(choices, value => value.EffectType.ToString()));
        SetMeta("EffectTargets", JoinChoices(choices, value => value.EffectTarget));
        SetMeta("SceneBacked", !string.IsNullOrEmpty(SceneFilePath));
        SetMeta("ScenePath", SceneFilePath);
    }

    /// <summary>
    /// 隐藏面板。
    /// </summary>
    public void HidePanel()
    {
        if (root != null)
        {
            root.Visible = false;
        }

        SetMeta("Shown", false);
        SetMeta("Closed", true);
    }

    /// <summary>
    /// 记录选择应用结果。
    /// </summary>
    public void RecordSelection(BrotatoLikeLevelUpChoiceApplyResult result)
    {
        SetMeta("SelectedChoiceId", result.ChoiceId);
        SetMeta("SelectedEffectType", result.EffectType);
        SetMeta("SelectedEffectTarget", result.EffectTarget);
        SetMeta("BeforeValue", result.BeforeValue);
        SetMeta("AfterValue", result.AfterValue);
        SetMeta("ApplySuccess", result.Success);
        SetMeta("ApplyMessage", result.Message);
    }

    private static string JoinChoices(
        IReadOnlyList<BrotatoLikeLevelUpChoiceDefinition> choices,
        Func<BrotatoLikeLevelUpChoiceDefinition, string> selector)
    {
        var values = new string[choices.Count];
        for (var i = 0; i < choices.Count; i++)
        {
            values[i] = selector(choices[i]);
        }

        return string.Join(",", values);
    }
}
