# BrotatoLike Validation Catalog

> 集中索引 BrotatoLike 游戏侧 Godot 验证场景和主场景验收。场景级事实源仍是各场景目录下的 `README.md` 或 `DocsAI/GodotSceneTesting.md`。

## 场景索引

| 场景路径 | 能力 | expectedInputs | expectedObservations | passCriteria | failCriteria | artifactPath |
|---|---|---|---|---|---|---|
| `res://Scenes/Main.tscn` | Game/Main playable slice | `GODOT_SCENE_TEST_ARTIFACT_DIR`、DataOS snapshot、确定性 MoveRight/MoveUp 输入 | 玩家输入与位置变化、DataOS 敌人追逐和接触伤害、slam/chain ability 与 HUD 证据 | stdout 含 `BrotatoLike playable slice PASS`、artifact `status: pass`、`failureReasons` 为空 | stdout 含 `BrotatoLike playable slice FAIL`、任一 criteria 失败或标准答案字段缺失 | `artifacts/scene-acceptance.json` |
| `res://Scenes/Main.tscn --gameos-smoke-exit` | Game/Main smoke | smoke 命令行参数、Main smoke probes、workspace SlimeAI.GameOS project reference | Runtime core、GodotBridge、Pool、DataOS、Ability、Projectile、Effect、AI、Attack、input probes 通过 | stdout 含 `BrotatoLike GameOS smoke PASS`、`scene-smoke.json` 与 `eventbus-dump.json` 被收集 | stdout 含 `BrotatoLike GameOS smoke FAIL`、任一 probe 失败或标准答案字段缺失 | `artifacts/scene-smoke.json` |
| `res://Src/Validation/Game/UnitComposition/BrotatoLikeUnitCompositionValidation.tscn` | Game/UnitComposition | BrotatoLikeGameRuntime、DataOS unit.player/unit.enemy records、BrotatoLikeUnitProfiles、Godot process frames | 玩家 profile 保留游戏侧输入/技能 adapter、真实 process 输入移动、敌人 AIControlled 追逐、动画播放、contact damage bridge | stdout 含 `BrotatoLike UnitComposition validation PASS`、artifact `status: pass`、`failureReasons` 为空 | stdout 含 `BrotatoLike UnitComposition validation FAIL`、任一 player/enemy/process/animation/contact check 失败或标准答案字段缺失 | `artifacts/brotatolike-unit-composition-validation.json` |
| `res://Src/Validation/Game/Input/BrotatoLikeInputEventValidation.tscn` | Game/Input | 玩家输入事件、BrotatoLike 输入 adapter、主动技能输入 adapter | 输入事件桥接到 game-side EventBus、移动方向写入、技能切换与触发可验证 | stdout 含 `BrotatoLike Game Input validation PASS`、artifact `status: pass`、`failureReasons` 为空、标准答案字段非空 | stdout 含 `BrotatoLike Game Input validation FAIL`、artifact `status: fail` 或标准答案字段缺失 | `artifacts/brotatolike-input-event-validation.json` |
| `res://Src/Validation/Game/GameLifecycle/BrotatoLikeGameplayLifecycleValidation.tscn` | Game/GameLifecycle feature-slice | BrotatoLikeGameRuntime、DataOS `unit.player/deluyi`、玩家实体、Damage/Movement/Ability/Collision/Unit、输入/HUD/进度服务 | 死亡阻断移动和技能输入、Dash 通过技能栏 input path 选中/释放并位移、Camera 死亡期间保持启用、自动重生恢复 HP/位置/input/camera、Camera 跟随、并发系统不冲突、暂停/恢复状态完整、HUD 死亡/重生状态干净 | stdout 含 `BrotatoLike Gameplay Lifecycle validation PASS`、artifact `status: pass`、`failureReasons=[]`、9 个 lifecycle checks 全部 pass、标准答案字段非空 | stdout 含 `BrotatoLike Gameplay Lifecycle validation FAIL`、任一 lifecycle check 失败、artifact `status: fail` 或标准答案字段缺失 | `artifacts/brotatolike-gameplay-lifecycle-validation.json` |
| `res://Src/Validation/Game/PlayableUX/BrotatoLikePlayableUXValidation.tscn` | Game/Playable UX | BrotatoLikeGameRuntime、DataOS 玩家和敌人、Godot `MoveRight/NextSkill/PreviousSkill/UseSkill` input actions、正式 UI 节点、Camera2D offset | `BrotatoLikeHUD`、`PlayerHealthLabel`、头顶血条、四槽技能栏、冷却、点选指示器、伤害/治疗飘字、玩家可见移动、血条和飘字 canvas 坐标匹配 world-to-canvas 转换 | stdout 含 `BrotatoLike Playable UX validation PASS`、artifact `status: pass`、`failureReasons=[]`、标准答案字段非空、坐标 checks 通过 | stdout 含 `BrotatoLike Playable UX validation FAIL`、任一正式 HUD/血条/技能栏/点选/飘字/可见性证据缺失或 canvas 坐标偏移超阈值 | `artifacts/brotatolike-playable-ux-validation.json` |
| `res://Src/Validation/Game/Progression/BrotatoLikeProgressionLoopValidation.tscn` | Game/Progression loop | BrotatoLikeGameRuntime、DataOS 敌人 `Unit.ExpReward`、pause/resume 调用、确定性帧推进 | wave runtime state、暂停菜单、schedule gate、HP recovery、dead skip、经验拾取、level-up 反馈和 HUD summary | stdout 含 `BrotatoLike Progression Loop validation PASS`、artifact `status: pass`、`failureReasons=[]`、标准答案字段非空 | stdout 含 `BrotatoLike Progression Loop validation FAIL`、wave/pause/recovery/pickup/experience/level-up 任一证据缺失 | `artifacts/brotatolike-progression-loop-validation.json` |
| `res://Src/Validation/Game/LegacyResources/BrotatoLikeLegacyResourceClassificationValidation.tscn` | Game/Legacy resource classification | DataOS snapshot `resources[]`、`legacyStatus`、Godot `ResourceLoader.Exists` active resource check | 旧 `res://Src/...` / `res://Data/...` 路径全部分类，missing legacy path 不可 active，输出 legacy counts | stdout 含 `BrotatoLike Legacy Resource Classification validation PASS`、artifact `status: pass`、`unsupportedStatusCount=0`、`missingActiveLegacyCount=0` | unsupported `legacyStatus`、missing old path 仍被标记 active、artifact 标准答案字段缺失 | `artifacts/brotatolike-legacy-resource-classification-validation.json` |

## 最新证据

2026-05-21 `validate-brotatolike-dash-main-skill`：

- GameLifecycle：`.ai-temp/scene-tests/runs/2026-05-21/15-14-41/index.json`，artifact `brotatolike-gameplay-lifecycle-validation.json` 为 `status=pass`、`failureReasons=[]`，`dash_input_skill_bar_path` 记录 `dash_selected_skill_id=ability-dash-player-deluyi`、selected index `3`、skill bar selected index `3`、`dash_trigger_result=Success`、释放前后位置 `-638.769,-640 -> -338.769,-640`、`dash_distance=300`、cooldown remaining `>0`、scene-backed skill bar `true`。
- Main playable：`.ai-temp/scene-tests/runs/2026-05-21/15-17-40/index.json`，artifact `scene-acceptance.json` 为 `status=pass`、`failureReasons=[]`；analyzer `gate-report.json` verdict `pass`，requested 1、passed 1、failed 0、missing 0。
- Scene gate 手动检查：上述 run 的 `index.json`、per-scene `result.json` 和 scene artifact 均通过，artifact `expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 均非空。GameLifecycle stderr 仍有既有 Godot RID leak 诊断，不把“无 error”作为正确性证明。

2026-05-21 `stabilize-brotatolike-release-batch`：

- Release-batch gate report：`.ai-temp/scene-tests/runs/2026-05-21/14-57-55/gate-report.json`，verdict `pass`，requested 25、passed 25、failed 0、missing 0。
- Release-batch index：`.ai-temp/scene-tests/runs/2026-05-21/14-57-55/index.json`，25 executed、25 passed、0 failed、0 timed out；manifest metadata、catalog、per-scene `result.json` 和 scene artifact 门禁已闭环。
- PlayableUX targeted evidence：`.ai-temp/scene-tests/runs/2026-05-21/14-57-13/index.json`，artifact `brotatolike-playable-ux-validation.json` 为 `status=pass`、`failureReasons=[]`，历史 `scene_backed_formal_ui` blocker 已在完整 release-batch `023_Src_Validation_Game_PlayableUX_BrotatoLikePlayableUXValidation.tscn_attempt1` 中复验通过。
- Progression targeted evidence：`.ai-temp/scene-tests/runs/2026-05-21/14-55-15/index.json`，artifact `brotatolike-progression-loop-validation.json` 为 `status=pass`、`failureReasons=[]`，历史 `pause_menu_blocks_and_resumes_tick` / `scene_backed_pause_menu` blocker 已在完整 release-batch `024_Src_Validation_Game_Progression_BrotatoLikeProgressionLoopValidation.tscn_attempt1` 中复验通过。
- Scene gate 手动检查：本次 release-batch 的 `index.json`、25 个 per-scene `result.json` 和所有非日志 scene artifact 均通过，artifact `expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 均非空。
- Diagnostic risk：Game/Input 在 passing run 的 `combined.log` 中仍出现 Godot stderr `Parameter "data.tree" is null`，框架 UnitComposition 仍报告 Godot RID leak；当前 gate 依据 `index.json`、`result.json` 和 artifact oracle 接受这些场景，不把“无 error”作为正确性证明。

2026-05-21 `fix-brotatolike-lifecycle-regressions`：

- AI Capability：`.ai-temp/scene-tests/runs/2026-05-21/12-37-18/index.json`，artifact `ai-capability-validation.json` 为 `status=pass`、`failureReasons=[]`，`injected_target_query_nearest_target` 选择 `ai-scene-near` 并忽略 `ai-scene-ability-entity`。
- GameLifecycle：`.ai-temp/scene-tests/runs/2026-05-21/12-39-21/index.json`，artifact `brotatolike-gameplay-lifecycle-validation.json` 为 `status=pass`、`failureReasons=[]`，`death_auto_respawn` 覆盖复活后输入/技能 adapter 重新绑定、真实 `MoveRight` 输入写入和位移。
- PlayableUX：`.ai-temp/scene-tests/runs/2026-05-21/12-39-37/index.json`，artifact `brotatolike-playable-ux-validation.json` 为 `status=pass`、`failureReasons=[]`；`enemy_head_health_bar_canvas_coordinates`、`damage_and_heal_numbers_canvas_coordinates`、`scene_backed_formal_ui` 均为 pass。
- Main smoke：`.ai-temp/scene-tests/runs/2026-05-21/12-39-58/index.json`，artifact `scene-smoke.json` 为 `status=pass`、`failureReasons=[]`。
- Scene gate 手动检查：上述四个 run 的 `index.json`、per-scene `result.json` 和 scene artifact 均通过，artifact `expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 均非空。

2026-05-21 `systemagent-integrated-validation-governance`：

- Targeted gate report：`.ai-temp/scene-tests/runs/2026-05-21/10-06-55/gate-report.json`，verdict `pass`，8/8 targeted scenes 通过；Game/Input 和 GameLifecycle 的 `index.json`、`result.json`、artifact 均通过，`expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 非空。
- GameLifecycle artifact：`.ai-temp/scene-tests/runs/2026-05-21/10-06-55/008_Src_Validation_Game_GameLifecycle_BrotatoLikeGameplayLifecycleValidation.tscn_attempt1/artifacts/brotatolike-gameplay-lifecycle-validation.json`，`status=pass`、`failureReasons=[]`，8 个 lifecycle checks 全部 pass。
- Release-batch gate report：`.ai-temp/scene-tests/runs/2026-05-21/10-13-37/gate-report.json`，requested 25、passed 23、failed 2、missing 0，verdict `block`；manifest metadata、README 五字段、catalog 和 artifact 五字段门禁已闭环，无 `actionItems`。
- 历史 release-batch blocker：`res://Src/Validation/Game/PlayableUX/BrotatoLikePlayableUXValidation.tscn` 在 `.ai-temp/scene-tests/runs/2026-05-21/10-13-37/.../brotatolike-playable-ux-validation.json` 为 `status=fail`；该场景已由 `stabilize-brotatolike-release-batch` 完整 release-batch `.ai-temp/scene-tests/runs/2026-05-21/14-57-55/index.json` 复验通过。
- 历史 release-batch blocker：`res://Src/Validation/Game/Progression/BrotatoLikeProgressionLoopValidation.tscn`，artifact `.ai-temp/scene-tests/runs/2026-05-21/10-13-37/024_Src_Validation_Game_Progression_BrotatoLikeProgressionLoopValidation.tscn_attempt1/artifacts/brotatolike-progression-loop-validation.json` 曾为 `status=fail`，失败 checks `pause_menu_blocks_and_resumes_tick`、`scene_backed_pause_menu`；该场景已由 `stabilize-brotatolike-release-batch` 完整 release-batch `.ai-temp/scene-tests/runs/2026-05-21/14-57-55/index.json` 复验通过。
- Diagnostic risk：Game/Input 在 passing run 的 `combined.log` 中仍出现 Godot stderr `Parameter "data.tree" is null`；当前 gate 依据 `index.json`、`result.json` 和 artifact oracle 接受该场景，不把“无 error”作为正确性证明。

历史证据：

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
