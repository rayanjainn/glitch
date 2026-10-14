using System.IO;
using System.Linq;
using BugReport.EditorTools.Art;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static BugReport.EditorTools.Setup.TestRoomBuilder;

namespace BugReport.EditorTools.Setup
{
    /// <summary>
    /// Brings the third-party CC0 art (Assets/ThirdParty, see CREDITS.md) into the generated levels:
    /// URP materials from the packs' atlas textures, the hero's animator controller, and helpers
    /// that place a model inside an existing gameplay object, scaled to fit it.
    ///
    /// Everything is optional: if the files have not been fetched yet
    /// (Tools/Assets/fetch-cc0-assets.sh), builders keep the procedural placeholders.
    /// Gameplay never depends on art: colliders, entities and capabilities stay on the gameplay
    /// objects; models are child visuals only.
    /// </summary>
    public static class ArtBuilder
    {
        public const string KnightPath = "Assets/ThirdParty/KayKit_Adventurers/Knight.fbx";
        public const string KnightTexturePath = "Assets/ThirdParty/KayKit_Adventurers/knight_texture.png";
        public const string DungeonFolder = "Assets/ThirdParty/KayKit_DungeonRemastered/Models";
        public const string DungeonTexturePath = "Assets/ThirdParty/KayKit_DungeonRemastered/dungeon_texture.png";
        public const string SoundFolder = "Assets/ThirdParty/Kenney_StarterKitSounds";
        public const string DisplayFontPath = "Assets/ThirdParty/Fonts/LilitaOne/LilitaOne-Regular.ttf";
        public const string HeroControllerPath = "Assets/_Project/Art/Animation/AC_Hero.controller";

        /// <summary>Clip per <c>HeroState</c> value (Idle, Walking, Blocked, Confused, Arrived).</summary>
        public static readonly string[] HeroClips = { "Idle", "Walking_A", "Idle", "Hit_A", "Cheer" };

        /// <summary>Knight accessories hidden so it carries one sword and one shield.</summary>
        private static readonly string[] HiddenKnightParts =
            { "1H_Sword_Offhand", "2H_Sword", "Rectangle_Shield", "Round_Shield", "Spike_Shield" };

        public static bool HeroAvailable => File.Exists(KnightPath) && File.Exists(KnightTexturePath);
        public static bool DungeonAvailable => File.Exists(DungeonTexturePath) && Directory.Exists(DungeonFolder);
        public static string DungeonModel(string name) => DungeonFolder + "/" + name + ".fbx";

        // ---------------------------------------------------------------- materials

        internal static Material AtlasMaterial(string name, string texturePath)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            m.SetTexture("_BaseMap", texture);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Smoothness", 0.25f);
            return MaterialSet.Save(m, name);
        }

        // ---------------------------------------------------------------- hero

        /// <summary>
        /// Puts the Knight under <paramref name="hero"/> as "Body", feet at the bottom of the
        /// CharacterController, scaled to <paramref name="height"/>, with an Animator driven by
        /// HeroAnimator. Returns null (caller keeps the placeholder) if the art isn't available.
        /// </summary>
        internal static GameObject BuildHeroModel(Transform hero, CharacterController cc, float height, int layer)
        {
            if (!HeroAvailable) return null;
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(KnightPath);
            if (source == null) return null;

            var model = (GameObject)PrefabUtility.InstantiatePrefab(source, hero);
            model.name = "Body";
            foreach (var part in model.GetComponentsInChildren<Transform>(true))
                if (System.Array.IndexOf(HiddenKnightParts, part.name) >= 0) part.gameObject.SetActive(false);

            var material = AtlasMaterial("M_Knight", KnightTexturePath);
            Prepare(model, material, layer);
            FitHeight(model, height);
            float feet = cc.center.y - cc.height * 0.5f;
            AlignBottom(model, hero.TransformPoint(new Vector3(0f, feet, 0f)).y);

            var animator = model.GetComponent<Animator>();
            if (animator == null) animator = model.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            animator.runtimeAnimatorController = HeroController();
            return model;
        }

        /// <summary>Idle/Walk/Blocked/Confused/Arrived states selected by the "State" int; walk speed scaled by "WalkSpeed".</summary>
        internal static AnimatorController HeroController()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(KnightPath).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__")).ToList();
            AnimationClip Clip(string name)
            {
                var clip = clips.FirstOrDefault(c => ThirdPartyModelImport.BaseName(c.name) == name);
                if (clip == null) throw new System.InvalidOperationException("Knight.fbx has no clip '" + name + "' (found: " + clips.Count + " clips).");
                return clip;
            }

            EnsureFolderFor(HeroControllerPath);
            string[] names = { "Idle", "Walking", "Blocked", "Confused", "Arrived" };
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(HeroControllerPath);
            if (existing != null)
            {
                // Reuse when it already matches, so rebuilding a level doesn't churn the asset's GUID.
                var current = existing.layers[0].stateMachine.states;
                bool matches = current.Length == names.Length;
                for (int i = 0; matches && i < current.Length; i++)
                {
                    var motion = current[i].state.motion;
                    matches = current[i].state.name == names[i] && motion != null
                        && ThirdPartyModelImport.BaseName(motion.name) == HeroClips[i];
                }
                if (matches) return existing;
                AssetDatabase.DeleteAsset(HeroControllerPath);
            }
            var controller = AnimatorController.CreateAnimatorControllerAtPath(HeroControllerPath);
            controller.AddParameter("State", AnimatorControllerParameterType.Int);
            controller.AddParameter("WalkSpeed", AnimatorControllerParameterType.Float);
            var parameters = controller.parameters;
            foreach (var p in parameters) if (p.name == "WalkSpeed") p.defaultFloat = 1f;
            controller.parameters = parameters;

            var machine = controller.layers[0].stateMachine;
            for (int i = 0; i < names.Length; i++)
            {
                var state = machine.AddState(names[i]);
                state.motion = Clip(HeroClips[i]);
                if (names[i] == "Walking")
                {
                    state.speedParameterActive = true;
                    state.speedParameter = "WalkSpeed";
                }
                var transition = machine.AddAnyStateTransition(state);
                transition.AddCondition(AnimatorConditionMode.Equals, i, "State");
                transition.hasExitTime = false;
                transition.duration = 0.15f;
                transition.canTransitionToSelf = false;
                if (i == 0) machine.defaultState = state;
            }
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        // ---------------------------------------------------------------- props

        /// <summary>
        /// Replaces a primitive's own mesh with a dungeon-kit model fitted inside its collider
        /// (uniform scale, centred, resting on the bottom). Returns false (placeholder kept) if the
        /// model isn't available.
        /// </summary>
        internal static bool SkinWithDungeonModel(GameObject target, string modelName, Material material)
        {
            if (!DungeonAvailable || material == null) return false;
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(DungeonModel(modelName));
            var box = target.GetComponent<BoxCollider>();
            if (source == null || box == null) return false;

            var model = (GameObject)PrefabUtility.InstantiatePrefab(source, target.transform);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.name = "Model";
            Prepare(model, material, target.layer);

            // Fit in the collider's local box (uniform scale), centred and resting on its bottom.
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
            var local = LocalBounds(model, target.transform);
            float scale = Mathf.Min(box.size.x / local.size.x, box.size.y / local.size.y, box.size.z / local.size.z);
            model.transform.localScale = Vector3.one * scale;
            local = LocalBounds(model, target.transform);
            var bottomCentre = new Vector3(box.center.x, box.center.y - box.size.y * 0.5f, box.center.z);
            model.transform.localPosition += bottomCentre - new Vector3(local.center.x, local.min.y, local.center.z);

            var own = target.GetComponent<MeshRenderer>();
            if (own != null) Object.DestroyImmediate(own);
            var filter = target.GetComponent<MeshFilter>();
            if (filter != null) Object.DestroyImmediate(filter);
            return true;
        }

        /// <summary>
        /// A decorative, static dungeon-kit model of the given height on the floor at
        /// <paramref name="position"/>, with a box collider so the anomaly can't fly through it.
        /// </summary>
        internal static GameObject Decor(string modelName, Transform parent, Vector3 position, float yaw, float height, Material material, int layer)
        {
            if (!DungeonAvailable || material == null) return null;
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(DungeonModel(modelName));
            if (source == null)
            {
                Debug.LogWarning("[BUG REPORT] Decor model missing: " + modelName);
                return null;
            }
            var root = new GameObject("Decor_" + modelName) { layer = layer, isStatic = true };
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(position, Quaternion.identity); // measure unrotated, rotate at the end
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            Prepare(model, material, layer);
            FitHeight(model, height);
            AlignBottom(model, position.y);

            var bounds = LocalBounds(model, root.transform);
            var box = root.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = bounds.size;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = true;
            return root;
        }

        // ---------------------------------------------------------------- helpers

        private static void Prepare(GameObject model, Material material, int layer)
        {
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
            foreach (var c in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                var materials = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < materials.Length; i++) materials[i] = material;
                r.sharedMaterials = materials;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }

        private static void FitHeight(GameObject model, float height)
        {
            var bounds = WorldBounds(model);
            if (bounds.size.y <= 0.0001f) return;
            model.transform.localScale *= height / bounds.size.y;
        }

        private static void AlignBottom(GameObject model, float worldY)
        {
            var bounds = WorldBounds(model);
            model.transform.position += Vector3.up * (worldY - bounds.min.y);
        }

        private static Bounds WorldBounds(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(false);
            if (renderers.Length == 0) return new Bounds(model.transform.position, Vector3.zero);
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        /// <summary>Bounds of the model's renderers expressed in <paramref name="space"/>'s local axes.</summary>
        private static Bounds LocalBounds(GameObject model, Transform space)
        {
            var world = WorldBounds(model);
            var min = space.InverseTransformPoint(world.min);
            var max = space.InverseTransformPoint(world.max);
            var b = new Bounds(min, Vector3.zero);
            b.Encapsulate(max);
            return b;
        }
    }
}
