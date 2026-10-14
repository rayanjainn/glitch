using System.IO;
using BugReport.Presentation;
using BugReport.Progress;
using BugReport.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using static BugReport.EditorTools.Setup.TestRoomBuilder;

namespace BugReport.EditorTools.Setup
{
    /// <summary>
    /// Builds the title screen scene: a small lit stage with the hero idling among a few props, a
    /// slowly orbiting camera, the post-processing look, and the MainMenu UI. It becomes the first
    /// scene in Build Settings, so a build starts here.
    /// </summary>
    public static class MenuBuilder
    {
        [MenuItem(ProjectSetup.MenuRoot + "4. Build Main Menu", priority = 4)]
        public static void BuildMenu()
        {
            if (!GameLayers.AreConfigured(out var missing))
            {
                EditorUtility.DisplayDialog("BUG REPORT", "Missing layers: " + missing + ".\nRun 'Configure Project' first.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string error;
            bool built;
            try
            {
                built = Build(out error);
            }
            catch (System.Exception e)
            {
                built = false;
                error = e.Message;
                Debug.LogException(e);
            }
            if (built) Debug.Log("[BUG REPORT] Built " + LevelCatalogBuilder.MainMenuPath + " (first scene in Build Settings).");
            else Debug.LogError("[BUG REPORT] Main menu build failed (scene not saved): " + error);
        }

        public static bool Build(out string error)
        {
            error = null;
            EnsureFolder(LevelCatalogBuilder.ScenesFolder);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var mats = new MaterialSet();
            if (!mats.Create(out error)) return false;
            var presentation = PresentationBuilder.LoadOrCreateAssets(mats);
            var catalog = LevelCatalogBuilder.LoadOrCreate();
            int world = GameLayers.Index(GameLayers.World);

            // ---- Light and stage
            var sun = new GameObject("Sun", typeof(Light)).GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.93f, 0.82f);
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.45f, 0.55f, 0.75f);
            RenderSettings.ambientEquatorColor = new Color(0.35f, 0.33f, 0.32f);
            RenderSettings.ambientGroundColor = new Color(0.15f, 0.13f, 0.12f);
            RenderSettings.sun = sun;
            CreatePointLight("Light_Cyan", new Vector3(2.5f, 2.5f, -1.5f), new Color(0.35f, 0.9f, 1f), 3f, 8f);
            CreatePointLight("Light_Warm", new Vector3(-2.5f, 2.2f, 1.5f), new Color(1f, 0.7f, 0.45f), 2.5f, 8f);

            var stage = new GameObject("--- STAGE ---").transform;
            var floor = Block("Floor", stage, new Vector3(0f, -0.1f, 0f), new Vector3(14f, 0.2f, 14f), mats.Floor, world);
            floor.GetComponent<MeshRenderer>().sharedMaterial = PresentationBuilder.GridFloor(14f, 14f);

            // ---- The hero, idling (placeholder capsule if the CC0 art isn't fetched)
            var hero = new GameObject("HeroIdle");
            hero.transform.SetParent(stage, false);
            hero.transform.position = new Vector3(0f, 1f, 0f);
            hero.transform.rotation = Quaternion.Euler(0f, 160f, 0f);
            var cc = hero.AddComponent<CharacterController>();
            cc.height = 2f;
            if (ArtBuilder.BuildHeroModel(hero.transform, cc, 1.85f, world) == null)
            {
                var body = Primitive(PrimitiveType.Capsule, "Body", hero.transform, Vector3.zero, Vector3.one, mats.Hero, world, keepCollider: false);
                body.transform.localPosition = Vector3.zero;
            }
            Object.DestroyImmediate(cc);

            if (ArtBuilder.DungeonAvailable)
            {
                var material = ArtBuilder.AtlasMaterial("M_Dungeon", ArtBuilder.DungeonTexturePath);
                ArtBuilder.Decor("barrel_large", stage, new Vector3(-1.8f, 0f, 1.2f), 0f, 1.1f, material, world);
                ArtBuilder.Decor("crates_stacked", stage, new Vector3(2.0f, 0f, 1.6f), 25f, 1.3f, material, world);
                ArtBuilder.Decor("chest_gold", stage, new Vector3(1.2f, 0f, -1.0f), 200f, 0.7f, material, world);
                ArtBuilder.Decor("box_large", stage, new Vector3(-2.4f, 0f, -0.6f), 10f, 0.9f, material, world);
            }

            // ---- Camera + look
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            var cam = camGo.GetComponent<Camera>();
            cam.fieldOfView = 50f;
            cam.clearFlags = CameraClearFlags.Skybox;
            PresentationBuilder.ConfigureCamera(cam);
            camGo.AddComponent<MenuCameraOrbit>().Configure(new Vector3(0.6f, 1.0f, 0f), 5.5f, 1.4f);
            var volume = new GameObject("PostFX").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = presentation.Profile;

            // ---- UI + settings
            var ui = new GameObject("UI");
            ui.AddComponent<UIDocument>().panelSettings = presentation.Panel;
            var menu = ui.AddComponent<MainMenu>();
            Wire(menu, "styleSheet", presentation.Style);
            Wire(menu, "catalog", catalog);
            var font = AssetDatabase.LoadAssetAtPath<Font>(ArtBuilder.DisplayFontPath);
            if (font != null) Wire(menu, "displayFont", font);
            ui.AddComponent<SettingsApplier>();

            var missingRefs = FindUnassignedReferences(scene);
            if (missingRefs.Count > 0)
            {
                error = "Scene NOT saved; unassigned references:\n  " + string.Join("\n  ", missingRefs);
                return false;
            }
            if (!EditorSceneManager.SaveScene(scene, LevelCatalogBuilder.MainMenuPath))
            {
                error = "Could not save " + LevelCatalogBuilder.MainMenuPath;
                return false;
            }
            AddToBuildSettings(LevelCatalogBuilder.MainMenuPath);
            LevelCatalogBuilder.SyncBuildSettings();
            return true;
        }
    }
}
