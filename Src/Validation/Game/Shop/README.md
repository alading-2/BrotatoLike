# BrotatoLike Shop Item Validation

## 测试目标

验证 BrotatoLike 第一版商店、道具、货币和购买闭环：DataOS authoring 导出、确定性 offer、scene-backed shop UI、可购买道具效果、买不起拒绝和 UI 清理。

## expectedInputs

- `BrotatoLikeGameRuntime` initialized from the BrotatoLike DataOS snapshot.
- DataOS generated `item_definition` and `shop_offer` authoring.
- Validation offer set with deterministic item ids and prices.
- Player currency stored on runtime data and shop purchase commands.

## expectedObservations

- Item definitions include id, display name, price, effect target/value, rarity and optional icon path.
- Unknown item effect target fails catalog validation.
- Shop UI records offer ids, prices, currency, affordable state, purchase result and close state.
- Affordable purchase deducts currency, acquires the item and mutates runtime gameplay data.
- Unaffordable purchase preserves currency/item state and records `insufficient_currency`.

## passCriteria

- Stdout contains `BrotatoLike Shop Item validation PASS`.
- `artifacts/brotatolike-shop-item-validation.json` has `status=pass`.
- All shop/item checks pass.
- `expectedInputs`, `expectedObservations`, `passCriteria`, `failCriteria`, and `artifactPath` are non-empty.

## failCriteria

- Stdout contains `BrotatoLike Shop Item validation FAIL`.
- Item authoring, deterministic offers, purchase gate, effect mutation or UI cleanup evidence is missing.
- The artifact is missing or standard-answer fields are empty.

## artifactPath

`artifacts/brotatolike-shop-item-validation.json`

## PASS/FAIL 判定

- PASS：`item_authoring_loaded_and_validates_targets`、`shop_offers_deterministic`、`affordable_purchase_applies_item`、`unaffordable_purchase_rejected`、`shop_ui_updates_and_cleanup` 全部通过，runner `result.json` 标记场景成功。
- FAIL：任一道具 authoring、购买、货币、Runtime Data 效果或 UI 证据缺失。
