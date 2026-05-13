# BrotatoLike

`BrotatoLike` 是 SlimeAI 的第一个正式 Godot 游戏仓库。

当前已建立骨架，接入 `SlimeAI.GameOS` Runtime smoke probe 和 GodotBridge 最小探针；旧 `assets/` 已复制到根目录，旧 `Data/` 和 `Src/Main/` 已放入 `MigrationInput/` 等待适配。

## 构建

```bash
Tools/run-build.sh
```

当前通过本地 `ProjectReference` 引用 `SlimeAI.GameOS`。
