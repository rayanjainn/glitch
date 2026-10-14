using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BugReport.Tests.PlayMode
{
    /// <summary>
    /// Creates objects on the Default layer (tests must not depend on project layer setup)
    /// and destroys everything it created, plus any installed GameServices, after each test.
    /// </summary>
    public abstract class PlayModeTestBase
    {
        private readonly List<Object> created = new List<Object>();

        /// <summary>Every PlayMode test uses a throwaway save file, never the player's.</summary>
        [SetUp]
        public void BaseSetUp() => RedirectSave();

        internal static void RedirectSave()
        {
            var path = System.IO.Path.Combine(Application.temporaryCachePath, "bugreport_test_save.json");
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            Progress.SaveStore.RedirectForTests(path);
        }

        [TearDown]
        public void BaseTearDown()
        {
            for (int i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null) Object.DestroyImmediate(created[i]);
            }
            created.Clear();
            GameServices.Current?.Dispose();
            Progress.SaveStore.RedirectForTests(null);
        }

        protected T Track<T>(T obj) where T : Object
        {
            created.Add(obj);
            return obj;
        }

        protected GameObject Box(string name, Vector3 position, Vector3 size)
        {
            var go = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = size;
            return go;
        }

        protected GameObject Ground() => Box("Ground", new Vector3(0f, -0.5f, 0f), new Vector3(60f, 1f, 60f));
    }
}
