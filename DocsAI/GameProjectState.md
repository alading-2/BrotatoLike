# BrotatoLike GameProjectState

> 更新日期：2026-05-21（release-batch stability）

## 当前状态

框架接入基线已创建。框架仓库已有 `SlimeAI.GameOS` Runtime 最小内核、typed Runtime Data contract、DataOS SQLite schema / migration / generator / validator / typed Runtime snapshot loader、GodotBridge 第一版，以及 Movement / Collision / Damage / Ability / Projectile / Effect / Feature / AI / Attack 第一批能力。

本轮追加：

- **Release-batch stability**：OpenSpec change `stabilize-brotatolike-release-batch` 已复跑 BrotatoLike manifest release-batch 并解除历史 PlayableUX / Progression blocker。`Tools/run-build.sh` 通过（DataOS validation PASS，26 个既有 XML comment warnings，0 errors）；完整 release-batch `.ai-temp/scene-tests/runs/2026-05-21/14-57-55/index.json` 为 25/25 passed，analyzer 生成 `.ai-temp/scene-tests/runs/2026-05-21/14-57-55/gate-report.json`，verdict `pass`、requested 25、passed 25、failed 0、missing 0。PlayableUX 和 Progression 的 targeted run 分别为 `.ai-temp/scene-tests/runs/2026-05-21/14-57-13/index.json`、`.ai-temp/scene-tests/runs/2026-05-21/14-55-15/index.json`，完整 batch 中对应 artifact 也为 `status=pass` 且标准答案五字段非空。
- **Lifecycle regression fix**：OpenSpec change `fix-brotatolike-lifecycle-regressions` 已修复 Main 运行中暴露的三条生命周期回归。复活路径通过框架 `GodotBridgeContext.DestroyEntity()` 同步注销 Runtime Entity、node registry 和 adapter registry 后再 `QueueFree()`，同 EntityId 新玩家可重新绑定 `BrotatoLikePlayerInputComponent` 与 `GodotActiveSkillInputComponent`；AI target selector 会过滤无有效 team / HP evidence 的非战斗实体，避免敌人攻击 ability-like entity；HUD 头顶血条和伤害/治疗飘字改由 `BrotatoLikeHud` 基于当前 viewport canvas transform 统一做 world-to-canvas 映射。Targeted 验证见“最新验证”。
- **SystemAgent integrated validation governance**：OpenSpec change `systemagent-integrated-validation-governance` 将 BrotatoLike Godot 验证纳入 manifest / batch runner / analyzer / scene-gate 证据闭环。`DocsAI/ValidationManifest.json` 是 release-batch 权威选择源，当前包含 25 个 `releaseBatch=true` 场景；`Tools/run-godot-scene.sh run-all --manifest DocsAI/ValidationManifest.json --release-batch` 会写入结构化 `index.json`，analyzer 会写入 `gate-report.json` 并检查 README 五字段、`index.json`、per-scene `result.json`、scene artifact 五字段、manifest checks、catalog 和 freshness。历史 release-batch `2026-05-21/10-13-37` 曾被 PlayableUX / Progression 两个 feature-slice artifact 失败阻断；新 release-batch `2026-05-21/14-57-55` 已覆盖同一 manifest 并通过。
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
- **R07 可玩切片验收**：普通 `Scenes/Main.tscn` 在 scene runner 的 artifact 环境下会执行 `BrotatoLikePlayableSliceAcceptance`，与 `--gameos-smoke-exit` smoke 路径分离；输出 `BrotatoLike playable slice PASS/FAIL`，并写入 `artifacts/scene-acceptance.json`。当前验收覆盖玩家生成、WASD + 方向键 input map、`Movement.InputDirection` / `Movement.LastMoveDirection`、玩家位移和摄像机/视口可见性、第 1 波敌人生成、敌人追逐移动、接触伤害、敌人死亡和 cleanup、`slam` / `chain_lightning` / `target_point_skill` 真实 input action 触发、点选确认、冷却门禁、命中、正式 HUD、玩家 HP Label、四槽技能栏、头顶血条、伤害数字、progression summary 和结构化 damage logs。`PlayableSliceHUD` 测试专用 Label 不再作为完成证据。
- **单位组合 profile 迁移**：玩家和近战敌人生成改为 DataOS 写入后调用框架 `GodotUnitComposer`，由 `BrotatoLikeUnitProfiles.Player / EnemyMelee` 选择 visual、animation、orientation、AI、attack、hurtbox 和 contact damage adapter；游戏侧仍只挂 `BrotatoLikePlayerInputComponent` 与 `GodotActiveSkillInputComponent`。`BrotatoLikeEnemySpawnSystem` 使用共享 `GodotMovementDriver` 并启动 `MoveMode.AIControlled`，验证不再手写 `Movement.AIMoveDirection`。
- **统一 Observation / runner**：`Tools/run-godot-scene.sh` 现在委托 `.codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs`，新日志结构固定为 `index.json + 001_<scene>_attempt1/{stdout,stderr,combined,result,artifacts}`；`BrotatoLikePlayableSliceAcceptance` 写入小写 `status=pass/fail` 和 `artifacts/logs/scene-log.jsonl`，`Main.cs` 使用 `GameOSLog.For("BrotatoLike.Main")` 输出流程日志；`Src/Validation/GameOS/Observation/ObservationLogValidation.tscn` 独立验证通用 log level、格式化、过滤、JSONL sink 和 runner session 路径。
- **EventBus observation dump**：`--gameos-smoke-exit` smoke 路径会在 runner artifact 环境下导出 `artifacts/eventbus-dump.json`；最新 `.ai-temp/scene-tests/runs/2026-05-13/09-23-37/.../eventbus-dump.json` 中 `SameTypeReentryBlockedCounts={}`、`HandlerExceptions=[]`，用于确认 BrotatoLike smoke 没有事件重入阻断或 handler 异常。
- **Scene artifact gate**：普通 `Scenes/Main.tscn` 的 `scene-acceptance.json` 和 `--gameos-smoke-exit` 的 `scene-smoke.json` 均输出标准答案字段：`expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath`。`run-main-smoke` 继续保留 `eventbus-dump.json`。
- **迁移台账**：新增 `DocsAI/MigrationLedger.md`，按旧 `Resources/Else/brotato-my` 主场景、Entity、Component、System、UI、Ability、DataNew、Config、ResourcePaths 和 Test 输入建立第一版映射；该台账用于审计和后续 R07 可玩切片追踪，明确 `DataOS-only` 与 `遗留引用` 不等于资源可加载或玩法完成。
- **Movement Acceleration 平滑移动**：框架 `MovementDataKeys.Acceleration` + `InputDrivenMovement` Lerp 平滑支持；DataOS `unit.player/deluyi` 已写入 `Movement.Acceleration = 12`；backward-compatible（无 Acceleration 时退化为直接速度）。
- **BrotatoLikePlayerInputComponent**：游戏侧 Bridge 新增输入桥接组件，每帧 `_Process` 读取 Godot Input Map（MoveLeft/Right/Up/Down + UseSkill/PreviousSkill/NextSkill），写入 `MovementDataKeys.InputDirection`，并发布 `BrotatoLike.Game.Events.InputUseSkill / InputPreviousSkill / InputNextSkill`；支持 `CanMoveInput` 门控和 AI 共存；已定义 BrotatoLike `project.godot` 输入映射（WASD + 方向键 + 手柄左摇杆）。
- **BrotatoLikeInputEventValidation**：新增游戏侧专项验证场景 `res://Src/Validation/Game/Input/BrotatoLikeInputEventValidation.tscn`，脚本位于 `Src/Validation/Game/Input/BrotatoLikeInputEventValidationScene.cs`，artifact 为 `artifacts/brotatolike-input-event-validation.json`。该场景验证 `BrotatoLikePlayerInputComponent` 写入 `MovementDataKeys.InputDirection`、技能输入事件类型归属 `BrotatoLike.Game.Events`，以及 `GodotActiveSkillInputComponent` 可由 `InputNextSkill / InputPreviousSkill / InputUseSkill` 切换并触发当前技能。该场景归属 BrotatoLike，不上提为框架 Runtime 场景。
- **BrotatoLikeGameRuntime 玩家生成**：新增 `SpawnPlayer(recordId, spawnPosition)`，从 DataOS `unit.player/deluyi` 读取数据，创建 `GodotEntity2D`，挂载 `BrotatoLikePlayerInputComponent`，加载视觉场景（`deluyi.tscn`），启动 `MoveMode.PlayerInput` 常驻移动，共享 `GodotMovementDriver`。
- **Main.tscn 自动创建玩家**：`StartGameRuntime()` 初始化后自动调用 `runtime.SpawnPlayer()`，发布 `Game.Started` 事件时玩家已就位。
- Smoke 新增 `BrotatoLikePlayerInputProbe`：覆盖组件注册、InputDirection 写入、Acceleration > 0 平滑加速（0.05s 时 ~45px/s，0.55s 时 ~100px/s）、Acceleration = 0 直接速度（瞬时 80px/s）。

当前 smoke probe 覆盖：

- Runtime Entity + Data -> Entity.Events 变更事件。
- Runtime Relationship 父子归属关系。
- Runtime Schedule 项目状态门禁。
- Runtime Pool 预热和释放。
- Runtime Timer Tick。
- Runtime ResourceCatalog 路径映射。
- DataOS `DataOS/Authoring/BrotatoLike.seed.sql` 生成 `DataOS/Snapshots/runtime_snapshot.json`；Godot smoke 通过 `BrotatoLikeDataOSBootstrap` 读取 typed snapshot，生成 `unit.enemy/yuren`、`unit.targeting_indicator/default`、`ability/chain_lightning`、`ability/parabola_shot`、`ability/sine_wave_shot`、`ability/boomerang_throw`、`ability/orbit_skill`、`ability/bezier_shot`、`ability/arc_shot`、`ability/circle_damage`、`system.config/SpawnSystem`、`system.preset/Default` 和 `spawn.config/default` Runtime Entity，断言 descriptor/manifest 进入 snapshot、旧 AbilityData 通用字段、链式技能参数、自动索敌参数、持续伤害参数、投射物速度 / 命中 / 生命周期 / 伤害参数、特效名称 / 持续时间参数，以及 SineWave / Orbit / Boomerang / Bezier / CircularArc 的 Movement handler authoring 参数，构建第 1 波敌人生成规则 catalog，并注册 `ResourceCatalog` 资源映射。
- `unit.player/deluyi` 已入 DataOS seed，含 `Movement.MoveSpeed = 200`、`Movement.Acceleration = 12`、`Attack.Damage = 10`、`Attack.Range = 150`、`Damage.MaxHp = 100` 等字段。
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
2. 扩展剩余玩家可用技能装配与逐技能主场景体验验收：`sine_wave_shot / boomerang_throw / arc_shot / bezier_shot / parabola_shot / orbit_skill / circle_damage / aura_shield` 当前主要是 handler/smoke 证据，还没有全部成为玩家普通局内可选技能。
3. 补 Chain Lightning 连线视觉、更多投射物/特效动画生命周期和样式验收；当前连锁伤害闭环已验证，但 `LineEffectScenePath` 仍未恢复为正式可视线效果。
4. 单独设计 shop/item/level-up choices/meta progression；本轮只实现最小经验拾取、经验阈值和 level-up 反馈，不包含商店、道具选择、存档和永久成长。
5. 继续做手动设备专项：物理手柄 LB/RB/X、摇杆、鼠标/手柄 Point target 细节和窗口焦点问题，作为自动 `Input.ActionPress` 之外的人工 QA 或专门设备测试。

## 最新验证

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
