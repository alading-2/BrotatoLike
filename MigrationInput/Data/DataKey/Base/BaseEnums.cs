using System;

/// <summary>
/// 阵营枚举
/// </summary>
public enum Team
{
    /// <summary>中立</summary>
    Neutral = 0,

    /// <summary>玩家</summary>
    Player = 1,

    /// <summary>敌人</summary>
    Enemy = 2,
}


/// <summary>
/// 目标阵营过滤 - [Flags] 位运算
/// </summary>
[Flags]
public enum TeamFilter
{
    None = 0,

    /// <summary>友方</summary>
    Friendly = 1 << 0,

    /// <summary>敌方</summary>
    Enemy = 1 << 1,

    /// <summary>中立</summary>
    Neutral = 1 << 2,

    /// <summary>自身</summary>
    Self = 1 << 3,

    // ============ 组合预设 ============
    // 友方和自身
    FriendlyAndSelf = Friendly | Self,

    // 所有
    All = Friendly | Enemy | Neutral | Self,
}

/// <summary>
/// 实体物理/技术类型 - [Flags] 位运算
/// </summary>
[Flags]
public enum EntityType
{
    /// <summary>空</summary>
    None = 0,

    /// <summary>单位 (生物)</summary>
    Unit = 1 << 0,

    /// <summary>投射物 (子弹)</summary>
    Projectile = 1 << 1,

    /// <summary>建筑</summary>
    Structure = 1 << 2,

    /// <summary>物品 (掉落物)</summary>
    Item = 1 << 3,

    /// <summary>技能实体</summary>
    Ability = 1 << 4,

    /// <summary>Buff实体</summary>
    Buff = 1 << 5,

    /// <summary>陷阱</summary>
    Trap = 1 << 6,

    /// <summary>瞄准指示器（技能施法点标记）</summary>
    TargetingIndicator = 1 << 7,

    /// <summary>特效实体</summary>
    Effect = 1 << 8,


    // ============ 组合预设 ============
}

/// <summary>
/// 单位品阶/等级
/// </summary>
public enum UnitRank
{
    Normal, // 普通
    Elite, // 精英
    Boss, // BOSS
    Summon // 召唤物
}