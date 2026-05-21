# Brotato-my 功能迁移审计

> 日期：2026-05-21
> 范围：`Resources/Else/brotato-my` 旧项目功能意图，对照当前 `Games/BrotatoLike` 与 `SlimeAI.GameOS` 实现。
> 结论口径：这是功能体验迁移审计，不是旧代码复制计划。旧项目只作为当前任务参考，不作为长期事实源。

## 结论摘要

当前已经从“最小可玩核心链路”推进到“可验证的基础玩家体验闭环”：主场景启动、DataOS snapshot 装载、玩家生成、输入数据写入、玩家位移、第 1 波敌人生成、敌人追逐、接触伤害、死亡清理、`slam` / `chain_lightning` / `target_point_skill` 真实 input action 触发、`dash` 技能栏主路径触发、Chain Lightning Line2D 端点绑定、正式 HUD、头顶血条、四槽技能栏、loadout 结构化证据、伤害/治疗飘字、点选指示器、暂停菜单、HP recovery、经验拾取、scene-backed 经验条、level-up feedback 和升级三选一都有 Godot scene artifact 证据。

最新 Main 场景 artifact `.ai-temp/scene-tests/runs/2026-05-21/17-45-33/index.json` 显示 `BrotatoLike playable slice PASS`。专项 Playable UX artifact `.ai-temp/scene-tests/runs/2026-05-21/17-45-17/index.json` 显示正式 HUD/血条/技能栏/loadout override/点选/飘字/可见移动均 pass；Progression artifact `.ai-temp/scene-tests/runs/2026-05-21/17-45-01/index.json` 显示 wave completion、pause gate、HP recovery、经验拾取、level-up、经验条和升级三选一均 pass；LegacyResources artifact 显示 25 个旧 `res://Src/...` / `res://Data/...` path 已分类且没有 missing active legacy path。

这仍不能等价为旧项目所有功能体验迁移完成。剩余缺口主要是：商店波间调度 / 刷新与完整经济曲线 / 替换面板 / passive panel / meta progression、完整多波曲线和波间流程、真实物理设备专项 QA、角色扩展，以及旧测试/调试工具是否按 AI-first 方式重建。商店和道具本身已有第一版 scene-backed service/UI/validation，但还没有接入完整波间流程。

用户点名的几个问题需要按下面口径处理：

- 血条：基础正式功能已迁。`BrotatoLikeHUD` 挂载玩家 HP，敌人头顶血条会跟随敌人位置、监听 HP 变化并在死亡/清理后移除；旧实现的颜色体系、平滑插值和对象池样式未完全复刻。
- 玩家移动：自动化 `Input.ActionPress` 到 Runtime Data、Node2D 位移和视口可见性已验收。若手动运行仍表现为不能动，下一步应定位窗口焦点、真实手柄/键盘设备映射、摄像机跟随和人工 QA 环境，而不是把核心输入链路视为未迁。
- 施放技能：`UseSkill/PreviousSkill/NextSkill/ConfirmTarget/CancelTarget` 的 runner input action 已闭环到正式技能栏、冷却状态、`slam` / `chain_lightning` / `target_point_skill` 命中证据；真实物理手柄/鼠标专项仍待补充。

## 审计口径

状态词汇：

- `已完成体验迁移`：普通主场景存在正式功能，且有可重复验证 artifact 覆盖主要行为。
- `核心链路已迁`：GameOS/DataOS/Bridge 行为已接入并被测试覆盖，但正式玩家体验、UI 或完整玩法循环仍缺。
- `测试证据-only`：只在 runner 或 acceptance 里动态创建证据，不能当作正式功能。
- `DataOS-only`：seed/snapshot 有记录，但没有正式运行系统或场景体验。
- `资产-only`：资源文件已复制，但没有被目标功能稳定引用。
- `遗留引用`：snapshot 或 ResourceCatalog 仍指向旧 `res://Src/...`、`res://Data/...` 路径。
- `未迁`：当前没有新实现或正式目标。
- `废弃候选`：可能被 AI-first 框架的新测试/调试入口替代，需要后续写明废弃依据。

证据口径：

- Evidence：直接来自代码、DataOS seed、scene artifact 或测试命令输出。
- Inference：基于 Evidence 的工程判断，文中会写明。
- Unknown：当前未找到明确证据，不能猜测为已迁。

## 关键证据

旧项目证据：

- `Resources/Else/brotato-my/Src/ECS/UI/UI/HealthBarUI/HealthBarUI.cs:30` 订阅实体生成，`:81` 每帧跟随和平滑更新，`:156` 监听 HP 变化，`:220` 按实体世界坐标和 `HealthBarHeight` 放到头顶，`:236` 按阵营/品阶更新颜色。
- `Resources/Else/brotato-my/Src/ECS/UI/UI/DamageNumberUI/DamageNumberUI.cs:48` 显示伤害，`:76` 显示 MISS，`:88` 显示治疗，`:125` 随机偏移播放动画，`:134` 动画结束回池。
- `Resources/Else/brotato-my/Src/ECS/UI/UI/SkillUI/ActiveSkillBarUI.cs:13` 固定 4 槽，`:45` 绑定技能增删和选中事件，`:124` 刷新槽位，`:155` 高亮当前槽。
- `Resources/Else/brotato-my/Src/ECS/UI/UI/SkillUI/ActiveSkillSlotUI.cs:5` 说明槽位职责，`:46` 获取图标/冷却/充能/按键节点，`:68` 每帧更新冷却遮罩，`:171` 监听充能变化，`:224` 技能激活后显示冷却，`:234` 更新充能显示。
- `Resources/Else/brotato-my/Src/ECS/Base/Component/Player/ActiveSkillInputComponent/ActiveSkillInputComponent.cs:88` LB/RB 切技能，`:92` X 施放，`:159` 判断 Point 目标，`:193` 进入点选会话。
- `Resources/Else/brotato-my/Src/ECS/Base/System/TargetingSystem/TargetingManager.cs:44` 启用瞄准运行态，`:116` 开始瞄准，`:139` 生成指示器，`:149` 确认目标后触发技能，`:202` 玩家死亡取消瞄准。
- `Resources/Else/brotato-my/Src/ECS/Base/System/Spawn/SpawnSystem.cs:111` 开启波次，`:124` 创建波次时长 timer，`:151` 周期检查生成，`:157` 发 `WaveStarted`，`:171` 发 `WaveCompleted`。
- `Resources/Else/brotato-my/Src/ECS/Base/System/RecoverySystem/RecoverySystem.cs:100` 注册恢复实体，`:163` 每秒 timer，`:192` 批量处理，`:247` HP 恢复，`:263` Mana 恢复。
- `Resources/Else/brotato-my/Src/ECS/Base/System/PauseMenu/PauseMenuSystem.cs:31` 初始化暂停菜单 UI，`:51` 轮询暂停输入，`:85` 切换 Pause overlay，`:104` 恢复按钮。
- `Resources/Else/brotato-my/Src/ECS/Base/Component/Collision/PickupComponent/PickupComponent.cs:3` 明确旧拾取也是占位，`:14` 注册时关闭监控。
- `Resources/Else/brotato-my/Data/DataNew/Ability/AbilityData.cs:19` 到 `:195` 定义了 `Slam / TargetPointSkill / OrbitSkill / SineWaveShot / ParabolaShot / BoomerangThrow / ArcShot / BezierShot / Dash / CircleDamage / AuraShield`。
- `Resources/Else/brotato-my/Data/DataNew/Unit/Enemy/EnemyData.cs:18` 有 `ExpReward`，`:84` 定义 `Yuren`，`:102` 定义 `Chailangren`。

当前项目证据：

- `Games/BrotatoLike/Src/Game/Main.cs` 普通运行走 `StartGameRuntime()`，只有 runner artifact 环境才执行 acceptance 并退出；acceptance 现在观察正式 HUD/targeting/progression 节点，不再动态创建 `PlayableSliceHUD` 作为完成证据。
- `Games/BrotatoLike/Src/Game/BrotatoLikeSkillLoadoutAuthoring.cs` 集中定义默认 loadout、可获得技能池、passive ids 和 validation override；`Games/BrotatoLike/Src/Game/BrotatoLikeGameRuntime.cs` 初始化 DataOS/Spawn schedule、生成玩家、进入 Gameplay、挂载正式 HUD/targeting/progression，并通过该 authoring 授予玩家 `slam / chain_lightning / target_point_skill / dash` 四个默认可见技能。
- `Games/BrotatoLike/Src/Game/UI/BrotatoLikeHud.cs` 提供 `BrotatoLikeHUD`、`PlayerHealthLabel`、`ActiveSkillBar`、`SkillSlot0..3`、`HeadHealthBarLayer`、`DamageNumberLayer`、`ProgressionSummary`。
- `Games/BrotatoLike/Src/Game/BrotatoLikeTargetingController.cs` 提供 `PointTargetingIndicator` 和 `PointTargetingSession`；Point 技能开始点选时不消耗冷却，确认后把目标点传给 `AbilityService.TryTrigger`。
- `Games/BrotatoLike/Src/Game/Progression/BrotatoLikeProgressionService.cs` 提供 wave runtime state、`BrotatoLikePauseMenu`、`RecoveryTickService`、`ExperiencePickupLayer` 和 `LevelUpFeedback`。
- `Games/BrotatoLike/Src/Game/GodotActiveSkillInputComponent.cs` 记录 `LastTriggerReport`，并把 Point 技能委托给 game-side targeting controller。
- `Games/BrotatoLike/project.godot` 已补 `ConfirmTarget`、`CancelTarget`、`PauseGame`，保留 WASD、方向键、手柄摇杆/方向键、空格/手柄 X、Q/E、LB/RB。
- Playable UX artifact：`.ai-temp/scene-tests/runs/2026-05-21/16-01-37/001_Src_Validation_Game_PlayableUX_BrotatoLikePlayableUXValidation.tscn_attempt1/artifacts/brotatolike-playable-ux-validation.json`，`status=pass`，正式 HUD/血条/技能栏/loadout override/点选/伤害飘字/可见移动均通过；check details 记录 12 owned / 4 visible / 8 hidden。
- Progression artifact：`.ai-temp/scene-tests/runs/2026-05-21/17-45-01/001_Src_Validation_Game_Progression_BrotatoLikeProgressionLoopValidation.tscn_attempt1/artifacts/brotatolike-progression-loop-validation.json`，`status=pass`，wave completion、pause gate、HP recovery、经验拾取、level-up 反馈、scene-backed 经验条、scene-backed 升级三选一、属性奖励、技能奖励和升级门禁均通过。
- Shop artifact：`.ai-temp/scene-tests/runs/2026-05-21/18-39-01/025_Src_Validation_Game_Shop_BrotatoLikeShopItemValidation.tscn_attempt1/artifacts/brotatolike-shop-item-validation.json`，`status=pass`，DataOS `item_definition / shop_offer` authoring、未知 effect target 拒绝、确定性 offer、可负担购买、买不起拒绝、Runtime Data 效果和 scene-backed UI cleanup 均通过。
- LegacyResources artifact：`.ai-temp/scene-tests/runs/2026-05-20/11-33-42/001_Src_Validation_Game_LegacyResources_BrotatoLikeLegacyResourceClassificationValidation.tscn_attempt1/artifacts/brotatolike-legacy-resource-classification-validation.json`，`legacyCount=25`、`unsupportedStatusCount=0`、`missingActiveLegacyCount=0`。
- Main artifact：`.ai-temp/scene-tests/runs/2026-05-21/16-05-14/001_Scenes_Main.tscn_attempt1/artifacts/scene-acceptance.json`，`status=pass`，`expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 均非空，`failureReasons=[]`，记录 `skill_loadout_source=default`、owned ids、visible slot ids、selected id、total count 和 available skill pool。

## 功能迁移矩阵

| 功能域 | 旧功能意图 | 当前状态 | 缺口判断 | 决策 |
| --- | --- | --- | --- | --- |
| 主场景启动 | 旧 `Main` 发游戏开始事件，系统按事件进入运行态。 | 核心链路和基础体验已迁。普通 Main 初始化 runtime、生成玩家、进入 Gameplay、挂正式 HUD/targeting/progression；smoke/acceptance 分离；商店/道具有独立 service/UI/validation entry。 | 完整多波、波间商店调度和 meta progression 仍缺。 | Keep：继续以当前 `BrotatoLikeGameRuntime` 为正式入口，不复刻旧场景树。 |
| 玩家生成与视觉 | 旧 PlayerEntity/Preset 组合单位数据、移动、生命、动画、输入。 | 核心链路已迁。当前 DataOS `unit.player/deluyi` + `GodotUnitComposer` + deluyi visual。 | 只生成 Deluyi；`bubing/guangfa` 资源是资产-only，没有角色选择或数据接线。 | Adopt Later：先完成 Deluyi 可玩体验，再扩展角色。 |
| 玩家移动控制 | 旧项目依赖玩家输入组件/移动组件。 | 已完成体验迁移。artifact 证明 input action、InputDirection、LastMoveDirection、Node2D 位置变化和可见性。 | 真实手动窗口焦点和物理设备 QA 仍需人工/设备专项。 | Keep：核心链路已过，后续做真实设备 QA。 |
| 摄像机/视口 | 旧 Main 有 Camera；未看到复杂 camera follow 逻辑。 | 部分迁移。当前 Main 有 `Camera2D`，Playable UX 验证记录玩家移动可见性。 | camera follow、缩放、边界等高级体验未设计。 | Adopt Later：需要地图/关卡边界时再设计。 |
| 正式 HUD/UI 根 | 旧项目有 `UIManager`、`UIBase`、主题、对象池绑定。 | 已完成基础体验迁移。`BrotatoLikeHUD` 由 production runtime 挂载，不由 acceptance 伪造。 | 旧 UIManager/主题/对象池不复制；复杂样式后续新设计。 | Keep：AI-first HUD 已接入。 |
| 头顶血条 | 旧血条自动绑定单位、跟随位置、监听 HP、平滑插值、按阵营/品阶变色。 | 已完成基础体验迁移。敌人头顶血条更新位置/数值并清理。 | 旧颜色规则、平滑插值、对象池优化待后续增强。 | Keep。 |
| 伤害飘字 | 旧 DamageNumberUI 支持普通/暴击/魔法/真实/治疗/MISS，动画结束回池。 | 已完成基础体验迁移。正式伤害/治疗数字节点从 damage/heal 事件生成并清理。 | 暴击/MISS/更多类型颜色和动画样式待后续增强。 | Keep。 |
| 技能栏与技能槽 | 旧技能栏 4 槽、图标、名称、高亮、冷却遮罩、充能、按键提示。 | 已完成基础体验迁移。正式四槽技能栏显示 owned abilities、选中、高亮、cooldown/charge fallback，并输出 loadout source、owned ids、visible slot ids、selected id、total/visible/hidden count。 | 图标、复杂充能表现、手柄提示可继续完善；超过四槽的普通获得流程交给升级/商店系统。 | Keep。 |
| 主动技能切换/施放 | 旧 LB/RB 切换，X 施放。 | 已完成基础体验迁移。`UseSkill/PreviousSkill/NextSkill` 通过 Godot input action 到正式 UI/AbilityService 闭环。 | 真实物理手柄/鼠标专项和失败原因 UI 待后续。 | Keep。 |
| Point 目标技能 | 旧 Point 技能进入 TargetingManager，会生成指示器，确认后才 TryTrigger。 | 已完成基础体验迁移。`target_point_skill` 默认装配，进入 `BrotatoLikeTargetingController`，确认后才触发。 | 鼠标/手柄移动目标和高级视觉仍待真实设备专项。 | Keep。 |
| Slam | 旧主动范围伤害技能。 | 核心链路和正式 UI 证据已迁。当前玩家默认拥有，artifact 覆盖触发、冷却、命中、Effect 和技能栏冷却。 | 升级/数值成长展示待后续。 | Keep：作为主动技能验收样板。 |
| Chain Lightning | 旧连锁闪电技能。 | 已完成当前体验迁移。当前玩家默认拥有，artifact 覆盖目标选择、触发、延迟命中、三段 Line2D 端点绑定和清理。 | 像素级美术样式、淡出材质和升级强化待后续增强。 | Keep。 |
| 其他投射物/位移技能 | 旧数据包含正弦波、回旋镖、贝塞尔、定点抛炸弹、圆弧、冲刺、环绕、护盾。 | 已完成专项行为验收。`dash` 已装到玩家四槽技能栏；`sine_wave_shot / boomerang_throw / bezier_shot / parabola_shot / arc_shot / orbit_skill / aura_shield` 通过 `BrotatoLikeSkillValidation` 记录 scene path、movement mode、轨迹位移、命中/伤害和 cleanup；升级三选一首版可把 `sine_wave_shot` 加入 hidden owned ability。 | visible slot 替换、商店获得和 passive panel 仍未实现；当前不是“默认四槽可玩”。 | Keep for handler/validation；扩展获得和替换流程交给后续 shop/item/panel changes。 |
| 被动/周期技能 | 旧有永久/周期触发概念。 | 已完成专项行为验收。`orbit_skill` 记录 3 个 Orbit projectile、碰撞/伤害和 max-duration cleanup；`circle_damage` 记录半径内敌人扣血、范围外/同队不受伤、光环 effect 和 cleanup；`aura_shield` 记录 AttachToHost 跟随、contact damage 和 cleanup。 | 普通主场景 passive panel、获得流程和更完整周期 UI 仍缺。 | Keep for runtime behavior；P1 Adopt Later for acquisition/UI。 |
| 敌人生成 | 旧 SpawnSystem 按波次、计时器、规则生成敌人并发 Wave 事件。 | 部分迁移。当前第 1 波生成 2 个豺狼人、3 个鱼人，Progression artifact 证明 wave completion state。 | 多波曲线、WaveStarted/WaveCompleted 对外事件、波间奖励/商店、随机策略仍缺。 | P1 Adopt Later：继续扩完整局内循环。 |
| 敌人 AI/接触伤害 | 旧敌人单位有 AI、攻击、碰撞、伤害盒。 | 核心链路已迁。artifact 覆盖追逐方向、位置变化、玩家 HP 下降。 | 缺多敌种行为差异、动画状态、攻击前后摇的普通主场景体验验收。 | P1 Adopt Later。 |
| 生命/伤害/死亡清理 | 旧 HealthComponent/DamageService/Lifecycle 处理 HP、伤害和销毁。 | 核心链路和基础表现已迁。artifact 记录伤害日志、敌人死亡、QueueFree、正式血条、飘字和经验拾取。 | 死亡动画、掉落种类、奖励展示仍可扩展。 | Keep。 |
| 恢复系统 | 旧 RecoverySystem 每秒处理 HP/Mana regen。 | 已完成 HP 基础迁移。Progression artifact 证明 HP recovery 和 dead skip；mana 因当前 active catalog 无数据记录为 `not-applicable`。 | 未来若接 mana，需要补 mana recovery/UI 验证。 | Keep。 |
| 拾取/掉落/经验/升级 | 旧 EnemyData 有 `ExpReward`；旧 PickupComponent 本身也是占位。 | 已完成首版局内成长闭环。敌人死亡生成经验拾取，玩家收集后经验/等级更新，有 scene-backed 经验条、level-up feedback 和升级三选一；属性奖励和技能奖励已验证。 | 替换选择、被动面板、连续多次升级队列和 meta progression 未实现；商店/道具第一版已单独验证。 | P1 Adopt Later：继续拆替换面板、被动面板、波间调度和 meta progression。 |
| 商店 / 道具 / 经济 | 旧体验需要 item pool、货币、购买、道具效果和波间商店。 | 已完成第一版闭环。DataOS seed authoring 导出 `shop_item_authoring.json`；`BrotatoLikeShopService` 管理货币、deterministic offer、购买门禁和 effect application；`ShopPanelUI` / `ShopOfferCardUI` scene-backed。 | 未接完整波间自动打开、刷新/锁定、售卖、随机权重、掉落货币和经济曲线。 | Keep：保留当前 service/UI/validation，后续接 wave flow 和经济调参。 |
| 暂停菜单 | 旧 PauseMenuSystem 是 CanvasLayer，Esc/Start 切换 overlay，有恢复按钮。 | 已完成基础体验迁移。正式 pause menu 和 schedule gate 已验证，pause 阻断 tick，resume 恢复。 | 菜单焦点导航、按钮行为、样式和真实设备输入仍待扩展。 | Keep。 |
| MouseSelection/调试选择 | 旧有鼠标选择系统/测试工具。 | DataOS-only 或废弃候选。 | AI-first 里应改成 Observation/Validation/debug overlay，不必复刻旧调试 UI。 | P2 Decide：需要则重写为调试工具。 |
| 旧 TestSystem/VisualPreview | 旧项目有大量可视测试场景。 | 部分替换。当前统一 runner、scene artifact、GameOS tests 已覆盖大量运行时。 | 旧可视测试没有逐一迁移；需要按新 Observation contract 重建有价值测试。 | P2 Replace：不复制旧测试面板。 |
| ResourceCatalog legacy path | 旧 ResourcePaths 被写入 DataOS resources。 | 已完成分类门禁。25 个旧 `res://Src/...` / `res://Data/...` 路径已分类，unsupported=0，missing active=0。 | 分类不等于迁移旧资源功能；后续启用时仍需替换为新路径或明确废弃。 | Keep。 |
| 资产 | 旧玩家/敌人/投射物/特效资源已复制到 `assets/`。 | 资产-only 到部分迁移。Deluyi、Yuren、Chailangren、部分 projectile/effect 已使用。 | Bubing/Guangfa、多特效、多投射物未全部接到 gameplay；连锁闪电线特效缺。 | P1 Adopt Later：随功能接线，不一次性搬场景。 |

## P0 已关闭功能

1. 正式 HUD 与血条
   - 结果：普通 `Scenes/Main.tscn` 运行时挂载 `BrotatoLikeHUD`，不依赖 acceptance 动态 Label。
   - 验收：Playable UX artifact 记录玩家 HP、敌人头顶血条数值/位置更新和死亡清理，`status=pass`。

2. 玩家移动手动体验基础验证
   - 结果：runner `Input.ActionPress` 到 `Movement.InputDirection`、`Movement.LastMoveDirection`、Node2D 位移和玩家可见性已闭环。
   - 剩余：真实窗口焦点、物理键盘/手柄、鼠标/摇杆设备属于后续 QA。

3. 主动技能真实输入专项
   - 结果：`UseSkill`、`PreviousSkill`、`NextSkill` 通过 Godot input action 验收，技能栏高亮和冷却 UI 同步。
   - 验收：Main artifact 记录 `slam`、`chain_lightning` 和 `target_point_skill` 触发/命中/冷却证据。

4. Point targeting
   - 结果：点选状态、TargetingIndicator、射程限制、确认/取消、确认后才 `AbilityService.TryTrigger` 已迁。
   - 验收：Playable UX artifact 记录 confirm 前 cooldown 不变、confirm 后成功触发、cancel cleanup。

5. Legacy resource gate
   - 结果：`resources[]` 中 25 个旧 `res://Src/...` / `res://Data/...` 路径已有 `legacyStatus` 分类。
   - 验收：LegacyResources artifact 记录 `legacyCount=25`、`unsupportedStatusCount=0`、`missingActiveLegacyCount=0`。

## P1 剩余功能

1. 完整波次循环：当前已有 wave runtime completion；仍缺多波曲线、WaveStarted/WaveCompleted 对外事件、波间状态，以及把已完成的 shop service 接入波间奖励。
2. 更多技能逐项验收：`dash` 已进入玩家四槽；`sine_wave_shot / boomerang_throw / arc_shot / bezier_shot / parabola_shot / orbit_skill / circle_damage / aura_shield` 已进入显式 skill pool / validation override，但仍需要逐技能主场景可玩验收。
3. 连锁闪电视觉增强：端点绑定、线段生命周期和延迟弹跳可视已过；像素级样式、淡出和升级强化可后续补。
4. Pause menu 体验增强：基础菜单和 schedule gate 已迁；仍缺焦点导航、按钮行为、样式和真实设备输入 QA。
5. Recovery 扩展：HP recovery 已迁；mana recovery 因当前 active catalog 无 mana 数据为 `not-applicable`，后续如恢复 mana 需新增 UI/验证。
6. 掉落/经验/升级扩展：经验拾取、level-up feedback、正式经验条、升级三选一和商店/道具首版已迁；仍缺替换选择、被动面板、连续多次升级队列和 meta progression。

## P2 待决策功能

1. MouseSelection、VisualPreview、旧 TestSystem：默认不要复刻旧 UI；按 AI-first Observation/Validation/debug overlay 重建有价值部分。
2. 角色扩展：`bubing`、`guangfa` 资源已在 assets，但没有数据和角色选择。
3. 旧 UIManager/对象池：是否保留 UI 对象池要看正式 HUD 复杂度；不要直接复制旧 UIManager。

## 不要误判为已完成

- DataOS record 不等于功能完成。`system.config/UIManager`、`PauseMenuSystem`、`RecoverySystem` 需要 scene artifact 才能算功能；本轮已为 HUD、pause、recovery 补了专项 artifact。
- Handler 注册不等于玩家能用。`BrotatoLikeAbilityHandlers.RegisterAll()` 覆盖多个技能；当前玩家默认四槽是 `slam / chain_lightning / target_point_skill / dash`，其他技能目前是显式 skill pool / validation override 候选，仍需逐技能主场景验收。
- Asset copied 不等于资源接线。`assets/` 下资源存在，不代表主场景会生成、播放、回收。
- Scene smoke 不等于正式 UI。正式 UI 迁移要看 `BrotatoLikeHUD` / Playable UX / Main artifact；不能只看 smoke。
- 自动索敌不等于 Point targeting。当前已有 point targeting controller，但后续新增 Point 技能仍必须验证确认前不消耗、确认后触发和取消清理。
- runner 的 `Input.ActionPress` 证明输入链可工作，但不能完全覆盖用户手动窗口焦点、手柄硬件映射和鼠标/摇杆手感问题。

## 建议后续变更

`restore-brotatolike-playable-ux` 已覆盖 P0 和部分 P1。后续建议拆成更窄的 OpenSpec change：

- `validate-brotatolike-projectile-and-passive-skills`：复用 deterministic validation loadout，逐个补投射物/被动技能可视、命中、生命周期和清理 artifact。
- 后续 wave/shop integration change：把已验证的 shop service 接入 wave break，补刷新/锁定、货币来源和经济曲线。
- 后续 progression/panel change：补 visible slot 替换、被动面板、连续多次升级队列和 meta progression。
- `brotatolike-manual-device-qa`：补真实手柄、鼠标点选、窗口焦点和人工可玩 checklist。

## 本次验证记录

最新 level-up choice loop 验证已读取：

- Build：`Tools/run-build.sh` PASS（DataOS validation PASS，26 个既有 XML comment warnings，0 errors）。
- Progression：`.ai-temp/scene-tests/runs/2026-05-21/17-45-01/index.json` PASS，artifact `brotatolike-progression-loop-validation.json` 为 `status=pass`、`failureReasons=[]`，记录 scene-backed 经验条、scene-backed 升级三选一、属性奖励、技能奖励和 `ModalUi/Suspended -> None/Running` 门禁。
- PlayableUX：`.ai-temp/scene-tests/runs/2026-05-21/17-45-17/index.json` PASS。
- Main scene：`.ai-temp/scene-tests/runs/2026-05-21/17-45-33/index.json` PASS。
- Scene gate：上述三个 run 的 `index.json`、`result.json` 和 scene artifact 均已检查，`expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 均非空。

本次审计期间已读取并确认最新验证 artifact：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-build.sh
Tools/run-godot-scene.sh run res://Src/Validation/Game/PlayableUX/BrotatoLikePlayableUXValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Src/Validation/Game/Progression/BrotatoLikeProgressionLoopValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Src/Validation/Game/LegacyResources/BrotatoLikeLegacyResourceClassificationValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

结果：

- `Tools/run-build.sh`：PASS，`0 Warning(s), 0 Error(s)`。
- Playable UX：PASS，artifact 位于 `.ai-temp/scene-tests/runs/2026-05-20/11-32-49/index.json`，正式 HUD/血条/技能栏/点选/飘字/可见移动均通过。
- Progression Loop：PASS，artifact 位于 `.ai-temp/scene-tests/runs/2026-05-20/11-33-15/index.json`，wave completion、pause gate、HP recovery、经验拾取和 level-up 反馈均通过；mana recovery 为 `not-applicable`。
- Legacy Resources：PASS，artifact 位于 `.ai-temp/scene-tests/runs/2026-05-20/11-33-42/index.json`，`legacyCount=25`、`unsupportedStatusCount=0`、`missingActiveLegacyCount=0`。
- Main scene：PASS，artifact 位于 `.ai-temp/scene-tests/runs/2026-05-20/11-33-55/index.json`，正式 HUD 与 Point targeting 已进入普通主场景验收。
- Scene gate：上述 `index.json`、`result.json`、scene artifact 均已检查，`expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 均非空，`failureReasons=[]`。
