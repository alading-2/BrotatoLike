# BrotatoLike Run Lifecycle 20 Waves Validation

## 测试目标

验证 BrotatoLike 完整单局生命周期：20 波 authoring、显式 phase transition、最终胜利、死亡失败、restart 清理和最小 summary payload。

## expectedInputs

- `BrotatoLikeGameRuntime` initialized from the BrotatoLike DataOS snapshot.
- Game-side run lifecycle authoring with a deterministic 20-wave run definition.
- DataOS generated `run_definition` / `run_enemy_entry` authoring for waves 1 through 20.
- Validation fast-forward commands for wave completion, death loss and restart.

## expectedObservations

- Runtime evidence exposes configured wave count, current wave index and end-condition metadata.
- Fast-forwarded wave completion records an explicit phase sequence through wave running, reward/shop intermission and `RunWon`.
- Player death in death-ends-run mode records `RunLost` with a stable loss reason.
- Restart clears old enemies, pickups, transient UI, scheduled wave state and stale runtime entities before a new run starts.
- Terminal states create a minimal summary payload for the later run summary UI change.

## passCriteria

- Stdout contains `BrotatoLike Run Lifecycle 20 Waves validation PASS`.
- `artifacts/brotatolike-run-lifecycle-20-waves-validation.json` has `status=pass`.
- Checks `twenty_wave_run_authoring_loaded / phase_sequence_reaches_run_won / death_enters_run_lost / restart_clears_run_state / summary_payload_created` all pass.
- `expectedInputs`, `expectedObservations`, `passCriteria`, `failCriteria`, and `artifactPath` are non-empty.

## failCriteria

- Stdout contains `BrotatoLike Run Lifecycle 20 Waves validation FAIL`.
- Wave authoring has fewer than 20 waves, terminal phase is not `RunWon` / `RunLost`, restart leaves stale entities/UI/state, or summary payload metadata is missing.
- The artifact is missing or standard-answer fields are empty.

## artifactPath

`artifacts/brotatolike-run-lifecycle-20-waves-validation.json`

## PASS/FAIL 判定

- PASS：所有 lifecycle checks 通过，runner `result.json` 标记场景成功，scene gate 能读取完整 artifact oracle。
- FAIL：任一 authoring、phase sequence、terminal reason、restart cleanup 或 summary payload 证据缺失。
