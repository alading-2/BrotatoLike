using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BrotatoLike.Game;
using BrotatoLike.Game.Items;
using BrotatoLike.Game.Shop;
using BrotatoLike.Game.UI;
using Godot;
using SlimeAI.GameOS.Capabilities.Attack;
using SlimeAI.GameOS.Capabilities.Damage;
using SlimeAI.GameOS.Observation;
using SlimeAI.GameOS.Runtime.Entity;

namespace BrotatoLike.Validation.Game.Shop;

/// <summary>
/// BrotatoLike 商店、道具、货币和购买闭环验证场景。
/// </summary>
public partial class BrotatoLikeShopItemValidationScene : Node
{
    private const string ScenePath = "res://Src/Validation/Game/Shop/BrotatoLikeShopItemValidation.tscn";
    private const string ArtifactFileName = "brotatolike-shop-item-validation.json";
    private const string PassMarker = "BrotatoLike Shop Item validation PASS";
    private const string FailMarker = "BrotatoLike Shop Item validation FAIL";

    /// <inheritdoc />
    public override async void _Ready()
    {
        EntityManager.Clear();
        BrotatoLikeAbilityHandlers.RegisterAll();

        using var observation = GameOSObservationSession.FromEnvironment(
            ScenePath,
            "validation",
            Path.Combine(Directory.GetCurrentDirectory(), ".ai-temp", "scene-tests", "manual", "artifacts"));
        using var validation = new SceneValidationSession(
            observation,
            "BrotatoLikeShopItemValidation",
            "Game/Shop",
            ArtifactFileName,
            dependencies: new[]
            {
                "BrotatoLike.Game.BrotatoLikeGameRuntime",
                "BrotatoLike.Game.Items.BrotatoLikeItemCatalog",
                "BrotatoLike.Game.Shop.BrotatoLikeShopService",
                "BrotatoLike.Game.UI.ShopPanelUI",
                "SlimeAI.GameOS.Runtime.Data"
            },
            notes: new[]
            {
                "Validation uses the DataOS generated shop_item_authoring.json.",
                "The first pass item set is intentionally small and deterministic.",
                "Full wave-break shop scheduling and meta progression are out of scope."
            },
            expectedInputs: new[]
            {
                "BrotatoLikeGameRuntime initialized from DataOS snapshot",
                "DataOS generated item_definition and shop_offer authoring",
                "Validation offer set with deterministic item ids and prices",
                "Player currency stored on runtime data and shop purchase commands"
            },
            expectedObservations: new[]
            {
                "item definitions include id, display name, price, effect target/value, rarity and optional icon path",
                "unknown item effect target fails catalog validation",
                "shop UI records offer ids, prices, currency, affordable state, purchase result and close state",
                "affordable purchase deducts currency, acquires the item and mutates runtime gameplay data",
                "unaffordable purchase preserves currency/item state and records insufficient_currency"
            },
            passCriteria: new[]
            {
                $"stdout contains {PassMarker}",
                "artifact status is pass",
                "all shop/item checks pass and standard-answer fields are non-empty"
            },
            failCriteria: new[]
            {
                $"stdout contains {FailMarker}",
                "item authoring, deterministic offers, purchase gate, effect mutation or UI cleanup evidence is missing",
                "artifact status is fail with feature-level failureReasons"
            });

        validation.Info("validation start");
        var values = await RunShopProbe();
        validation.Check("item_authoring_loaded_and_validates_targets", "Authoring", () => Result(values, "item_authoring_loaded_and_validates_targets"));
        validation.Check("shop_offers_deterministic", "Shop", () => Result(values, "shop_offers_deterministic"));
        validation.Check("affordable_purchase_applies_item", "Purchase", () => Result(values, "affordable_purchase_applies_item"));
        validation.Check("unaffordable_purchase_rejected", "Purchase", () => Result(values, "unaffordable_purchase_rejected"));
        validation.Check("shop_ui_updates_and_cleanup", "UI", () => Result(values, "shop_ui_updates_and_cleanup"));

        var success = validation.Success;
        if (success)
        {
            validation.Pass("all checks passed");
        }
        else
        {
            validation.Fail($"{validation.FailureReasons.Count} checks failed");
        }

        EntityManager.Clear();
        validation.WriteArtifact();
        GD.Print(success ? PassMarker : FailMarker);
        if (!success)
        {
            GD.Print($"BrotatoLike Shop Item failures: {string.Join("; ", validation.FailureReasons)}");
        }

        GetTree().Quit(success ? 0 : 1);
    }

    private async Task<Dictionary<string, object?>> RunShopProbe()
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        var catalog = BrotatoLikeItemCatalog.LoadFromResource();
        var unknownTargetRejected = ThrowsUnknownEffectTarget();

        var runtime = new BrotatoLikeGameRuntime
        {
            Name = "GameRuntime",
            AutoInitialize = false,
            AutoTick = false
        };
        AddChild(runtime);
        runtime.InitializeFromDataOS(1, runtime);
        runtime.BeginGameplay();
        var player = runtime.SpawnPlayer("deluyi", Vector2.Zero);
        await ProcessFrames(3);

        var service = runtime.ShopService!;
        service.SetCurrency(15);
        var maxHpBefore = player.Data.Get<float>(DamageDataKeys.MaxHp, 0f);
        var attackDamageBefore = player.Data.Get<float>(AttackDataKeys.Damage, 0f);
        var offers = runtime.OpenShop("validation");
        await ProcessFrames(3);
        var panel = FindDescendant(this, "ShopPanelUI") as ShopPanelUI;
        var panelSceneBacked = IsSceneBacked(panel);
        var offerIds = ReadStringMeta(panel, "OfferItemIds");
        var offerPrices = ReadStringMeta(panel, "OfferPrices");
        var offerSource = service.CurrentOfferSource;
        var panelVisibleOpen = panel?.IsPanelVisible ?? false;
        var currencyBeforePurchase = service.GetCurrency();

        var affordable = service.PurchaseSlot(0);
        await ProcessFrames(2);
        var maxHpAfterPurchase = player.Data.Get<float>(DamageDataKeys.MaxHp, 0f);
        var currencyAfterPurchase = service.GetCurrency();
        var ownedItemsAfterPurchase = ReadStringMeta(player, "OwnedItemIds");
        var panelCurrencyAfterPurchase = ReadIntMeta(panel, "Currency");
        var uiLastPurchaseSuccess = ReadBoolMeta(panel, "LastPurchaseSuccess");
        var purchasedStates = ReadStringMeta(panel, "PurchasedStates");

        var unaffordable = service.PurchaseSlot(2);
        await ProcessFrames(2);
        var currencyAfterReject = service.GetCurrency();
        var attackDamageAfterReject = player.Data.Get<float>(AttackDataKeys.Damage, 0f);
        var ownedItemsAfterReject = ReadStringMeta(player, "OwnedItemIds");
        var uiRejectReason = ReadStringMeta(panel, "LastPurchaseRejectedReason");
        service.CloseShop();
        await ProcessFrames(1);
        var panelVisibleAfterClose = panel?.IsPanelVisible ?? true;

        values["item_count"] = catalog.Items.Count;
        values["catalog_source"] = catalog.Snapshot.Source;
        values["unknown_target_rejected"] = unknownTargetRejected;
        values["offer_ids"] = offerIds;
        values["offer_prices"] = offerPrices;
        values["offer_source"] = offerSource;
        values["offer_count"] = offers.Count;
        values["shop_panel_scene_path"] = panel?.SceneFilePath ?? string.Empty;
        values["shop_panel_visible_open"] = panelVisibleOpen;
        values["currency_before_purchase"] = currencyBeforePurchase;
        values["affordable_purchase_success"] = affordable.Success;
        values["affordable_item_id"] = affordable.ItemId;
        values["affordable_currency_before"] = affordable.CurrencyBefore;
        values["affordable_currency_after"] = affordable.CurrencyAfter;
        values["affordable_effect_target"] = affordable.EffectTarget;
        values["affordable_before_value"] = affordable.BeforeValue;
        values["affordable_after_value"] = affordable.AfterValue;
        values["max_hp_before_purchase"] = maxHpBefore;
        values["max_hp_after_purchase"] = maxHpAfterPurchase;
        values["currency_after_purchase"] = currencyAfterPurchase;
        values["owned_items_after_purchase"] = ownedItemsAfterPurchase;
        values["panel_currency_after_purchase"] = panelCurrencyAfterPurchase;
        values["ui_last_purchase_success"] = uiLastPurchaseSuccess;
        values["purchased_states"] = purchasedStates;
        values["unaffordable_purchase_success"] = unaffordable.Success;
        values["unaffordable_item_id"] = unaffordable.ItemId;
        values["unaffordable_rejected_reason"] = unaffordable.RejectedReason;
        values["currency_after_reject"] = currencyAfterReject;
        values["attack_damage_before_reject"] = attackDamageBefore;
        values["attack_damage_after_reject"] = attackDamageAfterReject;
        values["owned_items_after_reject"] = ownedItemsAfterReject;
        values["ui_reject_reason"] = uiRejectReason;
        values["shop_panel_visible_after_close"] = panelVisibleAfterClose;

        values["item_authoring_loaded_and_validates_targets"] = catalog.Items.Count >= 3
            && unknownTargetRejected
            && catalog.TryGetItem("vital_seed", out var vitalSeed)
            && vitalSeed.BasePrice == 12
            && vitalSeed.EffectTarget == DamageDataKeys.MaxHp.StableKey;
        values["shop_offers_deterministic"] = offers.Count == 3
            && offerIds == "vital_seed,swift_boots,sharpening_stone"
            && offerPrices == "12,8,20"
            && offerSource.Contains("DataOS:shop_offer.validation", StringComparison.Ordinal);
        values["affordable_purchase_applies_item"] = affordable.Success
            && affordable.ItemId == "vital_seed"
            && affordable.CurrencyBefore == 15
            && affordable.CurrencyAfter == 3
            && currencyAfterPurchase == 3
            && maxHpAfterPurchase > maxHpBefore
            && ownedItemsAfterPurchase.Contains("vital_seed", StringComparison.Ordinal);
        values["unaffordable_purchase_rejected"] = !unaffordable.Success
            && unaffordable.ItemId == "sharpening_stone"
            && unaffordable.RejectedReason == "insufficient_currency"
            && currencyAfterReject == currencyAfterPurchase
            && Math.Abs(attackDamageAfterReject - attackDamageBefore) < 0.001f
            && ownedItemsAfterReject == ownedItemsAfterPurchase;
        values["shop_ui_updates_and_cleanup"] = panelSceneBacked
            && panelVisibleOpen
            && panelCurrencyAfterPurchase == 3
            && uiLastPurchaseSuccess
            && purchasedStates.StartsWith("true", StringComparison.Ordinal)
            && uiRejectReason == "insufficient_currency"
            && !panelVisibleAfterClose;

        return values;
    }

    private async Task ProcessFrames(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private static bool ThrowsUnknownEffectTarget()
    {
        const string invalidJson = """
        {
          "schemaVersion": 1,
          "generatedAtUtc": "1970-01-01T00:00:00Z",
          "source": "validation-invalid",
          "items": [
            {
              "id": "invalid",
              "displayName": "Invalid",
              "basePrice": 1,
              "rarity": "Common",
              "weight": 1,
              "iconPath": "",
              "effectTarget": "Unknown.Target",
              "effectOperation": "Add",
              "effectValue": 1,
              "effectDescription": "invalid"
            }
          ],
          "offers": []
        }
        """;

        try
        {
            _ = BrotatoLikeItemCatalog.FromJson(invalidJson);
            return false;
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message.Contains("Unknown item effect target", StringComparison.Ordinal);
        }
    }

    private static CheckResult Result(IReadOnlyDictionary<string, object?> values, string key)
    {
        var success = values.TryGetValue(key, out var raw) && raw is bool value && value;
        return CheckResult.From(success, success ? $"{key} passed" : $"{key} failed", values);
    }

    private static Node? FindDescendant(Node root, string name)
    {
        if (root.Name == name)
        {
            return root;
        }

        foreach (var child in root.GetChildren())
        {
            if (child.Name == name)
            {
                return child;
            }

            var descendant = FindDescendant(child, name);
            if (descendant != null)
            {
                return descendant;
            }
        }

        return null;
    }

    private static string ReadStringMeta(Node? node, string key)
    {
        return node != null && node.HasMeta(key)
            ? node.GetMeta(key).AsString()
            : string.Empty;
    }

    private static int ReadIntMeta(Node? node, string key)
    {
        if (node == null || !node.HasMeta(key))
        {
            return 0;
        }

        var value = node.GetMeta(key);
        return value.VariantType switch
        {
            Variant.Type.Int => value.AsInt32(),
            Variant.Type.Float => Mathf.RoundToInt(value.AsSingle()),
            Variant.Type.String => int.TryParse(value.AsString(), out var parsed) ? parsed : 0,
            _ => 0
        };
    }

    private static bool ReadBoolMeta(Node? node, string key)
    {
        if (node == null || !node.HasMeta(key))
        {
            return false;
        }

        var value = node.GetMeta(key);
        return value.VariantType switch
        {
            Variant.Type.Bool => value.AsBool(),
            Variant.Type.Int => value.AsInt32() != 0,
            Variant.Type.String => bool.TryParse(value.AsString(), out var parsed) && parsed,
            _ => false
        };
    }

    private static bool IsSceneBacked(Node? node)
    {
        return node != null && GodotObject.IsInstanceValid(node) && !string.IsNullOrEmpty(node.SceneFilePath);
    }
}
