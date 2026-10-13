using BugReport.Core.Entities;
using BugReport.EditorTools.Setup;
using BugReport.Glitches;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BugReport.Tests.EditMode
{
    public class GlitchDefinitionTests
    {
        [Test]
        public void CollisionTransferDefaults_AreValid()
        {
            var d = ScriptableObject.CreateInstance<CollisionTransferDefinition>();
            try
            {
                d.ApplyDefaults();
                Assert.That(d.ValidateDefinition(), Is.Null);
                var spec = d.ToSpec();
                Assert.That(spec.Arity, Is.EqualTo(2));
                Assert.That(spec.Slot(0).RequiredCapabilities.HasAll(EntityCapabilities.CollisionTransferable), Is.True);
                Assert.That(spec.Slot(0).Excludes(EntityCategory.Anomaly), Is.True);
                Assert.That(spec.Slot(1).Excludes(EntityCategory.Hero), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(d);
            }
        }

        [Test]
        public void CollisionTransfer_WithWrongSlotCount_IsInvalid()
        {
            var d = ScriptableObject.CreateInstance<CollisionTransferDefinition>();
            try
            {
                Assert.That(d.ValidateDefinition(), Is.Not.Null, "no slots");
                d.Configure("x", "X", "", Color.white, 0f, 0f, 0, new[] { new GlitchDefinition.TargetSlot("ONLY", EntityCapabilities.None) });
                StringAssert.Contains("exactly 2", d.ValidateDefinition());
            }
            finally
            {
                Object.DestroyImmediate(d);
            }
        }

        [Test]
        public void ProjectAsset_IfPresent_IsValid()
        {
            var asset = AssetDatabase.LoadAssetAtPath<CollisionTransferDefinition>(TestRoomBuilder.CollisionTransferPath);
            if (asset == null) Assert.Ignore("Not created yet: run 'Build Test Room Scene'.");
            Assert.That(asset.ValidateDefinition(), Is.Null);
        }
    }
}
