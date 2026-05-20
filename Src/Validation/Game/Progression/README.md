# BrotatoLike Progression Loop Validation

## expectedInputs

- `BrotatoLikeGameRuntime` initialized from the BrotatoLike DataOS snapshot.
- A player, at least one DataOS-spawned enemy, and runtime schedule pause/resume calls.
- Production progression code for wave state, pause menu, recovery, pickups, experience, and level-up feedback.

## expectedObservations

- Wave runtime state records wave index, elapsed time, spawned count, remaining enemies, and completion.
- Pause input or runtime pause opens formal UI and blocks schedule-gated gameplay until resume.
- HP recovery, enemy drop, pickup collection, experience gain, and level-up feedback are observable.

## passCriteria

- Stdout contains `BrotatoLike Progression Loop validation PASS`.
- `artifacts/brotatolike-progression-loop-validation.json` has `status=pass`.
- `expectedInputs`, `expectedObservations`, `passCriteria`, `failCriteria`, and `artifactPath` are non-empty.

## failCriteria

- Stdout contains `BrotatoLike Progression Loop validation FAIL`.
- Wave completion, pause UI, recovery, pickup, experience, or level-up feedback evidence is missing.
- The artifact is missing or standard-answer fields are empty.

## artifactPath

`artifacts/brotatolike-progression-loop-validation.json`
