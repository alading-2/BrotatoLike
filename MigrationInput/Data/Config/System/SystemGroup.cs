/// <summary>
/// 系统挂载分组。
/// <para>只决定系统挂在哪个 Host 下；装载筛选请使用 SystemTag 或显式 SystemId。</para>
/// </summary>
public enum SystemGroup : byte
{
    Base = 0,
    Gameplay = 1,
    Combat = 2,
    UI = 3,
    Debug = 4,
    Test = 5,
    Else = 6,
}
