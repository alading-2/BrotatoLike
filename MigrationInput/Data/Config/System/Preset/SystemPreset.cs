using Godot;

/// <summary>
/// 系统预设资源。
/// <para>用于快速切换不同游戏模式的系统配置（如 Roguelike、Survival、Tower 等）。</para>
/// <para>只允许一个 Preset 的 IsActive = true，启动时检查，多个 Active 则报错。</para>
/// </summary>
[GlobalClass]
public partial class SystemPreset : Resource
{
    /// <summary>预设名称。</summary>
    [ExportGroup("基础信息")]
    [Export]
    public string PresetName { get; set; } = "DefaultPreset";

    /// <summary>是否激活此预设（只允许一个 Preset 为 true）。</summary>
    [Export]
    public bool IsActive { get; set; } = false;

    /// <summary>启用的系统标签（Flags 组合）。</summary>
    [ExportGroup("批量开启规则")]
    [Export]
    public SystemTag EnabledTags { get; set; } = SystemTag.None;

    /// <summary>显式启用的系统 Id 列表。</summary>
    [Export]
    public string[] EnabledSystemIds { get; set; } = System.Array.Empty<string>();

    /// <summary>显式禁用的系统 Id 列表（优先级最高）。</summary>
    [ExportGroup("排除规则")]
    [Export]
    public string[] DisabledSystemIds { get; set; } = System.Array.Empty<string>();

    /// <summary>预设描述（用于文档和调试）。</summary>
    [ExportGroup("说明")]
    [Export(PropertyHint.MultilineText)]
    public string Description { get; set; } = string.Empty;
}
