# Prism Zone — Prefab Library

All reusable scene objects live here. **Drag a prefab into any scene to spawn a working instance.** Inspector values below are the knobs you'll most commonly tune.

---

## `Player.prefab`
The protagonist. Only one per scene.

Drop into scene at wherever the spawn point should be.

| Inspector knob | Default | Notes |
|---|---|---|
| `Player Controller › Walk Speed` | 3 | u/s |
| `Player Controller › Run Speed` | 5 | Shift held |
| `Player Controller › Climb Speed` | 2.5 | Only used inside Ladder trigger zones |
| `Player Controller › Run Noise Radius` | 8 | RED enemy hearing range (unused now) |
| `Player Interaction › Interact Radius` | 1.5 | Pickup/Door/Cabinet detection range |

Notes:
- Must be **tagged `Player`**. Prefab already is.
- Needs a scene with `_Bootstrap` (Input System enable).

---

## `GREEN_Watcher.prefab`
The tracking enemy. Walks between waypoints, chases when player enters vision radius.

**Self-contained**: the prefab includes a `Waypoints` child with `WP_1` and `WP_2` already. In the scene, select the waypoints and drag them to wherever the patrol line should go. If you don't need patrol, delete the waypoints — GREEN will stand still until chase triggers.

| Inspector knob | Default | Notes |
|---|---|---|
| `Green Enemy › Vision Range` | 10 | Aggro radius in units |
| `Green Enemy › Vision Angle Deg` | 360 | 360 = omnidirectional. Set 120 for cone. |
| `Green Enemy › Patrol Speed` | 1.5 | u/s walking between waypoints |
| `Green Enemy › Chase Speed` | 3 | u/s following BFS path |
| `Green Enemy › Aggro Loss Time` | 30 | seconds after losing LOS before giving up |
| `Green Enemy › Reveal Filter` | Red | Which filter makes GREEN fully visible |
| `Green Enemy › Hidden Alpha` | 0.35 | Ghost visibility when not revealed (0 = invisible, 1 = always visible) |
| `Green Enemy › Wall Tilemap` | (drag) | Must point to the scene's Tilemap_Walls |

Notes:
- GREEN's BoxCollider is `IsTrigger = true` so it passes through walls visually *but* BFS path respects walls. Do NOT turn off trigger unless you also rewrite collision logic.
- Wall Tilemap reference breaks when duplicated into a new scene — **re-drag the scene's Tilemap_Walls into it after duplicating**.

---

## `RED_Shrike.prefab` *(not in default scene)*
Flying blind ghost. Invisible until it chases. Hears player running (Shift) within 8u radius and dashes straight-line through walls.

| Inspector knob | Default | Notes |
|---|---|---|
| `Red Enemy › Chase Speed` | 5 | u/s, straight line |
| `Red Enemy › Return Speed` | 2 | u/s, walks home after giving up |
| `Red Enemy › Hearing Radius` | 8 | Activation range via player noise pulses |
| `Red Enemy › Stillness Recall Time` | 10 | seconds player must stand still to escape |
| `Red Enemy › Reveal Filter` | Blue | Visible under blue filter |
| `Red Enemy › Hidden Alpha` | 0.35 | Test-friendly ghost visibility |

Notes:
- Will phase through walls — that's the design. No Tilemap ref needed.
- Only reacts when player RUNS (Shift). Walking is silent.

---

## `BLUE_Weaver.prefab` *(not in default scene)*
Static trap. Player stepping in fires `AlarmBroadcaster` for 10 s, pulling all RED/GREEN in the scene toward the alarm point.

| Inspector knob | Default | Notes |
|---|---|---|
| `Blue Trap › Alarm Duration` | 10 | seconds the alarm is broadcast |
| `Blue Trap › Retrigger Cooldown` | 3 | seconds between re-activations |
| `Blue Trap › Reveal Filter` | Green | Visible under green filter |

Notes:
- Has a CircleCollider2D trigger radius 0.5. Player tagged `Player` triggers it.
- Works best in narrow corridors where you can't just walk around it.

---

## `Door_Passcode.prefab`
A door that opens when the player enters the correct 4-digit code.

| Inspector knob | Default | Notes |
|---|---|---|
| `Passcode Door › Passcode` | 3071 | Exactly 4 digits |
| `Passcode Door › Closed Sprite` | prop_door_closed | |
| `Passcode Door › Open Sprite` | prop_door_open | Shown after unlock |

Has two colliders: a solid blocking one + a trigger for interact detection. Both auto-wired in the prefab.

---

## `Cabinet.prefab`
Hideable cabinet. Player presses E to enter, E again to exit.

| Inspector knob | Default | Notes |
|---|---|---|
| `Cabinet › Hide Anchor` | child `HideAnchor` | Player teleports here when inside |
| `Cabinet › Closed Sprite` | prop_cabinet_closed | |
| `Cabinet › Open Sprite` | prop_cabinet_open | Shown when occupied |

Notes:
- Currently GREEN still sees player through cabinet (TODO: wire `PlayerController.IsHidden` into `CanSeePlayer`).

---

## `Pickup_CluePaper.prefab`
A small world item that either adds to inventory and/or shows a centered clue popup when picked up.

| Inspector knob | Default | Notes |
|---|---|---|
| `Pickup › Item Id` | (empty) | If set, adds to Inventory (max 4 slots). Key matches `item.*` in i18n JSON. |
| `Pickup › Clue Text Key` | `clue.room1.code` | i18n key for the CluePopup body. |
| `Pickup › Destroy On Pickup` | true | Cleans up the world item after E |
| `Pickup › One Shot Clue` | true | Only show once, then disappear |

To add a new clue: edit `Assets/Resources/i18n/en.json` and `zh.json`, add a new `"clue.myroom.thing": "Text"` entry, then set the Pickup's `Clue Text Key`.

---

## `HUD_Canvas.prefab`
The complete in-game UI layer. One instance per scene. Render mode: **ScreenSpaceOverlay** (drop and go).

Contents:
- `FilterHUD` — bottom-left filter indicator (4 dots + text)
- `InventoryHUD` — bottom-right 4 slot inventory
- `InteractPrompt` — bottom-center "E to …" prompt (auto-shows when near interactables)
- `CluePopup` — center modal for clue text (opens via `CluePopup.Instance.Show(key)`)
- `PasscodePanel` — center 4-digit keypad for `PasscodeDoor` (auto-opens on door E, close via X/Esc)

**Do not duplicate per level — drag the prefab in.** If you need a custom HUD for a specific level, right-click HUD_Canvas in Project → Create → Prefab Variant.

## `EventSystem.prefab`
Input System UI input module. One instance per scene. Always drop alongside HUD_Canvas.

---

## `_Bootstrap.prefab`
Holds the runtime singletons — **every scene needs exactly one**.

Components: `GameBootstrap`, `FilterManager`, `AlarmBroadcaster`, `Inventory`.

| Inspector knob | Default | Notes |
|---|---|---|
| `Filter Manager › Full Screen Material` | M_PrismFilter | The filter shader material. Do not change. |
| `Inventory › Starting Items` | [`item.glasses`] | Pre-filled inventory slots |

Notes:
- The `Level_Template.unity` scene has a _Bootstrap pre-placed. When you duplicate the template, keep it.

---

## Workflow tips

1. **Always edit prefab (Open Prefab), not the instance**, for changes that should apply to every copy. Edit the instance only for per-level tuning.
2. **Revert override**: if a scene instance gets accidentally modified, right-click the modified field → `Revert` to match the prefab.
3. **New enemy variant**: duplicate `GREEN_Watcher.prefab`, rename, tweak values, drop into scene.
4. **Testing a room standalone**: drop Player + _Bootstrap + GREEN + 1 tile room → press Play.
