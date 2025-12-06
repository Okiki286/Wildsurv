using UnityEngine;
using Sirenix.OdinInspector;

namespace WildernessSurvival.Gameplay.Map
{
    /// <summary>
    /// A point of interest marker placed in the scene by level designers.
    /// Used for spawn points, bonfire locations, and other key positions.
    /// </summary>
    public class MapMarker : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════════

        [BoxGroup("Marker Settings")]
        [EnumToggleButtons]
        [Tooltip("Type of marker")]
        public MapMarkerType type = MapMarkerType.PlayerSpawn;

        [BoxGroup("Marker Settings")]
        [Range(0.5f, 20f)]
        [Tooltip("Radius of influence for this marker")]
        public float radius = 1f;

        [BoxGroup("Marker Settings")]
        [Tooltip("Optional custom ID for this marker")]
        public string markerId = "";

        [BoxGroup("Visualization")]
        [Tooltip("Custom gizmo color (if not using default)")]
        public bool useCustomColor = false;

        [BoxGroup("Visualization")]
        [ShowIf("useCustomColor")]
        public Color gizmoColor = Color.white;

        // ═══════════════════════════════════════════════════════════════════
        // PROPERTIES
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// World position of this marker.
        /// </summary>
        public Vector3 Position => transform.position;

        /// <summary>
        /// Forward direction of this marker (useful for spawn facing).
        /// </summary>
        public Vector3 Forward => transform.forward;

        // ═══════════════════════════════════════════════════════════════════
        // GIZMOS
        // ═══════════════════════════════════════════════════════════════════

        private void OnDrawGizmos()
        {
            Color color = GetGizmoColor();
            
            // Draw solid sphere at center
            Gizmos.color = color;
            Gizmos.DrawSphere(transform.position, 0.3f);

            // Draw wire sphere for radius
            Color wireColor = color;
            wireColor.a = 0.5f;
            Gizmos.color = wireColor;
            Gizmos.DrawWireSphere(transform.position, radius);

            // Draw direction arrow for spawn markers
            if (type == MapMarkerType.PlayerSpawn || type == MapMarkerType.EnemySpawn)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position, transform.forward * 2f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Color color = GetGizmoColor();
            
            // Draw more visible selection
            Color fillColor = color;
            fillColor.a = 0.2f;
            Gizmos.color = fillColor;
            Gizmos.DrawSphere(transform.position, radius);

            // Draw label
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * (radius + 0.5f), 
                $"{type}\n{(string.IsNullOrEmpty(markerId) ? "" : $"ID: {markerId}")}");
            #endif
        }

        /// <summary>
        /// Gets the appropriate gizmo color based on marker type.
        /// </summary>
        private Color GetGizmoColor()
        {
            if (useCustomColor)
                return gizmoColor;

            return type switch
            {
                MapMarkerType.PlayerSpawn => new Color(0.2f, 0.9f, 0.2f), // Green
                MapMarkerType.Bonfire => new Color(1f, 0.6f, 0.1f),       // Orange
                MapMarkerType.EnemySpawn => new Color(0.9f, 0.2f, 0.2f),  // Red
                _ => Color.white
            };
        }

        // ═══════════════════════════════════════════════════════════════════
        // UTILITY
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Checks if a point is within this marker's radius.
        /// </summary>
        public bool IsPointInRadius(Vector3 point)
        {
            return Vector3.Distance(transform.position, point) <= radius;
        }

        /// <summary>
        /// Gets a random point within this marker's radius.
        /// </summary>
        public Vector3 GetRandomPointInRadius()
        {
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            return transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
        }
    }
}
