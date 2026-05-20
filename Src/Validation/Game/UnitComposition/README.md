# BrotatoLike Unit Composition Validation

## 测试目标

验证 BrotatoLike 玩家和敌人通过 `BrotatoLikeUnitProfiles` 调用框架 `GodotUnitComposer` 组合单位行为，并在真实 `_Process` 主循环中完成玩家输入移动、敌人 AI 追逐、动画播放和接触伤害桥接。

## expectedInputs

- `BrotatoLikeGameRuntime`。
- `unit.player/deluyi`、`unit.enemy/yuren`、`unit.enemy/chailangren` DataOS 数据。
- `BrotatoLikeUnitProfiles.Player` 和 `EnemyMelee`。
- Godot process frames、InputMap `MoveRight`。

## expectedObservations

- 玩家拥有框架 visual、animation、orientation、attack、hurtbox、contact damage receiver adapter，并保留游戏侧输入/技能 adapter。
- 玩家由真实 `_Process` 输入路径移动。
- 敌人由 spawn system 生成，拥有 AI/attack/hurtbox 等框架 adapter，并启动 AIControlled movement。
- 敌人靠 AI 和共享 movement driver 产生位移。
- AnimatedSprite2D 正在播放。
- 接触伤害通过 `GodotHurtboxComponent` / `GodotContactDamageComponent` 进入 `DamageService`。

## passCriteria

- stdout 包含 `BrotatoLike UnitComposition validation PASS`。
- artifact `brotatolike-unit-composition-validation.json` 的 `status` 为 `pass`。
- artifact `failureReasons` 为空，全部检查项均为 `pass`。

## failCriteria

- stdout 包含 `BrotatoLike UnitComposition validation FAIL`。
- 玩家 profile、敌人 profile、真实 process movement、AI movement、动画或接触伤害任一检查失败。
- artifact 标准答案字段缺失或 `failureReasons` 非空。

## artifactPath

```text
.ai-temp/scene-tests/runs/<date>/<time>/<scene-attempt>/artifacts/brotatolike-unit-composition-validation.json
```

## 运行命令

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-build.sh
Tools/run-godot-scene.sh run res://Src/Validation/Game/UnitComposition/BrotatoLikeUnitCompositionValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```
