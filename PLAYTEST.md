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

**New (Milestone 7): the game now teaches itself.** Try it first **without reading Parts 1–3 below**: follow the instructions above the bottom bar and the diamond marker. Write down every moment you felt lost or had to guess. Only read Parts 1–3 if you are stuck for more than a minute (and tell me where).

---

## Part 1: The wall
1. Watch the hero. It walks a bit, then **stops at the big blue wall**.
2. Look at the blue wall and press **E**. A cyan box appears around it.
3. Look at the **dark patch on the floor** and click the **left mouse button**.

**You should see:** a blue flash; a short message in the middle of the screen; the wall turns see-through; the hero starts walking again and goes through the wall.

**Tell me if:** nothing happens, the wall looks strange (for example pink or solid black), or the hero stays stuck.

## Part 2: The gate
1. The hero now stops at an **orange gate**. Two red squares are on the floor nearby. One has a crate on it.
2. Press **Q** until **DUPLICATION** is highlighted in the bar at the bottom of the screen.
3. Look at the crate and click the **left mouse button**.

**You should see:** a pink flash; a second crate appears on the empty red square; both squares turn green; the gate sinks into the floor; the hero walks on.

**Tell me if:** the copy appears in the wrong place, the squares don't turn green, or the gate doesn't open.

## Part 3: The piston
1. The hero stops before a narrow corridor. A **metal block slams back and forth** across it.
2. Press **Q** until **FREEZE** is highlighted in the bar at the bottom of the screen.
3. Wait until the block is **pulled back** (out of the corridor). Look at it and click the **left mouse button**.

**You should see:** the block turns pale blue and stops; the hero walks through; a "COMPLETE" screen appears.

**Tell me if:** the hero walks into the moving block, gets stuck, or the end screen doesn't appear.

## Part 4: Try to break it (optional, but very useful)
- Use your powers on lots of things, quickly. A red warning bar appears at the top. Keep going and the game starts **undoing your powers by itself** ("PATCH"). That is intended. Tell me if it feels too fast, too slow, or annoying.
- Press **Esc**, then **RESTART LEVEL**. Everything should be back to the start.
- Fly into walls and corners. The camera should never show the inside of a wall.

## Part 5: How it looks (after Milestone 6)
Just look and tell me what you like or dislike. Nothing here can break the game.
- **Tab:** small dark name tags appear over things you can affect, with coloured dots (blue = collision, pink = copy, light blue = freeze).
- **E on something:** a panel at the top right describes it.
- **Bottom bar:** every power you have, the current one highlighted, with a countdown while it recharges. Underneath, a hint for what to do next.
- **Mistakes:** if a power can't be used (for example "OCCUPIED"), a short red message says why, in the middle of the screen.
- **Esc:** the pause screen now has a list of all controls. You can use the mouse, or arrow keys + Enter.
- **The hero:** bobs while walking, shows "..." when waiting, "?" when confused, "!" when it sets off, and hops at the exit.
- **You (the glowing thing):** pointy shards spin around you, a dark "eye" looks at what you aim at, and you leave a short trail.
- **Every power:** a quick colour-split flash. If you dislike it, press **Esc** and turn **COLOUR-SPLIT EFFECTS** off.
- **Overall:** brighter highlights (bloom), a slightly darker screen edge, and a grid on the lab floor.

If something looks bad, you can switch it off yourself without me. Click `Assets/_Project/ScriptableObjects/Presentation/VS_Default`, untick the feature in the Inspector, and press Play again. Tell me which one you turned off and why.

## What to send me
For each part: **worked / didn't work**, plus anything weird you noticed. Screenshots help. If a red error appears at the bottom of Unity, click it and copy the text.
