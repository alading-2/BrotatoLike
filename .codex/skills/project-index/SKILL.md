---
name: project-index
description: BrotatoLike 游戏仓库导航入口。用于查找游戏状态、框架引用、场景、资产、数据和入口 Skill。
---

# BrotatoLike 项目导航

## 必读入口

- `DocsAI/INDEX.md`
- `DocsAI/GameProjectState.md`
- `DocsAI/ExternalFrameworkMap.md`
- `Plans/README.md`

## 查找规则

- 找框架版本和源码位置：先看 `DocsAI/ExternalFrameworkMap.md`。
- 找游戏状态：先看 `DocsAI/GameProjectState.md`。
- 找整体迁移阶段：先看 `Plans/README.md`。
- 找最小启动场景和 GodotBridge 探针：看 `Scenes/Main.tscn`、`Src/Game/Main.cs` 和 `Src/Game/SmokeGodotComponent.cs`。
- 找资产：查 `Assets/`。
- 找已复制旧资产：查 `assets/`，路径保持 `res://assets/...`。
- 找待适配旧数据和旧入口：查 `MigrationInput/`。
- 找游戏代码：查 `Src/Game/`。
