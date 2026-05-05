---
name: gameos-reference
description: 在游戏仓库中需要读取 SkilmeAI.GameOS 契约、API 索引或框架源码位置时使用。
---

# GameOS 引用入口

## 步骤

1. 阅读 `DocsAI/ExternalFrameworkMap.md`。
2. 优先读取框架发布的 Contracts / ApiIndex。
3. Runtime API 当前已覆盖 Data / Event / Entity / Relationship / Schedule / Resource / Pool / Timer，Movement Capability 第一段已覆盖纯 C# `MovementSystem / MoveMode.Charge / Orbit / SineWave / Parabola / CircularArc` 和 `GodotEntity2D / GodotMovementDriver`，GodotBridge 第一版已覆盖 GodotEntity / IGodotComponent / GameOSTimerDriver；先按契约使用，不从游戏仓库复制框架源码。
4. 如果还没有发布文档，再读取本地框架源码。
5. 默认不在游戏仓库直接修改框架源码。
