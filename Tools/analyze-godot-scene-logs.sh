#!/usr/bin/env bash
set -euo pipefail

project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
repo_root="$(cd "$project_root/../.." && pwd)"
analyzer="$repo_root/SkilmeAI/Tools/analyze-godot-scene-logs.sh"

if [ ! -f "$analyzer" ]; then
    echo "Common Godot scene log analyzer not found: $analyzer" >&2
    exit 1
fi

cd "$project_root"
"$analyzer" "$@"
