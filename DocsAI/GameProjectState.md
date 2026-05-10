# BrotatoLike GameProjectState

> 更新日期：2026-05-10（本轮追加）

## 当前状态

框架接入基线已创建。框架仓库已有 `SkilmeAI.GameOS` Runtime 最小内核、DataOS SQLite schema / migration / generator / validator / Runtime snapshot loader、GodotBridge 第一版，以及 Movement / Collision / Damage / Ability / Projectile / Effect / Feature / AI / Attack 第一批能力。

本轮追加：
- **R07 可玩切片验收**：普通 `Scenes/Main.tscn` 在 scene runner 的 artifact 环境下会执行 `BrotatoLikePlayableSliceAcceptance`，与 `--gameos-smoke-exit` smoke 路径分离；输出 `BrotatoLike playable slice PASS/FAIL`，并写入 `artifacts/scene-acceptance.json`。当前验收覆盖玩家生成、WASD + 方向键 input map、`Movement.InputDirection` / `Movement.LastMoveDirection`、玩家位移、第 1 波敌人生成、敌人追逐移动、接触伤害、敌人死亡和 cleanup、`slam` 与 `chain_lightning` 触发 / 冷却门禁 / 命中、最小 Health / CurrentSkill HUD Label、结构化 damage logs。
- **统一 Observation / runner**：`Tools/run-godot-scene.sh` 现在委托 `.codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs`，新日志结构固定为 `index.json + 001_<scene>_attempt1/{stdout,stderr,combined,result,artifacts}`；`BrotatoLikePlayableSliceAcceptance` 写入小写 `status=pass/fail` 和 `artifacts/logs/scene-log.jsonl`，`Main.cs` 使用 `GameOSLog.For("BrotatoLike.Main")` 输出流程日志；`Scenes/Validation/GameOS/Observation/ObservationLogValidation.tscn` 独立验证通用 log level、格式化、过滤、JSONL sink 和 runner session 路径。
- **迁移台账**：新增 `DocsAI/MigrationLedger.md`，按旧 `Else/brotato-my` 主场景、Entity、Component、System、UI、Ability、DataNew、Config、ResourcePaths 和 Test 输入建立第一版映射；该台账用于审计和后续 R07 可玩切片追踪，明确 `DataOS-only` 与 `遗留引用` 不等于资源可加载或玩法完成。
- **Movement Acceleration 平滑移动**：框架 `MovementDataKeys.Acceleration` + `InputDrivenMovement` Lerp 平滑支持；DataOS `unit.player/deluyi` 已写入 `Movement.Acceleration = 12`；backward-compatible（无 Acceleration 时退化为直接速度）。
- **GodotPlayerInputComponent**：框架 GodotBridge 新增输入桥接组件，每帧 `_Process` 读取 Godot Input Map（MoveLeft/Right/Up/Down），写入 `MovementDataKeys.InputDirection`；支持 `CanMoveInput` 门控和 AI 共存；已定义 BrotatoLike `project.godot` 输入映射（WASD + 方向键 + 手柄左摇杆）。
- **BrotatoLikeGameRuntime 玩家生成**：新增 `SpawnPlayer(recordId, spawnPosition)`，从 DataOS `unit.player/deluyi` 读取数据，创建 `GodotEntity2D`，挂载 `GodotPlayerInputComponent`，加载视觉场景（`deluyi.tscn`），启动 `MoveMode.PlayerInput` 常驻移动，共享 `GodotMovementDriver`。
- **Main.tscn 自动创建玩家**：`StartGameRuntime()` 初始化后自动调用 `runtime.SpawnPlayer()`，发布 `Game.Started` 事件时玩家已就位。
- Smoke 新增 `GodotPlayerInputProbe`：覆盖组件注册、InputDirection 写入、Acceleration > 0 平滑加速（0.05s 时 ~45px/s，0.55s 时 ~100px/s）、Acceleration = 0 直接速度（瞬时 80px/s）。

当前 smoke probe 覆盖：

- Runtime Entity + Data -> Entity.Events 变更事件。
- Runtime Relationship 父子归属关系。
- Runtime Schedule 项目状态门禁。
- Runtime Pool 预热和释放。
- Runtime Timer Tick。
- Runtime ResourceCatalog 路径映射。
- DataOS `DataOS/Authoring/BrotatoLike.seed.sql` 生成 `DataOS/Snapshots/runtime_snapshot.json`；Godot smoke 通过 `BrotatoLikeDataOSBootstrap` 读取 snapshot，生成 `unit.enemy/yuren`、`unit.targeting_indicator/default`、`ability/chain_lightning`、`ability/parabola_shot`、`ability/sine_wave_shot`、`ability/boomerang_throw`、`ability/orbit_skill`、`ability/bezier_shot`、`ability/arc_shot`、`ability/circle_damage`、`system.config/SpawnSystem`、`system.preset/Default` 和 `spawn.config/default` Runtime Entity，断言旧 AbilityData 通用字段、链式技能参数、自动索敌参数、持续伤害参数、投射物速度 / 命中 / 生命周期 / 伤害参数、特效名称 / 持续时间参数，以及 SineWave / Orbit / Boomerang / Bezier / CircularArc 的 Movement handler authoring 参数，构建第 1 波敌人生成规则 catalog，并注册 `ResourceCatalog` 资源映射。
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
- GodotBridge `GodotPlayerInputComponent` 已建立，headless smoke 覆盖组件注册、InputDirection Data 写入、平滑加速和直接速度回退。
- GodotBridge `GodotNodePool<Area2D> / GodotCollisionIsolation / GodotNodePoolManager.ReturnToPool` 已接入 headless smoke，当前 `_Ready` 测试模式覆盖延迟激活、回池脱树和复用，失败会返回非 0。
- Godot 场景测试 runner 已建立：`Tools/run-godot-scene.sh` 支持 `list / run / run-many / run-all / run-main-smoke`、构建开关、超时、attempts 和日志目录；`Tools/analyze-godot-scene-logs.sh` 读取新结构 `index.json/result.json/combined.log`、artifact status 和 JSONL 数量；`Tools/run-godot-smoke.sh` 保持旧兼容入口并委托到统一 runner。
- 旧 `assets/` 已复制到新仓库根目录 `assets/`，保留 `res://assets/...` 路径。
- 旧 `Data/` 和 `Src/Main/` 已复制到 `MigrationInput/`，当前排除编译；旧 Main 中已确认的游戏入口逻辑已迁入，后续按模块继续适配真实 UI / 输入 / 场景内容。

## 下一步

1. 继续把后续真实主场景 / UI / SpawnSystem 测试接入 `Tools/run-godot-scene.sh`。
2. 继续迁 Feature actions 和 Ability 具体 handler 执行逻辑；SineWave / Boomerang / BezierCurve / CircularArc / Orbit / AttachToHost、Dash、ChainLightning、Slam、TargetPoint、CircleDamage、AuraShield 与 ArcShot 已接入 DataOS 到真实执行闭环，后续继续迁尚未接线的被动 Feature actions。
3. 从 `MigrationInput/` 继续适配真实 UI / 输入 / 游戏场景内容。
4. 玩家技能输入（LB/RB 切换、X 释放、Point 目标点选）接入 `GodotPlayerInputComponent`。

## 最新验证

```bash
Tools/run-build.sh
Tools/run-godot-smoke.sh
Tools/run-godot-scene.sh run res://Scenes/Validation/GameOS/Observation/ObservationLogValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Scenes/Validation/Runtime/Event/RuntimeEventValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run-main-smoke --log-dir .ai-temp/scene-tests/runs
```

结果：0 warning / 0 error；`Tools/run-build.sh` 会先生成 DataOS runtime snapshot；Runtime Event validation、普通 `Scenes/Main.tscn` headless 可玩验收和 `run-main-smoke` 均通过新结构 runner，analyzer 输出 `status: pass`、`combinedLog`、artifact 列表和 JSONL 数量；普通主场景输出 `BrotatoLike playable slice PASS` 且 `scene-acceptance.json` 为 `status=pass`，smoke 保留 `BrotatoLike GameOS smoke PASS`。
