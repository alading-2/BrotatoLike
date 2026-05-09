#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

base_dir=".ai-temp/scene-tests/runs"
target_dir=""

usage() {
    cat >&2 <<'USAGE'
Usage:
  Tools/analyze-godot-scene-logs.sh [--run-dir <path>]

Reads the latest run under .ai-temp/scene-tests/runs by default and prints
a compact PASS/FAIL/error summary.
USAGE
}

while [ "$#" -gt 0 ]; do
    case "$1" in
        --run-dir)
            target_dir="${2:-}"
            if [ -z "$target_dir" ]; then
                echo "--run-dir requires a path." >&2
                exit 2
            fi
            shift 2
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            echo "Unknown option: $1" >&2
            usage
            exit 2
            ;;
    esac
done

if [ -z "$target_dir" ]; then
    if [ ! -d "$base_dir" ]; then
        echo "No scene test log directory found: $base_dir" >&2
        exit 1
    fi

    target_dir="$(find "$base_dir" -mindepth 2 -maxdepth 2 -type d | sort | tail -n 1)"
    if [ -z "$target_dir" ]; then
        echo "No scene test runs found under: $base_dir" >&2
        exit 1
    fi
fi

if [ ! -d "$target_dir" ]; then
    echo "Run directory not found: $target_dir" >&2
    exit 1
fi

stdout_file="$target_dir/stdout.log"
stderr_file="$target_dir/stderr.log"
acceptance_file="$target_dir/artifacts/scene-acceptance.json"

echo "Scene test run: $target_dir"

if [ -f "$stdout_file" ] && rg -q "BrotatoLike playable slice PASS" "$stdout_file"; then
    echo "Status: playable slice PASS marker found"
elif [ -f "$stdout_file" ] && rg -q "BrotatoLike playable slice FAIL" "$stdout_file"; then
    echo "Status: playable slice FAIL marker found"
elif [ -f "$stdout_file" ] && rg -q "BrotatoLike GameOS smoke PASS|PASS|\\[PASS\\]" "$stdout_file"; then
    echo "Status: PASS marker found"
else
    echo "Status: PASS marker not found"
fi

if [ -f "$acceptance_file" ]; then
    artifact_status="$(jq -r '.status // "unknown"' "$acceptance_file" 2>/dev/null || printf 'unreadable')"
    echo "Acceptance artifact: $acceptance_file ($artifact_status)"
else
    echo "Acceptance artifact: not found"
fi

matches="$(rg -n -m 40 "ERROR:|\\[ERROR\\]|\\[FAIL\\]|FAIL:|Exception|Cannot instantiate|Failed to load|scene not found" "$target_dir" || true)"
if [ -n "$matches" ]; then
    echo
    echo "First error markers:"
    printf '%s\n' "$matches"
else
    echo "Error markers: none"
fi

if [ -f "$stdout_file" ]; then
    echo
    echo "Stdout tail:"
    tail -n 20 "$stdout_file"
fi

if [ -f "$stderr_file" ] && [ -s "$stderr_file" ]; then
    echo
    echo "Stderr tail:"
    tail -n 20 "$stderr_file"
fi
