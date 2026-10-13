using System;
using System.IO;
using BugReport.Anomaly;
using BugReport.Hero;
using BugReport.Level;
using BugReport.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static BugReport.EditorTools.Setup.TestRoomBuilder;
using Object = UnityEngine.Object;

namespace BugReport.EditorTools.Setup
{
    /// <summary>
    /// Milestone 6 presentation layer, applied to any built level: post-processing volume, glitch
    /// camera bursts, inspection labels, hero body language, anomaly eye/shards/trail, camera AA.
    /// Every feature can be switched off in the VisualSettings asset without rebuilding.
    /// </summary>
    internal static class PresentationBuilder
    {
        public const string VisualSettingsPath = "Assets/_Project/ScriptableObjects/Presentation/VS_Default.asset";
        public const string VolumeProfilePath = "Assets/_Project/Art/Rendering/VP_BugReport.asset";
        public const string TetraMeshPath = "Assets/_Project/Art/Meshes/Generated/ME_Tetra.asset";
        public const string GridTexturePath = "Assets/_Project/Art/Textures/Generated/T_Grid.png";

        internal struct Assets
        {
            public VisualSettings Settings;
            public VolumeProfile Profile;
            public Mesh Tetra;
            public Material Pupil;
        }

        /// <summary>Create/load presentation assets. Call after NewScene (see TestRoomBuilder.Build).</summary>
        public static Assets LoadOrCreateAssets(MaterialSet mats)
        {
            var assets = new Assets
            {
                Settings = LoadOrCreate<VisualSettings>(VisualSettingsPath, _ => { }),
                Profile = LoadOrCreateProfile(),
                Tetra = LoadOrCreateTetra(),
            };
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var pupil = new Material(lit) { name = "M_AnomalyPupil" };
            pupil.SetColor("_BaseColor", new Color(0.02f, 0.02f, 0.04f));
            pupil.SetFloat("_Smoothness", 0.95f);
            assets.Pupil = MaterialSet.Save(pupil, "M_AnomalyPupil");
            return assets;
        }

        /// <summary>Adds everything presentational to a level whose gameplay objects already exist.</summary>
        public static void Apply(Assets assets, MaterialSet mats, Systems systems, GameObject anomaly, HeroController hero, Camera cam)
        {
            ConfigureCamera(cam);
            BuildPostFx(assets, systems, cam);
            UpgradeAnomaly(assets, mats, anomaly, cam);
            AttachHeroAnimator(assets, hero, cam);

            var labels = systems.Root.AddComponent<InspectionLabelsPresenter>();
            Wire(labels, "settings", assets.Settings);
            Wire(labels, "viewCamera", cam);

            Wire(systems.Root.GetComponent<EntityHighlightPresenter>(), "settings", assets.Settings);
            Wire(systems.Root.GetComponent<LevelHud>(), "settings", assets.Settings);
        }

        private static void ConfigureCamera(Camera cam)
        {
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        }

        private static void BuildPostFx(Assets assets, Systems systems, Camera cam)
        {
            var go = new GameObject("PostFX");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = assets.Profile;
            var fx = go.AddComponent<GlitchCameraFx>();
            Wire(fx, "settings", assets.Settings);
            Wire(fx, "glitches", systems.Glitches);
            Wire(fx, "containment", systems.Containment);
            Wire(fx, "viewCamera", cam);
        }

        private static void UpgradeAnomaly(Assets assets, MaterialSet mats, GameObject anomaly, Camera cam)
        {
            // Polygon shards instead of cubes.
            var visualRoot = anomaly.transform.Find("Visual");
            foreach (Transform child in visualRoot)
            {
                if (!child.name.StartsWith("Fragment_")) continue;
                child.GetComponent<MeshFilter>().sharedMesh = assets.Tetra;
                child.localScale = Vector3.one * 0.13f;
            }

            // Eye: dark pupil riding on the core's surface.
            var core = visualRoot.Find("Core");
            var pupil = Primitive(PrimitiveType.Sphere, "Pupil", visualRoot, Vector3.zero, new Vector3(0.14f, 0.14f, 0.06f), assets.Pupil, anomaly.layer, keepCollider: false);
            pupil.transform.localPosition = core.localPosition + Vector3.forward * 0.15f;
            var eye = core.gameObject.AddComponent<AnomalyEye>();
            eye.Configure(pupil.transform, cam.transform);
            Wire(eye, "settings", assets.Settings);

            // Short pixel trail.
            var trail = anomaly.AddComponent<TrailRenderer>();
            trail.sharedMaterial = mats.Line;
            trail.time = 0.35f;
            trail.startWidth = 0.14f;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.05f;
            trail.startColor = new Color(0.4f, 0.95f, 1f, 0.8f);
            trail.endColor = new Color(0.9f, 0.3f, 1f, 0f);
            trail.shadowCastingMode = ShadowCastingMode.Off;
        }

        private static void AttachHeroAnimator(Assets assets, HeroController hero, Camera cam)
        {
            // Animate a pivot holding the visuals, never the CharacterController's transform.
            var pivot = new GameObject("Visual").transform;
            pivot.SetParent(hero.transform, false);
            foreach (var name in new[] { "Body", "Visor" })
            {
                var part = hero.transform.Find(name);
                if (part != null) part.SetParent(pivot, true);
            }
            var animator = hero.gameObject.AddComponent<HeroAnimator>();
            animator.Configure(hero, pivot, cam);
            Wire(animator, "settings", assets.Settings);
        }

        /// <summary>Grid floor material for lab-style rooms; cells of <paramref name="cellsX"/>×<paramref name="cellsZ"/>.</summary>
        public static Material GridFloor(float cellsX, float cellsZ)
        {
            EnsureFolder(Path.GetDirectoryName(GridTexturePath).Replace('\\', '/'));
            if (!File.Exists(GridTexturePath))
            {
                var tex = ProceduralMeshes.GridTexture(256, new Color(0.62f, 0.6f, 0.56f), new Color(0.82f, 0.86f, 0.9f), 3);
                File.WriteAllBytes(GridTexturePath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(GridTexturePath);
            }
            var grid = AssetDatabase.LoadAssetAtPath<Texture2D>(GridTexturePath);
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "M_FloorGrid" };
            m.SetTexture("_BaseMap", grid);
            m.SetTextureScale("_BaseMap", new Vector2(cellsX, cellsZ));
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Smoothness", 0.3f);
            return MaterialSet.Save(m, "M_FloorGrid");
        }

        private static Mesh LoadOrCreateTetra()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(TetraMeshPath);
            if (existing != null) return existing;
            EnsureFolder(Path.GetDirectoryName(TetraMeshPath).Replace('\\', '/'));
            var mesh = ProceduralMeshes.Tetrahedron();
            AssetDatabase.CreateAsset(mesh, TetraMeshPath);
            return AssetDatabase.LoadAssetAtPath<Mesh>(TetraMeshPath);
        }

        /// <summary>Loads the profile, adding any missing override with defaults (existing tuning is never changed).</summary>
        private static VolumeProfile LoadOrCreateProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                EnsureFolder(Path.GetDirectoryName(VolumeProfilePath).Replace('\\', '/'));
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, VolumeProfilePath);
            }

            Ensure<Tonemapping>(profile, t => t.mode.Override(TonemappingMode.ACES));
            Ensure<Bloom>(profile, b =>
            {
                b.intensity.Override(0.6f);
                b.threshold.Override(1f);
                b.scatter.Override(0.65f);
            });
            Ensure<Vignette>(profile, v =>
            {
                v.intensity.Override(0.22f);
                v.smoothness.Override(0.4f);
                v.color.Override(Color.black);
            });
            Ensure<ColorAdjustments>(profile, c =>
            {
                c.postExposure.Override(0.1f);
                c.contrast.Override(8f);
                c.saturation.Override(6f);
            });
            Ensure<ChromaticAberration>(profile, c => c.intensity.Override(0f));
            Ensure<LensDistortion>(profile, l => l.intensity.Override(0f));
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static void Ensure<T>(VolumeProfile profile, Action<T> defaults) where T : VolumeComponent
        {
            if (profile.TryGet<T>(out _)) return;
            var component = profile.Add<T>(false);
            defaults(component);
            component.name = typeof(T).Name;
            // Volume components must be sub-assets of the profile, or they are lost on reload.
            AssetDatabase.AddObjectToAsset(component, profile);
        }
    }
}
