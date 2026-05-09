---
name: data-authoring
description: 在 BrotatoLike 游戏仓库中编写或迁移游戏数据时使用。
---

# 游戏数据入口

## 当前状态

DataOS 已接入本游戏仓库。当前 seed 位于 `DataOS/Authoring/BrotatoLike.seed.sql`，构建会生成 `DataOS/Snapshots/runtime_snapshot.json`，运行时通过 `BrotatoLikeDataOSBootstrap` 消费 snapshot。

## 必读入口

- `DocsAI/GameProjectState.md`
- `DocsAI/ExternalFrameworkMap.md`
- `DataOS/Authoring/BrotatoLike.seed.sql`
- `Src/Game/BrotatoLikeDataOSBootstrap.cs`

## 规则

- 游戏专有数据写入本仓库 `DataOS/Authoring/BrotatoLike.seed.sql`。
- 不把旧 `MigrationInput/Data` 直接恢复为长期运行时入口。
- 新字段如果需要框架 DataKey，先查框架 `GameOS/SkilmeAI.GameOS.Contracts.md` 和 `GameOS/SkilmeAI.GameOS.ApiIndex.md`。
- 修改 seed 后运行 `Tools/run-build.sh` 生成 snapshot，并跑 Godot smoke。

## 验证

```bash
Tools/run-build.sh
Tools/run-godot-scene.sh run-main-smoke --log-dir .ai-temp/scene-tests/runs
```
