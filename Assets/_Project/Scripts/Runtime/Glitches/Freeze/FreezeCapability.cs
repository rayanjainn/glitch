using System;
using System.Collections.Generic;
using BugReport.Core.Glitches;
using UnityEngine;

namespace BugReport.Glitches
{
    /// <summary>
    /// Local, per-entity time stop (never a global pause). The only code allowed to change
    /// the entity's Rigidbody.isKinematic and Animator speeds while a freeze is active.
    ///
    /// Frozen:   rigidbody → kinematic (forces and gravity are ignored, nothing accumulates),
    ///           animators → speed 0, scripted movers read <see cref="TimeScale"/> = 0.
    /// Unfrozen: kinematic flag back to its pre-freeze value; a dynamic body is released AT REST
    ///           (zero velocity). Restoring momentum is the effect's decision (FreezeEffect.OnEnded).
    ///
    /// Scripted movers must advance with <see cref="DeltaTime"/>/<see cref="FixedDeltaTime"/>
    /// instead of Time.deltaTime, otherwise freezing cannot stop them.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GameEntity))]
    public sealed class FreezeCapability : MonoBehaviour
    {
        private readonly List<Animator> animators = new List<Animator>();
        private readonly List<float> animatorSpeeds = new List<float>();
        private ModifierStack<bool> channel;
        private Rigidbody body;
        private bool bodyWasKinematic;

        public event Action<bool> FrozenChanged;

        public ModifierStack<bool> Channel
        {
            get
            {
                EnsureInitialized();
                return channel;
            }
        }

        public bool IsFrozen => channel != null && channel.Effective;
        public float TimeScale => IsFrozen ? 0f : 1f;
        public float DeltaTime => Time.deltaTime * TimeScale;
        public float FixedDeltaTime => Time.fixedDeltaTime * TimeScale;
        public Rigidbody Body
        {
            get
            {
                EnsureInitialized();
                return body;
            }
        }

        /// <summary>Has something a freeze can actually stop (body, animator, or a mover reading TimeScale).</summary>
        public bool HasAnythingToFreeze
        {
            get
            {
                EnsureInitialized();
                return body != null || animators.Count > 0 || GetComponent<ITimeScaled>() != null;
            }
        }

        private void Awake() => EnsureInitialized();

        private void EnsureInitialized()
        {
            if (channel != null) return;
            body = GetComponent<Rigidbody>();
            GetComponentsInChildren(true, animators);
            channel = new ModifierStack<bool>(false, ConflictPolicy.Exclusive, Apply);
        }

        private void Apply(bool frozen)
        {
            if (this == null) return; // destroyed while a lease was outstanding (scene teardown)

            if (body != null)
            {
                if (frozen)
                {
                    bodyWasKinematic = body.isKinematic;
                    body.isKinematic = true;
                }
                else
                {
                    body.isKinematic = bodyWasKinematic;
                    if (!bodyWasKinematic)
                    {
                        // Order matters in Unity 6: velocities can only be written to non-kinematic bodies.
                        body.linearVelocity = Vector3.zero;
                        body.angularVelocity = Vector3.zero;
                        body.WakeUp();
                    }
                }
            }

            if (frozen)
            {
                animatorSpeeds.Clear();
                for (int i = 0; i < animators.Count; i++)
                {
                    animatorSpeeds.Add(animators[i] != null ? animators[i].speed : 1f);
                    if (animators[i] != null) animators[i].speed = 0f;
                }
            }
            else
            {
                for (int i = 0; i < animators.Count && i < animatorSpeeds.Count; i++)
                {
                    if (animators[i] != null) animators[i].speed = animatorSpeeds[i];
                }
            }

            FrozenChanged?.Invoke(frozen);
        }
    }

    /// <summary>Marker for scripted movers that respect <see cref="FreezeCapability.TimeScale"/>.</summary>
    public interface ITimeScaled
    {
    }
}
