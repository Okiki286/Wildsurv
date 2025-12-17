using UnityEngine;
using Sirenix.OdinInspector;
using WildernessSurvival.Core.Systems;
using WildernessSurvival.Gameplay.Structures;

namespace WildernessSurvival.Gameplay.DebugTools
{
    /// <summary>
    /// Auto-finds Waystone target and assigns it to all DebugMoveToTarget components in scene.
    /// Uses priority-based fallback system to find Waystone.
    /// </summary>
    public class DebugWaystoneTargetFinder : MonoBehaviour
    {
        // ============================================
        // SETTINGS
        // ============================================

        [TitleGroup("Settings")]
        [Tooltip("Automatically find and assign target on Start")]
        [SerializeField]
        private bool autoFindOnStart = true;

        [TitleGroup("Settings")]
        [Tooltip("Log detailed info about fallback used")]
        [SerializeField]
        private bool debugMode = true;

        // ============================================
        // RUNTIME STATE
        // ============================================

        [TitleGroup("Runtime")]
        [ShowInInspector, ReadOnly]
        private Transform foundTarget;

        [ShowInInspector, ReadOnly]
        private string fallbackUsed = "None";

        [ShowInInspector, ReadOnly]
        private int assignedCount = 0;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Start()
        {
            if (autoFindOnStart)
            {
                FindAndAssignTarget();
            }
        }

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Find Waystone target using priority fallback, then assign to all DebugMoveToTarget in scene.
        /// </summary>
        [Button("Find and Assign Target", ButtonSizes.Large)]
        [GUIColor(0.4f, 1f, 0.4f)]
        public void FindAndAssignTarget()
        {
            foundTarget = FindWaystoneTarget();

            if (foundTarget != null)
            {
                AssignTargetToAllDebugMovers();
            }
            else
            {
                if (debugMode)
                {
                    UnityEngine.Debug.LogWarning("<color=orange>[DebugTargetFinder]</color> No Waystone target found!");
                }
            }
        }

        // ============================================
        // TARGET FINDING (PRIORITY FALLBACK)
        // ============================================

        private Transform FindWaystoneTarget()
        {
            // ═══════════════════════════════════════════════════════════
            // PRIORITY 1: BaseCenterSystem (if available and has center)
            // ═══════════════════════════════════════════════════════════
            if (BaseCenterSystem.Instance != null && BaseCenterSystem.Instance.HasCenter)
            {
                Transform center = BaseCenterSystem.Instance.CurrentCenter;
                if (center != null)
                {
                    fallbackUsed = "BaseCenterSystem.CurrentCenter";
                    if (debugMode)
                    {
                        UnityEngine.Debug.Log($"<color=cyan>[DebugTargetFinder]</color> Found target via BaseCenterSystem: {center.name}");
                    }
                    return center;
                }
            }

            // ═══════════════════════════════════════════════════════════
            // PRIORITY 2: WaystoneDebuffAura component
            // ═══════════════════════════════════════════════════════════
            var aura = FindAnyObjectByType<WaystoneDebuffAura>();
            if (aura != null)
            {
                fallbackUsed = "WaystoneDebuffAura";
                if (debugMode)
                {
                    UnityEngine.Debug.Log($"<color=cyan>[DebugTargetFinder]</color> Found target via WaystoneDebuffAura: {aura.gameObject.name}");
                }
                return aura.transform;
            }

            // ═══════════════════════════════════════════════════════════
            // PRIORITY 3: StructureController with "Waystone" in name
            // ═══════════════════════════════════════════════════════════
            var allStructures = FindObjectsByType<StructureController>(FindObjectsSortMode.None);
            for (int i = 0; i < allStructures.Length; i++)
            {
                var structure = allStructures[i];
                if (structure != null && structure.name.Contains("Waystone"))
                {
                    fallbackUsed = "StructureController (name contains 'Waystone')";
                    if (debugMode)
                    {
                        UnityEngine.Debug.Log($"<color=cyan>[DebugTargetFinder]</color> Found target via StructureController: {structure.name}");
                    }
                    return structure.transform;
                }
            }

            // ═══════════════════════════════════════════════════════════
            // PRIORITY 4: Any GameObject tagged "Waystone"
            // ═══════════════════════════════════════════════════════════
            try
            {
                GameObject taggedWaystone = GameObject.FindWithTag("Waystone");
                if (taggedWaystone != null)
                {
                    fallbackUsed = "GameObject.FindWithTag('Waystone')";
                    if (debugMode)
                    {
                        UnityEngine.Debug.Log($"<color=cyan>[DebugTargetFinder]</color> Found target via Tag: {taggedWaystone.name}");
                    }
                    return taggedWaystone.transform;
                }
            }
            catch
            {
                // Tag might not exist
            }

            // ═══════════════════════════════════════════════════════════
            // PRIORITY 5: Any GameObject with "Waystone" in name
            // ═══════════════════════════════════════════════════════════
            var allObjects = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            for (int i = 0; i < allObjects.Length; i++)
            {
                if (allObjects[i].name.Contains("Waystone"))
                {
                    fallbackUsed = "GameObject name contains 'Waystone'";
                    if (debugMode)
                    {
                        UnityEngine.Debug.Log($"<color=cyan>[DebugTargetFinder]</color> Found target via name search: {allObjects[i].name}");
                    }
                    return allObjects[i];
                }
            }

            fallbackUsed = "NONE - No target found";
            return null;
        }

        // ============================================
        // ASSIGNMENT
        // ============================================

        private void AssignTargetToAllDebugMovers()
        {
            var allMovers = FindObjectsByType<DebugMoveToTarget>(FindObjectsSortMode.None);
            assignedCount = 0;

            for (int i = 0; i < allMovers.Length; i++)
            {
                var mover = allMovers[i];
                if (mover != null && mover.target == null)
                {
                    mover.SetTarget(foundTarget);
                    assignedCount++;
                }
            }

            if (debugMode)
            {
                UnityEngine.Debug.Log($"<color=cyan>[DebugTargetFinder]</color> Assigned target to {assignedCount} DebugMoveToTarget components (fallback: {fallbackUsed})");
            }
        }

        // ============================================
        // DEBUG
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug Actions")]
        [Button("Log Status", ButtonSizes.Medium)]
        private void DebugLogStatus()
        {
            UnityEngine.Debug.Log($"[DebugTargetFinder] Status:\n" +
                $"  Found Target: {(foundTarget != null ? foundTarget.name : "NULL")}\n" +
                $"  Fallback Used: {fallbackUsed}\n" +
                $"  Assigned Count: {assignedCount}");
        }

        [Button("Re-Find Target", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.8f, 0.4f)]
        private void DebugReFindTarget()
        {
            if (Application.isPlaying) FindAndAssignTarget();
        }
#endif
    }
}
