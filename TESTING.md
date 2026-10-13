# Testing a branch before merging

You already have an empty Unity 6.6 project. That project is only a **donor**: we take Unity's generated settings from it and combine them with the code from this repo. Each step takes a few minutes. Do them in order.

## 0. One-time tools
1. Install **Git**: https://git-scm.com/downloads. On Windows, Git for Windows already includes Git LFS.
2. Install **Git LFS** if you're on macOS or Linux: https://git-lfs.com
3. Open a terminal (Windows: "Git Bash" or PowerShell) and run:
   ```bash
   git lfs install
   git config --global user.name "Rayan Jain"
   git config --global user.email "rayansjain@gmail.com"
   ```

## 1. Check the donor project uses URP
In your new project's folder, open `Packages/manifest.json` in any text editor and search for `com.unity.render-pipelines.universal`.
- **Found:** continue.
- **Not found:** you picked a non-URP template. In Unity Hub create a new project again and choose the **Universal 3D** template.

To find the folder: Unity Hub → Projects → the `⋯` next to your project → *Show in Explorer / Finder*.

**Close Unity** before continuing.

## 2. Get the branch
```bash
cd <a folder where you keep projects>
git clone -b foundation/vertical-slice-setup https://github.com/rayanjainn/glitch.git BugReport
```
This creates a `BugReport` folder. GitHub may ask you to sign in.

## 3. Copy Unity's generated settings into it
From your **donor** project, copy into `BugReport/`:

| Copy this from the donor | To here |
|---|---|
| `Packages/` (whole folder) | `BugReport/Packages/` |
| `ProjectSettings/` (whole folder) | `BugReport/ProjectSettings/` |
| `Assets/Settings/` (whole folder) **and** `Assets/Settings.meta` | `BugReport/Assets/Settings/` and `BugReport/Assets/Settings.meta` |

Don't copy anything else from the donor (no `Library`, no `Scenes`, no `TutorialInfo`).

## 4. Open it in Unity
1. Unity Hub → Projects → **Add** ▾ → *Add project from disk* → select the `BugReport` folder → open it with your 6.6 editor.
2. The first import takes a few minutes.
3. If Unity asks to **enable the new Input System backends and restart**, click **Yes**.
4. If Unity offers **Safe Mode**, there are compile errors. Enter Safe Mode, open the Console (Window ▸ General ▸ Console), and send me every red error line. Stop here.

## 5. Set up and build the test room
1. Top menu **BUG REPORT ▸ Setup ▸ 1. Configure Project** → OK. The Console prints a report. Lines starting with `ERROR` need fixing, so send them to me.
2. **BUG REPORT ▸ Setup ▸ 2. Build Test Room Scene.**
3. In the Project window open `Assets/_Project/Scenes/TestRoom.unity`.

## 6. Play it
Press **Play**, then click inside the Game view to capture the mouse. Work through the *Manual playtest checklist* at the bottom of `TASKS.md` (10 short checks). Press Esc to free the mouse.

## 7. Run the automated tests
Window ▸ General ▸ **Test Runner**:
- **EditMode** tab → *Run All*
- **PlayMode** tab → *Run All*

Green ticks mean pass. For any red test, click it and copy the message at the bottom.

## 8. Report back to me
- Your exact Unity version: Help ▸ About Unity, e.g. `6000.6.0f1`.
- Console errors, if any.
- Test Runner results: number passed and failed, plus the messages for failures.
- Anything from the playtest checklist that looked or felt wrong.

## 9. Save Unity's generated files to the branch
Once it opens without errors, Unity has created `.meta` files and settings that **must** be committed. Without them, scene references break on another machine.
```bash
cd BugReport
git add -A
git commit -m "Add Unity 6.6 project settings, packages and generated assets"
git push
```

## 10. Merge
When steps 4–7 are clean, tell me. I'll open a pull request from `foundation/vertical-slice-setup` into `main` for you to merge on GitHub.

---

## Optional: run the tests from the command line
Close the project in Unity first. Only one Unity instance can have it open.

Windows (PowerShell, from the `BugReport` folder):
```powershell
powershell -ExecutionPolicy Bypass -File Tools/UnityTests/run-unity-tests.ps1 -UnityPath "C:\Program Files\Unity\Hub\Editor\<version>\Editor\Unity.exe"
```
macOS:
```bash
Tools/UnityTests/run-unity-tests.sh "/Applications/Unity/Hub/Editor/<version>/Unity.app/Contents/MacOS/Unity"
```
Results are written to `TestResults/EditMode.xml` and `TestResults/PlayMode.xml`, with logs alongside.
