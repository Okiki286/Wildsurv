using UnityEngine;
using Sirenix.OdinInspector;

namespace WildernessSurvival.Gameplay.Map
{
    /// <summary>
    /// A zone that defines gameplay rules for an area.
    /// Uses a collider trigger to detect if points/objects are within the zone.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MapZone : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════════

        [BoxGroup("Zone Settings")]
        [EnumToggleButtons]
        [Tooltip("Type of zone")]
        public MapZoneType type = MapZoneType.BuildAllowed;

        [BoxGroup("Zone Settings")]
        [Tooltip("Optional custom ID for this zone")]
        public string zoneId = "";

        [BoxGroup("Zone Settings")]
        [Range(0, 10)]
        [Tooltip("Priority when zones overlap (higher = takes precedence)")]
        public int priority = 0;

        [BoxGroup("Visualization")]
        [Tooltip("Custom gizmo color (if not using default)")]
        public bool useCustomColor = false;

        [BoxGroup("Visualization")]
        [ShowIf("useCustomColor")]
        public Color gizmoColor = Color.white;

        // ═══════════════════════════════════════════════════════════════════
        // CACHED REFERENCES
        // ═══════════════════════════════════════════════════════════════════

        private Collider _collider;

        /// <summary>
        /// The zone's collider component.
        /// </summary>
        public Collider ZoneCollider
        {
            get
            {
                if (_collider == null)
                    _collider = GetComponent<Collider>();
                return _collider;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════════════════════════════

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            
            // Ensure collider is a trigger
            if (_collider != null && !_collider.isTrigger)
            {
                _collider.isTrigger = true;
                Debug.LogWarning($"[MapZone] {gameObject.name}: Collider was not set as trigger. Fixed automatically.");
            }
        }

        private void OnValidate()
        {
            // Auto-set collider as trigger in editor
            var col = GetComponent<Collider>();
            if (col != null && !col.isTrigger)
            {
                col.isTrigger = true;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // GIZMOS
        // ═══════════════════════════════════════════════════════════════════

        private void OnDrawGizmos()
        {
            DrawZoneGizmo(0.15f);
        }

        private void OnDrawGizmosSelected()
        {
            DrawZoneGizmo(0.35f);

            // Draw label
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, 
                $"{type}\n{(string.IsNullOrEmpty(zoneId) ? "" : $"ID: {zoneId}")}");
            #endif
        }

        /// <summary>
        /// Draws the zone visualization based on collider type.
        /// </summary>
        private void DrawZoneGizmo(float alpha)
        {
            Color color = GetGizmoColor();
            color.a = alpha;
            Gizmos.color = color;

            var col = GetComponent<Collider>();
            if (col == null) return;

            // Save original matrix
            Matrix4x4 originalMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            if (col is BoxCollider box)
            {
                // Draw filled box
                Gizmos.DrawCube(box.center, box.size);
                
                // Draw wire outline
                color.a = 1f;
                Gizmos.color = color;
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                // Draw filled sphere
                Gizmos.DrawSphere(sphere.center, sphere.radius);
                
                // Draw wire outline
                color.a = 1f;
                Gizmos.color = color;
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
            else if (col is CapsuleCollider capsule)
            {
                // Approximate capsule with spheres at ends
                float halfHeight = capsule.height / 2f - capsule.radius;
                Vector3 topCenter = capsule.center + Vector3.up * halfHeight;
                Vector3 bottomCenter = capsule.center - Vector3.up * halfHeight;
                
                Gizmos.DrawSphere(topCenter, capsule.radius);
                Gizmos.DrawSphere(bottomCenter, capsule.radius);
            }
            else if (col is MeshCollider mesh && mesh.sharedMesh != null)
            {
                // Draw mesh
                Gizmos.DrawMesh(mesh.sharedMesh);
            }

            // Restore matrix
            Gizmos.matrix = originalMatrix;
        }

        /// <summary>
        /// Gets the appropriate gizmo color based on zone type.
        /// </summary>
        private Color GetGizmoColor()
        {
            if (useCustomColor)
                return gizmoColor;

            return type switch
            {
                MapZoneType.BuildAllowed => new Color(0.2f, 0.7f, 0.3f),       // Green
                MapZoneType.NoBuild => new Color(0.9f, 0.2f, 0.2f),            // Red
                MapZoneType.Resource_Wood => new Color(0.6f, 0.4f, 0.2f),      // Brown
                MapZoneType.Resource_Stone => new Color(0.5f, 0.5f, 0.5f),     // Grey
                MapZoneType.Resource_Food => new Color(0.5f, 0.9f, 0.3f),      // Light Green
                _ => Color.white
            };
        }

        // ═══════════════════════════════════════════════════════════════════
        // PUBLIC API
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Checks if a world point is inside this zone.
        /// </summary>
        public bool ContainsPoint(Vector3 worldPoint)
        {
            if (ZoneCollider == null) return false;

            // Use collider bounds for quick check first
            if (!ZoneCollider.bounds.Contains(worldPoint))
                return false;

            // For more accurate check, use ClosestPoint
            Vector3 closest = ZoneCollider.ClosestPoint(worldPoint);
            return Vector3.Distance(closest, worldPoint) < 0.01f;
        }

        /// <summary>
        /// Gets the bounds of this zone.
        /// </summary>
        public Bounds GetBounds()
        {
            return ZoneCollider != null ? ZoneCollider.bounds : new Bounds(transform.position, Vector3.one);
        }

        /// <summary>
        /// Gets a random point inside this zone (approximation using bounds).
        /// </summary>
        public Vector3 GetRandomPointInZone()
        {
            if (ZoneCollider == null) return transform.position;

            Bounds bounds = ZoneCollider.bounds;
            
            // Try up to 10 times to find a point inside the actual collider
            for (int i = 0; i < 10; i++)
            {
                Vector3 randomPoint = new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    Random.Range(bounds.min.y, bounds.max.y),
                    Random.Range(bounds.min.z, bounds.max.z)
                );

                if (ContainsPoint(randomPoint))
                    return randomPoint;
            }

            // Fallback to center
            return bounds.center;
        }
    }
}
