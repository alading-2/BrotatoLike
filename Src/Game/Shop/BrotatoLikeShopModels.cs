using BrotatoLike.Game.Items;

namespace BrotatoLike.Game.Shop;

/// <summary>
/// 商店 UI 展示用 offer。
/// </summary>
public sealed record BrotatoLikeShopOfferView(
    int SlotIndex,
    string ItemId,
    string DisplayName,
    int Price,
    string Rarity,
    string IconPath,
    string EffectTarget,
    float EffectValue,
    string EffectDescription,
    string OfferSource,
    bool Affordable,
    bool Purchased,
    bool CloseShopOnPurchase)
{
    public static BrotatoLikeShopOfferView From(
        BrotatoLikeShopOfferDefinition offer,
        BrotatoLikeItemDefinition item,
        int currency,
        bool purchased)
    {
        var price = offer.PriceOverride ?? item.BasePrice;
        return new BrotatoLikeShopOfferView(
            offer.SlotIndex,
            item.Id,
            item.DisplayName,
            price,
            item.Rarity,
            item.IconPath,
            item.EffectTarget,
            item.EffectValue,
            item.EffectDescription,
            offer.OfferSource,
            currency >= price,
            purchased,
            offer.CloseShopOnPurchase);
    }
}

/// <summary>
/// 购买结果，用于 UI 与 validation artifact。
/// </summary>
public sealed record BrotatoLikePurchaseResult(
    bool Success,
    string ItemId,
    int SlotIndex,
    int Price,
    int CurrencyBefore,
    int CurrencyAfter,
    string EffectTarget,
    string BeforeValue,
    string AfterValue,
    string RejectedReason,
    string Message)
{
    public static BrotatoLikePurchaseResult Empty { get; } = new(
        false,
        string.Empty,
        -1,
        0,
        0,
        0,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty);
}
