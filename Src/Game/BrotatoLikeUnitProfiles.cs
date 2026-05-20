using SlimeAI.GameOS.GodotBridge;

namespace BrotatoLike.Game;

/// <summary>
/// BrotatoLike 游戏侧单位组合 profile；只选择框架通用 Adapter，不上提游戏输入或技能。
/// </summary>
public static class BrotatoLikeUnitProfiles
{
    /// <summary>玩家单位组合。</summary>
    public static GodotUnitCompositionProfile Player { get; } = new()
    {
        IncludeVisual = true,
        IncludeAnimation = true,
        IncludeOrientation = true,
        IncludeAI = false,
        IncludeAttack = true,
        IncludeHurtbox = true,
        IncludeContactDamageReceiver = true,
        FallbackHurtboxRadius = 26f
    };

    /// <summary>近战敌人单位组合。</summary>
    public static GodotUnitCompositionProfile EnemyMelee { get; } = new()
    {
        IncludeVisual = true,
        IncludeAnimation = true,
        IncludeOrientation = true,
        IncludeAI = true,
        IncludeAttack = true,
        IncludeHurtbox = true,
        IncludeContactDamageReceiver = false,
        FallbackHurtboxRadius = 18f
    };
}
