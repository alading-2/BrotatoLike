# MigrationInput（已清空，迁移仍在进行）

> 状态：2026-05-06 已从本仓库清理本目录下的 C# 源码、.tres 与场景文件。
> **本目录被清空 ≠ BrotatoLike 整体迁移完成。**
> 真实迁移完成度以 `Games/BrotatoLike/DocsAI/MigrationLedger.md` 和 `openspec/specs/brotatolike-migration-ledger/spec.md` 为准；可玩验证以 `Games/BrotatoLike/Scenes/Main.tscn` 普通运行路径（非 `--gameos-smoke-exit` 探针）输出的 PASS/FAIL marker 为准。

## 第一阶段已迁移项（仅表示已有对应入口，不代表行为对齐）

| 旧内容 | 第一阶段去向 |
|--------|--------|
| DataNew/*.cs（PlayerData, EnemyData, AbilityData 等） | `DataOS/Authoring/BrotatoLike.seed.sql` + `DataOS/Snapshots/runtime_snapshot.json` |
| DataKey/*.cs | `SkilmeAI.GameOS.Capabilities.*/DataKeys.cs` |
| EventType/*.cs | `SkilmeAI.GameOS.Runtime.Event` + Capability 分域事件 |
| ResourceManagement/*.cs | `SkilmeAI.GameOS.Runtime.Resource` + `DataOS` resource_entry |
| Config/*.cs | `DataOS/Authoring/BrotatoLike.seed.sql` system.config / system.preset |
| Src/Main/Main.cs | `Src/Game/Main.cs` + `BrotatoLikeGameRuntime` |
| .tres 文件 | SQLite seed 中对应的 data_field / data_record |

注意：上表只覆盖**第一阶段映射存在性**，不覆盖以下未完成项：

- 旧 `Src/ECS/UI/**`、HUD、技能输入面板、暂停菜单、波次推进 UI 等真实玩家路径资产。
- 旧 `Data/ResourceManagement/ResourcePaths.cs` 中部分 `res://Src/...` 路径仍在 DataOS seed 中以记录形式存在，但对应资源**未**进入新仓库 `Games/BrotatoLike/`。
- 旧 `Src/ECS/Test/**` 可玩测试场景。
- 旧 `Src/ECS/Base/Component/**` 与 `Src/ECS/Base/Entity/**` 中绝大多数 `.tscn` 实体场景。

## 审计依据

旧实现在框架仓库中保留位置：`/home/slime/Code/SkilmeAI/Else/brotato-my`（约 6022 个 C#、681 个 `.tscn`）。新仓库 `Games/BrotatoLike/Src/Game/` 当前仅 12 个 C# 文件，两者差距悬殊；任何"迁移完成"的判断都必须以 `Else/brotato-my` 为权威输入比对，不能仅以本目录被清空为依据。

## 保留原因

本 README 保留作为迁移审计入口，明确"已清理 ≠ 已迁移"。完整旧文件树请直接查阅 `Else/brotato-my`。
