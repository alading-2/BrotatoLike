# BrotatoLike Manual Device QA

> 更新日期：2026-05-21
> 范围：真实键鼠、真实手柄、窗口焦点、鼠标点选和 UI 焦点导航。
> 结论口径：自动 Godot runner 的 `Input.ActionPress` 只能作为 automated evidence，不能替代真实设备 pass。

## Evidence Boundary

| Evidence type | 可以证明 | 不能证明 |
| --- | --- | --- |
| automated runner / `Input.ActionPress` | Input action 能进入 runtime、技能栏和 artifact oracle | 真实键盘、鼠标、手柄、窗口焦点、摇杆漂移、按钮映射、UI 焦点体验 |
| real keyboard/mouse | 物理键盘、鼠标和窗口焦点下的实际操作 | 手柄映射和手柄 UI 焦点 |
| real gamepad | 摇杆、D-pad、按钮、肩键、Start、UI 焦点导航 | 鼠标屏幕坐标、键盘焦点 |

当前自动验证已覆盖：`MoveLeft/MoveRight/MoveUp/MoveDown`、`UseSkill`、`PreviousSkill`、`NextSkill`、`ConfirmTarget`、`CancelTarget`、`PauseGame` 的 runner action 路径。真实设备 QA 必须单独记录为 `pass`、`fail` 或 `not-tested`。

## Input Map Summary

| Workflow | Keyboard / mouse mapping | Gamepad mapping | Source |
| --- | --- | --- | --- |
| Movement | `WASD`、方向键 | left stick axis 0/1、D-pad | `project.godot` |
| Use skill | Space | button index `2` | `project.godot` |
| Previous / next skill | `Q` / `E` | button index `4` / `5` | `project.godot` |
| Confirm / cancel target | Enter / Esc | button index `0` / `1` | `project.godot` |
| Pause / resume | `P` | button index `6` | `project.godot` |

## Workflows Requiring Real-Device QA

| Workflow | Existing automated evidence | Real-device evidence required |
| --- | --- | --- |
| Movement | Main、PlayableUX、Input validation use `Input.ActionPress` and runtime data assertions | Window focused, physical keyboard movement, real gamepad left stick and D-pad movement, no stuck input after focus loss |
| Skill switching and release | Main、GameLifecycle、PlayableUX artifacts cover `PreviousSkill` / `NextSkill` / `UseSkill` action path | Q/E and LB/RB selection feel, Space and gamepad skill button release, selected slot visual feedback under real input |
| Point target confirm/cancel | PlayableUX and Main artifacts cover action-driven targeting session | Mouse target positioning, confirm/cancel under keyboard and gamepad, cancel cleanup after focus changes |
| Pause/resume | Progression and RunFlow artifacts cover schedule gate and pause state | Real keyboard/gamepad pause toggle, pause menu focus, resume button/focus behavior |
| Death/respawn | GameLifecycle, RunFlow and Main artifacts cover respawn state | Physical input ignored while dead/respawning and restored after respawn |
| UI focus navigation | Scene artifacts prove scene-backed UI exists | Pause menu, character selection, level-up choice and shop focus traversal with keyboard/gamepad |

## Run Record Template

Copy this section for each manual QA pass. Do not mark a row `pass` unless the listed physical device was actually used.

| Field | Value |
| --- | --- |
| date | `<YYYY-MM-DD>` |
| commit | `<git rev-parse --short HEAD>` |
| OS | `<OS and version>` |
| Godot run mode | `<editor play / exported debug / exported release>` |
| device model | `<keyboard model / mouse model / gamepad model>` |
| display mode | `<windowed / fullscreen / resolution / monitor count>` |
| input mapping notes | `<custom mappings, OS remappers, Steam Input, deadzone notes>` |
| tester notes | `<free-form notes>` |

## Keyboard / Mouse Checklist

| ID | Workflow | Steps | Expected | Status | Evidence / notes |
| --- | --- | --- | --- | --- | --- |
| KM-01 | Window focus | Launch `res://Scenes/Main.tscn`, click game window, press `WASD` and arrows | Player moves only when window is focused; focus loss does not leave stuck movement | not-tested | No physical device QA executed in this agent session |
| KM-02 | Movement | Hold and release each movement key, then move diagonally | Player direction changes smoothly and stops after release | not-tested |  |
| KM-03 | Skill switching | Press `Q` and `E` repeatedly | Selected active skill changes once per press and UI highlight follows | not-tested |  |
| KM-04 | Skill release | Select `slam`, `chain_lightning`, `target_point_skill`, and `dash`; press Space | Skill triggers through the selected slot; cooldown/targeting state is visible | not-tested |  |
| KM-05 | Point target confirm | Select `target_point_skill`, press Space, move mouse or set target, press Enter | Indicator appears, target clamps to range, confirm triggers the skill | not-tested |  |
| KM-06 | Point target cancel | Start point targeting, press Esc | Indicator and targeting session clear; cooldown is not consumed before confirm | not-tested |  |
| KM-07 | Pause/resume | Press `P`, navigate pause UI, resume | Runtime pauses, UI is focused, resume restores gameplay input | not-tested |  |
| KM-08 | Death/respawn | Trigger or wait for death/respawn path during a run | Input is blocked while dead and restored after respawn | not-tested |  |
| KM-09 | UI focus | Open character select, level-up choice, shop or pause UI where available | Keyboard focus is visible, deterministic, and does not trap input | not-tested |  |

## Gamepad Checklist

| ID | Workflow | Steps | Expected | Status | Evidence / notes |
| --- | --- | --- | --- | --- | --- |
| GP-01 | Device detection | Connect gamepad before launch and after launch | Godot receives the device without remapping surprises | not-tested | No physical gamepad available in this agent session |
| GP-02 | Stick movement | Move left stick in all directions and release | Player movement tracks stick direction and returns to idle without drift | not-tested |  |
| GP-03 | D-pad movement | Press D-pad directions | Player movement matches D-pad input | not-tested |  |
| GP-04 | Skill previous/next | Press shoulder buttons mapped to previous/next skill | Selected slot changes once per press and wraps correctly | not-tested |  |
| GP-05 | Use skill | Select each visible skill and press mapped use-skill button | Skill triggers through active slot and cooldown/targeting state is visible | not-tested |  |
| GP-06 | Point target confirm/cancel | Start point targeting, confirm with button `0`, cancel with button `1` | Confirm triggers target skill; cancel clears indicator/session | not-tested |  |
| GP-07 | Pause/resume | Press Start/button `6`, navigate pause UI, resume | Runtime pauses, focus is usable, resume restores gameplay input | not-tested |  |
| GP-08 | Death/respawn | Keep using stick/buttons during death/respawn | Input is ignored while gated and restored after respawn | not-tested |  |
| GP-09 | UI focus navigation | Navigate pause menu, character select, level-up choice and shop UI | Focus traversal is visible, reversible, and no panel traps focus | not-tested |  |

## Initial QA Record

| Field | Value |
| --- | --- |
| date | 2026-05-21 |
| commit used to prepare checklist | `9cb405c` |
| OS | Linux `slime-VMware-Virtual-Platform` 6.17.0-29-generic |
| Godot run mode | not-run manually in this agent session |
| device model | not available |
| display mode | not-tested |
| input mapping notes | `project.godot` mappings reviewed; no physical remapper verified |
| tester notes | Checklist created from current input map and code. All real-device workflows remain `not-tested` until a human executes them on hardware. |

## Failure Report Template

Use one record per failed checklist item.

| Field | Value |
| --- | --- |
| checklist id | `<KM-xx or GP-xx>` |
| device / OS / commit | `<device model, OS, commit>` |
| reproduction steps | `<numbered manual steps>` |
| expected behavior | `<what should happen>` |
| observed behavior | `<what happened>` |
| evidence | `<screenshot, recording, log path, or notes>` |
| follow-up link | `<OpenSpec change id, issue, or DocsAI follow-up entry>` |
| status after triage | `<open / fixed / deferred / duplicate>` |

## Follow-Up Policy

- `fail` entries MUST become a bugfix OpenSpec change or an explicit DocsAI follow-up entry before release.
- `not-tested` entries MUST remain `not-tested`; do not infer pass from automated runner evidence.
- If no physical gamepad is available, keep all `GP-*` rows `not-tested`.
- If mouse positioning cannot be verified in a real window, keep point target mouse rows `not-tested`.
