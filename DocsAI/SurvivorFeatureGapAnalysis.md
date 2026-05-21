# BrotatoLike 幸存者类功能缺口深度分析

> 日期：2026-05-21  
> 目标：把 BrotatoLike 从当前可验证基础切片，推进到更完整的幸存者 / bullet heaven / Brotato-like 产品闭环。  
> 口径：迁移的是功能思想、玩法结构、AI-first 设计方法和验证方式，不迁移外部代码、资产或数据。  
> 主要参考：`Resources/Games/Games/Brotato` 本地解包分析、当前 `Games/BrotatoLike/DocsAI`、公开 Steam / Wiki 资料和同类游戏设计。  

## 1. SystemAgent 流程记录

### 1.1 Selected workflow

本分析按 `Workspace/SystemAgent/Workflows/ResearchAdoption.md` 执行，角色口径为 ResearchAnalyst + Reviewer。输出不直接进入实现，不直接修改框架代码，不复制外部项目代码或资产。

### 1.2 Must-read 状态

| 路径 | 状态 | 用途 |
| --- | --- | --- |
| `Workspace/SystemAgent/README.md` | 已读 | SystemAgent 唯一事实源根与入口顺序 |
| `Workspace/SystemAgent/INDEX.md` | 已读 | 选择 ResearchAdoption workflow |
| `Workspace/SystemAgent/Workflows/ResearchAdoption.md` | 已读 | 外部资料研究采纳规则 |
| `Workspace/SystemAgent/Roles/ResearchAnalyst.md` | 已读 | Evidence / Inference / Unknown 与采纳决策要求 |
| `Workspace/SystemAgent/Policies/ExternalResources.md` | 已读 | `Resources/*` 只作当前任务参考 |
| `Workspace/SystemAgent/Gates/ReviewGates.md` | 已读 | 文档 / retrospective gate 口径 |
| `Workspace/SystemAgent/Gates/VerdictVocabulary.md` | 已读 | verdict 词表 |
| `Workspace/SystemAgent/Config/review-mode.txt` | 已读 | 当前为 `lean` |
| `Resources/Games/Games/Brotato/README.md` | 已读 | Brotato 参考资源索引 |
| `Resources/Games/Games/Brotato/Docs/skill-game-analysis.md` | 已读 | Brotato 本地分析索引 |
| `Games/BrotatoLike/DocsAI/INDEX.md` | 已读 | 游戏侧文档入口 |
| `Games/BrotatoLike/DocsAI/GameProjectState.md` | 已读 | 当前游戏状态与验证证据 |
| `Games/BrotatoLike/DocsAI/MigrationLedger.md` | 已读 | 迁移事实源 |
| `Games/BrotatoLike/DocsAI/BrotatoMyFeatureMigrationAudit.md` | 已读 | 旧功能意图对照当前实现 |
| `Games/BrotatoLike/DocsAI/UnfinishedMigrationHandoff.md` | 已读 | 未完成迁移接手边界 |

### 1.3 External resources 记录

```yaml
externalResources:
  enabled:
    - game-reference
  scope:
    - Resources/Games/Games/Brotato/README.md
    - Resources/Games/Games/Brotato/Docs/
    - Resources/Games/Games/Brotato/Unpacked 最小统计与结构核对
    - Steam / Wiki 公开页面：Brotato、Vampire Survivors、Deep Rock Galactic: Survivor、Halls of Torment、20 Minutes Till Dawn、Soulstone Survivors
  reason: 对照成熟幸存者类游戏的产品结构、构筑系统、波次节奏、Meta progression、验证与 AI-first 通用框架落点。
  expires: current-task
copiedCodeOrAssets: none
```

## 2. 研究证据摘要

### 2.1 Evidence

- Brotato 本地分析记录其核心循环为选择角色、存活波次、波间商店构筑、武器升级、击败 Boss；本地解包规模为 475 个 `.gd`、354 个 `.tscn`、2985 个 `.tres`，说明该类型的完整产品依赖大量数据驱动内容，而不是只依赖几个 runtime 系统。
- Brotato 本地分析显示其关键系统包括 RunData、PlayerRunData、ProgressData、ItemService、WeaponService、WaveManager、Shop、Character、EnemyAI、VFX、UI/Input。
- Brotato Wiki 公开资料显示：商店在每波后自动进入；商品不是完全随机，会受角色、Luck、已有武器等影响；早期商店有特殊限制；稀有度随波次增长；可 reroll 和 lock。
- Brotato Wiki 公开资料显示：多数角色最多持有 6 把武器；武器有近战 / 远程、4 个稀有度，同级相同武器可以合成更高一级。
- 当前 BrotatoLike 已有角色选择、两波 deterministic RunFlow、首版 Shop / Item、升级三选一、12 项 skill pool、逐技能专项验证、HUD、血条、伤害数字、点选技能、暂停门禁和 release-batch 证据。
- 当前 BrotatoLike 真实设备 QA 仍是 `not-tested`；自动 `Input.ActionPress` 不能替代物理键鼠、鼠标、手柄、窗口焦点和 UI focus pass。
- Deep Rock Galactic: Survivor 的 Steam 页面强调 survivor-like auto-shooter、自动开火、采矿、程序洞穴、敌人波、升级装备、每局独特；公开 Wiki 资料强调 class / weapon pool / upgrade card / overclock / artifact / biome / hazard 等组合。
- Halls of Torment 的 Steam 页面列出 quest-based meta progression、abilities / traits / items synergy、unique bosses、多个角色、多个地下世界、可带回地面的 unique items、class power、rare item variants、late game Shrine，以及 6 stages、11 characters、25 blessings、60 items、240 rare variants、74 abilities、30 artifacts、35+ bosses、70+ monsters、500 quests、1000+ traits。
- Vampire Survivors Wiki 公开资料显示 evolution / union / gift / morph 等武器进化结构；Arcanas 是跨武器 / 跨角色的构筑修改层。
- 20 Minutes Till Dawn Wiki 公开资料显示其有角色、武器、永久 runes、武器专属高阶升级；它说明同类游戏可以把“主动瞄准 / 手动射击 / reload”作为差异化，而不是所有幸存者类都必须完全自动。
- Soulstone Survivors Steam / Wiki 公开资料显示其强调大量 skills、boss、skill tree、runes、weapons、curses、maps 和 game modes。

### 2.2 Inference

- BrotatoLike 当前已经越过“能跑起来”的阶段，下一阶段最大缺口不是单点能力，而是产品闭环：完整 run lifecycle、构筑来源、经济曲线、内容规模、Meta progression、难度挑战和验证矩阵。
- AI-first 通用框架不应该直接包含 BrotatoLike 的具体武器、商店规则、角色名或道具池；应沉淀通用 capability、typed DataKey、DataOS authoring schema、validator、director 接口、Observation artifact 和测试模板。
- 游戏侧应保留具体玩法内容：角色、武器、道具、波次、商店价格、掉落权重、关卡主题、Boss 技能、UI 风格、Meta 条件和调参数据。
- 每个功能都应该先以 OpenSpec change 建立验收标准，再实现 DataOS / Runtime / UI / Validation 四层闭环；单靠 DataOS record、handler 注册或 scene smoke 不能声明完成。

### 2.3 Unknown

- 当前没有物理设备 QA pass 证据，不能断言手柄、鼠标点选、窗口焦点和 UI focus 已达到产品级手感。
- 当前没有长局 20 波 / 高压力对象池 / 大量投射物 / 大量敌人性能 artifact，不能断言完整幸存者密度已经稳定。
- 当前没有完整数值平衡报告，不能判断 HP、伤害、经济、经验、商店价格、升级权重和敌人曲线是否形成可持续挑战。
- 当前没有 Meta 存档 / 解锁 / 难度 / 成就的长期稳定 schema，不能把现有角色选择和局内成长等同于 roguelite 长期进度。

## 3. 当前 BrotatoLike 基线

### 3.1 已有可验证能力

| 功能域 | 当前证据 | 判断 |
| --- | --- | --- |
| 主场景 runtime | `BrotatoLikeGameRuntime` 已作为主运行时节点，Main 默认 fallback 到 `character:deluyi` | 基础入口成立 |
| 角色选择 | `character_definition / character_loadout`，`deluyi / guangfa` 两名角色，scene-backed UI artifact 通过 | 首版成立 |
| 玩家输入 | 移动、技能切换、技能释放、数字键直选、点选确认 / 取消已有自动化 artifact | 自动化路径成立，真实设备未验 |
| HUD / 血条 / 飘字 | 正式 HUD、玩家 HP、敌人头顶血条、伤害 / 治疗数字已有 PlayableUX / Main artifact | 基础表现成立 |
| 技能系统 | 默认四槽、12 项 skill pool、逐技能 `BrotatoLikeSkillValidation` 覆盖 projectile / passive / dash / chain line 等 | 技能 handler 与专项验收成立 |
| RunFlow | 两波 finite deterministic entries，第 1 波完成进入 `RewardShop` hook，第 2 波可启动 | 首版多波成立 |
| Shop / Item | `item_definition / shop_offer`、首批 3 个道具、确定性购买、货币门禁、scene-backed UI | 首版 service/UI 成立 |
| Progression | 经验拾取、经验条、level-up feedback、升级三选一、属性奖励、hidden ability grant | 首版局内成长成立 |
| Pause / Recovery | pause gate、HP recovery、dead skip 已验证 | 基础成立 |
| Release batch | 历史 26 scenes release-batch 已有 pass 记录 | 当前验证体系可用 |

### 3.2 不能误判为完成的内容

- 两波 deterministic authoring 不等于完整 20 波 / endless / boss / elite / horde。
- Shop service 和 Shop UI 不等于完整波间商店体验；还缺自动打开、reroll、lock、sell、ban、权重、货币来源和经济曲线。
- `AvailableSkillPoolAbilityIds` 不等于玩家普通局内能自然获得和替换技能。
- hidden owned ability 不等于 visible slot 替换 / passive panel / build inspect UI 已完成。
- 角色选择首版不等于完整局外菜单、unlock、存档、难度选择和成就系统。
- 自动 runner input 不等于真实设备 QA。
- 历史 artifact 只能说明当时通过；新功能声明需要新 run 和新 artifact。

## 4. 完整幸存者类游戏目标模型

幸存者类游戏的完整体验不是“自动攻击 + 敌人靠近”这么简单。成熟产品通常由四层组成：

1. **Run 内循环**：移动、自动攻击 / 主动技能、拾取经验、升级选择、构筑成长、波次压力、Boss / Elite、失败 / 胜利。
2. **波间 / 局内经济**：商店、reroll、lock、购买、出售、材料、掉落、稀有度、权重、武器合成、道具效果。
3. **局外长期进度**：角色解锁、武器 / 道具解锁、难度、挑战、成就、统计、存档迁移、永久升级。
4. **内容生产与验证**：DataOS authoring、schema、validator、内容 lint、数值模拟、scene artifact、release-batch、性能门禁、手感 QA。

对 SlimeAI AI-first GameOS 来说，目标不是把 BrotatoLike 的玩法写死进框架，而是把下面这些通用能力沉淀为可复用接口：

| 通用能力 | 框架职责 | 游戏侧职责 |
| --- | --- | --- |
| Run lifecycle | typed state、phase gate、pause/suspend/resume、win/lose/end event、snapshot | 具体波次长度、胜利条件、失败 UI、奖励规则 |
| Director | spawn budget、spawn request、difficulty curve interface、Observation | 敌人池、Boss、Elite、地图规则、具体曲线 |
| Build system | ability / feature / item / weapon 的组合协议、source tracking、modifier aggregation | 具体技能、武器、道具、套装、角色机制 |
| Economy | 通用 price / currency / reroll / lock / offer model 可选抽象 | 货币来源、价格公式、商店权重、售卖规则 |
| Meta progression | unlock condition DSL、save schema、migration、statistics events | 解锁内容、成就文案、难度曲线、奖励 |
| Validation | BDD 五字段、artifact schema、batch runner、gate report | 具体 validation scenes、pass/fail criteria、内容覆盖表 |

## 5. 功能缺口矩阵

### P0-01 完整 Run Lifecycle：20 波、胜利、失败、重开、结算

| 字段 | 内容 |
| --- | --- |
| 功能思想 | 幸存者类游戏需要一个清晰的单局生命周期：准备、运行、波间、暂停、死亡、复活或失败、胜利、结算、重开。没有这个闭环，后续商店、难度、Meta 和统计都缺少承载点。 |
| 参考来源 | Brotato 是 20 波短局，波间商店构筑，最终 Boss / 胜利；Vampire Survivors 以生存时长和解锁驱动；Halls of Torment 和 Soulstone Survivors 都围绕 run 完成、boss、挑战和长期奖励组织内容。 |
| 当前证据 | BrotatoLike 目前有两波 finite deterministic entries，第 1 波完成后进入 `RewardShop` hook，第 2 波可启动；pause、respawn、cleanup 有 RunFlow artifact。 |
| 缺口判断 | 缺完整 20 波配置、正式胜利 / 失败状态、结算 UI、restart 流程、run summary、奖励发放、长期统计事件。 |
| AI-first GameOS 通用设计 | 框架应提供 `RunLifecycleService` 或等价 Runtime Process：typed phase、phase transition command、pause/suspend gate、win/lose/end event、run snapshot、run summary observation。不要把 “20 波” 写入框架。 |
| BrotatoLike 实施方式 | 游戏侧 DataOS 新增 `run_definition`、`run_phase_rule`、`run_end_condition`、`run_reward_hook`。`BrotatoLikeProgressionService` 从当前两波 state machine 扩展为正式 run lifecycle；Main / RunFlow validation 验证 1 到 20 波、死亡失败、胜利结算和 restart。 |
| OpenSpec 拆分建议 | `complete-brotatolike-run-lifecycle-20-waves`：只做生命周期和胜负结算，不混入武器、Meta、难度。 |
| 验证方式 | Runtime tests 覆盖 phase transition；Godot scene 覆盖 20 波 fast-forward、死亡失败、胜利结算、restart 后 state 清空；artifact 五字段必须非空，记录 `phase_sequence`、`win_reason`、`loss_reason`、`summary`、`cleanup_counts`。 |
| 优先级 | P0 |

### P0-02 波间商店闭环：自动打开、购买、刷新、锁定、离开进入下一波

| 字段 | 内容 |
| --- | --- |
| 功能思想 | Brotato-like 的构筑核心在波间商店：玩家通过有限货币和随机 offer 做取舍。当前只有 Shop service/UI 专项，仍不是波间体验闭环。 |
| 参考来源 | Brotato 每波后自动进入商店，商品受角色、Luck、已有武器、早期规则和稀有度门槛影响；支持 reroll、lock、价格随波次增长。 |
| 当前证据 | BrotatoLike 有 `BrotatoLikeShopService`、`ShopPanelUI`、3 个 deterministic item、购买和买不起拒绝；RunFlow 只记录 `shop_offer.validation` hook。 |
| 缺口判断 | 缺波间自动打开、继续按钮、刷新 / reroll 成本递增、lock slot、offer 持久化、购买后 UI 更新、离开商店启动下一波、shop session artifact。 |
| AI-first GameOS 通用设计 | 框架可抽象 `OfferSession`、`CurrencyWallet`、`PriceModifier`、`RerollPolicy`、`LockPolicy`、`PurchaseCommand`、`OfferSource`。但具体商品池、价格公式和角色偏好留在游戏侧。 |
| BrotatoLike 实施方式 | 在 `RewardShop` phase 创建 `BrotatoLikeShopSession`，绑定 `ShopPanelUI`；DataOS 扩展 `shop_rule`、`shop_rarity_curve`、`shop_slot_rule`、`shop_reroll_rule`；ProgressionService 在 shop close 后调用 `TryStartNextWave()`。 |
| OpenSpec 拆分建议 | `connect-brotatolike-wave-shop-session`：只连接波间商店 session，不做大量道具扩容。 |
| 验证方式 | Godot scene 从 wave complete 进入 shop，验证 panel 自动打开、购买一个 item、reroll、lock、close、下一波启动；artifact 记录 `shop_opened_from_phase`、`offer_ids_before_after`、`reroll_cost`、`locked_offer_persisted`、`next_wave_started`。 |
| 优先级 | P0 |

### P0-03 失败 / 胜利结算 UI 与 Run Summary

| 字段 | 内容 |
| --- | --- |
| 功能思想 | 玩家需要知道为什么赢 / 输、获得了什么、解锁了什么、伤害和构筑表现如何。结算也是 Meta progression 的入口。 |
| 参考来源 | Brotato / Vampire Survivors / Halls of Torment 都把单局结果连接到 unlock、统计、成就或长期资源。 |
| 当前证据 | 当前有 progression summary 兼容节点，但不是完整 run summary；没有正式失败 / 胜利结算 UI 和长期奖励落点。 |
| 缺口判断 | 缺 summary 数据模型、damage dealt / kills / pickups / spent / build list、unlock preview、restart / back to menu 按钮。 |
| AI-first GameOS 通用设计 | 框架应提供 `RunSummaryBuilder` 接口和 typed Observation：kills、damage、healing、currency、wave、duration、items、abilities、events。具体展示和奖励由游戏侧处理。 |
| BrotatoLike 实施方式 | 游戏侧新增 `RunSummaryData` 和 `RunSummaryPanelUI.tscn`；Damage / Ability / Shop / Progression 通过事件或 observation 汇总；胜利 / 失败时进入 `RunEnded` gate。 |
| OpenSpec 拆分建议 | 可并入 `complete-brotatolike-run-lifecycle-20-waves`，如果 UI 量大则拆 `add-brotatolike-run-summary-ui`。 |
| 验证方式 | Runtime test 检查 summary aggregation；Godot scene 检查 win/loss panel、summary fields、buttons、restart cleanup。 |
| 优先级 | P0 |

### P0-04 长局压力与对象池稳定性

| 字段 | 内容 |
| --- | --- |
| 功能思想 | 幸存者类游戏的真实风险来自大量敌人、投射物、特效、伤害数字和拾取物长时间并发。没有压力验证，单波 pass 不代表产品稳定。 |
| 参考来源 | Brotato 使用对象池和生成节奏控制；Deep Rock Galactic: Survivor 的程序洞穴、矿物和敌群强化了大量实体压力。 |
| 当前证据 | 当前已修复 Projectile / Effect visual pooling、HUD transient UI pooling；但没有 20 波长局和大量实体压力 artifact。 |
| 缺口判断 | 缺压力场景、性能计数、pool hit/miss、node count、entity count、GC/内存、stale registry、collision isolation 检查。 |
| AI-first GameOS 通用设计 | 框架应提供 `RuntimeStressObservation`、pool statistics、node registry consistency check、entity lifecycle leak check。 |
| BrotatoLike 实施方式 | 新增 `BrotatoLikeLongRunStressValidation.tscn`，快速推进 20 波或模拟高密度 spawn，记录对象池、Runtime entity、Godot node、collision object、artifact size。 |
| OpenSpec 拆分建议 | `add-brotatolike-long-run-stress-validation`，可以先只加验证，不改玩法。 |
| 验证方式 | Godot scene artifact 记录每波 peak counts、final cleanup、pool reuse、firstError、frame budget 采样；release-batch 可先不默认启用，作为 targeted / nightly。 |
| 优先级 | P0 |

### P1-01 武器系统：6 武器槽、自动攻击、武器稀有度与合成

| 字段 | 内容 |
| --- | --- |
| 功能思想 | Brotato 的核心差异不是单个技能，而是多武器并行自动攻击、武器槽上限、武器稀有度、同武器合成、近战 / 远程构筑。当前 BrotatoLike 更像“主动技能栏 + 技能池”，缺 Brotato-like 武器层。 |
| 参考来源 | Brotato 多数角色最多持有 6 武器；武器分近战 / 远程和 4 个 tier；相同 tier 武器可以合成到更高 tier。Vampire Survivors 也依赖武器数量、被动物品和进化关系形成构筑。 |
| 当前证据 | BrotatoLike 当前有 Ability / Skill pool，默认四槽为 `slam / chain_lightning / target_point_skill / dash`；没有独立 Weapon inventory、weapon auto-fire、weapon merge。 |
| 缺口判断 | 缺 `weapon_definition`、weapon instance、slot cap、auto attack scheduler、rarity / tier、merge rule、weapon offer、weapon UI、weapon stats scaling。 |
| AI-first GameOS 通用设计 | 框架可沉淀 `WeaponCapability` 或扩展 Attack / Ability：`WeaponDataKeys`、`WeaponInstance`、`AutoFireProcess`、`WeaponScalingRule`、`WeaponUpgradeRule`。但是否叫武器、最多几把、合成公式留给游戏侧。 |
| BrotatoLike 实施方式 | 先在游戏侧实现 `BrotatoLikeWeaponCatalog` 和 `weapon_definition` authoring；把武器视为 owner-owned runtime entity，绑定 Attack / Projectile / Effect / Feature；Shop offer 可同时包含 weapon 和 item。 |
| OpenSpec 拆分建议 | `add-brotatolike-weapon-inventory-and-autofire` 第一阶段只做 2-3 把武器、自动攻击和 UI；`add-brotatolike-weapon-rarity-merge` 第二阶段做 tier / merge。 |
| 验证方式 | Runtime test 验证 slot cap、auto-fire cooldown、scaling；Godot scene 验证装备 2 把武器自动攻击、购买相同武器、合成 tier、UI 展示。 |
| 优先级 | P1 |

### P1-02 构筑系统：主动技能、被动、武器、道具、角色机制统一成 Build Graph

| 字段 | 内容 |
| --- | --- |
| 功能思想 | 幸存者类游戏的乐趣来自“组合爆炸”：角色被动、武器、道具、升级、被动技能、套装、进化互相影响。若系统之间没有统一构筑图，AI 难以解释、测试和平衡。 |
| 参考来源 | Brotato 的角色、武器、物品、套装和 effect key 形成构筑；Vampire Survivors 有 weapon + passive item evolution / union；Halls of Torment 有 abilities、traits、items、class marks。 |
| 当前证据 | BrotatoLike 有 Ability / Feature / Item effect / LevelUpChoice / Character loadout，但它们还没有统一 build inspect 和 source tracking。 |
| 缺口判断 | 缺 build graph、效果来源追溯、冲突规则、tag / keyword、synergy 查询、构筑 UI、AI 查询接口。 |
| AI-first GameOS 通用设计 | 框架应提供 typed `BuildSource`、`ModifierSource`、`BuildGraphObservation`、`TagQuery`、`SynergyRule`，并允许 Ability / Feature / Item / Weapon / Character 都注册构筑节点。 |
| BrotatoLike 实施方式 | 游戏侧先定义 `BrotatoLikeBuildGraphService` 汇总角色、owned abilities、items、weapons、modifiers；HUD 或 pause panel 增加 build inspect。 |
| OpenSpec 拆分建议 | `add-brotatolike-build-graph-observation` 先做 observation 和验证，不急着做完整 UI；后续 `add-brotatolike-build-inspect-ui`。 |
| 验证方式 | Runtime test 检查 source tracking；Godot scene 通过购买 item、升级 grant ability、装备 weapon 后 artifact 输出 build graph。 |
| 优先级 | P1 |

### P1-03 升级选择扩展：随机权重、稀有度、reroll、banish、skip、替换槽

| 字段 | 内容 |
| --- | --- |
| 功能思想 | 当前 deterministic 三选一只能证明流程；成熟幸存者类需要升级池、权重、稀有度、reroll、banish、skip、选择锁定和替换规则，才能形成长期构筑。 |
| 参考来源 | Vampire Survivors / 20 Minutes Till Dawn / Soulstone Survivors 都在升级时提供多选项和构筑取舍；20 Minutes Till Dawn 还有永久 runes 与武器专属升级。 |
| 当前证据 | BrotatoLike 有确定性 `max_hp_plus_10 / move_speed_plus_20 / unlock_sine_wave_shot`，技能奖励加入 hidden owned ability。 |
| 缺口判断 | 缺随机候选池、权重、可重复 / 不可重复、稀有度、reroll currency、banish、skip reward、visible slot 替换、passive panel。 |
| AI-first GameOS 通用设计 | 框架可提供 `ChoiceOfferEngine`：输入候选、权重、过滤器、seed、reroll policy、banish policy，输出可复验 offer。选择效果通过 typed command 执行。 |
| BrotatoLike 实施方式 | DataOS 新增 `levelup_choice_pool`、`choice_weight_rule`、`choice_rarity`、`choice_exclusion_rule`；UI 增加 reroll / skip / replace；`GodotActiveSkillInputComponent` 只操作 visible slots。 |
| OpenSpec 拆分建议 | `expand-brotatolike-levelup-choice-pool`；替换槽建议独立 `add-brotatolike-active-slot-replacement`。 |
| 验证方式 | Runtime test 用固定 seed 验证候选可复现；Godot scene 验证 reroll、skip、banish、grant hidden、replace visible slot、passive 不进入主动栏。 |
| 优先级 | P1 |

### P1-04 被动面板与永久光环 / 周期技能常驻表现

| 字段 | 内容 |
| --- | --- |
| 功能思想 | 被动技能、光环、召唤、环绕物和持续效果必须被玩家理解：有什么、来自哪里、持续多久、是否可升级。否则构筑不可读。 |
| 参考来源 | Halls of Torment 的 ability / trait / item 组合、Soulstone Survivors 的 active/passive skills 和 runes 都需要清晰构筑可读性。 |
| 当前证据 | `orbit_skill / circle_damage / aura_shield` 有专项行为验收，但普通局内 passive panel / 获得流程未实现。 |
| 缺口判断 | 缺 passive inventory、panel UI、持续效果 icon、duration / stack / source、升级入口。 |
| AI-first GameOS 通用设计 | 框架 Feature / Effect 应输出 `ActiveEffectObservation`：source、owner、duration、stack、tags、visual entity、cleanup reason。 |
| BrotatoLike 实施方式 | 在 HUD / pause 中增加 `PassivePanelUI`，读取 build graph 和 active effects；升级选择或商店可授予 passive。 |
| OpenSpec 拆分建议 | `add-brotatolike-passive-panel-and-acquisition`。 |
| 验证方式 | Godot scene 选择 passive、验证 panel icon、effect active、duration cleanup、死亡 / 下一波清理策略。 |
| 优先级 | P1 |

### P1-05 敌人阵容扩展：多敌种行为、远程敌、冲锋敌、召唤 / 分裂 / 自爆

| 字段 | 内容 |
| --- | --- |
| 功能思想 | 幸存者类压力来自敌人组合，不只是数量。不同敌种让移动路线、优先目标和构筑选择产生变化。 |
| 参考来源 | Brotato 有不同敌人、精英、Boss、horde；Halls of Torment 有 70+ monsters 和 35+ bosses；Deep Rock Galactic: Survivor 利用虫群、地形和生物群落制造压力。 |
| 当前证据 | BrotatoLike 当前主要有 `chailangren / yuren`，AI 以追逐、接触伤害为主。 |
| 缺口判断 | 缺远程敌、冲锋、保持距离、环绕、分裂、自爆、召唤、护盾、精英词缀，以及对应动画 / VFX / artifact。 |
| AI-first GameOS 通用设计 | 框架 AI Capability 已有 behavior tree 基础，可补通用 behavior blocks、telegraph、attack pattern、status tags。具体敌种和参数留游戏侧。 |
| BrotatoLike 实施方式 | DataOS `enemy_definition` 扩展 behavior kind、attack pattern、spawn tags、reward；为每个敌种建小型 validation scene。 |
| OpenSpec 拆分建议 | `add-brotatolike-enemy-archetype-pack-1`，一次只加 3-5 个敌种。 |
| 验证方式 | Godot scene 验证每个敌种的 movement / attack / damage / cleanup；RunFlow 验证混合 spawn 不破坏 pause / respawn。 |
| 优先级 | P1 |

### P1-06 Elite / Boss / 特殊波

| 字段 | 内容 |
| --- | --- |
| 功能思想 | Elite 和 Boss 是 build check。它们要求玩家在单局里证明输出、走位、清怪、单体、恢复和防御都足够。 |
| 参考来源 | Brotato 有 Boss 波、Elite 波、Horde 波；Halls of Torment 强调 unique bosses；Soulstone Survivors 强调 massive bosses 和 curses。 |
| 当前证据 | 当前 RunFlow 没有正式 Boss / Elite / Horde；只有两波普通敌人。 |
| 缺口判断 | 缺 boss entity、攻击模式、telegraph、阶段、掉落、boss wave rule、elite modifier、boss health bar 和胜利条件。 |
| AI-first GameOS 通用设计 | 框架可提供 `EncounterDirector`、`BossPhaseDataKeys`、`TelegraphEvent`、`PatternScheduler`、`EliteModifier` 通用结构。 |
| BrotatoLike 实施方式 | 第一步做一个 Elite：普通敌 + modifier + 血量 / 伤害 / 速度 / 掉落提升；第二步做一个 Boss：多阶段攻击、BossBarUI、wave end condition。 |
| OpenSpec 拆分建议 | `add-brotatolike-elite-wave` 与 `add-brotatolike-first-boss-encounter` 分开。 |
| 验证方式 | Elite scene 验证 modifier 和掉落；Boss scene 验证 phase transition、telegraph、damage window、boss bar、victory trigger。 |
| 优先级 | P1 |

### P1-07 掉落经济：材料、经验、金币、宝箱、吸附、拾取半径

| 字段 | 内容 |
| --- | --- |
| 功能思想 | 掉落经济决定玩家是否敢冒险、是否能滚雪球、是否要投资拾取 / Luck / Harvesting。它连接战斗和商店。 |
| 参考来源 | Brotato 击杀掉落材料，波后进入商店购买；Deep Rock Galactic: Survivor 把采矿资源加入 survivor-like 循环；Halls of Torment 有 destructible objects、consumables 和可带回的 item。 |
| 当前证据 | BrotatoLike 已有经验拾取和货币 service，但货币来源、材料转换、宝箱、吸附半径、拾取表现不完整。 |
| 缺口判断 | 缺材料掉落、自动吸附、拾取半径属性、宝箱 / crate、波后转换、Luck / Harvesting 影响、掉落清理策略。 |
| AI-first GameOS 通用设计 | 框架可提供 `DropTable`、`PickupCapability`、`AttractionPolicy`、`RewardEvent`、`CurrencyDelta` observation。 |
| BrotatoLike 实施方式 | DataOS 新增 `drop_table`、`pickup_definition`、`currency_rule`；敌人死亡按 drop table 生成 pickup；玩家 pickup radius 由 DataKey 控制。 |
| OpenSpec 拆分建议 | `add-brotatolike-drop-pickup-economy`。 |
| 验证方式 | Godot scene 验证敌人死亡掉落材料 / 经验、吸附路径、拾取半径变化、波后材料转货币、cleanup。 |
| 优先级 | P1 |

### P1-08 商店内容扩展：道具池、权重、稀有度、角色偏好、ban / filter

| 字段 | 内容 |
| --- | --- |
| 功能思想 | 商店的乐趣不是买固定 3 个道具，而是在随机与掌控之间建立策略。 |
| 参考来源 | Brotato 商店受角色、Luck、已有武器影响；稀有度随波次增长；早期商店限制；可 lock / reroll / ban。 |
| 当前证据 | 当前 3 个 deterministic item：`vital_seed / swift_boots / sharpening_stone`。 |
| 缺口判断 | 缺大规模道具池、tag、rarity、weight、price formula、character bias、weapon bias、ban token、unique limit、已拥有处理。 |
| AI-first GameOS 通用设计 | 框架可提供可复现 weighted offer engine、过滤器、seed 和 explanation：为什么出现 / 为什么被过滤。 |
| BrotatoLike 实施方式 | DataOS 建 `item_tag`、`item_rarity`、`item_weight_rule`、`shop_filter_rule`；Shop artifact 输出每个 offer 的 source explanation。 |
| OpenSpec 拆分建议 | `expand-brotatolike-shop-content-and-weighting`。 |
| 验证方式 | Runtime test 固定 seed；Godot scene 验证角色偏好、Luck 改变稀有度、ban 后不出现、early wave weapon guarantee。 |
| 优先级 | P1 |

### P1-09 角色系统扩展：机制型角色、经济型角色、高风险高回报角色

| 字段 | 内容 |
| --- | --- |
| 功能思想 | 幸存者类角色不是皮肤，而是构筑规则的入口。角色差异应该改变商店偏好、起始 loadout、属性成长、限制、奖励和风险。 |
| 参考来源 | Brotato 本地分析把角色分为属性专精型、机制改造型、经济型、高风险高回报型；公开 Wiki 也显示某些角色改变武器槽、购买货币、商店规则等。 |
| 当前证据 | BrotatoLike 有 `deluyi / guangfa` 两名角色，视觉、属性、loadout 有差异。 |
| 缺口判断 | 缺机制型角色、角色限制、角色专属 unlock、角色商店偏好、角色统计和选择菜单完整 UX。 |
| AI-first GameOS 通用设计 | 框架可提供 Character as BuildSource / RuleSource，不知道具体角色；支持角色添加 tags、modifiers、constraints、starting loadout。 |
| BrotatoLike 实施方式 | DataOS 扩 `character_rule`、`character_constraint`、`character_shop_bias`、`character_unlock`；新增 4-6 个差异明显的角色，每个有 validation。 |
| OpenSpec 拆分建议 | `expand-brotatolike-character-archetypes`。 |
| 验证方式 | CharacterSelection scene 验证每类角色规则；RunFlow 验证限制生效，如不能装备某类 weapon、商店偏好变化。 |
| 优先级 | P1 |

### P1-10 关卡 / 区域 / 地形规则

| 字段 | 内容 |
| --- | --- |
| 功能思想 | 关卡提供空间压力和内容主题。没有关卡差异，后续难度和敌人池会显得单薄。 |
| 参考来源 | Brotato 有 ZoneService 和区域背景；Halls of Torment 有多个地下世界；Deep Rock Galactic: Survivor 的 biomes / procedural caves / mining 是核心差异。 |
| 当前证据 | BrotatoLike 当前主要围绕单主场景和 spawn authoring，没有正式多地图 / zone 选择。 |
| 缺口判断 | 缺 stage definition、边界、背景、障碍、地形危险、出生规则、敌人池、奖励差异、stage selection UI。 |
| AI-first GameOS 通用设计 | 框架可提供 stage / zone data contract、spawn area query、hazard event、navigation obstacle hooks；不应固化具体地图。 |
| BrotatoLike 实施方式 | DataOS 新增 `stage_definition`、`stage_spawn_zone`、`stage_hazard`；先做 2 个 arena：标准开阔、障碍 / 危险区域。 |
| OpenSpec 拆分建议 | `add-brotatolike-stage-zone-system`。 |
| 验证方式 | Godot scene 验证 stage load、spawn zone、hazard damage、camera bounds、enemy pathing、HUD stage label。 |
| 优先级 | P1 |

### P2-01 Meta Progression：解锁、难度、成就、统计、永久资源

| 字段 | 内容 |
| --- | --- |
| 功能思想 | Roguelite 的长期目标来自局外进度：通关解锁角色 / 武器 / 道具 / 难度，完成挑战，积累统计和永久资源。 |
| 参考来源 | Brotato 有 ProgressData、characters / weapons / items / difficulties / challenges；Halls of Torment 有 quest-based meta progression 和大量 quests；Soulstone Survivors 有 skill tree / runes / curses。 |
| 当前证据 | 当前有首版角色选择，但没有完整 unlock、save、achievements、difficulty progression。 |
| 缺口判断 | 缺 persistent save schema、unlock condition DSL、statistics event、achievement board、difficulty unlock、profile migration。 |
| AI-first GameOS 通用设计 | 通用框架应提供 `ProgressionProfile`、`UnlockCondition`、`StatCounter`、save migration、versioned profile validator；具体 unlock 内容留游戏。 |
| BrotatoLike 实施方式 | DataOS 建 `unlock_rule`、`achievement_definition`、`difficulty_definition`；游戏侧 `BrotatoLikeProfileService` 保存本地 JSON 或 Godot Resource。 |
| OpenSpec 拆分建议 | `add-brotatolike-meta-progression-profile` 第一阶段只做存档、统计、2-3 个 unlock；后续扩成成就和难度。 |
| 验证方式 | Runtime tests 覆盖 unlock condition；Godot scene 验证完成一局后解锁角色 / 道具、重启后保留、版本迁移。 |
| 优先级 | P2 |

### P2-02 难度 / Danger / Curse / Hazard 系统

| 字段 | 内容 |
| --- | --- |
| 功能思想 | 难度层让同一内容重复可玩，并给 Meta progression 目标。难度不应只是敌人 HP 倍率，应组合敌人数量、Elite、Boss、经济、掉落、地图危险和奖励。 |
| 参考来源 | Brotato 有 Danger；Deep Rock Galactic: Survivor 有 hazard levels；Soulstone Survivors 有 curses；Halls of Torment 有 late game Shrine / artifacts。 |
| 当前证据 | 当前没有正式 difficulty authoring。 |
| 缺口判断 | 缺 difficulty definition、解锁、选择 UI、对 wave/shop/drop/enemy 的 modifier、验证矩阵。 |
| AI-first GameOS 通用设计 | 框架可提供 `DifficultyModifierSet` 和 typed modifier application，director / shop / drop / enemy 都能读取 difficulty context。 |
| BrotatoLike 实施方式 | DataOS 新增 `difficulty_definition`、`difficulty_modifier`; 先做 0-2 三档，影响 enemy hp/damage/spawn budget/shop price。 |
| OpenSpec 拆分建议 | `add-brotatolike-danger-difficulty-system`。 |
| 验证方式 | 固定 seed 下对比 D0/D1/D2 的 spawn count、enemy stats、shop price、reward；UI 验证选择和锁定。 |
| 优先级 | P2 |

### P2-03 武器 / 技能进化与套装 Synergy

| 字段 | 内容 |
| --- | --- |
| 功能思想 | 进化和套装把“收集多个组件”变成明确目标，增强构筑记忆点。 |
| 参考来源 | Vampire Survivors 有 evolution、union、gift、morph；Brotato 有 weapon class / set bonus；Halls of Torment 有 traits、items、rare variants。 |
| 当前证据 | BrotatoLike 目前没有 evolution / union / set bonus；Feature / Item / Ability 具备实现基础。 |
| 缺口判断 | 缺 synergy rule、required components、trigger condition、evolved entity、UI hint、source cleanup 或保留策略。 |
| AI-first GameOS 通用设计 | 框架应提供 `SynergyRule` / `EvolutionRule` 检测接口和 build graph query，不知道具体组合。 |
| BrotatoLike 实施方式 | DataOS 新增 `synergy_rule`：如 `chain_lightning + attack_speed_item -> storm_chain`；先做 2 条可验证组合。 |
| OpenSpec 拆分建议 | `add-brotatolike-synergy-evolution-rules`。 |
| 验证方式 | Runtime test 检查规则匹配；Godot scene 购买 / 获得组件后触发进化，验证 UI hint、效果变化、build graph。 |
| 优先级 | P2 |

### P2-04 状态异常与元素体系

| 字段 | 内容 |
| --- | --- |
| 功能思想 | 元素和状态异常给武器 / 技能 / 敌人带来差异化：燃烧、冰冻、中毒、流血、击退、脆弱、护盾等。 |
| 参考来源 | Brotato 有 burning 等效果；Soulstone Survivors 以大量 skill types / status effects 支撑构筑。 |
| 当前证据 | Damage / Feature 能处理基础伤害、治疗、护盾等，但 BrotatoLike 游戏侧没有完整元素状态体系。 |
| 缺口判断 | 缺 status definition、stacking、duration、tick、immunity、VFX、UI icon、boss resistance。 |
| AI-first GameOS 通用设计 | 框架可提供 `StatusEffectCapability`：typed status instance、stack rule、duration tick、source tracking、event hooks。 |
| BrotatoLike 实施方式 | 先做 3 个状态：burn、slow、vulnerable；接入 weapon / item / enemy。 |
| OpenSpec 拆分建议 | `add-gameos-status-effect-capability` 如果要通用；游戏侧先 `add-brotatolike-status-effects-first-pass`。 |
| 验证方式 | Runtime test 验证叠层 / tick / 到期；Godot scene 验证 VFX、敌人速度变化、伤害变化、cleanup。 |
| 优先级 | P2 |

### P2-05 主菜单 / 局外流程：角色、难度、关卡、武器选择、设置

| 字段 | 内容 |
| --- | --- |
| 功能思想 | 产品级游戏不能直接进 Main fallback；需要局外流程选择角色、难度、关卡、起始武器和设置。 |
| 参考来源 | Brotato 有角色选择、起始武器和难度；20 Minutes Till Dawn 有角色 / 武器 / runes loadout；Soulstone Survivors 有角色、weapon、runes、skill tree。 |
| 当前证据 | 当前有首版 CharacterSelectPanelUI，但还不是完整主菜单和存档流程。 |
| 缺口判断 | 缺 title / main menu、profile load、character/difficulty/stage/loadout selection、settings、credits、quit、controller focus。 |
| AI-first GameOS 通用设计 | 框架通常不承载具体菜单，但 UI binding、save/profile、input focus gate、scene transition observation 可通用。 |
| BrotatoLike 实施方式 | 新增 `Scenes/UI/MainMenu.tscn` 和 `RunSetupPanelUI`，将 CharacterSelect 扩展为 setup flow；Main runtime 从 setup context 启动。 |
| OpenSpec 拆分建议 | `add-brotatolike-run-setup-menu`。 |
| 验证方式 | Godot scene 验证键鼠 / 自动 focus 选择角色、难度、关卡并启动 run；真实设备 QA 后续手动记录。 |
| 优先级 | P2 |

### P2-06 存档、配置、统计与迁移

| 字段 | 内容 |
| --- | --- |
| 功能思想 | 解锁和设置需要稳定存档；AI-first 项目还需要迁移和校验，避免长期数据漂移。 |
| 参考来源 | Brotato 本地分析有 ProgressData 和多个 version loader；成熟 roguelite 通常长期维护存档兼容。 |
| 当前证据 | BrotatoLike 当前主要是 runtime snapshot / DataOS，不是玩家 profile 存档。 |
| 缺口判断 | 缺 profile save file、schema version、migration、settings persistence、statistics export。 |
| AI-first GameOS 通用设计 | 通用 `SaveProfile`、`MigrationRegistry`、`ProfileValidator`、`StatEvent` 可沉淀到框架或 workspace pattern。 |
| BrotatoLike 实施方式 | 游戏侧 `BrotatoLikeProfileService` 管理 `user://brotatolike-profile.json`；新增 migration tests。 |
| OpenSpec 拆分建议 | 可并入 Meta Progression 第一阶段。 |
| 验证方式 | Unit tests 读写 / migration；Godot scene 验证解锁后重启仍存在，损坏存档 fallback 安全。 |
| 优先级 | P2 |

### P2-07 内容规模与 DataOS authoring 工具

| 字段 | 内容 |
| --- | --- |
| 功能思想 | 完整幸存者游戏需要大量内容。手写 SQL 和人工查错会成为瓶颈，必须有内容 authoring、lint、diff 和生成报告。 |
| 参考来源 | Brotato 有大量 `.tres` 内容；Halls of Torment / Soulstone Survivors 的内容量说明品类依赖大量可组合数据。 |
| 当前证据 | BrotatoLike 已有 DataOS table-first authoring 和 snapshot generator，但内容规模还很小。 |
| 缺口判断 | 缺内容覆盖报告、tag lint、权重检查、未引用资源、重复 id、数值异常、缺本地化、缺图标检查。 |
| AI-first GameOS 通用设计 | DataOS 应增加 authoring validator：schema + content rules + balance heuristics + dependency graph。 |
| BrotatoLike 实施方式 | 新增 `Tools/run-content-audit.sh` 或扩 DataOS validator，输出 `DocsAI/ContentAudit.md` / JSON artifact。 |
| OpenSpec 拆分建议 | `add-brotatolike-content-authoring-audit`。 |
| 验证方式 | CLI validation artifact，检查 weapons/items/enemies/waves/stages/unlocks 全覆盖且无 dangling refs。 |
| 优先级 | P2 |

### P2-08 本地化、文本模板、Tooltip 与数值解释

| 字段 | 内容 |
| --- | --- |
| 功能思想 | 构筑游戏需要清晰文本。玩家必须理解物品、武器、技能、状态和角色规则。AI 也需要文本模板来生成和验证内容。 |
| 参考来源 | Brotato 本地分析有 TranslationServer、BBCode、效果文本生成；同类游戏都依赖 tooltip 表达复杂规则。 |
| 当前证据 | BrotatoLike 当前 UI 多为验证和首版文本，缺统一本地化和 tooltip 模板。 |
| 缺口判断 | 缺 localization key、effect text renderer、tooltip、变量替换、颜色标记、缺失 key validator。 |
| AI-first GameOS 通用设计 | 框架可提供 `EffectDescriptionRenderer` 或至少规范 DataOS text template / value binding；本地化资源留游戏侧。 |
| BrotatoLike 实施方式 | DataOS 新增 `localized_text`、`tooltip_template`；UI slot / item / character / choice 使用 template 渲染。 |
| OpenSpec 拆分建议 | `add-brotatolike-localization-and-tooltips`。 |
| 验证方式 | CLI 检查所有 content id 有文本；Godot scene 截取 tooltip text artifact，检查变量值和缺 key。 |
| 优先级 | P2 |

### P3-01 AI 内容生成接口与平衡模拟

| 字段 | 内容 |
| --- | --- |
| 功能思想 | 这是 AI-first 框架，不应只让 AI 写代码；应让 AI 能生成候选内容、运行验证、解释构筑、发现数值异常。 |
| 参考来源 | Brotato 的数据驱动适合 AI 生成，但其字典 / 单例方式不够类型安全；SlimeAI 已选择 typed DataKey + DataOS。 |
| 当前证据 | DataOS table-first authoring 已建立，但没有专门的 AI 内容生成 / 平衡模拟 API。 |
| 缺口判断 | 缺 content generation prompt schema、dry-run validator、balance simulation、expected power curve、AI explain endpoint。 |
| AI-first GameOS 通用设计 | 框架 / Workspace 应提供 `ContentAuthoringContract`、`BalanceSimulationRunner`、`BuildExplanation`、`OpenSpec artifact template`。 |
| BrotatoLike 实施方式 | 为 weapon/item/enemy/wave 定义生成模板和 lint；AI 新增内容后必须产出 validation scene 或 content audit artifact。 |
| OpenSpec 拆分建议 | `add-ai-content-authoring-workflow-for-brotatolike`。 |
| 验证方式 | 用 AI 生成一个小内容包，通过 DataOS validator、content audit、targeted scene，不直接进入 release-batch。 |
| 优先级 | P3 |

### P3-02 手感与视觉门禁：截图、像素、音效、反馈

| 字段 | 内容 |
| --- | --- |
| 功能思想 | 自动化行为 pass 不能保证好玩。幸存者类需要打击反馈、屏幕震动、低血提示、拾取音效、升级音效、Boss telegraph、可读性。 |
| 参考来源 | Brotato 本地分析包含伤害数字、屏幕震动、红屏、粒子、shader outline、背景渐变。 |
| 当前证据 | BrotatoLike 有基础血条、飘字、链电线段和技能 VFX，但没有系统化截图 / 像素 / 音效 / 低血反馈门禁。 |
| 缺口判断 | 缺 visual regression、UI overlap、effect visibility、audio cue、screenshake / hitstop、damage vignette、readability。 |
| AI-first GameOS 通用设计 | Validation tooling 可支持 screenshot capture、canvas pixel checks、UI overlap checks、audio event observation。 |
| BrotatoLike 实施方式 | 建立 `VisualUXValidation` 场景，记录截图路径和关键像素 / node visibility；手动 QA 补真实设备。 |
| OpenSpec 拆分建议 | `add-brotatolike-visual-feedback-validation`。 |
| 验证方式 | Godot headless screenshot artifact + JSON checks；真实设备 QA 记录触感和窗口焦点。 |
| 优先级 | P3 |

### P3-03 Mod / 扩展内容边界

| 字段 | 内容 |
| --- | --- |
| 功能思想 | 如果目标是通用 AI-first GameOS，后续可能需要游戏内容扩展或 AI 生成内容包。现在不必做 Mod，但要避免把内容硬编码到框架。 |
| 参考来源 | Brotato 解包资源包含 mod_loader，但本项目不应复制该实现。 |
| 当前证据 | SlimeAI 已有 DataOS 和游戏侧 seed，框架 / 游戏边界较清晰。 |
| 缺口判断 | 缺 content pack 目录规范、ID namespace、resource safety、schema version、load order。 |
| AI-first GameOS 通用设计 | DataOS 可以支持 content pack manifest 和 namespace，但不应为 BrotatoLike 专属。 |
| BrotatoLike 实施方式 | 先写规则文档，不急着实现运行时 mod loader。 |
| OpenSpec 拆分建议 | `define-dataos-content-pack-boundary`，框架级，等内容规模上来后再做。 |
| 验证方式 | manifest validator 和 conflict detector。 |
| 优先级 | P3 |

## 6. 推荐路线图

### 6.1 P0：先补产品闭环，不扩内容规模

1. `complete-brotatolike-run-lifecycle-20-waves`
   - 完成 20 波、胜利 / 失败 / 重开 / run summary。
   - 只使用现有少量敌人和技能，避免内容膨胀。

2. `connect-brotatolike-wave-shop-session`
   - 第 1 波结束自动打开 shop，支持 purchase / reroll / lock / close，close 后启动下一波。
   - 复用现有 3 个道具。

3. `add-brotatolike-long-run-stress-validation`
   - 不加玩法，补长局压力 artifact。

### 6.2 P1：建立 Brotato-like 构筑核心

1. `add-brotatolike-weapon-inventory-and-autofire`
2. `add-brotatolike-weapon-rarity-merge`
3. `expand-brotatolike-levelup-choice-pool`
4. `add-brotatolike-drop-pickup-economy`
5. `expand-brotatolike-shop-content-and-weighting`
6. `add-brotatolike-enemy-archetype-pack-1`
7. `add-brotatolike-elite-wave`

### 6.3 P2：建立长期游玩目标

1. `add-brotatolike-meta-progression-profile`
2. `add-brotatolike-danger-difficulty-system`
3. `add-brotatolike-stage-zone-system`
4. `add-brotatolike-run-setup-menu`
5. `add-brotatolike-localization-and-tooltips`

### 6.4 P3：AI-first 和产品化工具

1. `add-brotatolike-content-authoring-audit`
2. `add-ai-content-authoring-workflow-for-brotatolike`
3. `add-brotatolike-visual-feedback-validation`
4. `define-dataos-content-pack-boundary`

## 7. 通用 GameOS 落点总表

| 缺口 | 应沉到通用框架 | 留在 BrotatoLike |
| --- | --- | --- |
| Run lifecycle | phase gate、transition command、summary observation | 20 波、胜负规则、奖励 |
| Wave director | spawn budget、director interface、spawn observation | 敌人池、波次曲线、Boss 安排 |
| Shop / offers | offer session、currency、reroll / lock 抽象 | 商品池、价格、权重、UI 风格 |
| Weapon | owner-owned weapon entity、auto-fire、scaling hook | 武器内容、槽位数、合成公式 |
| Build graph | source tracking、modifier graph、tag query | 具体 synergy、角色机制、道具 |
| Choice offer | seeded weighted choice、reroll / banish policy | 升级池、稀有度、文案 |
| Drop / pickup | drop table、pickup attraction、reward event | 掉落表、材料名称、音效 |
| Meta progression | unlock DSL、stat counter、save migration | 具体解锁、成就、难度 |
| Status effect | stack / duration / tick / source | burn/slow 等具体数值和表现 |
| Validation | artifact schema、runner、gate、observation helpers | 具体场景、pass criteria、内容覆盖 |

## 8. 采纳决策

| 研究项 | 决策 | 理由 | SlimeAI 落点 |
| --- | --- | --- | --- |
| Brotato 波间商店思想 | Adopt Now | 当前已有 Shop service 但缺波间闭环，是 P0 产品缺口 | BrotatoLike game-side + 可选通用 OfferSession |
| Brotato 武器槽 / 合成思想 | Adopt Later | 是 Brotato-like 核心，但需要先完成 run / shop 闭环 | 游戏侧 WeaponCatalog，后续评估框架 WeaponCapability |
| Brotato 角色机制分类 | Adopt Later | 当前只有 2 角色，扩展后能显著提升重玩 | Character as BuildSource，规则留游戏侧 |
| Vampire Survivors evolution / union 思想 | Adopt Later | 对构筑深度有价值，但应在 weapon / build graph 后做 | 通用 SynergyRule + 游戏侧组合 |
| Deep Rock Galactic: Survivor 地形 / 采矿思想 | Reject for now | 当前 BrotatoLike 目标更接近 Brotato；采矿 / 程序洞穴会显著扩大范围 | 可作为未来 stage / zone 灵感，不进近期计划 |
| Halls of Torment quest-based meta progression | Adopt Later | 长期目标和 unlock 很重要，但应在 run summary / profile 后做 | MetaProgression profile + achievement DSL |
| Soulstone Survivors curses / skill tree / runes | Adopt Later | 适合 P2/P3 长期进度，不适合 P0 | DifficultyModifierSet、Rune-like loadout 可后续设计 |
| 20 Minutes Till Dawn 手动射击 / reload 差异 | Reject for now | 与当前自动射击 / 技能栏方向不同，会改变核心手感 | 可保留为未来角色或武器特例 |
| 直接复制 Brotato 代码 / 资产 / 数据 | Reject | 违反任务口径和 ExternalResources policy | copiedCodeOrAssets: none |

## 9. Review Gate 自检

### RV-PLAN-FEASIBILITY

| 检查项 | 状态 | 证据 |
| --- | --- | --- |
| 范围是否可拆分 | APPROVE | 已按 P0/P1/P2/P3 拆为独立 OpenSpec change |
| 是否引用真实路径 | APPROVE | 引用 `Games/BrotatoLike/DocsAI/*`、`Resources/Games/Games/Brotato/*` |
| 是否区分框架 / 游戏侧 | APPROVE | 每项都有 AI-first GameOS 通用设计与 BrotatoLike 实施方式 |
| 是否避免复制外部代码 / 资产 | APPROVE | `copiedCodeOrAssets: none`，采纳功能思想 |

### RV-DOC-SYNC

| 检查项 | 状态 | 证据 |
| --- | --- | --- |
| 文档入口 | APPROVE | 本文应由 `DocsAI/INDEX.md` 挂入口 |
| 与现有台账冲突 | APPROVE | 当前基线来自 `MigrationLedger.md`、`BrotatoMyFeatureMigrationAudit.md`、`GameProjectState.md` |
| 是否伪造验证 | APPROVE | 历史 artifact 只作为当前状态证据；新功能均要求新验证 |

### RV-RETROSPECTIVE

| 检查项 | 状态 | 证据 |
| --- | --- | --- |
| 外部资源策略 | APPROVE | 已记录 enabled / scope / reason / expires |
| Evidence / Inference / Unknown | APPROVE | 第 2 节明确分类 |
| 剩余风险 | CONCERNS | 未跑新 Godot scene；本文是研究文档，验证以文档自检和 git 范围检查为主 |

CONCERNS: 本文是研究采纳文档，不是实现；所有功能仍需按 OpenSpec change 单独设计、实现和验证。

## 10. 参考资料

### 本地资料

- `Resources/Games/Games/Brotato/README.md`
- `Resources/Games/Games/Brotato/Docs/01-GameOverviewAndCoreSystems/01-GameOverview.md`
- `Resources/Games/Games/Brotato/Docs/01-GameOverviewAndCoreSystems/05-WeaponSystem.md`
- `Resources/Games/Games/Brotato/Docs/01-GameOverviewAndCoreSystems/07-WaveSystem.md`
- `Resources/Games/Games/Brotato/Docs/01-GameOverviewAndCoreSystems/08-ShopSystem.md`
- `Resources/Games/Games/Brotato/Docs/01-GameOverviewAndCoreSystems/09-ProgressionSystem.md`
- `Resources/Games/Games/Brotato/Docs/02-AIFrameworkAnalysis/08-AIFrameworkLessons.md`
- `Games/BrotatoLike/DocsAI/GameProjectState.md`
- `Games/BrotatoLike/DocsAI/MigrationLedger.md`
- `Games/BrotatoLike/DocsAI/BrotatoMyFeatureMigrationAudit.md`
- `Games/BrotatoLike/DocsAI/UnfinishedMigrationHandoff.md`

### 公开资料

- Brotato Steam: https://store.steampowered.com/app/1942280/Brotato/
- Brotato Wiki Shop: https://brotato.wiki.spellsandguns.com/Shop
- Brotato Wiki Weapons: https://brotato.wiki.spellsandguns.com/Weapons
- Brotato Wiki Characters: https://brotato.wiki.spellsandguns.com/Characters
- Vampire Survivors Wiki Evolution: https://vampire.survivors.wiki/w/Evolution
- Deep Rock Galactic: Survivor Steam: https://store.steampowered.com/app/2321470/Deep_Rock_Galactic_Survivor/
- Deep Rock Galactic: Survivor Equipment Wiki: https://deeprockgalactic.wiki.gg/wiki/Survivor:Equipment
- Deep Rock Galactic: Survivor Biomes Wiki: https://deeprockgalactic.wiki.gg/wiki/Survivor:Biomes
- Halls of Torment Steam: https://store.steampowered.com/app/2218750/Halls_of_Torment/
- Halls of Torment Wiki Quest: https://hot.fandom.com/wiki/Quest
- 20 Minutes Till Dawn Wiki Runes: https://20minutestilldawn.wiki.gg/wiki/Runes
- 20 Minutes Till Dawn Wiki Characters: https://20minutestilldawn.wiki.gg/wiki/Characters
- 20 Minutes Till Dawn Wiki Weapons: https://20minutestilldawn.wiki.gg/wiki/Weapons
- Soulstone Survivors Steam: https://store.steampowered.com/app/2066020/Soulstone_Survivors/
- Soulstone Survivors Wiki Game Mechanics: https://soulstone-survivors.fandom.com/wiki/Game_Mechanics
