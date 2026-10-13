using System;
using BugReport.Core.Entities;
using BugReport.Core.Glitches;
using BugReport.Core.Interaction;
using UnityEngine;

namespace BugReport.Glitches
{
    /// <summary>
    /// Designer-editable glitch settings (ScriptableObject). One subclass per glitch
    /// type supplies the effect implementation; everything else is data.
    /// </summary>
    public abstract class GlitchDefinition : ScriptableObject
    {
        [Serializable]
        public struct TargetSlot
        {
            [Tooltip("Shown in prompts, e.g. SOURCE / TARGET.")]
            public string label;
            public EntityCapabilities requiredCapabilities;
            public EntityCategory[] excludedCategories;

            public TargetSlot(string label, EntityCapabilities required, params EntityCategory[] excluded)
            {
                this.label = label;
                requiredCapabilities = required;
                excludedCategories = excluded;
            }
        }

        [SerializeField] private string id = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea(2, 4)] private string description = string.Empty;
        [SerializeField] private Sprite icon;
        [SerializeField] private Color color = Color.white;

        [Header("Rules")]
        [Tooltip("Seconds until it restores itself. 0 = until reverted.")]
        [SerializeField, Min(0f)] private float duration;
        [SerializeField, Min(0f)] private float cooldown;
        [Tooltip("Maximum simultaneously active. 0 = unlimited.")]
        [SerializeField, Min(0)] private int maxActive;
        [Tooltip("How suspicious the containment system finds this glitch (Milestone 4).")]
        [SerializeField, Min(0f)] private float containmentWeight = 1f;
        [SerializeField] private TargetSlot[] slots = new TargetSlot[0];

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? Id : displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public Color Color => color;
        public float ContainmentWeight => containmentWeight;
        public int SlotCount => slots != null ? slots.Length : 0;
        public string SlotLabel(int index) => slots[index].label;

        /// <summary>Returns null if the data is usable, otherwise what is wrong.</summary>
        public virtual string ValidateDefinition()
        {
            if (SlotCount == 0) return "has no target slots";
            return null;
        }

        public GlitchSpec ToSpec()
        {
            var requirements = new TargetRequirement[SlotCount];
            for (int i = 0; i < requirements.Length; i++)
            {
                requirements[i] = new TargetRequirement(slots[i].requiredCapabilities, slots[i].excludedCategories ?? new EntityCategory[0]);
            }
            return new GlitchSpec(Id, DisplayName, requirements, duration, cooldown, maxActive);
        }

        public abstract IGlitchEffect CreateEffect(GameServices services);

        /// <summary>Authoring helper for editor tooling and tests.</summary>
        public void Configure(string newId, string newDisplayName, string newDescription, Color newColor,
            float newDuration, float newCooldown, int newMaxActive, TargetSlot[] newSlots)
        {
            id = newId;
            displayName = newDisplayName;
            description = newDescription;
            color = newColor;
            duration = Mathf.Max(0f, newDuration);
            cooldown = Mathf.Max(0f, newCooldown);
            maxActive = Mathf.Max(0, newMaxActive);
            slots = newSlots ?? new TargetSlot[0];
        }
    }
}
