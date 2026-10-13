using BugReport.Anomaly;
using BugReport.Core.Entities;
using BugReport.Core.GameFlow;
using BugReport.Glitches;
using BugReport.Hero;
using BugReport.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BugReport.Tests.PlayMode
{
    public class PresentationTests : PlayModeTestBase
    {
        private GameObject T(GameObject go) => Track(go);

        // ---------------------------------------------------------------- camera bursts

        private GlitchCameraFx CameraFx(VisualSettings settings = null)
        {
            var profile = Track(ScriptableObject.CreateInstance<VolumeProfile>());
            profile.Add<Vignette>(true).intensity.Override(0.2f);
            var go = Track(new GameObject("PostFX"));
            go.SetActive(false);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            var fx = go.AddComponent<GlitchCameraFx>();
            var controllerGo = Track(new GameObject("Glitches"));
            var field = typeof(GlitchCameraFx).GetField("glitches", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(fx, controllerGo.AddComponent<GlitchController>());
            if (settings != null)
                typeof(GlitchCameraFx).GetField("settings", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(fx, settings);
            go.SetActive(true);
            return fx;
        }

        [Test]
        public void GlitchBurst_SpikesThenReturnsToZero()
        {
            bool previous = ComfortSettings.ChromaticEffects;
            try
            {
                ComfortSettings.ChromaticEffects = true;
                var fx = CameraFx();
                fx.Step(0f);
                Assert.That(fx.CurrentChromatic, Is.EqualTo(0f));

                fx.TriggerGlitchBurst(10f);
                fx.Step(10.07f);
                Assert.That(fx.CurrentChromatic, Is.GreaterThan(0.5f));

                fx.Step(11f);
                Assert.That(fx.CurrentChromatic, Is.EqualTo(0f), "bursts, never continuous");
            }
            finally
            {
                ComfortSettings.ChromaticEffects = previous;
            }
        }

        [Test]
        public void ComfortSettingOff_DisablesColourSplit()
        {
            bool previous = ComfortSettings.ChromaticEffects;
            try
            {
                ComfortSettings.ChromaticEffects = false;
                var fx = CameraFx();
                fx.TriggerGlitchBurst(10f);
                fx.Step(10.07f);
                Assert.That(fx.CurrentChromatic, Is.EqualTo(0f));
            }
            finally
            {
                ComfortSettings.ChromaticEffects = previous;
            }
        }

        [Test]
        public void PatchBurst_RaisesVignetteThenRestoresBase()
        {
            var fx = CameraFx();
            fx.Step(0f);
            Assert.That(fx.CurrentVignette, Is.EqualTo(0.2f).Within(1e-4f));
            fx.TriggerPatchBurst(5f);
            fx.Step(5.1f);
            Assert.That(fx.CurrentVignette, Is.GreaterThan(0.3f));
            fx.Step(7f);
            Assert.That(fx.CurrentVignette, Is.EqualTo(0.2f).Within(1e-4f));
        }

        [Test]
        public void VisualSettingsOff_DisablesVolume()
        {
            var settings = Track(ScriptableObject.CreateInstance<VisualSettings>());
            settings.postProcessing = false;
            var fx = CameraFx(settings);
            Assert.That(fx.GetComponent<Volume>().enabled, Is.False);
        }

        // ---------------------------------------------------------------- hero body language

        private (HeroController hero, HeroAnimator animator, Transform pivot) AnimatedHero(HeroNavGraph graph, HeroNavNode goal)
        {
            var hero = HeroTestKit.Hero(T, new Vector3(0, 1.05f, 0), graph, goal);
            var pivot = Track(new GameObject("Visual")).transform;
            pivot.SetParent(hero.transform, false);
            var animator = hero.gameObject.AddComponent<HeroAnimator>();
            animator.Configure(hero, pivot, null);
            return (hero, animator, pivot);
        }

        [Test]
        public void HeroIcons_ReflectState()
        {
            Assume.That(GameLayers.AreConfigured(out _), Is.True);
            Ground().layer = GameLayers.Index(GameLayers.World);
            var graph = HeroTestKit.Chain(T, out var n, new Vector3(0, 0, 0), new Vector3(0, 0, 10));
            var wall = Box("Wall", new Vector3(0, 1.5f, 5), new Vector3(10, 3, 0.5f));
            wall.layer = GameLayers.Index(GameLayers.World);
            Physics.SyncTransforms();
            var (hero, animator, _) = AnimatedHero(graph, n[1]);

            HeroTestKit.Simulate(hero, 1f);
            animator.Animate(0.1f, 1f);
            Assert.That(hero.State, Is.EqualTo(HeroState.Blocked));
            Assert.That(animator.CurrentIcon, Is.EqualTo("..."));

            wall.GetComponent<Collider>().enabled = false;
            hero.NotifyWorldChanged();
            HeroTestKit.SimulateUntil(hero, HeroState.Walking, 2f);
            animator.Animate(0.1f, 2f);
            Assert.That(animator.CurrentIcon, Is.EqualTo("!"), "sets off again");
            animator.Animate(0.1f, 4f);
            Assert.That(animator.CurrentIcon, Is.Empty);
        }

        [Test]
        public void HeroAnimation_MovesOnlyThePivot_AndCanBeSwitchedOff()
        {
            Ground();
            var graph = HeroTestKit.Chain(T, out var n, new Vector3(0, 0, 0), new Vector3(0, 0, 10));
            var (hero, animator, pivot) = AnimatedHero(graph, n[1]);
            HeroTestKit.Simulate(hero, 0.5f);
            var rootBefore = hero.transform.position;

            animator.Animate(0.1f, 0.13f);
            Assert.That(pivot.localPosition.y, Is.GreaterThan(0f), "walking bob");
            Assert.That(hero.transform.position, Is.EqualTo(rootBefore), "gameplay transform untouched");

            var settings = Track(ScriptableObject.CreateInstance<VisualSettings>());
            settings.heroAnimation = false;
            typeof(HeroAnimator).GetField("settings", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(animator, settings);
            animator.Animate(0.1f, 0.2f);
            Assert.That(pivot.localPosition, Is.EqualTo(Vector3.zero));
        }

        // ---------------------------------------------------------------- anomaly eye

        [Test]
        public void AnomalyEye_LooksAtTheHoveredEntity()
        {
            var services = new GameServices();
            GameServices.Install(services);
            services.State.TryTransition(GameState.Playing);
            var target = Box("Target", new Vector3(5, 0, 0), Vector3.one);
            target.AddComponent<GameEntity>();
            var eyeGo = Track(new GameObject("Core"));
            var pupil = Track(new GameObject("Pupil")).transform;
            var eye = eyeGo.AddComponent<AnomalyEye>();
            eye.Configure(pupil, null);

            services.Selection.SetHovered(target.GetComponent<GameEntity>().Id);
            for (int i = 0; i < 30; i++) eye.Look(0.1f);

            Assert.That(Vector3.Dot(eye.LookDirection, Vector3.right), Is.GreaterThan(0.95f));
            Assert.That(pupil.position.x, Is.GreaterThan(0f));
        }
    }
}
