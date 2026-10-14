using BugReport.Presentation;
using UnityEngine;

namespace BugReport.Hero
{
    /// <summary>
    /// Procedural body language for the placeholder hero, so its state reads at a glance
    /// (GDD 2.2: readable, expressive). Moves only the visual pivot, never the
    /// CharacterController, so it cannot affect gameplay. <see cref="CurrentIcon"/> (drawn by the UI) reads:
    /// "?" confused, "..." waiting at an obstacle, "!" setting off again.
    /// </summary>
    public sealed class HeroAnimator : MonoBehaviour
    {
        [SerializeField] private HeroController hero;
        [SerializeField] private Transform visual;
        [SerializeField, OptionalReference] private VisualSettings settings;
        [SerializeField, OptionalReference, Tooltip("Animated model (Knight). When set, it plays the body language and the procedural bob is off.")]
        private Animator animator;
        [SerializeField, Min(0.1f), Tooltip("Walk clip playback speed; tune until the feet don't slide at the hero's walk speed.")]
        private float walkCycleSpeed = 1.4f;

        private static readonly int StateParameter = Animator.StringToHash("State");
        private static readonly int WalkSpeedParameter = Animator.StringToHash("WalkSpeed");

        private Vector3 restPosition;
        private Quaternion restRotation;
        private float stateTime;
        private HeroState lastState;
        private float exclaimUntil;

        public string CurrentIcon { get; private set; } = string.Empty;

        public void Configure(HeroController newHero, Transform newVisual, Animator newAnimator = null)
        {
            hero = newHero;
            visual = newVisual;
            animator = newAnimator;
        }

        public bool HasAnimatedModel => animator != null;

        private void Awake()
        {
            if (visual != null)
            {
                restPosition = visual.localPosition;
                restRotation = visual.localRotation;
            }
        }

        private void LateUpdate() => Animate(Time.deltaTime, Time.time);

        /// <summary>Public for tests.</summary>
        public void Animate(float deltaTime, float time)
        {
            if (hero == null || visual == null) return;
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetInteger(StateParameter, (int)hero.State);
                animator.SetFloat(WalkSpeedParameter, walkCycleSpeed);
            }
            if (!VisualSettings.On(settings, s => s.heroAnimation))
            {
                visual.localPosition = restPosition;
                visual.localRotation = restRotation;
                CurrentIcon = string.Empty;
                return;
            }

            if (hero.State != lastState)
            {
                if ((lastState == HeroState.Blocked || lastState == HeroState.Confused) && hero.State == HeroState.Walking)
                    exclaimUntil = time + 1f;
                lastState = hero.State;
                stateTime = 0f;
            }
            stateTime += deltaTime;

            var offset = Vector3.zero;
            var euler = Vector3.zero;
            switch (hero.State)
            {
                case HeroState.Walking:
                    offset.y = Mathf.Abs(Mathf.Sin(time * 9f)) * 0.07f;
                    euler.x = 6f;
                    break;
                case HeroState.Blocked:
                    offset.y = Mathf.Sin(time * 2f) * 0.02f; // breathing
                    euler.y = Mathf.Sin(time * 1.3f) * 12f; // looking around for a way
                    break;
                case HeroState.Confused:
                    euler.z = Mathf.Sin(time * 14f) * 8f;
                    euler.y = Mathf.Sin(time * 5f) * 20f;
                    break;
                case HeroState.Arrived:
                    offset.y = Mathf.Abs(Mathf.Sin(stateTime * 6f)) * 0.35f * Mathf.Exp(-stateTime * 0.8f);
                    euler.y = Mathf.Min(stateTime, 1f) * 360f;
                    break;
                default:
                    offset.y = Mathf.Sin(time * 2f) * 0.02f;
                    break;
            }
            if (animator != null)
            {
                // The clips carry the body language; keep only the "looking for a way" turn.
                offset = Vector3.zero;
                if (hero.State != HeroState.Blocked) euler = Vector3.zero;
            }
            visual.localPosition = restPosition + offset;
            visual.localRotation = restRotation * Quaternion.Euler(euler);

            CurrentIcon = hero.State == HeroState.Confused ? "?"
                : hero.State == HeroState.Blocked ? "..."
                : time < exclaimUntil ? "!"
                : string.Empty;
        }
    }
}
