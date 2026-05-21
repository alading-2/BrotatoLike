using System;
using BrotatoLike.Game.Characters;
using Godot;

namespace BrotatoLike.Game.UI;

/// <summary>
/// Scene-backed 角色选择卡片。
/// </summary>
public partial class CharacterSelectCardUI : PanelContainer
{
    private Label? titleLabel;
    private Label? statsLabel;
    private Label? skillsLabel;
    private Button? selectButton;
    private string characterId = string.Empty;

    public event Action<string>? SelectPressed;

    public override void _Ready()
    {
        titleLabel = GetNode<Label>("Margin/Content/TitleLabel");
        statsLabel = GetNode<Label>("Margin/Content/StatsLabel");
        skillsLabel = GetNode<Label>("Margin/Content/SkillsLabel");
        selectButton = GetNode<Button>("Margin/Content/SelectButton");
        selectButton.Pressed += () =>
        {
            if (!string.IsNullOrWhiteSpace(characterId))
            {
                SelectPressed?.Invoke(characterId);
            }
        };
    }

    public void Bind(BrotatoLikeCharacterDefinition character, string visibleAbilityIds, bool selected)
    {
        characterId = character.Id;
        if (titleLabel != null)
        {
            titleLabel.Text = character.DisplayName;
        }

        if (statsLabel != null)
        {
            statsLabel.Text = $"HP {character.MaxHp:0}  SPD {character.MoveSpeed:0}  ATK {character.AttackDamage:0}";
        }

        if (skillsLabel != null)
        {
            skillsLabel.Text = visibleAbilityIds;
        }

        if (selectButton != null)
        {
            selectButton.Text = selected ? "已选择" : "选择";
            selectButton.Disabled = selected;
        }

        SetMeta("CharacterId", character.Id);
        SetMeta("DisplayName", character.DisplayName);
        SetMeta("PlayerRecordId", character.PlayerRecordId);
        SetMeta("VisualScenePath", character.VisualScenePath);
        SetMeta("StartingLoadoutId", character.StartingLoadoutId);
        SetMeta("VisibleAbilityIds", visibleAbilityIds);
        SetMeta("Selected", selected);
    }

    public void Clear()
    {
        characterId = string.Empty;
        if (titleLabel != null)
        {
            titleLabel.Text = string.Empty;
        }

        if (statsLabel != null)
        {
            statsLabel.Text = string.Empty;
        }

        if (skillsLabel != null)
        {
            skillsLabel.Text = string.Empty;
        }

        if (selectButton != null)
        {
            selectButton.Text = "-";
            selectButton.Disabled = true;
        }

        SetMeta("CharacterId", string.Empty);
        SetMeta("Selected", false);
    }
}
