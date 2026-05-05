# MigrationInput

这里保存从旧 `brotato-my` 直接复制过来的迁移输入材料。

当前内容：

- `Data/`：旧数据配置、DataNew、ResourceManagement 生成结果和历史 `.tres`。
- `Src/Main/`：旧主场景入口脚本和场景。

这些文件暂时不参与编译，`BrotatoLike.csproj` 已排除 `MigrationInput/**/*.cs`。后续迁移时按模块复制到正式目录并适配 `SkilmeAI.GameOS` 契约。

资产已按旧路径复制到仓库根目录的 `assets/`，保留 `res://assets/...` 路径兼容性。
