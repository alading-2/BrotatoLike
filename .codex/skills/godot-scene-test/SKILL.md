---
name: godot-scene-test
description: 在 BrotatoLike 游戏仓库中运行 Godot 场景测试时使用。
---

# Godot 场景测试入口

当前统一 Godot headless runner：

```bash
Tools/run-godot-scene.sh
```

默认 Godot CLI：

```text
/home/slime/Code/Godot/GodotEngine/4.x/Godot_v4.6.2-stable_mono_linux_x86_64/Godot_v4.6.2-stable_mono_linux.x86_64
```

常用命令：

```bash
Tools/run-godot-scene.sh list
Tools/run-godot-scene.sh run-main-smoke --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 3 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

已迁 bundled scripts：

```text
.codex/skills/godot-scene-test/scripts/run-test.sh
.codex/skills/godot-scene-test/scripts/analyze-logs.sh
.codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs
```

兼容入口仍可使用：

```bash
Tools/run-godot-smoke.sh
```

`run-main-smoke` 会覆盖 Runtime / DataOS bootstrap / Ability handler-specific 参数 / Movement / Collision / Damage / Attack / AI / Ability / Projectile / Effect / GodotBridge / NodePool / 主运行时入口，失败时返回非 0。
