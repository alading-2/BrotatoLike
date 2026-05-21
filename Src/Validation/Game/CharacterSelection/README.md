# BrotatoLike Character Selection Validation

## 测试目标

验证 BrotatoLike 第一版角色选择闭环：DataOS 角色 authoring、scene-backed 角色选择 UI、按选择生成玩家、双角色视觉/属性/loadout 差异，以及 HUD/Input/血条/技能栏/镜头绑定。

## expectedInputs

- `BrotatoLikeGameRuntime` initialized from the BrotatoLike DataOS snapshot.
- DataOS generated `character_definition` and `character_loadout` authoring.
- Scene-backed `CharacterSelectPanelUI` with deterministic `deluyi` and `guangfa` entries.
- Runtime selected-character spawn path and production HUD/input/camera services.

## expectedObservations

- Character catalog includes id, display name, player record id, visual scene path, stats and starting loadout.
- Missing player records and missing visual scenes fail catalog validation.
- CharacterSelectPanelUI records scene path, visible ids and button-selected character id.
- Selected character spawn records selected id, player entity id, visual path and starting skills.
- `deluyi` and `guangfa` differ by visual path, base stats and starting loadout.
- Both spawned players bind input, active skill input, HUD, player health bar, skill bar and camera.

## passCriteria

- Stdout contains `BrotatoLike Character Selection validation PASS`.
- `artifacts/brotatolike-character-selection-validation.json` has `status=pass`.
- Checks `character_catalog_authoring_valid / character_select_ui_scene_backed / selected_character_spawns_runtime_player / two_characters_distinct_evidence / selected_player_bindings` all pass.
- `expectedInputs`, `expectedObservations`, `passCriteria`, `failCriteria`, and `artifactPath` are non-empty.

## failCriteria

- Stdout contains `BrotatoLike Character Selection validation FAIL`.
- Character authoring, UI selection, selected spawn, distinct character evidence or player bindings are missing.
- The artifact is missing or standard-answer fields are empty.

## artifactPath

`artifacts/brotatolike-character-selection-validation.json`

## PASS/FAIL 判定

- PASS：所有角色选择 checks 通过，runner `result.json` 标记场景成功，scene gate 能读取完整 artifact oracle。
- FAIL：任一 authoring、UI scene、selected spawn、双角色差异或玩家绑定证据缺失。
