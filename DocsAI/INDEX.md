# BrotatoLike DocsAI 索引

## 入口

- 游戏状态：`DocsAI/GameProjectState.md`
- 框架引用：`DocsAI/ExternalFrameworkMap.md`
- Godot 场景测试：`DocsAI/GodotSceneTesting.md`
- 迁移台账：`DocsAI/MigrationLedger.md`
- 功能迁移审计：`DocsAI/BrotatoMyFeatureMigrationAudit.md`
- 验证场景索引：`DocsAI/ValidationCatalog.md`

## 当前阶段

框架接入基线已完成，GodotBridge 第一版已编译接入，DataOS seed / runtime snapshot 已扩大到 TargetingIndicator、ChainAbility、旧 AbilityData 通用字段、Ability handler-specific 参数第三段、Feature、System、Spawn 和 ResourcePaths 第一批，`BrotatoLikeDataOSBootstrap` 已作为正式 snapshot 生成入口，并能构建 SpawnSystem 消费的敌人生成规则 catalog 和 RuntimeSchedule `SystemConfig`；`BrotatoLikeEnemySpawnSystem` 已消费该 catalog，`BrotatoLikeScheduledEnemySpawnSystem` 已通过 `RuntimeSchedule.Execute` 门禁驱动 Tick，实例化真实 Godot 敌人包装节点并写入 DataOS 字段；`BrotatoLikeGameRuntime` 已提取为主运行时节点并挂到 `Scenes/Main.tscn/GameRuntime`，普通运行路径初始化该节点并发布游戏启动事件，旧 Main 的初始化日志和 `Camera2D` 已补回，smoke 路径仍独立执行探针；玩家和近战敌人现在通过 `BrotatoLikeUnitProfiles` 调用框架 `GodotUnitComposer` 组合 visual、animation、orientation、AI、attack、hurtbox 和 contact damage adapter，游戏侧只额外挂输入和主动技能 adapter。

`restore-brotatolike-playable-ux` 已把功能迁移审计中的 P0/P1 基础体验补到可验证状态：普通 Main 和专项 validation 现在覆盖正式 `BrotatoLikeHUD`、玩家 HP、敌人头顶血条、四槽技能栏、`UseSkill/PreviousSkill/NextSkill` input action、`target_point_skill` 点选/确认/取消、伤害/治疗飘字、wave runtime completion、暂停菜单与 schedule gate、HP recovery、经验拾取、level-up 反馈，以及 25 个 legacy resource path 的 `legacyStatus` 分类门禁。`BrotatoLikePlayableSliceAcceptance` 继续输出 `BrotatoLike playable slice PASS/FAIL`、`scene-acceptance.json(status=pass/fail)` 和 `artifacts/logs/scene-log.jsonl`，但通过正式 runtime UI/progression 节点取证，不再用测试专用 `PlayableSliceHUD` 作为完成依据。剩余主要缺口是商店/道具/升级选项/meta progression、更多技能装配到玩家局内体验、连锁闪电线段视觉、完整多波曲线和真实设备 QA。`DocsAI/MigrationLedger.md` 和 `DocsAI/BrotatoMyFeatureMigrationAudit.md` 是后续迁移拆分事实源。

`BrotatoLikeAbilityHandlers` 已接入 `sine_wave_shot / boomerang_throw / bezier_shot / parabola_shot(CircularArc) / arc_shot(CircularArc) / orbit_skill` 的 `AbilityService -> FeatureHandler -> ProjectileTool -> MovementSystem` 执行闭环，接入 `dash` 的 `AbilityService -> FeatureHandler -> MovementSystem + EffectTool` 位移闭环，接入 `chain_lightning` 的 `AbilityService -> FeatureHandler -> DamageTool -> TimerManager` 延迟弹跳伤害闭环，并接入 `slam / circle_damage` 的 `AbilityService -> FeatureHandler -> DamageTool + EffectTool` 范围伤害与特效闭环。当前游戏 smoke、UnitComposition validation、Playable UX validation、Progression validation、LegacyResources validation 和 Main playable acceptance 覆盖 Runtime / DataOS bootstrap / Ability / Projectile / Effect / Movement handler authoring 参数、SineWave / Boomerang / BezierCurve / CircularArc / Orbit / Dash 真实 DataOS handler 执行、连锁闪电真实 DataOS handler 执行、猛击与圆环伤害范围伤害真实 DataOS handler 执行、Spawn catalog / `BrotatoLikeGameRuntime` / Main 正式启动事件 / RuntimeSchedule 门禁驱动的 SpawnSystem Tick 实例化 / Movement / Collision / Damage / ContactDamage / Attack / Godot AI bridge / Ability 点选目标 / Ability 自动索敌 / Projectile / Effect Runtime 与 Godot 实例化 / GodotBridge 接入。Feature、Ability Periodic 自动触发、AI 行为树和 Attack Runtime 结算由框架 Runtime tests 覆盖。`Tools/run-godot-scene.sh` 已委托 skill runner，支持 `index.json + per-scene result/combined/artifacts` 新日志结构。旧 `assets/` 已复制，旧 `Data/` 和 `Src/Main/` 已放入 `MigrationInput/`。

P3 `refactor-runtime-events-purge-game-leakage` 已把玩家主动技能输入事件迁到游戏侧：`Src/Game/Event/BrotatoLikeInputEvents.cs` 定义 `InputUseSkill / InputPreviousSkill / InputNextSkill`，`Src/Game/Bridge/BrotatoLikePlayerInputComponent.cs` 承接原输入桥接行为并发布 game-side events。框架侧 Bucket A 旧事件 `MouseSelection* / Wave* / GameStart / GameOver / GamePause / GameResume` 已删除且未在 BrotatoLike 创建替换；BrotatoLike 继续使用既有 `GameStarted` 游戏启动事件。
