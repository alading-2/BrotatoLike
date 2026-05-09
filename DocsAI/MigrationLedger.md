# BrotatoLike 迁移台账

> 更新日期：2026-05-09
> 来源：`Else/brotato-my` 旧项目输入、`Games/BrotatoLike` 当前实现、`Plans/BrotatoLike 迁移质量审计与重启建议.md`。
> 状态说明：本台账是迁移事实源，不是完成声明。`DataOS-only` 表示数据记录已进入 DataOS，但不代表资源可加载或玩法已完成。

## 状态词汇

- `已迁移`：新仓库存在目标实现或资源，且验证命令覆盖旧职责的主要行为。
- `部分迁移`：新实现覆盖旧职责的一部分，仍缺真实主场景、UI、逐技能或逐系统验收。
- `DataOS-only`：旧数据已进入 `DataOS/Authoring/BrotatoLike.seed.sql` 或 `DataOS/Snapshots/runtime_snapshot.json`，但只证明记录存在。
- `遗留引用`：仍存在旧 `res://Src/...` 或 `res://Data/...` 引用，需要替换、迁移、删除或标记 intentionally dropped。
- `未迁移`：新仓库没有对应正式目标，或只有旧输入来源。
- `废弃候选`：可能被新框架能力替代，但仍需要后续 change 写明废弃依据。

## 第一版台账

| 旧路径 | 旧职责 | 新目标路径 | 迁移方式 | 当前状态 | 验证命令 | 缺口 |
| --- | --- | --- | --- | --- | --- | --- |
| `Else/brotato-my/Src/Main/Main.tscn` | 旧主场景入口、Camera、游戏启动节点 | `Games/BrotatoLike/Scenes/Main.tscn`、`Games/BrotatoLike/Src/Game/Main.cs`、`Games/BrotatoLike/Src/Game/BrotatoLikeGameRuntime.cs` | adapt/rewrite | 部分迁移 | 已有记录：`cd Games/BrotatoLike && Tools/run-build.sh && Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 3 --log-dir .ai-temp/scene-tests/runs` | 普通主场景只有 initialized 日志，没有 playable PASS/FAIL marker；UI、完整输入、波次验收未完成。 |
| `Else/brotato-my/Src/ECS/Base/Entity/Ability/AbilityEntity.tscn` | 旧技能实体预制 | `Games/BrotatoLike/Src/Game/BrotatoLikeAbilityHandlers.cs`、`SkilmeAI/GameOS/Capabilities/Ability`、DataOS `ability/*` | rewrite/DataOS | 部分迁移 | 已有记录：`cd Games/BrotatoLike && Tools/run-godot-smoke.sh` | 旧实体场景未迁为可加载场景；当前只验证 handler 执行路径和 runtime entity。 |
| `Else/brotato-my/Src/ECS/Base/Entity/Effect/EffectEntity.tscn` | 旧特效实体预制 | `SkilmeAI/GameOS/Capabilities/Effect`、`SkilmeAI/GameOS/GodotBridge/GodotProjectileEffectSpawner.cs`、`Games/BrotatoLike/assets/Effect/**/*` | rewrite/copy assets | 部分迁移 | 已有记录：`cd Games/BrotatoLike && Tools/run-godot-smoke.sh` | 旧 EffectEntity 场景本身未迁；需要逐特效资源和生命周期验收。 |
| `Else/brotato-my/Src/ECS/Base/Entity/Effect/LightningLineEffect/LightningLineEffect.tscn` | 闪电链连线特效 | DataOS `ability.chain_lightning`、`BrotatoLikeAbilityHandlers.cs` | rewrite/DataOS | 部分迁移 | 已有记录：`cd Games/BrotatoLike && Tools/run-godot-smoke.sh` | DataOS 中 `Ability.LineEffectScenePath` 为空；连线视觉未恢复。 |
| `Else/brotato-my/Src/ECS/Base/Entity/Preview/VisualPreviewEntity.tscn` | 预览/调试实体 | 无正式目标 | drop-candidate | 未迁移 | 未验证：当前可玩切片不依赖旧预览实体。 | 需要决定迁到调试工具、测试场景，或标记 intentionally dropped。 |
| `Else/brotato-my/Src/ECS/Base/Entity/Projectile/ProjectileEntity.tscn` | 旧投射物实体预制 | `SkilmeAI/GameOS/Capabilities/Projectile`、`GodotProjectileEffectSpawner.cs`、`Games/BrotatoLike/assets/Projectile/**/*` | rewrite/copy assets | 部分迁移 | 已有记录：`cd Games/BrotatoLike && Tools/run-godot-smoke.sh` | 旧 ProjectileEntity 场景未迁；需要逐投射物视觉、命中、穿透和清理验收。 |
| `Else/brotato-my/Src/ECS/Base/Entity/Unit/Enemy/EnemyEntity.tscn` | 旧敌人实体预制 | `Games/BrotatoLike/Src/Game/BrotatoLikeEnemySpawnSystem.cs`、DataOS `unit.enemy/*`、`Games/BrotatoLike/assets/Unit/Enemy/**/*` | rewrite/DataOS/copy assets | 部分迁移 | 已有记录：`cd Games/BrotatoLike && Tools/run-godot-smoke.sh` | 第 1 波生成 smoke 已覆盖，但普通主场景尚未有敌人追逐、攻击、死亡 PASS marker。 |
| `Else/brotato-my/Src/ECS/Base/Entity/Unit/Player/PlayerEntity.tscn` | 旧玩家实体预制 | `BrotatoLikeGameRuntime.SpawnPlayer()`、`GodotPlayerInputComponent`、DataOS `unit.player/deluyi`、`assets/Unit/Player/**/*` | rewrite/DataOS/copy assets | 部分迁移 | 已有记录：`cd Games/BrotatoLike && Tools/run-godot-smoke.sh` | 玩家生成和输入 smoke 已覆盖；普通主场景还缺移动验收 marker 和 HUD 接入。 |
| `Else/brotato-my/Src/ECS/Base/Entity/Unit/TargetingIndicator/TargetingIndicatorEntity.tscn` | 目标指示器实体 | DataOS `unit.targeting_indicator/default`、Ability point target runtime | rewrite/DataOS | 部分迁移 | 已有记录：`cd Games/BrotatoLike && Tools/run-godot-smoke.sh` | 指示器视觉/交互场景未迁；需要真实点选输入验收。 |
| `Else/brotato-my/Src/ECS/Base/Component/Ability/{ChargeComponent,CooldownComponent,CostComponent,TriggerComponent}/*.tscn` | 技能充能、冷却、消耗、触发组件 | `SkilmeAI/GameOS/Capabilities/Ability`、DataOS `Ability.*` 字段 | rewrite/DataOS | 部分迁移 | 已有记录：`cd /home/slime/Code/SkilmeAI/SkilmeAI && Tools/run-build.sh && Tools/run-tests.sh` | 旧 Component 场景未迁；游戏侧技能输入、UI 冷却显示和 charge 玩法未完整验收。 |
| `Else/brotato-my/Src/ECS/Base/Component/Collision/{CollisionComponent,ContactDamageComponent,HurtboxComponent,PickupComponent}/*.tscn` | 碰撞、伤害盒、接触伤害、拾取组件 | `SkilmeAI/GameOS/Capabilities/Collision`、`SkilmeAI/GameOS/GodotBridge/GodotCollisionComponent.cs`、`GodotHurtboxComponent.cs`、`GodotContactDamageComponent.cs` | rewrite | 部分迁移 | 已有记录：`cd /home/slime/Code/SkilmeAI/SkilmeAI && Tools/run-build.sh && Tools/run-tests.sh` | Pickup 未在当前 BrotatoLike 可玩切片中证明；旧场景节点结构未迁。 |
| `Else/brotato-my/Src/ECS/Base/Component/Effect/EffectComponent/EffectComponent.tscn` | 特效播放组件 | `EffectTool`、`GodotProjectileEffectSpawner.cs` | rewrite | 部分迁移 | 已有记录：`cd Games/BrotatoLike && Tools/run-godot-smoke.sh` | 缺逐特效动画结束、回收和主场景可视验收。 |
| `Else/brotato-my/Src/ECS/Base/Component/Movement/{EntityMovementComponent,EntityOrientationComponent}.tscn` | 位移和朝向组件 | `MovementSystem`、`GodotMovementDriver.cs`、`GodotOrientationComponent.cs` | rewrite | 部分迁移 | 已有记录：`cd /home/slime/Code/SkilmeAI/SkilmeAI && Tools/run-build.sh && Tools/run-tests.sh` | 主场景玩家/敌人移动验收 marker 未完成。 |
| `Else/brotato-my/Src/ECS/Base/Component/Player/ActiveSkillInputComponent/ActiveSkillInputComponent.tscn` | 玩家主动技能输入 | `Games/BrotatoLike/Src/Game/GodotActiveSkillInputComponent.cs`、`GodotPlayerInputComponent` | rewrite | 部分迁移 | 已有记录：`cd Games/BrotatoLike && Tools/run-build.sh` | LB/RB 切换、X 释放、Point 目标点选仍列为下一步；缺主场景验收。 |
| `Else/brotato-my/Src/ECS/Base/Component/Presets/Ability/AbilityPreset.tscn` | 旧技能组件组合预设 | DataOS `ability/*`、`feature.definition/*` | rewrite/DataOS | DataOS-only | `cd /home/slime/Code/SkilmeAI/SkilmeAI && Tools/run-dataos-validate.sh` | 只证明数据结构可验证；没有可加载 AbilityPreset 场景。 |
| `Else/brotato-my/Src/ECS/Base/Component/Presets/Unit/{EnemyPreset,PlayerPreset,UnitCorePreset}.tscn` | 旧单位组件组合预设 | DataOS `unit.enemy/*`、`unit.player/deluyi`、runtime spawn wrappers | rewrite/DataOS | 部分迁移 | 已有记录：`cd Games/BrotatoLike && Tools/run-godot-smoke.sh` | 旧 preset 场景未迁；缺普通主场景玩家/敌人完整行为验收。 |
| `Else/brotato-my/Src/ECS/Base/Component/Unit/Common/{AttackComponent,DataInitComponent,HealthComponent,LifecycleComponent,RecoveryComponent,UnitAnimationComponent,UnitStateComponent}.tscn` | 单位攻击、数据初始化、生命、生命周期、恢复、动画、状态 | GameOS Attack/Damage/Unit/GodotBridge、DataOS `unit.*`、`GodotUnitAnimationComponent.cs` | rewrite/DataOS | 部分迁移 | 已有记录：`cd /home/slime/Code/SkilmeAI/SkilmeAI && Tools/run-build.sh && Tools/run-tests.sh` | Recovery/UnitState/Lifecycle 的游戏侧完整闭环未验收；旧节点场景未迁。 |
| `Else/brotato-my/Src/ECS/Base/Component/Unit/Enemy/AI/AIComponent.tscn` | 敌人 AI 组件 | `SkilmeAI/GameOS/Capabilities/AI`、`GodotAIComponent.cs` | rewrite | 部分迁移 | 已有记录：`cd /home/slime/Code/SkilmeAI/SkilmeAI && Tools/run-build.sh && Tools/run-tests.sh` | 主场景敌人追逐、攻击链路未有 PASS marker。 |
| `Else/brotato-my/Src/ECS/Base/Component/Unit/TargetingIndicatorControlComponent/TargetingIndicatorControlComponent.tscn` | 目标指示器控制 | Ability point target、DataOS `unit.targeting_indicator/default` | rewrite/DataOS | 部分迁移 | 已有记录：`cd Games/BrotatoLike && Tools/run-godot-smoke.sh` | 真实鼠标/手柄点选控制未验收。 |
| `Else/brotato-my/Src/ECS/Base/System/DamageSystem/DamageService.tscn` | 旧伤害服务系统节点 | `SkilmeAI/GameOS/Capabilities/Damage/DamageService.cs` | rewrite | 部分迁移 | 已有记录：`cd /home/slime/Code/SkilmeAI/SkilmeAI && Tools/run-build.sh && Tools/run-tests.sh` | 旧系统场景未迁；BrotatoLike 主场景伤害数字/死亡链路未完整验收。 |
| `Else/brotato-my/Src/ECS/Base/System/DamageSystem/DamageStatisticsSystem.tscn` | 伤害统计系统 | DataOS `system.config/DamageStatisticsSystem`、GameOS Damage statistics processor | rewrite/DataOS | DataOS-only | `cd /home/slime/Code/SkilmeAI/SkilmeAI && Tools/run-dataos-validate.sh` | 没有游戏侧统计 UI 或主场景统计验收。 |
| `Else/brotato-my/Src/ECS/Base/System/MouseSelection/MouseSelectionSystem.tscn` | 调试鼠标选择系统 | DataOS `system.config/MouseSelectionSystem` | DataOS | DataOS-only | `cd /home/slime/Code/SkilmeAI/SkilmeAI && Tools/run-dataos-validate.sh` | 没有新调试选择系统或 UI。 |
| `Else/brotato-my/Src/ECS/Base/System/PauseMenu/PauseMenuSystem.tscn` | 暂停菜单系统 | DataOS `system.config/PauseMenuSystem`、GameOS overlay/schedule state | rewrite/DataOS | DataOS-only | `cd /home/slime/Code/SkilmeAI/SkilmeAI && Tools/run-dataos-validate.sh` | 暂停菜单 UI 未迁；只保留 schedule config。 |
| `Else/brotato-my/Src/ECS/Base/System/RecoverySystem/RecoverySystem.tscn` | 恢复系统 | DataOS `system.config/RecoverySystem`、Damage/Heal capability | rewrite/DataOS | DataOS-only | `cd /home/slime/Code/SkilmeAI/SkilmeAI && Tools/run-dataos-validate.sh` | 游戏侧恢复逻辑和验收未接入。 |
| `Else/brotato-my/Src/ECS/Base/System/Spawn/SpawnSystem.tscn` | 敌人/道具生成系统 | `Games/BrotatoLike/Src/Game/BrotatoLikeEnemySpawnSystem.cs`、DataOS `spawn.config/default`、`system.config/SpawnSystem` | rewrite/DataOS | 部分迁移 | 已有记录：`cd Games/BrotatoLike && Tools/run-godot-smoke.sh` | 第 1 波 smoke 覆盖；普通主场景波次 PASS marker、道具生成未完成。 |
| `Else/brotato-my/Src/ECS/Base/System/TestSystem/**/*` | 旧运行时测试面板和测试模块场景 | `Games/BrotatoLike/Tools/run-godot-scene.sh`、`Tools/analyze-godot-scene-logs.sh` | replace | 部分迁移 | 已有记录：`cd Games/BrotatoLike && Tools/run-godot-scene.sh list` | 旧可视测试模块没有迁入；需要按 Observation contract 重建可发现测试场景。 |
| `Else/brotato-my/Src/ECS/UI/Core/*` | UI 基类、主题、UIManager | 无正式 UI 目标；DataOS `system.config/UIManager` | DataOS/drop-candidate | DataOS-only | `cd /home/slime/Code/SkilmeAI/SkilmeAI && Tools/run-dataos-validate.sh` | UI 管理节点和主题未迁；不能视为 HUD 已完成。 |
| `Else/brotato-my/Src/ECS/UI/UI/DamageNumberUI/*` | 伤害数字 UI 和 runtime bridge | DataOS `system.config/DamageNumberRuntimeBridge` | DataOS | DataOS-only | `cd /home/slime/Code/SkilmeAI/SkilmeAI && Tools/run-dataos-validate.sh` | 没有新伤害数字 UI 场景；主场景没有可视伤害数字验收。 |
| `Else/brotato-my/Src/ECS/UI/UI/HealthBarUI/*` | 生命条 UI | DataOS `Unit.HealthBarHeight`、`Unit.IsShowHealthBar` | DataOS | DataOS-only | `cd /home/slime/Code/SkilmeAI/SkilmeAI && Tools/run-dataos-validate.sh` | 没有新生命条 UI 场景；主场景 HUD 未完成。 |
| `Else/brotato-my/Src/ECS/UI/UI/SkillUI/{ActiveSkillBarUI,ActiveSkillSlotUI}/*` | 主动技能栏和技能槽 UI | 无正式 UI 目标；DataOS `ability/*` common fields | DataOS | DataOS-only | `cd /home/slime/Code/SkilmeAI/SkilmeAI && Tools/run-dataos-validate.sh` | 技能栏 UI 未迁；技能切换/冷却显示未验收。 |
| `Else/brotato-my/Data/Data/Ability/Ability/Slam/*`、`Else/brotato-my/Data/Data/Ability/Resource/SlamConfig.tres` | 猛击技能逻辑和配置 | DataOS `ability/slam`、`feature.definition/slam`、`BrotatoLikeAbilityHandlers.cs` | rewrite/DataOS | 部分迁移 | 已有记录：`cd Games/BrotatoLike && Tools/run-godot-smoke.sh` | smoke 覆盖范围伤害和 Effect 事件；缺旧技能逐项视觉、冷却、目标选择主场景验收。 |
| `Else/brotato-my/Data/Data/Ability/Ability/ChainLightning/**/*` | 闪电链技能逻辑、链式参数和配置 | DataOS `ability/chain_lightning`、`feature.definition/chain_lightning`、`BrotatoLikeAbilityHandlers.cs` | rewrite/DataOS | 部分迁移 | 已有记录：`cd Games/BrotatoLike && Tools/run-godot-smoke.sh` | 延迟命中 smoke 已覆盖；连线视觉路径为空，缺主场景验收。 |
| `Else/brotato-my/Data/Data/Ability/Ability/CircleDamage/CircleDamage.cs`、`Else/brotato-my/Data/Data/Ability/Resource/CircleDamageConfig.tres` | 圆环伤害技能 | DataOS `ability/circle_damage`、`BrotatoLikeAbilityHandlers.cs` | rewrite/DataOS | 部分迁移 | 已有记录：`cd Games/BrotatoLike && Tools/run-godot-smoke.sh` | 缺持续视觉和主场景周期伤害验收。 |
| `Else/brotato-my/Data/Data/Ability/Ability/Movement/ArcShot/ArcShot.cs`、`Else/brotato-my/Data/Data/Ability/Resource/Movement/ArcShotConfig.tres` | 圆弧射击 | DataOS `ability/arc_shot`、`BrotatoLikeAbilityHandlers.cs` | rewrite/DataOS | 部分迁移 | 已有记录：`cd Games/BrotatoLike && Tools/run-godot-smoke.sh` | 缺主场景可视轨迹和命中验收。 |
| `Else/brotato-my/Data/Data/Ability/Ability/Movement/BezierShot/BezierShot.cs`、`Else/brotato-my/Data/Data/Ability/Resource/Movement/BezierShotConfig.tres` | 贝塞尔射击 | DataOS `ability/bezier_shot`、`BrotatoLikeAbilityHandlers.cs` | rewrite/DataOS | 部分迁移 | 已有记录：`cd Games/BrotatoLike && Tools/run-godot-smoke.sh` | 缺主场景可视轨迹和命中验收。 |
| `Else/brotato-my/Data/Data/Ability/Ability/Movement/BoomerangThrow/BoomerangThrow.cs`、`Else/brotato-my/Data/Data/Ability/Resource/Movement/BoomerangThrowConfig.tres` | 回旋镖投掷 | DataOS `ability/boomerang_throw`、`BrotatoLikeAbilityHandlers.cs` | rewrite/DataOS | 部分迁移 | 已有记录：`cd Games/BrotatoLike && Tools/run-godot-smoke.sh` | 缺主场景往返命中和回收验收。 |
| `Else/brotato-my/Data/Data/Ability/Ability/Movement/Dash/Dash.cs`、`Else/brotato-my/Data/Data/Ability/Resource/Movement/DashConfig.tres` | 冲刺 | DataOS `ability/dash`、`BrotatoLikeAbilityHandlers.cs` | rewrite/DataOS | 部分迁移 | 已有记录：`cd Games/BrotatoLike && Tools/run-godot-smoke.sh` | 缺玩家输入触发和主场景位移验收。 |
| `Else/brotato-my/Data/Data/Ability/Ability/Movement/OrbitSkill/OrbitSkill.cs`、`Else/brotato-my/Data/Data/Ability/Resource/OrbitSkillConfig.tres` | 环绕技能 | DataOS `ability/orbit_skill`、`BrotatoLikeAbilityHandlers.cs` | rewrite/DataOS | 部分迁移 | 已有记录：`cd Games/BrotatoLike && Tools/run-godot-smoke.sh` | 缺主场景持续环绕、碰撞和清理验收。 |
| `Else/brotato-my/Data/Data/Ability/Ability/Movement/ParabolaShot/ParabolaShot.cs`、`Else/brotato-my/Data/Data/Ability/Resource/Movement/ParabolaShotConfig.tres` | 定点抛炸弹 | DataOS `ability/parabola_shot`、`BrotatoLikeAbilityHandlers.cs` | rewrite/DataOS | 部分迁移 | 已有记录：`cd Games/BrotatoLike && Tools/run-godot-smoke.sh` | 缺主场景落点、爆炸范围和周期触发验收。 |
| `Else/brotato-my/Data/Data/Ability/Ability/Movement/SineWaveShot/SineWaveShot.cs`、`Else/brotato-my/Data/Data/Ability/Resource/Movement/SineWaveShotConfig.tres` | 正弦波射击 | DataOS `ability/sine_wave_shot`、`BrotatoLikeAbilityHandlers.cs` | rewrite/DataOS | 部分迁移 | 已有记录：`cd Games/BrotatoLike && Tools/run-godot-smoke.sh` | 缺主场景可视轨迹和命中验收。 |
| `Else/brotato-my/Data/Data/Ability/Resource/TargetPointSkillConfig.tres` | 位置目标技能配置 | DataOS `ability/target_point_skill`、`BrotatoLikeAbilityHandlers.cs` | rewrite/DataOS | 部分迁移 | 已有记录：`cd Games/BrotatoLike && Tools/run-godot-smoke.sh` | 缺真实点选输入、指示器和主场景验收。 |
| `Else/brotato-my/Data/Data/Ability/AbilityConfig.cs` | 旧技能配置基类 | DataOS `Ability.*` 字段、GameOS `AbilityDataKeys` | rewrite/DataOS | DataOS-only | `cd /home/slime/Code/SkilmeAI/SkilmeAI && Tools/run-dataos-validate.sh` | 旧配置类未保留；字段覆盖仍需逐技能对照。 |
| `Else/brotato-my/Data/DataNew/Ability/*` | 新旧 Ability authoring 数据结构 | `Games/BrotatoLike/DataOS/Authoring/BrotatoLike.seed.sql`、runtime snapshot `ability/*` | rewrite/DataOS | DataOS-only | `cd /home/slime/Code/SkilmeAI/SkilmeAI && Tools/run-dataos-validate.sh` | 只证明 authoring -> snapshot；不证明技能行为完整。 |
| `Else/brotato-my/Data/DataNew/Feature/*` | Feature definition 数据 | DataOS `feature.definition/*`、`feature.modifier/*` | rewrite/DataOS | DataOS-only | `cd /home/slime/Code/SkilmeAI/SkilmeAI && Tools/run-dataos-validate.sh` | Feature action 剩余项未逐项迁移。 |
| `Else/brotato-my/Data/DataNew/System/*` | System config/preset 数据结构 | DataOS `system.config/*`、`system.preset/Default`、GameOS RuntimeSchedule | rewrite/DataOS | DataOS-only | `cd /home/slime/Code/SkilmeAI/SkilmeAI && Tools/run-dataos-validate.sh` | 多个旧系统只有配置记录，没有新运行系统。 |
| `Else/brotato-my/Data/DataNew/Unit/**/*` | 玩家、敌人、目标指示器数据结构 | DataOS `unit.player/*`、`unit.enemy/*`、`unit.targeting_indicator/*` | rewrite/DataOS | DataOS-only | `cd /home/slime/Code/SkilmeAI/SkilmeAI && Tools/run-dataos-validate.sh` | 只证明单位数据；主场景完整行为仍缺验收。 |
| `Else/brotato-my/Data/Config/GlobalConfig.cs` | 全局配置常量 | DataOS seed、`BrotatoLikeGameRuntime` 默认参数 | rewrite/DataOS | 部分迁移 | 已有记录：`cd Games/BrotatoLike && Tools/run-build.sh` | 需要逐项确认旧全局配置是否进入 DataOS 或新 runtime。 |
| `Else/brotato-my/Data/Config/Spawn/SpawnSystemConfig.cs` | 生成系统配置常量 | DataOS `spawn.config/default`、`BrotatoLikeEnemySpawnSystem.cs` | rewrite/DataOS | 部分迁移 | 已有记录：`cd Games/BrotatoLike && Tools/run-godot-smoke.sh` | 只覆盖第 1 波 smoke；完整波次和道具生成未验收。 |
| `Else/brotato-my/Data/Config/System/Preset/**/*` | 系统预设资源 | DataOS `system.preset/Default` | rewrite/DataOS | DataOS-only | `cd /home/slime/Code/SkilmeAI/SkilmeAI && Tools/run-dataos-validate.sh` | 只保留预设记录；旧 `.tres` 未迁为资源。 |
| `Else/brotato-my/Data/Config/System/System/**/*` | 系统配置资源和类型 | DataOS `system.config/*`、GameOS `RuntimeSchedule` | rewrite/DataOS | DataOS-only | `cd /home/slime/Code/SkilmeAI/SkilmeAI && Tools/run-dataos-validate.sh` | 多数旧系统没有新运行实现或 UI 验收。 |
| `Else/brotato-my/Data/ResourceManagement/ResourcePaths.cs` | 旧资源路径目录 | `Games/BrotatoLike/DataOS/Snapshots/runtime_snapshot.json` `resources[]`、GameOS `ResourceCatalog` | rewrite/DataOS | 遗留引用 | `cd /home/slime/Code/SkilmeAI/SkilmeAI && Tools/run-dataos-validate.sh` | snapshot 仍含旧 `res://Src/...` 和 `res://Data/...` 路径；必须逐项迁移、替换、删除或标记 intentionally dropped。 |
| `Else/brotato-my/Src/ECS/Test/GlobalTest/**/*` | 旧全局测试场景和视觉预览 | `Games/BrotatoLike/Tools/run-godot-scene.sh`、后续 Observation contract | replace | 未迁移 | 未验证：旧全局测试场景未进入新仓库。 | 需要决定迁移为 headless scene test、可视调试场景，或废弃。 |
| `Else/brotato-my/Src/ECS/Test/SingleTest/ECS/**/*` | 旧 ECS 单项测试 | GameOS runtime tests、BrotatoLike scene runner | replace | 部分迁移 | 已有记录：`cd /home/slime/Code/SkilmeAI/SkilmeAI && Tools/run-build.sh && Tools/run-tests.sh` | GameOS tests 覆盖框架行为，不等价于旧 Godot 测试场景迁移。 |
| `Else/brotato-my/Src/ECS/Test/SingleTest/Tools/**/*` | 输入、日志、数学、对象池、目标选择工具测试 | GameOS tests、BrotatoLike scene runner | replace | 部分迁移 | 已有记录：`cd /home/slime/Code/SkilmeAI/SkilmeAI && Tools/run-build.sh && Tools/run-tests.sh` | 旧可视工具测试未迁；需要按新 Observation/scene test 入口重建需要的测试。 |

## DataOS-only 与遗留引用说明

当前 `Games/BrotatoLike/DataOS/Snapshots/runtime_snapshot.json` 同时包含两类路径：

- 已迁到新资源目录的路径，例如 `res://assets/Unit/...`、`res://assets/Projectile/...`、`res://assets/Effect/...`。
- 仍指向旧结构的路径，例如 `res://Src/ECS/Base/Entity/...`、`res://Src/ECS/Base/System/...`、`res://Src/ECS/UI/...`、`res://Data/...`。

第二类只能说明旧路径被记录到 snapshot 或 ResourceCatalog，不能说明资源存在。后续 R07 或 DataOS manifest validation 需要把这些路径逐项标记为 migrated、legacy、missing 或 intentionally dropped。

## 后续拆分规则

- R07 可玩切片触碰任一 `部分迁移` 行时，必须在本台账中补充实际命令和结果。
- 如果实现开始依赖某个分组行中的单个旧场景或配置，先把该分组拆成单项行。
- 如果决定废弃旧输入，必须写明替代目标、废弃原因和验证证据，不能只删除路径。
