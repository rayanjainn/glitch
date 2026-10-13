# BUG//REPORT — Task Checklist

> Manual playtesting is deferred by the owner. Use **PLAYTEST.md** (plain language) when ready. The per-milestone checklists below are the detailed engineering versions.

Tags: **[auto]** done and verified in the dev container · **[editor]** needs the Unity Editor · **[visual]** needs a human to look/feel · **[hw]** needs hardware (gamepad, reference PC).

Status: `[x]` done and verified · `[~]` implemented, verification pending · `[ ]` not started.

---

## Milestone 0 — Inspect & plan ✅
- [x] Inspect repository (empty; no Unity project) [auto]
- [x] PROJECT_PLAN.md, ARCHITECTURE.md, TASKS.md, DEVLOG.md [auto]
- [x] Risk register and automation matrix [auto]

## Milestone 1 — Vertical-slice foundation (current)

### Project setup
- [x] `.gitignore`, `.gitattributes` for Unity [auto]
- [x] Create a Unity 6.6 URP template project and copy `Packages/`, `ProjectSettings/`, `Assets/Settings/` (6000.6.0f1) [editor]
- [ ] Remove template packages we don't use: `com.unity.visualscripting`, `com.unity.collab-proxy` (Plastic SCM; we use git). Keep `ai.navigation` (M4 decision) and `timeline` (cinematics later) [editor]
- [x] `BUG REPORT ▸ Setup ▸ 1. Configure Project`: layers, Intangible collision matrix, Input System/URP/colour-space checks [editor]
- [x] Commit generated `.meta` files, ProjectSettings, Packages (audited: no missing/orphan metas, all scene GUIDs resolve) [editor]
- [x] Build settings: removed the template's missing SampleScene and the dangling project-wide input actions reference [auto]

### Core (engine-free)
- [x] `GameEntityId`, `EntityCategory`, `EntityCapabilities`, `IEntityDescriptor` [auto]
- [x] `EntityRegistry`: unique, never-reused ids, dead-entity pruning, reentrancy-safe clear [auto]
- [x] `TargetRequirement` + `TargetValidator` with typed failure reasons [auto]
- [x] `SelectionModel`: hover/selection by id, auto-clear on unregister [auto]
- [x] `GameStateMachine`: explicit transitions, pause resumes the previous mode [auto]
- [x] `ProgressMonitor`: stall detection for agents [auto]
- [x] 49 NUnit tests, passing via `Tools/CoreTests/run-core-tests.sh` [auto]

### Runtime (Unity)
- [~] `GameServices` + `GameBootstrap`: per-level services, load-order-safe `WhenReady`, static reset [editor]
- [~] `GameEntity`: registration lifecycle, bounds [editor]
- [~] `BugReportControls.inputactions` (KB/M + Gamepad) + `InputReader` [editor]
- [~] `AnomalyMotor`: free flight, accel/decel, boost, collision without velocity build-up [editor]
- [~] `AnomalyCameraRig`: orbit, pitch clamp, sphere-cast occlusion, pointer vs stick sensitivity [editor]
- [~] `AnomalyPlayerController`: input → motor/camera/selection/state [editor]
- [~] `TargetScanner`: nearest-hit sphere cast, ignores the anomaly's hierarchy, allocation-free [editor]
- [~] `SelectionController`: select/toggle/cancel + feedback events [editor]
- [~] `PlaceholderHero`: waypoint walk, blocked detection, retry [editor]
- [~] `GameStatePresenter`: cursor lock + pause time scale, restored on destroy [editor]
- [~] `EntityHighlightPresenter` + `BoundsOutline`: inspection/hover/selection wireframes [editor][visual]
- [~] `ReticlePresenter`, `DebugOverlay` (F1 toggle, F5 restart) [editor][visual]
- [~] `AnomalyVisual`: placeholder core + orbiting fragments [visual]

### Editor tooling
- [~] `BUG REPORT ▸ Setup ▸ 2. Build Test Room Scene`: room, firewall, exit, props, hero + route, anomaly, camera, systems, materials, build settings [editor]
- [x] Builder's serialized field names cross-checked against component fields (script) [auto]

### Tests (Unity)
- [~] EditMode: input asset completeness, layer/matrix config, generated scene integrity (single systems, no missing scripts, no null refs, build settings) [editor]
- [~] PlayMode: motor (8), scanner (5), hero incl. blocked → wall removed → arrives (5), entity lifecycle (8), selection flow (5) [editor]
- [x] All project C# type-checks against Unity API stubs (`Tools/CompileCheck`, 0 errors). Weaker than a Unity compile [auto]

### Verification (exit gate)
- [x] Unity compile: 0 errors (owner, 6000.6.0f1). Warning count not reported [editor]
- [x] Test Runner: EditMode + PlayMode all green (owner-reported) [editor]
- [ ] Manual checklist in PROJECT_PLAN §5 [editor][visual]
- [ ] Gamepad pass [hw]
- [ ] Merge to `main`

---

## Milestone 2 — Glitch framework + Collision Transfer ✅ (tests verified in Unity, merged; manual playtest deferred)
- [x] Core `ModifierStack<T>`: owner leases, priority, conflict policy (Exclusive/Stack), base changes survive revert. 10 tests [auto]
- [x] Core `GlitchSpec`, `GlitchOperation`, `GlitchManager`: apply/validate, cooldown, max active, expiry (injected clock), revert, LIFO revert, EndAll, target-lost cascade, effect veto (`CanEnd`), exception isolation. 28 tests [auto]
- [~] `GlitchDefinition` ScriptableObject (slots, duration, cooldown, max active, colour, containment weight) + `CollisionTransferDefinition` [editor]
- [~] `IGlitchEffect` strategy; `GlitchController` workflow (E select source, aim target, LMB apply, R revert, Q cycle) [editor]
- [~] `CollisionCapability`: explicit colliders, exclusive mode channel, layer switching [editor]
- [~] Collision Transfer effect: occupancy rejection, blocked revert while occupied, dynamic sources rejected (policy decided) [editor]
- [~] Capability validation: flag without component → clear failure + EditMode scene test [editor]
- [~] Feedback: ghost/solidified materials, glitched outlines, transfer beam, reticle preview, overlay glitch panel [editor][visual]
- [~] PlayMode tests: CollisionTransferTests (15), GlitchControllerTests (6); EditMode GlitchDefinitionTests (3) + scene tests [editor]
- [~] Test room: FIREWALL_01 (solid), SHADOW_DECAL and HOLO_PANEL (intangible). Firewall → shadow lets the hero through; firewall → holo panel blocks it again [editor][visual]
- [ ] Remove unused template packages (Visual Scripting, Collab Proxy) [editor]

### Manual playtest checklist (Milestone 2)
After **BUG REPORT ▸ Setup ▸ 2. Build Test Room Scene → Rebuild**, press Play:
1. The overlay shows `GLITCH COLLISION TRANSFER` and `SELECT A SOURCE [E]`.
2. Aim at **FIREWALL_01** → E (selected). Aim at **SHADOW_DECAL** → the reticle is cyan and the overlay says READY. Press LMB:
   - a blue beam runs firewall → shadow;
   - the firewall turns see-through blue and the shadow glows;
   - both stay outlined blue.
3. The hero (BLOCKED) retries within about 2 s, walks through the firewall, and reaches the exit (**ARRIVED**).
4. Fly the anomaly through the ghost firewall: no collision.
5. R → both restored. Do it while flying *inside* the ghost firewall: you get "CANNOT RESTORE … INSIDE" and nothing changes. Fly out, press R again: restored.
6. Restart (F5). Transfer firewall → **HOLO_PANEL**: the hero passes the firewall but is BLOCKED at the solid panel.
7. Invalid cases each show a red reticle and a reason:
   - crate as source (missing capability);
   - shadow as source (already intangible);
   - firewall onto the firewall (must differ);
   - LMB with nothing selected (prompt).
8. Esc pause → LMB/R do nothing. Resume works.

## Milestone 3 — Duplication + Freeze ✅ (tests verified in Unity, merged; manual playtest deferred)
- [x] Core: operation products (`AddProduct`, `Involves`, `CountActiveOn`); losing a product ends the operation; copy-of-copy chains collapse. 4 new tests (91 total) [auto]
- [~] `FreezeCapability` (exclusive frozen channel, kinematic + animator handling, release at rest), `ITimeScaled` + `PingPongMover`, `FreezeDefinition` (duration, release policy), `FreezeEffect` (velocity snapshot in the operation) [editor]
- [~] `DuplicationProfile` (prefab, per-source cap, objective policy, inherit velocity), `DuplicationCapability`, `DuplicateMarker`, `DuplicationDefinition` (global cap), `DuplicationEffect` (prefab-only spawn under inactive holder, free-space placement, despawn lease) [editor]
- [~] `RendererOverrides` (priority material arbitration); collision/freeze visuals use it [editor][visual]
- [~] Presenters: per-glitch outline colours, outlines on copies, beam source → copy [editor][visual]
- [~] Test room: crates are prefab instances (PF_Crate + DP_Crate), ball starts mid-air, LIFT_PLATFORM oscillates; Q cycles COLLISION → DUPLICATION → FREEZE [editor]
- [~] Tests: FreezeTests (11), DuplicationTests (12), RendererOverridesTests (1), EditMode capability/prefab checks [editor]
- [ ] Remove unused template packages (Visual Scripting, Collab Proxy) [editor]

### Manual playtest checklist (Milestone 3)
After **Build Test Room Scene → Rebuild**, press Play:
1. Q cycles the glitch shown in the overlay: COLLISION TRANSFER → DUPLICATION → FREEZE.
2. **FREEZE** on TEST_BALL while it falls: it hangs pale cyan in mid-air for 8 s, then drops from rest. The crates and the lift keep working meanwhile.
3. **FREEZE** on LIFT_PLATFORM: it stops mid-travel, then resumes from the same height (no jump).
4. **DUPLICATION** on CRATE_A: a magenta beam, then "CRATE_A COPY n" appears beside it (not inside anything) and stays outlined magenta.
   - A second copy works. A third gives "COPY LIMIT".
   - R removes the newest copy.
5. Freeze a copy, then revert the duplication: the copy vanishes and its freeze ends ("TARGET LOST").
6. Freeze CRATE_A, then duplicate it: the copy falls and is not frozen.
7. Duplicate a crate pushed into a corner: "NO SPACE" when every side is blocked.
8. Invalid cases: duplicating the firewall, freezing the hero, freezing the pillar → red reticle and a reason.
9. F5 restart: no copies remain, nothing stays frozen, ENTITIES count matches a fresh load.

## Milestone 4 — Hero behaviour + Containment ✅ (tests verified in Unity, merged; manual playtest deferred)
- [x] Navigation decision: authored waypoint graph + live capsule-sweep passability (not NavMesh); see ARCHITECTURE §6 [auto]
- [x] Core `NavGraph` (A*, partial paths, deterministic). 8 tests [auto]
- [x] Core `ContainmentModel` + `ContainmentSettings` (explicit sources, decay, hysteresis, patch relief, reset). 12 tests [auto]
- [x] Core: `GlitchSpec.ContainmentWeight`, `GlitchManager.CooldownScale` / `ActiveContainmentWeight` / `ActiveProductCount`. 2 tests (113 total) [auto]
- [~] `HeroNavNode`, `HeroNavGraph`, `HeroController` FSM (Idle/Walking/Blocked/Confused/Arrived); replaces `PlaceholderHero` [editor]
- [~] `ContainmentSettingsAsset`, `ContainmentDirector` (cooldown scaling, patches oldest revertible glitch, skips vetoed), `ContainmentPresenter`, patched-object flash [editor][visual]
- [~] Test room: 7-node hero graph (firewall is the only way east; the holo panel can be bypassed north/south), containment settings asset [editor]
- [~] Tests: HeroControllerTests (7), ContainmentDirectorTests (7), CollisionTransferTests migrated to the new hero, scene tests (graph connectivity, systems) [editor]
- [ ] Remove unused template packages (Visual Scripting, Collab Proxy) [editor]

### Manual playtest checklist (Milestone 4)
After **Build Test Room Scene → Rebuild**, press Play:
1. The hero walks from spawn to just before the firewall and stops: overlay `HERO BLOCKED … (partial)`.
2. Transfer FIREWALL_01 → SHADOW_DECAL: within about 0.3 s the hero re-plans and walks to the exit (**ARRIVED**).
3. Restart. Transfer FIREWALL_01 → HOLO_PANEL: the hero passes the firewall and **walks around** the now-solid panel (north or south), then arrives.
4. Restart. Duplicate a crate and push or drop copies so they sit on the hero's path: it goes CONFUSED, then re-routes or waits.
5. Containment:
   - apply several glitches quickly → the pressure bar rises → "// ANOMALY DETECTED //", then "LOCAL INVESTIGATION" (cooldowns x1.5 in the overlay);
   - keep going → "PATCH DEPLOYMENT" with a countdown → the oldest glitch is reverted, flashes red/white, "PATCHED BY SYSTEM".
6. Stop glitching → pressure falls back to 0 and the banner disappears.
7. Stand inside the ghost firewall when a patch is due: it patches something else instead, never the wall around you.
8. F5 restart: containment back to stage 0.

## Milestone 5 — Vertical-slice level (current)
- [x] Core `ObjectiveTracker`: sequential, latching, cascades when alternatives satisfy later goals. 8 tests (121 total) [auto]
- [~] Level mechanics: `PressurePlate` (weight incl. frozen bodies and copies), `GateController` (latched, announces world change), `HeroHazard` (hero refuses dangerous machinery; unfreeze/restore vetoed while someone is in its path), `WorldBoundsGuard` kill plane [editor]
- [~] `ObjectiveDirector` + `HeroReachedZone` (objectives describe goals, not solutions → alternatives count) [editor]
- [~] Pause menu (resume / restart / quit), level-complete screen, minimal HUD (objective + hint, glitch + cooldown, contextual prompt) [editor][visual]
- [~] Procedural placeholder SFX (original, no licensing) wired to selection, glitches, patches, containment stages, objectives [editor][listen]
- [~] Builder refactor: shared helpers + `SliceLevelBuilder` → `Level_01_TrainingLab.unity`, first in Build Settings; `[OptionalReference]` for legitimately empty fields [editor]
- [~] Tests: LevelMechanicsTests (8), SliceLevelSceneTests (7), and **SliceLevelPlaythroughTests**, which plays the whole level with the intended solution through the player workflow [editor]
- [ ] Manual playtest with PLAYTEST.md (deferred by owner; covers M1–M5 in plain language) [visual]
- [ ] Remove unused template packages (Visual Scripting, Collab Proxy) [editor]

## Milestones 6–8
See PROJECT_PLAN §3. Not broken down yet, deliberately, to avoid planning against unknowns.

---

## Manual playtest checklist (Milestone 1)
0. One-time: Edit/Unity ▸ Preferences ▸ General ▸ **Script Changes While Playing = Recompile After Finished Playing** (hot-reloading during Play mode is not supported).
1. Open `Assets/_Project/Scenes/TestRoom.unity` → Play. The Console should show no errors. One warning appears if layers are missing.
2. Click the Game view to capture the mouse. Fly with WASD, Space/Ctrl, Shift. Bump into walls and the ceiling, and fly over the firewall.
3. Orbit the camera into a wall corner. The view should pull in rather than show the inside of geometry.
4. Aim at a crate → cyan reticle and a white box. E → pulsing cyan box and "SELECTED: CRATE_A". E again → cleared.
5. Aim at the pillar → red reticle and a red box. E → "INCOMPATIBLE: REQUIRES INSPECTABLE".
6. Aim at the shadow decal on the floor → selectable even though it is on the Intangible layer. Fly down into it → nothing collides with it.
7. Tab → faint cyan boxes on every inspectable entity. Tab → off.
8. Esc → "PAUSED", hero frozen, cursor free. Esc → resumes into the previous mode.
9. Watch the hero: Idle → WALKING → WAITING at WP_0 → WALKING → BLOCKED at the firewall → periodic retries.
10. F5 → level restarts with no errors and the same ENTITIES count.
