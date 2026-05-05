using System;

/// <summary>
/// 系统标签（逻辑分类）。
/// <para>多选 Flags 枚举，用于系统预设装载、运行时筛选和调试展示。</para>
/// </summary>
[Flags]
public enum SystemTag : ulong
{
    None = 0,
    Core = 1UL << 0,
    Gameplay = 1UL << 1,
    Combat = 1UL << 2,
    UI = 1UL << 3,
    Debug = 1UL << 4,
    Test = 1UL << 5,
    Roguelike = 1UL << 6,
    Runtime = 1UL << 7,
}
