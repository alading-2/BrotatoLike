# Runtime Event Validation

## 测试目标

验证 GameOS `Runtime/Event` 基础层在 Godot headless 场景中的可运行行为，并产出稳定分级日志、PASS/FAIL marker 与 JSON artifact。

核心 EventBus 断言覆盖：

- typed handler 和 parameterless handler 触发。
- priority 顺序。
- `Once` 只执行一次。
- `Off` 移除 handler。
- handler exception 被 `HandlerException` 捕获，后续 handler 继续执行。
- 同事件重入被阻止，并记录尝试次数和实际执行次数。
- `EventContext.StopPropagation()` 阻断低优先级 handler。
- entity-local `EventBus` 与 `GlobalEventBus.Global` 不混淆。

跨层桥接检查：

- `RuntimeEntity + Data.Set` 触发 `GameEventType.Data.PropertyChanged`。
- 该项依赖 Runtime Data 和 Entity，仅用于证明 Data-to-Event bridge 在 Godot 场景中可运行，不算纯 EventBus 核心断言。

## 允许依赖

- Godot scene runner: `Tools/run-godot-scene.sh`
- GameOS Runtime Event: `EventBus`、`EventContext`、`EventPriority`、`GlobalEventBus`、`GameEventType`
- GameOS Runtime Entity/Data bridge: `RuntimeEntity`、`Data.Set`、`EventDataChangeSink`
- BrotatoLike 仅作为 `res://` 场景承载工程，不引入玩法代码作为通过条件。

## 不覆盖内容

- 不验证 Data 层完整行为。
- 不验证 Entity 生命周期管理器。
- 不验证 Capability、GodotBridge 组件、BrotatoLike 玩法或 UI。
- 不改变 `EventBus` 或 `GlobalEventBus` API。

## 运行命令

框架侧：

```bash
cd /home/slime/Code/SkilmeAI/SkilmeAI
Tools/run-build.sh
Tools/run-tests.sh
```

游戏侧：

```bash
cd /home/slime/Code/SkilmeAI/Games/BrotatoLike
Tools/run-build.sh
Tools/run-godot-scene.sh run res://Scenes/Validation/Runtime/Event/RuntimeEventValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

## PASS/FAIL 判定

PASS 必须同时满足：

- Godot 进程 exit code 为 `0`。
- stdout 至少包含每个检查项的 `[INFO]` / `[PASS]` 分级日志。
- stdout 包含 `GameOS Runtime Event validation PASS`。
- artifact `runtime-event-validation.json` 的 `status` 为 `pass`。
- artifact `logs` 包含每个检查项的关键日志条目。
- artifact `failureReasons` 为空。

FAIL 判定：

- stdout 包含 `GameOS Runtime Event validation FAIL`，或 Godot 进程 exit code 非 `0`。
- stdout 包含对应检查项的 `[FAIL]` 日志。
- artifact `runtime-event-validation.json` 的 `status` 为 `fail`。
- artifact `failureReasons` 至少包含一条失败原因。

## 日志

场景使用框架侧 `SceneValidationSession` 输出固定文本格式，便于 Godot Output、人类终端和 AI 检索：

```text
[INFO][RuntimeEventValidation] check typed_and_parameterless_handlers start
[PASS][RuntimeEventValidation] check typed_and_parameterless_handlers handlers fired
[FAIL][RuntimeEventValidation] check <name> <reason>
```

日志不是 artifact 的替代品。stdout 用于快速判断执行路径，artifact 用于保留完整结构化证据。

## Artifact

scene runner 会把 artifact 写入：

```text
.ai-temp/scene-tests/runs/<date>/<time>/<scene-attempt>/artifacts/runtime-event-validation.json
```

同一场景还会写入：

```text
.ai-temp/scene-tests/runs/<date>/<time>/<scene-attempt>/artifacts/logs/scene-log.jsonl
```

字段：

- `status`: `pass` 或 `fail`。
- `scene`: `res://Scenes/Validation/Runtime/Event/RuntimeEventValidation.tscn`。
- `layer`: `Runtime/Event`。
- `checks`: 检查项数组，每项包含 `name`、`status`、`category`、`details`。
- `logs`: 与 stdout 同源的关键日志条目，每项包含 `timestamp`、`level`、`context`、`message`、`values`。
- `failureReasons`: 失败原因数组，PASS 时为空。
- `dependencies`: 本场景允许依赖。
- `notes`: 跨层桥接说明和后续扩展说明。

## 常见失败排查顺序

1. 先看 Godot 进程 exit code 和 stdout 的 PASS/FAIL marker。
2. 打开最新 run 目录中的 `artifacts/runtime-event-validation.json`，查看 `failureReasons`。
3. 如果 `handler_exception_capture` 失败，确认 `HandlerException` 是否被订阅且异常 handler 后续订阅仍执行。
4. 如果 `same_event_reentrancy_block` 失败，确认重入 emit 的尝试次数和实际执行次数是否区分记录。
5. 如果 `local_global_bus_isolation` 失败，确认场景开头和结尾是否调用 `GlobalEventBus.Global.Clear()`。
6. 如果 `data_to_event_bridge` 失败，按跨层桥接处理，检查 `RuntimeEntity`、`Data.Set` 和 `EventDataChangeSink`，不要先修改 EventBus。
