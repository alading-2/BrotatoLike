# BrotatoLike Validation Catalog

> 集中索引 BrotatoLike 游戏侧 Godot 验证场景和主场景验收。场景级事实源仍是各场景目录下的 `README.md` 或 `DocsAI/GodotSceneTesting.md`。

## 场景索引

| 场景路径 | 能力 | expectedInputs | expectedObservations | passCriteria | failCriteria | artifactPath |
|---|---|---|---|---|---|---|
| `res://Scenes/Main.tscn` | Game/Main playable slice | `GODOT_SCENE_TEST_ARTIFACT_DIR`、DataOS snapshot、确定性 MoveRight/MoveUp 输入 | 玩家输入与位置变化、DataOS 敌人追逐和接触伤害、slam/chain ability 与 HUD 证据 | stdout 含 `BrotatoLike playable slice PASS`、artifact `status: pass`、`failureReasons` 为空 | stdout 含 `BrotatoLike playable slice FAIL`、任一 criteria 失败或标准答案字段缺失 | `artifacts/scene-acceptance.json` |
| `res://Scenes/Main.tscn --gameos-smoke-exit` | Game/Main smoke | smoke 命令行参数、Main smoke probes、workspace SlimeAI.GameOS project reference | Runtime core、GodotBridge、Pool、DataOS、Ability、Projectile、Effect、AI、Attack、input probes 通过 | stdout 含 `BrotatoLike GameOS smoke PASS`、`scene-smoke.json` 与 `eventbus-dump.json` 被收集 | stdout 含 `BrotatoLike GameOS smoke FAIL`、任一 probe 失败或标准答案字段缺失 | `artifacts/scene-smoke.json` |
| `res://Src/Validation/Game/UnitComposition/BrotatoLikeUnitCompositionValidation.tscn` | Game/UnitComposition | BrotatoLikeGameRuntime、DataOS unit.player/unit.enemy records、BrotatoLikeUnitProfiles、Godot process frames | 玩家 profile 保留游戏侧输入/技能 adapter、真实 process 输入移动、敌人 AIControlled 追逐、动画播放、contact damage bridge | stdout 含 `BrotatoLike UnitComposition validation PASS`、artifact `status: pass`、`failureReasons` 为空 | stdout 含 `BrotatoLike UnitComposition validation FAIL`、任一 player/enemy/process/animation/contact check 失败或标准答案字段缺失 | `artifacts/brotatolike-unit-composition-validation.json` |
| `res://Src/Validation/Game/Input/BrotatoLikeInputEventValidation.tscn` | Game/Input | 玩家输入事件、BrotatoLike 输入 adapter、主动技能输入 adapter | 输入事件桥接到 game-side EventBus、移动方向写入、技能切换与触发可验证 | stdout 含 `BrotatoLike Game Input validation PASS`、artifact `status: pass` | stdout 含 `BrotatoLike Game Input validation FAIL` 或 artifact `status: fail` | `artifacts/brotatolike-input-event-validation.json` |

## 最新证据

2026-05-20 `migrate-brotatolike-unit-composition`：

- UnitComposition：`.ai-temp/scene-tests/runs/2026-05-20/09-18-27/index.json`，artifact `brotatolike-unit-composition-validation.json` 为 `status=pass`、`failureReasons=[]`。
- Main playable：`.ai-temp/scene-tests/runs/2026-05-20/09-20-05/index.json`，artifact `scene-acceptance.json` 为 `status=pass`、`failureReasons=[]`。
- Main smoke：`.ai-temp/scene-tests/runs/2026-05-20/09-20-45/index.json`，artifact `scene-smoke.json` 为 `status=pass`、`failureReasons=[]`。
