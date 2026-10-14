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
Press **NEW GAME**. Each level unlocks the next one.

1. **TRAINING LAB**: the game teaches you step by step (text above the bottom bar, a blue diamond on the right object). Try it **without** reading anything else.
2. **BRIDGE OUT**: the bridge is only a hologram.
3. **TWO DOORS, ONE SHADOW**: two walls, one shadow. (Hint if stuck: [R] gives the shadow back.)
4. **TWO BIRDS**: one power use should fix two problems.
5. **THREE PLATES**: three plates, one crate.
6. **COPY THE COPY**: copies are real crates too.
7. **STOP THE PLATFORM**: the hero won't step onto something moving.
8. **TWO PISTONS**: one at a time.
9. **THE BUILD**: everything together.

Stuck for more than two minutes? That's useful information. Write down which level and what you tried, then use **Esc ▸ MAIN MENU ▸ LEVEL SELECT** to skip ahead (only levels you've unlocked).

## Also try
- **SETTINGS** (main menu and pause): volume, mouse sensitivity, invert look, colour-split effects, and the key list. Click a key, press a new one (Esc cancels). Quit and start again: your settings should still be there.
- **CONTINUE** on the main menu should take you to the first level you haven't finished.
- Use powers fast and a lot: a red bar fills at the top, and the game starts undoing your powers itself ("PATCH"). That is intended.

## What to send me
For each level: **finished / stuck / broken**, roughly how long it took, and anything weird. Screenshots help. If a red error appears at the bottom of Unity, click it and copy the text.

**Most useful of all:** watch someone who has never seen the game play TRAINING LAB and level 2 without help. Note every moment they stop or ask something.
