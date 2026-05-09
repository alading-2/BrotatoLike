#!/usr/bin/env bash
set -euo pipefail

# run-test.sh — 简洁包装 Godot headless 场景测试
# 用法: ./run-test.sh [--build] [--attempts N] [--timeout MS] <scene-path>
# 场景路径支持 res:// 或相对路径

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RUNNER="${SCRIPT_DIR}/godot-scene-runner.mjs"

GODOT_BIN="${GODOT_BIN:-}"
GODOT_BIN="${GODOT_BIN:-/home/slime/Code/Godot/GodotEngine/4.x/Godot_v4.6.2-stable_mono_linux_x86_64/Godot_v4.6.2-stable_mono_linux.x86_64}"

BUILD=""
ATTEMPTS="2"
TIMEOUT="60000"
SCENE=""
EXTRA_ARGS=()

while [[ $# -gt 0 ]]; do
    case "$1" in
        --build)
            BUILD="--build"
            shift ;;
        --attempts)
            ATTEMPTS="$2"
            shift 2 ;;
        --timeout)
            TIMEOUT="$2"
            shift 2 ;;
        --no-retry)
            ATTEMPTS="1"
            shift ;;
        --full-logs)
            EXTRA_ARGS+=("--full-logs")
            shift ;;
        --errors-only)
            EXTRA_ARGS+=("--errors-only")
            shift ;;
        -*)
            echo "未知选项: $1"
            exit 1 ;;
        *)
            SCENE="$1"
            shift ;;
    esac
done

if [[ -z "$SCENE" ]]; then
    echo "用法: $0 [--build] [--attempts N] [--timeout MS] <scene-path>"
    echo ""
    echo "示例:"
    echo "  $0 --build res://Src/ECS/Test/GlobalTest/MainTest/MainTest.tscn"
    echo "  $0 --no-retry res://Src/ECS/Test/SingleTest/ECS/System/Data/DataTestScene.tscn"
    exit 1
fi

export GODOT_BIN
exec node "$RUNNER" run "$SCENE" \
    $BUILD \
    --attempts "$ATTEMPTS" \
    --timeout "$TIMEOUT" \
    --log-dir .ai-temp/scene-tests/runs \
    "${EXTRA_ARGS[@]}"
