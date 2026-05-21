# BrotatoLike Skill Validation

## 测试目标

验证 BrotatoLike 已迁入的 projectile / passive 技能能从 DataOS 确定性 loadout 触发，并产出逐 ability id 的释放、视觉、movement mode、命中/伤害和 cleanup 证据。

## 允许依赖

- `BrotatoLikeGameRuntime` 从 DataOS snapshot 初始化。
- `BrotatoLikeAbilityHandlers` 生产 handler 注册。
- `BrotatoLikeSkillLoadoutAuthoring.ValidationAllSkillAbilityIds` 确定性验证 loadout。
- GameOS `Ability / Projectile / Movement / Damage / Effect` capability 和 `GodotProjectileEffectSpawner`。

## 不覆盖内容

- 不验证技能获得、升级分支、商店购买或数值平衡。
- 不验证默认四槽 UI 交互；该内容由 Playable UX 场景覆盖。
- 不要求所有技能进入默认可见技能栏。

## 运行命令

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-build.sh
Tools/run-godot-scene.sh run res://Src/Validation/Game/Skills/BrotatoLikeSkillValidation.tscn --timeout 20 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh --run-dir .ai-temp/scene-tests/runs/<date>/<time>
```

## expectedInputs

- `BrotatoLikeGameRuntime` initialized from the BrotatoLike DataOS snapshot.
- `SpawnPlayerWithValidationLoadout` using `ValidationAllSkillAbilityIds`.
- Deterministic enemy targets with explicit team, HP, collision radius, and position.
- Manual `AbilityService.TryTrigger` calls for `sine_wave_shot`, `boomerang_throw`, `bezier_shot`, `parabola_shot`, `arc_shot`, `orbit_skill`, `circle_damage`, and `aura_shield`.

## expectedObservations

- Projectile skills spawn runtime projectile entities with authored `Projectile.ScenePath` and `Movement.HandlerMoveMode`.
- Projectile skills move, collide, reduce enemy HP, and clean up runtime/visual projectile entities.
- `orbit_skill` uses Orbit movement, `aura_shield` uses AttachToHost movement, and both provide sustained hit/follow/cleanup evidence.
- `circle_damage` damages in-radius enemies only, spawns the authored aura effect, and exposes effect cleanup evidence.
- Artifact checks are grouped by ability id and include deterministic loadout source plus owned ability ids.

## passCriteria

- Stdout contains `BrotatoLike Skill validation PASS`.
- `artifacts/brotatolike-skill-validation.json` has `status=pass`.
- Every ability-id-named check passes.
- `expectedInputs`, `expectedObservations`, `passCriteria`, `failCriteria`, and `artifactPath` are non-empty.

## failCriteria

- Stdout contains `BrotatoLike Skill validation FAIL`.
- Any skill trigger, projectile/effect spawn, movement mode, hit/damage, visual, or cleanup evidence is missing.
- Artifact checks are not grouped by ability id.
- The artifact is missing or standard-answer fields are empty.

## artifactPath

`artifacts/brotatolike-skill-validation.json`

## PASS/FAIL 判定

- PASS：所有 projectile/passive 技能检查和 `ability_id_grouping` 检查通过，runner `result.json` 标记场景成功。
- FAIL：任一 ability id 的触发、视觉、movement、命中、半径语义或 cleanup 证据缺失。

## artifact 字段

- `checks[].name` 使用 ability id 或 `ability_id_grouping`。
- `checks[].details.abilityId` 记录当前能力 id。
- `checks[].details.loadoutSource` 记录 `validation-override`。
- `checks[].details.spawnedProjectileIds / effectEntityIds` 记录运行时实体。
- `checks[].details.projectileScenePaths / effectScenePaths` 记录视觉资源路径。
- `checks[].details.movementModes` 记录 movement mode。
- `checks[].details.targetHpBefore / targetHpAfter` 或半径内外 HP 记录玩法效果。
- `checks[].details.cleanupRuntimeDestroyed` 记录 cleanup 结果。

## 常见失败排查顺序

1. 先检查 `BrotatoLikeAbilityHandlers.RegisterAll` 是否接入 runtime 共享 `MovementSystem`。
2. 检查 DataOS `ability`, `projectile`, `ability_movement_*`, `effect` 行是否仍包含目标 ability id。
3. 检查 `GodotProjectileEffectSpawner` 是否在 runtime 初始化时订阅 Projectile/Effect 事件。
4. 检查 `MovementCollisionParams` 是否带有正确 team/layer 过滤和碰撞半径。
5. 检查 artifact 中失败 ability id 的 `movementModes`, `collisionCount`, `targetHpBefore/After`, `destroyedEntityIds`。
