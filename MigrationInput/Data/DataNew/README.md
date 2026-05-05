# DataNew 纯 C# 数据表使用说明

`Data/DataNew/` 是当前唯一运行时数据配置方式：一张表对应一个 C# 数据类，一个静态实例对应一行数据。
旧的 `Data/Data + Data/Config + .tres` 仅保留归档，运行时不再导入。

> 迁移方向：DataNew 是当前阶段的运行时入口，但最终目标不是继续扩大手写 C# 表，而是迁移到 `SQLite Authoring DB -> validate -> generate snapshot -> runtime load`。详见 `DocsAI/Protocols/AI原生数据层协议.md`。

## 1. 数据表怎么写

以敌人表为例：`EnemyData` 就是敌人表，`Yuren` / `Chailangren` 是表里的两行数据。

```csharp
public class EnemyData : UnitData
{
    /// <summary>全部敌人数据。</summary>
    public static IReadOnlyList<EnemyData> All => DataTable.GetAll<EnemyData>();

    /// <summary>按 Name 获取敌人数据，找不到返回 null 并写 Log.Error。</summary>
    public static EnemyData? Get(string name) => DataTable.GetByName<EnemyData>(name);

    /// <summary>鱼人</summary>
    public static readonly EnemyData Yuren = new()
    {
        Name = "鱼人",
        Team = Team.Enemy,
        VisualScenePath = "res://assets/Unit/Enemy/yuren/AnimatedSprite2D/yuren.tscn",
        BaseHp = 150f,
        BaseAttack = 6f,
        MoveSpeed = 150f,
    };
}
```

关键规则：

- `Name` 是默认查询字段，同一张表内必须唯一。
- 数据行使用 `public static readonly XxxData`。
- 默认值优先读取 `DataKey.Xxx.DefaultValue`。
- 不需要继承 `DataTable`；`DataTable` 是静态工具类。

## 2. 怎么通过名字获取数据

推荐业务侧调用每张表自己的 `Get` 包装：

```csharp
var enemy = EnemyData.Get("鱼人") ?? EnemyData.Yuren;
```

也可以直接使用通用工具：

```csharp
var enemy = DataTable.GetByName<EnemyData>("鱼人") ?? EnemyData.Yuren;
```

获取整张表：

```csharp
foreach (var enemy in EnemyData.All)
{
    GD.Print(enemy.Name);
}
```

`GetByName` 找不到数据时不会抛异常，会调用 `Log.Error`，并返回 `null`。调用方用 `??` 明确写兜底逻辑。

## 3. 怎么进入运行时 Entity.Data

`EntityManager.Spawn` 的 `Config` 现在支持 DataNew POCO：

```csharp
var enemyConfig = EnemyData.Get("鱼人") ?? EnemyData.Yuren;
var enemy = EntityManager.Spawn<EnemyEntity>(new EntitySpawnConfig
{
    Config = enemyConfig, // DataNew POCO，会通过 Data.LoadFromConfig 注入 Entity.Data
    UsingObjectPool = true,
    PoolName = ObjectPoolNames.EnemyPool,
    Position = spawnPosition
});
```

内部流程：

1. `EntityManager.Spawn` 接收 `EntitySpawnConfig.Config`。
2. 调用 `entity.Data.LoadFromConfig(config)`。
3. `LoadFromConfig` 反射读取配置对象公开属性。
4. 属性名或 `[DataKey(nameof(DataKey.Xxx))]` 映射到运行时 `DataKey`。
5. 写入 `Entity.Data`，组件通过 `DataKey` 读取。

场景引用只写入 `res://` 字符串路径，`Data` 不保存 `PackedScene`。例如 `DataKey.VisualScenePath` / `DataKey.EffectScene` / `DataKey.ProjectileScene` 都保持字符串，具体系统在注入视觉、生成特效或生成投射物时再加载。

## 4. 什么时候需要 `[DataKey]`

如果属性名和目标 `DataKey` 不一致，需要显式标注。

```csharp
[DataKey(nameof(DataKey.AbilityFeatureGroup))]
public string? FeatureGroupId { get; set; }

[DataKey(nameof(DataKey.AbilityDamageBonus))]
public float BaseSkillDamage { get; set; }
```

如果属性名本身就是目标 DataKey 名称，例如 `BaseHp`、`MoveSpeed`、`SpawnInterval`，可以不写 `[DataKey]`。

## 5. 技能数据怎么写

DataNew 技能必须把运行时会读取的枚举字段写完整，不能依赖裸类型默认值。

```csharp
public static readonly AbilityData Dash = new()
{
    Name = "冲刺",
    FeatureGroupId = "技能.位移",
    FeatureHandlerId = FeatureId.Ability.Movement.Dash,
    AbilityType = AbilityType.Active, // 手动技能必须是 Active，技能栏只显示非 Passive + Manual
    AbilityTriggerMode = AbilityTriggerMode.Manual,
    AbilityTargetSelection = AbilityTargetSelection.None,
};
```

关键规则：

- `FeatureGroupId` 只控制 UI / 测试面板分组。
- `FeatureHandlerId` 是运行时执行器主键，必须能在 `FeatureHandlerRegistry` 找到。
- 手动技能要显式写 `AbilityType = AbilityType.Active` 和 `AbilityTriggerMode = AbilityTriggerMode.Manual`。
- 被动常驻技能通常写 `AbilityType = AbilityType.Passive` 和 `AbilityTriggerMode = AbilityTriggerMode.Permanent`。
- `EffectScenePath` / `ProjectileScenePath` 必须写 `res://` 字符串路径，不能写 `PackedScene`；技能执行器继续传路径，`EffectTool` / `ProjectileTool` 在最终视觉注入点加载。

## 6. 运行时数据源

运行时数据导入统一使用 `Data/DataNew` 纯 C# 表。

- 业务侧按 `Name` 调用 `XxxData.Get(name)` 获取配置。
- Entity 生成时把 DataNew 对象传入 `EntitySpawnConfig.Config`。
- `Data.LoadFromConfig(object config)` 会把配置字段注入 `Entity.Data`。
- 旧 `Data/Data + .tres` 目录保留归档，但不再作为运行时数据导入来源。
