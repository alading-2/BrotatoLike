# BrotatoLike Gameplay Lifecycle Integration Validation

> 端到端游戏循环集成验证。对应 `openspec/specs/gameplay-lifecycle-integration/bdd.md` 的 8 个 Scenario。

## expectedInputs

- `BrotatoLikeGameRuntime` with `AutoInitialize=false, AutoTick=true`
- DataOS snapshot with `unit.player/deluyi` record
- Player spawned via `SpawnPlayer()`
- Framework Capabilities: Damage, Movement, Ability, Collision, Unit
- Game-side components: BrotatoLikePlayerInputComponent, GodotActiveSkillInputComponent, BrotatoLikeHud, BrotatoLikeProgressionService

## expectedObservations

- `death_blocks_movement_input`: Death sets CanMoveInput=false and clears InputDirection
- `death_blocks_skill_input`: Skill events (NextSkill/PreviousSkill/UseSkill) are ignored on dead player
- `death_camera_stays_enabled`: Camera2D remains Enabled=true during death
- `death_auto_respawn`: Respawn restores HP=MaxHp, position=(0,0), CanMoveInput=true, Camera enabled
- `camera_follows_player`: Camera2D has Enabled=true, PositionSmoothingEnabled=true, PositionSmoothingSpeed>0
- `concurrent_systems_no_conflict`: Skill cooldown + contact damage + enemy loot + HUD update in same frame without exceptions
- `pause_resume_state_integrity`: HP, position, and cooldown values are identical before and after pause/resume cycle
- `hud_death_respawn_state_clean`: After respawn, player is alive with HP>0

## passCriteria

- stdout contains `BrotatoLike Gameplay Lifecycle validation PASS`
- artifact `brotatolike-gameplay-lifecycle-validation.json` has `status=pass`
- `failureReasons` is empty
- All 8 checks return `true`

## failCriteria

- stdout contains `BrotatoLike Gameplay Lifecycle validation FAIL`
- Any integration check fails with specific failure reason
- Artifact shows individual check failures

## artifactPath

`brotatolike-gameplay-lifecycle-validation.json`
