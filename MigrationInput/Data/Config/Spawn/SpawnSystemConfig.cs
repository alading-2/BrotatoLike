using Godot;
using System.Collections.Generic;

/// <summary>
/// 全局生成配置 - 静态配置方式（相当于常量调用）。
/// <para>这里只演示“纯常量配置”这一层，适合默认波次时长、上限、间隔这类稳定参数。</para>
/// <para>如果你要看复杂生成链路，应从 <c>Docs/框架/ECS/System/Core/系统与状态分层总览.md</c> 和 <c>Src/ECS/Base/System/Core/README.md</c> 开始，再回到 <c>SpawnSystem</c> 本体。</para>
/// </summary>
public static class SpawnSystemConfig
{
    /// <summary> 每一波的默认持续时间（秒） </summary>
    public const float WaveDuration = 60.0f;

    /// <summary> 最大波次数量 </summary>
    public const int MaxWaves = 20;

    /// <summary> 波次间隔时间（休息时间） </summary>
    public const float WaveBreakTime = 5.0f;
}
