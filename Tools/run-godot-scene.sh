#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

default_godot="/home/slime/Code/Godot/GodotEngine/4.x/Godot_v4.6.2-stable_mono_linux_x86_64/Godot_v4.6.2-stable_mono_linux.x86_64"
godot_bin="${GODOT_BIN:-$default_godot}"
timeout_seconds=60
build=false
log_dir=""
scene_args=()

usage() {
    cat >&2 <<'USAGE'
Usage:
  Tools/run-godot-scene.sh list [--filter <text>]
  Tools/run-godot-scene.sh run <res://scene.tscn> [--build] [--godot <path>] [--timeout <seconds>] [--log-dir <path>] [-- <scene args...>]
  Tools/run-godot-scene.sh run-main-smoke [--godot <path>] [--timeout <seconds>] [--log-dir <path>]

Examples:
  Tools/run-godot-scene.sh list
  Tools/run-godot-scene.sh run res://Scenes/Main.tscn --build -- --gameos-smoke-exit
  Tools/run-godot-scene.sh run-main-smoke --log-dir .ai-temp/scene-tests/runs
USAGE
}

to_res_path() {
    local path="$1"
    path="${path#./}"
    printf 'res://%s\n' "$path"
}

require_godot() {
    if [ ! -x "$godot_bin" ]; then
        echo "Godot executable not found or not executable: $godot_bin" >&2
        exit 1
    fi
}

require_scene() {
    local scene="$1"
    if [[ "$scene" != res://*.tscn ]]; then
        echo "Scene path must be a res:// .tscn path: $scene" >&2
        exit 2
    fi

    local local_path="${scene#res://}"
    if [ ! -f "$repo_root/$local_path" ]; then
        echo "Scene not found: $scene" >&2
        exit 1
    fi
}

make_run_dir() {
    local base="$1"
    local date_id time_id run_dir
    date_id="$(date +%F)"
    time_id="$(date +%H-%M-%S)"
    run_dir="$repo_root/$base/$date_id/$time_id"
    mkdir -p "$run_dir/screenshots" "$run_dir/artifacts"
    printf '%s\n' "$run_dir"
}

run_with_optional_log() {
    local run_dir="$1"
    shift

    if [ -n "$run_dir" ]; then
        "$@" > >(tee "$run_dir/stdout.log") 2> >(tee "$run_dir/stderr.log" >&2)
        return $?
    fi

    "$@"
}

run_scene() {
    local scene="$1"
    require_godot
    require_scene "$scene"

    local run_dir=""
    if [ -n "$log_dir" ]; then
        run_dir="$(make_run_dir "$log_dir")"
        export GODOT_SCENE_TEST_RUN_DIR="$run_dir"
        export GODOT_SCENE_TEST_SCENE_DIR="$run_dir"
        export GODOT_SCENE_TEST_SCREENSHOT_DIR="$run_dir/screenshots"
        export GODOT_SCENE_TEST_ARTIFACT_DIR="$run_dir/artifacts"
        echo "Scene test log dir: $run_dir"
    fi

    if [ "$build" = true ]; then
        run_with_optional_log "$run_dir" "$godot_bin" --headless --build-solutions --quit --path . --no-header
    fi

    run_with_optional_log "$run_dir" timeout "$timeout_seconds" "$godot_bin" \
        --headless \
        --path . \
        --scene "$scene" \
        --quit-after "$timeout_seconds" \
        --no-header \
        -- "${scene_args[@]}"
}

command="${1:-}"
if [ -z "$command" ]; then
    usage
    exit 2
fi
shift || true

case "$command" in
    list)
        filter=""
        while [ "$#" -gt 0 ]; do
            case "$1" in
                --filter)
                    filter="${2:-}"
                    if [ -z "$filter" ]; then
                        echo "--filter requires text." >&2
                        exit 2
                    fi
                    shift 2
                    ;;
                -h|--help)
                    usage
                    exit 0
                    ;;
                *)
                    echo "Unknown option for list: $1" >&2
                    usage
                    exit 2
                    ;;
            esac
        done

        while IFS= read -r scene_file; do
            res_path="$(to_res_path "$scene_file")"
            if [ -z "$filter" ] || [[ "$res_path" == *"$filter"* ]]; then
                printf '%s\n' "$res_path"
            fi
        done < <(find Scenes Src -name '*.tscn' -type f 2>/dev/null | sort)
        ;;

    run)
        scene="${1:-}"
        if [ -z "$scene" ]; then
            echo "run requires a scene path." >&2
            usage
            exit 2
        fi
        shift

        while [ "$#" -gt 0 ]; do
            case "$1" in
                --build)
                    build=true
                    shift
                    ;;
                --godot)
                    godot_bin="${2:-}"
                    if [ -z "$godot_bin" ]; then
                        echo "--godot requires a path." >&2
                        exit 2
                    fi
                    shift 2
                    ;;
                --timeout)
                    timeout_seconds="${2:-}"
                    if ! [[ "$timeout_seconds" =~ ^[0-9]+$ ]] || [ "$timeout_seconds" -le 0 ]; then
                        echo "--timeout requires a positive integer." >&2
                        exit 2
                    fi
                    shift 2
                    ;;
                --log-dir)
                    log_dir="${2:-}"
                    if [ -z "$log_dir" ]; then
                        echo "--log-dir requires a path." >&2
                        exit 2
                    fi
                    shift 2
                    ;;
                --)
                    shift
                    scene_args=("$@")
                    break
                    ;;
                -h|--help)
                    usage
                    exit 0
                    ;;
                *)
                    echo "Unknown option for run: $1" >&2
                    usage
                    exit 2
                    ;;
            esac
        done

        run_scene "$scene"
        ;;

    run-main-smoke)
        build=true
        scene_args=("--gameos-smoke-exit")

        while [ "$#" -gt 0 ]; do
            case "$1" in
                --godot)
                    godot_bin="${2:-}"
                    if [ -z "$godot_bin" ]; then
                        echo "--godot requires a path." >&2
                        exit 2
                    fi
                    shift 2
                    ;;
                --timeout)
                    timeout_seconds="${2:-}"
                    if ! [[ "$timeout_seconds" =~ ^[0-9]+$ ]] || [ "$timeout_seconds" -le 0 ]; then
                        echo "--timeout requires a positive integer." >&2
                        exit 2
                    fi
                    shift 2
                    ;;
                --log-dir)
                    log_dir="${2:-}"
                    if [ -z "$log_dir" ]; then
                        echo "--log-dir requires a path." >&2
                        exit 2
                    fi
                    shift 2
                    ;;
                -h|--help)
                    usage
                    exit 0
                    ;;
                *)
                    echo "Unknown option for run-main-smoke: $1" >&2
                    usage
                    exit 2
                    ;;
            esac
        done

        run_scene "res://Scenes/Main.tscn"
        ;;

    -h|--help)
        usage
        ;;

    *)
        echo "Unknown command: $command" >&2
        usage
        exit 2
        ;;
esac
