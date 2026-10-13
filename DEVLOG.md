# BUG//REPORT — Development Log

## 2026-10-13 — Milestone 0 + Milestone 1 (code)

**Context:** The repository was empty. The dev container has no Unity Editor, and the Unity package registry is unreachable from it. I installed the .NET 8 SDK to compile and test engine-free code.

**Done**
- Wrote the plan, architecture, and tasks.
- Engine-free Core: entity ids/registry, capability flags, target validation, selection model, game state machine, progress monitor.
- Unity runtime: services/bootstrap, GameEntity, InputReader + actions asset, anomaly motor/camera/controller/visual, target scanner, selection controller, placeholder hero, presenters (state, highlights, reticle), debug overlay.
- Editor tooling: project configuration (layers, collision matrix, environment validation) and a procedural test-room builder.
- Tests: 49 Core unit tests, 3 EditMode fixtures, 5 PlayMode fixtures.

**Verified here**
- `Tools/CoreTests/run-core-tests.sh`: Core compiles as netstandard2.1 / C# 9 with warnings-as-errors. **49 passed, 0 failed.**
- `Tools/CompileCheck`: all 47 project C# files type-check against hand-written Unity API stubs (0 errors). This catches mistakes in our own code only, not wrong assumptions about Unity's API.
- A script cross-checked every serialized field name the scene builder wires through `SerializedObject`.

**Not verified (needs the Editor)**
- Unity compile, EditMode/PlayMode test runs, scene generation, visuals, input feel, gamepad.

**Decisions**
- Core is engine-free (`noEngineReferences`) so the most important rules are testable without Unity.
- Packages/ProjectSettings come from Unity's own URP template instead of being hand-written (PROJECT_PLAN §2).
- Namespaces `BugReport.EditorTools` and `BugReport.InputHandling` avoid shadowing `UnityEditor.Editor` and `UnityEngine.Input`.
- The floor and outer walls are not entities, which keeps selection outlines readable.
- Targeting includes the Intangible layer so collision-removed objects stay restorable.
- `AnomalyMotor` derives post-collision velocity from its own displacement and delta time, not `CharacterController.velocity`, so it stays deterministic under manual ticking in tests.

## 2026-10-13 — Owner answers, testing workflow

- Unity 6.6 is confirmed. The exact build is pending until `ProjectVersion.txt` is committed.
- Git LFS was adopted: binary image, 3D, audio, video, font and lighting-data patterns now route through LFS.
- Added TESTING.md, a novice-friendly path from "new empty Unity project" to "branch tested, ready to merge".
- Added `Tools/UnityTests/run-unity-tests.{ps1,sh}` for headless EditMode/PlayMode runs. **Not executed** (no Unity here); first real run will be on the owner's machine.
- Licence/automation: no credentials are shared in chat. Preferred path is running Claude Code on the owner's machine; GameCI with GitHub secrets is a later option.

## 2026-10-13 — First Unity compile (owner's machine, Unity 6.6)

- **Result:** 8 errors in `BugReport.Runtime`. All came from `CS0104: 'EntityId' is an ambiguous reference between 'BugReport.Core.Entities.EntityId' and 'UnityEngine.EntityId'`. Unity 6.x added its own `UnityEngine.EntityId`.
- **Fix:** renamed our type to `GameEntityId`, including the file and its tests.
- **Gap:** the stub compile check missed this because the stubs didn't contain `UnityEngine.EntityId`. Added it so this class of collision now reproduces in `Tools/CompileCheck`.
- Editor and test assemblies never compiled (they depend on Runtime), so more errors may still appear on the next import.

## 2026-10-13 — Second Unity run: compiles; setup menu works

- All assemblies compile in **Unity 6000.6.0f1**: the BUG REPORT menu ran, so the Editor assembly loaded.
- Configure Project added the World/Props/Hero/Anomaly/Intangible layers at slots 8–12 and set the Intangible collision rule. The second run was idempotent ("OK layer exists").
- It reported `ERROR no render pipeline asset assigned`. The check only read `GraphicsSettings.defaultRenderPipeline`; it now reads `GraphicsSettings.currentRenderPipeline` (quality-level asset first, Graphics default as fallback) and logs both. Root cause on the owner's machine still to be confirmed: a URP asset may genuinely be missing.

## 2026-10-13 — Third Unity run: URP OK, input asset not wired

- Configure Project shows **OK URP active: PC_RPAsset** (assigned per quality level). The "Graphics default empty" warning is harmless here.
- Play mode logged `InputReader has no InputActionAsset assigned`, so the builder saved the scene with a null reference.
- **Likely cause (unconfirmed):** the builder loaded the `.inputactions` asset *before* creating materials and folders, and those operations can trigger re-imports that invalidate already-loaded asset objects.
- **Fix:** create all assets and folders first, then load referenced assets. The builder now refuses to save the scene if any project component has an unassigned object reference, and lists them.
- Also fixed the compile-check stubs, which had `Scene` in `UnityEngine` instead of `UnityEngine.SceneManagement`.

## 2026-10-13 — Fourth Unity run: hero NullReferenceException

- `NullReferenceException` in `PlaceholderHero.BeginWalk`: the `ProgressMonitor` field was null.
- **Likely cause (unconfirmed):** scripts recompiled while Play mode was running (Unity's default "Recompile And Continue Playing"). That nulls non-serialized fields and does not re-run Awake. The other possibility is an earlier exception in Awake.
- **Fix:** `ProgressMonitor` is now created lazily, with its parameters clamped, instead of only in Awake.
- **Project policy:** in-play-mode recompilation is not supported, since static services can't survive it. Owners should set Preferences ▸ General ▸ Script Changes While Playing = *Recompile After Finished Playing*.

## 2026-10-13 — Fifth Unity run: wiring validation fired

- The builder correctly refused to save: `InputReader on 'ANOMALY': actions` was unassigned, even though the asset loaded fine. That **disproves** the re-import theory from the previous entry.
- **New likely cause:** `EditorSceneManager.NewScene(..., Single)` unloads unused assets, destroying the input-actions object that was loaded before it.
- **Fix:** open the scene first, then create and load every asset it references.
- `Wire()` now reads each assignment back and throws with diagnostics (destroyed?, field type, asset path, persistence) if Unity silently rejected it.
- Validation now also rejects renderers without a material.
- The build menu catches exceptions, so a failed build is reported as "scene not saved" instead of an unhandled exception.

## 2026-10-13 — Milestone 1 verified in Unity (owner)

- The owner reports all EditMode and PlayMode tests passing in Unity 6000.6.0f1 and pushed the generated project (151 files).
- **Audit of that commit:**
  - Every asset has a `.meta`, there are no orphan metas, and no GUIDs are duplicated.
  - Every GUID TestRoom references resolves: scripts, materials, input actions.
  - Both quality levels point at committed URP assets. Input System only, Linear colour space.
  - No Library/Temp/UserSettings committed.
- **Fixed in EditorBuildSettings:**
  - Removed the template's `SampleScene` entry (file not in repo). TestRoom is now build index 0.
  - Removed the dangling project-wide input actions reference to the template's uncommitted `InputSystem_Actions` asset.
- **Open:** remove the unused template packages (Visual Scripting, Collab Proxy); manual playtest checklist and gamepad pass not yet reported.

## 2026-10-13 — Milestone 1 merged; Milestone 2 implemented (Editor verification pending)

- PR #1 merged into `main` by the owner. Milestone 2 is on `feature/glitch-framework-collision-transfer`.
- **Core:** `ModifierStack<T>`, `OperationId`, `GlitchSpec`, `GlitchOperation`, `GlitchManager`, `IGlitchEffect`, `GlitchResult`. **87/87 core tests pass here** (38 new).
- **Runtime:**
  - `CollisionCapability`, `PhysicsOccupancy`, `GlitchDefinition` + `CollisionTransferDefinition`, `CollisionTransferEffect`, `GlitchController`, `CollisionModeVisual`, `GlitchPulsePresenter`.
  - Glitch outlines, reticle preview, debug overlay glitch panel.
  - `GameServices.Glitches`, disposed before the registry so objects are restored while still registered.
- **Builder:** firewall/shadow/holo panel collision setup, glitch definition asset (load-or-create, never overwritten), ghost and solidified materials.
- **Decisions:**
  - Dynamic sources are rejected.
  - Reverts and expiry wait while the source is occupied; forced endings don't.
  - Two-target glitches use the selection as SOURCE and the hovered entity as TARGET.
  - Q cycles glitches until the wheel UI lands in M5.
- **Verified here:**
  - Core tests: 87/87.
  - `Tools/CompileCheck`: 0 errors. The stubs gained only real Unity APIs: Sprite, CreateAssetMenu, OverlapBoxNonAlloc, BlendMode, RenderQueue, LogAssert, …
  - The committed collision matrix has Intangible colliding with nothing.
- **Not verified:** Unity compile, the 21 new PlayMode and 4 new EditMode tests, the transparent ghost material's look (URP properties set by script), and the playtest checklist.

## 2026-10-13 — Milestone 2 merged; Milestone 3 implemented (Editor verification pending)

- PR #2 merged (Milestone 2; owner reported all Unity tests passing). The owner pushed the generated assets to `main`; audit: no missing metas, every scene reference resolves.
- Unity's ProjectSettings changes in that commit: splash screen on (required by Unity Personal), fullscreen/resizable/run-in-background changed. Revisit in release prep (M8).
- **Milestone 3** on `feature/duplication-freeze`:
  - **Core:** operation products, `IsInvolved`, `CountActiveOn` (91/91 tests).
  - **Freeze:** capability, mover, definition, effect, visual.
  - **Duplication:** profile, capability, marker, definition, effect.
  - **Presentation:** `RendererOverrides`; per-glitch outline colours; copy beam.
  - **Builder:** crate prefab + profile, ball, lift, three glitch assets (load-or-create).
- **Decisions:**
  - Copies come only from prefab assets, via a profile ScriptableObject (a prefab can't reference itself).
  - Copies spawn under an inactive holder.
  - Freeze releases at rest by default; momentum lives in the operation.
  - Collision Transfer judges "dynamic" by the authored body state.
  - A despawn lease skips copies that are already unregistering, to avoid double destruction.
- **Moved files:** collision scripts now live in `Runtime/Glitches/Collision/`, moved together with their `.meta` files so the committed scene keeps its references.
- **Not verified:** Unity compile, the new PlayMode/EditMode tests, prefab generation via `PrefabUtility`, the visuals, and the playtest checklist.

## 2026-10-13 — Milestone 3 first Unity run: 2 failures, one regression

- **Failures:** `CollisionTransferTests.DynamicSource_IsRejected` and `FreezeTests.FrozenDynamicBody_StillCountsAsDynamicForCollisionTransfer`.
- **Cause:** both came from M3 changing `CollisionCapability.HasDynamicBody` to a snapshot taken at initialisation.
  - The snapshot missed bodies added later (DynamicSource, a regression of a passing M2 test).
  - `attachedRigidbody` is null for colliders on inactive objects, which is when the capability initialises.
- **Fix:** evaluate live again. Fall back to `GetComponentInParent<Rigidbody>(true)`, and treat a body that is kinematic only because of a freeze as dynamic (`FreezeCapability.KinematicBeforeFreeze`).
- **Lesson:** snapshotting state at init is fragile with deferred activation; prefer live queries plus explicit ownership information.

## 2026-10-13 — Milestone 3 merged; Milestone 4 implemented (Editor verification pending)

- PR #3 merged; the owner reported all Unity tests passing.
- **Milestone 4** on `feature/hero-ai-containment`:
  - **Navigation decision:** waypoint graph + live physics passability instead of NavMesh (ARCHITECTURE §6).
  - **Core:** `NavGraph`, `ContainmentModel`, spec containment weight, manager cooldown scale and containment inputs. **113/113 tests pass.**
  - **Hero:** `HeroController` FSM over `HeroNavGraph`, replacing `PlaceholderHero` (script and tests removed; collision tests migrated).
  - **Containment:** settings asset, director (cooldown scaling, oldest-first patches that skip vetoed reverts), HUD presenter, patch flash outlines.
- **Behaviour choice found while writing tests:** when blocked mid-route with no closer reachable node, the hero waits at the obstacle instead of walking back to its last node.
- **Not verified:** Unity compile, 14 new PlayMode tests and the updated EditMode tests, the routes in the real room, and the containment HUD/feel. Thresholds will likely need tuning by playtest.

## 2026-10-13 — Milestone 4 first Unity run: 2 test bugs

- **Failures:** `ContainmentDirectorTests.PatchDeployment_…` and `BlockedPatch_…` failed with "COOLDOWN 0.5s". The tests applied two transfers at the same `Time.time`, and the cooldown correctly rejected the second. That's a test bug, not a game bug.
- **Fix:** the test helper uses an explicit clock advancing 1 s per apply.
- **Audit:** the same pattern made M3's `FreezeTests.Unfreeze_RestoresTheKinematicFlagItFound` pass vacuously (its second freeze was silently rejected). It now asserts both freezes succeed.
- **Rule:** tests that apply several glitches must pass explicit, increasing `now` values and assert each apply succeeded.

## 2026-10-13 — Milestone 4 merged; Milestone 5 implemented (Editor verification pending)

- PR #4 merged (automated tests passing in Unity). The owner deferred manual playtests and asked for a plain-language guide: **PLAYTEST.md**.
- **Milestone 5** on `feature/vertical-slice-level`. The level "TRAINING LAB" has 3 connected objectives:
  1. firewall (Collision Transfer);
  2. gate needing two weighted plates with one crate (Duplication; alternative: transfer the gate);
  3. a piston corridor the hero refuses to enter while it moves (Freeze; alternative: make it intangible).
- **New code:**
  - **Core:** `ObjectiveTracker` (121/121 tests).
  - **Runtime:** plates, gate, hazard, kill plane, objective director/condition, HUD, menus, procedural audio, `GameServices.WorldChanged`, `[OptionalReference]`.
  - **Editor:** builder refactor + `SliceLevelBuilder`.
  - **Tests:** mechanics, scene validation, and a full playthrough test (3× speed, real player workflow, unloads the level afterwards).
- **Safety additions found while designing:**
  - The hero checks moving hazards every frame, not every 0.4 s.
  - Unfreezing or restoring a hazard is vetoed while anyone is in its path.
  - The gate latches open.
- **Avoided:** `FindObjectsByType(FindObjectsSortMode)` in tests. Its sort mode is being phased out in newer Unity 6 versions [Likely]; the entity registry is used instead.
- **Not verified:** Unity compile, all new Unity tests (especially the playthrough timing), look, sound, and feel.

## 2026-10-13 — Milestone 5 first Unity run: objective conditions lost

- **Symptoms:**
  - `SliceLevelSceneTests`: objectives 2 and 3 had null conditions.
  - The playthrough timed out at objective 2.
  - Console: "GameObject 'Zone_Exit' references runtime script in scene file. Fixing!"
- **Cause:** `HeroReachedZone` (a MonoBehaviour) was declared in `ObjectiveCondition.cs`. Unity maps components to scripts by file name, so it dropped the component when saving the scene. Compiles fine; fails silently.
- **Fix:** moved it to `HeroReachedZone.cs`.
- **Guard rails:**
  - EditMode `ScriptFileNameTests` checks every MonoBehaviour/ScriptableObject resolves to its own MonoScript.
  - `Tools/CompileCheck/check-unity-filenames.py` runs the same check without Unity (run before every push).

## 2026-10-13 — Milestone 5 merged; Milestone 6 pass 1 implemented (Editor verification pending)

- PR #5 merged; all Unity tests passing, **including the full automated playthrough** of TRAINING LAB.
- **Milestone 6, pass 1** on `feature/art-polish-pass`:
  - every visual feature is gated by `VisualSettings` (quick rollback while unreviewed);
  - post-processing volume + glitch camera bursts with a comfort toggle;
  - inspection labels and property panel;
  - hero body language and state icons;
  - anomaly shards, eye and trail;
  - spawn/freeze outline flashes, HUD panels, grid floor, SMAA.
- **New dependencies:** Unity.RenderPipelines.Core/Universal.Runtime (already in the project via URP) are now referenced by the Runtime, Editor and test assemblies.
- **Honest limitation:** this is a visual milestone and nobody has looked at it yet. Values are first guesses, and every feature can be turned off. **Profiling is not done** (needs the Unity Profiler on the reference PC).

## 2026-10-13 — Milestone 6 first Unity compile: name clash

- **Error:** `CS0104 'DebugOverlay' is ambiguous between BugReport.DebugTools.DebugOverlay and UnityEngine.Rendering.DebugOverlay`. M6 made the Editor assembly reference SRP Core, and TestRoomBuilder imports `UnityEngine.Rendering`.
- I had flagged this exact risk in M1 and dismissed it because the assembly didn't reference SRP Core then.
- **Fix:** renamed ours to `DiagnosticsOverlay`, moving the file and `.meta` together so scenes keep the reference. The compile-check stubs now include SRP's `DebugOverlay`/`DebugManager`/`DebugUI` so such clashes reproduce outside Unity.
- **Rule:** when an assembly gains a new reference, re-check project type names against the newly imported namespaces.

## 2026-10-13 — Milestone 6 pass 1 merged; pass 2: UI Toolkit game UI (Editor verification pending)

- PR #6 merged after the owner's Unity run: all EditMode and PlayMode tests passing.
- **Pass 2** on `feature/ui-toolkit-hud` replaces every gameplay IMGUI placeholder with UI Toolkit:
  - `UiLayer` base + `GameHud`, `GameMenus`, `InspectionOverlay` on one `UIDocument`;
  - the tree is built in C# (type-checked, testable by element name); all styling is in `BugReportUI.uss`;
  - new for players: a glitch dock showing every glitch and its cooldown, short feedback messages (rejection reasons such as OCCUPIED were previously only in the debug overlay), and a controls list on the pause screen.
- **Decision I'm reversing:** ARCHITECTURE said the designed UI would wait until the layout had been playtested. I did it first because the information layout is the same as before (plus the dock and messages), and future layout changes are now USS edits instead of IMGUI rect maths. If the playtest changes what the HUD should show, that is a code change either way.
- **Verified here:** Core tests 135/135; stub compile 0 errors / 0 warnings, now including UI Toolkit stubs; filename check OK. The stub compile caught a wrong enum name and a definite-assignment error before Unity did.
- **Not verified:** anything visual; USS parsing (Unity reports bad properties as import warnings); UI Toolkit's Input System backend for mouse and gamepad menu navigation (Unity 6 documents it as default when no EventSystem exists).

## 2026-10-13 — Milestone 6 pass 2, first Unity compile: cross-assembly access

- **Error:** `CS0122 'PresentationBuilder' is inaccessible` in the new EditMode UI asset tests. The builder was `internal` to `BugReport.Editor`; the tests are a separate assembly.
- **Why the offline check missed it:** `Tools/CompileCheck` compiled every file into **one** assembly, so `internal` was visible everywhere.
- **Fixes:**
  - `PresentationBuilder` is now public, with its helpers that take internal types kept internal (same pattern as `TestRoomBuilder`).
  - `compile-check.py` replaces the single project. It generates one project per `.asmdef` with the same references; `noEngineReferences` assemblies get no Unity stubs. Verified: with the old `internal` it reproduces the same 5 errors; with the fix it reports 0.
  - It also catches a missing `.asmdef` reference between our own assemblies. It cannot catch a missing Unity package reference, because the stubs are one assembly.
- **Warning UAC0005** (Unity analyzer) in `ScriptFileNameTests`: `AppDomain.GetAssemblies()` replaced with `TypeCache.GetTypesDerivedFrom<T>()`.

## 2026-10-14 — First human playthrough (macOS build)

- The owner built the game as a macOS app and finished TRAINING LAB: all three objectives, completion screen. This is the **first manual playthrough** of the slice. Before this, only the automated playthrough had finished it.
- **Bug seen in the screenshots:** spacing missing on the pause and complete screens (title touching the first button, key labels touching their descriptions, summary touching the buttons).
  - **Cause:** the USS reset `.br-root Label { margin: 0; padding: 0 }` has higher specificity (class + type) than single-class rules such as `.br-key` and `.br-title`, so it removed their spacing.
  - **Fix:** the reset is now a single class (`.br-label`) declared first, so later rules override it.
- **Readability:** the prompt under the glitch dock was hard to read over the light floor. It now sits on a dark pill.
- **Still open (needs the owner's feedback, not more guessing):**
  - The anomaly sits just under the reticle and covers part of the view.
  - The hero's "..." icon is small.
  - Whether the puzzles were understandable without the guide.

## 2026-10-14 — Milestone 6 pass 2 merged; Milestone 7 (onboarding) started

- PR #7 merged after the owner's Unity run (all tests passing) and a full playthrough in a macOS build.
- **Playtest finding:** the owner could only finish with the written guide, so the next milestone is teaching (owner's choice), before new levels or art.
- **Owner's direction:** full game (20+ levels), CC0 asset packs, all four level styles, plus an unspecified "something else".
- **Correction:** I had proposed sci-fi asset kits. Re-reading the GDD (Part 2.1: PROJECT HERO is a colourful fantasy adventure), the "normal layer" must be fantasy-adventure; sci-fi kits only suit the lab, industrial and security chapters. DESIGN_ROADMAP.md uses the GDD's direction.
- **Network:** this cloud environment blocks kenney.nl, quaternius.com, polyhaven.com and opengameart.org; assets must be downloaded by the owner or those domains allowed.
- **Implemented on `feature/onboarding-tutorial`:**
  - guide system (Core tracker + runtime director + HUD card + world marker + chime);
  - glitch locking; TRAINING LAB rewritten as a 14-step guided tutorial;
  - camera shoulder offset; bigger hero icon.
- **Design choice:** the tutorial is TRAINING LAB itself, not a new level in front of it. A separate tutorial would teach the same three puzzles twice.
- **Verified here:** Core 144/144; per-assembly compile 0/0; filename check OK. **Not verified:** whether the guide actually teaches, which needs a fresh player.
