#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
framework_root="${SlimeAI_FRAMEWORK_ROOT:-/home/slime/Code/SlimeAI/SlimeAI}"
db_path="$repo_root/DataOS/.generated/brotatolike.authoring.db"
snapshot_path="$repo_root/DataOS/Snapshots/runtime_snapshot.json"
shop_item_snapshot_path="$repo_root/DataOS/Snapshots/shop_item_authoring.json"

mkdir -p "$(dirname "$db_path")" "$(dirname "$snapshot_path")"
rm -f "$db_path"

while IFS= read -r migration; do
    sqlite3 "$db_path" ".read $migration"
done < <(find "$framework_root/DataOS/Migrations" -maxdepth 1 -name '*.sql' | sort)
sqlite3 "$db_path" ".read $repo_root/DataOS/Authoring/BrotatoLike.seed.sql"
DATAOS_PROFILE=brotatolike DATAOS_CATALOG_ID=brotatolike "$framework_root/DataOS/Generators/generate-runtime-snapshot.sh" "$db_path" "$snapshot_path"

sqlite3 "$db_path" > "$shop_item_snapshot_path" <<SQL
WITH
item_docs AS (
    SELECT json_object(
        'id', id,
        'displayName', display_name,
        'basePrice', base_price,
        'rarity', rarity,
        'weight', weight,
        'iconPath', COALESCE(icon_path, ''),
        'effectTarget', effect_target,
        'effectOperation', effect_operation,
        'effectValue', effect_value,
        'effectDescription', effect_description
    ) AS doc
    FROM item_definition
    ORDER BY id
),
offer_docs AS (
    SELECT json_object(
        'offerSetId', offer_set_id,
        'slotIndex', slot_index,
        'itemId', item_id,
        'priceOverride', price_override,
        'offerSource', offer_source,
        'closeShopOnPurchase', json(CASE close_shop_on_purchase WHEN 1 THEN 'true' ELSE 'false' END)
    ) AS doc
    FROM shop_offer
    ORDER BY offer_set_id, slot_index
)
SELECT json_object(
    'schemaVersion', 1,
    'generatedAtUtc', '${DATAOS_GENERATED_AT_UTC:-1970-01-01T00:00:00Z}',
    'source', 'DataOS:BrotatoLike.seed.sql:item_definition+shop_offer',
    'items', COALESCE((SELECT json_group_array(json(doc)) FROM item_docs), json('[]')),
    'offers', COALESCE((SELECT json_group_array(json(doc)) FROM offer_docs), json('[]'))
);
SQL

echo "BrotatoLike DataOS snapshot generated: $snapshot_path"
echo "BrotatoLike shop/item authoring generated: $shop_item_snapshot_path"
