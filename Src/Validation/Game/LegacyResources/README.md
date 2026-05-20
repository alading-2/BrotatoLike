# BrotatoLike Legacy Resource Classification Validation

## expectedInputs

- `res://DataOS/Snapshots/runtime_snapshot.json` generated from BrotatoLike DataOS authoring.
- Snapshot `resources[]` entries with `path`, `key`, `category`, and `legacyStatus`.
- Godot `ResourceLoader.Exists` checks for entries marked as active.

## expectedObservations

- Every old `res://Src/...` or `res://Data/...` resource path has an explicit classification.
- Active legacy paths must be loadable; missing old paths must be classified as `missing`, `legacy-input`, or `intentionally-dropped`.
- The artifact lists legacy counts and every invalid active legacy path.

## passCriteria

- Stdout contains `BrotatoLike Legacy Resource Classification validation PASS`.
- `artifacts/brotatolike-legacy-resource-classification-validation.json` has `status=pass`.
- `expectedInputs`, `expectedObservations`, `passCriteria`, `failCriteria`, and `artifactPath` are non-empty.

## failCriteria

- Stdout contains `BrotatoLike Legacy Resource Classification validation FAIL`.
- Any old `res://Src/...` or `res://Data/...` path has an unsupported classification.
- Any missing old path remains classified as active.

## artifactPath

`artifacts/brotatolike-legacy-resource-classification-validation.json`
