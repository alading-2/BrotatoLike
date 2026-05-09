---
name: gameos-reference
description: 在游戏仓库中需要读取 SkilmeAI.GameOS 契约、API 索引或框架源码位置时使用。
---

# GameOS 引用入口

## 步骤

1. 阅读 `DocsAI/ExternalFrameworkMap.md`。
2. 优先读取框架发布的 Contracts / ApiIndex。
3. Runtime API 当前已覆盖 Data / Event / Entity / Relationship / Schedule / Resource / Pool / Timer，Capability 已覆盖 Movement / Collision / Damage / Ability / Projectile / Effect / Feature / AI / Attack 第一批，GodotBridge 已覆盖 GodotEntity / Component / Movement / Orientation / Collision / ContactDamage / Attack / AI / ProjectileEffectSpawner / NodePool。
4. 如果还没有发布文档，再读取本地框架源码。
5. 默认不在游戏仓库直接修改框架源码。

## 框架入口

```text
/home/slime/Code/SkilmeAI/SkilmeAI/GameOS/SkilmeAI.GameOS.Contracts.md
/home/slime/Code/SkilmeAI/SkilmeAI/GameOS/SkilmeAI.GameOS.ApiIndex.md
/home/slime/Code/SkilmeAI/SkilmeAI/GameOS/SkilmeAI.GameOS.DebugGuide.md
```
