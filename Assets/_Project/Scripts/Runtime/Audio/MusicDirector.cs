using BugReport.Core.Audio;
using BugReport.Core.GameFlow;
using BugReport.Progress;
using UnityEngine;

namespace BugReport.Audio
{
    /// <summary>
    /// Plays the generative score (Core MusicEngine, original and procedural) through an AudioSource
    /// that loops a silent clip: the engine writes straight into the audio buffer in
    /// OnAudioFilterRead. The theme follows the level's chapter; tension follows containment.
    /// Music volume is a player setting and the score ducks while paused.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class MusicDirector : MonoBehaviour
    {
        [SerializeField, OptionalReference, Tooltip("Looks up the level's chapter; chapter below is used when empty or uncatalogued.")]
        private LevelCatalog catalog;
        [SerializeField, Tooltip("0 = main menu theme, 1..7 = story chapters.")]
        private int chapter = 1;
        [SerializeField, Range(0f, 1f)] private float baseVolume = 0.5f;

        private MusicEngine engine;
        private AudioSource source;
        private AudioClip silence;
        private GameServices services;
        private float duck = 1f;
        private System.Action<GameServices> bindCallback;

        public MusicEngine Engine => engine;
        public string ThemeName => engine != null ? engine.Theme.Name : string.Empty;

        public void Configure(LevelCatalog levelCatalog, int fallbackChapter)
        {
            catalog = levelCatalog;
            chapter = fallbackChapter;
        }

        private void Awake()
        {
            int resolved = chapter;
            if (catalog != null)
            {
                int index = catalog.IndexOfScene(gameObject.scene.name);
                if (index >= 0) resolved = catalog.Get(index).chapterIndex;
            }
            engine = new MusicEngine(MusicThemes.ForChapter(resolved), Mathf.Max(8000, AudioSettings.outputSampleRate), (uint)(1000 + resolved * 77));
            engine.Intensity = 0.35f;

            silence = AudioClip.Create("music_silence", 4096, 1, AudioSettings.outputSampleRate, false);
            source = GetComponent<AudioSource>();
            source.clip = silence;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.Play();
            bindCallback = s => services = s;
            GameServices.WhenReady(bindCallback);
        }

        private void Update()
        {
            if (engine == null) return;
            float target = 1f;
            if (services != null && !services.IsDisposed)
            {
                var state = services.State.Current;
                if (state == GameState.Paused) target = 0.35f;
                engine.Tension = services.Containment.Stage / 4f;
            }
            else
            {
                engine.Tension = 0f;
            }
            duck = Mathf.MoveTowards(duck, target, Time.unscaledDeltaTime * 2f);
            engine.Volume = baseVolume * SaveStore.Settings.musicVolume * duck;
        }

        // Audio thread. Never touches Unity objects; only the pure-C# engine.
        private void OnAudioFilterRead(float[] data, int channels)
        {
            var e = engine;
            if (e == null || channels <= 0) return;
            e.Render(data, data.Length / channels, channels);
        }

        private void OnDestroy()
        {
            GameServices.CancelWhenReady(bindCallback);
            if (silence != null) Destroy(silence);
        }
    }
}
