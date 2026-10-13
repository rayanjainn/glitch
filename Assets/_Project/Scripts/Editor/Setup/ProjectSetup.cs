using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BugReport.EditorTools.Setup
{
    /// <summary>
    /// Reproducible project configuration. Idempotent: running it twice changes nothing.
    /// Never overwrites a layer slot that already holds a different name.
    /// </summary>
    public static class ProjectSetup
    {
        public const string MenuRoot = "BUG REPORT/Setup/";
        private const int FirstUserLayer = 8;

        [MenuItem(MenuRoot + "1. Configure Project", priority = 1)]
        public static void ConfigureProjectMenu()
        {
            bool ok = ConfigureProject(out var report);
            if (ok) Debug.Log(report);
            else Debug.LogError(report);
            EditorUtility.DisplayDialog("BUG REPORT setup", ok ? "Project configured. See Console for details." : "Configuration finished with problems. See Console.", "OK");
        }

        [MenuItem(MenuRoot + "Validate Project", priority = 20)]
        public static void ValidateProjectMenu()
        {
            bool ok = Validate(out var report);
            if (ok) Debug.Log(report);
            else Debug.LogWarning(report);
        }

        public static bool ConfigureProject(out string report)
        {
            var log = new StringBuilder("[BUG REPORT] Configure Project\n");
            bool ok = EnsureLayers(log);
            if (ok) ConfigureCollisionMatrix(log);
            AssetDatabase.SaveAssets();
            ok &= ValidateEnvironment(log);
            report = log.ToString();
            return ok;
        }

        public static bool Validate(out string report)
        {
            var log = new StringBuilder("[BUG REPORT] Validate Project\n");
            bool ok = true;
            if (!GameLayers.AreConfigured(out var missing))
            {
                log.AppendLine("  ERROR missing layers: " + missing);
                ok = false;
            }
            else
            {
                log.AppendLine("  OK layers present");
                ok &= ValidateCollisionMatrix(log);
            }
            ok &= ValidateEnvironment(log);
            report = log.ToString();
            return ok;
        }

        private static bool EnsureLayers(StringBuilder log)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                log.AppendLine("  ERROR cannot load ProjectSettings/TagManager.asset");
                return false;
            }

            var tagManager = new SerializedObject(assets[0]);
            var layers = tagManager.FindProperty("layers");
            if (layers == null || !layers.isArray)
            {
                log.AppendLine("  ERROR TagManager has no 'layers' array");
                return false;
            }

            bool ok = true;
            foreach (var layerName in GameLayers.Required)
            {
                if (FindLayer(layers, layerName) >= 0)
                {
                    log.AppendLine("  OK layer '" + layerName + "' exists");
                    continue;
                }

                int slot = -1;
                for (int i = FirstUserLayer; i < layers.arraySize; i++)
                {
                    if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) { slot = i; break; }
                }

                if (slot < 0)
                {
                    log.AppendLine("  ERROR no free user layer for '" + layerName + "'");
                    ok = false;
                    continue;
                }

                layers.GetArrayElementAtIndex(slot).stringValue = layerName;
                log.AppendLine("  ADDED layer '" + layerName + "' at index " + slot);
            }

            tagManager.ApplyModifiedPropertiesWithoutUndo();
            return ok;
        }

        private static int FindLayer(SerializedProperty layers, string layerName)
        {
            for (int i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue == layerName) return i;
            }
            return -1;
        }

        /// <summary>Intangible collides with nothing (including itself). Other pairs keep Unity defaults.</summary>
        private static void ConfigureCollisionMatrix(StringBuilder log)
        {
            int intangible = GameLayers.Index(GameLayers.Intangible);
            if (intangible < 0) return;
            for (int other = 0; other < 32; other++)
            {
                Physics.IgnoreLayerCollision(intangible, other, true);
            }
            log.AppendLine("  SET Intangible ignores all physics layers");
        }

        private static bool ValidateCollisionMatrix(StringBuilder log)
        {
            int intangible = GameLayers.Index(GameLayers.Intangible);
            var colliding = new List<string>();
            for (int other = 0; other < 32; other++)
            {
                if (!Physics.GetIgnoreLayerCollision(intangible, other))
                {
                    string n = LayerMask.LayerToName(other);
                    colliding.Add(string.IsNullOrEmpty(n) ? other.ToString() : n);
                }
            }
            if (colliding.Count == 0)
            {
                log.AppendLine("  OK Intangible ignores all layers");
                return true;
            }
            log.AppendLine("  ERROR Intangible still collides with: " + string.Join(", ", colliding));
            return false;
        }

        private static bool ValidateEnvironment(StringBuilder log)
        {
            bool ok = true;
            log.AppendLine("  INFO Unity " + Application.unityVersion + " (project targets Unity 6.6 / 6000.6.x)");
            if (!Application.unityVersion.StartsWith("6000."))
            {
                log.AppendLine("  WARN this is not a Unity 6 editor");
            }

#if ENABLE_INPUT_SYSTEM
            log.AppendLine("  OK Input System backend enabled");
#else
            log.AppendLine("  ERROR Input System backend disabled: Project Settings > Player > Active Input Handling = 'Input System Package (New)' (requires restart)");
            ok = false;
#endif

            var pipeline = GraphicsSettings.defaultRenderPipeline;
            if (pipeline == null)
            {
                log.AppendLine("  ERROR no render pipeline asset assigned (Built-in RP). Assign the URP asset in Project Settings > Graphics.");
                ok = false;
            }
            else if (!pipeline.GetType().FullName.Contains("Universal"))
            {
                log.AppendLine("  ERROR render pipeline is " + pipeline.GetType().Name + ", expected URP");
                ok = false;
            }
            else
            {
                log.AppendLine("  OK URP asset: " + pipeline.name);
            }

            if (PlayerSettings.colorSpace != ColorSpace.Linear)
            {
                log.AppendLine("  WARN color space is Gamma; Linear is expected for URP lighting");
            }

            if (System.Type.GetType("NUnit.Framework.TestAttribute, nunit.framework") == null)
            {
                log.AppendLine("  WARN Unity Test Framework (com.unity.test-framework) not detected; tests will not run");
            }
            return ok;
        }
    }
}
