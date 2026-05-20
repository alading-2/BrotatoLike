# BrotatoLike Playable UX Validation

## expectedInputs

- `BrotatoLikeGameRuntime` initialized from the BrotatoLike DataOS snapshot.
- A DataOS player, DataOS-spawned enemies, and Godot input actions `MoveRight`, `NextSkill`, `PreviousSkill`, and `UseSkill`.
- Formal BrotatoLike-owned UI nodes mounted by production gameplay code, not by this validation scene.

## expectedObservations

- The formal HUD host exposes player HP, skill slots, current selection, cooldown state, and progression summary.
- Enemy head health bars follow enemy nodes, update when HP changes, and clean up when an enemy is destroyed.
- Real Godot input actions drive skill selection/use, point targeting, and combat feedback nodes.
- **Scene-backed evidence**: formal UI nodes (HUD root, skill slots, head health bars, damage/heal numbers, targeting indicator) MUST have non-empty `SceneFilePath`, proving they were instantiated from PackedScene, not built via `new Control`/`new Label`/`new ProgressBar` in C#.

## passCriteria

- Stdout contains `BrotatoLike Playable UX validation PASS`.
- `artifacts/brotatolike-playable-ux-validation.json` has `status=pass`.
- `expectedInputs`, `expectedObservations`, `passCriteria`, `failCriteria`, and `artifactPath` are non-empty.

## failCriteria

- Stdout contains `BrotatoLike Playable UX validation FAIL`.
- Any formal HUD, health bar, skill bar, point targeting, damage number, or visibility check fails.
- Any formal UI node has empty `SceneFilePath` (code-created, not scene-backed).
- The artifact is missing or standard-answer fields are empty.

## artifactPath

`artifacts/brotatolike-playable-ux-validation.json`
