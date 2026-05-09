---
name: game-development
description: 在 BrotatoLike 游戏仓库中开发游戏特定功能、场景、资产接入或游戏数据时使用。
---

# 游戏开发入口

## 边界

- 游戏特定代码写入 `Src/Game/`。
- 游戏资产当前保留在 `assets/`，使用 `res://assets/...` 路径。
- 游戏数据写入 `DataOS/Authoring/BrotatoLike.seed.sql`，生成后的运行时快照是 `DataOS/Snapshots/runtime_snapshot.json`。
- 框架能力缺口先记录，再切到框架仓库处理。
- 旧仓库复制输入只读参考 `MigrationInput/`，不要把它重新变成主入口。

## 必读入口

- `DocsAI/INDEX.md`
- `DocsAI/GameProjectState.md`
- `DocsAI/ExternalFrameworkMap.md`
- `Plans/README.md`

## 验证

```bash
Tools/run-build.sh
Tools/run-godot-scene.sh run-main-smoke --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```
