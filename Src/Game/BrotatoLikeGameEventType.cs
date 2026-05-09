using Godot;

namespace BrotatoLike.Game;

/// <summary>
/// BrotatoLike 游戏侧全局事件名和 payload。
/// </summary>
public static class BrotatoLikeGameEventType
{
    /// <summary>
    /// 游戏主流程事件。
    /// </summary>
    public static class Game
    {
        /// <summary>正式游戏入口已初始化并进入 Gameplay。</summary>
        public const string Started = "game:started";

        /// <summary>
        /// 正式游戏启动 payload。
        /// </summary>
        public readonly record struct StartedEventData(
            BrotatoLikeGameRuntime Runtime,
            Node EntryNode,
            int Wave);
    }
}
