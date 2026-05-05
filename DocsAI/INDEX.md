# BrotatoLike DocsAI 索引

## 入口

- 游戏状态：`DocsAI/GameProjectState.md`
- 框架引用：`DocsAI/ExternalFrameworkMap.md`
- 整体迁移计划：`Plans/README.md`

## 当前阶段

框架接入基线已完成，GodotBridge 第一版已编译接入，DataOS 第一批 authoring seed / runtime snapshot、Movement Capability 旧 `MoveMode` 纯 C# 策略、Godot 2D 位移桥、MovementCollision、Godot Physics broadphase、Godot Orientation、DamageService 处理器管线、HealService、DamageTool、AbilityService、AbilityTargetingTool、ProjectileTool、EffectTool、FeatureService、AIService、AttackService、ContactDamage、GodotAttackComponent、AttackComponent 兼容包装、GodotAIComponent、GodotUnitAnimationComponent 和 GodotProjectileEffectSpawner 已进入可验证边界；当前游戏 smoke 覆盖 Runtime / DataOS snapshot / Movement / Collision / Damage / ContactDamage / Attack / 旧 AttackComponent 兼容 / Attack 动画事件 / Godot AI bridge / Ability 点选目标 / Ability 自动索敌 / Projectile / Effect Runtime 与 Godot 实例化 / Effect 动画播放 / Projectile 命中伤害、穿透和生命周期销毁 / GodotBridge 接入，Feature、Ability Periodic 自动触发、AI 最小行为树 / 最近目标查询 / 巡逻 / 行为树预制块 / 攻击请求事件和 Attack Runtime 结算由框架 Runtime tests 覆盖。旧 `assets/` 已复制，旧 `Data/` 和 `Src/Main/` 已放入 `MigrationInput/`；下一步扩大 DataOS 迁移范围并推进正式游戏入口。
