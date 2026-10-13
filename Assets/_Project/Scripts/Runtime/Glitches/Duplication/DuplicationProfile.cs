using UnityEngine;

namespace BugReport.Glitches
{
    public enum DuplicateObjectivePolicy
    {
        /// <summary>Copies never count for objectives (ObjectiveRelevant is removed). Default.</summary>
        NotObjective,
        /// <summary>Copies keep ObjectiveRelevant: the game can be "confused" by them.</summary>
        Inherit,
    }

    /// <summary>
    /// Designer data for one duplicable object type. A ScriptableObject (not a field on the
    /// object) because a prefab cannot reference itself: Unity remaps a self-reference to the
    /// new instance on Instantiate, so copies would be cloned from the live scene object, the
    /// exact thing duplication must never do.
    /// </summary>
    [CreateAssetMenu(menuName = "BUG REPORT/Duplication Profile", fileName = "DP_New")]
    public sealed class DuplicationProfile : ScriptableObject
    {
        [Tooltip("Prefab asset copies are instantiated from. Never a scene object.")]
        [SerializeField] private GameObject prefab;
        [SerializeField, Min(1)] private int maxCopiesPerSource = 2;
        [SerializeField] private DuplicateObjectivePolicy objectivePolicy = DuplicateObjectivePolicy.NotObjective;
        [Tooltip("Copy the source's current velocity onto the copy.")]
        [SerializeField] private bool inheritVelocity;

        public GameObject Prefab => prefab;
        public int MaxCopiesPerSource => maxCopiesPerSource;
        public DuplicateObjectivePolicy ObjectivePolicy => objectivePolicy;
        public bool InheritVelocity => inheritVelocity;

        /// <summary>Updates only the prefab reference (editor tooling regenerates prefabs; tuning is kept).</summary>
        public void SetPrefab(GameObject newPrefab) => prefab = newPrefab;

        public void Configure(GameObject newPrefab, int newMaxCopiesPerSource = 2,
            DuplicateObjectivePolicy newPolicy = DuplicateObjectivePolicy.NotObjective, bool newInheritVelocity = false)
        {
            prefab = newPrefab;
            maxCopiesPerSource = Mathf.Max(1, newMaxCopiesPerSource);
            objectivePolicy = newPolicy;
            inheritVelocity = newInheritVelocity;
        }
    }
}
