using System;
using System.Collections.Generic;
using BrotatoLike.Game.Items;
using BrotatoLike.Game.UI;
using Godot;
using SlimeAI.GameOS.GodotBridge;

namespace BrotatoLike.Game.Shop;

/// <summary>
/// BrotatoLike 商店服务：货币、确定性 offer、购买门禁和道具效果。
/// </summary>
public partial class BrotatoLikeShopService : Node
{
    public const int DefaultStartingCurrency = 15;
    public const string DefaultOfferSetId = "validation";

    private readonly HashSet<int> purchasedSlots = new();
    private BrotatoLikeGameRuntime? runtime;
    private BrotatoLikeItemCatalog? catalog;
    private ShopPanelUI? panel;
    private string currentOfferSetId = DefaultOfferSetId;
    private IReadOnlyList<BrotatoLikeShopOfferDefinition> currentOfferDefinitions = Array.Empty<BrotatoLikeShopOfferDefinition>();
    private List<BrotatoLikeShopOfferView> currentOffers = new();

    /// <summary>
    /// 当前 UI/购买流程使用的 offer。
    /// </summary>
    public IReadOnlyList<BrotatoLikeShopOfferView> CurrentOffers => currentOffers;

    /// <summary>
    /// 最近一次购买结果。
    /// </summary>
    public BrotatoLikePurchaseResult LastPurchaseResult { get; private set; } = BrotatoLikePurchaseResult.Empty;

    /// <summary>
    /// 当前 offer 来源。
    /// </summary>
    public string CurrentOfferSource { get; private set; } = string.Empty;

    /// <summary>
    /// 绑定运行时和 item catalog。
    /// </summary>
    public void Bind(BrotatoLikeGameRuntime runtime, BrotatoLikeItemCatalog catalog)
    {
        this.runtime = runtime;
        this.catalog = catalog;
        EnsureCurrency();
    }

    /// <summary>
    /// 设置玩家货币，供验证和后续奖励流程使用。
    /// </summary>
    public void SetCurrency(int amount)
    {
        var player = runtime?.PlayerEntity;
        if (player == null)
        {
            return;
        }

        player.Data.Set(BrotatoLikeShopDataKeys.Currency, Math.Max(0, amount));
        player.SetMeta("Currency", Math.Max(0, amount));
        RefreshPanel();
    }

    /// <summary>
    /// 读取当前玩家货币。
    /// </summary>
    public int GetCurrency()
    {
        var player = runtime?.PlayerEntity;
        return player?.Data.Get<int>(BrotatoLikeShopDataKeys.Currency, 0) ?? 0;
    }

    /// <summary>
    /// 打开指定 offer set 的商店面板。
    /// </summary>
    public IReadOnlyList<BrotatoLikeShopOfferView> OpenShop(string offerSetId = DefaultOfferSetId)
    {
        if (catalog == null)
        {
            throw new InvalidOperationException("BrotatoLikeShopService catalog is not bound.");
        }

        currentOfferSetId = offerSetId;
        purchasedSlots.Clear();
        LastPurchaseResult = BrotatoLikePurchaseResult.Empty;
        currentOfferDefinitions = catalog.GetOffers(offerSetId);
        if (currentOfferDefinitions.Count == 0)
        {
            throw new InvalidOperationException($"BrotatoLike shop offer set not found: {offerSetId}");
        }

        CurrentOfferSource = JoinOfferSources(currentOfferDefinitions);
        RebuildOfferViews();
        EnsurePanel();
        panel?.ShowShop(GetCurrency(), currentOffers, CurrentOfferSource);
        return currentOffers;
    }

    /// <summary>
    /// 关闭商店面板。
    /// </summary>
    public void CloseShop()
    {
        panel?.HidePanel();
    }

    /// <summary>
    /// 购买指定 slot。
    /// </summary>
    public BrotatoLikePurchaseResult PurchaseSlot(int slotIndex)
    {
        var player = runtime?.PlayerEntity;
        if (player == null || catalog == null)
        {
            LastPurchaseResult = Reject(slotIndex, string.Empty, 0, "runtime_not_ready");
            RefreshPanel();
            return LastPurchaseResult;
        }

        var offer = FindOffer(slotIndex);
        if (offer == null)
        {
            LastPurchaseResult = Reject(slotIndex, string.Empty, 0, "offer_not_found");
            RefreshPanel();
            return LastPurchaseResult;
        }

        if (!catalog.TryGetItem(offer.ItemId, out var item))
        {
            LastPurchaseResult = Reject(slotIndex, offer.ItemId, 0, "item_not_found");
            RefreshPanel();
            return LastPurchaseResult;
        }

        var price = offer.PriceOverride ?? item.BasePrice;
        var currencyBefore = GetCurrency();
        if (purchasedSlots.Contains(slotIndex))
        {
            LastPurchaseResult = Reject(slotIndex, item.Id, price, "already_purchased");
            RefreshPanel();
            return LastPurchaseResult;
        }

        if (currencyBefore < price)
        {
            LastPurchaseResult = new BrotatoLikePurchaseResult(
                false,
                item.Id,
                slotIndex,
                price,
                currencyBefore,
                currencyBefore,
                item.EffectTarget,
                string.Empty,
                string.Empty,
                "insufficient_currency",
                "currency is lower than price");
            RefreshPanel();
            return LastPurchaseResult;
        }

        if (!BrotatoLikeItemEffectApplicator.TryApply(player, item, out var before, out var after, out var message))
        {
            LastPurchaseResult = new BrotatoLikePurchaseResult(
                false,
                item.Id,
                slotIndex,
                price,
                currencyBefore,
                currencyBefore,
                item.EffectTarget,
                before,
                after,
                message,
                message);
            RefreshPanel();
            return LastPurchaseResult;
        }

        var currencyAfter = currencyBefore - price;
        player.Data.Set(BrotatoLikeShopDataKeys.Currency, currencyAfter);
        player.SetMeta("Currency", currencyAfter);
        AddOwnedItem(player, item.Id);
        purchasedSlots.Add(slotIndex);
        LastPurchaseResult = new BrotatoLikePurchaseResult(
            true,
            item.Id,
            slotIndex,
            price,
            currencyBefore,
            currencyAfter,
            item.EffectTarget,
            before,
            after,
            string.Empty,
            message);

        RefreshPanel();
        if (offer.CloseShopOnPurchase)
        {
            CloseShop();
        }

        return LastPurchaseResult;
    }

    private void EnsureCurrency()
    {
        var player = runtime?.PlayerEntity;
        if (player == null)
        {
            return;
        }

        if (!player.Data.Has(BrotatoLikeShopDataKeys.Currency))
        {
            player.Data.Set(BrotatoLikeShopDataKeys.Currency, DefaultStartingCurrency);
        }

        player.SetMeta("Currency", player.Data.Get<int>(BrotatoLikeShopDataKeys.Currency, 0));
    }

    private void EnsurePanel()
    {
        if (panel != null && GodotObject.IsInstanceValid(panel))
        {
            return;
        }

        var scene = GD.Load<PackedScene>("res://Scenes/UI/ShopPanelUI.tscn");
        panel = scene.Instantiate<ShopPanelUI>();
        panel.Name = "ShopPanelUI";
        panel.PurchasePressed += slot => PurchaseSlot(slot);
        panel.ClosePressed += CloseShop;
        AddChild(panel);
    }

    private void RefreshPanel()
    {
        RebuildOfferViews();
        panel?.UpdateShop(GetCurrency(), currentOffers, LastPurchaseResult);
    }

    private void RebuildOfferViews()
    {
        currentOffers = new List<BrotatoLikeShopOfferView>(currentOfferDefinitions.Count);
        if (catalog == null)
        {
            return;
        }

        var currency = GetCurrency();
        for (var i = 0; i < currentOfferDefinitions.Count; i++)
        {
            var offer = currentOfferDefinitions[i];
            if (!catalog.TryGetItem(offer.ItemId, out var item))
            {
                continue;
            }

            currentOffers.Add(BrotatoLikeShopOfferView.From(
                offer,
                item,
                currency,
                purchasedSlots.Contains(offer.SlotIndex)));
        }
    }

    private BrotatoLikeShopOfferDefinition? FindOffer(int slotIndex)
    {
        for (var i = 0; i < currentOfferDefinitions.Count; i++)
        {
            if (currentOfferDefinitions[i].SlotIndex == slotIndex)
            {
                return currentOfferDefinitions[i];
            }
        }

        return null;
    }

    private static void AddOwnedItem(GodotEntity2D player, string itemId)
    {
        var existing = player.HasMeta("OwnedItemIds") ? player.GetMeta("OwnedItemIds").AsString() : string.Empty;
        var next = string.IsNullOrWhiteSpace(existing) ? itemId : $"{existing},{itemId}";
        player.SetMeta("OwnedItemIds", next);
        player.SetMeta("OwnedItemCount", CountCsv(next));
        player.SetMeta("LastAcquiredItemId", itemId);
    }

    private static int CountCsv(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? 0
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
    }

    private static string JoinOfferSources(IReadOnlyList<BrotatoLikeShopOfferDefinition> offers)
    {
        var values = new string[offers.Count];
        for (var i = 0; i < offers.Count; i++)
        {
            values[i] = offers[i].OfferSource;
        }

        return string.Join(",", values);
    }

    private static BrotatoLikePurchaseResult Reject(int slotIndex, string itemId, int price, string reason)
    {
        return new BrotatoLikePurchaseResult(
            false,
            itemId,
            slotIndex,
            price,
            0,
            0,
            string.Empty,
            string.Empty,
            string.Empty,
            reason,
            reason);
    }
}
