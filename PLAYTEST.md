# How to playtest BUG//REPORT (simple version)

You don't need to know how the game works inside. Just play, and write down anything that looks wrong, feels confusing, or shows a red error.

## Before you start (once per update)
1. In Unity: **BUG REPORT ▸ Setup ▸ 5. Build ALL (menu + every level)**. Wait until the Console says it built everything.
2. Open `Assets/_Project/Scenes/MainMenu` and press **Play**. Or make a real build (File ▸ Build Profiles ▸ Build): it starts at the main menu.

## Controls (you can change the keys in SETTINGS)
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

You are the small glowing thing. The knight is "the hero": it walks to the exit by itself. Your job is to clear its way.

## What to play
Press **NEW GAME**. Each level unlocks the next one. There are 20 levels in 7 chapters; the level select shows your stars (1 = finished, 2 = finished using few powers, 3 = also barely set off the alarm).

**Chapter 1: Hello, world** (the wall powers)
1. **TRAINING LAB**: the game teaches you step by step. Try it **without** reading anything else.
2. **BRIDGE OUT**: the bridge is only a hologram.
3. **TWO DOORS, ONE SHADOW**: two walls, one shadow. (If stuck: [R] gives the shadow back.)
4. **TWO BIRDS**: one power use should fix two problems.

**Chapter 2: Duplicate objective**
5. **THREE PLATES**: three plates, one crate.
6. **COPY THE COPY**: copies are real crates too.

**Chapter 3: Time does not apply**
7. **STOP THE PLATFORM**: the hero won't step onto something moving.
8. **TWO PISTONS**: one at a time.
9. **THE BUILD**: everything so far.

**Chapter 4: Dead weight** (new power: MASS SWAP, two objects trade weight)
10. **HEAVY LIFTING**: the plate needs something heavy. There is something heavy in the room.
11. **TWIN SCALES**: two plates. Combine copying with swapping.
12. **WEIGH STATION**: a heavy gate and a hologram bridge, two different problems.

**Chapter 5: Night shift** (new power: AI OVERRIDE, send a guard to look at something else)
13. **NIGHT WATCH**: a skeleton guard stands in the doorway. The hero waits until it is safe.
14. **DOUBLE SHIFT**: two guards, one decoy. A lure only lasts 14 seconds, so timing matters.
15. **FROZEN STIFF**: a guard and a piston, different answers.

**Chapter 6: False floors** (new power: DESYNC, move what is solid away from what you see)
16. **FALSE WALL**: a wall the hero cannot pass, but its solid part can be lifted out of the way.
17. **FALSE FLOOR**: a floor plate whose solid part can be slid across a pit.
18. **SEARCHLIGHT**: a drone sweeps the room; anything in its light raises the alarm.
19. **PATCH DAY**: everything in chapter 6 together.

**Chapter 7**
20. **REPORT CLOSED**: every power, one long hall.

Stuck for more than two minutes? That's useful information. Write down which level and what you tried, then use **Esc ▸ MAIN MENU ▸ LEVEL SELECT** to skip ahead (only levels you've unlocked).

## Also try
- **Music**: there is a score that changes per chapter and gets tense when the alarm rises. Try the MUSIC VOLUME slider in SETTINGS.
- **Hints**: the game does not tell you the solution up front. If you are stuck for about 40 seconds the objective text gets a hint, and after much longer a marker appears on the key object. Tell me if that was too early or too late.
- **Story**: the first time you play a level a few lines are typed out at the bottom; when you finish, the card shows your stars, time and a closing line.
- **SETTINGS** (main menu and pause): volume, mouse sensitivity, invert look, colour-split effects, and the key list. Click a key, press a new one (Esc cancels). Quit and start again: your settings should still be there.
- **CONTINUE** on the main menu should take you to the first level you haven't finished.
- Use powers fast and a lot: a red bar fills at the top, and the game starts undoing your powers itself ("PATCH"). That is intended.

## What to send me
For each level: **finished / stuck / broken**, roughly how long it took, and anything weird. Screenshots help. If a red error appears at the bottom of Unity, click it and copy the text.

**Most useful of all:** watch someone who has never seen the game play TRAINING LAB and level 2 without help. Note every moment they stop or ask something.
