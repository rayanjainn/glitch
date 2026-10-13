using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BugReport.InputHandling
{
    /// <summary>
    /// The only gameplay-facing class that touches the Input System. Exposes
    /// device-agnostic values and events; gameplay code depends on these, never on
    /// devices or bindings, so keyboard/mouse and gamepad share one code path and
    /// bindings can be remapped without touching gameplay.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InputReader : MonoBehaviour
    {
        public const string AnomalyMapName = "Anomaly";

        public static class Actions
        {
            public const string Move = "Move";
            public const string Look = "Look";
            public const string Vertical = "Vertical";
            public const string Boost = "Boost";
            public const string Inspect = "Inspect";
            public const string Apply = "Apply";
            public const string Cancel = "Cancel";
            public const string GlitchWheel = "GlitchWheel";
            public const string ToggleInspection = "ToggleInspection";
            public const string Revert = "Revert";
            public const string Pause = "Pause";

            public static readonly string[] All =
            {
                Move, Look, Vertical, Boost, Inspect, Apply, Cancel, GlitchWheel, ToggleInspection, Revert, Pause,
            };
        }

        [SerializeField] private InputActionAsset actions;

        private InputActionAsset runtimeActions;
        private InputActionMap map;
        private InputAction move, look, vertical, boost, inspect, apply, cancel, glitchWheel, toggleInspection, revert, pause;

        public event Action InspectPerformed;
        public event Action ApplyPerformed;
        public event Action CancelPerformed;
        public event Action GlitchWheelPerformed;
        public event Action ToggleInspectionPerformed;
        public event Action RevertPerformed;
        public event Action PausePerformed;

        public bool IsBound => map != null;

        /// <summary>Planar move, x = strafe, y = forward, magnitude ≤ 1.</summary>
        public Vector2 Move => move != null ? Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f) : Vector2.zero;
        /// <summary>Ascend (+1) / descend (-1).</summary>
        public float Vertical => vertical != null ? Mathf.Clamp(vertical.ReadValue<float>(), -1f, 1f) : 0f;
        public bool Boost => boost != null && boost.IsPressed();

        /// <summary>
        /// Look input this frame. Pointer deltas are per-frame distances; stick values
        /// are rates. <paramref name="fromPointer"/> tells the camera which one it got.
        /// </summary>
        public Vector2 ReadLook(out bool fromPointer)
        {
            fromPointer = false;
            if (look == null) return Vector2.zero;
            var control = look.activeControl;
            fromPointer = control != null && control.device is Pointer;
            return look.ReadValue<Vector2>();
        }

        private void Awake()
        {
            if (actions == null)
            {
                Debug.LogError("[BUG REPORT] InputReader has no InputActionAsset assigned.", this);
                return;
            }

            // Work on a copy so enabling/rebinding at runtime never dirties the project asset.
            runtimeActions = Instantiate(actions);
            map = runtimeActions.FindActionMap(AnomalyMapName, throwIfNotFound: false);
            if (map == null)
            {
                Debug.LogError("[BUG REPORT] Input asset '" + actions.name + "' has no '" + AnomalyMapName + "' action map.", this);
                return;
            }

            move = Find(Actions.Move);
            look = Find(Actions.Look);
            vertical = Find(Actions.Vertical);
            boost = Find(Actions.Boost);
            inspect = Find(Actions.Inspect);
            apply = Find(Actions.Apply);
            cancel = Find(Actions.Cancel);
            glitchWheel = Find(Actions.GlitchWheel);
            toggleInspection = Find(Actions.ToggleInspection);
            revert = Find(Actions.Revert);
            pause = Find(Actions.Pause);
        }

        private void OnEnable()
        {
            if (map == null) return;
            Subscribe(inspect, OnInspect);
            Subscribe(apply, OnApply);
            Subscribe(cancel, OnCancel);
            Subscribe(glitchWheel, OnGlitchWheel);
            Subscribe(toggleInspection, OnToggleInspection);
            Subscribe(revert, OnRevert);
            Subscribe(pause, OnPause);
            map.Enable();
        }

        private void OnDisable()
        {
            if (map == null) return;
            map.Disable();
            Unsubscribe(inspect, OnInspect);
            Unsubscribe(apply, OnApply);
            Unsubscribe(cancel, OnCancel);
            Unsubscribe(glitchWheel, OnGlitchWheel);
            Unsubscribe(toggleInspection, OnToggleInspection);
            Unsubscribe(revert, OnRevert);
            Unsubscribe(pause, OnPause);
        }

        private void OnDestroy()
        {
            if (runtimeActions != null) Destroy(runtimeActions);
        }

        private InputAction Find(string actionName)
        {
            var action = map.FindAction(actionName, throwIfNotFound: false);
            if (action == null) Debug.LogError("[BUG REPORT] Missing input action '" + AnomalyMapName + "/" + actionName + "'.", this);
            return action;
        }

        private static void Subscribe(InputAction action, Action<InputAction.CallbackContext> handler)
        {
            if (action != null) action.performed += handler;
        }

        private static void Unsubscribe(InputAction action, Action<InputAction.CallbackContext> handler)
        {
            if (action != null) action.performed -= handler;
        }

        private void OnInspect(InputAction.CallbackContext _) => InspectPerformed?.Invoke();
        private void OnApply(InputAction.CallbackContext _) => ApplyPerformed?.Invoke();
        private void OnCancel(InputAction.CallbackContext _) => CancelPerformed?.Invoke();
        private void OnGlitchWheel(InputAction.CallbackContext _) => GlitchWheelPerformed?.Invoke();
        private void OnToggleInspection(InputAction.CallbackContext _) => ToggleInspectionPerformed?.Invoke();
        private void OnRevert(InputAction.CallbackContext _) => RevertPerformed?.Invoke();
        private void OnPause(InputAction.CallbackContext _) => PausePerformed?.Invoke();
    }
}
