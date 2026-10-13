# BUG//REPORT

A 3D systemic puzzle game in which you play the bug inside someone else's video game. Unity 6.6 · C# · URP · Input System.

| Document | Contents |
|---|---|
| [PROJECT_PLAN.md](PROJECT_PLAN.md) | Inspection findings, **one-time Unity setup**, milestones, vertical-slice definition, risks |
| [ARCHITECTURE.md](ARCHITECTURE.md) | Layering, assemblies, entity/capability model, glitch system design |
| [TASKS.md](TASKS.md) | Checklist per milestone + manual playtest checklist |
| [DEVLOG.md](DEVLOG.md) | What was done, what was verified, decisions |

## Quick start
1. Follow **PROJECT_PLAN.md §2** once to add Unity's generated `Packages/` and `ProjectSettings/`.
2. In Unity: **BUG REPORT ▸ Setup ▸ 1. Configure Project**, then **2. Build Test Room Scene**.
3. Open `Assets/_Project/Scenes/TestRoom.unity` → Play.
4. Window ▸ General ▸ Test Runner → run EditMode and PlayMode.

## Tests without Unity
```bash
Tools/CoreTests/run-core-tests.sh            # Core rules: compile at C# 9 / netstandard2.1 + NUnit
dotnet build Tools/CompileCheck              # type-check all C# against Unity API stubs (weaker than a Unity compile)
```
Requires the .NET 8 SDK.

## Controls (default)
WASD move · Mouse look · Space/Ctrl up/down · Shift boost · E select · RMB clear · Tab inspection · Esc pause · F1 debug overlay · F5 restart.
Gamepad: sticks · LB/RB down/up · A select · B clear · LT inspection · Start pause.
