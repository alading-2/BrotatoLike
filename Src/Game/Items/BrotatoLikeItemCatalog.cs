using System;
using System.Collections.Generic;
using System.Text.Json;
using BrotatoLike.Game.Shop;
using Godot;

namespace BrotatoLike.Game.Items;

/// <summary>
/// BrotatoLike 道具和商店 offer authoring catalog。
/// </summary>
public sealed class BrotatoLikeItemCatalog
{
    private readonly Dictionary<string, BrotatoLikeItemDefinition> itemsById;
    private readonly List<BrotatoLikeShopOfferDefinition> offers;

    private BrotatoLikeItemCatalog(
        BrotatoLikeShopItemAuthoringSnapshot snapshot,
        Dictionary<string, BrotatoLikeItemDefinition> itemsById,
        List<BrotatoLikeShopOfferDefinition> offers)
    {
        Snapshot = snapshot;
        this.itemsById = itemsById;
        this.offers = offers;
    }

    /// <summary>
    /// DataOS 导出的原始 shop/item 快照。
    /// </summary>
    public BrotatoLikeShopItemAuthoringSnapshot Snapshot { get; }

    /// <summary>
    /// 所有道具定义。
    /// </summary>
    public IReadOnlyCollection<BrotatoLikeItemDefinition> Items => itemsById.Values;

    /// <summary>
    /// 从 Godot res:// 路径读取 shop/item authoring。
    /// </summary>
    public static BrotatoLikeItemCatalog LoadFromResource(string path = "res://DataOS/Snapshots/shop_item_authoring.json")
    {
        using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
        if (file == null)
        {
            throw new InvalidOperationException($"BrotatoLike shop/item authoring not found: {path}");
        }

        return FromJson(file.GetAsText());
    }

    /// <summary>
    /// 从 JSON 文本读取 shop/item authoring。
    /// </summary>
    public static BrotatoLikeItemCatalog FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var snapshot = JsonSerializer.Deserialize<BrotatoLikeShopItemAuthoringSnapshot>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("BrotatoLike shop/item authoring JSON 解析失败。");

        var itemsById = new Dictionary<string, BrotatoLikeItemDefinition>(StringComparer.Ordinal);
        for (var i = 0; i < snapshot.Items.Count; i++)
        {
            var item = snapshot.Items[i];
            if (string.IsNullOrWhiteSpace(item.Id))
            {
                throw new InvalidOperationException("BrotatoLike item id is empty.");
            }

            if (!itemsById.TryAdd(item.Id, item))
            {
                throw new InvalidOperationException($"Duplicate BrotatoLike item id: {item.Id}");
            }

            if (!string.Equals(item.EffectOperation, "Add", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Unsupported item effect operation: {item.Id}/{item.EffectOperation}");
            }

            if (!BrotatoLikeItemEffectApplicator.IsSupportedTarget(item.EffectTarget))
            {
                throw new InvalidOperationException($"Unknown item effect target: {item.Id}/{item.EffectTarget}");
            }
        }

        var offers = new List<BrotatoLikeShopOfferDefinition>(snapshot.Offers.Count);
        for (var i = 0; i < snapshot.Offers.Count; i++)
        {
            var offer = snapshot.Offers[i];
            if (!itemsById.ContainsKey(offer.ItemId))
            {
                throw new InvalidOperationException($"Shop offer references missing item id: {offer.OfferSetId}/{offer.ItemId}");
            }

            offers.Add(offer);
        }

        offers.Sort(CompareOffers);
        return new BrotatoLikeItemCatalog(snapshot, itemsById, offers);
    }

    /// <summary>
    /// 查找道具定义。
    /// </summary>
    public bool TryGetItem(string itemId, out BrotatoLikeItemDefinition item)
    {
        return itemsById.TryGetValue(itemId, out item!);
    }

    /// <summary>
    /// 读取指定 offer set 的确定性 offer。
    /// </summary>
    public IReadOnlyList<BrotatoLikeShopOfferDefinition> GetOffers(string offerSetId)
    {
        var result = new List<BrotatoLikeShopOfferDefinition>();
        for (var i = 0; i < offers.Count; i++)
        {
            if (string.Equals(offers[i].OfferSetId, offerSetId, StringComparison.Ordinal))
            {
                result.Add(offers[i]);
            }
        }

        return result;
    }

    private static int CompareOffers(BrotatoLikeShopOfferDefinition left, BrotatoLikeShopOfferDefinition right)
    {
        var set = string.Compare(left.OfferSetId, right.OfferSetId, StringComparison.Ordinal);
        return set != 0 ? set : left.SlotIndex.CompareTo(right.SlotIndex);
    }
}
