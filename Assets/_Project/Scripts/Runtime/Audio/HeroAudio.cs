using BugReport.Hero;
using UnityEngine;

namespace BugReport.Audio
{
    /// <summary>
    /// The hero's footsteps: a looping 3D clip while it walks, silent otherwise and while paused.
    /// Presentation only.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class HeroAudio : MonoBehaviour
    {
        [SerializeField] private HeroController hero;
        [SerializeField] private AudioClip footsteps;
        [SerializeField, Range(0f, 1f)] private float volume = 0.45f;

        private AudioSource source;

        public bool IsPlaying => source != null && source.isPlaying;

        public void Configure(HeroController newHero, AudioClip newFootsteps)
        {
            hero = newHero;
            footsteps = newFootsteps;
        }

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 1f;
            source.minDistance = 2f;
            source.maxDistance = 25f;
            source.rolloffMode = AudioRolloffMode.Linear;
        }

        private void Update() => Refresh();

        /// <summary>Public for tests.</summary>
        public void Refresh()
        {
            if (source == null || hero == null || footsteps == null) return;
            bool walking = hero.State == HeroState.Walking && Time.timeScale > 0f;
            if (walking && !source.isPlaying)
            {
                source.clip = footsteps;
                source.volume = volume;
                source.Play();
            }
            else if (!walking && source.isPlaying)
            {
                source.Stop();
            }
        }
    }
}
