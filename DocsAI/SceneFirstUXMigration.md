# BrotatoLike Scene-first UX 迁移原则

> 日期：2026-05-20  
> 适用范围：`Games/BrotatoLike` 从 `Resources/Else/brotato-my` 迁移玩家可见 UI、HUD、菜单、目标指示器、伤害数字、技能栏、视觉特效和验证场景时使用。  
> 关联计划：`openspec/changes/scene-first-brotatolike-ux-migration/`

## 结论

BrotatoLike 的玩家可见 UX 迁移应优先使用 Godot 场景，而不是在 C# 中大量创建 UI 节点。这里的“使用场景”包括加载并实例化 `PackedScene`；运行时代码负责绑定 Runtime Data、事件和交互状态，不负责把复合 UI 布局一行行拼出来。

这不是要求所有 UI 都必须来自 `.tscn`。少量、临时、调试或纯验证节点可以用代码创建；但正式玩家可见的复合 UI、菜单、技能槽、血条、飘字、目标指示器和特效节点，默认必须有场景资产，除非在迁移台账里写明例外原因和后续替代计划。

## 规则

### 优先路径

1. 先查旧项目是否已有可用 `.tscn`。
2. 能复用视觉结构时，迁入或改造成 BrotatoLike-owned scene。
3. 旧 C# 逻辑不复制，重写为 AI-first 的 Runtime Data / Event / Ability / Damage / Schedule 绑定。
4. 运行时代码通过 `PackedScene` 实例化场景，再绑定节点引用和数据。
5. 验证 artifact 必须记录关键 UI/视觉节点的 scene path 或 scene-backed evidence。

### 禁止模式

- 禁止在正式 gameplay code 中大量 `new Label`、`new Control`、`new ProgressBar`、`new HBoxContainer` 等拼出玩家可见复合 UI。
- 禁止把 headless validation 动态创建的 UI 当成正式迁移完成证据。
- 禁止因为旧 ECS / UIManager 架构不适合迁移，就跳过旧 `.tscn` 的视觉结构审查。
- 禁止把 DataOS `resources[]` 中的 legacy path 分类当成功能迁移完成。

### 允许例外

- 简单一次性 debug marker、测试探针、纯验证 scene 内部节点。
- 极小的非复合状态节点，例如只有 metadata 的 runtime session node。
- 由场景实例化后，代码动态填充文本、图标、数值、进度和状态。
- 需要对象池时，池中对象也应优先来自 `PackedScene`。

## 当前问题证据

`restore-brotatolike-playable-ux` 已补齐多个行为闭环，但当前实现仍有明显 scene-first 缺口：

- `Games/BrotatoLike/Scenes/Main.tscn` 只有 `Main`、`GameRuntime`、`Camera2D`，没有正式 HUD / Menu / UX scene composition。
- `Games/BrotatoLike/Src/Game/UI/BrotatoLikeHud.cs` 用代码创建 `Control`、`Label`、`HBoxContainer`、`ProgressBar`，属于需要整改的正式 UI 拼装。
- `Games/BrotatoLike/Src/Game/Progression/BrotatoLikeProgressionService.cs` 用代码创建暂停菜单、升级反馈和经验拾取层，其中暂停菜单和反馈 UI 应改为 scene-backed。
- `Games/BrotatoLike/Src/Game/BrotatoLikeTargetingController.cs` 只创建 `Node2D` 指示器，未迁入旧 TargetingIndicator 场景视觉结构。
- `BrotatoLikePlayableUXValidation.tscn`、`BrotatoLikeProgressionLoopValidation.tscn`、`BrotatoLikeLegacyResourceClassificationValidation.tscn` 本身只是挂脚本的轻场景，适合逻辑验收，但不能替代正式 UX 场景资产。

旧项目中值得优先采纳的场景输入：

| 旧场景 | 采纳建议 | 新目标 |
| --- | --- | --- |
| `Resources/Else/brotato-my/Src/ECS/UI/UI/HealthBarUI/HealthBarUI.tscn` | Adopt Now | 改造成 BrotatoLike head health bar scene，脚本重写绑定 Runtime HP。 |
| `Resources/Else/brotato-my/Src/ECS/UI/UI/DamageNumberUI/DamageNumberUI.tscn` | Adopt Now | 保留 Label + AnimationPlayer 思路，重写伤害/治疗事件绑定和生命周期。 |
| `Resources/Else/brotato-my/Src/ECS/UI/UI/SkillUI/ActiveSkillBarUI.tscn` | Adopt Now | 改造成四槽技能栏 scene，运行时代码只绑定 owned abilities。 |
| `Resources/Else/brotato-my/Src/ECS/UI/UI/SkillUI/ActiveSkillSlotUI.tscn` | Adopt Now | 改造成技能槽 scene，保留图标、冷却遮罩、充能、按键、名称节点。 |
| `Resources/Else/brotato-my/Src/ECS/Base/System/PauseMenu/PauseMenuSystem.tscn` | Adopt Now | 改造成 BrotatoLike pause menu scene，不恢复旧 SystemManager 逻辑。 |
| `Resources/Else/brotato-my/Src/ECS/Base/Entity/Unit/TargetingIndicator/TargetingIndicatorEntity.tscn` | Adopt Now | 改造成点选指示器 scene，接入新 `BrotatoLikeTargetingController`。 |
| `Resources/Else/brotato-my/Src/ECS/Base/Entity/Effect/LightningLineEffect/LightningLineEffect.tscn` | Adopt Now | 改造成 chain lightning Line2D VFX scene，补 `LineEffectScenePath`。 |
| 旧 `PlayerEntity.tscn` / `EnemyEntity.tscn` / `UnitCorePreset.tscn` | Adopt Later | 当前优先保留 `GodotUnitComposer` AI-first 组合方式，只提炼 scene shell 或视觉子场景。 |
| 旧 `UIManager.tscn` / `UIManager.cs` | Reject | 不恢复旧 UIManager / 旧对象池架构；只采纳必要 scene asset 和布局思想。 |

## 验收口径

后续涉及正式 UI/UX 的任务，不能只验收“节点存在”或“数值变化”。必须补充以下证据：

- artifact 记录 UI / VFX 的 `PackedScene` 路径或 scene-backed 标记。
- `README.md` 五字段完整：`expectedInputs`、`expectedObservations`、`passCriteria`、`failCriteria`、`artifactPath`。
- 检查 `index.json`、per-scene `result.json` 和 scene artifact。
- 对正式 UI code 进行静态扫描，发现大量 `new Label/new Control/new ProgressBar/new Container` 时必须解释或重构。
- 主场景和专项 validation 都要通过；`run-main-smoke` 只能作为回归补充。

## 与 AI-first 的关系

AI-first 不等于放弃 Godot 引擎能力。正确分层是：

- GameOS / DataOS / Ability / Damage / Movement 负责可验证逻辑。
- Godot scene 负责玩家可见结构、布局、动画和可编辑资产。
- C# code-behind 负责把 Runtime Data、Entity.Events、WorldEvents 和 input action 绑定到 scene。
- 迁移的是功能和体验，不复制旧代码；但旧 scene 的视觉结构是迁移输入，不能默认跳过。

## 后续 OpenSpec

执行计划见：

```text
openspec/changes/scene-first-brotatolike-ux-migration/
```

该 change 应先把场景优先规则变成验收 contract，再逐步替换当前代码拼出来的正式 UI/UX。
