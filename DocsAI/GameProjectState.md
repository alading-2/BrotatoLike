# BrotatoLike GameProjectState

> 更新日期：2026-05-05

## 当前状态

框架接入基线已创建。框架仓库已有 `SkilmeAI.GameOS` Runtime 最小内核、DataOS SQLite schema / migration / generator / validator / Runtime snapshot loader、GodotBridge 第一版、Node 对象池 / 碰撞隔离扩展第一段、Movement Capability 旧 `MoveMode` 纯 C# 策略 + Godot 2D 位移桥、Movement 运动碰撞、同帧多命中、Godot Physics broadphase 和 Godot Orientation 输出，Collision Capability 纯运行时和 Godot bridge 第一批，以及 Damage / ContactDamage / Damage 处理器管线 / HealService / DamageTool 第一批、Ability Runtime 最小切片 + 点选目标语义 + 自动索敌第一段 + Periodic 自动触发 Tick、Projectile / Effect Runtime 生成第一段、Projectile 命中生命周期、Projectile 穿透 / 生命周期扩展、Effect 动画播放第一段和 Godot 实例化第一段、Feature Runtime 最小生命周期、AI Runtime 最小行为树 + 最近目标查询 + 巡逻 + 行为树预制块 + Ability 自动索敌上下文准备 + Godot AI bridge + 攻击请求事件、Attack Runtime 最小结算、GodotAttackComponent bridge 第一段、旧 AttackComponent 兼容包装、GodotUnitAnimationComponent 动画事件桥第二段和旧 Attack 动画选择兼容第一段；本游戏已建立最小 Godot C# 项目、本地框架项目引用、`Scenes/Main.tscn` 启动场景、`GameBootstrap.RunFrameworkSmokeProbe()`、DataOS 第一批 authoring seed / runtime snapshot、Movement / MovementCollision / Godot Physics / Godot Orientation / Collision / Damage / ContactDamage / Attack / 旧 AttackComponent 兼容 / Attack Animation / AI bridge / Ability 点选目标 / Ability 自动索敌 / Projectile / Effect Runtime 与 Godot 实例化 / Effect 动画播放 / Projectile 命中生命周期 / 穿透 / MaxLifeTime 销毁 smoke 和 GodotBridge 探针。

当前 smoke probe 覆盖：

- Runtime Entity + Data -> Entity.Events 变更事件。
- Runtime Relationship 父子归属关系。
- Runtime Schedule 项目状态门禁。
- Runtime Pool 预热和释放。
- Runtime Timer Tick。
- Runtime ResourceCatalog 路径映射。
- DataOS `DataOS/Authoring/BrotatoLike.seed.sql` 生成 `DataOS/Snapshots/runtime_snapshot.json`；Godot smoke 读取 snapshot，将 `unit.enemy/yuren` 和 `ability/slam` 写入 Runtime Data，并注册 `ResourceCatalog` 资源映射。
- Collision `CollisionSystem / CollisionLayers / CollisionDataKeys / GameEventType.Collision` 纯 Runtime 事件。
- MovementCollision 纯 Runtime 线段 / 圆形扫描、碰撞停止、GodotMovementDriver Node2D 接触点同步，以及 `GodotAreaEntity2D + CollisionShape2D` Physics broadphase 候选验证。
- Godot Collision bridge `GodotAreaEntity2D / GodotCollisionComponent / GodotHurtboxComponent` 手动发射进入/离开事件 smoke。
- Movement `MovementSystem` 和旧 `MoveMode` 纯 Runtime 轨迹推进。
- GodotMovementDriver + GodotEntity2D 真实 `Node2D.Position` 同步，headless smoke 覆盖 Charge / Orbit / SineWave / BezierCurve / Boomerang / AttachToHost / PlayerInput / AIControlled / Parabola / CircularArc。
- GodotOrientationComponent + GodotEntity2D root `RotationDegrees` 输出，headless smoke 覆盖 FollowMovement 和 `MovementParams.Orientation` SpinOnly。
- DamageService 处理器管线 + GodotContactDamageComponent，headless smoke 覆盖 Hurtbox 进入造成接触伤害和同队过滤。
- FeatureService 最小生命周期由框架 Runtime tests 覆盖，包含 Modifier 授予 / 回滚、handler 生命周期和 AbilityService 调用 Feature handler；尚未接入本游戏 Godot smoke。
- AbilityService 点选目标语义和 Periodic 自动触发 Tick 由框架 Runtime tests 覆盖；`AbilityTargetingTool` 自动索敌第一段由框架 Runtime tests 覆盖同队 / 死亡 / 范围过滤和 AI 自动施法上下文准备；本游戏 Godot smoke 已覆盖 Point 目标正式触发和 Ability 自动索敌命中。
- ProjectileTool / EffectTool 纯 Runtime 生成由框架 Runtime tests 覆盖，包含 Data 写入、关系绑定、事件发布和 Effect 动画名写入；`ProjectileTool.StartMovement` 命中生命周期由框架 Runtime tests 覆盖，包含 MovementCollision、Projectile.Hit、DamageService 扣血、命中后销毁、穿透多目标和 MaxLifeTime 停止销毁；本游戏 Godot smoke 已覆盖项目侧调用、路径字符串、目标位置、关系绑定、`GodotProjectileEffectSpawner` 按 `ScenePath` 实例化投射物 / 特效视觉节点并注册到 `GodotNodeRegistry`，自动播放 Effect `AnimatedSprite2D`，以及投射物命中锁定目标、造成伤害、穿透两名目标、按生命周期销毁 Runtime 和清理视觉节点。
- AIService 最小行为树由框架 Runtime tests 覆盖，包含最近目标查询、追目标写入 Movement AI 意图、确定性左右巡逻和等待倒计时、行为树预制块攻击优先 / 追逐 / 巡逻回退、范围内发出 `GameEventType.Attack.Requested`、行为树中准备 Ability 自动索敌上下文并推进 Ability Periodic 自动触发；本游戏 Godot smoke 已覆盖 `GodotAIComponent` 导出参数写入、手动 Tick 写入巡逻移动意图，并由 `GodotMovementDriver + MoveMode.AIControlled` 同步到真实节点位置。
- AttackService 最小 Runtime 由框架 Runtime tests 覆盖，包含消费攻击请求事件、前摇 / 后摇 / 冷却 Timer、距离和死亡门禁，以及通过 DamageService 造成 `DamageTags.Attack` 伤害；本游戏 Godot smoke 已覆盖 `GodotAttackComponent` 导出参数写入、节点目标解析、攻击请求、HP 扣减、旧 `AttackComponent` 包装类保留已有 Attack Data 并结算伤害，以及 Attack Started / Cancelled 转发到 `GodotUnitAnimationComponent` 后从可用 `attack*` 动画回退选择、播放 attack / 取消回 idle / 一次性动画完成发布 `unit:animation_finished` 并回 idle。
- GodotBridge `GodotEntity / IGodotComponent / GameOSTimerDriver` 编译接入。
- GodotBridge `GodotNodePool<Area2D> / GodotCollisionIsolation / GodotNodePoolManager.ReturnToPool` 已接入 headless smoke，当前 `_Ready` 测试模式覆盖延迟激活、回池脱树和复用，失败会返回非 0。
- 旧 `assets/` 已复制到新仓库根目录 `assets/`，保留 `res://assets/...` 路径。
- 旧 `Data/` 和 `Src/Main/` 已复制到 `MigrationInput/`，当前排除编译，等待按模块适配。

## 下一步

1. 扩大 `MigrationInput/Data` 中 DataNew / 配置表到 DataOS authoring seed 的迁移范围，并替换临时硬编码 smoke 数据。
2. 将 `MigrationInput/Src/Main` 中仍属于游戏入口的逻辑迁入 `Src/Game/Main.cs` 或后续游戏系统。
3. 将 `Tools/run-godot-smoke.sh` 接入后续统一场景测试 runner。
4. 继续按 `Plans/README.md` 推进游戏仓库整体迁移，下一步是正式 Data / Capabilities。

## 最新验证

```bash
Tools/run-build.sh
Tools/run-godot-smoke.sh
```

结果：0 warning / 0 error；`Tools/run-build.sh` 会先生成 DataOS runtime snapshot；`Tools/run-godot-smoke.sh` 使用 `/home/slime/Code/Godot/GodotEngine/4.x/Godot_v4.6.2-stable_mono_linux_x86_64/Godot_v4.6.2-stable_mono_linux.x86_64` 完成 `--build-solutions --quit` 和 `Scenes/Main.tscn` headless smoke，覆盖 Runtime / DataOS snapshot / Movement / MovementCollision / Damage / ContactDamage / Attack / 旧 AttackComponent 兼容 / Attack Animation lifecycle / Attack 可用动画回退选择 / Godot AI bridge / Ability 点选目标 / Ability 自动索敌 / Projectile / Effect Runtime 与 Godot 实例化 / Effect 动画播放 / Projectile 命中生命周期 / 穿透 / MaxLifeTime 销毁 / Godot Physics broadphase / GodotMovementDriver / GodotOrientationComponent / GodotBridge / NodePool，输出 `BrotatoLike GameOS smoke PASS`。
