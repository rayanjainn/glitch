using UnityEditor;

namespace BugReport.EditorTools.Art
{
    /// <summary>
    /// Import rules for third-party models under Assets/ThirdParty, so nobody has to click through
    /// importer settings by hand and every machine imports them the same way:
    /// - no embedded materials (the art builder assigns URP materials from the pack texture),
    /// - no cameras/lights,
    /// - characters (KayKit Adventurers) import as Generic rigs with their animation clips,
    ///   loop flags set on the clips that cycle; props import without rigs or animation.
    /// </summary>
    public sealed class ThirdPartyModelImport : AssetPostprocessor
    {
        private const string Root = "Assets/ThirdParty/";
        private const string Characters = Root + "KayKit_Adventurers/";
        private const string Skeletons = Root + "KayKit_Skeletons/";

        private static readonly string[] LoopingClips =
        {
            "Idle", "Unarmed_Idle", "Walking_A", "Walking_B", "Walking_C", "Running_A", "Running_B", "Cheer", "Blocking",
        };

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Root)) return;
            var importer = (ModelImporter)assetImporter;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;

            bool character = IsCharacter(assetPath);
            importer.animationType = character ? ModelImporterAnimationType.Generic : ModelImporterAnimationType.None;
            importer.importAnimation = character;
        }

        private void OnPreprocessAnimation()
        {
            if (!IsCharacter(assetPath)) return;
            var importer = (ModelImporter)assetImporter;
            var clips = importer.defaultClipAnimations;
            for (int i = 0; i < clips.Length; i++) clips[i].loopTime = IsLooping(clips[i].name);
            importer.clipAnimations = clips;
        }

        /// <summary>Adventurers and the skeleton characters (not the skeleton weapons) are rigged.</summary>
        private static bool IsCharacter(string path)
            => path.StartsWith(Characters) || (path.StartsWith(Skeletons) && !path.Contains("/Weapons/"));

        /// <summary>Clip names may carry an exporter prefix ("Rig|Idle").</summary>
        public static string BaseName(string clipName)
        {
            int bar = clipName.LastIndexOf('|');
            return bar >= 0 ? clipName.Substring(bar + 1) : clipName;
        }

        private static bool IsLooping(string clipName) => System.Array.IndexOf(LoopingClips, BaseName(clipName)) >= 0;
    }
}
