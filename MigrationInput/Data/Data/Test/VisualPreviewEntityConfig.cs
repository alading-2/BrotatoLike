using Godot;

namespace slime.config.Test
{
    /// <summary>
    /// 视觉预览实体运行时配置。
    /// <para>由预览场景代码临时创建，不需要落成 .tres。</para>
    /// </summary>
    public partial class VisualPreviewEntityConfig : Resource
    {
        [DataKey(nameof(DataKey.Name))]
        [Export] public string Name { get; set; } = string.Empty;

        [DataKey(nameof(DataKey.Team))]
        [Export] public Team Team { get; set; } = Team.Neutral;

        [DataKey(nameof(DataKey.EntityType))]
        [Export] public EntityType EntityType { get; set; } = EntityType.Unit;

        [DataKey(nameof(DataKey.PreviewDefaultAnimation))]
        [Export] public string PreviewDefaultAnimation { get; set; } = string.Empty;

        [DataKey(nameof(DataKey.VisualScenePath))]
        [Export] public string VisualScenePath { get; set; } = string.Empty;
    }
}
