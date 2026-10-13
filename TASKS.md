# BUG//REPORT — Task Checklist

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
- [ ] Create a Unity 6.6 URP template project and copy `Packages/`, `ProjectSettings/`, `Assets/Settings/` (PROJECT_PLAN §2) [editor]
- [ ] Remove template packages we don't use (e.g. Visual Scripting, Multiplayer Center) if present [editor]
- [~] `BUG REPORT ▸ Setup ▸ 1. Configure Project`: layers, Intangible collision matrix, Input System/URP/colour-space checks [editor]
- [ ] Commit generated `.meta` files, ProjectSettings, Packages [editor]

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
- [ ] Unity compile: 0 errors, 0 `BugReport.*` warnings [editor]
- [ ] Test Runner: EditMode + PlayMode all green [editor]
- [ ] Manual checklist in PROJECT_PLAN §5 [editor][visual]
- [ ] Gamepad pass [hw]
- [ ] Merge to `main`

---

## Milestone 2 — Glitch framework + Collision Transfer (next)
- [ ] Core `ModifierStack<T>` with owner handles, priority, conflict policy (Exclusive/Stack) + tests [auto]
- [ ] Core `GlitchOperation`, `OperationState`, end reasons; `GlitchManager` (apply, revert, LIFO revert, by-target, EndAll, invalidation cascade, expiry with injected clock) + tests [auto]
- [ ] `GlitchDefinition` ScriptableObject → `TargetRequirement`, arity, duration, colour, cost/cooldown, containment weight [auto/editor]
- [ ] `IGlitchEffect` strategy interface; `GlitchController` player workflow (select glitch → collect targets → apply; R revert; RMB cancel) [auto/editor]
- [ ] `CollisionCapability`: explicit collider list, PhysicsLayer channel, latent colliders [editor]
- [ ] Collision Transfer effect: occupancy check ("OCCUPIED"), dynamic-body policy decided + documented [editor]
- [ ] Capability-component validation (flag without component → clear failure) [auto]
- [ ] Feedback: source/target wireframe, transfer pulse (placeholder VFX), invalid-target message [editor][visual]
- [ ] PlayMode tests: apply, revert, expire, target destroyed mid-operation, double-apply conflict, scene unload cleanup, unrelated objects unaffected [editor]
- [ ] Test room: firewall → shadow solution works; hero walks through and arrives [editor][visual]

## Milestone 3 — Duplication + Freeze
- [ ] `DuplicationCapability` (prefab reference), prefab-only instantiation, approved state transfer, lineage, caps, spawn overlap check [editor]
- [ ] `FreezeCapability`: kinematic + LocalTimeScale + animator channels; `EntityClock` for scripted movers; release policies [editor]
- [ ] Cross-glitch tests: freeze + collision; duplicate of frozen object; collision on duplicate then despawn [editor]
- [ ] Demonstration scene / test-room additions [editor][visual]

## Milestone 4 — Hero behaviour + Containment
- [ ] Navigation spike: NavMesh carving vs waypoint graph with runtime passability. Decide and record it [editor]
- [ ] Hero FSM (Idle, Patrol, Investigate, Confused, …) driven by objectives [editor]
- [ ] Core `ContainmentModel` with decay, thresholds, hysteresis, reset + tests [auto]
- [ ] Containment responders: UI warning, audio, PatchAction via `GlitchManager` [editor]

## Milestone 5 — Vertical-slice level
- [ ] Objective system (explicit conditions) [auto/editor]
- [ ] Training-lab room, three connected objectives with alternatives (PROJECT_PLAN §4) [editor][visual]
- [ ] Pause menu, restart, minimal HUD (objective, target, glitch, containment) [editor][visual]
- [ ] Persistent Bootstrap scene + level loading/cleanup [editor]
- [ ] Placeholder audio set (original/licensed) [editor]

## Milestones 6–8
See PROJECT_PLAN §3. Not broken down yet, deliberately, to avoid planning against unknowns.

---

## Manual playtest checklist (Milestone 1)
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
