using System.Collections.Generic;
using UnityEngine;

namespace BugReport.Hero
{
    /// <summary>Authored waypoint on the floor. Links are undirected; listing a link on either end is enough.</summary>
    [DisallowMultipleComponent]
    public sealed class HeroNavNode : MonoBehaviour
    {
        [SerializeField] private HeroNavNode[] links = new HeroNavNode[0];

        public IReadOnlyList<HeroNavNode> Links => links;

        public void Configure(params HeroNavNode[] newLinks) => links = newLinks ?? new HeroNavNode[0];

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, 0.2f);
            foreach (var other in links)
            {
                if (other != null) Gizmos.DrawLine(transform.position, other.transform.position);
            }
        }
    }
}
