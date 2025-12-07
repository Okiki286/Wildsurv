using UnityEngine;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WildernessSurvival.Gameplay.Map
{
    /// <summary>
    /// Visual guide that draws concentric rings in the Scene View to represent
    /// the gameplay zones defined in the GDD (Core, Inner, Mid, Outer).
    /// </summary>
    public class MapLayoutGuide : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════════
        // RING RADII
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Gameplay Rings")]
        [BoxGroup("Gameplay Rings/Core")]
        [GUIColor(0.3f, 0.9f, 0.3f)]
        [Range(10f, 50f)]
        [Tooltip("Core safe zone - Bonfire, base building")]
        public float coreRadius = 30f;

        [BoxGroup("Gameplay Rings/Inner")]
        [GUIColor(1f, 1f, 0.3f)]
        [Range(40f, 100f)]
        [Tooltip("Inner ring - Resources, early game")]
        public float innerRingRadius = 70f;

        [BoxGroup("Gameplay Rings/Mid")]
        [GUIColor(1f, 0.6f, 0.2f)]
        [Range(80f, 150f)]
        [Tooltip("Mid ring - Wilderness, exploration")]
        public float midRingRadius = 110f;

        [BoxGroup("Gameplay Rings/Outer")]
        [GUIColor(0.9f, 0.3f, 0.3f)]
        [Range(100f, 200f)]
        [Tooltip("Outer ring - Danger zone, enemies")]
        public float outerRingRadius = 150f;

        // ═══════════════════════════════════════════════════════════════════
        // VISUALIZATION OPTIONS
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Visualization")]
        [Tooltip("Show ring labels in Scene View")]
        public bool showLabels = true;

        [Tooltip("Show filled zones (semi-transparent)")]
        public bool showFill = true;

        [Tooltip("Line thickness for rings")]
        [Range(1f, 5f)]
        public float lineThickness = 2f;

        [Tooltip("Height offset for labels")]
        public float labelHeight = 5f;

        // ═══════════════════════════════════════════════════════════════════
        // COLORS
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Colors")]
        public Color coreColor = new Color(0.2f, 0.9f, 0.2f, 1f);
        public Color innerColor = new Color(1f, 0.9f, 0.2f, 1f);
        public Color midColor = new Color(1f, 0.5f, 0.1f, 1f);
        public Color outerColor = new Color(0.9f, 0.2f, 0.2f, 1f);

        // ═══════════════════════════════════════════════════════════════════
        // GIZMOS
        // ═══════════════════════════════════════════════════════════════════

        private void OnDrawGizmos()
        {
            DrawRingsGizmos(false);
        }

        private void OnDrawGizmosSelected()
        {
            DrawRingsGizmos(true);
        }

        /// <summary>
        /// Draws all ring visualizations.
        /// </summary>
        private void DrawRingsGizmos(bool isSelected)
        {
            Vector3 center = transform.position;
            float alpha = isSelected ? 0.15f : 0.05f;

            #if UNITY_EDITOR
            // Use Handles for better disc drawing in editor
            DrawRingWithHandles(center, outerRingRadius, outerColor, "ENEMIES (Danger)", alpha);
            DrawRingWithHandles(center, midRingRadius, midColor, "WILDERNESS", alpha);
            DrawRingWithHandles(center, innerRingRadius, innerColor, "RESOURCES", alpha);
            DrawRingWithHandles(center, coreRadius, coreColor, "CORE (Safe)", alpha);
            #else
            // Fallback to Gizmos at runtime
            DrawRingWithGizmos(center, outerRingRadius, outerColor);
            DrawRingWithGizmos(center, midRingRadius, midColor);
            DrawRingWithGizmos(center, innerRingRadius, innerColor);
            DrawRingWithGizmos(center, coreRadius, coreColor);
            #endif
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Draws a ring using Handles (editor only).
        /// </summary>
        private void DrawRingWithHandles(Vector3 center, float radius, Color color, string label, float fillAlpha)
        {
            // Draw filled disc
            if (showFill)
            {
                Color fillColor = color;
                fillColor.a = fillAlpha;
                Handles.color = fillColor;
                Handles.DrawSolidDisc(center, Vector3.up, radius);
            }

            // Draw wire disc outline
            Handles.color = color;
            Handles.DrawWireDisc(center, Vector3.up, radius, lineThickness);

            // Draw label
            if (showLabels)
            {
                GUIStyle labelStyle = new GUIStyle();
                labelStyle.normal.textColor = color;
                labelStyle.fontStyle = FontStyle.Bold;
                labelStyle.fontSize = 14;
                labelStyle.alignment = TextAnchor.MiddleCenter;

                Vector3 labelPos = center + new Vector3(radius * 0.7f, labelHeight, radius * 0.7f);
                Handles.Label(labelPos, label, labelStyle);
            }
        }
        #endif

        /// <summary>
        /// Fallback ring drawing using Gizmos.
        /// </summary>
        private void DrawRingWithGizmos(Vector3 center, float radius, Color color)
        {
            Gizmos.color = color;
            
            // Draw circle using line segments
            int segments = 64;
            float angleStep = 360f / segments;
            
            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep * Mathf.Deg2Rad;
                float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;
                
                Vector3 p1 = center + new Vector3(Mathf.Cos(angle1) * radius, 0f, Mathf.Sin(angle1) * radius);
                Vector3 p2 = center + new Vector3(Mathf.Cos(angle2) * radius, 0f, Mathf.Sin(angle2) * radius);
                
                Gizmos.DrawLine(p1, p2);
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // UTILITY
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Gets which zone ring a world position is in.
        /// </summary>
        public MapRingZone GetRingAtPosition(Vector3 worldPosition)
        {
            float distance = Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z), 
                                               new Vector3(worldPosition.x, 0f, worldPosition.z));

            if (distance <= coreRadius) return MapRingZone.Core;
            if (distance <= innerRingRadius) return MapRingZone.Inner;
            if (distance <= midRingRadius) return MapRingZone.Mid;
            return MapRingZone.Outer;
        }

        /// <summary>
        /// Checks if a position is in the safe core zone.
        /// </summary>
        public bool IsInCoreZone(Vector3 worldPosition)
        {
            return GetRingAtPosition(worldPosition) == MapRingZone.Core;
        }

        /// <summary>
        /// Gets a random position within a specific ring.
        /// </summary>
        public Vector3 GetRandomPositionInRing(MapRingZone ring)
        {
            float minRadius, maxRadius;
            
            switch (ring)
            {
                case MapRingZone.Core:
                    minRadius = 0f;
                    maxRadius = coreRadius;
                    break;
                case MapRingZone.Inner:
                    minRadius = coreRadius;
                    maxRadius = innerRingRadius;
                    break;
                case MapRingZone.Mid:
                    minRadius = innerRingRadius;
                    maxRadius = midRingRadius;
                    break;
                case MapRingZone.Outer:
                default:
                    minRadius = midRingRadius;
                    maxRadius = outerRingRadius;
                    break;
            }

            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float radius = Random.Range(minRadius, maxRadius);
            
            return transform.position + new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );
        }

        // ═══════════════════════════════════════════════════════════════════
        // DEBUG
        // ═══════════════════════════════════════════════════════════════════

        [TitleGroup("Debug")]
        [Button("📊 Print Ring Info", ButtonSizes.Medium)]
        private void PrintRingInfo()
        {
            Debug.Log($"=== Map Layout Rings ===\n" +
                $"Core (Safe): 0 - {coreRadius}m\n" +
                $"Inner (Resources): {coreRadius} - {innerRingRadius}m\n" +
                $"Mid (Wilderness): {innerRingRadius} - {midRingRadius}m\n" +
                $"Outer (Danger): {midRingRadius} - {outerRingRadius}m\n" +
                $"Total Map Diameter: {outerRingRadius * 2}m");
        }

        private void OnValidate()
        {
            // Ensure radii are in order
            if (innerRingRadius < coreRadius) innerRingRadius = coreRadius + 10f;
            if (midRingRadius < innerRingRadius) midRingRadius = innerRingRadius + 10f;
            if (outerRingRadius < midRingRadius) outerRingRadius = midRingRadius + 10f;
        }
    }

    /// <summary>
    /// Enum representing the gameplay ring zones.
    /// </summary>
    public enum MapRingZone
    {
        Core,   // Safe, base building
        Inner,  // Resources, early game
        Mid,    // Wilderness, exploration
        Outer   // Danger, enemies
    }
}
