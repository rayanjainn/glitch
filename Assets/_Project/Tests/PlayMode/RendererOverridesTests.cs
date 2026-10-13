using BugReport.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace BugReport.Tests.PlayMode
{
    public class RendererOverridesTests : PlayModeTestBase
    {
        [Test]
        public void HighestPriorityWins_AndOriginalsReturn()
        {
            var go = Box("Thing", Vector3.zero, Vector3.one);
            var renderer = go.GetComponent<Renderer>();
            var original = renderer.sharedMaterial;
            var low = Track(new Material(Shader.Find("Sprites/Default")));
            var high = Track(new Material(Shader.Find("Sprites/Default")));
            var overrides = go.AddComponent<RendererOverrides>();

            overrides.Set("collision", low, 10);
            Assert.That(renderer.sharedMaterial, Is.SameAs(low));
            overrides.Set("freeze", high, 20);
            Assert.That(renderer.sharedMaterial, Is.SameAs(high));
            overrides.Clear("freeze");
            Assert.That(renderer.sharedMaterial, Is.SameAs(low), "collision look comes back, not the original");
            overrides.Clear("collision");
            Assert.That(renderer.sharedMaterial, Is.SameAs(original));
        }
    }
}
