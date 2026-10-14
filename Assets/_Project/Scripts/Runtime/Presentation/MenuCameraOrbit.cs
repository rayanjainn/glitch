using UnityEngine;

namespace BugReport.Presentation
{
    /// <summary>Slow orbit around a point for the title screen. Presentation only.</summary>
    public sealed class MenuCameraOrbit : MonoBehaviour
    {
        [SerializeField] private Vector3 focus = new Vector3(0f, 1f, 0f);
        [SerializeField, Min(0.5f)] private float radius = 5.5f;
        [SerializeField] private float height = 2.2f;
        [SerializeField] private float degreesPerSecond = 6f;
        [SerializeField] private float startAngle = 200f;

        private float angle;

        public void Configure(Vector3 newFocus, float newRadius, float newHeight)
        {
            focus = newFocus;
            radius = newRadius;
            height = newHeight;
        }

        private void Awake()
        {
            angle = startAngle;
            Place();
        }

        private void LateUpdate()
        {
            angle = Mathf.Repeat(angle + degreesPerSecond * Time.unscaledDeltaTime, 360f);
            Place();
        }

        private void Place()
        {
            var offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0f, -radius);
            transform.position = focus + offset + Vector3.up * height;
            transform.LookAt(focus);
        }
    }
}
