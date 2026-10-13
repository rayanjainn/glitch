# BUG//REPORT

A 3D systemic puzzle game in which you play the bug inside someone else's video game. Unity 6.6 · C# · URP · Input System.

| Document | Contents |
|---|---|
| [PROJECT_PLAN.md](PROJECT_PLAN.md) | Inspection findings, **one-time Unity setup**, milestones, vertical-slice definition, risks |
| [ARCHITECTURE.md](ARCHITECTURE.md) | Layering, assemblies, entity/capability model, glitch system design |
| [TASKS.md](TASKS.md) | Checklist per milestone + manual playtest checklist |
| [TESTING.md](TESTING.md) | **Step-by-step: test a branch in Unity before merging** |
| [DEVLOG.md](DEVLOG.md) | What was done, what was verified, decisions |

## Quick start
Full walkthrough: **[TESTING.md](TESTING.md)**.
1. Follow **TESTING.md §1–4** once to add Unity's generated `Packages/` and `ProjectSettings/`.
2. In Unity: **BUG REPORT ▸ Setup ▸ 1. Configure Project**, then **2. Build Test Room Scene**.
3. Open `Assets/_Project/Scenes/TestRoom.unity` → Play.
4. Window ▸ General ▸ Test Runner → run EditMode and PlayMode.

## Tests without Unity
```bash
Tools/CoreTests/run-core-tests.sh            # Core rules: compile at C# 9 / netstandard2.1 + NUnit
dotnet build Tools/CompileCheck              # type-check all C# against Unity API stubs (weaker than a Unity compile)
```
Requires the .NET 8 SDK. Headless Unity test runs: `Tools/UnityTests/run-unity-tests.ps1` (Windows) or `.sh` (macOS/Linux).

Binary assets use **Git LFS**: run `git lfs install` once before committing art or audio.

## Controls (default)
WASD move · Mouse look · Space/Ctrl up/down · Shift boost · E select · RMB clear · Tab inspection · Esc pause · F1 debug overlay · F5 restart.
Gamepad: sticks · LB/RB down/up · A select · B clear · LT inspection · Start pause.
