# BrotatoLike DocsAI 索引

## 入口

- 游戏状态：`DocsAI/GameProjectState.md`
- 框架引用：`DocsAI/ExternalFrameworkMap.md`
- Godot 场景测试：`DocsAI/GodotSceneTesting.md`
- 迁移台账：`DocsAI/MigrationLedger.md`
- 整体迁移计划：`Plans/README.md`

## 当前阶段

框架接入基线已完成，GodotBridge 第一版已编译接入，DataOS seed / runtime snapshot 已扩大到 TargetingIndicator、ChainAbility、旧 AbilityData 通用字段、Ability handler-specific 参数第三段、Feature、System、Spawn 和 ResourcePaths 第一批，`BrotatoLikeDataOSBootstrap` 已作为正式 snapshot 生成入口，并能构建 SpawnSystem 消费的敌人生成规则 catalog 和 RuntimeSchedule `SystemConfig`；`BrotatoLikeEnemySpawnSystem` 已消费该 catalog，`BrotatoLikeScheduledEnemySpawnSystem` 已通过 `RuntimeSchedule.Execute` 门禁驱动 Tick，实例化真实 Godot 敌人包装节点并写入 DataOS 字段；`BrotatoLikeGameRuntime` 已提取为主运行时节点并挂到 `Scenes/Main.tscn/GameRuntime`，普通运行路径初始化该节点并发布游戏启动事件，旧 Main 的初始化日志和 `Camera2D` 已补回，smoke 路径仍独立执行探针；`BrotatoLikeAbilityHandlers` 已接入 `sine_wave_shot / boomerang_throw / bezier_shot / parabola_shot(CircularArc) / arc_shot(CircularArc) / orbit_skill` 的 `AbilityService -> FeatureHandler -> ProjectileTool -> MovementSystem` 执行闭环，接入 `dash` 的 `AbilityService -> FeatureHandler -> MovementSystem + EffectTool` 位移闭环，接入 `chain_lightning` 的 `AbilityService -> FeatureHandler -> DamageTool -> TimerManager` 延迟弹跳伤害闭环，并接入 `slam / circle_damage` 的 `AbilityService -> FeatureHandler -> DamageTool + EffectTool` 范围伤害与特效闭环。当前游戏 smoke 覆盖 Runtime / DataOS bootstrap / Ability / Projectile / Effect / Movement handler authoring 参数、SineWave / Boomerang / BezierCurve / CircularArc / Orbit / Dash 真实 DataOS handler 执行、连锁闪电真实 DataOS handler 执行、猛击与圆环伤害范围伤害真实 DataOS handler 执行、Spawn catalog / `BrotatoLikeGameRuntime` / Main 正式启动事件 / RuntimeSchedule 门禁驱动的 SpawnSystem Tick 实例化 / Movement / Collision / Damage / ContactDamage / Attack / Godot AI bridge / Ability 点选目标 / Ability 自动索敌 / Projectile / Effect Runtime 与 Godot 实例化 / GodotBridge 接入，Feature、Ability Periodic 自动触发、AI 行为树和 Attack Runtime 结算由框架 Runtime tests 覆盖。`Tools/run-godot-scene.sh` 已建立统一场景测试入口，支持日志目录和 Observation / Debug / Trace 环境变量。旧 `assets/` 已复制，旧 `Data/` 和 `Src/Main/` 已放入 `MigrationInput/`；`DocsAI/MigrationLedger.md` 已建立为迁移审计和后续拆分事实源，明确区分 `DataOS-only`、`遗留引用`、`部分迁移` 和真实可玩行为证据。下一步继续适配真实主场景 / UI / Feature actions，并继续迁入尚未接线的具体 Ability / Feature actions。
