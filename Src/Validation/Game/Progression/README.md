# BrotatoLike Progression Loop Validation

## expectedInputs

- `BrotatoLikeGameRuntime` initialized from the BrotatoLike DataOS snapshot.
- A player, at least one DataOS-spawned enemy, and runtime schedule pause/resume calls.
- Production progression code for wave state, pause menu, recovery, pickups, experience, scene-backed experience UI, and level-up choices.

## expectedObservations

- Wave runtime state records wave index, elapsed time, spawned count, remaining enemies, and completion.
- Pause input or runtime pause opens formal UI and blocks schedule-gated gameplay until resume.
- HP recovery, enemy drop, pickup collection, experience gain, level-up feedback, offered choices, selected choice, and before/after reward state are observable.
- **Scene-backed evidence**: formal pause menu, experience bar, level-up feedback, and level-up choice panel nodes MUST have non-empty `SceneFilePath`.
- Level-up choice gate uses `ModalUi+Suspended`, blocking schedule-gated gameplay while the panel is pending and resuming after selection.

## passCriteria

- Stdout contains `BrotatoLike Progression Loop validation PASS`.
- `artifacts/brotatolike-progression-loop-validation.json` has `status=pass`.
- `expectedInputs`, `expectedObservations`, `passCriteria`, `failCriteria`, and `artifactPath` are non-empty.

## failCriteria

- Stdout contains `BrotatoLike Progression Loop validation FAIL`.
- Wave completion, pause UI, recovery, pickup, experience, level-up choice, selected reward mutation, or formal experience UI evidence is missing.
- Formal pause menu, experience bar, level-up feedback, or level-up choice panel has empty `SceneFilePath` (code-created, not scene-backed).
- The artifact is missing or standard-answer fields are empty.

## artifactPath

`artifacts/brotatolike-progression-loop-validation.json`
