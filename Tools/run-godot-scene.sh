#!/usr/bin/env bash
set -euo pipefail

project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
repo_root="$(cd "$project_root/../.." && pwd)"
runner="$repo_root/.codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs"
default_godot="/home/slime/Code/Godot/GodotEngine/4.x/Godot_v4.6.2-stable_mono_linux_x86_64/Godot_v4.6.2-stable_mono_linux.x86_64"

usage() {
    cat >&2 <<'USAGE'
Usage:
  Tools/run-godot-scene.sh list [--filter <text>]
  Tools/run-godot-scene.sh run <res://scene.tscn> [--build] [--godot <path>] [--timeout <seconds>] [--log-dir <path>] [--attempts <1-3>] [--full-logs] [--errors-only] [--log-retention-days <days>] [-- <scene args...>]
  Tools/run-godot-scene.sh run-many <scene...> [--build] [--continue-on-fail] [runner options]
  Tools/run-godot-scene.sh run-all [--build] [--continue-on-fail] [--filter <text>] [runner options]
  Tools/run-godot-scene.sh run-main-smoke [--godot <path>] [--timeout <seconds>] [--log-dir <path>]

Examples:
  Tools/run-godot-scene.sh list
  Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
  Tools/run-godot-scene.sh run-main-smoke --log-dir .ai-temp/scene-tests/runs
USAGE
}

convert_timeout_args() {
    local converted=()
    while [ "$#" -gt 0 ]; do
        case "$1" in
            --timeout)
                if [ -z "${2:-}" ]; then
                    echo "--timeout requires a positive integer seconds value." >&2
                    exit 2
                fi
                if ! [[ "$2" =~ ^[0-9]+$ ]] || [ "$2" -le 0 ]; then
                    echo "--timeout requires a positive integer seconds value." >&2
                    exit 2
                fi
                converted+=("--timeout" "$(( $2 * 1000 ))")
                shift 2
                ;;
            --)
                converted+=("--")
                shift
                converted+=("$@")
                break
                ;;
            *)
                converted+=("$1")
                shift
                ;;
        esac
    done

    printf '%s\0' "${converted[@]}"
}

run_runner() {
    if [ ! -f "$runner" ]; then
        echo "Godot scene skill runner not found: $runner" >&2
        exit 1
    fi

    cd "$project_root"
    GODOT_BIN="${GODOT_BIN:-$default_godot}" \
    GODOT_SCENE_TEST_PROJECT_ROOT="$project_root" \
    GODOT_SCENE_TEST_SCAN_ROOTS="Scenes,Src" \
        node "$runner" "$@"
}

command="${1:-}"
if [ -z "$command" ]; then
    usage
    exit 2
fi
shift || true

case "$command" in
    run-main-smoke)
        mapfile -d '' args < <(convert_timeout_args "$@")
        run_runner run res://Scenes/Main.tscn --build "${args[@]}" -- --gameos-smoke-exit
        ;;
    -h|--help)
        usage
        ;;
    *)
        mapfile -d '' args < <(convert_timeout_args "$@")
        run_runner "$command" "${args[@]}"
        ;;
esac
