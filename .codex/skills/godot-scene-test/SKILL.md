---
name: godot-scene-test
description: 在 BrotatoLike 游戏仓库中运行 Godot 场景测试时使用。
---

# Godot 场景测试入口

当前已有最小 Godot headless smoke：

```bash
Tools/run-godot-smoke.sh
```

默认 Godot CLI：

```text
/home/slime/Code/Godot/GodotEngine/4.x/Godot_v4.6.2-stable_mono_linux_x86_64/Godot_v4.6.2-stable_mono_linux.x86_64
```

`Tools/run-godot-smoke.sh` 会先执行：

```bash
godot --headless --build-solutions --quit --path . --no-header
```

再运行：

```bash
godot --headless --path . --scene res://Scenes/Main.tscn --quit-after 10 --no-header -- --gameos-smoke-exit
```

`--gameos-smoke-exit` 会让 `Src/Game/Main.cs` 执行 Runtime / Movement / GodotMovementDriver / GodotBridge / GodotNodePool 断言，失败时返回非 0。

统一测试 runner 尚未迁入。批量场景 runner 临时参考输入仓库：

```text
/home/slime/Code/Godot/Games/MyGames/brotato-my/.codex/skills/GodotSkill
```
