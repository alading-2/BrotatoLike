using SlimeAI.GameOS.Runtime.Data;

namespace BrotatoLike.Game.Shop;

/// <summary>
/// BrotatoLike 游戏侧商店 Runtime DataKey。
/// </summary>
public static class BrotatoLikeShopDataKeys
{
    /// <summary>玩家本局货币。</summary>
    public static readonly DataKey<int> Currency = DataKey.Create("BrotatoLike.Currency", 0);

    /// <summary>
    /// 显式触发静态 DataKey 注册。
    /// </summary>
    public static void RegisterAll()
    {
        _ = Currency;
    }
}
