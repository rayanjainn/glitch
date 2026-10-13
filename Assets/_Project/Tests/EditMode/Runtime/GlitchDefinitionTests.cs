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
        public void DuplicationAndFreezeDefaults_AreValid()
        {
            var dup = ScriptableObject.CreateInstance<DuplicationDefinition>();
            var freeze = ScriptableObject.CreateInstance<FreezeDefinition>();
            try
            {
                dup.ApplyDefaults();
                freeze.ApplyDefaults();
                Assert.That(dup.ValidateDefinition(), Is.Null);
                Assert.That(freeze.ValidateDefinition(), Is.Null);
                Assert.That(dup.ToSpec().Slot(0).RequiredCapabilities.HasAll(EntityCapabilities.Duplicable), Is.True);
                Assert.That(freeze.ToSpec().Slot(0).Excludes(EntityCategory.Hero), Is.True);
                Assert.That(freeze.ToSpec().Duration, Is.GreaterThan(0f), "freeze must have a duration limit");
                Assert.That(dup.ToSpec().MaxActive, Is.GreaterThan(0), "duplication needs a global cap");
            }
            finally
            {
                Object.DestroyImmediate(dup);
                Object.DestroyImmediate(freeze);
            }
        }

        [Test]
        public void ProjectAsset_IfPresent_IsValid()
        {
            foreach (var path in new[] { TestRoomBuilder.CollisionTransferPath, TestRoomBuilder.DuplicationPath, TestRoomBuilder.FreezePath })
            {
                var asset = AssetDatabase.LoadAssetAtPath<GlitchDefinition>(path);
                if (asset == null) Assert.Ignore("Not created yet: run 'Build Test Room Scene'.");
                Assert.That(asset.ValidateDefinition(), Is.Null, path);
            }
        }
    }
}
