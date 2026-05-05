#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
framework_root="${SKILMEAI_FRAMEWORK_ROOT:-/home/slime/Code/SkilmeAI/SkilmeAI}"
db_path="$repo_root/DataOS/.generated/brotatolike.authoring.db"
snapshot_path="$repo_root/DataOS/Snapshots/runtime_snapshot.json"

mkdir -p "$(dirname "$db_path")" "$(dirname "$snapshot_path")"
rm -f "$db_path"

sqlite3 "$db_path" ".read $framework_root/DataOS/Migrations/001_initial.sql"
sqlite3 "$db_path" ".read $repo_root/DataOS/Authoring/BrotatoLike.seed.sql"
"$framework_root/DataOS/Generators/generate-runtime-snapshot.sh" "$db_path" "$snapshot_path"

echo "BrotatoLike DataOS snapshot generated: $snapshot_path"
