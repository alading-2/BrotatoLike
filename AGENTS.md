# BrotatoLike 游戏仓库规则

## 定位

本仓库是第一个 `SkilmeAI` 游戏仓库。游戏运行时通过本地项目引用、本地 NuGet 或 DLL 使用 `SkilmeAI.GameOS`，不复制框架源码。

## 必读入口

- `DocsAI/INDEX.md`
- `DocsAI/GameProjectState.md`
- `DocsAI/ExternalFrameworkMap.md`

## 修改规则

- 默认中文回复。
- 游戏仓库只放游戏资产、场景、游戏特定代码和游戏数据；AI 入口统一从工作区根和 `DocsAI/` 路由。
- 默认不直接修改框架源码；确认框架 bug 时切到框架仓库修复。
- 新增 C# XML 注释默认中文；公开 API 注释保持简短，详细说明写入 `DocsAI/`。
- 默认不 commit、不 push。

## 验证入口

最小验证：

```bash
Tools/run-build.sh
godot --headless --build-solutions --path .
```
