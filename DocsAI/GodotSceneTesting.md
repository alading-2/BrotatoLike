# Godot Scene Testing

## 目标

统一 BrotatoLike 的 Godot 场景测试入口，给 AI 和 CI 使用同一套 headless 命令，并把 Observation / Debug / Trace 输出放到固定目录。

BrotatoLike 的 `SlimeAI/` 是框架仓 git submodule 镜像。当前初始开发阶段，框架仓新增或修改 `Scenes/Validation/...`、`Src/SceneTests/...` 后，BrotatoLike 作为默认承载游戏，直接同步到 `Games/BrotatoLike/SlimeAI/` 工作树以跑通验证。后续多游戏 / 成品阶段不默认同步所有游戏，改按每个游戏的框架版本策略更新 submodule 指针，再选择对应承载游戏跑验证。

`Tools/run-godot-scene.sh` 的 scan roots 必须包含 `Scenes,Src,SlimeAI/Scenes,SlimeAI/Src`，`BrotatoLike.csproj` 必须重新包含 `SlimeAI/Src/SceneTests/**/*.cs`，否则 submodule 场景脚本不会被编译。

## 命令

列出当前可运行场景：

```bash
Tools/run-godot-scene.sh list
```

运行单场景：

```bash
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --build -- --gameos-smoke-exit
```

运行框架 Runtime/Data 专项验证场景：

```bash
Tools/run-godot-scene.sh run res://SlimeAI/Scenes/Validation/Runtime/Data/RuntimeDataValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

运行普通主场景可玩切片验收：

```bash
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

运行当前主 smoke：

```bash
Tools/run-godot-scene.sh run-main-smoke
```

保留日志：

```bash
Tools/run-godot-scene.sh run-main-smoke --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

旧兼容入口仍可用：

```bash
Tools/run-godot-smoke.sh
```

## Observation / Debug / Trace

`--log-dir` 会创建：

```text
.ai-temp/scene-tests/runs/<date>/<time>/
  index.json
  001_<scene>_attempt1/
    stdout.log
    stderr.log
    combined.log
    result.json
    screenshots/
    artifacts/
      logs/scene-log.jsonl
```

运行时会注入这些环境变量：

```text
GODOT_SCENE_TEST_RUN_DIR
GODOT_SCENE_TEST_SCENE_DIR
GODOT_SCENE_TEST_SCREENSHOT_DIR
GODOT_SCENE_TEST_ARTIFACT_DIR
GODOT_SCENE_TEST_*_REL
```

后续场景测试需要截图、JSON trace、状态快照时，统一写到 `screenshots/` 或 `artifacts/`，不要写到仓库根目录。

R07 可玩切片验收会写入：

```text
.ai-temp/scene-tests/runs/<date>/<time>/<scene-attempt>/artifacts/scene-acceptance.json
```

该 artifact 记录 `status=pass/fail`、检查项、观测到的 EntityId / HP / skill / damage log 等结构化证据。通用日志 JSONL 写入 `artifacts/logs/scene-log.jsonl`。

## 失败判定

优先看：

- Godot 进程 exit code。
- `result.json` / `index.json` 中的 exit code 和 failure reason。
- artifact `status`，例如 `scene-acceptance.json` 或 validation artifact。
- 明确输出，例如 `BrotatoLike playable slice PASS` / `FAIL`、`GameOS Runtime Data validation PASS` / `FAIL`、`GameOS Runtime Event validation PASS` / `FAIL`。
- smoke 明确输出，例如 `BrotatoLike GameOS smoke PASS`。
- `Tools/analyze-godot-scene-logs.sh` 提取的 `ERROR:`、`[FAIL]`、`Exception`、`Cannot instantiate`、`Failed to load`。

普通 Godot `ERROR:` 不一定等于测试失败，但必须结合最终 PASS/FAIL 和 exit code 判断。

## 清理

AI 调试完一次临时日志后，默认清理对应 run 目录：

```bash
rm -r -- .ai-temp/scene-tests/runs/<date>/<time>
```

只有需要留给人工复查时才保留日志路径。
