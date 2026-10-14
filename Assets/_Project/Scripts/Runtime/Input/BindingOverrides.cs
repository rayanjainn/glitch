using System;
using System.Collections.Generic;
using BugReport.Progress;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BugReport.InputHandling
{
    /// <summary>
    /// Keyboard/mouse rebinding. Overrides live in the save file (Input System JSON) and are applied
    /// to runtime copies of the actions asset (never to the project asset). Only single keys and
    /// composite parts listed in <see cref="Rebindables"/> can be changed; gamepad bindings stay fixed.
    /// </summary>
    public static class BindingOverrides
    {
        public const string KeyboardGroup = "Keyboard&Mouse";
        public const string MapName = "Anomaly";

        public readonly struct Rebindable
        {
            public readonly string Label;
            public readonly string Action;
            /// <summary>Composite binding name (e.g. "WASD"), or null for a plain binding.</summary>
            public readonly string Composite;
            /// <summary>Composite part name (e.g. "up").</summary>
            public readonly string Part;

            public Rebindable(string label, string action, string composite = null, string part = null)
            {
                Label = label;
                Action = action;
                Composite = composite;
                Part = part;
            }
        }

        public static readonly Rebindable[] Rebindables =
        {
            new Rebindable("MOVE FORWARD", "Move", "WASD", "up"),
            new Rebindable("MOVE BACK", "Move", "WASD", "down"),
            new Rebindable("MOVE LEFT", "Move", "WASD", "left"),
            new Rebindable("MOVE RIGHT", "Move", "WASD", "right"),
            new Rebindable("FLY UP", "Vertical", "Space/Ctrl", "positive"),
            new Rebindable("FLY DOWN", "Vertical", "Space/Ctrl", "negative"),
            new Rebindable("BOOST", "Boost"),
            new Rebindable("SELECT", "Inspect"),
            new Rebindable("APPLY GLITCH", "Apply"),
            new Rebindable("UNDO LAST GLITCH", "Revert"),
            new Rebindable("SWITCH GLITCH", "GlitchWheel"),
            new Rebindable("INSPECT MODE", "ToggleInspection"),
        };

        /// <summary>Replaces any overrides on <paramref name="asset"/> with the saved ones.</summary>
        public static void ApplyTo(InputActionAsset asset)
        {
            if (asset == null) return;
            asset.RemoveAllBindingOverrides();
            var json = SaveStore.Current.bindingOverrides;
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                asset.LoadBindingOverridesFromJson(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BUG REPORT] Saved key bindings could not be loaded; using defaults (" + e.Message + ").");
                asset.RemoveAllBindingOverrides();
            }
        }

        /// <summary>Stores <paramref name="asset"/>'s overrides in the save and tells running scenes.</summary>
        public static void SaveFrom(InputActionAsset asset)
        {
            if (asset == null) return;
            SaveStore.Current.bindingOverrides = asset.SaveBindingOverridesAsJson();
            SaveStore.ApplySettings();
            SaveStore.Flush();
        }

        public static void ResetAll(InputActionAsset asset)
        {
            if (asset != null) asset.RemoveAllBindingOverrides();
            SaveStore.Current.bindingOverrides = string.Empty;
            SaveStore.ApplySettings();
            SaveStore.Flush();
        }

        public static InputAction FindAction(InputActionAsset asset, Rebindable r)
        {
            var map = asset != null ? asset.FindActionMap(MapName, throwIfNotFound: false) : null;
            return map != null ? map.FindAction(r.Action, throwIfNotFound: false) : null;
        }

        /// <summary>Index of the keyboard/mouse binding (or composite part) to rebind; -1 if missing.</summary>
        public static int BindingIndex(InputAction action, Rebindable r)
        {
            if (action == null) return -1;
            IReadOnlyList<InputBinding> bindings = action.bindings;
            if (r.Composite == null)
            {
                for (int i = 0; i < bindings.Count; i++)
                {
                    var b = bindings[i];
                    if (!b.isComposite && !b.isPartOfComposite && HasGroup(b, KeyboardGroup)) return i;
                }
                return -1;
            }
            for (int i = 0; i < bindings.Count; i++)
            {
                if (!bindings[i].isComposite || bindings[i].name != r.Composite) continue;
                for (int j = i + 1; j < bindings.Count && bindings[j].isPartOfComposite; j++)
                    if (bindings[j].name == r.Part) return j;
            }
            return -1;
        }

        /// <summary>Short, upper-case label of the current key ("E", "LEFT BUTTON").</summary>
        public static string KeyLabel(InputActionAsset asset, Rebindable r)
        {
            var action = FindAction(asset, r);
            int index = BindingIndex(action, r);
            if (index < 0) return "?";
            var text = action.GetBindingDisplayString(index);
            return string.IsNullOrEmpty(text) ? "?" : text.ToUpperInvariant();
        }

        private static bool HasGroup(InputBinding binding, string group)
            => !string.IsNullOrEmpty(binding.groups) && Array.IndexOf(binding.groups.Split(';'), group) >= 0;
    }
}
