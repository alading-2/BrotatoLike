# MigrationInput（已归档）

> 状态：2026-05-06 已完成迁移，本目录下所有 C# 源码、.tres Resource 和场景文件已清理。

## 迁移去向

| 旧内容 | 新位置 |
|--------|--------|
| DataNew/*.cs（PlayerData, EnemyData, AbilityData 等） | `DataOS/Authoring/BrotatoLike.seed.sql` + `DataOS/Snapshots/runtime_snapshot.json` |
| DataKey/*.cs | `SkilmeAI.GameOS.Capabilities.*/DataKeys.cs` |
| EventType/*.cs | `SkilmeAI.GameOS.Runtime.Event` + Capability 分域事件 |
| ResourceManagement/*.cs | `SkilmeAI.GameOS.Runtime.Resource` + `DataOS` resource_entry |
| Config/*.cs | `DataOS/Authoring/BrotatoLike.seed.sql` system.config / system.preset |
| Src/Main/Main.cs | `Src/Game/Main.cs` + `BrotatoLikeGameRuntime` |
| .tres 文件 | SQLite seed 中对应的 data_field / data_record |

## 保留原因

本目录保留作为迁移审计痕迹。如需查阅旧代码历史，见旧仓库 `brotato-my`。
