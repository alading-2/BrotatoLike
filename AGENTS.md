# BrotatoLike 游戏仓库规则

## 定位

本仓库是第一个 `SlimeAI` 游戏仓库。游戏仓通过 `git submodule` 持有框架仓 `SlimeAI` 的只读镜像（`SlimeAI/` 目录）。框架源码物理嵌入游戏 `res://` 空间，编译时由游戏 csproj 统一编译，脚本和场景加载链路完全成立。

- **单向数据流**：框架改动只在 `SlimeAI` 框架仓提交；游戏仓通过 `git submodule update` 拉取新版本。
- **游戏仓禁止对 `SlimeAI/` 目录做业务改动**，只允许 submodule 指针前进。

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
