# BUG//REPORT — Project Plan

## 1. Repository inspection (Milestone 0 findings, 2026-10-13)

| Item | Finding |
|---|---|
| Repository | `rayanjainn/glitch`, **empty**: no commits and no branches on the remote. |
| Unity project | **None.** No `Assets/`, `Packages/manifest.json`, or `ProjectSettings/`. This is a new project. |
| Unity version | **Unknown / not installable here.** The development container has no Unity Editor or licence, and the Unity package registry is blocked by network policy. |
| Packages / URP / Input System | Not present (no manifest). |
| Scenes, scripts, assets | None. |
| Build/test tooling | No Unity CLI. The .NET 8 SDK was installed in the container to compile and test engine-independent code. |

**Consequence:** I cannot compile the Unity-dependent code, run Unity tests, open scenes, or produce builds from this environment. Each milestone report separates what was verified here from what needs the Editor.

## 2. Bootstrapping the Unity project (one-time, manual)

Hand-writing `ProjectSettings/*` and `Packages/manifest.json` for an editor version I cannot run risks wrong package versions and a broken URP setup. The reliable path is to let Unity 6.6 generate them from its URP template:

1. Unity Hub → **New project** → Editor **6.6.x** → template **Universal 3D** (URP) → create it anywhere, e.g. `~/tmp/BugReportTemplate`. Close Unity.
2. Copy these from the template into the root of this repo: `Packages/`, `ProjectSettings/`, and `Assets/Settings/` (URP pipeline and renderer assets with their `.meta` files). Without `Assets/Settings` the graphics settings point to a missing pipeline asset.
3. Optional: also copy `Assets/InputSystem_Actions.inputactions` (+ `.meta`) if the template's Project-wide Actions setting references it. Otherwise don't. Our own asset is `Assets/_Project/Input/BugReportControls.inputactions`. Don't copy `Assets/TutorialInfo`, `Assets/Readme.asset`, or the sample scene.
4. Unity Hub → **Add project from disk** → this repo → open with 6.6.
5. Confirm the Package Manager lists **Input System**, **Universal RP**, and **Test Framework**. Install any that are missing from the Unity Registry.
6. Run **BUG REPORT ▸ Setup ▸ 1. Configure Project** (layers + collision matrix + environment checks), then **BUG REPORT ▸ Setup ▸ 2. Build Test Room Scene**.
7. Commit `Packages/`, `ProjectSettings/`, `Assets/Settings/`, the generated scene and materials, and **every `.meta` file Unity created**. Unity assigns GUIDs on first import, so metas must be committed once and then kept.

## 3. Milestones

Each milestone ends playable, tested, and documented, and is merged to `main` only after it is verified in the Editor.

| # | Name | Goal | Exit criteria (summary) |
|---|---|---|---|
| 0 | Inspect & plan | Repo inspection, architecture, task list | This document, ARCHITECTURE.md, TASKS.md ✅ |
| 1 | Vertical-slice foundation | Input, anomaly flight, camera, target selection, placeholder hero, test room, debug UI | See §5. Code done; **Editor verification pending** |
| 2 | Glitch framework + Collision Transfer | `GlitchDefinition` SO, `GlitchOperation`, `GlitchManager`, `ModifierStack`, collision capability, revert (R), invalid-target feedback | Hero walks through the transferred firewall; shadow becomes solid; all apply/revert/destroy/conflict tests green |
| 3 | Duplication + Freeze | Both on the M2 framework (same lifecycle) | Caps respected, unique ids, prefab-based copies, no accumulated velocity, cross-glitch tests |
| 4 | Hero behaviour + Containment | FSM hero (navigation decision), deterministic containment stages + patch action | Hero reliable with no interference; containment escalates and decays per thresholds |
| 5 | Vertical-slice level | One polished room, 3 connected objectives, restart, pause menu, minimal HUD, audio/VFX feedback | Completable from fresh launch; restart leaves no stale modifications |
| 6 | Art & polish | Materials, lighting, anomaly model, outline/glitch shaders, audio, camera polish, profiling | 60 FPS @1080p on the reference PC (measured) |
| 7 | Content expansion | Further glitches/levels, only after the slice proves fun | — |
| 8 | Release prep | Settings + save persistence, rebinding UI, error handling, reproducible build | Clean build from a script; external playtest checklist |

Detailed, checkable tasks are in TASKS.md.

## 4. First playable vertical slice (target: end of Milestone 5)

**Scope:** one handcrafted "training lab" room. It uses Inspect, Collision Transfer, Duplication, and Freeze, has a functioning hero, a simple containment response, three connected objectives, a pause menu, restart, and audio and visual feedback.

**Objective chain (draft, refined in M5):**
1. *Get the hero past the firewall.* Collision Transfer, wall → shadow. Alternatives: transfer the wall's collision to a crate, or let containment patch the wall at stage 3.
2. *Power two terminals with one keycard.* Duplicate the keycard. Containment notices duplicates of an objective item.
3. *Cross the moving shutter / conveyor.* Freeze the shutter or a platform. Alternative: duplicate a crate and freeze it as a step.

**Acceptance criteria:**
- [ ] Launches into the slice from a fresh start with no developer setup steps (after the build exists).
- [ ] Each objective has its intended solution plus at least one alternative, and both are tested.
- [ ] The hero completes the level unaided once the player has made the required changes, and never softlocks along the intended solutions.
- [ ] Every glitch shows valid and invalid feedback, can be reverted, survives target destruction, and is cleaned up on restart and scene change.
- [ ] Containment rises on exploits, decays during normal play, crosses stages at defined thresholds, and resets on restart.
- [ ] Zero recurring console exceptions during a full playthrough. Zero missing references (automated scene test).
- [ ] Restart (pause menu) returns to a known state: the entity count, active operations (0), and containment stage (0) match a fresh load.
- [ ] A new player understands select → apply → revert from in-game prompts alone (observed playtest, 3 people).
- [ ] Profiled at 1080p on the reference PC and the result documented.

## 5. Milestone 1 definition (current)

**Deliverables:** Input System actions (KB/M + gamepad), anomaly free-flight with collision, third-person orbit camera with occlusion handling, screen-centre targeting with valid and invalid feedback, selection model, Playing / Inspection / Paused states, a placeholder hero that gets blocked and retries, a procedurally built test room, a debug overlay, editor setup tooling, and tests.

**Acceptance criteria:**
- [x] Core rules compile at Unity's language level and their unit tests pass outside Unity. *(verified here: 49/49)*
- [ ] Project compiles in Unity 6.6 with zero errors and zero warnings from `BugReport.*`. *(Editor)*
- [ ] EditMode and PlayMode suites pass in the Unity Test Runner. *(Editor)*
- [ ] Play TestRoom: WASD/mouse flight, Space/Ctrl vertical, Shift boost, camera never clips into walls. *(Editor, visual)*
- [ ] Reticle turns cyan on crates/hero/firewall/shadow and red on the pillar. E selects (pulsing cyan box), E again or RMB deselects. *(Editor, visual)*
- [ ] Tab toggles inspection outlines. Esc pauses (time stops, cursor free) and resumes into the previous mode. *(Editor, visual)*
- [ ] Hero walks to WP_0, then toward the exit, stops at the firewall, and the overlay shows BLOCKED. It retries without errors. *(Editor, visual)*
- [ ] F5 restarts with no errors and the entity count is unchanged. *(Editor)*
- [ ] Gamepad: same flow with sticks/A/B/LT/Start. *(Editor, hardware)*

## 6. What can be automated vs. what needs the Editor

| Task type | Automated here | Needs Unity Editor | Needs human eyes |
|---|---|---|---|
| Core logic (rules, state machines, glitch bookkeeping, containment math) | ✅ compile + unit tests | runs again in Test Runner | — |
| Type consistency of all C# | ⚠️ against stubs only | ✅ authoritative compile | — |
| Scene construction | ✅ as an editor script | ✅ run the menu item | layout and feel |
| Layers, collision matrix | ✅ as an editor script | ✅ run it once | — |
| Packages / URP / ProjectSettings | ❌ | ✅ template + copy (§2) | — |
| PlayMode behaviour (physics, hero, scanner) | ✅ tests written | ✅ run them | — |
| VFX, shaders, lighting, materials, animation | partly (scripted materials/params) | ✅ | ✅ readability, polish |
| Audio | ❌ (needs licensed/original assets) | ✅ | ✅ |
| Game feel, puzzle clarity, difficulty | ❌ | ✅ | ✅ playtests |
| Performance | ❌ | ✅ Profiler | on reference hardware |
| Player build | ❌ | ✅ (later: scripted batch build) | smoke test |

**Unblocking option (recommended):** a machine with Unity 6.6 and a licence that can run headless (`-batchmode -runTests`), such as a GitHub Actions runner with GameCI or your own PC. Once it exists I can add a CI workflow that compiles and runs both test suites on every push, which turns most "Editor" cells above into automated checks.

## 7. Risk register

| Risk | Impact | Mitigation |
|---|---|---|
| No Unity in the dev container | Unity-side compile errors found late | Core kept engine-free and tested here; stub type-check; small milestones; CI with a licensed runner (§6) |
| Reversible state corrupted by overlapping glitches/patches | Softlocks, ghost state | Modifier stacks with per-operation ownership, never snapshots (ARCHITECTURE §5.3); per-channel conflict policy; cross-glitch tests |
| Duplicating scene instances clones live/glitched state | Leaked modifiers, shared references | Prefab-only duplication + explicit state transfer (§5.5) |
| Freezing rigidbodies → velocity spikes on release | Physics explosions | Kinematic modifier, stored velocity in the operation, ReleaseAtRest default, Unity 6 ordering rule (§5.6) |
| Hero navigation vs. runtime collision changes | Hero ignores or can't use glitched geometry | Placeholder already uses physical blocking + retry; M4 prototypes NavMesh carving vs. waypoint graph before committing |
| Making objects solid inside other bodies | Depenetration explosions | Overlap check rejects with "OCCUPIED" |
| Intangible dynamic bodies fall out of the world | Lost puzzle items | Kinematic-while-intangible or forbid; kill plane + respawn for registered props |
| Desync glitch readability | Confusing play | Deferred until after the slice (GDD Part 11) |
| Scope creep | No finished slice | Deferred list (GDD Part 11) enforced in TASKS.md |
| Binary asset growth in git | Slow clones | Decide on Git LFS before M6 art lands |

## 8. Branching and commits

- `main` holds verified, releasable code only. Work goes on descriptive feature branches (e.g. `foundation/vertical-slice-setup`) and is merged to `main` once verified in the Editor.
- Commit author: Rayan Jain `<rayansjain@gmail.com>`.

## 9. Open questions for the project owner

1. Exact Unity version installed, e.g. `6000.6.0f1`? I'll pin it in docs and CI.
2. Should binary art and audio go through **Git LFS** (recommended before M6)?
3. Is there a Unity licence and machine (or GameCI secrets) for automated test runs?
4. Merge flow: should I open PRs into `main`, or will you merge feature branches yourself after Editor verification?
