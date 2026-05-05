# ExternalFrameworkMap

## SkilmeAI.GameOS

当前使用本地源码项目引用：

```text
framework_repo: /home/slime/Code/SkilmeAI/SkilmeAI
framework_project: /home/slime/Code/SkilmeAI/SkilmeAI/GameOS/SkilmeAI.GameOS.csproj
framework_solution: /home/slime/Code/SkilmeAI/SkilmeAI/SkilmeAI.slnx
framework_local_nuget: /home/slime/Code/SkilmeAI/SkilmeAI/Packages/LocalNuGet
engine_source_path: /home/slime/Code/SkilmeAI/Engine/godot-4.6.2-stable
source_input_repo: /home/slime/Code/Godot/Games/MyGames/brotato-my
copied_assets: /home/slime/Code/SkilmeAI/Games/BrotatoLike/assets
migration_input: /home/slime/Code/SkilmeAI/Games/BrotatoLike/MigrationInput
```

## 规则

- 游戏任务默认只读取框架契约和 API 索引。
- 框架源码修改必须切换到框架仓库。
- 框架升级后必须在本仓库跑构建和游戏回归。

## 当前框架状态

- `SkilmeAI.GameOS` 已可 build，并包含 Data / Event / Entity / Relationship / Schedule / Resource / Pool / Timer Runtime 最小内核、Movement Capability 旧 `MoveMode` 纯 C# 策略 + Godot 2D 位移桥 + MovementCollision + 同帧多命中 + Godot Physics broadphase + Godot Orientation、Collision Capability 纯运行时第一批、Damage / ContactDamage / Damage 处理器管线 / HealService / DamageTool 第一批、Ability Runtime 最小切片 + 点选目标语义 + 自动索敌第一段 + Periodic 自动触发 Tick、Projectile / Effect Runtime 生成第一段、Projectile 命中生命周期、穿透 / 生命周期扩展、Effect 动画播放第一段和 Godot 实例化第一段、Feature Runtime 最小生命周期、AI Runtime 最小行为树 + 最近目标查询 + 巡逻 + 行为树预制块 + Ability 自动索敌上下文准备 + Godot AI bridge + 攻击请求事件、Attack Runtime 最小结算、GodotAttackComponent bridge 第一段、旧 AttackComponent 兼容包装、GodotUnitAnimationComponent 动画事件桥第一段和 GodotBridge 第一版。
- 本地 NuGet 包已可由 `Tools/run-pack.sh` 生成。
- 游戏仓库已建立本地 `ProjectReference`。
- 游戏仓库当前通过 `GameBootstrap.RunFrameworkSmokeProbe()` 调用框架 Runtime API，覆盖 Relationship / Schedule / Movement / Collision；`Src/Game/Main.cs` 额外创建 `GodotEntity + SmokeGodotComponent + GameOSTimerDriver`、`GodotEntity2D + GodotMovementDriver`、`GodotOrientationComponent`、`GodotContactDamageComponent`、`GodotAttackComponent`、旧 `AttackComponent`、`GodotAIComponent`、`GodotUnitAnimationComponent`、`GodotProjectileEffectSpawner`、Ability 点选目标、Ability 自动索敌、Projectile / Effect Runtime 生成与 Godot 实例化、Effect 动画播放、Projectile 命中生命周期、Projectile 穿透 / MaxLifeTime 销毁和 `GodotAreaEntity2D + CollisionShape2D` 验证 GodotBridge 编译接入，Movement smoke 覆盖旧 `MoveMode` 纯运行时策略、Godot Physics broadphase 和 Godot Orientation，Damage smoke 覆盖处理器管线后的接触伤害和 Projectile 命中伤害，Attack smoke 覆盖导出参数写入、节点目标解析、HP 扣减、旧 AttackComponent 兼容和 attack / idle 动画事件桥，AI smoke 覆盖导出参数写入、巡逻意图和 AIControlled 位移闭环。Feature Runtime、Ability Periodic 自动触发和 AI Ability 自动索敌上下文准备当前由框架 `Tools/run-tests.sh` 覆盖。
- 旧 `assets/` 已复制并保留小写路径，旧 `Data/` 和 `Src/Main/` 已复制到 `MigrationInput/`，暂不编译。

## 当前验证命令

```bash
cd /home/slime/Code/SkilmeAI/SkilmeAI
Tools/run-build.sh
Tools/run-tests.sh

cd /home/slime/Code/SkilmeAI/Games/BrotatoLike
Tools/run-build.sh
Tools/run-godot-smoke.sh
```
