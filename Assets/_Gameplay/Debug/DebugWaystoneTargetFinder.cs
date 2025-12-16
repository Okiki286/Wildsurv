using UnityEngine;
using Sirenix.OdinInspector;
using WildernessSurvival.Core.Systems;
using WildernessSurvival.Gameplay.Structures;

namespace WildernessSurvival.Gameplay.DevTools
{
    /// <summary>
    /// Trova automaticamente il Waystone in scena e lo assegna a tutti i DebugMoveToTarget.
    /// Mettere in scena su un GameObject vuoto (es. "Debug_TargetFinder").
    /// </summary>
    public class DebugWaystoneTargetFinder : MonoBehaviour
    {
        // ============================================
        // SETTINGS
        // ============================================

        [TitleGroup("Settings")]
        [Tooltip("Cerca il target in Start automaticamente")]
        [SerializeField] private bool autoFindOnStart = true;

        [TitleGroup("Settings")]
        [Tooltip("Assegna il target a tutti i DebugMoveToTarget in scena")]
        [SerializeField] private bool assignToAllMovers = true;

        [TitleGroup("Settings")]
        [Tooltip("Delay prima di cercare (per aspettare spawn strutture)")]
        [Range(0f, 5f)]
        [SerializeField] private float searchDelay = 0.5f;

        [TitleGroup("Debug")]
        [SerializeField] private bool debugLogs = true;

        // ============================================
        // RUNTIME STATE
        // ============================================

        [TitleGroup("Runtime Status")]
        [ShowInInspector]
        [ReadOnly]
        private Transform foundTarget;

        [ShowInInspector]
        [ReadOnly]
        private int moversAssigned = 0;

        [ShowInInspector]
        [ReadOnly]
        private string searchMethod = "Not searched yet";

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Start()
        {
            if (autoFindOnStart)
            {
                if (searchDelay > 0f)
                {
                    Invoke(nameof(FindAndAssignTarget), searchDelay);
                }
                else
                {
                    FindAndAssignTarget();
                }
            }
        }

        // ============================================
        // SEARCH LOGIC
        // ============================================

        /// <summary>
        /// Cerca il Waystone e assegna ai movers.
        /// Priorità: BaseCenterSystem -> WaystoneDebuffAura -> StructureController "Waystone"
        /// </summary>
        [Button("Find & Assign Target", ButtonSizes.Large)]
        [GUIColor(0.4f, 1f, 0.4f)]
        public void FindAndAssignTarget()
        {
            foundTarget = null;
            moversAssigned = 0;
            searchMethod = "Searching...";

            // ============================================
            // PRIORITY 1: BaseCenterSystem
            // ============================================
            if (BaseCenterSystem.Instance != null && BaseCenterSystem.Instance.HasCenter)
            {
                var waystoneAura = FindAnyObjectByType<WaystoneDebuffAura>();
                if (waystoneAura != null)
                {
                    foundTarget = waystoneAura.transform;
                    searchMethod = "BaseCenterSystem + WaystoneDebuffAura";
                    if (debugLogs) Log($"Found via BaseCenterSystem: {foundTarget.name}");
                }
            }

            // ============================================
            // PRIORITY 2: WaystoneDebuffAura diretto
            // ============================================
            if (foundTarget == null)
            {
                var aura = FindAnyObjectByType<WaystoneDebuffAura>();
                if (aura != null)
                {
                    foundTarget = aura.transform;
                    searchMethod = "WaystoneDebuffAura (FindObjectOfType)";
                    if (debugLogs) Log($"Found via WaystoneDebuffAura: {foundTarget.name}");
                }
            }

            // ============================================
            // PRIORITY 3: StructureController con nome "Waystone"
            // ============================================
            if (foundTarget == null)
            {
                var structures = FindObjectsByType<StructureController>(FindObjectsSortMode.None);
                foreach (var structure in structures)
                {
                    if (structure.name.ToLower().Contains("waystone"))
                    {
                        foundTarget = structure.transform;
                        searchMethod = "StructureController (name contains 'waystone')";
                        if (debugLogs) Log($"Found via StructureController name: {foundTarget.name}");
                        break;
                    }
                }
            }

            // ============================================
            // PRIORITY 4: Qualsiasi GameObject con nome "Waystone"
            // ============================================
            if (foundTarget == null)
            {
                var allObjects = FindObjectsByType<Transform>(FindObjectsSortMode.None);
                foreach (var t in allObjects)
                {
                    if (t.name.ToLower().Contains("waystone"))
                    {
                        foundTarget = t;
                        searchMethod = "GameObject (name contains 'waystone')";
                        if (debugLogs) Log($"Found via GameObject name: {foundTarget.name}");
                        break;
                    }
                }
            }

            // ============================================
            // RESULT
            // ============================================
            if (foundTarget == null)
            {
                searchMethod = "NOT FOUND";
                LogWarning("No Waystone found in scene! Place a WaystoneBeacon prefab first.");
                return;
            }

            if (assignToAllMovers)
            {
                AssignToAllMovers();
            }
        }

        private void AssignToAllMovers()
        {
            if (foundTarget == null) return;

            var movers = FindObjectsByType<DebugMoveToTarget>(FindObjectsSortMode.None);
            moversAssigned = 0;

            foreach (var mover in movers)
            {
                if (mover.Target == null)
                {
                    mover.Target = foundTarget;
                    moversAssigned++;
                    if (debugLogs) Log($"Assigned target to: {mover.gameObject.name}");
                }
                else if (debugLogs)
                {
                    Log($"{mover.gameObject.name} already has target: {mover.Target.name}");
                }
            }

            if (debugLogs)
            {
                Log($"Total movers assigned: {moversAssigned}/{movers.Length}");
            }
        }

        // ============================================
        // MANUAL SEARCH
        // ============================================

        [TitleGroup("Debug Actions")]
        [Button("Force Re-Search", ButtonSizes.Medium)]
        [GUIColor(1f, 0.8f, 0.4f)]
        private void ForceReSearch()
        {
            var movers = FindObjectsByType<DebugMoveToTarget>(FindObjectsSortMode.None);
            foreach (var mover in movers)
            {
                mover.Target = null;
            }

            FindAndAssignTarget();
        }

        [Button("Log Scene Status", ButtonSizes.Medium)]
        private void LogSceneStatus()
        {
            UnityEngine.Debug.Log("<color=cyan>═══════════════════════════════════════════════════════════</color>");
            UnityEngine.Debug.Log("<color=cyan>[DebugTargetFinder] SCENE STATUS</color>");
            UnityEngine.Debug.Log("<color=cyan>═══════════════════════════════════════════════════════════</color>");

            bool hasBaseCenter = BaseCenterSystem.Instance != null && BaseCenterSystem.Instance.HasCenter;
            UnityEngine.Debug.Log($"<color=white>BaseCenterSystem:</color> {(hasBaseCenter ? "<color=green>HAS CENTER</color>" : "<color=gray>No center</color>")}");

            var auras = FindObjectsByType<WaystoneDebuffAura>(FindObjectsSortMode.None);
            UnityEngine.Debug.Log($"<color=white>WaystoneDebuffAura count:</color> {auras.Length}");
            foreach (var aura in auras)
            {
                UnityEngine.Debug.Log($"  - {aura.gameObject.name} (enabled: {aura.enabled})");
            }

            var structures = FindObjectsByType<StructureController>(FindObjectsSortMode.None);
            int waystoneCount = 0;
            foreach (var s in structures)
            {
                if (s.name.ToLower().Contains("waystone"))
                {
                    waystoneCount++;
                    UnityEngine.Debug.Log($"<color=white>StructureController Waystone:</color> {s.name}");
                }
            }
            if (waystoneCount == 0)
            {
                UnityEngine.Debug.Log($"<color=white>StructureController Waystone:</color> <color=gray>None</color>");
            }

            var movers = FindObjectsByType<DebugMoveToTarget>(FindObjectsSortMode.None);
            UnityEngine.Debug.Log($"<color=white>DebugMoveToTarget count:</color> {movers.Length}");
            foreach (var mover in movers)
            {
                string targetName = mover.Target != null ? mover.Target.name : "<color=red>NULL</color>";
                UnityEngine.Debug.Log($"  - {mover.gameObject.name} -> {targetName}");
            }

            UnityEngine.Debug.Log("<color=cyan>═══════════════════════════════════════════════════════════</color>");
        }

        // ============================================
        // LOGGING
        // ============================================

        private void Log(string message)
        {
            UnityEngine.Debug.Log($"<color=lime>[TargetFinder]</color> {message}");
        }

        private void LogWarning(string message)
        {
            UnityEngine.Debug.LogWarning($"<color=orange>[TargetFinder]</color> {message}");
        }

        // ============================================
        // GIZMOS
        // ============================================

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (foundTarget != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(foundTarget.position, 2f);
                Gizmos.DrawLine(transform.position, foundTarget.position);
            }
        }
#endif
    }
}
