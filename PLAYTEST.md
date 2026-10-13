# How to playtest BUG//REPORT (simple version)

You don't need to know how the game works inside. Just play, and write down anything that looks wrong, feels confusing, or shows a red error.

**Before you start (once):**
1. In Unity, click the menu **BUG REPORT ▸ Setup ▸ 3. Build Vertical Slice Level**. If it asks to rebuild, click **Rebuild**.
2. In the Project window, open `Assets/_Project/Scenes/Level_01_TrainingLab`.
3. Press the **Play** button at the top. Click once inside the game picture so it takes your mouse.

**Controls:**
| Key | What it does |
|---|---|
| W A S D + mouse | fly around |
| Space / Ctrl | fly up / down |
| Shift | fly faster |
| E | pick the thing you are looking at |
| Left mouse button | use your power on what you are looking at |
| R | undo your last power |
| Q | switch power |
| Tab | x-ray view (shows what you can affect) |
| Esc | pause menu |

You are the small glowing thing. The yellow capsule is "the hero". You can't control the hero; it walks to the green exit by itself. Your job is to clear its way.

---

## Part 1: The wall
1. Watch the hero. It walks a bit, then **stops at the big blue wall**.
2. Look at the blue wall and press **E**. A cyan box appears around it.
3. Look at the **dark patch on the floor** and click the **left mouse button**.

**You should see:** a blue flash; the wall turns see-through; the hero starts walking again and goes through the wall.

**Tell me if:** nothing happens, the wall looks strange (for example pink or solid black), or the hero stays stuck.

## Part 2: The gate
1. The hero now stops at an **orange gate**. Two red squares are on the floor nearby. One has a crate on it.
2. Press **Q** until the bottom of the screen says **DUPLICATION**.
3. Look at the crate and click the **left mouse button**.

**You should see:** a pink flash; a second crate appears on the empty red square; both squares turn green; the gate sinks into the floor; the hero walks on.

**Tell me if:** the copy appears in the wrong place, the squares don't turn green, or the gate doesn't open.

## Part 3: The piston
1. The hero stops before a narrow corridor. A **metal block slams back and forth** across it.
2. Press **Q** until the screen says **FREEZE**.
3. Wait until the block is **pulled back** (out of the corridor). Look at it and click the **left mouse button**.

**You should see:** the block turns pale blue and stops; the hero walks through; a "COMPLETE" screen appears.

**Tell me if:** the hero walks into the moving block, gets stuck, or the end screen doesn't appear.

## Part 4: Try to break it (optional, but very useful)
- Use your powers on lots of things, quickly. A red warning bar appears at the top. Keep going and the game starts **undoing your powers by itself** ("PATCH"). That is intended. Tell me if it feels too fast, too slow, or annoying.
- Press **Esc**, then **RESTART LEVEL**. Everything should be back to the start.
- Fly into walls and corners. The camera should never show the inside of a wall.

## What to send me
For each part: **worked / didn't work**, plus anything weird you noticed. Screenshots help. If a red error appears at the bottom of Unity, click it and copy the text.
