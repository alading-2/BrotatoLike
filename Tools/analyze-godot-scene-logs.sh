#!/usr/bin/env bash
set -euo pipefail

project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
repo_root="$(cd "$project_root/../.." && pwd)"
analyzer="$repo_root/.codex/skills/godot-scene-test/scripts/analyze-logs.sh"

if [ ! -f "$analyzer" ]; then
    echo "Godot scene skill log analyzer not found: $analyzer" >&2
    exit 1
fi

cd "$project_root"
"$analyzer" "$@"
