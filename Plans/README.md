# BrotatoLike 整体迁移计划

> 更新日期：2026-05-04
> 状态：执行中

## 定位

`BrotatoLike` 是 SkilmeAI 的第一个正式游戏仓库。旧 `brotato-my` 继续作为迁移输入仓库，新游戏仓库只保留游戏资产、场景、游戏特定代码、游戏数据和入口 Skill；框架能力通过 `SkilmeAI.GameOS` 引用。

## 迁移原则

- 旧代码不是全部重写。能直接复制且语义仍成立的游戏代码、数据和场景，先复制到新仓库再做最小适配。
- 框架 Runtime 不盲目复制旧 Godot 依赖；纯 C# 内核优先抽契约，Godot 生命周期进入 GodotBridge。
- 注释默认中文，公开 API 注释保持短，不把详细设计写进源码注释；详细设计进入 `DocsAI/` 或 `Plans/`。
- 游戏仓库不复制框架源码；框架 bug 切到 `/home/slime/Code/SkilmeAI/SkilmeAI` 修复。
- 引擎底层修改入口统一为 `/home/slime/Code/SkilmeAI/Engine/godot-4.6.2-stable`。

## 阶段计划

### G1：框架接入基线

已完成：

- Godot C# 项目骨架。
- `ProjectReference` 引用 `SkilmeAI.GameOS`。
- `Scenes/Main.tscn` 启动场景。
- `GameBootstrap.RunFrameworkSmokeProbe()` 覆盖 Data / Event / Entity / Relationship / Schedule / Resource / Pool / Timer。
- `GameBootstrap.RunFrameworkSmokeProbe()` 已追加 Movement `MovementSystem + MoveMode.Charge` 到点停止 smoke。
- `_Ready` 内 GodotBridge 探针已追加 `GodotEntity2D + GodotMovementDriver`，覆盖 Charge / Orbit / SineWave / BezierCurve / Boomerang / AttachToHost / PlayerInput / AIControlled / Parabola / CircularArc 的 Runtime Position 同步到真实 `Node2D.Position`。
- `_Ready` 内 GodotBridge 探针覆盖 `GodotEntity / IGodotComponent / GameOSTimerDriver` 编译接入。
- `_Ready` 内 GodotBridge 探针已追加 `GodotNodePool<Area2D>`，覆盖延迟激活、回池脱树、复用和 `GodotNodePoolManager.ReturnToPool` 编译接入。
- `Tools/run-godot-smoke.sh` 已建立，使用 Godot 4.6.2 mono headless 运行 `Scenes/Main.tscn`，并通过 `--gameos-smoke-exit` 让探针失败时返回非 0。

验收：

```bash
Tools/run-build.sh
Tools/run-godot-smoke.sh
```

### G2：直接迁移游戏资产和场景

输入：

- `/home/slime/Code/Godot/Games/MyGames/brotato-my/assets`
- 旧项目可复用 `.tscn` 场景。
- 旧 `Src/Main` 中属于游戏入口而非框架的逻辑。

做法：

- 先复制资产和场景，保持原路径可追踪。
- 再按新 `Src/Game` 边界改 namespace、引用和入口。
- 不在这一阶段重写 Movement / Collision / Damage / Ability / AI 框架能力。

当前状态：

- `assets/` 已复制到新仓库根目录，保留旧 `res://assets/...` 路径。
- `icon.svg` 和 `icon.svg.import` 已复制，兼容旧 Ability icon path。
- 旧 `Data/` 和 `Src/Main/` 已复制到 `MigrationInput/`，并通过 `BrotatoLike.csproj` 排除 `MigrationInput/**/*.cs`，避免旧框架依赖进入当前构建。

验收：

```bash
Tools/run-build.sh
GODOT_BIN=/path/to/Godot_v4.6.2-stable_mono_linux.x86_64 Tools/run-godot-smoke.sh
```

### G3：GodotBridge 后接真实运行时

依赖：

- `SkilmeAI.GameOS.Runtime` 已有纯 C# Relationship / Schedule。
- `GameOS/GodotBridge` 已有第一版和 M5.1 扩展第一段：Node Entity、Component 生命周期、`_Process` Timer bridge、Node 对象池、泊车 / 脱树和碰撞隔离工具。

输出：

- 当前 `_Ready` 内临时探针已升级为 GodotBridge headless smoke 断言。
- 后续接入统一 headless 场景测试 runner。

### G4：分批接 Capabilities

顺序：

1. Movement：旧 `MoveMode` 纯 C# 策略和 Godot 2D 位移桥已完成；下一段迁运动碰撞、朝向组件和真实输入/AI/宿主桥接。
2. Collision
3. Damage
4. Ability：最小 Runtime、点选目标语义、自动索敌第一段和 Periodic 自动触发已接入；后续补具体技能 handler。
5. Projectile / Effect：纯 Runtime 生成入口、Godot 场景实例化第一段、Effect 动画播放第一段、Projectile 命中生命周期、穿透和 MaxLifeTime 销毁已接入。
6. Feature
7. AI

原则：

- 能从旧仓库直接复制的组件和数据先复制，再删除旧框架耦合。
- 每个 Capability 在框架仓库先有 Contract、测试和 Debug 文档，再在游戏仓库接入。

### G5：DataOS 接管游戏数据

输出：

- 游戏数据 schema。框架侧已提供 DataOS core schema，游戏侧使用 seed 写入 authoring DB。
- 从旧 `Data/` 迁移出的 authoring 表。已完成第一批 `DataOS/Authoring/BrotatoLike.seed.sql`，覆盖玩家 / 敌人 / Ability 最小运行字段。
- runtime snapshot。已完成 `DataOS/Snapshots/runtime_snapshot.json`，由 `Tools/run-dataos-snapshot.sh` 生成。
- 数据校验命令。已接入框架 `DataOS/Validation/validate-dataos.sh`。

## 当前未完成

- 真实资产已复制；旧主场景和旧 Data 已进入 `MigrationInput/`，DataOS 已适配第一批运行字段，尚未全量覆盖旧 DataNew / 系统配置。
- 旧 `Src/Main` 游戏入口逻辑尚未迁入正式 `Src/Game`。
- GodotBridge 已迁入第一版并追加 Node 池化 / 碰撞隔离 headless smoke。
- Capability 已分批接入 smoke：DataOS snapshot / Movement / Collision / Damage / ContactDamage / Ability 点选目标 / Ability 自动索敌 / Projectile / Effect Runtime 与 Godot 实例化 / Effect 动画播放 / Projectile 命中生命周期 / 穿透 / MaxLifeTime 销毁 / AI bridge / Attack / 旧 AttackComponent 兼容。
- Godot CLI 默认路径已确认：`/home/slime/Code/Godot/GodotEngine/4.x/Godot_v4.6.2-stable_mono_linux_x86_64/Godot_v4.6.2-stable_mono_linux.x86_64`。
