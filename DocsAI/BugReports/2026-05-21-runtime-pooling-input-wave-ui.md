# 2026-05-21 Runtime Pooling / Input / Wave / UI Bug Report

## 用户反馈

- 特效结束后没有归还对象池；旧实现里 Effect、Projectile、HealthBar、DamageNumber、LightningLine 等都走对象池复用。
- 被攻击时画面附近出现很多内容，伤害飘字本身可能正常，但需要区分飘字、特效残留和头顶血条。
- 刷怪只刷几个后就没有了。
- 技能不能切换；输入和快捷键应参考旧 `ActiveSkillInputComponent` / `InputManager` 的切换与释放语义。
- 头顶血条高度不对：豺狼人偏高，鱼人偏低。

## 证据

externalResources:
  enabled:
    - legacy-godot-csharp-ecs
  scope:
    - Resources/Else/brotato-my/Src/ECS/Tools/ObjectPool/ObjectPoolInit.cs
    - Resources/Else/brotato-my/Src/ECS/Base/Component/Player/ActiveSkillInputComponent
    - Resources/Else/brotato-my/Src/ECS/Tools/Input
    - Resources/Else/brotato-my/Data/DataNew/Unit
  reason: 用户指定旧实现作为行为参考，用于核对对象池、输入和单位血条数据语义。
  expires: current-task

- 旧对象池：`ObjectPoolInit.cs` 注册 `EnemyPool`、`AbilityPool`、`HealthBarPool`、`DamageNumberUIPool`、`EffectPool`、`LightningLinePool`、`ProjectilePool`。
- 当前 `GodotProjectileEffectSpawner` 对视觉节点使用 `PackedScene.Instantiate()`，Runtime entity 销毁时直接 `QueueFree()`，没有自动按 `Effect.Duration` / 动画时长销毁 Runtime effect，也没有返回 `GodotNodePool`。
- 当前 `BrotatoLikeHud` 对头顶血条和伤害飘字使用 scene instantiate / `QueueFree()`，没有复用池。
- 当前技能槽 UI 显示 `1..4`，但输入链路只有 `UseSkill`、`PreviousSkill`、`NextSkill`；没有直接按槽位选择事件。
- 旧输入逻辑为 `LB/RB` 循环切换、`X` 释放当前主动技能；当前 gamepad LB/RB/X 映射存在，但 UI 数字提示不可操作。
- 旧单位默认 `HealthBarHeight` 是 `100f`；当前 DataOS descriptor 和 `BrotatoLikeHud` fallback 为 `0/36f`，鱼人没有显式高度所以落到 36f。豺狼人沿用旧值 155f，但当前视觉是 centered `AnimatedSprite2D`，该值偏高。
- 当前 `wave_enemy_entry` 明确配置第 1 波 2 个豺狼人 + 3 个鱼人，第 2 波 2 个鱼人 + 3 个豺狼人；因此“只刷几个”是当前验证波次 authoring，而不是 SpawnSystem 停摆。

## 初步根因

1. Effect / Projectile 视觉生命周期迁移时保留了 Runtime entity cleanup，但没有把 Godot 节点接回对象池，也没有为普通 effect 建立自动结束策略。
2. HUD 可见对象仍是一次性 UI 节点；高频战斗下会造成节点 churn，并让视觉残留和血条重叠更难判断。
3. 技能栏提示和输入协议不一致：UI 暗示数字直选，输入只支持循环切换。
4. 血条高度数据没有从旧默认完整迁移，且豺狼人显式高度需要按当前 centered sprite 校准。
5. 刷怪数量符合当前 `wave_authoring` 设计；如果需要持续刷怪，应修改 wave authoring / completion mode，而不是修 SpawnSystem。

## 验收方向

- Effect / Projectile / HUD 短生命周期视觉对象优先返回对象池；没有池映射时才允许 `QueueFree()` fallback。
- 普通 effect 在有限 `Effect.Duration` 或可推导动画时长结束后销毁 Runtime entity，并触发视觉回池。
- 技能栏数字槽位输入可切换到对应可见技能；LB/RB 循环切换和 X/Space 释放保持可用。
- 鱼人和豺狼人头顶血条高度按当前 sprite 校准，验证 artifact 记录两者高度和 canvas 坐标。
- Wave UI / artifact 明确记录当前波次是有限验证波，以及 expected spawn count。

## copiedCodeOrAssets

none
