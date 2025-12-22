using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using WildernessSurvival.Gameplay.Workers;
using WildernessSurvival.Gameplay.Workers.Housing;
using WildernessSurvival.Core.Navigation;
using System;

namespace WildernessSurvival.Gameplay.Structures.Housing
{
    /// <summary>
    /// Componente che trasforma una struttura in un rifugio notturno per i worker.
    /// Gestisce capacità, assegnazione residenti, e entry/exit dei worker.
    /// Attaccare a qualsiasi prefab Structure che può ospitare worker di notte.
    /// </summary>
    public class ShelterHome : MonoBehaviour
    {
        // ============================================
        // CONFIGURATION
        // ============================================

        [TitleGroup("Shelter Settings")]
        [SerializeField]
        [Range(1, 10)]
        [Tooltip("Numero massimo di worker che possono risiedere in questo shelter")]
        private int capacity = 2;

        [TitleGroup("Shelter Settings")]
        [SerializeField]
        [Tooltip("Punto dove i worker si spostano quando entrano (opzionale, usa transform.position se null)")]
        private Transform entryPoint;

        [TitleGroup("Shelter Settings")]
        [SerializeField]
        [Tooltip("Punto dove i worker spawneranno se lo shelter viene distrutto (opzionale, usa transform.position se null)")]
        private Transform exitPoint;

        [TitleGroup("Approach Slots")]
        [SerializeField]
        [Tooltip("Optional: ApproachSlots component for managing entrance positions. If null, uses EntryPosition directly.")]
        private ApproachSlots entranceSlots;

        [TitleGroup("Approach Slots")]
        [SerializeField]
        [Tooltip("Auto-create ApproachSlots at runtime if not assigned")]
        private bool autoCreateSlots = true;

        [TitleGroup("Approach Slots")]
        [SerializeField]
        [Range(2, 8)]
        [Tooltip("Number of slots to create if auto-creating")]
        private int autoSlotCount = 4;

        [TitleGroup("Approach Slots")]
        [SerializeField]
        [Range(0.5f, 2f)]
        [Tooltip("Radius for auto-created slots")]
        private float autoSlotRadius = 0.8f;

        // ============================================
        // RUNTIME STATE
        // ============================================

        [TitleGroup("Runtime Status")]
        [ShowInInspector, ReadOnly]
        [ListDrawerSettings(IsReadOnly = true, ShowPaging = false)]
        private List<WorkerInstance> residents = new List<WorkerInstance>();

        [TitleGroup("Runtime Status")]
        [ShowInInspector, ReadOnly]
        [ListDrawerSettings(IsReadOnly = true, ShowPaging = false)]
        private List<WorkerInstance> currentlyInside = new List<WorkerInstance>();

        // Reference to structure controller (for operational check)
        private StructureController structureController;

        // ============================================
        // EVENTS
        // ============================================

        /// <summary>
        /// Invoked when a worker enters the shelter during night.
        /// </summary>
        public event Action<WorkerInstance> OnWorkerEntered;

        /// <summary>
        /// Invoked when a worker exits the shelter (day start or shelter destroyed).
        /// </summary>
        public event Action<WorkerInstance> OnWorkerExited;

        /// <summary>
        /// Invoked when the shelter is destroyed with workers inside.
        /// Passes list of workers that were ejected.
        /// </summary>
        public event Action<List<WorkerInstance>> OnShelterDestroyedWithOccupants;

        // ============================================
        // PROPERTIES
        // ============================================

        public int Capacity => capacity;
        public int ResidentCount => residents.Count;
        public int OccupantCount => currentlyInside.Count;
        public bool HasFreeResidentSlot => residents.Count < capacity;
        public bool IsOperational => structureController != null && structureController.IsOperational;
        public IReadOnlyList<WorkerInstance> Residents => residents;
        public IReadOnlyList<WorkerInstance> CurrentlyInside => currentlyInside;

        /// <summary>
        /// Position where workers should move to when entering shelter (base position without slots).
        /// </summary>
        public Vector3 EntryPosition => entryPoint != null ? entryPoint.position : transform.position;

        /// <summary>
        /// Gets an entry position for a specific worker, using approach slots if available.
        /// Reserves a slot for the worker if one is free.
        /// </summary>
        /// <param name="worker">The worker requesting entry position</param>
        /// <returns>The position to move to (slot position or fallback)</returns>
        public Vector3 GetEntryPositionForWorker(WorkerInstance worker)
        {
            if (entranceSlots != null && worker != null)
            {
                if (entranceSlots.TryReserveSlot(worker, out Vector3 slotPos))
                {
                    return slotPos;
                }
                // All slots full, use fallback queue position
                return entranceSlots.GetFallbackQueuePos(worker);
            }
            return EntryPosition;
        }

        /// <summary>
        /// Releases any entrance slot held by the worker.
        /// Called automatically when worker exits or is unassigned.
        /// </summary>
        public void ReleaseEntranceSlot(WorkerInstance worker)
        {
            if (entranceSlots != null && worker != null)
            {
                entranceSlots.ReleaseSlot(worker);
            }
        }

        /// <summary>
        /// Position where workers spawn when shelter is destroyed.
        /// </summary>
        public Vector3 ExitPosition => exitPoint != null ? exitPoint.position : transform.position;

        /// <summary>
        /// The ApproachSlots component for this shelter, if available.
        /// </summary>
        public ApproachSlots EntranceSlots => entranceSlots;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            structureController = GetComponent<StructureController>();
            InitializeEntranceSlots();
        }

        private void InitializeEntranceSlots()
        {
            // Try to find existing ApproachSlots if not assigned
            if (entranceSlots == null)
            {
                entranceSlots = GetComponent<ApproachSlots>();
            }

            // Auto-create if enabled and still null
            if (entranceSlots == null && autoCreateSlots)
            {
                entranceSlots = gameObject.AddComponent<ApproachSlots>();
                entranceSlots.Initialize(autoSlotCount, autoSlotRadius, entryPoint);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"<color=cyan>[ShelterHome]</color> {name}: Auto-created ApproachSlots with {autoSlotCount} slots");
#endif
            }
        }

        private void Start()
        {
            // [FIX] Don't register Ghost/Preview objects as valid shelters
            if (name.Contains("Ghost") || name.Contains("Preview") || name.Contains("(Clone)_Preview"))
            {
                return;
            }

            // [NEW] Sync residents on start to restore state if needed
            SyncResidents();

            // Register with WorkerNightRetreatSystem if it exists
            if (WorkerNightRetreatSystem.Instance != null)
            {
                WorkerNightRetreatSystem.Instance.RegisterShelter(this);
            }
        }

        private void OnDestroy()
        {
            // Eject all occupants before destruction
            if (currentlyInside.Count > 0)
            {
                EjectAllOccupants(isDestructionEject: true);
            }

            // Clear all resident assignments
            foreach (var worker in residents)
            {
                if (worker != null)
                {
                    worker.AssignedHome = null;
                }
            }
            residents.Clear();

            // Unregister from system
            if (WorkerNightRetreatSystem.Instance != null)
            {
                WorkerNightRetreatSystem.Instance.UnregisterShelter(this);
            }
        }

        // ============================================
        // RESIDENT ASSIGNMENT API
        // ============================================

        /// <summary>
        /// Assigns a worker as a permanent resident of this shelter.
        /// The worker will automatically enter at night if the shelter is operational.
        /// </summary>
        /// <returns>True if assignment successful</returns>
        public bool AssignResident(WorkerInstance worker)
        {
            if (worker == null) return false;
            
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"<color=cyan>[ShelterHome]</color> {name}: AssignResident called for {worker.CustomName}. Current list count: {residents.Count}");
#endif

            if (residents.Contains(worker)) return true; // Already assigned
            if (residents.Count >= capacity)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"<color=orange>[ShelterHome]</color> {name}: Cannot assign {worker.CustomName}, shelter full ({residents.Count}/{capacity})");
#endif
                return false;
            }

            // Unassign from previous home if any
            if (worker.AssignedHome != null && worker.AssignedHome != this)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"<color=yellow>[ShelterHome]</color> {name}: Worker {worker.CustomName} already has home {worker.AssignedHome.name}. Unassigning first.");
#endif
                worker.AssignedHome.UnassignResident(worker);
            }

            residents.Add(worker);
            worker.AssignedHome = this;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"<color=green>[ShelterHome]</color> {name}: {worker.CustomName} assigned successfully. New count: {residents.Count}");
#endif

            return true;
        }

        /// <summary>
        /// Removes a worker from this shelter's resident list.
        /// If worker is currently inside, they will be ejected.
        /// </summary>
        public void UnassignResident(WorkerInstance worker)
        {
            if (worker == null) return;
            
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"<color=orange>[ShelterHome]</color> {name}: UnassignResident called for {worker.CustomName}. List contains: {residents.Contains(worker)}");
#endif

            if (!residents.Contains(worker)) 
            {
                // [FIX] Force clear AssignedHome even if not in list to fix inconsistency bugs
                if (worker.AssignedHome == this)
                {
                    Debug.LogWarning($"<color=red>[ShelterHome]</color> {name}: Worker {worker.CustomName} had this as AssignedHome but was NOT in residents list! Fixing inconsistency.");
                    worker.AssignedHome = null;
                }
                return;
            }

            // If inside, eject first
            if (currentlyInside.Contains(worker))
            {
                ExitWorker(worker);
            }

            residents.Remove(worker);
            if (worker.AssignedHome == this)
            {
                worker.AssignedHome = null;
            }

            // Release any entrance slot the worker may have reserved
            ReleaseEntranceSlot(worker);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"<color=orange>[ShelterHome]</color> {name}: {worker.CustomName} unassigned. Remaining: {residents.Count}");
#endif
        }

        /// <summary>
        /// [NEW] Hard-syncs the residents list with the global worker state.
        /// Fixes inconsistency bugs where workers have AssignedHome set but aren't in the list.
        /// </summary>
        public void SyncResidents()
        {
            if (WorkerSystem.Instance == null) return;

            residents.Clear();
            var allWorkers = WorkerSystem.Instance.AllWorkerInstances;
            foreach (var worker in allWorkers)
            {
                if (worker.AssignedHome == this)
                {
                    residents.Add(worker);
                }
            }
            
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"<color=cyan>[ShelterHome]</color> {name}: Synced residents. Count: {residents.Count}");
#endif
        }

        // ============================================
        // ENTRY/EXIT API (called by WorkerNightRetreatSystem)
        // ============================================

        /// <summary>
        /// Worker enters the shelter (called when night starts and worker reaches entry point).
        /// Hides the worker and marks them as sheltered (non-targetable).
        /// </summary>
        /// <returns>True if entry successful</returns>
        public bool EnterWorker(WorkerInstance worker)
        {
            if (worker == null) return false;
            if (!IsOperational) return false;
            if (currentlyInside.Contains(worker)) return true; // Already inside

            // Must be a resident to enter
            if (!residents.Contains(worker))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"<color=orange>[ShelterHome]</color> {name}: {worker.CustomName} tried to enter but is NOT a resident!");
#endif
                return false;
            }

            currentlyInside.Add(worker);
            worker.SetSheltered(true);

            // Hide the physical worker
            if (worker.PhysicalWorker != null)
            {
                // [NEW] Physical warp inside the building before hiding
                // This ensures the worker isn't left at the external approach slot
                worker.PhysicalWorker.Warp(EntryPosition);
                worker.PhysicalWorker.EnterShelter();
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"<color=green>[ShelterHome]</color> {name}: {worker.CustomName} entered shelter ({currentlyInside.Count} inside)");
#endif

            OnWorkerEntered?.Invoke(worker);
            return true;
        }

        /// <summary>
        /// Worker exits the shelter (called when day starts or shelter destroyed).
        /// Restores worker visibility and targetability.
        /// </summary>
        public void ExitWorker(WorkerInstance worker)
        {
            if (worker == null) return;
            if (!currentlyInside.Contains(worker)) return;

            currentlyInside.Remove(worker);
            worker.SetSheltered(false);

            // Release entrance slot when exiting
            ReleaseEntranceSlot(worker);

            // Show the physical worker
            if (worker.PhysicalWorker != null)
            {
                worker.PhysicalWorker.ExitShelter(ExitPosition);
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"<color=yellow>[ShelterHome]</color> {name}: {worker.CustomName} exited shelter ({currentlyInside.Count} remaining)");
#endif

            OnWorkerExited?.Invoke(worker);
        }

        /// <summary>
        /// Ejects all workers currently inside the shelter.
        /// Used when day starts or when shelter is destroyed.
        /// </summary>
        /// <param name="isDestructionEject">If true, workers will need to retreat to Waystone</param>
        public void EjectAllOccupants(bool isDestructionEject = false)
        {
            if (currentlyInside.Count == 0) return;

            // Copy list to avoid modification during iteration
            var occupantsToEject = new List<WorkerInstance>(currentlyInside);

            foreach (var worker in occupantsToEject)
            {
                if (worker != null)
                {
                    ExitWorker(worker);
                }
            }

            if (isDestructionEject && occupantsToEject.Count > 0)
            {
                OnShelterDestroyedWithOccupants?.Invoke(occupantsToEject);
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"<color=orange>[ShelterHome]</color> {name}: Ejected {occupantsToEject.Count} occupants (destruction: {isDestructionEject})");
#endif
        }

        /// <summary>
        /// Checks if a specific worker is currently inside this shelter.
        /// </summary>
        public bool IsWorkerInside(WorkerInstance worker)
        {
            return currentlyInside.Contains(worker);
        }

        // ============================================
        // DEBUG
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug Actions")]
        [Button("Log Status", ButtonSizes.Medium)]
        private void DebugLogStatus()
        {
            Debug.Log($"[ShelterHome] {name}\n" +
                $"  Capacity: {capacity}\n" +
                $"  Residents: {residents.Count}\n" +
                $"  Currently Inside: {currentlyInside.Count}\n" +
                $"  Is Operational: {IsOperational}");

            if (residents.Count > 0)
            {
                Debug.Log("  Residents:");
                foreach (var r in residents)
                {
                    string inside = currentlyInside.Contains(r) ? " [INSIDE]" : "";
                    Debug.Log($"    - {r?.CustomName ?? "NULL"}{inside}");
                }
            }
        }

        [Button("Force Eject All", ButtonSizes.Medium)]
        [GUIColor(1f, 0.5f, 0.5f)]
        private void DebugForceEjectAll()
        {
            if (Application.isPlaying)
            {
                EjectAllOccupants(isDestructionEject: false);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw entry point
            Vector3 entry = entryPoint != null ? entryPoint.position : transform.position;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(entry, 0.5f);
            Gizmos.DrawLine(transform.position, entry);

            // Draw exit point
            Vector3 exit = exitPoint != null ? exitPoint.position : transform.position + Vector3.right;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(exit, 0.5f);
            Gizmos.DrawLine(transform.position, exit);

            // Draw capacity indicator
            Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.3f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, new Vector3(capacity * 0.5f, 0.5f, 0.5f));
        }
#endif
    }
}
