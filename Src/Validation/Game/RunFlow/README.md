# BrotatoLike Run Flow Validation

## 测试目标

验证 BrotatoLike 第一版多波 run flow：DataOS wave authoring、第一波开始、第一波完成、波间奖励/shop hook、第二波开始、pause gate、死亡/复活和 wave cleanup 证据。

## expectedInputs

- `BrotatoLikeGameRuntime` initialized from the BrotatoLike DataOS snapshot.
- DataOS generated `wave_definition` and `wave_enemy_entry` authoring.
- Wave 1 and Wave 2 have deterministic enemy entries, finite spawn counts, completion rules and next-wave behavior.
- Production progression, shop, HUD and spawn systems are used.

## expectedObservations

- Wave authoring includes wave id, enemy entries, spawn timing, completion mode, next-wave behavior and validates enemy/resource references.
- Wave 1 starts in `Running`, spawns deterministic enemies, then enters `Completed` and `RewardShop`.
- Reward/shop hook metadata records the available shop offer set without requiring full economy balancing.
- Wave 2 starts through the runtime state machine and spawns authored second-wave enemies.
- Pause blocks schedule-gated spawn ticks; death/respawn keeps player lifecycle valid.
- Cleanup metadata records enemy, pickup, projectile/effect and runtime entity counts.
- Scene-backed `ExperienceBarUI` records wave index and wave phase.

## passCriteria

- Stdout contains `BrotatoLike Run Flow validation PASS`.
- `artifacts/brotatolike-run-flow-validation.json` has `status=pass`.
- Checks `wave_authoring_loaded_and_validates_refs / first_wave_starts_and_spawns / pause_gate_and_respawn_preserved / first_wave_completion_reward_phase / second_wave_starts / wave_cleanup_counts / wave_ui_phase_scene_backed` all pass.
- `expectedInputs`, `expectedObservations`, `passCriteria`, `failCriteria`, and `artifactPath` are non-empty.

## failCriteria

- Stdout contains `BrotatoLike Run Flow validation FAIL`.
- Wave authoring, first/second wave transition, pause, death/respawn, cleanup, or scene-backed wave UI evidence is missing.
- The artifact is missing or standard-answer fields are empty.

## artifactPath

`artifacts/brotatolike-run-flow-validation.json`

## PASS/FAIL 判定

- PASS：所有 run-flow checks 通过，runner `result.json` 标记场景成功，scene gate 能读取完整 artifact oracle。
- FAIL：任一 wave authoring、状态机、生命周期 gate、cleanup count 或 UI phase 证据缺失。
