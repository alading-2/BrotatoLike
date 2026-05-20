# BrotatoLike Validation Catalog

> 集中索引 BrotatoLike 游戏侧 Godot 验证场景和主场景验收。场景级事实源仍是各场景目录下的 `README.md` 或 `DocsAI/GodotSceneTesting.md`。

## 场景索引

| 场景路径 | 能力 | expectedInputs | expectedObservations | passCriteria | failCriteria | artifactPath |
|---|---|---|---|---|---|---|
| `res://Scenes/Main.tscn` | Game/Main playable slice | `GODOT_SCENE_TEST_ARTIFACT_DIR`、DataOS snapshot、确定性 MoveRight/MoveUp 输入 | 玩家输入与位置变化、DataOS 敌人追逐和接触伤害、slam/chain ability 与 HUD 证据 | stdout 含 `BrotatoLike playable slice PASS`、artifact `status: pass`、`failureReasons` 为空 | stdout 含 `BrotatoLike playable slice FAIL`、任一 criteria 失败或标准答案字段缺失 | `artifacts/scene-acceptance.json` |
| `res://Scenes/Main.tscn --gameos-smoke-exit` | Game/Main smoke | smoke 命令行参数、Main smoke probes、workspace SlimeAI.GameOS project reference | Runtime core、GodotBridge、Pool、DataOS、Ability、Projectile、Effect、AI、Attack、input probes 通过 | stdout 含 `BrotatoLike GameOS smoke PASS`、`scene-smoke.json` 与 `eventbus-dump.json` 被收集 | stdout 含 `BrotatoLike GameOS smoke FAIL`、任一 probe 失败或标准答案字段缺失 | `artifacts/scene-smoke.json` |
| `res://Src/Validation/Game/UnitComposition/BrotatoLikeUnitCompositionValidation.tscn` | Game/UnitComposition | BrotatoLikeGameRuntime、DataOS unit.player/unit.enemy records、BrotatoLikeUnitProfiles、Godot process frames | 玩家 profile 保留游戏侧输入/技能 adapter、真实 process 输入移动、敌人 AIControlled 追逐、动画播放、contact damage bridge | stdout 含 `BrotatoLike UnitComposition validation PASS`、artifact `status: pass`、`failureReasons` 为空 | stdout 含 `BrotatoLike UnitComposition validation FAIL`、任一 player/enemy/process/animation/contact check 失败或标准答案字段缺失 | `artifacts/brotatolike-unit-composition-validation.json` |
| `res://Src/Validation/Game/Input/BrotatoLikeInputEventValidation.tscn` | Game/Input | 玩家输入事件、BrotatoLike 输入 adapter、主动技能输入 adapter | 输入事件桥接到 game-side EventBus、移动方向写入、技能切换与触发可验证 | stdout 含 `BrotatoLike Game Input validation PASS`、artifact `status: pass` | stdout 含 `BrotatoLike Game Input validation FAIL` 或 artifact `status: fail` | `artifacts/brotatolike-input-event-validation.json` |
| `res://Src/Validation/Game/PlayableUX/BrotatoLikePlayableUXValidation.tscn` | Game/Playable UX | BrotatoLikeGameRuntime、DataOS 玩家和敌人、Godot `MoveRight/NextSkill/PreviousSkill/UseSkill` input actions、正式 UI 节点 | `BrotatoLikeHUD`、`PlayerHealthLabel`、头顶血条、四槽技能栏、冷却、点选指示器、伤害/治疗飘字、玩家可见移动 | stdout 含 `BrotatoLike Playable UX validation PASS`、artifact `status: pass`、`failureReasons=[]`、标准答案字段非空 | stdout 含 `BrotatoLike Playable UX validation FAIL`、任一正式 HUD/血条/技能栏/点选/飘字/可见性证据缺失 | `artifacts/brotatolike-playable-ux-validation.json` |
| `res://Src/Validation/Game/Progression/BrotatoLikeProgressionLoopValidation.tscn` | Game/Progression loop | BrotatoLikeGameRuntime、DataOS 敌人 `Unit.ExpReward`、pause/resume 调用、确定性帧推进 | wave runtime state、暂停菜单、schedule gate、HP recovery、dead skip、经验拾取、level-up 反馈和 HUD summary | stdout 含 `BrotatoLike Progression Loop validation PASS`、artifact `status: pass`、`failureReasons=[]`、标准答案字段非空 | stdout 含 `BrotatoLike Progression Loop validation FAIL`、wave/pause/recovery/pickup/experience/level-up 任一证据缺失 | `artifacts/brotatolike-progression-loop-validation.json` |
| `res://Src/Validation/Game/LegacyResources/BrotatoLikeLegacyResourceClassificationValidation.tscn` | Game/Legacy resource classification | DataOS snapshot `resources[]`、`legacyStatus`、Godot `ResourceLoader.Exists` active resource check | 旧 `res://Src/...` / `res://Data/...` 路径全部分类，missing legacy path 不可 active，输出 legacy counts | stdout 含 `BrotatoLike Legacy Resource Classification validation PASS`、artifact `status: pass`、`unsupportedStatusCount=0`、`missingActiveLegacyCount=0` | unsupported `legacyStatus`、missing old path 仍被标记 active、artifact 标准答案字段缺失 | `artifacts/brotatolike-legacy-resource-classification-validation.json` |

## 最新证据

2026-05-20 `restore-brotatolike-playable-ux`：

- Playable UX：`.ai-temp/scene-tests/runs/2026-05-20/11-32-49/index.json`，artifact `brotatolike-playable-ux-validation.json` 为 `status=pass`、`failureReasons=[]`；scene gate 已检查 `index.json`、`result.json` 和 scene artifact，`expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 均非空。
- Progression Loop：`.ai-temp/scene-tests/runs/2026-05-20/11-33-15/index.json`，artifact `brotatolike-progression-loop-validation.json` 为 `status=pass`、`failureReasons=[]`；证据包含 `wave_completed_meta=true`、pause 后 tick 阻断、resume 后 tick 恢复、`mana_recovery_status=not-applicable`、`last_reward=5`、`new_level=2`。
- Legacy Resources：`.ai-temp/scene-tests/runs/2026-05-20/11-33-42/index.json`，artifact `brotatolike-legacy-resource-classification-validation.json` 为 `status=pass`、`failureReasons=[]`、`legacyCount=25`、`unsupportedStatusCount=0`、`missingActiveLegacyCount=0`。
- Main playable：`.ai-temp/scene-tests/runs/2026-05-20/11-33-55/index.json`，artifact `scene-acceptance.json` 为 `status=pass`、`failureReasons=[]`；正式 HUD 证据包含 `formal_hud_found=True`、`formal_hud_current_skill=位置目标`、`formal_hud_damage_number_count=77`、`skill_point_report=Success`、`skill_point_targeting_started=True`。
- Main smoke：`.ai-temp/scene-tests/runs/2026-05-20/11-34-19/index.json`，artifact `scene-smoke.json` 为 `status=pass`、`failureReasons=[]`。

2026-05-20 `migrate-brotatolike-unit-composition`：

- UnitComposition：`.ai-temp/scene-tests/runs/2026-05-20/09-18-27/index.json`，artifact `brotatolike-unit-composition-validation.json` 为 `status=pass`、`failureReasons=[]`。
- Main playable：`.ai-temp/scene-tests/runs/2026-05-20/09-20-05/index.json`，artifact `scene-acceptance.json` 为 `status=pass`、`failureReasons=[]`。
- Main smoke：`.ai-temp/scene-tests/runs/2026-05-20/09-20-45/index.json`，artifact `scene-smoke.json` 为 `status=pass`、`failureReasons=[]`。
