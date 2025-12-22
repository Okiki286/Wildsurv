using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using WildernessSurvival.Gameplay.Workers;

namespace WildernessSurvival.Core.Navigation
{
    /// <summary>
    /// Component that manages approach slots around a pivot point.
    /// Prevents worker blocking by reserving positions in a ring layout.
    /// Attach to ShelterHome (doors), StructureController (worksites), or Waystone (retreat).
    /// </summary>
    public class ApproachSlots : MonoBehaviour
    {
        // ============================================
        // CONFIGURATION
        // ============================================

        [TitleGroup("Slot Configuration")]
        [SerializeField]
        [Range(2, 12)]
        [Tooltip("Number of slots in the ring around the pivot")]
        private int slotCount = 4;

        [TitleGroup("Slot Configuration")]
        [SerializeField]
        [Range(0.3f, 3f)]
        [Tooltip("Radius of the slot ring from pivot")]
        private float slotRadius = 0.8f;

        [TitleGroup("Slot Configuration")]
        [SerializeField]
        [Tooltip("Custom pivot point. If null, uses transform.position")]
        private Transform customPivot;

        [TitleGroup("Slot Configuration")]
        [SerializeField]
        [Range(0f, 360f)]
        [Tooltip("Rotation offset for the first slot (degrees)")]
        private float startAngleOffset = 0f;

        [TitleGroup("Fallback")]
        [SerializeField]
        [Range(1f, 5f)]
        [Tooltip("Distance for fallback queue position when all slots full")]
        private float fallbackQueueDistance = 2f;

        // ============================================
        // RUNTIME STATE
        // ============================================

        // Maps worker InstanceId (string) -> slotIndex
        private Dictionary<string, int> workerToSlot = new Dictionary<string, int>();

        // Tracks which slots are occupied (slotIndex -> worker InstanceId)
        private Dictionary<int, string> slotToWorker = new Dictionary<int, string>();

        // Cached slot positions (calculated once, updated if config changes)
        private Vector3[] cachedSlotPositions;
        private bool slotsDirty = true;

        // ============================================
        // PROPERTIES
        // ============================================

        /// <summary>
        /// The center point around which slots are arranged.
        /// </summary>
        public Vector3 PivotPosition => customPivot != null ? customPivot.position : transform.position;

        /// <summary>
        /// Number of currently occupied slots.
        /// </summary>
        public int OccupiedCount => workerToSlot.Count;

        /// <summary>
        /// Number of available slots.
        /// </summary>
        public int AvailableCount => slotCount - workerToSlot.Count;

        /// <summary>
        /// Whether any slots are available.
        /// </summary>
        public bool HasAvailableSlot => workerToSlot.Count < slotCount;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            RecalculateSlotPositions();
        }

        private void OnValidate()
        {
            slotsDirty = true;
        }

        // ============================================
        // INITIALIZATION
        // ============================================

        /// <summary>
        /// Initializes the ApproachSlots with custom configuration.
        /// Use this when creating ApproachSlots at runtime.
        /// </summary>
        /// <param name="slots">Number of slots in the ring</param>
        /// <param name="radius">Radius of the slot ring</param>
        /// <param name="pivot">Optional custom pivot transform</param>
        /// <param name="angleOffset">Starting angle offset in degrees</param>
        public void Initialize(int slots, float radius, Transform pivot = null, float angleOffset = 0f)
        {
            slotCount = Mathf.Clamp(slots, 2, 12);
            slotRadius = Mathf.Clamp(radius, 0.3f, 3f);
            customPivot = pivot;
            startAngleOffset = angleOffset;
            slotsDirty = true;
            RecalculateSlotPositions();
        }

        // ============================================
        // SLOT POSITION CALCULATION
        // ============================================

        /// <summary>
        /// Recalculates all slot positions in a ring around the pivot.
        /// Call this if pivot or configuration changes at runtime.
        /// </summary>
        public void RecalculateSlotPositions()
        {
            if (cachedSlotPositions == null || cachedSlotPositions.Length != slotCount)
            {
                cachedSlotPositions = new Vector3[slotCount];
            }

            Vector3 pivot = PivotPosition;
            float angleStep = 360f / slotCount;
            float startRad = startAngleOffset * Mathf.Deg2Rad;

            for (int i = 0; i < slotCount; i++)
            {
                float angle = startRad + (i * angleStep * Mathf.Deg2Rad);
                float x = pivot.x + slotRadius * Mathf.Cos(angle);
                float z = pivot.z + slotRadius * Mathf.Sin(angle);
                cachedSlotPositions[i] = new Vector3(x, pivot.y, z);
            }

            slotsDirty = false;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"<color=cyan>[ApproachSlots]</color> {name}: Calculated {slotCount} slots at radius {slotRadius}");
#endif
        }

        /// <summary>
        /// Gets the world position of a specific slot.
        /// </summary>
        public Vector3 GetSlotPosition(int slotIndex)
        {
            if (slotsDirty) RecalculateSlotPositions();

            if (slotIndex < 0 || slotIndex >= slotCount)
            {
                return PivotPosition;
            }

            // Recalculate position relative to current pivot (handles moving objects)
            Vector3 pivot = PivotPosition;
            float angleStep = 360f / slotCount;
            float angle = (startAngleOffset + slotIndex * angleStep) * Mathf.Deg2Rad;
            float x = pivot.x + slotRadius * Mathf.Cos(angle);
            float z = pivot.z + slotRadius * Mathf.Sin(angle);
            return new Vector3(x, pivot.y, z);
        }

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Attempts to reserve a slot for a worker.
        /// Returns true if successful, with the slot position in slotPos.
        /// If the worker already has a slot, returns their existing slot position.
        /// </summary>
        /// <param name="worker">The worker requesting a slot</param>
        /// <param name="slotPos">Output: the reserved slot world position</param>
        /// <returns>True if a slot was reserved or already held</returns>
        public bool TryReserveSlot(WorkerInstance worker, out Vector3 slotPos)
        {
            if (worker == null)
            {
                slotPos = PivotPosition;
                return false;
            }

            string workerId = worker.InstanceId;

            // Check if worker already has a slot
            if (workerToSlot.TryGetValue(workerId, out int existingSlot))
            {
                slotPos = GetSlotPosition(existingSlot);
                return true;
            }

            // Find first available slot
            for (int i = 0; i < slotCount; i++)
            {
                if (!slotToWorker.ContainsKey(i))
                {
                    // Reserve this slot
                    workerToSlot[workerId] = i;
                    slotToWorker[i] = workerId;
                    slotPos = GetSlotPosition(i);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"<color=green>[ApproachSlots]</color> {name}: {worker.CustomName} reserved slot {i} at {slotPos}");
#endif
                    return true;
                }
            }

            // No slots available
            slotPos = PivotPosition;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"<color=orange>[ApproachSlots]</color> {name}: No slots available for {worker.CustomName}");
#endif
            return false;
        }

        /// <summary>
        /// Releases a worker's reserved slot, making it available for others.
        /// Safe to call even if worker has no slot.
        /// </summary>
        /// <param name="worker">The worker releasing their slot</param>
        public void ReleaseSlot(WorkerInstance worker)
        {
            if (worker == null) return;

            string workerId = worker.InstanceId;

            if (workerToSlot.TryGetValue(workerId, out int slotIndex))
            {
                workerToSlot.Remove(workerId);
                slotToWorker.Remove(slotIndex);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"<color=yellow>[ApproachSlots]</color> {name}: {worker.CustomName} released slot {slotIndex}");
#endif
            }
        }

        /// <summary>
        /// Gets a fallback queue position when all slots are full.
        /// Returns a position behind the slot ring, offset based on queue order.
        /// </summary>
        /// <param name="worker">The worker needing a queue position</param>
        /// <returns>A position to wait at while slots are full</returns>
        public Vector3 GetFallbackQueuePos(WorkerInstance worker)
        {
            if (worker == null) return PivotPosition;

            // Use worker instance ID hash to create a consistent offset angle
            int idHash = worker.InstanceId.GetHashCode();
            float angleOffset = (Mathf.Abs(idHash) % 8) * 45f * Mathf.Deg2Rad;

            Vector3 pivot = PivotPosition;
            float queueRadius = slotRadius + fallbackQueueDistance;
            float x = pivot.x + queueRadius * Mathf.Cos(angleOffset);
            float z = pivot.z + queueRadius * Mathf.Sin(angleOffset);

            return new Vector3(x, pivot.y, z);
        }

        /// <summary>
        /// Checks if a specific worker has a reserved slot.
        /// </summary>
        public bool HasReservedSlot(WorkerInstance worker)
        {
            if (worker == null) return false;
            return workerToSlot.ContainsKey(worker.InstanceId);
        }

        /// <summary>
        /// Gets the slot index for a worker, or -1 if no slot reserved.
        /// </summary>
        public int GetWorkerSlotIndex(WorkerInstance worker)
        {
            if (worker == null) return -1;

            if (workerToSlot.TryGetValue(worker.InstanceId, out int slotIndex))
            {
                return slotIndex;
            }
            return -1;
        }

        /// <summary>
        /// Clears all slot reservations. Use when resetting the structure.
        /// </summary>
        public void ClearAllSlots()
        {
            workerToSlot.Clear();
            slotToWorker.Clear();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"<color=orange>[ApproachSlots]</color> {name}: All slots cleared");
#endif
        }

        // ============================================
        // DEBUG
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug")]
        [ShowInInspector, ReadOnly]
        private string SlotStatus => $"{OccupiedCount}/{slotCount} occupied";

        [TitleGroup("Debug")]
        [Button("Log Slot Status", ButtonSizes.Medium)]
        private void DebugLogStatus()
        {
            Debug.Log($"[ApproachSlots] {name}:\n" +
                $"  Slots: {OccupiedCount}/{slotCount} occupied\n" +
                $"  Radius: {slotRadius}\n" +
                $"  Pivot: {PivotPosition}");

            if (workerToSlot.Count > 0)
            {
                Debug.Log("  Reservations:");
                foreach (var kvp in workerToSlot)
                {
                    Debug.Log($"    Worker ID {kvp.Key} -> Slot {kvp.Value}");
                }
            }
        }

        [TitleGroup("Debug")]
        [Button("Force Clear All", ButtonSizes.Medium)]
        [GUIColor(1f, 0.5f, 0.5f)]
        private void DebugForceClear()
        {
            if (Application.isPlaying)
            {
                ClearAllSlots();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (slotsDirty || cachedSlotPositions == null)
            {
                RecalculateSlotPositions();
            }

            Vector3 pivot = PivotPosition;

            // Draw pivot
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(pivot, 0.15f);

            // Draw slot ring
            Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.5f);
            DrawCircle(pivot, slotRadius, 32);

            // Draw individual slots
            for (int i = 0; i < slotCount; i++)
            {
                Vector3 slotPos = GetSlotPosition(i);

                // Check if occupied
                bool occupied = slotToWorker.ContainsKey(i);
                Gizmos.color = occupied ? Color.red : Color.green;
                Gizmos.DrawWireSphere(slotPos, 0.2f);

                // Draw line to pivot
                Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                Gizmos.DrawLine(pivot, slotPos);

                // Draw slot number
#if UNITY_EDITOR
                UnityEditor.Handles.Label(slotPos + Vector3.up * 0.3f, i.ToString());
#endif
            }

            // Draw fallback queue ring
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            DrawCircle(pivot, slotRadius + fallbackQueueDistance, 32);
        }

        private void DrawCircle(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(radius, 0, 0);

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(
                    radius * Mathf.Cos(angle),
                    0,
                    radius * Mathf.Sin(angle)
                );
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }
#endif
    }
}
