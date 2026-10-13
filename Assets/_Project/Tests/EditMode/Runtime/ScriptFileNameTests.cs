using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace BugReport.Tests.EditMode
{
    /// <summary>
    /// Unity can only serialise a MonoBehaviour/ScriptableObject into scenes and assets if it lives
    /// in a file named exactly like the class. A mismatch compiles fine but the component silently
    /// disappears on save ("references runtime script in scene file. Fixing!").
    /// </summary>
    public class ScriptFileNameTests
    {
        [Test]
        public void EverySerializableProjectType_IsInAFileWithItsName()
        {
            var problems = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name.StartsWith("BugReport") && !a.GetName().Name.Contains("Tests"))
                .SelectMany(a => a.GetTypes())
                .Where(t => !t.IsAbstract && !t.IsGenericType &&
                            (typeof(UnityEngine.MonoBehaviour).IsAssignableFrom(t) || typeof(UnityEngine.ScriptableObject).IsAssignableFrom(t)) &&
                            !typeof(UnityEditor.Editor).IsAssignableFrom(t) && !typeof(EditorWindow).IsAssignableFrom(t))
                .Where(t => !AssetDatabase.FindAssets(t.Name + " t:MonoScript")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<MonoScript>)
                    .Any(s => s != null && s.GetClass() == t))
                .Select(t => t.FullName)
                .ToList();

            Assert.That(problems, Is.Empty, "Move each class into <ClassName>.cs:\n" + string.Join("\n", problems));
        }
    }
}
