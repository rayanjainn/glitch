using NUnit.Framework;
using UnityEngine;

namespace BugReport.Tests.EditMode
{
    /// <summary>Fails until "BUG REPORT > Setup > 1. Configure Project" has been run.</summary>
    public class ProjectConfigurationTests
    {
        [Test]
        public void RequiredLayersExist()
        {
            Assert.That(GameLayers.AreConfigured(out var missing), Is.True, "Missing layers: " + missing);
        }

        [Test]
        public void IntangibleLayerCollidesWithNothing()
        {
            int intangible = GameLayers.Index(GameLayers.Intangible);
            Assume.That(intangible, Is.GreaterThanOrEqualTo(0), "Intangible layer not configured");
            for (int other = 0; other < 32; other++)
            {
                Assert.That(Physics.GetIgnoreLayerCollision(intangible, other), Is.True, "Intangible collides with layer " + other);
            }
        }

        [Test]
        public void TargetingMaskExcludesAnomalyButIncludesIntangible()
        {
            int anomaly = GameLayers.Index(GameLayers.Anomaly);
            int intangible = GameLayers.Index(GameLayers.Intangible);
            Assume.That(anomaly >= 0 && intangible >= 0, "layers not configured");
            int mask = GameLayers.TargetingMask;
            Assert.That(mask & (1 << anomaly), Is.EqualTo(0));
            Assert.That(mask & (1 << intangible), Is.Not.EqualTo(0));
        }
    }
}
