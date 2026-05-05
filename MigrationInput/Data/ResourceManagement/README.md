# ResourceManagement - 资源管理系统

## 概述
`ResourceManagement` 是项目的统一资源加载入口。它通过 `ResourcePaths.cs`（由 `ResourceGenerator` 自动生成）获取资源路径，并提供类型安全的加载方法。

## 核心原则
1. **禁止硬编码**: 严禁在业务代码中使用 `res://` 开头的字符串路径。统一管理有利于路径变更时的重构。
2. **统一分类**: 资源必须属于 `ResourceCategory` 中的某一分类。
3. **类型安全**: 优先使用 `ResourceManagement.Load<T>` 进行加载。
4. **目录选择**: UI / 编辑器需要“列出可选资源”时，优先使用 `ResourceCatalog`，不要在运行时全盘扫描 `res://`。

## 使用方法

### 1. 加载场景 (PackedScene)
推荐使用 `typeof(T).Name` 进行类型安全的加载，确保代码重构（如类更名）时能自动保持同步。
```csharp
// 自动匹配类型名作为资源名
var scene = ResourceManagement.Load<PackedScene>(typeof(PlayerEntity).Name, ResourceCategory.Entity);
```

### 2. 加载配置 (Resource/tres)
```csharp
// 加载单个配置
var config = ResourceManagement.Load<Resource>(ResourcePaths.DataUnit_deluyi, ResourceCategory.DataUnit);

// 加载分类下所有配置
var allEnemies = ResourceManagement.LoadAll<Resource>(ResourceCategory.DataUnit, "Unit/Enemy");
```

### 3. 加载 UI
```csharp
var uiScene = ResourceManagement.Load<PackedScene>("HealthBarUI", ResourceCategory.UI);
```

### 4. 构建资源选择列表

`ResourceCatalog` 基于 `ResourcePaths.Resources` 生成选择器条目，分类由资源路径自动推导：

- `Data/Data/Unit/Enemy/Resource/chailangren.tres` => `Unit.Enemy`
- `Data/Data/Unit/Player/Resource/deluyi.tres` => `Unit.Player`
- `Data/Data/Ability/Resource/Movement/DashConfig.tres` => `Ability.Movement`
- `assets/Effect/Explosion/Explosion.tscn` => `Effect.Explosion`
- `assets/Unit/Enemy/chailangren/AnimatedSprite2D/chailangren.tscn` => `AssetUnit.Enemy`
- `assets/Unit/Player/deluyi/AnimatedSprite2D/deluyi.tscn` => `AssetUnit.Player`

路径中的 `Resource` 目录只是资源存放目录，不参与分类名。

示例：

```csharp
var enemyEntries = ResourceCatalog.GetEntries("Unit.Enemy");
var allGroups = ResourceCatalog.GetGroups(
    "Unit", // 全部单位
    "Ability", // 全部技能配置
    "Effect", // 全部特效资源
    "AssetUnit" // 全部单位视觉 Asset
);
```

目录服务只负责发现与展示分组；真正加载仍通过 `ResourceManagement.Load<T>(entry.ResourceKey, entry.Category)` 完成。独立视觉预览场景位于 `Src/ECS/Test/GlobalTest/VisualPreview/`，它直接基于 `ResourcePaths.Resources` 收集全部 `Asset*` 分类并批量生成可选中的预览 Entity。

## 🛠️ 最佳实践

### 文件命名与分类规范
为避免 `.tscn` (Asset) 与 `.tres` (Config) 冲突，项目采用以下约定：

- **Entity**: 游戏实体预制体 (如 `PlayerEntity.tscn`)
- **Component**: 组件预制体 (如 `HealthComponent.tscn`)
- **UI**: 界面预制体 (如 `HealthBarUI.tscn`)
- **Asset**: 纯视觉资源场景 (如 `豺狼人.tscn` - 仅包含动画/解构视图)
- **Config**: 数据配置文件 (如 `豺狼人.tres`)，通常存储在 `Data/Data/Resources` 下。

## 资源生成
本项目使用 `Tools/ResourceGenerator` 自动扫描目录并生成 `ResourcePaths.cs`。

> [!IMPORTANT]
> **什么时候需要运行生成器？**
> 当你 **添加、重命名、移动或删除** 了任何 `.tscn` 或 `.tres` 文件时，必须运行生成器更新索引，否则 `ResourceManagement.Load` 将无法找到资源。
> 
> ```bash
> dotnet run --project Tools/ResourceGenerator/ResourceGenerator.csproj
> ```
