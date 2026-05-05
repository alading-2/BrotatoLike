#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

godot_bin="${GODOT_BIN:-/home/slime/Code/Godot/GodotEngine/4.x/Godot_v4.6.2-stable_mono_linux_x86_64/Godot_v4.6.2-stable_mono_linux.x86_64}"

"$godot_bin" --headless --build-solutions --quit --path . --no-header
"$godot_bin" --headless --path . --scene res://Scenes/Main.tscn --quit-after 10 --no-header -- --gameos-smoke-exit
