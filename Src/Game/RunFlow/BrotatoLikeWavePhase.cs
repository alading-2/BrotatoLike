namespace BrotatoLike.Game.RunFlow;

/// <summary>
/// BrotatoLike 多波 run flow 的最小状态机阶段。
/// </summary>
public enum BrotatoLikeWavePhase
{
    /// <summary>
    /// 波次准备阶段。
    /// </summary>
    Preparing,

    /// <summary>
    /// 波次运行阶段。
    /// </summary>
    Running,

    /// <summary>
    /// 波次已完成。
    /// </summary>
    Completed,

    /// <summary>
    /// 波间奖励或商店阶段。
    /// </summary>
    RewardShop,

    /// <summary>
    /// 单局胜利终态。
    /// </summary>
    RunWon,

    /// <summary>
    /// 单局失败终态。
    /// </summary>
    RunLost,

    /// <summary>
    /// 正在重开并清理旧局状态。
    /// </summary>
    Restarting,

    /// <summary>
    /// 正在切换到下一波。
    /// </summary>
    NextWave,

    /// <summary>
    /// 没有后续波次，run 结束。
    /// </summary>
    Ended
}
