using UnityEngine;

namespace BugReport.Glitches
{
    /// <summary>Marks an entity as duplicable and points at the profile (and prefab) its copies come from.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GameEntity))]
    public sealed class DuplicationCapability : MonoBehaviour
    {
        [SerializeField] private DuplicationProfile profile;

        public DuplicationProfile Profile => profile;

        public void Configure(DuplicationProfile newProfile) => profile = newProfile;
    }
}
