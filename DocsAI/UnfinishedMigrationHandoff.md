# BrotatoLike 未完成迁移接手文档

> 更新日期：2026-05-21  
> 当前游戏仓基线：以当前 `Games/BrotatoLike` 仓库 HEAD 为准；继续前请先运行 `git log -1 --oneline` 和 `git status --short`。  
> 用途：给新对话继续拆分 BrotatoLike 旧框架迁移、可玩体验补完和验收修复时使用。  
> 重要边界：本文是接手导航，不是完成声明。凡是写为“存在”“已接入 DataOS”“handler 已有”的项目，都还需要看对应 artifact 或补专项验证，不能直接当作玩家可玩功能完成。

## 1. 先读什么

新对话开始后，按下面顺序读，避免从旧输入或过期结论直接动手：

1. 工作区规则：`/home/slime/Code/SlimeAI/AGENTS.md`
2. SystemAgent 入口：`/home/slime/Code/SlimeAI/Workspace/SystemAgent/README.md`
3. SystemAgent 索引：`/home/slime/Code/SlimeAI/Workspace/SystemAgent/INDEX.md`
4. 框架事实源：`/home/slime/Code/SlimeAI/SlimeAI/DocsAI/INDEX.md`
5. 游戏事实源：`/home/slime/Code/SlimeAI/Games/BrotatoLike/DocsAI/INDEX.md`
6. 游戏当前状态：`/home/slime/Code/SlimeAI/Games/BrotatoLike/DocsAI/GameProjectState.md`
7. 迁移台账：`/home/slime/Code/SlimeAI/Games/BrotatoLike/DocsAI/MigrationLedger.md`
8. 功能审计：`/home/slime/Code/SlimeAI/Games/BrotatoLike/DocsAI/BrotatoMyFeatureMigrationAudit.md`
9. Scene-first UX 原则：`/home/slime/Code/SlimeAI/Games/BrotatoLike/DocsAI/SceneFirstUXMigration.md`
10. 验证目录：`/home/slime/Code/SlimeAI/Games/BrotatoLike/DocsAI/ValidationCatalog.md`

按任务选择 owner skill：

- UI / HUD / 血条 / 技能栏：`ui-bind`
- 技能释放 / 冷却 / 自动索敌 / 点选：`ability-system`
- 伤害 / 治疗 / HP / 伤害数字：`damage-system`
- 位移 / Dash / 投射物轨迹：`movement-system`
- 投射物 / 特效 / 链电线段：`projectile-effect-system`
- DataOS seed / snapshot / authoring 表：`data-authoring`
- Godot headless 场景和 artifact：`godot-scene-test`
- 功能级变更或长期迁移计划：优先走 OpenSpec change

旧输入位置要分清：

- 当前游戏仓内的迁移输入入口：`/home/slime/Code/SlimeAI/Games/BrotatoLike/MigrationInput/README.md`
- 旧项目原始参考仍主要来自：`/home/slime/Code/SlimeAI/Resources/Else/brotato-my`
- 不要把旧 `Resources/Else/brotato-my` 当作新实现事实源；它只用于对照职责、资源和数值。

## 2. 当前已完成的可用基线

`9fa6ab4` 之后，普通 `Scenes/Main.tscn` 和 targeted validation 已经覆盖以下基础体验：

- 玩家复活：死亡后原地复活，复活期间 HP 随 `RespawnProgress` 增长。
- HUD：正式 `BrotatoLikeHud`，左上角状态面板、玩家 HP、progression summary、底部四槽技能栏。
- 血条：`HealthBarKind.Player / Enemy / Neutral`，玩家绿色、敌人红色、通用语义入口已经存在。
- 技能栏：普通玩家默认四槽为 `slam / chain_lightning / target_point_skill / dash`。
- 技能 loadout：`BrotatoLikeSkillLoadoutAuthoring` 已把默认四槽、12 项 available skill pool、passive ids 和 deterministic validation override 集中到游戏侧 authoring；`ActiveSkillBarUI` artifact 可区分 visible active slots、total owned abilities 和 selected ability。
- 技能输入：`UseSkill / PreviousSkill / NextSkill` action 可驱动当前技能栏状态；Dash 已有通过技能栏 input path 触发并位移的 GameLifecycle 专项证据。
- 点选技能：`target_point_skill` 有指示器、确认、取消和死亡清理。
- 伤害反馈：敌人头顶血条、伤害数字、治疗数字有 scene-backed UI。
- 运行时：`BrotatoLikeGameRuntime` 是主运行时节点，`Main.cs` 负责启动路径和 acceptance。
- 进度首版闭环：wave runtime state、pause menu、pause schedule gate、HP recovery、经验 pickup、scene-backed 经验条、level-up feedback、升级三选一、属性奖励、hidden ability 解锁和升级选择门禁已有最小实现。

最近可引用证据：

- Main artifact：`.ai-temp/scene-tests/runs/2026-05-21/17-45-33/001_Scenes_Main.tscn_attempt1/artifacts/scene-acceptance.json`
- GameLifecycle artifact：`.ai-temp/scene-tests/runs/2026-05-21/15-14-41/001_Src_Validation_Game_GameLifecycle_BrotatoLikeGameplayLifecycleValidation.tscn_attempt1/artifacts/brotatolike-gameplay-lifecycle-validation.json`
- PlayableUX artifact：`.ai-temp/scene-tests/runs/2026-05-21/17-45-17/001_Src_Validation_Game_PlayableUX_BrotatoLikePlayableUXValidation.tscn_attempt1/artifacts/brotatolike-playable-ux-validation.json`
- Progression artifact：`.ai-temp/scene-tests/runs/2026-05-21/17-45-01/001_Src_Validation_Game_Progression_BrotatoLikeProgressionLoopValidation.tscn_attempt1/artifacts/brotatolike-progression-loop-validation.json`

如果新对话要声明这些功能仍然通过，需要重新跑对应命令并读取新 artifact。历史 artifact 只能说明当时通过。

## 3. 事实边界

后续最容易误判的是下面几类：

- `DataOS-only` 不等于可玩。`DataOS/Authoring/BrotatoLike.seed.sql` 有记录，只能说明 authoring 和 snapshot 字段存在。
- handler 或 skill pool 存在不等于玩家可释放。`BrotatoLikeAbilityHandlers.cs` 有执行逻辑，`BrotatoLikeSkillLoadoutAuthoring.AvailableSkillPoolAbilityIds` 也可能只表示可获得候选或 validation loadout 候选；是否成为普通局内技能仍要看获得流程、visible slot 和 artifact。
- scene 存在不等于视觉完整。`Scenes/VFX/LightningLineEffect.tscn` 已通过 Main artifact 验证端点、生命周期和节点清理；屏幕像素级门禁仍是可选增强。
- runner `Input.ActionPress` 不等于真实设备 QA。真实手柄、鼠标、窗口焦点、焦点导航仍要单独验。
- 旧 resource path 分类不等于旧功能完成。`legacyStatus` 只保证旧路径被分类，不代表可加载或可用。
- 不要直接改 `Games/BrotatoLike/SlimeAI/` 里的框架代码。框架改动要去 `/home/slime/Code/SlimeAI/SlimeAI`，再按 submodule 流程更新游戏仓指针。

## 4. P0 接手项：验证基线已重新拉齐

### 4.1 release-batch 历史 blocker 已处理

现状：

- 历史 release-batch `.ai-temp/scene-tests/runs/2026-05-21/10-13-37/gate-report.json` 是 `block`，当时失败点是：
  - `BrotatoLikePlayableUXValidation` 的 `scene_backed_formal_ui`
  - `BrotatoLikeProgressionLoopValidation` 的 `pause_menu_blocks_and_resumes_tick / scene_backed_pause_menu`
- OpenSpec change `stabilize-brotatolike-release-batch` 已补齐新的完整 release-batch evidence：`.ai-temp/scene-tests/runs/2026-05-21/14-57-55/gate-report.json` 为 `verdict=pass`，requested 25、passed 25、failed 0、missing 0。
- PlayableUX targeted run `.ai-temp/scene-tests/runs/2026-05-21/14-57-13/index.json` 通过；Progression targeted run `.ai-temp/scene-tests/runs/2026-05-21/14-55-15/index.json` 通过。
- 本次 release-batch 已检查 `index.json`、25 个 per-scene `result.json` 和所有非日志 scene artifact，`expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 均非空。

仍需注意：

- Game/Input 在 passing run 的 `combined.log` 中仍出现 Godot stderr `Parameter "data.tree" is null`。
- 框架 UnitComposition passing run 中仍有 Godot RID leak 诊断。
- 以上两项当前不覆盖 artifact oracle，不能把当前 `gate-report=pass` 表述为“无 stderr / 无引擎诊断”。

关键路径：

- `DocsAI/ValidationManifest.json`
- `DocsAI/ValidationCatalog.md`
- `Src/Validation/Game/PlayableUX/BrotatoLikePlayableUXValidationScene.cs`
- `Src/Validation/Game/Progression/BrotatoLikeProgressionLoopValidationScene.cs`
- `Src/Game/Progression/BrotatoLikeProgressionService.cs`
- `Scenes/UI/PauseMenuUI.tscn`

复验命令：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-build.sh
Tools/run-godot-scene.sh run-all --manifest DocsAI/ValidationManifest.json --release-batch --continue-on-fail --log-dir .ai-temp/scene-tests/runs --errors-only
Tools/analyze-godot-scene-logs.sh --run-dir <new-run-dir> --manifest DocsAI/ValidationManifest.json --gate-report <new-run-dir>/gate-report.json
```

接手注意：

- Godot scene gate 要检查 `index.json`、每个场景的 `result.json` 和 artifact。
- artifact 中 `expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 必须非空。
- 如果后续只跑 targeted scene，不要把它写成完整 release-batch 已恢复；当前完整 release-batch evidence 固定引用 `.ai-temp/scene-tests/runs/2026-05-21/14-57-55/gate-report.json`。

## 5. P1 接手项：玩家技能体验还没完整迁完

### 5.1 默认四槽以外的技能已有 pool / validation loadout，仍缺逐技能体验验收

现状：

- 玩家默认技能由 `Src/Game/BrotatoLikeSkillLoadoutAuthoring.cs` 集中装配，并在 `BrotatoLikeGameRuntime.SpawnPlayer()` 使用：
  - `slam`
  - `chain_lightning`
  - `target_point_skill`
  - `dash`
- `BrotatoLikeSkillLoadoutAuthoring.AvailableSkillPoolAbilityIds` 已显式列出完整可获得候选：
  - `slam`
  - `chain_lightning`
  - `target_point_skill`
  - `dash`
  - `sine_wave_shot`
  - `boomerang_throw`
  - `bezier_shot`
  - `parabola_shot`
  - `arc_shot`
  - `orbit_skill`
  - `circle_damage`
  - `aura_shield`
- passive ids 当前为：
  - `orbit_skill`
  - `circle_damage`
  - `aura_shield`
- deterministic validation loadout 可生成 12 个 owned ability entity，但 UI 只暴露 4 个 visible active slots，隐藏/被动技能不会被普通 `NextSkill/UseSkill` 误选或误触发。

已验证：

- Build：`cd /home/slime/Code/SlimeAI/Games/BrotatoLike && Tools/run-build.sh` PASS（26 个既有 XML comment warnings，0 errors）。
- PlayableUX：`.ai-temp/scene-tests/runs/2026-05-21/16-01-37/index.json` PASS；artifact `validation_loadout_override_visible_slots=pass`，记录 12 owned / 4 visible / 8 hidden。
- Main：`.ai-temp/scene-tests/runs/2026-05-21/16-05-14/index.json` PASS；artifact 记录 `skill_loadout_source=default`、owned ids、visible slot ids、selected id、total count 和 12 项 `skill_available_pool_ids`。
- Skills：`.ai-temp/scene-tests/runs/2026-05-21/17-16-37/index.json` PASS；artifact `brotatolike-skill-validation.json` 按 ability id 逐项记录 `sine_wave_shot / boomerang_throw / bezier_shot / parabola_shot / arc_shot / orbit_skill / circle_damage / aura_shield` 的 scene path、movement mode、hit/damage 和 cleanup。
- Scene gate 已检查上述 run 的 `index.json`、per-scene `result.json` 和 scene artifact；`expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 均非空，analyzer `gate-report.json` 为 `pass`。

仍缺什么：

- 普通局内获得流程：升级三选一首版可授予 hidden ability，但 visible slot 替换、商店购买、分页或 passive panel 还未实现。
- 不要把 available skill pool 写成“已可玩”；它目前是可获得候选、validation loadout 来源和后续升级/商店池输入。

关键路径：

- `DataOS/Authoring/BrotatoLike.seed.sql`
- `DataOS/Snapshots/runtime_snapshot.json`
- `Src/Game/BrotatoLikeGameRuntime.cs`
- `Src/Game/BrotatoLikeAbilityHandlers.cs`
- `Src/Game/GodotActiveSkillInputComponent.cs`
- `Src/Game/UI/ActiveSkillBarUI.cs`
- `Src/Game/UI/ActiveSkillSlotUI.cs`
- `Scenes/UI/ActiveSkillBarUI.tscn`
- `Scenes/UI/ActiveSkillSlotUI.tscn`
- `Src/Validation/Game/PlayableUX/BrotatoLikePlayableUXValidationScene.cs`
- `Src/Validation/Game/Skills/BrotatoLikeSkillValidationScene.cs`

旧输入参考：

- `Resources/Else/brotato-my/Data/Data/Ability/Ability/Movement/*`
- `Resources/Else/brotato-my/Data/Data/Ability/Resource/Movement/*`
- `Resources/Else/brotato-my/Data/Data/Ability/Ability/CircleDamage`
- `Resources/Else/brotato-my/Data/Data/Ability/Resource/OrbitSkillConfig.tres`

建议拆分：

- OpenSpec `expand-brotatolike-skill-loadout`：已定义默认四槽、available skill pool、passive ids 和 deterministic validation loadout。
- OpenSpec `validate-brotatolike-projectile-and-passive-skills`：已完成 projectile 技能轨迹、命中、生命周期，以及 `orbit_skill / circle_damage / aura_shield` 的持续效果和清理专项验收。

### 5.2 Dash 玩家输入位移专项验收已补齐

现状：

- `dash` 已进入默认四槽。
- DataOS 中有 `ability/dash`、`ability_movement_charge` 和 dash effect。
- `BrotatoLikeDashAbilityHandler` 能启动 Charge Movement 并生成 Effect，且在正式 runtime 初始化时使用共享 `GodotMovementDriver.MovementSystem`，input path 触发后可同步 Godot player position。
- `BrotatoLikeGameplayLifecycleValidation` 已新增 `dash_input_skill_bar_path`，通过 `NextSkill` 按 ability id 选中 `ability-dash-player-deluyi`，再用 `UseSkill` 触发 Dash。
- 最新 GameLifecycle artifact `.ai-temp/scene-tests/runs/2026-05-21/15-14-41/001_Src_Validation_Game_GameLifecycle_BrotatoLikeGameplayLifecycleValidation.tscn_attempt1/artifacts/brotatolike-gameplay-lifecycle-validation.json` 记录 `dash_trigger_result=Success`、释放前后位置 `-638.769,-640 -> -338.769,-640`、距离 `300`、cooldown remaining `>0`、scene-backed skill bar。
- Main 回归 `.ai-temp/scene-tests/runs/2026-05-21/15-17-40/index.json` 通过，analyzer `gate-report.json` verdict `pass`。

仍需注意：

- 当前自动验收覆盖 runner `Input.ActionPress`，不等于真实键鼠/手柄设备 QA。
- Dash 视觉样式仍是当前 effect 资源路径和 runtime event 证据，没有做像素级美术验收。
- GameLifecycle passing run 的 stderr 仍有既有 Godot RID leak 诊断，不能把通过表述为“无 stderr”。

关键路径：

- `Src/Game/BrotatoLikeGameRuntime.cs`
- `Src/Game/BrotatoLikeAbilityHandlers.cs`
- `Src/Game/GodotActiveSkillInputComponent.cs`
- `Src/Game/BrotatoLikePlayableSliceAcceptance.cs`
- `Src/Validation/Game/GameLifecycle/BrotatoLikeGameplayLifecycleValidationScene.cs`
- `DataOS/Authoring/BrotatoLike.seed.sql`

建议验证：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run res://Src/Validation/Game/GameLifecycle/BrotatoLikeGameplayLifecycleValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh --run-dir <new-main-run-dir>
```

### 5.3 Chain Lightning 连线视觉已恢复到 headless artifact 完成状态

现状：

- DataOS 已有：
  - `ability_line_effect('chain_lightning', 'res://Scenes/VFX/LightningLineEffect.tscn')`
  - `Ability.LineEffectScenePath`
- `Scenes/VFX/LightningLineEffect.tscn` 和 `Src/Game/VFX/LightningLineEffect.cs` 已存在，并设置非零 Line2D width/color。
- `BrotatoLikeGameRuntime` 常驻 `GodotProjectileEffectSpawner` 与游戏侧 `BrotatoLikeChainLightningVfxBinder`。
- `BrotatoLikeChainLightningHandler.SpawnLineEffect()` 会通过 `EffectTool.Spawn()` 生成 finite-duration chain effect。
- `BrotatoLikeChainLightningVfxBinder` 从 Runtime effect typed `SourceEntity / TargetEntity` 读取端点，调用 `LightningLineEffect.SetLine(from, to)`，并通过销毁 effect entity 触发通用 spawner 清理节点。
- `BrotatoLikePlayableSliceAcceptance` 已把 `LineEffectScenePath`、source/target ids、start/end world positions、Line2D local/world points、duration 和 cleanup 纳入 structured evidence。

已验证：

- Main run `.ai-temp/scene-tests/runs/2026-05-21/15-43-35/index.json` PASS。
- `scene-acceptance.json(status=pass)` 中 `skill.chain_line_vfx_bound / cleanup / multi_bounce` 全部通过。
- artifact 记录 expected `3`、recorded `3`、bound `3`、cleanup `3`，scene path 为 `res://Scenes/VFX/LightningLineEffect.tscn`，duration 为 `0.2;0.2;0.2`。
- Scene gate 已检查 `index.json`、per-scene `result.json` 和 scene artifact，`expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 均非空；analyzer `gate-report.json` verdict `pass`。

仍可增强：

- 截图像素门禁、闪电材质/动画、淡出和更完整美术表现尚未做；这不再阻断当前链电连线迁移完成。

关键路径：

- `Src/Game/BrotatoLikeAbilityHandlers.cs`
- `Src/Game/VFX/LightningLineEffect.cs`
- `Scenes/VFX/LightningLineEffect.tscn`
- `SlimeAI/GameOS/GodotBridge/GodotProjectileEffectSpawner.cs`（submodule 镜像，只读对照；如需改框架，去 `/home/slime/Code/SlimeAI/SlimeAI`）
- `DataOS/Authoring/BrotatoLike.seed.sql`
- `Src/Game/BrotatoLikePlayableSliceAcceptance.cs`

建议拆分：

- OpenSpec `restore-brotatolike-chain-lightning-line-vfx` 已完成实现与验证，后续应归档到 baseline。
- 如果未来要上提通用端点数据，需要另开框架级 change；不要把 `LightningLineEffect` 类型硬编码进框架。

## 6. P1 接手项：商店、道具、替换面板和 meta progression 还没迁

现状：

- `Src/Game/Progression/BrotatoLikeProgressionService.cs` 当前覆盖：
  - wave runtime state metadata
  - scene-backed pause menu
  - HP recovery
  - experience pickup
  - scene-backed level-up feedback
  - scene-backed experience bar
  - scene-backed level-up choice panel
  - stat reward / hidden ability reward
  - `ModalUi + Suspended` level-up gate
- `BrotatoLikeHud` 左上角已有正式 `ExperienceBarUI.tscn`，`ProgressionSummary` 只保留 metadata 兼容用途。
- 没有正式商店、道具、visible slot 替换、被动面板、永久成长、存档、解锁。

缺什么：

- Level-up choices 后续增强：随机/权重池、连续多次升级队列、visible slot 替换、被动面板和更完整 reward authoring。
- Item system：道具定义、掉落/商店购买、效果应用、图标和叠加规则。
- Shop system：波间商店、刷新、价格、购买、锁定、货币。
- Meta progression：局外存档、解锁、角色/道具池扩展。

关键路径：

- `Src/Game/Progression/BrotatoLikeProgressionService.cs`
- `Src/Game/UI/BrotatoLikeHud.cs`
- `Scenes/UI/PauseMenuUI.tscn`
- `DataOS/Authoring/BrotatoLike.seed.sql`
- `DocsAI/BrotatoMyFeatureMigrationAudit.md`
- `DocsAI/MigrationLedger.md`

旧输入参考：

- `Resources/Else/brotato-my/Data/DataNew/Feature/*`
- `Resources/Else/brotato-my/Data/DataNew/System/*`
- `Resources/Else/brotato-my/Data/Config/System/*`
- `Resources/Else/brotato-my/Src/ECS/UI/Core/*`
- 如果要找 shop/item 旧实现，先用 `rg -n "Shop|Item|Level|Upgrade|Unlock|Save" /home/slime/Code/SlimeAI/Resources/Else/brotato-my`

建议拆分：

- OpenSpec `design-brotatolike-shop-item-loop`
- 后续 progression/panel change：补随机/权重 reward pool、连续多次升级队列、visible slot 替换和被动面板。
- DataOS 表设计要先明确 item/choice/feature modifier 的 authoring shape，避免把临时 UI 数据塞到 runtime meta。

## 7. P1 接手项：完整多波流程还没完成

现状：

- 第一波敌人生成和 wave completion 已有证据。
- `BrotatoLikeEnemySpawnSystem` 能从 DataOS spawn catalog 实例化敌人。
- `BrotatoLikeProgressionService` 能记录 wave runtime state。

缺什么：

- 多波配置、难度曲线、波间奖励、wave start/end UI。
- 敌种组合变化、生成节奏、随机或权重策略。
- 波间 shop / item 奖励接入，以及与升级选择、替换面板的衔接。
- 长时间运行稳定性：节点、Runtime Entity、Timer、Effect、Projectile 是否泄漏。

关键路径：

- `Src/Game/BrotatoLikeEnemySpawnSystem.cs`
- `Src/Game/BrotatoLikeGameRuntime.cs`
- `Src/Game/Progression/BrotatoLikeProgressionService.cs`
- `DataOS/Authoring/BrotatoLike.seed.sql`
- `Src/Validation/Game/Progression/BrotatoLikeProgressionLoopValidationScene.cs`
- `DocsAI/ValidationManifest.json`

建议拆分：

- OpenSpec `complete-brotatolike-wave-run-flow`
- 验证至少覆盖：第 1 波开始、敌人生成、敌人死亡、wave complete、奖励阶段、第 2 波开始、pause gate、失败/死亡复活规则。

## 8. P2 接手项：角色、敌种、UI 主题和真实设备 QA

### 8.1 更多角色和角色选择

现状：

- 当前玩家主要是 `unit.player/deluyi`。
- `assets/Unit/Player/**/*` 中存在更多角色资源的可能性，但未形成正式角色选择和 DataOS authoring 证据。

缺什么：

- 角色数据表、角色选择 UI、起始技能/属性差异。
- 角色资源加载、动画、碰撞尺寸、血条偏移验证。

关键路径：

- `assets/Unit/Player/**/*`
- `DataOS/Authoring/BrotatoLike.seed.sql`
- `Src/Game/BrotatoLikeUnitProfiles.cs`
- `Src/Game/BrotatoLikeGameRuntime.cs`
- `Scenes/Main.tscn`

### 8.2 UI 主题仍是最小实现

现状：

- 已有 scene-backed UI：
  - `Scenes/UI/ActiveSkillBarUI.tscn`
  - `Scenes/UI/ActiveSkillSlotUI.tscn`
  - `Scenes/UI/DamageNumberUI.tscn`
  - `Scenes/UI/HealthBarUI.tscn`
  - `Scenes/UI/PauseMenuUI.tscn`
  - `Scenes/UI/TargetingIndicatorUI.tscn`
- `BrotatoLikeHud` 仍有不少 code-created 结构，例如左上状态面板、progression summary、level-up feedback label。

缺什么：

- 更完整的 scene-backed HUD 布局、图标、经验条、金币/货币、波次提示。
- 技能图标、冷却遮罩、键位/手柄提示、不可释放原因。
- pause menu 焦点导航、设置、返回主菜单等。

关键路径：

- `Src/Game/UI/BrotatoLikeHud.cs`
- `Src/Game/UI/*.cs`
- `Scenes/UI/*.tscn`
- `DocsAI/SceneFirstUXMigration.md`
- `DocsAI/BrotatoMyFeatureMigrationAudit.md`

注意：

- 不建议照搬旧 `UIManager`。应按 scene-first 和当前 GodotBridge 方式补正式场景。
- 改 UI 后必须跑 PlayableUX 和 Main，必要时做截图或 canvas 坐标 artifact。

### 8.3 真实设备 QA 没完成

现状：

- 自动验证主要使用 Godot runner 和 `Input.ActionPress`。
- Input map 覆盖 WASD、方向键、部分手柄 action，但没有实际设备证据。

缺什么：

- 物理手柄：左摇杆、LB/RB、X 或确认键、取消键。
- 鼠标点选：窗口焦点、屏幕坐标到 world 坐标、不同分辨率。
- 键鼠和手柄混用：当前 target session、技能切换、pause 焦点。

关键路径：

- `project.godot`
- `Src/Game/Bridge/BrotatoLikePlayerInputComponent.cs`
- `Src/Game/GodotActiveSkillInputComponent.cs`
- `Src/Game/BrotatoLikeTargetingController.cs`
- `Src/Validation/Game/Input/BrotatoLikeInputEventValidationScene.cs`

建议：

- 建一个人工 QA checklist 文档或专门 validation artifact，不要把真实设备结论写进自动 runner 结果。
- OpenSpec 可命名为 `brotatolike-manual-device-qa`。

## 9. P2 接手项：旧调试和测试工具没有完整迁移

现状：

- 新项目已有统一 runner：
  - `Tools/run-godot-scene.sh`
  - `Tools/analyze-godot-scene-logs.sh`
- 新验证场景在：
  - `Src/Validation/Game/Input`
  - `Src/Validation/Game/UnitComposition`
  - `Src/Validation/Game/PlayableUX`
  - `Src/Validation/Game/Progression`
  - `Src/Validation/Game/GameLifecycle`
  - `Src/Validation/Game/LegacyResources`
- 旧可视调试和测试场景没有逐个迁入。

缺什么：

- 旧 GlobalTest / SingleTest 中仍有价值的调试面板、可视预览、工具测试，需要决定迁移、替换或废弃。
- 如果迁移，应按 Observation/Validation contract 生成标准 artifact，而不是恢复旧临时 UI。

旧输入参考：

- `Resources/Else/brotato-my/Src/ECS/Test/GlobalTest/**/*`
- `Resources/Else/brotato-my/Src/ECS/Test/SingleTest/ECS/**/*`
- `Resources/Else/brotato-my/Src/ECS/Test/SingleTest/Tools/**/*`

关键新路径：

- `DocsAI/ValidationCatalog.md`
- `DocsAI/ValidationManifest.json`
- `Src/Validation/Game/**/*`
- `SlimeAI/DocsAI/Tests/GodotSceneTesting.md`

## 10. 推荐新 OpenSpec changes

按优先级建议这样拆，不要一次性做成巨大 change：

1. `stabilize-brotatolike-release-batch`
   - 目标：重跑 release-batch，修复 Progression/PlayableUX blocker，更新 gate evidence。
2. `validate-brotatolike-dash-main-skill`
   - 目标：玩家真实技能栏触发 Dash，验证位移、冷却、暂停/复活交互。
3. `restore-brotatolike-chain-lightning-line-vfx`（已完成；等待归档）
   - 结果：链电线段端点、生命周期、多段弹跳和清理专项验收已由 Main artifact 覆盖。
4. `expand-brotatolike-skill-loadout`
   - 状态：已完成。默认四槽、12 项 available skill pool、passive ids 和 validation override 均有 PlayableUX / Main artifact 证据。
5. `validate-brotatolike-projectile-and-passive-skills`
   - 状态：已完成。Skills validation `.ai-temp/scene-tests/runs/2026-05-21/17-16-37/index.json` PASS，Main 回归 `.ai-temp/scene-tests/runs/2026-05-21/17-17-42/index.json` PASS。
6. `design-brotatolike-levelup-choice-loop`
   - 状态：已完成并归档到 baseline spec。Progression `.ai-temp/scene-tests/runs/2026-05-21/17-45-01/index.json` PASS，PlayableUX `.ai-temp/scene-tests/runs/2026-05-21/17-45-17/index.json` PASS，Main `.ai-temp/scene-tests/runs/2026-05-21/17-45-33/index.json` PASS。
7. `design-brotatolike-shop-item-loop`
   - 目标：商店、道具、货币、波间购买。
8. `complete-brotatolike-wave-run-flow`
   - 目标：多波、波间奖励、难度曲线、长时间稳定性。
9. `brotatolike-character-selection`
   - 目标：更多角色数据、角色选择 UI、起始技能差异。
10. `brotatolike-manual-device-qa`
    - 目标：真实手柄、鼠标点选、窗口焦点和焦点导航 checklist。

## 11. 常用验证命令

基础构建：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-build.sh
```

主场景：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

核心 gameplay targeted scenes：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run res://Src/Validation/Game/PlayableUX/BrotatoLikePlayableUXValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Src/Validation/Game/GameLifecycle/BrotatoLikeGameplayLifecycleValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Src/Validation/Game/Progression/BrotatoLikeProgressionLoopValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Src/Validation/Game/Input/BrotatoLikeInputEventValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

完整 release batch：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run-all --manifest DocsAI/ValidationManifest.json --release-batch --continue-on-fail --log-dir .ai-temp/scene-tests/runs --errors-only
Tools/analyze-godot-scene-logs.sh --run-dir <new-run-dir> --manifest DocsAI/ValidationManifest.json --gate-report <new-run-dir>/gate-report.json
```

新增或改动验证场景时，必须检查：

- `<run-dir>/index.json`
- `<run-dir>/<scene>/result.json`
- `<run-dir>/<scene>/artifacts/*.json`
- artifact 五字段：`expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath`

## 12. 接手建议

新对话不要从 `Main.cs` 大改开始。建议先选一个很窄的 OpenSpec change，读对应文档和代码，补一个能失败的 targeted validation，再实现。当前最合适的第一刀是 release-batch 稳定化或 Dash 玩家输入专项，因为它们范围小、已有代码基础、能快速恢复迁移信心。
