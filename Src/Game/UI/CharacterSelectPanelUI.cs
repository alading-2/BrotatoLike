using System;
using System.Collections.Generic;
using BrotatoLike.Game.Characters;
using Godot;

namespace BrotatoLike.Game.UI;

/// <summary>
/// Scene-backed 角色选择面板。
/// </summary>
public partial class CharacterSelectPanelUI : CanvasLayer
{
    private Control? root;
    private Label? selectedLabel;
    private readonly List<CharacterSelectCardUI> cards = new();

    public event Action<string>? CharacterSelected;

    public bool IsPanelVisible => root?.Visible ?? false;

    public override void _Ready()
    {
        root = GetNode<Control>("Root");
        selectedLabel = root.GetNode<Label>("Panel/Margin/Content/Header/SelectedLabel");
        var cardContainer = root.GetNode<HBoxContainer>("Panel/Margin/Content/CharacterCards");
        cards.Clear();
        foreach (var child in cardContainer.GetChildren())
        {
            if (child is CharacterSelectCardUI card)
            {
                card.SelectPressed += id => CharacterSelected?.Invoke(id);
                cards.Add(card);
            }
        }

        HidePanel();
    }

    public void ShowCharacters(BrotatoLikeCharacterCatalog catalog, string selectedCharacterId)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        if (root == null)
        {
            return;
        }

        root.Visible = true;
        for (var i = 0; i < cards.Count; i++)
        {
            if (i < catalog.Characters.Count)
            {
                var character = catalog.Characters[i];
                cards[i].Bind(
                    character,
                    catalog.JoinVisibleAbilityIds(character),
                    string.Equals(character.Id, selectedCharacterId, StringComparison.Ordinal));
            }
            else
            {
                cards[i].Clear();
            }
        }

        if (selectedLabel != null)
        {
            selectedLabel.Text = selectedCharacterId;
        }

        SetMeta("SceneBacked", !string.IsNullOrWhiteSpace(SceneFilePath));
        SetMeta("ScenePath", SceneFilePath);
        SetMeta("VisibleCharacterIds", JoinCharacterIds(catalog.Characters));
        SetMeta("SelectedCharacterId", selectedCharacterId);
        SetMeta("CardCount", cards.Count);
    }

    public void HidePanel()
    {
        if (root != null)
        {
            root.Visible = false;
        }

        SetMeta("Closed", true);
    }

    private static string JoinCharacterIds(IReadOnlyList<BrotatoLikeCharacterDefinition> characters)
    {
        var ids = new string[characters.Count];
        for (var i = 0; i < characters.Count; i++)
        {
            ids[i] = characters[i].Id;
        }

        return string.Join(",", ids);
    }
}
