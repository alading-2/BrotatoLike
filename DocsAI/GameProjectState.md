# BrotatoLike GameProjectState

> 更新日期：2026-05-21（character selection）

## 当前状态

框架接入基线已创建。框架仓库已有 `SlimeAI.GameOS` Runtime 最小内核、typed Runtime Data contract、DataOS SQLite schema / migration / generator / validator / typed Runtime snapshot loader、GodotBridge 第一版，以及 Movement / Collision / Damage / Ability / Projectile / Effect / Feature / AI / Attack 第一批能力。

本轮追加：

- **Character selection**：OpenSpec change `brotatolike-character-selection` 已把 BrotatoLike 从固定默认玩家推进到首版 DataOS-backed 角色选择闭环。游戏侧 DataOS seed 新增 `character_definition / character_loadout / character_loadout_ability`，`Tools/run-dataos-snapshot.sh` 生成 `DataOS/Snapshots/character_authoring.json`；首批可选角色为 `deluyi`（默认 fallback，HP 100 / MoveSpeed 200 / Attack 10，loadout `slam,chain_lightning,target_point_skill,dash`）和 `guangfa`（HP 85 / MoveSpeed 185 / Attack 14，loadout `chain_lightning,sine_wave_shot,target_point_skill,dash`）。`BrotatoLikeCharacterCatalog` 校验 player record、visual scene 和 ability refs，`BrotatoLikeGameRuntime` 新增 `InitialCharacterId`、`TrySelectCharacter`、`SpawnCharacter`、`SpawnSelectedCharacter`，Main 启动路径改为默认角色选择 fallback 而不是直接固定 `SpawnPlayer()`；旧 `SpawnPlayer(recordId)` 保留为兼容和专项验证入口。新增 `Scenes/UI/CharacterSelectPanelUI.tscn` / `CharacterSelectCardUI.tscn` scene-backed UI。验证：`Tools/run-build.sh` PASS（DataOS validation PASS，85 个 XML comment warnings，0 errors）；CharacterSelection validation `.ai-temp/scene-tests/runs/2026-05-21/20-00-19/index.json` PASS，artifact 记录 invalid player/visual reject、UI scene path、visible ids `deluyi,guangfa`、按钮选择 `guangfa`、`player-guangfa`、visual path、starting skills、视觉/属性/loadout 差异，以及两名角色 input、active skill input、HUD、PlayerHealthBar、ActiveSkillBar、camera 绑定；analyzer gate report 为 `pass`，README 与 artifact 五字段非空。Main 回归 `.ai-temp/scene-tests/runs/2026-05-21/20-03-04/index.json` PASS，artifact 记录默认角色 fallback 的 `skill_loadout_source=character:deluyi`。
- **Wave run flow**：OpenSpec change `complete-brotatolike-wave-run-flow` 已把 BrotatoLike 从单波 completion 推进到首版多波运行闭环。游戏侧 DataOS seed 新增 `wave_definition` / `wave_enemy_entry`，`Tools/run-dataos-snapshot.sh` 生成 `DataOS/Snapshots/wave_authoring.json`；当前 authoring 包含两波 finite deterministic enemy entries，第 1 波为 2 个 `chailangren` + 3 个 `yuren`，完成后进入 `RewardShop` 并记录 `shop_offer.validation` / `validation` hook，第 2 波调整生成顺序和数量后结束。`BrotatoLikeWaveCatalog` 校验 wave id、completion mode、enemy id 和视觉资源；`BrotatoLikeGameRuntime.TryStartWave/TryStartNextWave` 可切换当前波次 spawn catalog；`BrotatoLikeProgressionService` 记录 `Preparing/Running/Completed/RewardShop/NextWave/Ended` 状态机、完成判定、reward hook、下一波和 cleanup count；`ExperienceBarUI` / `ProgressionSummary` 记录 wave index 与 phase。验证：`Tools/run-build.sh` PASS（DataOS validation PASS，49 个既有 XML comment warnings，0 errors）；RunFlow validation `.ai-temp/scene-tests/runs/2026-05-21/19-17-09/index.json` PASS，artifact 记录 wave authoring source、两波 expected spawn count=5、invalid enemy/resource reject、第一波 `Running` 生成 5 个敌人、pause tick 阻断/恢复、死亡复活、`Completed -> RewardShop`、cleanup `runtime 10 -> 5` / enemy `5 -> 0`、第二波 `Running` 生成 5 个敌人、scene-backed `ExperienceBarUI` wave phase；analyzer gate report 为 `pass`，README 与 artifact 五字段非空。Progression 回归 `.ai-temp/scene-tests/runs/2026-05-21/19-18-08/index.json` PASS，Main 回归 `.ai-temp/scene-tests/runs/2026-05-21/19-18-22/index.json` PASS。
- **Shop item loop**：OpenSpec change `design-brotatolike-shop-item-loop` 已新增 BrotatoLike 第一版商店、道具、货币和购买闭环。游戏侧 DataOS seed 新增 `item_definition` / `shop_offer`，`Tools/run-dataos-snapshot.sh` 生成 `DataOS/Snapshots/shop_item_authoring.json`；首批 deterministic 道具为 `vital_seed`（`Damage.MaxHp +8`，price 12）、`swift_boots`（`Movement.MoveSpeed +18`，price 8）、`sharpening_stone`（`Attack.Damage +5`，price 20）。`BrotatoLikeShopService` 暴露货币、deterministic offer、购买门禁、owned item metadata 和 Runtime Data effect application；`ShopPanelUI.tscn` / `ShopOfferCardUI.tscn` 是 scene-backed UI。验证：`Tools/run-build.sh` PASS（DataOS validation PASS，0 warnings，0 errors）；Shop validation `.ai-temp/scene-tests/runs/2026-05-21/18-39-01/index.json` PASS，artifact 记录 unknown effect target reject、`offer_ids=vital_seed,swift_boots,sharpening_stone`、`offer_prices=12,8,20`、货币 `15 -> 3`、`Damage.MaxHp` 增加、`insufficient_currency` reject、UI purchased/currency/close state；analyzer gate report 为 `pass`，scene artifact 五字段非空。
- **Level-up choice loop**：OpenSpec change `design-brotatolike-levelup-choice-loop` 已把经验条、升级反馈和升级三选一从 debug/metadata 证据推进到 scene-backed 首版局内成长闭环。`BrotatoLikeProgressionService` 在经验 pickup 跨过阈值后打开 `LevelUpChoicePanelUI.tscn`，并通过 `BrotatoLikeGameRuntime.OpenLevelUpChoiceGate()` 进入 `ModalUi + Suspended` 门禁；三项确定性选择为 `max_hp_plus_10`、`move_speed_plus_20`、`unlock_sine_wave_shot`，其中 HP 奖励写入 `Damage.MaxHp` 并治疗等量 HP，技能奖励把 `sine_wave_shot` 加入 hidden owned ability 供后续替换/面板流程使用。正式经验条由 `ExperienceBarUI.tscn` 承载，`ProgressionSummary` 继续作为 metadata 兼容节点但不再是玩家可见完成证据。验证：`Tools/run-build.sh` PASS（DataOS validation PASS，26 个既有 XML comment warnings，0 errors）；Progression `.ai-temp/scene-tests/runs/2026-05-21/17-45-01/index.json` PASS，artifact 记录 choice ids/effect types、`choice_panel_scene_path=res://Scenes/UI/LevelUpChoicePanelUI.tscn`、`experience_bar_scene_path=res://Scenes/UI/ExperienceBarUI.tscn`、`max_hp_before_choice=100 -> max_hp_after_stat_choice=110`、owned ability count `4 -> 5`、`sine_wave_ability_owned=true`、gate `ModalUi/Suspended -> None/Running`；PlayableUX `.ai-temp/scene-tests/runs/2026-05-21/17-45-17/index.json` PASS；Main `.ai-temp/scene-tests/runs/2026-05-21/17-45-33/index.json` PASS；三个 analyzer gate report 均为 `pass`，scene artifact 五字段非空。
- **Skill loadout expansion**：OpenSpec change `expand-brotatolike-skill-loadout` 已把默认四槽、可获得技能池和 deterministic validation loadout 从散落生成逻辑收束到游戏侧 `BrotatoLikeSkillLoadoutAuthoring`。默认 visible active slots 仍为 `slam / chain_lightning / target_point_skill / dash`；available skill pool 显式包含 `slam / chain_lightning / target_point_skill / dash / sine_wave_shot / boomerang_throw / bezier_shot / parabola_shot / arc_shot / orbit_skill / circle_damage / aura_shield`；passive ids 为 `orbit_skill / circle_damage / aura_shield`。`GodotActiveSkillInputComponent` 只在 visible active slots 内切换/触发，`ActiveSkillBarUI` 和 Main/PlayableUX artifacts 记录 loadout source、owned ids、visible slot ids、selected id、total/visible/hidden count。验证：`Tools/run-build.sh` PASS（26 个既有 XML comment warnings，0 errors）；PlayableUX `.ai-temp/scene-tests/runs/2026-05-21/16-01-37/index.json` PASS，validation override 记录 12 owned / 4 visible / 8 hidden；Main `.ai-temp/scene-tests/runs/2026-05-21/16-05-14/index.json` PASS，analyzer `gate-report.json` verdict `pass`；scene gate 五字段非空。
- **Projectile/passive skill validation**：OpenSpec change `validate-brotatolike-projectile-and-passive-skills` 已把非默认 projectile/passive 技能从 handler/DataOS 证据推进到 scene-backed 逐技能验收。`BrotatoLikeProjectileAbilityHandler` 现在接入 runtime 共享 `MovementSystem`，触发后的投射物可由 `GodotMovementDriver` tick、命中时走 `DamageTool`、并在 movement stop 后销毁 runtime/visual entity。新增 `res://Src/Validation/Game/Skills/BrotatoLikeSkillValidation.tscn`，复用 `ValidationAllSkillAbilityIds`，逐 ability id 验证 `sine_wave_shot / boomerang_throw / bezier_shot / parabola_shot / arc_shot / orbit_skill / circle_damage / aura_shield` 的 scene path、movement mode、轨迹/跟随、hit/damage 和 cleanup。专项 evidence 为 `.ai-temp/scene-tests/runs/2026-05-21/17-16-37/index.json`，artifact `brotatolike-skill-validation.json` 为 `status=pass`、`failureReasons=[]`，gate report verdict `pass` 且 README/artifact 五字段非空；Main 回归 `.ai-temp/scene-tests/runs/2026-05-21/17-17-42/index.json` PASS。
- **Release-batch stability**：OpenSpec change `stabilize-brotatolike-release-batch` 已复跑 BrotatoLike manifest release-batch 并解除历史 PlayableUX / Progression blocker。`Tools/run-build.sh` 通过（DataOS validation PASS，26 个既有 XML comment warnings，0 errors）；完整 release-batch `.ai-temp/scene-tests/runs/2026-05-21/14-57-55/index.json` 为 25/25 passed，analyzer 生成 `.ai-temp/scene-tests/runs/2026-05-21/14-57-55/gate-report.json`，verdict `pass`、requested 25、passed 25、failed 0、missing 0。PlayableUX 和 Progression 的 targeted run 分别为 `.ai-temp/scene-tests/runs/2026-05-21/14-57-13/index.json`、`.ai-temp/scene-tests/runs/2026-05-21/14-55-15/index.json`，完整 batch 中对应 artifact 也为 `status=pass` 且标准答案五字段非空。
- **Dash main skill validation**：OpenSpec change `validate-brotatolike-dash-main-skill` 已把 Dash 从 handler/smoke 证据推进到玩家技能栏主路径验收。`BrotatoLikeGameRuntime.Initialize()` 现在用共享 `GodotMovementDriver.MovementSystem` 注册 Dash handler，避免 input 触发 Dash 后只改 Runtime movement、不同步 Godot player position；`BrotatoLikeGameplayLifecycleValidation` 新增 `dash_input_skill_bar_path` check，通过 `NextSkill` 选中 `ability-dash-player-deluyi` 后用 `UseSkill` 触发，artifact 记录 selected skill id/index、释放前后位置、Dash 距离、冷却和 scene-backed skill bar。最新 GameLifecycle evidence 为 `.ai-temp/scene-tests/runs/2026-05-21/15-14-41/index.json`，Main 回归 evidence 为 `.ai-temp/scene-tests/runs/2026-05-21/15-17-40/index.json`，两者 `index.json`、`result.json` 和 scene artifact 均通过，标准答案五字段非空。
- **Chain Lightning line VFX validation**：OpenSpec change `restore-brotatolike-chain-lightning-line-vfx` 已把链电连线从 `LineEffectScenePath` 路径证据推进到 scene-backed 端点绑定验收。`BrotatoLikeGameRuntime` 现在常驻 `GodotProjectileEffectSpawner` 与游戏侧 `BrotatoLikeChainLightningVfxBinder`；binder 只识别 `res://Scenes/VFX/LightningLineEffect.tscn`，从 Runtime effect 的 typed `SourceEntity / TargetEntity` 读取端点，调用 `LightningLineEffect.SetLine(from, to)`，并按有限 `Effect.Duration=0.2` 通过 `EntityManager.Destroy(effect)` 触发通用 spawner 清理。Main evidence 为 `.ai-temp/scene-tests/runs/2026-05-21/15-43-35/index.json`，`scene-acceptance.json` 记录 3 段链电线段、3 段 bound、3 段 cleanup、source/target ids、start/end world positions、Line2D local/world points 和 duration；analyzer `gate-report.json` verdict `pass`，标准答案五字段非空。
- **Lifecycle regression fix**：OpenSpec change `fix-brotatolike-lifecycle-regressions` 已修复 Main 运行中暴露的三条生命周期回归。复活路径通过框架 `GodotBridgeContext.DestroyEntity()` 同步注销 Runtime Entity、node registry 和 adapter registry 后再 `QueueFree()`，同 EntityId 新玩家可重新绑定 `BrotatoLikePlayerInputComponent` 与 `GodotActiveSkillInputComponent`；AI target selector 会过滤无有效 team / HP evidence 的非战斗实体，避免敌人攻击 ability-like entity；HUD 头顶血条和伤害/治疗飘字改由 `BrotatoLikeHud` 基于当前 viewport canvas transform 统一做 world-to-canvas 映射。Targeted 验证见“最新验证”。
- **SystemAgent integrated validation governance**：OpenSpec change `systemagent-integrated-validation-governance` 将 BrotatoLike Godot 验证纳入 manifest / batch runner / analyzer / scene-gate 证据闭环。`DocsAI/ValidationManifest.json` 是 release-batch 权威选择源，当前包含 26 个 `releaseBatch=true` 场景；`Tools/run-godot-scene.sh run-all --manifest DocsAI/ValidationManifest.json --release-batch` 会写入结构化 `index.json`，analyzer 会写入 `gate-report.json` 并检查 README 五字段、`index.json`、per-scene `result.json`、scene artifact 五字段、manifest checks、catalog 和 freshness。历史 release-batch `2026-05-21/10-13-37` 曾被 PlayableUX / Progression 两个 feature-slice artifact 失败阻断；`2026-05-21/14-57-55` 覆盖当时 25 个 manifest scene 并通过；Shop scene 加入 manifest 后，`2026-05-21/18-39-01` 完整 release-batch 覆盖 26 个 scene 并通过。
- **DataOS table-first authoring**：OpenSpec change `refactor-dataos-table-authoring` 已把 BrotatoLike seed 从手写 `data_field` 业务行迁到清晰业务表：`unit_player / unit_enemy / unit_targeting_indicator / ability / ability_effect / ability_projectile / ability_movement_* / feature_definition / feature_modifier / system_config / system_preset / spawn_config`。`runtime_snapshot.json` shape 保持 `manifest / descriptors / records / resources`，unit / ability 的 `table/id/field/type/value` 归一化对比无差异（16 条记录、497 个字段行一致）；`resource_entry` 收敛为 ResourceCatalog lookup / legacy 分类面，content-owned effect/projectile/unit visual 路径改由业务表持有，snapshot resources 从 27 收敛到 18。
- **restore-brotatolike-playable-ux**：OpenSpec change `restore-brotatolike-playable-ux` 已把审计中 P0/P1 体验缺口按 AI-first 游戏侧实现补齐到可验证状态。普通 `Scenes/Main.tscn` 现在由正式 `BrotatoLikeHUD` 暴露玩家 HP、当前技能、四槽技能栏、头顶血条、伤害/治疗飘字和 progression summary；玩家默认技能扩展为 `slam / chain_lightning / target_point_skill / dash`；`GodotActiveSkillInputComponent` 通过真实 Godot input action 触发技能，Point 目标技能进入游戏侧 `BrotatoLikeTargetingController`，确认后才调用 `AbilityService.TryTrigger`，取消/死亡会清理指示器和会话；游戏侧 `BrotatoLikeProgressionService` 记录 wave runtime state、暂停菜单、pause schedule gate、HP recovery、dead skip、经验拾取、经验/等级和 level-up 反馈；legacy `resources[]` 路径新增 `legacyStatus` 分类门禁，25 个旧 `res://Src/...` / `res://Data/...` 路径已通过分类验证。验证证据见“最新验证”。
- **typed `EntityId` 同步（P2a）**：框架仓 OpenSpec change `refactor-runtime-entity-id-typed-value` 把 Runtime Entity 引用从 raw `string` 升级为 `readonly record struct EntityId`，所有 IEntity / RuntimeEntity / EntityManager / EntitySpawnConfig / Capability DataKey / Event payload / GodotBridge adapter 已 typed 化。BrotatoLike submodule 工作树已 rsync 同步框架最新 GameOS / Tests / SceneTests，游戏侧 `Src/Game/*.cs` 已 typed 适配（`new EntityId("...")` 字面量、`.Value` 适配 string-based registry / Relationship 调用、`HashSet<string>` 改 `HashSet<EntityId>`、`DataKey<IEntity?>` 改 `DataKey<EntityId?>`）。BrotatoLike `Tools/run-build.sh` 0 errors，`Tools/run-godot-scene.sh run-main-smoke` PASS（`BrotatoLike GameOS smoke PASS`，artifact 写到 `.ai-temp/scene-tests/runs/2026-05-15/06-34-59/`）。submodule 指针未 commit / push，仅工作树同步（默认开发期策略）。
- **Runtime LifecycleTree 迁移（P1）**：框架仓 commit `b73b54f` 已移除旧 `RelationshipManager / RelationshipType / RelationshipRecord`，改用 `LifecycleTree / LifecycleLink` 表达生命周期父子树，用 `EntityIdList` typed DataKey 表达 Ability / Projectile / Effect 等业务引用，并通过 `RuntimeOwnedReferenceRegistry` 清理 owner 列表。BrotatoLike commit `b3ce009` 已同步 submodule 指针到 `b73b54f`，游戏侧 `Main.cs`、`GameBootstrap.cs`、`GodotActiveSkillInputComponent.cs`、`BrotatoLikePlayableSliceAcceptance.cs` 和 runtime glue 已迁到 `LifecycleTree.IsAttached`、`GodotNodeRegistry.IsAdapterRegistered`、`EntityIdList` 与 typed DataKey。最新验证见本文件“最新验证”。
- **RuntimeWorld facade 同步（P2b）**：框架仓 OpenSpec change `refactor-runtime-world-facade` 已 archived，新增 `RuntimeWorld.Default` 和 `RuntimeWorld.CreateScoped()`，将 Entity / Lifecycle / Events / Resources / Pools 状态收束到 world-scoped subsystem；`EntityManager / LifecycleTree / WorldEvents.World / ResourceCatalog / ObjectPoolManager` 仍保留 static facade 并转发到 `Default`，BrotatoLike 主流程无需强制改造。BrotatoLike `SlimeAI/` submodule 工作树已同步框架 GameOS / Tests / DocsAI 改动；游戏侧代码不新增依赖注入，仅继续通过既有 static API 访问默认 world。P2b 同步验证已通过，artifact 见“最新验证”。
- **Runtime events leakage cleanup（P3）**：框架仓 OpenSpec change `refactor-runtime-events-purge-game-leakage` 已把 BrotatoLike 主动技能输入事件迁到游戏侧。新增 `Src/Game/Event/BrotatoLikeInputEvents.cs`（`InputUseSkill / InputPreviousSkill / InputNextSkill`）和 `Src/Game/Bridge/BrotatoLikePlayerInputComponent.cs`；`GodotActiveSkillInputComponent`、`BrotatoLikeGameRuntime`、`BrotatoLikePlayableSliceAcceptance` 与 `Main` smoke 已切到 game-side namespace。框架 Bucket A 旧事件 `MouseSelection* / Wave* / GameStart / GameOver / GamePause / GameResume` 已删除，未创建游戏侧替换。
- **Runtime CommandBuffer + Phase playback（P4）**：BrotatoLike `SlimeAI/` submodule 工作树已同步框架 P4 GameOS / DocsAI / Tests 改动（未 commit / push）。`BrotatoLikeGameRuntime._Process(delta)` 现在在现有 `TickSpawn(delta)` 前后显式调用 `RuntimeWorld.Default.Schedule.RunPhase(SchedulePhase.BeginTick / BeforeSystemTick / AfterSystemTick / AfterEventDispatch / EndOfFrame)`；私有 `RuntimeSchedule` 仍只负责 SpawnSystem 同步门禁，phase playback 不 tick capability service。Phase 2 inventory 未发现 `c-explicit-wait`，BrotatoLike explicit smoke / DataOS factory / startup publish 调用继续保持 guard 外同步语义。
- **typed Data / DataOS snapshot contract**：BrotatoLike seed 已补 `capability_manifest` 和 `data_key_descriptor`，`DataOS/Snapshots/runtime_snapshot.json` 现在内嵌 `manifest / descriptors / records / resources`；`Tools/run-dataos-snapshot.sh` 使用 `DATAOS_PROFILE=brotatolike` 和 `DATAOS_CATALOG_ID=brotatolike` 生成 profile snapshot。
- **active catalog + typed loader**：`BrotatoLikeDataOSBootstrap` 从 snapshot manifest/descriptors 构建 active `DataCatalog`，通过框架 `RuntimeDataSnapshot` resolve stable key 到 `DataKey<T>` 后 typed apply；`EntitySpawnConfig.DataCatalog` 会把 catalog 传入 Runtime Entity。loader 会把 wrong type、unknown key、descriptor missing/extra、type/default drift 作为错误，不再静默回退 runtime default。
- **typed runtime migration**：游戏侧 Ability / Feature handler、SpawnSystem、runtime bootstrap 和 smoke 断言已从旧 string/DataMeta access 迁到 typed `DataKey<T>` 读写。`GameBootstrap` 的 smoke local key 已改为 `DataKey<int>`，BrotatoLike build 通过 `SlimeAIGameOSProject` 指向工作区主框架仓，避免编译只读 submodule 旧源码。
- **Runtime/Data 专项场景**：框架侧新增 `res://SlimeAI/Src/Validation/Runtime/Data/RuntimeDataValidation.tscn`，BrotatoLike runner 可作为承载工程运行；该场景覆盖 typed `DataKey<T>` lifecycle、`DataCatalog` resolve、modifier/computed dirty、category reset 和 Data-to-Event bridge artifact。
- **submodule 承载策略**：BrotatoLike 的 `SlimeAI/` 是框架仓 git submodule 镜像。当前初始开发阶段，BrotatoLike 作为默认承载游戏，框架侧验证场景可直接同步到该工作树以跑通 Godot；后续多游戏 / 成品阶段不默认同步所有游戏，改按每个游戏的框架版本策略更新 submodule 指针。
- **drift evidence**：typed loader / validator 曾捕获 `Movement.OrbitTotalAngle` descriptor default mirror 与 C# runtime default 不一致，修正 seed 中 default mirror 为 `-1` 后通过验证。
- **R07 可玩切片验收**：普通 `Scenes/Main.tscn` 在 scene runner 的 artifact 环境下会执行 `BrotatoLikePlayableSliceAcceptance`，与 `--gameos-smoke-exit` smoke 路径分离；输出 `BrotatoLike playable slice PASS/FAIL`，并写入 `artifacts/scene-acceptance.json`。当前验收覆盖玩家生成、WASD + 方向键 input map、`Movement.InputDirection` / `Movement.LastMoveDirection`、玩家位移和摄像机/视口可见性、第 1 波敌人生成、敌人追逐移动、接触伤害、敌人死亡和 cleanup、`slam` / `chain_lightning` / `target_point_skill` 真实 input action 触发、Dash 在 GameLifecycle 中通过技能栏 input path 触发并位移、点选确认、冷却门禁、命中、正式 HUD、玩家 HP Label、四槽技能栏、头顶血条、伤害数字、progression summary 和结构化 damage logs。`PlayableSliceHUD` 测试专用 Label 不再作为完成证据。
- **单位组合 profile 迁移**：玩家和近战敌人生成改为 DataOS 写入后调用框架 `GodotUnitComposer`，由 `BrotatoLikeUnitProfiles.Player / EnemyMelee` 选择 visual、animation、orientation、AI、attack、hurtbox 和 contact damage adapter；游戏侧仍只挂 `BrotatoLikePlayerInputComponent` 与 `GodotActiveSkillInputComponent`。`BrotatoLikeEnemySpawnSystem` 使用共享 `GodotMovementDriver` 并启动 `MoveMode.AIControlled`，验证不再手写 `Movement.AIMoveDirection`。
- **统一 Observation / runner**：`Tools/run-godot-scene.sh` 现在委托 `.codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs`，新日志结构固定为 `index.json + 001_<scene>_attempt1/{stdout,stderr,combined,result,artifacts}`；`BrotatoLikePlayableSliceAcceptance` 写入小写 `status=pass/fail` 和 `artifacts/logs/scene-log.jsonl`，`Main.cs` 使用 `GameOSLog.For("BrotatoLike.Main")` 输出流程日志；`Src/Validation/GameOS/Observation/ObservationLogValidation.tscn` 独立验证通用 log level、格式化、过滤、JSONL sink 和 runner session 路径。
- **EventBus observation dump**：`--gameos-smoke-exit` smoke 路径会在 runner artifact 环境下导出 `artifacts/eventbus-dump.json`；最新 `.ai-temp/scene-tests/runs/2026-05-13/09-23-37/.../eventbus-dump.json` 中 `SameTypeReentryBlockedCounts={}`、`HandlerExceptions=[]`，用于确认 BrotatoLike smoke 没有事件重入阻断或 handler 异常。
- **Scene artifact gate**：普通 `Scenes/Main.tscn` 的 `scene-acceptance.json` 和 `--gameos-smoke-exit` 的 `scene-smoke.json` 均输出标准答案字段：`expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath`。`run-main-smoke` 继续保留 `eventbus-dump.json`。
- **迁移台账**：新增 `DocsAI/MigrationLedger.md`，按旧 `Resources/Else/brotato-my` 主场景、Entity、Component、System、UI、Ability、DataNew、Config、ResourcePaths 和 Test 输入建立第一版映射；该台账用于审计和后续 R07 可玩切片追踪，明确 `DataOS-only` 与 `遗留引用` 不等于资源可加载或玩法完成。
- **Movement Acceleration 平滑移动**：框架 `MovementDataKeys.Acceleration` + `InputDrivenMovement` Lerp 平滑支持；DataOS `unit.player/deluyi` 已写入 `Movement.Acceleration = 12`；backward-compatible（无 Acceleration 时退化为直接速度）。
- **BrotatoLikePlayerInputComponent**：游戏侧 Bridge 新增输入桥接组件，每帧 `_Process` 读取 Godot Input Map（MoveLeft/Right/Up/Down + UseSkill/PreviousSkill/NextSkill），写入 `MovementDataKeys.InputDirection`，并发布 `BrotatoLike.Game.Events.InputUseSkill / InputPreviousSkill / InputNextSkill`；支持 `CanMoveInput` 门控和 AI 共存；已定义 BrotatoLike `project.godot` 输入映射（WASD + 方向键 + 手柄左摇杆）。
- **BrotatoLikeInputEventValidation**：新增游戏侧专项验证场景 `res://Src/Validation/Game/Input/BrotatoLikeInputEventValidation.tscn`，脚本位于 `Src/Validation/Game/Input/BrotatoLikeInputEventValidationScene.cs`，artifact 为 `artifacts/brotatolike-input-event-validation.json`。该场景验证 `BrotatoLikePlayerInputComponent` 写入 `MovementDataKeys.InputDirection`、技能输入事件类型归属 `BrotatoLike.Game.Events`，以及 `GodotActiveSkillInputComponent` 可由 `InputNextSkill / InputPreviousSkill / InputUseSkill` 切换并触发当前技能。该场景归属 BrotatoLike，不上提为框架 Runtime 场景。
- **BrotatoLikeGameRuntime 玩家生成**：`SpawnSelectedCharacter()` / `SpawnCharacter(characterId)` 通过 `BrotatoLikeCharacterCatalog` 选择 DataOS `unit.player/*` record 和角色起始 loadout；`SpawnPlayer(recordId, spawnPosition)` 仍保留给旧验证和兼容调用。当前 `deluyi` 与 `guangfa` 都从 DataOS 读取数据，创建 `GodotEntity2D`，挂载 `BrotatoLikePlayerInputComponent` / `GodotActiveSkillInputComponent`，加载各自 visual scene，启动 `MoveMode.PlayerInput` 常驻移动，共享 `GodotMovementDriver`。
- **Main.tscn 自动创建玩家**：`StartGameRuntime()` 初始化后自动调用 `runtime.SpawnSelectedCharacter()`，未显式选择时使用 `InitialCharacterId=deluyi` 默认 fallback，发布 `Game.Started` 事件时玩家已就位。
- Smoke 新增 `BrotatoLikePlayerInputProbe`：覆盖组件注册、InputDirection 写入、Acceleration > 0 平滑加速（0.05s 时 ~45px/s，0.55s 时 ~100px/s）、Acceleration = 0 直接速度（瞬时 80px/s）。

当前 smoke probe 覆盖：

- Runtime Entity + Data -> Entity.Events 变更事件。
- Runtime Relationship 父子归属关系。
- Runtime Schedule 项目状态门禁。
- Runtime Pool 预热和释放。
- Runtime Timer Tick。
- Runtime ResourceCatalog 路径映射。
- DataOS `DataOS/Authoring/BrotatoLike.seed.sql` 生成 `DataOS/Snapshots/runtime_snapshot.json`；Godot smoke 通过 `BrotatoLikeDataOSBootstrap` 读取 typed snapshot，生成 `unit.enemy/yuren`、`unit.targeting_indicator/default`、`ability/chain_lightning`、`ability/parabola_shot`、`ability/sine_wave_shot`、`ability/boomerang_throw`、`ability/orbit_skill`、`ability/bezier_shot`、`ability/arc_shot`、`ability/circle_damage`、`system.config/SpawnSystem`、`system.preset/Default` 和 `spawn.config/default` Runtime Entity，断言 descriptor/manifest 进入 snapshot、旧 AbilityData 通用字段、链式技能参数、自动索敌参数、持续伤害参数、投射物速度 / 命中 / 生命周期 / 伤害参数、特效名称 / 持续时间参数，以及 SineWave / Orbit / Boomerang / Bezier / CircularArc 的 Movement handler authoring 参数，构建第 1 波敌人生成规则 catalog，并注册 `ResourceCatalog` 资源映射。
- `unit.player/deluyi` 与 `unit.player/guangfa` 已入 DataOS seed，并通过 `character_definition` 形成 selectable character authoring；`deluyi` 保留默认 fallback，`guangfa` 提供不同 visual、HP/MoveSpeed/Attack 和起始技能证据。
- `BrotatoLikeDataOSBootstrap.BuildSystemScheduleConfig()` 已能从 DataOS `system.config/SpawnSystem` 生成 `RuntimeSchedule` 配置，解析 Group / Tags / Priority / FlowState / Overlay / SimulationState / Dependencies。
- `BrotatoLikeEnemySpawnSystem` 已消费 DataOS Spawn catalog，显式 `Tick()` 可按规则实例化 `GodotEntity2D` 敌人包装节点、加载 `Unit.VisualScenePath` 视觉场景、写入 DataOS 敌人字段和 `Movement.Position`；`BrotatoLikeScheduledEnemySpawnSystem` 已通过 `RuntimeSchedule.Execute` 驱动 Tick，headless smoke 覆盖 Boot 状态阻断、Gameplay 状态生成第 1 波 2 个 `chailangren` 与 3 个 `yuren`、Pause 状态阻断。
- `BrotatoLikeGameRuntime` 已提取主运行时节点，统一封装 DataOS 初始化、资源注册、Spawn catalog、`RuntimeSchedule` 注册、玩家生成、Gameplay / Pause 状态切换和 `_Process` 自动 Tick；当前 smoke 通过该节点验证，后续真实主场景可直接挂载或由 Main 创建。
- `Scenes/Main.tscn` 已挂载 `GameRuntime` 子节点并补回旧主场景 `Camera2D`；普通运行路径由 `Main.StartGameRuntime()` 初始化该节点并生成玩家，发布 `BrotatoLikeGameEventType.Game.Started` 并输出初始化日志；`--gameos-smoke-exit` 路径仍保持独立，smoke 只在显式探针中调用正式入口验证事件与场景挂载。
- Collision `CollisionSystem / CollisionLayers / CollisionDataKeys / GameEventType.Collision` 纯 Runtime 事件。
- MovementCollision 纯 Runtime 线段 / 圆形扫描、碰撞停止、GodotMovementDriver Node2D 接触点同步，以及 `GodotAreaEntity2D + CollisionShape2D` Physics broadphase 候选验证。
- Godot Collision bridge `GodotAreaEntity2D / GodotCollisionComponent / GodotHurtboxComponent` 手动发射进入/离开事件 smoke。
- Movement `MovementSystem` 和旧 `MoveMode` 纯 Runtime 轨迹推进。
- **Movement Acceleration 平滑**：`InputDrivenMovement` 在 `Acceleration > 0` 时使用 Lerp 帧率无关平滑；`Acceleration = 0` 时退化为直接速度；headless smoke 覆盖 12f 加速度下 0.05s ~45px/s、0.55s ~100px/s 的渐进加速。
- GodotMovementDriver + GodotEntity2D 真实 `Node2D.Position` 同步，headless smoke 覆盖 Charge / Orbit / SineWave / BezierCurve / Boomerang / AttachToHost / PlayerInput / AIControlled / Parabola / CircularArc。
- GodotOrientationComponent + GodotEntity2D root `RotationDegrees` 输出，headless smoke 覆盖 FollowMovement 和 `MovementParams.Orientation` SpinOnly。
- DamageService 处理器管线 + GodotContactDamageComponent，headless smoke 覆盖 Hurtbox 进入造成接触伤害和同队过滤。
- FeatureService 最小生命周期由框架 Runtime tests 覆盖，包含 Modifier 授予 / 回滚、handler 生命周期和 AbilityService 调用 Feature handler；尚未接入本游戏 Godot smoke。
- AbilityService 点选目标语义和 Periodic 自动触发 Tick 由框架 Runtime tests 覆盖；`AbilityTargetingTool` 自动索敌第一段由框架 Runtime tests 覆盖同队 / 死亡 / 范围过滤和 AI 自动施法上下文准备；本游戏 Godot smoke 已覆盖 Point 目标正式触发和 Ability 自动索敌命中。
- `BrotatoLikeAbilityHandlers` 已注册 `技能.主动.猛击 / 位置目标 / 连锁闪电`、`技能.投射物.正弦波射击 / 回旋镖投掷 / 贝塞尔射击 / 定点抛炸弹 / 圆弧射击`、`技能.位移.冲刺` 和 `技能.被动.环绕技能 / 圆环伤害 / 光环护盾` 游戏侧 Feature handler；headless smoke 已覆盖 `AbilityService -> FeatureHandler -> ProjectileTool -> MovementSystem` 真实执行路径，从 DataOS `sine_wave_shot / boomerang_throw / bezier_shot / parabola_shot / arc_shot / orbit_skill / aura_shield` Runtime Data 生成投射物并启动 SineWave / Boomerang / BezierCurve / CircularArc / Orbit / AttachToHost Movement；`dash` 已从 DataOS 读取 Charge Movement 参数、CastRange 和 Effect.*，通过 `MovementSystem + EffectTool` 启动施法者冲刺并生成 Effect Runtime 事件；同时覆盖 `chain_lightning` 从 DataOS 链式参数读取目标、伤害、弹跳次数、范围、延迟和衰减，并通过 `DamageTool + TimerManager` 延迟命中 3 个敌方 Runtime Entity；`slam / target_point_skill / circle_damage` 已从 DataOS 读取 EffectRadius / Damage / DamageInterval / DamageRepeatCount / ApplyImmediateDamage / Effect.*，通过 `DamageTool + EffectTool` 对范围内敌人造成伤害、过滤同队和范围外目标，并生成真实 Effect Runtime 事件。
- ProjectileTool / EffectTool 纯 Runtime 生成由框架 Runtime tests 覆盖，包含 Data 写入、关系绑定、事件发布和 Effect 动画名写入；`ProjectileTool.StartMovement` 命中生命周期由框架 Runtime tests 覆盖，包含 MovementCollision、Projectile.Hit、DamageService 扣血、命中后销毁、穿透多目标和 MaxLifeTime 停止销毁；本游戏 Godot smoke 已覆盖项目侧调用、路径字符串、目标位置、关系绑定、`GodotProjectileEffectSpawner` 按 `ScenePath` 实例化投射物 / 特效视觉节点并注册到 `GodotNodeRegistry`，自动播放 Effect `AnimatedSprite2D`，以及投射物命中锁定目标、造成伤害、穿透两名目标、按生命周期销毁 Runtime 和清理视觉节点。
- AIService 最小行为树由框架 Runtime tests 覆盖，包含最近目标查询、追目标写入 Movement AI 意图、确定性左右巡逻和等待倒计时、行为树预制块攻击优先 / 追逐 / 巡逻回退、范围内发出 `GameEventType.Attack.Requested`、行为树中准备 Ability 自动索敌上下文并推进 Ability Periodic 自动触发；本游戏 Godot smoke 已覆盖 `GodotAIComponent` 导出参数写入、手动 Tick 写入巡逻移动意图，并由 `GodotMovementDriver + MoveMode.AIControlled` 同步到真实节点位置。
- AttackService 最小 Runtime 由框架 Runtime tests 覆盖，包含消费攻击请求事件、前摇 / 后摇 / 冷却 Timer、距离和死亡门禁，以及通过 DamageService 造成 `DamageTags.Attack` 伤害；本游戏 Godot smoke 已覆盖 `GodotAttackComponent` 导出参数写入、节点目标解析、攻击请求、HP 扣减、旧 `AttackComponent` 包装类保留已有 Attack Data 并结算伤害，以及 Attack Started / Cancelled 转发到 `GodotUnitAnimationComponent` 后从可用 `attack*` 动画回退选择、播放 attack / 取消回 idle / 一次性动画完成发布 `unit:animation_finished` 并回 idle。
- GodotBridge `GodotEntity / IGodotComponent / GameOSTimerDriver` 编译接入。
- 游戏侧 Bridge `BrotatoLikePlayerInputComponent` 已建立，headless smoke 覆盖组件注册、InputDirection Data 写入、平滑加速和直接速度回退；框架 GodotBridge 不再持有 BrotatoLike-specific 输入组件。
- GodotBridge `GodotNodePool<Area2D> / GodotCollisionIsolation / GodotNodePoolManager.ReturnToPool` 已接入 headless smoke，当前 `_Ready` 测试模式覆盖延迟激活、回池脱树和复用，失败会返回非 0。
- Godot 场景测试 runner 已建立：`Tools/run-godot-scene.sh` 支持 `list / run / run-many / run-all / run-main-smoke`、构建开关、超时、attempts 和日志目录；`Tools/analyze-godot-scene-logs.sh` 读取新结构 `index.json/result.json/combined.log`、artifact status 和 JSONL 数量；`Tools/run-godot-smoke.sh` 保持旧兼容入口并委托到统一 runner。
- 旧 `assets/` 已复制到新仓库根目录 `assets/`，保留 `res://assets/...` 路径。
- 旧 `Data/` 和 `Src/Main/` 已复制到 `MigrationInput/`，当前排除编译；旧 Main 中已确认的游戏入口逻辑已迁入，后续按模块继续适配真实 UI / 输入 / 场景内容。

## 下一步

1. 跟进 passing scenes 中的诊断 stderr：Game/Input 的 `Parameter "data.tree" is null` 和框架 UnitComposition 的 Godot RID leak；这两项当前不覆盖 artifact oracle，但不能作为“无 error”证明。
2. 继续把 wave reward hook 推进到完整商店体验和经济曲线；当前多波流程已能从第 1 波进入 `RewardShop` 并启动第 2 波，但 shop/item 仍只覆盖 deterministic offer、购买门禁和道具效果，不包含刷新/锁定/售卖、货币掉落、存档和永久成长。替换/被动面板和 meta progression 仍需后续 change。
3. 继续补更多投射物/特效动画样式验收；Chain Lightning 连线与 8 个 projectile/passive 技能已具备行为和 cleanup artifact，后续只剩美术样式或像素级截图门禁增强。
4. 继续做手动设备专项：物理手柄 LB/RB/X、摇杆、鼠标/手柄 Point target 细节和窗口焦点问题，作为自动 `Input.ActionPress` 之外的人工 QA 或专门设备测试。

## 最新验证

**brotatolike-character-selection（2026-05-21）**

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-build.sh
Tools/run-godot-scene.sh run res://Src/Validation/Game/CharacterSelection/BrotatoLikeCharacterSelectionValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh --run-dir .ai-temp/scene-tests/runs/2026-05-21/20-00-19 --manifest DocsAI/ValidationManifest.json
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh --run-dir .ai-temp/scene-tests/runs/2026-05-21/20-03-04 --manifest DocsAI/ValidationManifest.json
```

结果：`Tools/run-build.sh` PASS（DataOS validation PASS，生成 `runtime_snapshot.json`、`shop_item_authoring.json`、`wave_authoring.json`、`character_authoring.json`，85 个 XML comment warnings，0 errors）。CharacterSelection validation `.ai-temp/scene-tests/runs/2026-05-21/20-00-19/index.json` PASS；per-scene `result.json` exitCode `0`、`firstError=null`；artifact `brotatolike-character-selection-validation.json` 为 `status=pass`、`failureReasons=[]`。关键 checks `character_catalog_authoring_valid / character_select_ui_scene_backed / selected_character_spawns_runtime_player / two_characters_distinct_evidence / selected_player_bindings` 全部 pass，记录 `deluyi,guangfa`、invalid player/visual reject、UI 按钮选择 `guangfa`、`player-guangfa`、起始技能 `chain_lightning,sine_wave_shot,target_point_skill,dash`、视觉/属性/loadout 差异和两名角色的 input/HUD/health bar/skill UI/camera 绑定。Main 回归 `.ai-temp/scene-tests/runs/2026-05-21/20-03-04/index.json` PASS，artifact `scene-acceptance.json` 为 `status=pass`、`failureReasons=[]`，记录默认 fallback `skill_loadout_source=character:deluyi`。两组 analyzer `gate-report.json` 均为 `verdict=pass`；已检查 `index.json`、per-scene `result.json` 和 scene artifact，`expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 均非空。

**complete-brotatolike-wave-run-flow（2026-05-21）**

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-build.sh
Tools/run-godot-scene.sh run res://Src/Validation/Game/RunFlow/BrotatoLikeRunFlowValidation.tscn --timeout 15 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh --run-dir .ai-temp/scene-tests/runs/2026-05-21/19-17-09 --manifest DocsAI/ValidationManifest.json --gate-report .ai-temp/scene-tests/runs/2026-05-21/19-17-09/gate-report.json
Tools/run-godot-scene.sh run res://Src/Validation/Game/Progression/BrotatoLikeProgressionLoopValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh --run-dir .ai-temp/scene-tests/runs/2026-05-21/19-18-08 --manifest DocsAI/ValidationManifest.json --gate-report .ai-temp/scene-tests/runs/2026-05-21/19-18-08/gate-report.json
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh --run-dir .ai-temp/scene-tests/runs/2026-05-21/19-18-22 --manifest DocsAI/ValidationManifest.json --gate-report .ai-temp/scene-tests/runs/2026-05-21/19-18-22/gate-report.json
```

结果：`Tools/run-build.sh` PASS（DataOS validation PASS，生成 `runtime_snapshot.json`、`shop_item_authoring.json`、`wave_authoring.json`，49 个既有 XML comment warnings，0 errors）。RunFlow validation `.ai-temp/scene-tests/runs/2026-05-21/19-17-09/index.json` PASS；per-scene `result.json` exitCode `0`、`firstError=null`；artifact `brotatolike-run-flow-validation.json` 为 `status=pass`、`failureReasons=[]`。关键 checks `wave_authoring_loaded_and_validates_refs / first_wave_starts_and_spawns / pause_gate_and_respawn_preserved / first_wave_completion_reward_phase / second_wave_starts / wave_cleanup_counts / wave_ui_phase_scene_backed` 全部 pass，记录两波 expected spawn count 均为 5、非法 enemy/resource 拒绝、第一波 `Running`、pause gate 阻断/恢复、死亡复活、`Completed -> RewardShop`、`shop_offer.validation` hook、cleanup `runtime 10 -> 5` / enemy `5 -> 0`、第二波 `Running` 与 scene-backed `ExperienceBarUI` wave phase。RunFlow、Progression 回归 `.ai-temp/scene-tests/runs/2026-05-21/19-18-08/index.json`、Main 回归 `.ai-temp/scene-tests/runs/2026-05-21/19-18-22/index.json` 的 analyzer `gate-report.json` 均为 `verdict=pass`；已检查三组 `index.json`、per-scene `result.json` 和 scene artifact，`expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 均非空。

**design-brotatolike-shop-item-loop（2026-05-21）**

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-build.sh
Tools/run-godot-scene.sh run res://Src/Validation/Game/Shop/BrotatoLikeShopItemValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run-all --manifest DocsAI/ValidationManifest.json --release-batch --continue-on-fail --log-dir .ai-temp/scene-tests/runs --errors-only
Tools/analyze-godot-scene-logs.sh --run-dir .ai-temp/scene-tests/runs/2026-05-21/18-39-01 --manifest DocsAI/ValidationManifest.json --gate-report .ai-temp/scene-tests/runs/2026-05-21/18-39-01/gate-report.json
```

结果：`Tools/run-build.sh` PASS（DataOS validation PASS，`runtime_snapshot.json` 与 `shop_item_authoring.json` 已生成，0 warnings，0 errors）。Targeted Shop validation PASS；Main 回归 `.ai-temp/scene-tests/runs/2026-05-21/18-38-12/index.json` PASS。完整 release-batch `.ai-temp/scene-tests/runs/2026-05-21/18-39-01/index.json` 为 26 executed、26 passed、0 failed、0 missing；Shop entry `025_Src_Validation_Game_Shop_BrotatoLikeShopItemValidation.tscn_attempt1` 的 `result.json` exitCode `0`，artifact `brotatolike-shop-item-validation.json` 为 `status=pass`、`failureReasons=[]`，checks `item_authoring_loaded_and_validates_targets / shop_offers_deterministic / affordable_purchase_applies_item / unaffordable_purchase_rejected / shop_ui_updates_and_cleanup` 全部 pass。Scene gate 已检查完整 batch 的 `index.json`、26 个 per-scene `result.json` 和 scene artifact，所有 artifact 的 `expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 均非空，missingChecks 与 staleAgainst 均为空；analyzer `gate-report.json` verdict `pass`。

**design-brotatolike-levelup-choice-loop（2026-05-21）**

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-build.sh
Tools/run-godot-scene.sh run res://Src/Validation/Game/Progression/BrotatoLikeProgressionLoopValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh --run-dir .ai-temp/scene-tests/runs/2026-05-21/17-45-01
Tools/run-godot-scene.sh run res://Src/Validation/Game/PlayableUX/BrotatoLikePlayableUXValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh --run-dir .ai-temp/scene-tests/runs/2026-05-21/17-45-17
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh --run-dir .ai-temp/scene-tests/runs/2026-05-21/17-45-33
```

结果：`Tools/run-build.sh` PASS（DataOS validation PASS，26 个既有 XML comment warnings，0 errors）。Progression `.ai-temp/scene-tests/runs/2026-05-21/17-45-01/index.json` PASS，`result.json` exitCode `0`，artifact `brotatolike-progression-loop-validation.json` 为 `status=pass`、`failureReasons=[]`；新增 checks `level_up_choice_scene_backed / level_up_choice_applies_stat_reward / level_up_choice_applies_ability_reward / level_up_choice_gate_blocks_and_resumes_tick / experience_ui_scene_backed_updates` 全部 pass，记录三选一 ids/effect types、scene-backed choice panel、scene-backed experience bar、HP `100 -> 110`、owned ability count `4 -> 5`、`sine_wave_ability_owned=true`、gate `ModalUi/Suspended -> None/Running`。PlayableUX `.ai-temp/scene-tests/runs/2026-05-21/17-45-17/index.json` PASS，Main `.ai-temp/scene-tests/runs/2026-05-21/17-45-33/index.json` PASS；三个 analyzer `gate-report.json` 均为 `verdict=pass`。Scene gate 已检查上述 run 的 `index.json`、per-scene `result.json` 和 scene artifact，`expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 均非空。

**validate-brotatolike-projectile-and-passive-skills（2026-05-21）**

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-build.sh
Tools/run-godot-scene.sh run res://Src/Validation/Game/Skills/BrotatoLikeSkillValidation.tscn --timeout 20 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh --run-dir .ai-temp/scene-tests/runs/2026-05-21/17-16-37
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh --run-dir .ai-temp/scene-tests/runs/2026-05-21/17-17-42
```

结果：`Tools/run-build.sh` PASS（DataOS validation PASS，26 个既有 XML comment warnings，0 errors）。Skills validation `.ai-temp/scene-tests/runs/2026-05-21/17-16-37/index.json` PASS，`result.json` exitCode `0`，artifact `brotatolike-skill-validation.json` 为 `status=pass`、`failureReasons=[]`；`sine_wave_shot / boomerang_throw / bezier_shot / parabola_shot / arc_shot / orbit_skill / circle_damage / aura_shield / ability_id_grouping` 全部 pass。Gate report `.ai-temp/scene-tests/runs/2026-05-21/17-16-37/gate-report.json` verdict `pass`，README 和 artifact 的 `expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 均非空。Main 回归 `.ai-temp/scene-tests/runs/2026-05-21/17-17-42/index.json` PASS，`scene-acceptance.json` 为 `status=pass`、`failureReasons=[]`，analyzer `gate-report.json` verdict `pass`。

**expand-brotatolike-skill-loadout（2026-05-21）**

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-build.sh
Tools/run-godot-scene.sh run res://Src/Validation/Game/PlayableUX/BrotatoLikePlayableUXValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh --run-dir .ai-temp/scene-tests/runs/2026-05-21/16-05-14
```

结果：`Tools/run-build.sh` PASS（DataOS validation PASS，26 个既有 XML comment warnings，0 errors）。PlayableUX `.ai-temp/scene-tests/runs/2026-05-21/16-01-37/index.json` PASS，artifact `brotatolike-playable-ux-validation.json` 为 `status=pass`、`failureReasons=[]`，`validation_loadout_override_visible_slots` 通过并记录 12 owned / 4 visible / 8 hidden、available pool 和 passive ids。Main `.ai-temp/scene-tests/runs/2026-05-21/16-05-14/index.json` PASS，artifact `scene-acceptance.json` 为 `status=pass`、`failureReasons=[]`，记录 `skill_loadout_source=default`、owned ids、visible slot ids、selected id、`skill_total_owned_count=4`、available pool 12 项；analyzer `gate-report.json` verdict `pass`。Scene gate 已检查上述 run 的 `index.json`、per-scene `result.json` 和 scene artifact，`expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 均非空。

**restore-brotatolike-chain-lightning-line-vfx（2026-05-21）**

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-build.sh
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh --run-dir .ai-temp/scene-tests/runs/2026-05-21/15-43-35
```

结果：`Tools/run-build.sh` PASS（26 个既有 XML comment warnings，0 errors）。Main `.ai-temp/scene-tests/runs/2026-05-21/15-43-35/index.json` PASS，artifact `scene-acceptance.json` 为 `status=pass`、`failureReasons=[]`；新增 `skill.chain_line_vfx_bound / cleanup / multi_bounce` 全部通过，记录 expected `3`、recorded `3`、bound `3`、cleanup `3`，scene path 为 `res://Scenes/VFX/LightningLineEffect.tscn`，source/target 为 `player-deluyi->spawn-chailangren-1;spawn-chailangren-1->spawn-chailangren-2;spawn-chailangren-2->spawn-yuren-3`，duration 为 `0.2;0.2;0.2`。Scene gate 已检查本次 run 的 `index.json`、per-scene `result.json` 和 scene artifact，`expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 均非空；analyzer `gate-report.json` verdict `pass`。

**validate-brotatolike-dash-main-skill（2026-05-21）**

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-build.sh
Tools/run-godot-scene.sh run res://Src/Validation/Game/GameLifecycle/BrotatoLikeGameplayLifecycleValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh --run-dir .ai-temp/scene-tests/runs/2026-05-21/15-17-40
```

结果：`Tools/run-build.sh` PASS（26 个既有 XML comment warnings，0 errors）。GameLifecycle `.ai-temp/scene-tests/runs/2026-05-21/15-14-41/index.json` PASS，artifact `brotatolike-gameplay-lifecycle-validation.json` 为 `status=pass`、`failureReasons=[]`；`dash_input_skill_bar_path` 通过，记录 `dash_selected_skill_id=ability-dash-player-deluyi`、selected index `3`、skill bar selected index `3`、`dash_trigger_result=Success`、位置从 `-638.769,-640` 到 `-338.769,-640`、`dash_distance=300`、`dash_cooldown_remaining>0`、`dash_skill_bar_scene_backed=true`。Main `.ai-temp/scene-tests/runs/2026-05-21/15-17-40/index.json` PASS，`scene-acceptance.json` 为 `status=pass`、`failureReasons=[]`；analyzer `gate-report.json` verdict `pass`。Scene gate 已检查上述 run 的 `index.json`、per-scene `result.json` 和 scene artifact，`expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 均非空。GameLifecycle stderr 仍有既有 Godot RID leak 诊断，不作为无 error 证明。

**stabilize-brotatolike-release-batch（2026-05-21）**

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-build.sh
Tools/run-godot-scene.sh run res://Src/Validation/Game/PlayableUX/BrotatoLikePlayableUXValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Src/Validation/Game/Progression/BrotatoLikeProgressionLoopValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run-all --manifest DocsAI/ValidationManifest.json --release-batch --continue-on-fail --log-dir .ai-temp/scene-tests/runs --errors-only
Tools/analyze-godot-scene-logs.sh --run-dir .ai-temp/scene-tests/runs/2026-05-21/14-57-55 --manifest DocsAI/ValidationManifest.json --gate-report .ai-temp/scene-tests/runs/2026-05-21/14-57-55/gate-report.json
```

结果：`Tools/run-build.sh` PASS（DataOS validation PASS，snapshot regenerated，26 个既有 XML comment warnings，0 errors）。Targeted PlayableUX `.ai-temp/scene-tests/runs/2026-05-21/14-57-13/index.json` PASS；Targeted Progression `.ai-temp/scene-tests/runs/2026-05-21/14-55-15/index.json` PASS。完整 release-batch `.ai-temp/scene-tests/runs/2026-05-21/14-57-55/index.json` 为 25 executed、25 passed、0 failed、0 missing，`.ai-temp/scene-tests/runs/2026-05-21/14-57-55/gate-report.json` 为 `verdict=pass`。Scene gate 已批量检查本次 release-batch 的 `index.json`、25 个 `result.json` 和所有非日志 scene artifact，`expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 均非空。

**fix-brotatolike-lifecycle-regressions（2026-05-21）**

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
Tools/run-build.sh
Tools/run-tests.sh

cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-build.sh
```

结果：框架 build PASS（`0 Warning(s), 0 Error(s)`）；框架 tests 全部 PASS，包含 `AI service finds nearest target`；BrotatoLike build PASS（DataOS validation PASS，`0 Warning(s), 0 Error(s)`）。

Targeted Godot evidence：

- AI Capability：`.ai-temp/scene-tests/runs/2026-05-21/12-37-18/index.json`，artifact `ai-capability-validation.json` 为 `status=pass`、`failureReasons=[]`，`injected_target_query_nearest_target` 选择 `ai-scene-near` 并忽略 `ai-scene-ability-entity`。
- GameLifecycle：`.ai-temp/scene-tests/runs/2026-05-21/12-39-21/index.json`，artifact `brotatolike-gameplay-lifecycle-validation.json` 为 `status=pass`、`failureReasons=[]`，`death_auto_respawn` 记录复活后输入/技能节点存在、真实 MoveRight 写入输入并驱动位移。
- PlayableUX：`.ai-temp/scene-tests/runs/2026-05-21/12-39-37/index.json`，artifact `brotatolike-playable-ux-validation.json` 为 `status=pass`、`failureReasons=[]`，`enemy_head_health_bar_canvas_coordinates` 与 `damage_and_heal_numbers_canvas_coordinates` 通过，飘字 canvas distance 为 0。
- Main smoke：`.ai-temp/scene-tests/runs/2026-05-21/12-39-58/index.json`，artifact `scene-smoke.json` 为 `status=pass`、`failureReasons=[]`。

Scene gate 手动检查已覆盖上述四个 run 的 `index.json`、per-scene `result.json` 和 scene artifact；artifact 中 `expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 均非空。

**systemagent-integrated-validation-governance（2026-05-21）**

```bash
Tools/run-build.sh
Tools/run-godot-scene.sh run-all --manifest DocsAI/ValidationManifest.json --release-batch --continue-on-fail --log-dir .ai-temp/scene-tests/runs --errors-only
Tools/analyze-godot-scene-logs.sh --run-dir .ai-temp/scene-tests/runs/2026-05-21/10-13-37 --manifest DocsAI/ValidationManifest.json --gate-report .ai-temp/scene-tests/runs/2026-05-21/10-13-37/gate-report.json
```

结果：`Tools/run-build.sh` PASS（26 warnings，0 errors；warnings 为既有 XML comment 类）。Targeted run `.ai-temp/scene-tests/runs/2026-05-21/10-06-55/gate-report.json` 为 `pass`，Game/Input 和 GameLifecycle 均有 `index.json`、`result.json`、artifact oracle，五字段非空。Release-batch run `.ai-temp/scene-tests/runs/2026-05-21/10-13-37/gate-report.json` 为 `block`，25 requested、23 passed、2 failed、0 missing；剩余 blocker 是 `res://Src/Validation/Game/PlayableUX/BrotatoLikePlayableUXValidation.tscn` 的 `scene_backed_formal_ui` 与 `res://Src/Validation/Game/Progression/BrotatoLikeProgressionLoopValidation.tscn` 的 `pause_menu_blocks_and_resumes_tick` / `scene_backed_pause_menu`。失败 artifact 分别位于 `023_Src_Validation_Game_PlayableUX_BrotatoLikePlayableUXValidation.tscn_attempt1/artifacts/brotatolike-playable-ux-validation.json` 和 `024_Src_Validation_Game_Progression_BrotatoLikeProgressionLoopValidation.tscn_attempt1/artifacts/brotatolike-progression-loop-validation.json`，均为 `status=fail` 且五字段非空。

**DataOS table-first authoring（2026-05-20）**

```bash
Tools/run-build.sh
Tools/run-godot-scene.sh run res://SlimeAI/Src/Validation/Runtime/Data/RuntimeDataValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run-main-smoke --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

结果：`Tools/run-build.sh` PASS（DataOS validation PASS，snapshot regenerated，`0 Warning(s), 0 Error(s)`）。Runtime/Data 场景输出 `GameOS Runtime Data validation PASS`，artifact 位于 `.ai-temp/scene-tests/runs/2026-05-20/22-30-25/index.json`。`run-main-smoke` 输出 `BrotatoLike GameOS smoke PASS`，`scene-smoke.json` 中 `dataos.snapshot` check 为 pass，`snapshotApplied / abilityApplied / resourcesRegistered / spawnSystemSynced` 均为 true，artifact 位于 `.ai-temp/scene-tests/runs/2026-05-20/22-30-33/index.json`。Scene gate 已检查 `index.json`、`result.json` 和 scene artifact，`expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 均非空。注意：本次 analyzer 捕获到 `BrotatoLikeGameRuntime.SpawnPlayer()` 的既有 reparent stderr，需要后续 DebugFix 跟进；DataOS snapshot probe 本身通过。

**restore-brotatolike-playable-ux（2026-05-20）**

```bash
Tools/run-build.sh
Tools/run-godot-scene.sh run res://Src/Validation/Game/PlayableUX/BrotatoLikePlayableUXValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Src/Validation/Game/Progression/BrotatoLikeProgressionLoopValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Src/Validation/Game/LegacyResources/BrotatoLikeLegacyResourceClassificationValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

结果：`Tools/run-build.sh` PASS（`0 Warning(s), 0 Error(s)`）。`BrotatoLikePlayableUXValidation` 输出 `BrotatoLike Playable UX validation PASS`，artifact 位于 `.ai-temp/scene-tests/runs/2026-05-20/11-32-49/index.json`，正式 HUD/玩家 HP/头顶血条/四槽技能栏/点选/伤害飘字/可见移动检查均为 pass。`BrotatoLikeProgressionLoopValidation` 输出 `BrotatoLike Progression Loop validation PASS`，artifact 位于 `.ai-temp/scene-tests/runs/2026-05-20/11-33-15/index.json`，wave completion、pause gate、HP recovery、dead skip、经验拾取和 level-up 均为 pass，mana recovery 因当前 active catalog 无 mana 数据记录为 `not-applicable`。`BrotatoLikeLegacyResourceClassificationValidation` 输出 `BrotatoLike Legacy Resource Classification validation PASS`，artifact 位于 `.ai-temp/scene-tests/runs/2026-05-20/11-33-42/index.json`，`legacyCount=25`、`unsupportedStatusCount=0`、`missingActiveLegacyCount=0`。普通 Main 输出 `BrotatoLike playable slice PASS`，artifact 位于 `.ai-temp/scene-tests/runs/2026-05-20/11-33-55/index.json`，记录 `formal_hud_found=True`、`formal_hud_current_skill=位置目标`、`formal_hud_damage_number_count=77`、`skill_point_report=Success`。`run-main-smoke` 输出 `BrotatoLike GameOS smoke PASS`，artifact 位于 `.ai-temp/scene-tests/runs/2026-05-20/11-34-19/index.json`。Scene gate 已检查上述 `index.json`、`result.json` 和 scene artifact，`expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 均非空。

**单位组合 profile 迁移（2026-05-20）**

```bash
Tools/run-build.sh
Tools/run-godot-scene.sh run res://Src/Validation/Game/UnitComposition/BrotatoLikeUnitCompositionValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run-main-smoke --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

结果：`Tools/run-build.sh` PASS（`0 Warning(s), 0 Error(s)`）；`BrotatoLikeUnitCompositionValidation` 输出 `BrotatoLike UnitComposition validation PASS`，artifact 位于 `.ai-temp/scene-tests/runs/2026-05-20/09-18-27/index.json`；普通 Main 输出 `BrotatoLike playable slice PASS`，artifact 位于 `.ai-temp/scene-tests/runs/2026-05-20/09-20-05/index.json`；`run-main-smoke` 输出 `BrotatoLike GameOS smoke PASS`，artifact 位于 `.ai-temp/scene-tests/runs/2026-05-20/09-20-45/index.json`。Scene gate 已检查对应 `index.json`、`result.json` 和 artifact，`expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 均非空。

**7 个 GameOS OpenSpec 未完成项收敛验证（2026-05-19）**

```bash
Tools/run-build.sh
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run-main-smoke --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

结果：`Tools/run-build.sh` PASS（`0 Warning(s), 0 Error(s)`）；普通 Main 输出 `BrotatoLike playable slice PASS`，artifact 位于 `.ai-temp/scene-tests/runs/2026-05-19/20-58-59/index.json`；`run-main-smoke` 输出 `BrotatoLike GameOS smoke PASS`，analyzer 输出 `status: pass`、`firstError: none`，latest artifact 位于 `.ai-temp/scene-tests/runs/2026-05-19/20-59-07/index.json`。`scene-smoke.json` 与普通 Main `scene-acceptance.json` 的 `expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 均非空。

```bash
Tools/run-build.sh
Tools/run-godot-scene.sh run res://Src/Validation/Game/Input/BrotatoLikeInputEventValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run-main-smoke --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

结果：`Tools/run-build.sh` PASS（0 warnings / 0 errors）；`BrotatoLikeInputEventValidation` 输出 `BrotatoLike Game Input validation PASS`，artifact 位于 `.ai-temp/scene-tests/runs/2026-05-15/18-36-50/index.json`；`run-main-smoke` 输出 `BrotatoLike GameOS smoke PASS`，analyzer 输出 `status: pass`、`firstError: none`，artifact 位于 `.ai-temp/scene-tests/runs/2026-05-15/18-36-58/index.json`。

**gameos-capability-scoped-services 接入（2026-05-19）**：框架 change 修改 `DamageService` 构造（可选注入 `HealService`）、`LifestealProcessor` 构造注入、`AIContext.AbilityService` 移除默认值（`GodotAIComponent` 已显式注入 `AbilityService.Instance`）、`GodotContactDamageComponent` 改为 `DamageService.Default`。BrotatoLike 无游戏侧代码改动；`Tools/run-build.sh` PASS（0 errors）；`run-main-smoke` PASS，artifact 位于 `.ai-temp/scene-tests/runs/2026-05-19/17-22-28/index.json`。

结果：`Tools/run-build.sh` PASS（0 errors；XML 注释 warnings 仍存在，其中包含 P4 新 public CommandBuffer 类型的同类 warning）；`run-main-smoke` 输出 `BrotatoLike GameOS smoke PASS` 且 `bridge:True pool:True dataos:True main:True`；analyzer 输出 `status: pass`、`firstError: none`。P4 最新 passing smoke artifact 位于 `.ai-temp/scene-tests/runs/2026-05-15/16-45-27/index.json`。

P1 Runtime LifecycleTree 迁移补充验证：`Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs && Tools/analyze-godot-scene-logs.sh` 输出 `BrotatoLike playable slice PASS`，analyzer 输出 `status: pass`、`firstError: none`，artifact 位于 `.ai-temp/scene-tests/runs/2026-05-15/09-40-30/index.json`。

补充：事件系统归档验证中，`Tools/run-godot-scene.sh run-main-smoke --timeout 10 --log-dir .ai-temp/scene-tests/runs` 生成 `.ai-temp/scene-tests/runs/2026-05-13/09-23-37/index.json` 和 `artifacts/eventbus-dump.json`；普通 `Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs` 生成 `.ai-temp/scene-tests/runs/2026-05-13/09-24-15/index.json`，analyzer 输出 `status: pass`、`firstError: none`。

Runtime/Data 专项场景补充验证：`Tools/run-godot-scene.sh list` 已列出 `res://SlimeAI/Src/Validation/Runtime/Data/RuntimeDataValidation.tscn`；`Tools/run-godot-scene.sh run res://SlimeAI/Src/Validation/Runtime/Data/RuntimeDataValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs && Tools/analyze-godot-scene-logs.sh` 输出 `GameOS Runtime Data validation PASS`，analyzer 输出 `status: pass`、`firstError: none`，artifact 位于 `.ai-temp/scene-tests/runs/2026-05-13/15-47-30/index.json`。
