using Godot;

namespace BrotatoLike.Game.VFX;

/// <summary>
/// 链电连线 VFX，纯 Line2D 节点，由 AbilityHandler 设置起点/终点和持续时间。
/// </summary>
public partial class LightningLineEffect : Line2D
{
    /// <summary>
    /// 设置线段从起点到终点。
    /// </summary>
    public void SetLine(Vector2 from, Vector2 to)
    {
        Points = [from, to];
    }

    /// <summary>
    /// 播放连线效果并在 duration 秒后自动释放。
    /// </summary>
    public void Play(float durationSeconds)
    {
        var timer = GetTree().CreateTimer(durationSeconds);
        timer.Timeout += QueueFree;
    }
}
