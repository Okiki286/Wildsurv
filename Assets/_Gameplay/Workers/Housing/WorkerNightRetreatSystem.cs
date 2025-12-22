using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using WildernessSurvival.Core.Events;
using WildernessSurvival.Core.Systems;
using WildernessSurvival.Core.Navigation;
using WildernessSurvival.Gameplay.Structures;
using WildernessSurvival.Gameplay.Structures.Housing;
using WildernessSurvival.Gameplay.Workers;

namespace WildernessSurvival.Gameplay.Workers.Housing
{
    /// <summary>
    /// Sistema centrale che gestisce il ritiro notturno dei worker.
    /// - OnNightStarted: manda i worker a casa o al Waystone
    /// - OnDayStarted: fa uscire i worker dalle case
    /// - Gestisce la distruzione delle case durante la notte
    ///
    /// MOBILE-FRIENDLY: nessuna allocazione in Update, usa event-driven approach.
    /// </summary>
    public class WorkerNightRetreatSystem : MonoBehaviour
    {
        // ============================================
        // SINGLETON
        // ============================================

        public static WorkerNightRetreatSystem Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            Instance = null;
        }

        // ============================================
        // CONFIGURATION
        // ============================================

        [TitleGroup("Events")]
        [SerializeField]
        [Required]
        [Tooltip("GameEvent raised when night starts")]
        private GameEvent onNightStarted;

        [SerializeField]
        [Required]
        [Tooltip("GameEvent raised when day starts")]
        private GameEvent onDayStarted;

        [TitleGroup("Waystone Retreat Settings")]
        [SerializeField]
        [Range(1f, 5f)]
        [Tooltip("Minimum distance from Waystone center for homeless workers")]
        private float waystoneMinRadius = 1.5f;

        [SerializeField]
        [Range(2f, 8f)]
        [Tooltip("Maximum distance from Waystone center for homeless workers")]
        private float waystoneMaxRadius = 3f;

        [TitleGroup("Waystone Retreat Settings")]
        [SerializeField]
        [Tooltip("Enable ApproachSlots for Waystone to prevent worker blocking")]
        private bool useWaystoneSlots = true;

        [SerializeField]
        [Range(4, 12)]
        [Tooltip("Number of slots around Waystone for homeless workers")]
        private int waystoneSlotCount = 8;

        [TitleGroup("Debug")]
        [SerializeField]
        private bool debugMode = true;

        // ============================================
        // RUNTIME STATE
        // ============================================

        [TitleGroup("Runtime Status")]
        [ShowInInspector, ReadOnly]
        private bool isNightTime = false;

        [ShowInInspector, ReadOnly]
        [ListDrawerSettings(IsReadOnly = true, ShowPaging = false)]
        private List<ShelterHome> registeredShelters = new List<ShelterHome>();

        // Cached lists for processing (avoid GC)
        private readonly List<WorkerInstance> workersToProcess = new List<WorkerInstance>(64);
        private readonly List<WorkerInstance> homelessWorkers = new List<WorkerInstance>(32);
        private readonly List<WorkerInstance> workersWithHomes = new List<WorkerInstance>(32);

        // Track workers retreating to waystone (for potential future use)
        private readonly HashSet<WorkerInstance> workersRetreatingToWaystone = new HashSet<WorkerInstance>();

        // Waystone ApproachSlots (created dynamically on first use)
        private ApproachSlots waystoneSlots;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[WorkerNightRetreatSystem] Duplicate instance destroyed!");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            // Subscribe to day/night events
            if (onNightStarted != null)
            {
                onNightStarted.AddListener(HandleNightStarted);
            }
            if (onDayStarted != null)
            {
                onDayStarted.AddListener(HandleDayStarted);
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            if (onNightStarted != null)
            {
                onNightStarted.RemoveListener(HandleNightStarted);
            }
            if (onDayStarted != null)
            {
                onDayStarted.RemoveListener(HandleDayStarted);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ============================================
        // SHELTER REGISTRATION
        // ============================================

        /// <summary>
        /// Register a shelter with this system. Called by ShelterHome.Start().
        /// </summary>
        public void RegisterShelter(ShelterHome shelter)
        {
            if (shelter == null) return;
            if (registeredShelters.Contains(shelter)) return;

            registeredShelters.Add(shelter);

            // Subscribe to shelter destruction event
            shelter.OnShelterDestroyedWithOccupants += HandleShelterDestroyedWithOccupants;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
            {
                Debug.Log($"<color=cyan>[NightRetreat]</color> Registered shelter: {shelter.name} (total: {registeredShelters.Count})");
            }
#endif
        }

        /// <summary>
        /// Unregister a shelter from this system. Called by ShelterHome.OnDestroy().
        /// </summary>
        public void UnregisterShelter(ShelterHome shelter)
        {
            if (shelter == null) return;

            shelter.OnShelterDestroyedWithOccupants -= HandleShelterDestroyedWithOccupants;
            registeredShelters.Remove(shelter);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
            {
                Debug.Log($"<color=orange>[NightRetreat]</color> Unregistered shelter: {shelter.name} (total: {registeredShelters.Count})");
            }
#endif
        }

        // ============================================
        // NIGHT STARTED HANDLER
        // ============================================

        private void HandleNightStarted()
        {
            isNightTime = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
            {
                Debug.Log("<color=blue>[NightRetreat]</color> === NIGHT STARTED - Processing worker retreat ===");
            }
#endif

            // Clear cached lists
            workersToProcess.Clear();
            homelessWorkers.Clear();
            workersWithHomes.Clear();

            // Get all workers from WorkerSystem
            if (WorkerSystem.Instance == null)
            {
                Debug.LogWarning("[WorkerNightRetreatSystem] WorkerSystem not found!");
                return;
            }

            // Collect all worker instances (both available and assigned)
            workersToProcess.AddRange(WorkerSystem.Instance.GetAvailableWorkers());
            workersToProcess.AddRange(WorkerSystem.Instance.GetAssignedWorkers());

            // Sort workers into categories
            foreach (var worker in workersToProcess)
            {
                if (worker == null) continue;

                // Skip downed workers
                if (worker.PhysicalWorker != null)
                {
                    var downedStatus = worker.PhysicalWorker.GetComponent<WorkerDownedStatus>();
                    if (downedStatus != null && downedStatus.IsDowned)
                    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        if (debugMode)
                        {
                            Debug.Log($"<color=gray>[NightRetreat]</color> Skipping {worker.CustomName}: DOWNED");
                        }
#endif
                        continue;
                    }
                }

                // Check if worker has assigned home
                if (worker.AssignedHome != null && worker.AssignedHome.IsOperational)
                {
                    workersWithHomes.Add(worker);
                }
                else
                {
                    homelessWorkers.Add(worker);
                }
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
            {
                Debug.Log($"<color=cyan>[NightRetreat]</color> Workers with homes: {workersWithHomes.Count}, Homeless: {homelessWorkers.Count}");
            }
#endif

            // Process workers with homes - send to shelter
            foreach (var worker in workersWithHomes)
            {
                SendWorkerToShelter(worker);
            }

            // Process homeless workers - send to Waystone
            foreach (var worker in homelessWorkers)
            {
                SendWorkerToWaystone(worker);
            }
        }

        // ============================================
        // DAY STARTED HANDLER
        // ============================================

        private void HandleDayStarted()
        {
            isNightTime = false;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
            {
                Debug.Log("<color=yellow>[NightRetreat]</color> === DAY STARTED - Waking up workers ===");
            }
#endif

            // ════════════════════════════════════════════════════════════════════
            // CRITICAL FIX: Collect workers who need to return BEFORE ejection
            // EjectAllOccupants resets state to Idle, so we must identify returning
            // workers while they're still in Sheltered/Retreating state
            // ════════════════════════════════════════════════════════════════════
            
            var workersToReturn = new List<WorkerInstance>();

            if (WorkerSystem.Instance != null)
            {
                workersToProcess.Clear();
                workersToProcess.AddRange(WorkerSystem.Instance.GetAvailableWorkers());
                workersToProcess.AddRange(WorkerSystem.Instance.GetAssignedWorkers());

                foreach (var worker in workersToProcess)
                {
                    if (worker == null) continue;

                    // Identify workers who should return to work
                    if (worker.CurrentState == WorkerState.Sheltered || worker.CurrentState == WorkerState.Retreating)
                    {
                        if (worker.AssignedStructure != null && worker.AssignedStructure.gameObject != null)
                        {
                            // This worker has a valid worksite to return to
                            workersToReturn.Add(worker);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                            if (debugMode)
                            {
                                Debug.Log($"<color=cyan>[NightRetreat]</color> {worker.CustomName} will return to {worker.AssignedStructure.Data?.DisplayName}");
                            }
#endif
                        }
                    }
                }
            }

            // Exit all workers from shelters (this resets their state to Idle)
            foreach (var shelter in registeredShelters)
            {
                if (shelter != null)
                {
                    shelter.EjectAllOccupants(isDestructionEject: false);
                }
            }

            // Release all Waystone slots before clearing the set
            ReleaseAllWaystoneSlots();

            // Clear retreating workers set
            workersRetreatingToWaystone.Clear();

            // ════════════════════════════════════════════════════════════════════
            // NOW send workers back to their worksites
            // We use the list we collected BEFORE ejection
            // ════════════════════════════════════════════════════════════════════
            foreach (var worker in workersToReturn)
            {
                if (worker == null) continue;

                // Double-check structure is still valid
                if (worker.AssignedStructure != null && worker.AssignedStructure.gameObject != null)
                {
                    // Structure still valid - go back to work
                    worker.SetState(WorkerState.Moving);
                    worker.IsAtWorksite = false;

                    if (worker.PhysicalWorker != null)
                    {
                        // Use slot-based work position to prevent blocking
                        Vector3 workPos = worker.AssignedStructure.GetWorkPositionForWorker(worker);
                        worker.PhysicalWorker.CommandMoveTo(workPos);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        if (debugMode)
                        {
                            Debug.Log($"<color=green>[NightRetreat]</color> {worker.CustomName} returning to {worker.AssignedStructure.Data?.DisplayName}");
                        }
#endif
                    }
                }
                else
                {
                    // Structure destroyed or invalid - become idle
                    worker.SetState(WorkerState.Idle);

                    if (!worker.IsAssigned)
                    {
                        WorkerSystem.Instance.NotifyWorkerBecameIdleBuilder(worker);
                    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (debugMode)
                    {
                        Debug.Log($"<color=orange>[NightRetreat]</color> {worker.CustomName} structure no longer valid, set to Idle");
                    }
#endif
                }
            }
        }

        // ============================================
        // SHELTER DESTRUCTION HANDLER
        // ============================================

        private void HandleShelterDestroyedWithOccupants(List<WorkerInstance> ejectedWorkers)
        {
            if (!isNightTime) return; // Only handle during night
            if (ejectedWorkers == null || ejectedWorkers.Count == 0) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
            {
                Debug.Log($"<color=red>[NightRetreat]</color> Shelter destroyed with {ejectedWorkers.Count} workers inside! Retreating to Waystone...");
            }
#endif

            // Send all ejected workers to Waystone
            foreach (var worker in ejectedWorkers)
            {
                if (worker != null)
                {
                    SendWorkerToWaystone(worker);
                }
            }
        }

        // ============================================
        // WORKER MOVEMENT COMMANDS
        // ============================================

        private void SendWorkerToShelter(WorkerInstance worker)
        {
            if (worker == null || worker.AssignedHome == null) return;

            var shelter = worker.AssignedHome;
            if (!shelter.IsOperational)
            {
                // Shelter not operational, send to Waystone instead
                SendWorkerToWaystone(worker);
                return;
            }

            // Save current job before retreat (for restoration at day)
            worker.SaveJobBeforeRetreat();

            // Cancel current job safely
            CancelWorkerJobSafely(worker);

            // Set state to retreating
            worker.SetState(WorkerState.Retreating);

            // Command physical worker to move to shelter using slot-based entry
            if (worker.PhysicalWorker != null)
            {
                var controller = worker.PhysicalWorker;

                // Use slot-based entry position if available
                Vector3 targetPos = shelter.GetEntryPositionForWorker(worker);

                // Use NavMesh sample to ensure valid position
                if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                {
                    targetPos = hit.position;
                }

                controller.CommandMoveToShelter(targetPos, shelter);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (debugMode)
                {
                    Debug.Log($"<color=green>[NightRetreat]</color> {worker.CustomName} going to shelter: {shelter.name}");
                }
#endif
            }
        }

        private void SendWorkerToWaystone(WorkerInstance worker)
        {
            if (worker == null) return;

            // Save current job before retreat
            worker.SaveJobBeforeRetreat();

            // Cancel current job safely
            CancelWorkerJobSafely(worker);

            // Set state to retreating
            worker.SetState(WorkerState.Retreating);
            workersRetreatingToWaystone.Add(worker);

            // Get Waystone position
            Vector3 waystonePos = Vector3.zero;
            Transform waystoneTransform = null;

            if (BaseCenterSystem.Instance != null && BaseCenterSystem.Instance.HasCenter)
            {
                waystonePos = BaseCenterSystem.Instance.CenterPosition;
                waystoneTransform = BaseCenterSystem.Instance.CurrentCenter;
            }
            else
            {
                // Fallback: search for WaystoneBeaconController
                var beacon = FindAnyObjectByType<WildernessSurvival.Gameplay.Core.WaystoneBeaconController>();
                if (beacon != null)
                {
                    waystonePos = beacon.transform.position;
                    waystoneTransform = beacon.transform;
                }
                else
                {
                    Debug.LogWarning("[WorkerNightRetreatSystem] No Waystone found! Worker will stay in place.");
                    return;
                }
            }

            // Calculate target position - use slots if enabled
            Vector3 targetPos;
            if (useWaystoneSlots)
            {
                targetPos = GetWaystoneSlotPosition(worker, waystonePos, waystoneTransform);
            }
            else
            {
                targetPos = GetRandomPositionAroundWaystone(waystonePos);
            }

            // Command physical worker to move
            if (worker.PhysicalWorker != null)
            {
                worker.PhysicalWorker.CommandMoveToRetreat(targetPos);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (debugMode)
                {
                    Debug.Log($"<color=yellow>[NightRetreat]</color> {worker.CustomName} retreating to Waystone area");
                }
#endif
            }
        }

        /// <summary>
        /// Gets a slot position around the Waystone for the worker.
        /// Creates ApproachSlots if not already existing.
        /// </summary>
        private Vector3 GetWaystoneSlotPosition(WorkerInstance worker, Vector3 waystonePos, Transform waystoneTransform)
        {
            // Initialize waystone slots if needed
            if (waystoneSlots == null)
            {
                InitializeWaystoneSlots(waystonePos, waystoneTransform);
            }

            if (waystoneSlots != null)
            {
                if (waystoneSlots.TryReserveSlot(worker, out Vector3 slotPos))
                {
                    // Validate on NavMesh
                    if (NavMesh.SamplePosition(slotPos, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                    {
                        return hit.position;
                    }
                    return slotPos;
                }

                // All slots full, use fallback queue position
                Vector3 fallbackPos = waystoneSlots.GetFallbackQueuePos(worker);
                if (NavMesh.SamplePosition(fallbackPos, out NavMeshHit fallbackHit, 3f, NavMesh.AllAreas))
                {
                    return fallbackHit.position;
                }
                return fallbackPos;
            }

            // Fallback to random if slots failed
            return GetRandomPositionAroundWaystone(waystonePos);
        }

        /// <summary>
        /// Initializes the ApproachSlots for the Waystone.
        /// </summary>
        private void InitializeWaystoneSlots(Vector3 waystonePos, Transform waystoneTransform)
        {
            // Create a new GameObject to hold the ApproachSlots
            var slotsGO = new GameObject("WaystoneRetreatSlots");
            slotsGO.transform.position = waystonePos;

            if (waystoneTransform != null)
            {
                slotsGO.transform.SetParent(waystoneTransform, true);
            }
            else
            {
                slotsGO.transform.SetParent(transform, false);
            }

            waystoneSlots = slotsGO.AddComponent<ApproachSlots>();
            waystoneSlots.Initialize(waystoneSlotCount, waystoneMaxRadius);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
            {
                Debug.Log($"<color=cyan>[NightRetreat]</color> Created Waystone ApproachSlots with {waystoneSlotCount} slots at radius {waystoneMaxRadius}");
            }
#endif
        }

        /// <summary>
        /// Releases all Waystone slots for workers who were retreating.
        /// Called at day start.
        /// </summary>
        private void ReleaseAllWaystoneSlots()
        {
            if (waystoneSlots != null)
            {
                foreach (var worker in workersRetreatingToWaystone)
                {
                    if (worker != null)
                    {
                        waystoneSlots.ReleaseSlot(worker);
                    }
                }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (debugMode)
                {
                    Debug.Log($"<color=cyan>[NightRetreat]</color> Released {workersRetreatingToWaystone.Count} Waystone slots");
                }
#endif
            }
        }

        private Vector3 GetRandomPositionAroundWaystone(Vector3 waystonePos)
        {
            // Generate random angle
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float radius = Random.Range(waystoneMinRadius, waystoneMaxRadius);

            Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Vector3 targetPos = waystonePos + offset;

            // Sample NavMesh to get valid position
            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                return hit.position;
            }

            // Fallback: try center
            if (NavMesh.SamplePosition(waystonePos, out hit, 5f, NavMesh.AllAreas))
            {
                return hit.position;
            }

            return waystonePos;
        }

        private void CancelWorkerJobSafely(WorkerInstance worker)
        {
            if (worker == null) return;

            // ════════════════════════════════════════════════════════════════════
            // NIGHT RETREAT: Do NOT unassign worker from structure!
            // Just mark IsAtWorksite = false so production/build doesn't count them.
            // Assignment is preserved; worker returns automatically at day start.
            // ════════════════════════════════════════════════════════════════════

            // [NEW] HARDENING: Notify structure BEFORE setting IsAtWorksite = false
            // This ensures build/production speed drops to 0 during night
            if (worker.IsAtWorksite && worker.AssignedStructure != null)
            {
                worker.AssignedStructure.OnWorkerDepartedFromSite();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (debugMode)
                {
                    Debug.Log($"<color=yellow>[NightRetreat]</color> {worker.CustomName} departing from {worker.AssignedStructure.name} for night");
                }
#endif
            }

            worker.IsAtWorksite = false;

            // Stop physical movement
            if (worker.PhysicalWorker != null)
            {
                worker.PhysicalWorker.StopMovement();
            }
        }

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Called by WorkerController when worker arrives at shelter.
        /// </summary>
        public void NotifyWorkerArrivedAtShelter(WorkerInstance worker, ShelterHome shelter)
        {
            if (worker == null || shelter == null) return;

            if (shelter.EnterWorker(worker))
            {
                worker.SetState(WorkerState.Sheltered);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (debugMode)
                {
                    Debug.Log($"<color=green>[NightRetreat]</color> {worker.CustomName} entered shelter: {shelter.name}");
                }
#endif
            }
        }

        /// <summary>
        /// Check if we're currently in night time.
        /// </summary>
        public bool IsNightTime => isNightTime;

        // ============================================
        // DEBUG
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug Actions")]
        [Button("Force Night Retreat", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.5f, 0.9f)]
        private void DebugForceNightRetreat()
        {
            if (Application.isPlaying)
            {
                HandleNightStarted();
            }
        }

        [Button("Force Day Wakeup", ButtonSizes.Large)]
        [GUIColor(1f, 0.9f, 0.4f)]
        private void DebugForceDayWakeup()
        {
            if (Application.isPlaying)
            {
                HandleDayStarted();
            }
        }

        [Button("Log Status", ButtonSizes.Medium)]
        private void DebugLogStatus()
        {
            Debug.Log($"[WorkerNightRetreatSystem] Status:\n" +
                $"  Is Night: {isNightTime}\n" +
                $"  Registered Shelters: {registeredShelters.Count}\n" +
                $"  Workers Retreating to Waystone: {workersRetreatingToWaystone.Count}");

            foreach (var shelter in registeredShelters)
            {
                Debug.Log($"  - {shelter.name}: {shelter.OccupantCount}/{shelter.Capacity} inside, {shelter.ResidentCount} residents");
            }
        }
#endif
    }
}
