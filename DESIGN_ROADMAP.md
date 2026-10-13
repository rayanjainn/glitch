# BUG//REPORT — Design roadmap (full game)

Status: **proposal for the owner to react to.** Nothing here is built yet except where marked.
It follows the GDD (Parts 1–7), stretched from 8 levels to a 20+ level game, as the owner asked.

## 1. What the first playtest told us
- The owner finished TRAINING LAB in a real macOS build, **but only with the written guide.** The game does not teach its own rules yet.
- Consequence: teaching comes before more levels. Every new glitch must arrive with a short level that teaches it **by doing**, not by reading.
- Implemented on `feature/onboarding-tutorial`: a step-by-step guide that reacts to what the player does, a marker on the object it talks about, and glitches unlocking one at a time (TRAINING LAB is now the guided tutorial).

## 2. Teaching rules (apply to every new mechanic)
1. **One new idea per room.** The first room for a glitch has one solution and nothing else to touch.
2. **Show, then let them do it.** The guide names the object, a marker sits on it, and the instruction updates the moment the player acts.
3. **Short words, real keys.** Max ~12 words per instruction, keys highlighted (`[E]`, `[LMB]`).
4. **Never block progress.** Objectives advance on their own; a player who solves it differently skips the guide.
5. **Second room: same glitch, no guide, one twist.** Third room: combine with an older glitch.
6. **Failure is explained.** Every rejection says why (already in: OCCUPIED, NOT FREEZABLE, …).
7. **Measure it.** After each chapter, one person who has never seen the game plays it while we watch. "Needed help" counts as a bug.

## 3. Game structure: 7 chapters + finale (about 26 levels)
Each chapter introduces one glitch (GDD 4.7) in its own location, then combines it with earlier ones.

| Ch | Name (GDD level) | Location | New glitch | Levels (one-line puzzle idea) |
|---|---|---|---|---|
| 1 | Hello, World | Tutorial meadow of PROJECT HERO | Inspect + Collision Transfer | 1 **First Steps**: wall → shadow (guided). 2 **Bridge Out**: a hologram bridge is not solid; take solidity from a wall the hero doesn't need. 3 **Two Doors, One Shadow**: transfer, wait, undo, transfer again (teaches `[R]` and timing). 4 **Where Did It Go?**: solidity must land somewhere; give it to the wrong thing and a ledge collapses |
| 2 | Duplicate Objective | Small laboratory | Duplication | 5 **Twin Plates** (current gate). 6 **One Key, Two Locks**: copy the key; the hero gets confused about which to take, and the game notices. 7 **Stairway**: copies of crates become steps. 8 **Copy Protection**: duplicates get patched after a while, so timing matters |
| 3 | Time Does Not Apply | Moving industrial facility | Freeze | 9 **Piston** (current). 10 **Conveyor**: freeze a moving platform where it bridges a gap. 11 **Falling Floor**: freeze crumbling tiles under the hero. 12 **Pendulum**: freeze a swinging hammer at the right angle as a ramp |
| 4 | Mass Confusion | Physics testing chamber | Property Swap (amber) | 13 **Heavy Feather**: swap mass so a light box presses a plate. 14 **Rubber Crate**: swap bounciness to bounce a crate onto a ledge. 15 **Fast Lane**: swap speed between a slow lift and a fast fan. 16 **Size Matters**: swap size (within limits) to fit a crate through a hatch |
| 5 | The Guard Is Wrong | Security facility | AI Override (violet) + guards | 17 **Distraction**: make a guard investigate a fake noise. 18 **Groundhog Patrol**: force a patrol to repeat while the hero passes. 19 **Two Heroes**: duplicate the hero; the guard chases the copy. 20 **Friendly Fire**: redirect a security robot into the turret's path |
| 6 | Patch Day | Facility under emergency recovery | Desync + containment as a tool | 21 **Patch Notes**: let the system's patch undo something you can't reach. 22 **Ghost Platform**: the platform you see isn't where it is (Desync). 23 **Hotfix**: drones scan for anomalies; glitch while unseen. 24 **Rollback**: containment restores an old state, so prepare the world for it |
| 7 | The Build | Corrupted remix of the tutorial | Combinations | 25 **Déjà Vu**: level 1 with every rule bent. 26 **Everything Everywhere**: one room, all glitches, several solutions |
| — | Report Closed | The game's central system | Finale | The containment core; endings from GDD Part 6 (escape, admin, restore, corrupt) |

Story beats (short, between chapters): system log messages and the hero's reactions. The game "talking back" (GDD pillar C) is the story.

**Open question for the owner:** you also picked "Something else" for how levels should feel. What did you have in mind?

## 4. Systems still to build (in chapter order)
- Main menu, level select, saved progress (which levels are done). Settings screen (volume, mouse sensitivity, invert Y, colour-split), key rebinding.
- Pickups the hero carries (keys), doors with locks. Hero reactions to duplicated objectives (ch 2).
- Moving platforms / crumbling tiles / pendulums as freezable machinery (ch 3).
- Property Swap with an explicit property list: mass, speed, bounciness, size, material (ch 4).
- Guards and robots with a small state machine; AI Override (ch 5).
- Desync (controlled visual/collision offset), drones and sensors that "sight" the anomaly, stage-4 route shutdowns (ch 6).
- Endings and their conditions (finale).

## 5. Assets
### Art direction (GDD Part 7)
- **Normal layer:** stylized, colourful fantasy adventure (PROJECT HERO). Warm neutrals, blue skies, greens. Strong silhouettes, consistent scale.
- **System layer:** cyan/white wireframes, debug labels, glitch colours (blue collision, magenta duplication, pale-cyan freeze, amber swap, violet AI, red/white containment). This layer is mostly ours (shaders, UI, VFX) and already started.
- Early chapters (lab, industrial, security) can use clean sci-fi kits; the hero and the "game world" should be fantasy-adventure.

### Sources: CC0 only (free for commercial use, no credit required)
Always confirm the licence on the download page; keep each pack's licence file next to it.

| Need | Pack (author, site) | Use |
|---|---|---|
| The hero (animated) | **KayKit Adventurers** (Kay Lousberg, kaylousberg.itch.io) or **Ultimate Animated Character Pack** (Quaternius, quaternius.com) | Replaces the yellow capsule; walk / idle / cheer animations |
| Fantasy world | **Kenney Nature Kit**, **Kenney Castle Kit**, **Kenney Fantasy Town Kit** (kenney.nl); **Quaternius Stylized Nature / Medieval Village** packs | Chapter 1 meadow, the "real game" look |
| Lab / industrial / security | **Kenney Space Kit**, **Quaternius Modular Sci-Fi** pack | Chapters 2, 3, 5, 6 |
| Dungeon / props | **KayKit Dungeon Remastered** (itch.io) | Crates, doors, chests, keys |
| Textures, skies | **Poly Haven** (polyhaven.com): HDRI skies, a few ground textures | Lighting and sky |
| Sound effects | **Kenney Interface Sounds, Sci-Fi Sounds, Impact Sounds, Digital Audio** (kenney.nl) | Replace the synthesized placeholders |
| UI font | **Rajdhani** or **Exo 2** + **JetBrains Mono** (Google Fonts, SIL Open Font License: free, ships with the game) | UI and the system layer |
| Music | Not chosen yet. CC0 music is rarer; options are opengameart.org (filter CC0) or commissioning. **Avoid CC-BY** unless we add a credits screen |

### How assets enter the project
1. The owner downloads the zips (this cloud environment blocks those sites; or allow them in the environment's Network settings and I fetch them).
2. Unzip into `Assets/ThirdParty/<PackName>/` with the pack's `License.txt`. Binary files go through Git LFS (already configured).
3. I write the import settings and swap placeholders for real models **through one mapping asset** (an "art set": which model/material each entity uses). If a model looks wrong, one line changes back.
4. `CREDITS.md` lists every pack, author, licence and URL, even for CC0.

## 6. Milestone order (replaces PROJECT_PLAN §3 rows 7–8)
| # | Name | Exit criteria |
|---|---|---|
| 7 | **Onboarding** (in progress) | A new player finishes TRAINING LAB without outside help (observed) |
| 8 | Art pipeline + Chapter 1 look | Art-set mapping; animated hero; meadow kit; real SFX; one level fully re-skinned and still passing all tests |
| 9 | Front end | Main menu, level select, saved progress, settings, rebinding |
| 10 | Chapters 1–3 complete (12 levels) | Each chapter playtested by someone new; 60 FPS measured |
| 11 | Property Swap + Chapter 4 | — |
| 12 | Guards, AI Override + Chapter 5 | — |
| 13 | Desync, drones + Chapter 6 | — |
| 14 | Chapter 7, finale, endings | — |
| 15 | Release prep | Builds from a script, store page assets, external playtest |

**Realistic expectation:** a full 26-level game is many months of work, and the art is the long pole. A **3-chapter demo (milestones 7–10)** is the right first finish line: it can be shown to people and tells us whether the full game is worth it.
