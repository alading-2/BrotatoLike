using Godot;

/// <summary>
/// 系统配置资源。
/// <para>数据驱动架构：系统元数据从代码迁移到表格资源，支持可视化编辑。</para>
/// <para>代码注册只提供 SystemId + Factory，其他配置在此表格中定义。</para>
/// </summary>
[GlobalClass]
public partial class SystemConfig : Resource
{
    /// <summary>系统唯一 Id（必须与注册字符串和资源文件名一致）。</summary>
    [ExportGroup("基础信息")]
    [Export]
    public string SystemId { get; set; } = string.Empty;

    /// <summary>系统挂载分组（Host 位置，单选）。</summary>
    [Export]
    public SystemGroup MountGroup { get; set; } = SystemGroup.Else;

    /// <summary>系统标签（逻辑分类和预设筛选），多选 Flags。</summary>
    [Export]
    public SystemTag Tags { get; set; } = SystemTag.None;

    /// <summary>是否为必需系统（无论预设如何都装载）。</summary>
    [ExportGroup("加载配置")]
    [Export]
    public bool Required { get; set; } = false;

    /// <summary>默认是否自动装载（没有激活预设时使用）。</summary>
    [Export]
    public bool AutoLoad { get; set; } = true;

    /// <summary>首次纳入管理时的人工开关默认值。</summary>
    [Export]
    public bool StartEnabled { get; set; } = true;

    /// <summary>加载优先级（数值越小越优先，用于依赖排序）。</summary>
    [Export]
    public int Priority { get; set; } = 0;

    /// <summary>允许的流程状态（Flags 组合，为 None 表示不限制；可直接选择 GameFlowState 预设组合）。</summary>
    [ExportGroup("运行条件")]
    [Export]
    public GameFlowState AllowedFlowStates { get; set; } = GameFlowState.None;

    /// <summary>要求存在的覆盖层（Flags 组合，为 None 表示不要求覆盖层；可直接选择 OverlayFlags 预设组合）。</summary>
    [Export]
    public OverlayFlags RequiredOverlays { get; set; } = OverlayFlags.None;

    /// <summary>禁止的覆盖层（Flags 组合，为 None 表示不屏蔽覆盖层；可直接选择 OverlayFlags 预设组合）。</summary>
    [Export]
    public OverlayFlags BlockedOverlays { get; set; } = OverlayFlags.None;

    /// <summary>允许的模拟状态（Flags 组合，为 None 表示不限制；可直接选择 SimulationState 预设组合）。</summary>
    [Export]
    public SimulationState AllowedSimulationStates { get; set; } = SimulationState.None;

    /// <summary>依赖系统 Id 列表。</summary>
    [ExportGroup("依赖关系")]
    [Export]
    public string[] Dependencies { get; set; } = System.Array.Empty<string>();

    /// <summary>系统描述（用于文档和调试）。</summary>
    [ExportGroup("说明")]
    [Export(PropertyHint.MultilineText)]
    public string Description { get; set; } = string.Empty;
}
