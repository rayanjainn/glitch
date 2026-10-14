# BUG//REPORT

A 3D systemic puzzle game in which you play the bug inside someone else's video game. Unity 6.6 · C# · URP · Input System.

| Document | Contents |
|---|---|
| [PROJECT_PLAN.md](PROJECT_PLAN.md) | Inspection findings, **one-time Unity setup**, milestones, vertical-slice definition, risks |
| [ARCHITECTURE.md](ARCHITECTURE.md) | Layering, assemblies, entity/capability model, glitch system design |
| [TASKS.md](TASKS.md) | Checklist per milestone + manual playtest checklist |
| [PLAYTEST.md](PLAYTEST.md) | **Play the level and check it, in plain language** |
| [TESTING.md](TESTING.md) | **Step-by-step: test a branch in Unity before merging** |
| [DEVLOG.md](DEVLOG.md) | What was done, what was verified, decisions |

## Quick start
Full walkthrough: **[TESTING.md](TESTING.md)**.
1. Follow **TESTING.md §1–4** once to add Unity's generated `Packages/` and `ProjectSettings/`.
2. In Unity: **BUG REPORT ▸ Setup ▸ 1. Configure Project**, then **3. Build Vertical Slice Level** (and **2. Build Test Room Scene** for the sandbox).
3. Open `Assets/_Project/Scenes/Level_01_TrainingLab.unity` → Play. See PLAYTEST.md.
4. Window ▸ General ▸ Test Runner → run EditMode and PlayMode.

## Tests without Unity
```bash
Tools/CoreTests/run-core-tests.sh            # Core rules: compile at C# 9 / netstandard2.1 + NUnit
python3 Tools/CompileCheck/compile-check.py  # type-check each Unity assembly against API stubs (weaker than a Unity compile)
python3 Tools/CompileCheck/check-unity-filenames.py  # MonoBehaviours/ScriptableObjects live in <ClassName>.cs
```
Requires the .NET 8 SDK. Headless Unity test runs: `Tools/UnityTests/run-unity-tests.ps1` (Windows) or `.sh` (macOS/Linux).

Binary assets use **Git LFS**: run `git lfs install` once before committing art or audio.

Third-party CC0 art (see CREDITS.md) is fetched, not hand-copied: `bash Tools/Assets/fetch-cc0-assets.sh` downloads the pinned files from the authors' GitHub repos and verifies their checksums.

## Controls (default)
WASD move · Mouse look · Space/Ctrl up/down · Shift boost · E select · RMB clear · Tab inspection · Esc pause · F1 debug overlay · F5 restart.
Gamepad: sticks · LB/RB down/up · A select · B clear · LT inspection · Start pause.
