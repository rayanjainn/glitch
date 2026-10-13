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
