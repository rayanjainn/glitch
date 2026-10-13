using BugReport.Hero;
using UnityEngine;

namespace BugReport.Level
{
    /// <summary>Met once the hero's feet are inside the zone box (latches).</summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class HeroReachedZone : ObjectiveCondition
    {
        [SerializeField] private HeroController hero;

        private BoxCollider zone;
        private bool reached;

        public override bool IsMet
        {
            get
            {
                if (reached) return true;
                if (hero == null) return false;
                if (zone == null) zone = GetComponent<BoxCollider>();
                reached = zone.bounds.Contains(hero.Feet + Vector3.up * 0.1f);
                return reached;
            }
        }

        public void Configure(HeroController newHero) => hero = newHero;

        private void Awake()
        {
            zone = GetComponent<BoxCollider>();
            zone.isTrigger = true;
        }
    }
}
