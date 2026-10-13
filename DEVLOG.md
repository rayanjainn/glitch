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
