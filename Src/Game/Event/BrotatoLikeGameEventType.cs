using Godot;
using SlimeAI.GameOS.Runtime.Event;

namespace BrotatoLike.Game;

/// <summary>正式游戏入口已初始化并进入 Gameplay。</summary>
public readonly record struct GameStarted(
    BrotatoLikeGameRuntime Runtime,
    Node EntryNode,
    int Wave) : IGlobalEvent;
