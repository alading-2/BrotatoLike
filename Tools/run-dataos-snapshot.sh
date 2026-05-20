#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
framework_root="${SlimeAI_FRAMEWORK_ROOT:-/home/slime/Code/SlimeAI/SlimeAI}"
db_path="$repo_root/DataOS/.generated/brotatolike.authoring.db"
snapshot_path="$repo_root/DataOS/Snapshots/runtime_snapshot.json"

mkdir -p "$(dirname "$db_path")" "$(dirname "$snapshot_path")"
rm -f "$db_path"

while IFS= read -r migration; do
    sqlite3 "$db_path" ".read $migration"
done < <(find "$framework_root/DataOS/Migrations" -maxdepth 1 -name '*.sql' | sort)
sqlite3 "$db_path" ".read $repo_root/DataOS/Authoring/BrotatoLike.seed.sql"
DATAOS_PROFILE=brotatolike DATAOS_CATALOG_ID=brotatolike "$framework_root/DataOS/Generators/generate-runtime-snapshot.sh" "$db_path" "$snapshot_path"

echo "BrotatoLike DataOS snapshot generated: $snapshot_path"
