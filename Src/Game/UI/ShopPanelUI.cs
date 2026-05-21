using System;
using System.Collections.Generic;
using BrotatoLike.Game.Shop;
using Godot;

namespace BrotatoLike.Game.UI;

/// <summary>
/// Scene-backed 商店面板。
/// </summary>
public partial class ShopPanelUI : CanvasLayer
{
    private Control? root;
    private Label? currencyLabel;
    private Label? resultLabel;
    private Button? closeButton;
    private readonly List<ShopOfferCardUI> cards = new();

    /// <summary>
    /// 用户点击购买。
    /// </summary>
    public event Action<int>? PurchasePressed;

    /// <summary>
    /// 用户点击关闭。
    /// </summary>
    public event Action? ClosePressed;

    /// <summary>
    /// 面板是否可见。
    /// </summary>
    public bool IsPanelVisible => root?.Visible ?? false;

    /// <inheritdoc />
    public override void _Ready()
    {
        root = GetNode<Control>("Root");
        currencyLabel = root.GetNode<Label>("Panel/Body/Margin/Content/Header/CurrencyLabel");
        resultLabel = root.GetNode<Label>("Panel/Body/Margin/Content/ResultLabel");
        closeButton = root.GetNode<Button>("Panel/Body/Margin/Content/Footer/CloseButton");
        closeButton.Pressed += () => ClosePressed?.Invoke();

        var cardContainer = root.GetNode<HBoxContainer>("Panel/Body/Margin/Content/OfferCards");
        cards.Clear();
        foreach (var child in cardContainer.GetChildren())
        {
            if (child is ShopOfferCardUI card)
            {
                card.PurchasePressed += slot => PurchasePressed?.Invoke(slot);
                cards.Add(card);
            }
        }

        HidePanel();
    }

    /// <summary>
    /// 显示商店。
    /// </summary>
    public void ShowShop(int currency, IReadOnlyList<BrotatoLikeShopOfferView> offers, string offerSource)
    {
        if (root == null)
        {
            return;
        }

        root.Visible = true;
        SetMeta("Shown", true);
        SetMeta("Closed", false);
        SetMeta("SceneBacked", !string.IsNullOrEmpty(SceneFilePath));
        SetMeta("ScenePath", SceneFilePath);
        SetMeta("OfferSource", offerSource);
        BindOffers(currency, offers);
        if (resultLabel != null)
        {
            resultLabel.Text = string.Empty;
        }
    }

    /// <summary>
    /// 更新商店 UI。
    /// </summary>
    public void UpdateShop(
        int currency,
        IReadOnlyList<BrotatoLikeShopOfferView> offers,
        BrotatoLikePurchaseResult result)
    {
        BindOffers(currency, offers);
        if (resultLabel != null)
        {
            resultLabel.Text = result == BrotatoLikePurchaseResult.Empty
                ? string.Empty
                : result.Success
                    ? $"{result.ItemId} + {result.EffectTarget}"
                    : result.RejectedReason;
        }

        SetMeta("LastPurchaseSuccess", result.Success);
        SetMeta("LastPurchaseItemId", result.ItemId);
        SetMeta("LastPurchaseRejectedReason", result.RejectedReason);
        SetMeta("LastPurchaseCurrencyBefore", result.CurrencyBefore);
        SetMeta("LastPurchaseCurrencyAfter", result.CurrencyAfter);
        SetMeta("LastPurchaseBeforeValue", result.BeforeValue);
        SetMeta("LastPurchaseAfterValue", result.AfterValue);
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

    private void BindOffers(int currency, IReadOnlyList<BrotatoLikeShopOfferView> offers)
    {
        if (currencyLabel != null)
        {
            currencyLabel.Text = $"{currency}";
        }

        for (var i = 0; i < cards.Count; i++)
        {
            if (i < offers.Count)
            {
                cards[i].Bind(offers[i]);
            }
            else
            {
                cards[i].Clear();
            }
        }

        SetMeta("Currency", currency);
        SetMeta("OfferItemIds", JoinOffers(offers, offer => offer.ItemId));
        SetMeta("OfferPrices", JoinOffers(offers, offer => offer.Price.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        SetMeta("OfferAffordableStates", JoinOffers(offers, offer => offer.Affordable ? "true" : "false"));
        SetMeta("PurchasedStates", JoinOffers(offers, offer => offer.Purchased ? "true" : "false"));
    }

    private static string JoinOffers(
        IReadOnlyList<BrotatoLikeShopOfferView> offers,
        Func<BrotatoLikeShopOfferView, string> selector)
    {
        var values = new string[offers.Count];
        for (var i = 0; i < offers.Count; i++)
        {
            values[i] = selector(offers[i]);
        }

        return string.Join(",", values);
    }
}
