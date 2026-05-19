# BrotatoLike Game/Input Validation

## 测试目标

验证 P3 后玩家输入事件留在 BrotatoLike 游戏侧，而不是框架 Runtime：移动输入写入 `MovementDataKeys.InputDirection`，技能输入事件使用 `BrotatoLike.Game.Events`，并能驱动 `GodotActiveSkillInputComponent` 切换和触发主动技能。

## 允许依赖

- `BrotatoLike.Game.Bridge.BrotatoLikePlayerInputComponent`
- `BrotatoLike.Game.GodotActiveSkillInputComponent`
- `BrotatoLike.Game.Events`
- `SlimeAI.GameOS.Capabilities.Movement`
- `SlimeAI.GameOS.Capabilities.Ability`
- `SlimeAI.GameOS.Runtime.Entity`

## 不覆盖内容

- 真实手柄物理设备。
- 鼠标 / 手柄点选目标。
- 完整可玩切片；该项由 `Scenes/Main.tscn` acceptance 和 `run-main-smoke` 覆盖。

## 运行命令

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run res://Src/Validation/Game/Input/BrotatoLikeInputEventValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
```

## PASS/FAIL 判定

- PASS marker: `BrotatoLike Game Input validation PASS`
- FAIL marker: `BrotatoLike Game Input validation FAIL`

## Artifact

`artifacts/brotatolike-input-event-validation.json`

## 常见失败排查顺序

1. 打开最新 `index.json`。
2. 打开 per-scene `combined.log`。
3. 打开 `artifacts/brotatolike-input-event-validation.json` 的 `failureReasons` 和 `checks`。
