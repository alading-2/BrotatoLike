using Godot;

namespace BrotatoLike.Game.VFX;

/// <summary>
/// 链电连线 VFX，纯 Line2D 节点，由游戏侧 VFX binder 设置起点、终点和持续时间。
/// </summary>
public partial class LightningLineEffect : Line2D
{
    /// <summary>
    /// 最近一次绑定的世界起点。
    /// </summary>
    public Vector2 LastWorldStart { get; private set; }

    /// <summary>
    /// 最近一次绑定的世界终点。
    /// </summary>
    public Vector2 LastWorldEnd { get; private set; }

    /// <summary>
    /// 最近一次播放持续时间。
    /// </summary>
    public float LastDurationSeconds { get; private set; }

    /// <summary>
    /// 设置线段从世界起点到世界终点。
    /// </summary>
    public void SetLine(Vector2 from, Vector2 to)
    {
        LastWorldStart = from;
        LastWorldEnd = to;
        GlobalPosition = from;
        Points = [Vector2.Zero, ToLocal(to)];
    }

    /// <summary>
    /// 播放连线效果。生命周期由 Runtime effect entity 销毁统一驱动。
    /// </summary>
    public void Play(float durationSeconds)
    {
        LastDurationSeconds = durationSeconds;
    }
}
