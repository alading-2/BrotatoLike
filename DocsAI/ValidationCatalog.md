# BrotatoLike Validation Catalog

> 集中索引 BrotatoLike 游戏侧 Godot 验证场景和主场景验收。场景级事实源仍是各场景目录下的 `README.md` 或 `DocsAI/GodotSceneTesting.md`。

## 场景索引

| 场景路径 | 能力 | expectedInputs | expectedObservations | passCriteria | failCriteria | artifactPath |
|---|---|---|---|---|---|---|
| `res://Scenes/Main.tscn` | Game/Main playable slice | `GODOT_SCENE_TEST_ARTIFACT_DIR`、DataOS snapshot、确定性 MoveRight/MoveUp/UseSkill/NextSkill/SkillSlot 输入 | 玩家输入与位置变化、DataOS 敌人追逐和接触伤害、finite wave id/phase/expected spawn count、slam/chain ability、Chain Lightning Line2D 端点/清理、HUD 与 skill loadout 证据、数字技能槽直选路径 | stdout 含 `BrotatoLike playable slice PASS`、artifact `status: pass`、`failureReasons` 为空，`skill.chain_line_vfx_bound / cleanup / multi_bounce`、`skill.direct_slot_selected`、finite wave evidence 和 loadout evidence 通过 | stdout 含 `BrotatoLike playable slice FAIL`、任一 criteria 失败或标准答案字段缺失 | `artifacts/scene-acceptance.json` |
| `res://Scenes/Main.tscn --gameos-smoke-exit` | Game/Main smoke | smoke 命令行参数、Main smoke probes、workspace SlimeAI.GameOS project reference | Runtime core、GodotBridge、Pool、DataOS、Ability、Projectile、Effect、AI、Attack、input probes 通过 | stdout 含 `BrotatoLike GameOS smoke PASS`、`scene-smoke.json` 与 `eventbus-dump.json` 被收集 | stdout 含 `BrotatoLike GameOS smoke FAIL`、任一 probe 失败或标准答案字段缺失 | `artifacts/scene-smoke.json` |
| `res://Src/Validation/Game/UnitComposition/BrotatoLikeUnitCompositionValidation.tscn` | Game/UnitComposition | BrotatoLikeGameRuntime、DataOS unit.player/unit.enemy records、BrotatoLikeUnitProfiles、Godot process frames | 玩家 profile 保留游戏侧输入/技能 adapter、真实 process 输入移动、敌人 AIControlled 追逐、动画播放、contact damage bridge | stdout 含 `BrotatoLike UnitComposition validation PASS`、artifact `status: pass`、`failureReasons` 为空 | stdout 含 `BrotatoLike UnitComposition validation FAIL`、任一 player/enemy/process/animation/contact check 失败或标准答案字段缺失 | `artifacts/brotatolike-unit-composition-validation.json` |
| `res://Src/Validation/Game/Input/BrotatoLikeInputEventValidation.tscn` | Game/Input | 玩家输入事件、BrotatoLike 输入 adapter、主动技能输入 adapter、`SkillSlot1..4` input actions | 输入事件桥接到 game-side EventBus、移动方向写入、Next/Previous/Use 技能切换与触发、数字技能槽直选可见 active slot 且越界槽位被忽略 | stdout 含 `BrotatoLike Game Input validation PASS`、artifact `status: pass`、`failureReasons` 为空、`InputSelectSkillSlot` evidence 和标准答案字段非空 | stdout 含 `BrotatoLike Game Input validation FAIL`、artifact `status: fail`、直接槽位/旧切换语义失败或标准答案字段缺失 | `artifacts/brotatolike-input-event-validation.json` |
| `res://Src/Validation/Game/GameLifecycle/BrotatoLikeGameplayLifecycleValidation.tscn` | Game/GameLifecycle feature-slice | BrotatoLikeGameRuntime、DataOS `unit.player/deluyi`、玩家实体、Damage/Movement/Ability/Collision/Unit、输入/HUD/进度服务 | 死亡阻断移动和技能输入、Dash 通过技能栏 input path 选中/释放并位移、Camera 死亡期间保持启用、自动重生恢复 HP/位置/input/camera、Camera 跟随、并发系统不冲突、暂停/恢复状态完整、HUD 死亡/重生状态干净 | stdout 含 `BrotatoLike Gameplay Lifecycle validation PASS`、artifact `status: pass`、`failureReasons=[]`、9 个 lifecycle checks 全部 pass、标准答案字段非空 | stdout 含 `BrotatoLike Gameplay Lifecycle validation FAIL`、任一 lifecycle check 失败、artifact `status: fail` 或标准答案字段缺失 | `artifacts/brotatolike-gameplay-lifecycle-validation.json` |
| `res://Src/Validation/Game/PlayableUX/BrotatoLikePlayableUXValidation.tscn` | Game/Playable UX | BrotatoLikeGameRuntime、DataOS 玩家和敌人、Godot `MoveRight/NextSkill/PreviousSkill/UseSkill/SkillSlot` input actions、正式 UI 节点、Camera2D offset、validation loadout override | `BrotatoLikeHUD`、`PlayerHealthLabel`、池化头顶血条、四槽技能栏、数字技能槽直选、loadout source/owned/visible/selected/total evidence、validation override 12 owned / 4 visible、点选指示器、池化伤害/治疗飘字、鱼人/豺狼人血条高度、玩家可见移动、血条和飘字 canvas 坐标匹配 world-to-canvas 转换 | stdout 含 `BrotatoLike Playable UX validation PASS`、artifact `status: pass`、`failureReasons=[]`、`skill_bar_direct_slot_input_updates / validation_loadout_override_visible_slots / damage_and_heal_numbers_lifecycle` 通过、标准答案字段非空、坐标 checks 通过 | stdout 含 `BrotatoLike Playable UX validation FAIL`、任一正式 HUD/血条/技能栏/loadout/点选/飘字/池化 cleanup/可见性证据缺失或 canvas 坐标偏移超阈值 | `artifacts/brotatolike-playable-ux-validation.json` |
| `res://Src/Validation/Game/Skills/BrotatoLikeSkillValidation.tscn` | Game/Skills projectile/passive | BrotatoLikeGameRuntime、DataOS snapshot、`ValidationAllSkillAbilityIds` deterministic loadout、确定性 enemy targets、手动 `AbilityService.TryTrigger` | `sine_wave_shot / boomerang_throw / bezier_shot / parabola_shot / arc_shot` 记录 projectile scene path、movement mode、target/direction、hit/damage、runtime cleanup；`orbit_skill / circle_damage / aura_shield` 记录 sustained/passive runtime entities、视觉 scene、玩法效果、effect/projectile cleanup 和 pool-backed visual lifecycle evidence；artifact checks 按 ability id 分组 | stdout 含 `BrotatoLike Skill validation PASS`、artifact `status: pass`、8 个 ability-id checks 和 `ability_id_grouping` 全部 pass、cleanup evidence 与标准答案字段非空 | stdout 含 `BrotatoLike Skill validation FAIL`、任一 ability id 的 trigger/projectile/effect/movement/hit/damage/visual cleanup 证据缺失，或 artifact 未按 ability id 分组 | `artifacts/brotatolike-skill-validation.json` |
| `res://Src/Validation/Game/Progression/BrotatoLikeProgressionLoopValidation.tscn` | Game/Progression loop | BrotatoLikeGameRuntime、DataOS 敌人 `Unit.ExpReward`、pause/resume 调用、升级选择、确定性帧推进 | wave runtime state、暂停菜单、schedule gate、HP recovery、dead skip、经验拾取、level-up 反馈、scene-backed 经验条、scene-backed 升级三选一、属性奖励、技能奖励和升级门禁 | stdout 含 `BrotatoLike Progression Loop validation PASS`、artifact `status: pass`、`failureReasons=[]`、`level_up_choice_*` 与 `experience_ui_scene_backed_updates` checks 通过、标准答案字段非空 | stdout 含 `BrotatoLike Progression Loop validation FAIL`、wave/pause/recovery/pickup/experience/level-up choice/formal experience UI 任一证据缺失 | `artifacts/brotatolike-progression-loop-validation.json` |
| `res://Src/Validation/Game/Shop/BrotatoLikeShopItemValidation.tscn` | Game/Shop item loop | BrotatoLikeGameRuntime、DataOS `item_definition` / `shop_offer` authoring、确定性 validation offer set、玩家 Runtime Data 货币 | item 定义包含 id/name/price/rarity/effect/icon，未知 effect target 被拒绝；shop UI 记录 offer ids/prices/affordable/purchased/currency/result/close；可负担购买扣货币、获得道具并修改 Runtime Data；买不起时保持货币和属性不变并记录 `insufficient_currency` | stdout 含 `BrotatoLike Shop Item validation PASS`、artifact `status: pass`、`item_authoring_loaded_and_validates_targets / shop_offers_deterministic / affordable_purchase_applies_item / unaffordable_purchase_rejected / shop_ui_updates_and_cleanup` 全部 pass、标准答案字段非空 | stdout 含 `BrotatoLike Shop Item validation FAIL`、任一 authoring/offer/purchase/effect/UI cleanup 证据缺失，或 artifact 标准答案字段为空 | `artifacts/brotatolike-shop-item-validation.json` |
| `res://Src/Validation/Game/RunFlow/BrotatoLikeRunFlowValidation.tscn` | Game/RunFlow multi-wave loop | BrotatoLikeGameRuntime、DataOS `wave_definition` / `wave_enemy_entry` authoring、两波 finite deterministic enemy entries、生产 Progression/Spawn/HUD/Shop hook | wave authoring reference validation、第一波 Running 和 expected/actual spawn count、pause gate、死亡/复活、第一波 Completed -> RewardShop、shop offer hook、第二波 Running 和 expected/actual spawn count、cleanup count、scene-backed ExperienceBarUI wave phase、finite authoring 说明 | stdout 含 `BrotatoLike Run Flow validation PASS`、artifact `status: pass`、`wave_authoring_loaded_and_validates_refs / first_wave_starts_and_spawns / pause_gate_and_respawn_preserved / first_wave_completion_reward_phase / second_wave_starts / wave_cleanup_counts / wave_ui_phase_scene_backed` 全部 pass、两波 expected/actual spawn count 匹配且标准答案字段非空 | stdout 含 `BrotatoLike Run Flow validation FAIL`、任一 authoring/transition/spawn count/pause/respawn/cleanup/UI phase 证据缺失，或 artifact 标准答案字段为空 | `artifacts/brotatolike-run-flow-validation.json` |
| `res://Src/Validation/Game/CharacterSelection/BrotatoLikeCharacterSelectionValidation.tscn` | Game/CharacterSelection selectable character loop | BrotatoLikeGameRuntime、DataOS `character_definition` / `character_loadout` authoring、scene-backed CharacterSelectPanelUI、生产 HUD/input/camera services | character catalog 记录 id/display/player record/visual/stats/loadout，非法 player/visual 引用被拒绝；UI 记录 scene path、visible ids 和按钮选择；`deluyi` 与 `guangfa` 分别生成并记录 entity/visual/stats/starting skills；两者存在视觉、属性和 loadout 差异；两个玩家都绑定 input、active skill input、HUD、PlayerHealthBar、ActiveSkillBar 和 camera | stdout 含 `BrotatoLike Character Selection validation PASS`、artifact `status: pass`、`character_catalog_authoring_valid / character_select_ui_scene_backed / selected_character_spawns_runtime_player / two_characters_distinct_evidence / selected_player_bindings` 全部 pass、标准答案字段非空 | stdout 含 `BrotatoLike Character Selection validation FAIL`、任一 authoring/UI selected spawn/双角色差异/player binding 证据缺失，或 artifact 标准答案字段为空 | `artifacts/brotatolike-character-selection-validation.json` |
| `res://Src/Validation/Game/LegacyResources/BrotatoLikeLegacyResourceClassificationValidation.tscn` | Game/Legacy resource classification | DataOS snapshot `resources[]`、`legacyStatus`、Godot `ResourceLoader.Exists` active resource check | 旧 `res://Src/...` / `res://Data/...` 路径全部分类，missing legacy path 不可 active，输出 legacy counts | stdout 含 `BrotatoLike Legacy Resource Classification validation PASS`、artifact `status: pass`、`unsupportedStatusCount=0`、`missingActiveLegacyCount=0` | unsupported `legacyStatus`、missing old path 仍被标记 active、artifact 标准答案字段缺失 | `artifacts/brotatolike-legacy-resource-classification-validation.json` |

## 最新证据

2026-05-21 `fix-brotatolike-runtime-pooling-input-wave-ui-regressions`：

- Build：`Tools/run-build.sh` PASS（DataOS validation PASS，`0 Warning(s), 0 Error(s)`）。
- Targeted Godot run：`.ai-temp/scene-tests/runs/2026-05-21/21-56-47/index.json` 为 7 executed、7 passed、0 failed、0 stderr lines；`gate-report.json` verdict `pass`，manifest requested 5、passed 5、failed 0、missing 0。
- Effect / Projectile：`effect-capability-validation.json` 的 `finite_visual_lifecycle_pool_cleanup` 和 `projectile-capability-validation.json` 的 `visual_lifecycle_pool_cleanup` 均 pass，记录 node registry cleanup 与 `poolReturned=true`。
- Input / Skill slot：`brotatolike-input-event-validation.json` 记录 `InputSelectSkillSlot`、`SkillSlot3 -> index 2`、越界槽位 ignored、Next/Previous/Use 仍可用；Main artifact 记录 `SkillSlot1` 与 `SkillSlot4` 真实 action path。
- HUD / PlayableUX：`brotatolike-playable-ux-validation.json` 的 `skill_bar_direct_slot_input_updates / damage_and_heal_numbers_lifecycle / enemy_head_health_bar_canvas_coordinates` 均 pass，记录鱼人/豺狼人血条高度、伤害/治疗飘字 active/idle pool evidence。
- RunFlow / Main：RunFlow artifact 记录两波 finite authoring expected/actual spawn count 均为 5；Main artifact 记录 `wave_current_id=1`、`wave_phase=Running`、`wave_expected_spawn_count=5`、`wave_actual_spawned_count=5`、`wave_authoring_kind=finite`。
- Scene gate：已检查 `index.json`、7 个 per-scene `result.json`、7 个 scene artifact 和 `gate-report.json`；所有 artifact 的 `expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 均非空，`failureReasons=[]`。

2026-05-21 `brotatolike-character-selection`：

- Build：`Tools/run-build.sh` PASS（DataOS validation PASS，`runtime_snapshot.json`、`shop_item_authoring.json`、`wave_authoring.json`、`character_authoring.json` 已生成，85 个 XML comment warnings，0 errors）。
- CharacterSelection validation：`.ai-temp/scene-tests/runs/2026-05-21/20-00-19/index.json` PASS；per-scene `result.json` exitCode `0`、`firstError=null`；artifact `brotatolike-character-selection-validation.json` 为 `status=pass`、`failureReasons=[]`。
- 关键 artifact 证据：`character_ids=deluyi,guangfa`、非法 player/visual rejected、`character_select_panel_scene_path=res://Scenes/UI/CharacterSelectPanelUI.tscn`、visible ids `deluyi,guangfa`、按钮选择 `guangfa`、`player-guangfa`、visual `res://assets/Unit/Player/guangfa/AnimatedSprite2D/guangfa.tscn`、起始技能 `chain_lightning,sine_wave_shot,target_point_skill,dash`、`visual_differs=true`、`stats_differ=true`、`loadout_differs=true`、两名角色 input/HUD/health bar/skill UI/camera 绑定均通过。
- Main 回归：`.ai-temp/scene-tests/runs/2026-05-21/20-03-04/index.json` PASS；artifact `scene-acceptance.json` 为 `status=pass`、`failureReasons=[]`，记录默认角色 fallback `skill_loadout_source=character:deluyi`。
- Scene gate：CharacterSelection 与 Main 两个 `gate-report.json` verdict 均为 `pass`；已检查两组 `index.json`、per-scene `result.json` 和 scene artifact，README 与 artifact 的 `expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 均非空。

2026-05-21 `complete-brotatolike-wave-run-flow`：

- Build：`Tools/run-build.sh` PASS（DataOS validation PASS，`runtime_snapshot.json`、`shop_item_authoring.json`、`wave_authoring.json` 已生成，49 个既有 XML comment warnings，0 errors）。
- RunFlow validation：`.ai-temp/scene-tests/runs/2026-05-21/19-17-09/index.json` PASS；per-scene `result.json` exitCode `0`、`firstError=null`；artifact `brotatolike-run-flow-validation.json` 为 `status=pass`、`failureReasons=[]`。
- 关键 artifact 证据：`wave_count=2`、`wave1_expected_spawn_count=5`、`wave2_expected_spawn_count=5`、非法 enemy/resource rejected、第一波 `Running` 且生成 `chailangren,chailangren,yuren,yuren,yuren`、pause tick 阻断/恢复、玩家死亡后复活 HP `100`、第一波 `Completed -> RewardShop`、`reward_hook=shop_offer.validation`、cleanup `runtime 10 -> 5` / enemy `5 -> 0`、第二波 `Running` 且生成 `chailangren,chailangren,chailangren,yuren,yuren`、`ExperienceBarUI` scene path 与 wave phase。
- Progression 回归：`.ai-temp/scene-tests/runs/2026-05-21/19-18-08/index.json` PASS；artifact `brotatolike-progression-loop-validation.json` 为 `status=pass`、`failureReasons=[]`。
- Main 回归：`.ai-temp/scene-tests/runs/2026-05-21/19-18-22/index.json` PASS；artifact `scene-acceptance.json` 为 `status=pass`、`failureReasons=[]`。
- Scene gate：RunFlow、Progression、Main 三个 `gate-report.json` verdict 均为 `pass`；已检查三组 `index.json`、per-scene `result.json` 和 scene artifact，README 与 artifact 的 `expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 均非空。

2026-05-21 `design-brotatolike-shop-item-loop`：

- Build：`Tools/run-build.sh` PASS（DataOS validation PASS，`runtime_snapshot.json` 与 `shop_item_authoring.json` 已生成，0 warnings，0 errors）。
- Shop/Item validation：`.ai-temp/scene-tests/runs/2026-05-21/18-39-01/index.json` PASS；per-scene `result.json` exitCode `0`；artifact `brotatolike-shop-item-validation.json` 为 `status=pass`、`failureReasons=[]`。
- 关键 artifact 证据：`offer_ids=vital_seed,swift_boots,sharpening_stone`、`offer_prices=12,8,20`、`offer_source=DataOS:shop_offer.validation`、可负担购买 `vital_seed` 后货币 `15 -> 3`、`Damage.MaxHp` 增加、owned item 记录 `vital_seed`；购买 `sharpening_stone` 因 `insufficient_currency` 被拒绝且货币与 `Attack.Damage` 不变。
- Scene gate：`.ai-temp/scene-tests/runs/2026-05-21/18-39-01/gate-report.json` verdict `pass`；已检查 `index.json`、per-scene `result.json` 和 scene artifact，README 与 artifact 的 `expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 均非空。

2026-05-21 `design-brotatolike-levelup-choice-loop`：

- Build：`Tools/run-build.sh` PASS（DataOS validation PASS，26 个既有 XML comment warnings，0 errors）。
- Progression validation：`.ai-temp/scene-tests/runs/2026-05-21/17-45-01/index.json` PASS；per-scene `result.json` exitCode `0`；artifact `brotatolike-progression-loop-validation.json` 为 `status=pass`、`failureReasons=[]`，checks `level_up_choice_scene_backed / level_up_choice_applies_stat_reward / level_up_choice_applies_ability_reward / level_up_choice_gate_blocks_and_resumes_tick / experience_ui_scene_backed_updates` 全部 pass。
- 关键 artifact 证据：`choice_ids=max_hp_plus_10,move_speed_plus_20,unlock_sine_wave_shot`、`choice_effect_types=AddMaxHp,AddMoveSpeed,GrantAbility`、`choice_panel_scene_path=res://Scenes/UI/LevelUpChoicePanelUI.tscn`、`experience_bar_scene_path=res://Scenes/UI/ExperienceBarUI.tscn`、HP `100 -> 110`、owned ability count `4 -> 5`、`sine_wave_ability_owned=true`、gate `ModalUi/Suspended -> None/Running`。
- PlayableUX：`.ai-temp/scene-tests/runs/2026-05-21/17-45-17/index.json` PASS，artifact `brotatolike-playable-ux-validation.json` 为 `status=pass`、`failureReasons=[]`。
- Main 回归：`.ai-temp/scene-tests/runs/2026-05-21/17-45-33/index.json` PASS，artifact `scene-acceptance.json` 为 `status=pass`、`failureReasons=[]`。
- Analyzer：上述三个 run 的 `gate-report.json` verdict 均为 `pass`；README 五字段与 artifact `expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 均非空。

2026-05-21 `validate-brotatolike-projectile-and-passive-skills`：

- Build：`Tools/run-build.sh` PASS（DataOS validation PASS，26 个既有 XML comment warnings，0 errors）。
- Skills validation：`.ai-temp/scene-tests/runs/2026-05-21/17-16-37/index.json` PASS；per-scene `result.json` exitCode `0`；artifact `brotatolike-skill-validation.json` 为 `status=pass`、`failureReasons=[]`，checks `sine_wave_shot / boomerang_throw / bezier_shot / parabola_shot / arc_shot / orbit_skill / circle_damage / aura_shield / ability_id_grouping` 全部 pass。
- 关键 artifact 证据：`sine_wave_shot=SineWave/ArrowNeedle`、`boomerang_throw=Boomerang/BulletDiamond`、`bezier_shot=5x BezierCurve/ArrowNeedle`、`parabola_shot=CircularArc/ArrowNeedle`、`arc_shot=CircularArc/BoomerangChevron`、`orbit_skill=3x Orbit/BulletDiamond`、`circle_damage=Effect 003 radius damage`、`aura_shield=AttachToHost/BulletDiamond`；各技能均记录 hit/damage 和 cleanup。
- Analyzer：`.ai-temp/scene-tests/runs/2026-05-21/17-16-37/gate-report.json` verdict `pass`，README 五字段与 artifact `expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 均为 true。
- Main 回归：`.ai-temp/scene-tests/runs/2026-05-21/17-17-42/index.json` PASS，artifact `scene-acceptance.json` 为 `status=pass`、`failureReasons=[]`，analyzer `gate-report.json` verdict `pass`。

2026-05-21 `expand-brotatolike-skill-loadout`：

- Build：`Tools/run-build.sh` PASS（DataOS validation PASS，26 个既有 XML comment warnings，0 errors）。
- PlayableUX：`.ai-temp/scene-tests/runs/2026-05-21/16-01-37/index.json`，artifact `brotatolike-playable-ux-validation.json` 为 `status=pass`、`failureReasons=[]`；`validation_loadout_override_visible_slots` 通过，记录 12 owned / 4 visible / 8 hidden、available pool 12 项、passive ids `orbit_skill,circle_damage,aura_shield`。
- Main playable：`.ai-temp/scene-tests/runs/2026-05-21/16-05-14/index.json`，artifact `scene-acceptance.json` 为 `status=pass`、`failureReasons=[]`；记录 `skill_loadout_source=default`、owned ids、visible slot ids、selected id、`skill_total_owned_count=4`、available pool 12 项。
- Analyzer：PlayableUX 与 Main 的 `gate-report.json` verdict 均为 `pass`。
- Scene gate 手动检查：上述 run 的 `index.json`、per-scene `result.json` 和 scene artifact 均通过，artifact `expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 均非空。

2026-05-21 `restore-brotatolike-chain-lightning-line-vfx`：

- Main playable：`.ai-temp/scene-tests/runs/2026-05-21/15-43-35/index.json`，artifact `scene-acceptance.json` 为 `status=pass`、`failureReasons=[]`；新增 `skill.chain_line_vfx_bound / cleanup / multi_bounce` 全部通过。
- Chain Lightning line VFX evidence：artifact 记录 expected `3`、recorded `3`、bound `3`、cleanup `3`，scene path 为 `res://Scenes/VFX/LightningLineEffect.tscn`，source/target 为 `player-deluyi->spawn-chailangren-1;spawn-chailangren-1->spawn-chailangren-2;spawn-chailangren-2->spawn-yuren-3`，Line2D local points 和 world points 与 start/end positions 匹配，duration 为 `0.2;0.2;0.2`。
- Analyzer：`.ai-temp/scene-tests/runs/2026-05-21/15-43-35/gate-report.json` verdict `pass`。
- Scene gate 手动检查：本次 run 的 `index.json`、per-scene `result.json` 和 scene artifact 均通过，artifact `expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 均非空。

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
