using UnityEngine;
using Sirenix.OdinInspector;

namespace WildernessSurvival.World
{
    /// <summary>
    /// Type categories for Map Stamps.
    /// </summary>
    public enum StampType
    {
        [LabelText("🏠 Core")]
        Core,
        
        [LabelText("🌲 Forest")]
        Forest,
        
        [LabelText("🪨 Rock")]
        Rock,
        
        [LabelText("💧 Water")]
        Water,
        
        [LabelText("🌾 Open Field")]
        OpenField
    }

    /// <summary>
    /// Component attached to Map Stamp prefab roots.
    /// Defines the stamp's metadata and visual bounds.
    /// </summary>
    public class MapStamp : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════════
        // STAMP DATA
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Stamp Info")]
        [EnumToggleButtons]
        [Tooltip("Category of this stamp")]
        public StampType stampType = StampType.Forest;

        [TitleGroup("Stamp Info")]
        [Tooltip("Size of the stamp area (X = width, Y = depth)")]
        public Vector2 size = new Vector2(20f, 20f);

        [TitleGroup("Stamp Info")]
        [Tooltip("Height of the stamp bounds for visualization")]
        [Range(1f, 20f)]
        public float height = 5f;

        [TitleGroup("Properties")]
        [Tooltip("Does this stamp have blocking collision?")]
        public bool hasCollision = true;

        [TitleGroup("Properties")]
        [Tooltip("Can structures be built within this stamp?")]
        public bool allowBuilding = true;

        [TitleGroup("Properties")]
        [ReadOnly]
        [Tooltip("Number of child objects in this stamp")]
        public int objectCount;

        // ═══════════════════════════════════════════════════════════════════
        // GIZMOS
        // ═══════════════════════════════════════════════════════════════════

        private void OnDrawGizmos()
        {
            DrawBoundsGizmo(0.8f);
        }

        private void OnDrawGizmosSelected()
        {
            DrawBoundsGizmo(1f);
            DrawChildIndicators();
        }

        /// <summary>
        /// Draws the stamp boundary wireframe.
        /// </summary>
        private void DrawBoundsGizmo(float alpha)
        {
            Color color = GetGizmoColor();
            color.a = alpha;
            Gizmos.color = color;

            // Calculate bounds center (offset upward by half height)
            Vector3 center = transform.position + Vector3.up * (height / 2f);
            Vector3 boundsSize = new Vector3(size.x, height, size.y);

            // Draw wireframe cube
            Gizmos.DrawWireCube(center, boundsSize);

            // Draw filled bottom with low alpha
            Color fillColor = color;
            fillColor.a = 0.1f;
            Gizmos.color = fillColor;
            Gizmos.DrawCube(center, boundsSize);
        }

        /// <summary>
        /// Draws small spheres at child positions when selected.
        /// </summary>
        private void DrawChildIndicators()
        {
            Gizmos.color = Color.cyan;
            foreach (Transform child in transform)
            {
                Gizmos.DrawWireSphere(child.position, 0.3f);
            }
        }

        /// <summary>
        /// Gets the gizmo color based on stamp type.
        /// </summary>
        private Color GetGizmoColor()
        {
            return stampType switch
            {
                StampType.Core => new Color(1f, 0.8f, 0.2f),      // Gold
                StampType.Forest => new Color(0.2f, 0.8f, 0.3f),  // Green
                StampType.Rock => new Color(0.6f, 0.6f, 0.6f),    // Grey
                StampType.Water => new Color(0.3f, 0.6f, 1f),     // Blue
                StampType.OpenField => new Color(0.9f, 0.9f, 0.5f), // Light Yellow
                _ => Color.yellow
            };
        }

        // ═══════════════════════════════════════════════════════════════════
        // UTILITY
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Updates the object count from children.
        /// </summary>
        [Button("🔄 Update Object Count")]
        public void UpdateObjectCount()
        {
            objectCount = transform.childCount;
        }

        /// <summary>
        /// Calculates the actual bounds of all children.
        /// </summary>
        [Button("📐 Auto-Calculate Size")]
        public void AutoCalculateSize()
        {
            if (transform.childCount == 0)
            {
                Debug.LogWarning("[MapStamp] No children to calculate bounds from.");
                return;
            }

            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            bool hasRenderer = false;

            foreach (Transform child in transform)
            {
                Renderer renderer = child.GetComponent<Renderer>();
                if (renderer != null)
                {
                    if (!hasRenderer)
                    {
                        bounds = renderer.bounds;
                        hasRenderer = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }
                else
                {
                    bounds.Encapsulate(child.position);
                }
            }

            if (hasRenderer)
            {
                size = new Vector2(bounds.size.x, bounds.size.z);
                height = bounds.size.y;
                Debug.Log($"[MapStamp] Auto-calculated size: {size}, height: {height:F1}");
            }
        }

        private void OnValidate()
        {
            UpdateObjectCount();
        }
    }
}
