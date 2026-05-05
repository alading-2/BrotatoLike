/// <summary>
/// 视觉预览测试数据键。
/// </summary>
public static partial class DataKey
{
    public static readonly DataMeta PreviewResourceKey = DataRegistry.Register(
        new DataMeta { Key = nameof(PreviewResourceKey), DisplayName = "预览资源键", Category = DataCategory_Test.VisualPreview, Type = typeof(string), DefaultValue = string.Empty });

    public static readonly DataMeta PreviewResourcePath = DataRegistry.Register(
        new DataMeta { Key = nameof(PreviewResourcePath), DisplayName = "预览资源路径", Category = DataCategory_Test.VisualPreview, Type = typeof(string), DefaultValue = string.Empty });

    public static readonly DataMeta PreviewResourceCategory = DataRegistry.Register(
        new DataMeta { Key = nameof(PreviewResourceCategory), DisplayName = "预览资源分类", Category = DataCategory_Test.VisualPreview, Type = typeof(string), DefaultValue = string.Empty });

    public static readonly DataMeta PreviewCatalogPath = DataRegistry.Register(
        new DataMeta { Key = nameof(PreviewCatalogPath), DisplayName = "预览目录分类", Category = DataCategory_Test.VisualPreview, Type = typeof(string), DefaultValue = string.Empty });

    public static readonly DataMeta PreviewDefaultAnimation = DataRegistry.Register(
        new DataMeta { Key = nameof(PreviewDefaultAnimation), DisplayName = "预览默认动作", Category = DataCategory_Test.VisualPreview, Type = typeof(string), DefaultValue = string.Empty });

    public static readonly DataMeta PreviewCurrentAnimation = DataRegistry.Register(
        new DataMeta { Key = nameof(PreviewCurrentAnimation), DisplayName = "预览当前动作", Category = DataCategory_Test.VisualPreview, Type = typeof(string), DefaultValue = string.Empty });
}
