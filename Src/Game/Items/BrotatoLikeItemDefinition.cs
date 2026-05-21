using System.Collections.Generic;

namespace BrotatoLike.Game.Items;

/// <summary>
/// BrotatoLike 道具定义，由 DataOS item_definition 导出。
/// </summary>
public sealed class BrotatoLikeItemDefinition
{
    public string Id { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public int BasePrice { get; init; }

    public string Rarity { get; init; } = string.Empty;

    public int Weight { get; init; } = 1;

    public string IconPath { get; init; } = string.Empty;

    public string EffectTarget { get; init; } = string.Empty;

    public string EffectOperation { get; init; } = string.Empty;

    public float EffectValue { get; init; }

    public string EffectDescription { get; init; } = string.Empty;
}

/// <summary>
/// BrotatoLike 商店 offer 定义，由 DataOS shop_offer 导出。
/// </summary>
public sealed class BrotatoLikeShopOfferDefinition
{
    public string OfferSetId { get; init; } = string.Empty;

    public int SlotIndex { get; init; }

    public string ItemId { get; init; } = string.Empty;

    public int? PriceOverride { get; init; }

    public string OfferSource { get; init; } = string.Empty;

    public bool CloseShopOnPurchase { get; init; }
}

/// <summary>
/// DataOS shop/item authoring 导出快照。
/// </summary>
public sealed class BrotatoLikeShopItemAuthoringSnapshot
{
    public int SchemaVersion { get; init; }

    public string GeneratedAtUtc { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;

    public List<BrotatoLikeItemDefinition> Items { get; init; } = new();

    public List<BrotatoLikeShopOfferDefinition> Offers { get; init; } = new();
}
