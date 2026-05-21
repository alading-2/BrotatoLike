using System;
using BrotatoLike.Game.Shop;
using Godot;

namespace BrotatoLike.Game.UI;

/// <summary>
/// 商店道具 offer 卡片。
/// </summary>
public partial class ShopOfferCardUI : PanelContainer
{
    private Label? titleLabel;
    private Label? rarityLabel;
    private Label? effectLabel;
    private Label? priceLabel;
    private Button? purchaseButton;
    private int slotIndex = -1;

    /// <summary>
    /// 点击购买按钮。
    /// </summary>
    public event Action<int>? PurchasePressed;

    /// <inheritdoc />
    public override void _Ready()
    {
        titleLabel = GetNode<Label>("Margin/Content/Header/TitleLabel");
        rarityLabel = GetNode<Label>("Margin/Content/Header/RarityLabel");
        effectLabel = GetNode<Label>("Margin/Content/EffectLabel");
        priceLabel = GetNode<Label>("Margin/Content/Footer/PriceLabel");
        purchaseButton = GetNode<Button>("Margin/Content/Footer/PurchaseButton");
        purchaseButton.Pressed += () =>
        {
            if (slotIndex >= 0)
            {
                PurchasePressed?.Invoke(slotIndex);
            }
        };
    }

    /// <summary>
    /// 绑定 offer 展示数据。
    /// </summary>
    public void Bind(BrotatoLikeShopOfferView offer)
    {
        slotIndex = offer.SlotIndex;
        if (titleLabel != null)
        {
            titleLabel.Text = offer.DisplayName;
        }

        if (rarityLabel != null)
        {
            rarityLabel.Text = offer.Rarity;
        }

        if (effectLabel != null)
        {
            effectLabel.Text = offer.EffectDescription;
        }

        if (priceLabel != null)
        {
            priceLabel.Text = $"{offer.Price}";
        }

        if (purchaseButton != null)
        {
            purchaseButton.Text = offer.Purchased ? "已购买" : "购买";
            purchaseButton.Disabled = offer.Purchased || !offer.Affordable;
        }

        SetMeta("SlotIndex", offer.SlotIndex);
        SetMeta("ItemId", offer.ItemId);
        SetMeta("DisplayName", offer.DisplayName);
        SetMeta("Price", offer.Price);
        SetMeta("Rarity", offer.Rarity);
        SetMeta("EffectTarget", offer.EffectTarget);
        SetMeta("EffectValue", offer.EffectValue);
        SetMeta("OfferSource", offer.OfferSource);
        SetMeta("Affordable", offer.Affordable);
        SetMeta("Purchased", offer.Purchased);
    }

    /// <summary>
    /// 清空卡片。
    /// </summary>
    public void Clear()
    {
        slotIndex = -1;
        if (titleLabel != null)
        {
            titleLabel.Text = string.Empty;
        }

        if (rarityLabel != null)
        {
            rarityLabel.Text = string.Empty;
        }

        if (effectLabel != null)
        {
            effectLabel.Text = string.Empty;
        }

        if (priceLabel != null)
        {
            priceLabel.Text = string.Empty;
        }

        if (purchaseButton != null)
        {
            purchaseButton.Text = "-";
            purchaseButton.Disabled = true;
        }

        SetMeta("SlotIndex", -1);
        SetMeta("ItemId", string.Empty);
    }
}
