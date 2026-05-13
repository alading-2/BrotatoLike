using Godot;
using SlimeAI.GameOS.GodotBridge;
using SlimeAI.GameOS.Runtime.Entity;

namespace BrotatoLike.Game;

/// <summary>
/// GodotBridge 最小 Component 探针。
/// </summary>
public partial class SmokeGodotComponent : Node, IGodotComponent
{
    /// <summary>
    /// 是否收到 Component 注册回调。
    /// </summary>
    public bool Registered { get; private set; }

    /// <inheritdoc />
    public void OnComponentRegistered(IEntity entity, Node entityNode)
    {
        Registered = true;
    }

    /// <inheritdoc />
    public void OnComponentUnregistered(IEntity? entity, Node? entityNode)
    {
        Registered = false;
    }
}
