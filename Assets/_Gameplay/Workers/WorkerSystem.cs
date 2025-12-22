using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using WildernessSurvival.Gameplay.Structures;
using WildernessSurvival.Core.Events;
using System.Linq;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// Sistema centrale per la gestione dei worker.
    /// Gestisce spawn, assegnazione, job e tick loop.
    /// </summary>
    public class WorkerSystem : MonoBehaviour
    {
        public static WorkerSystem Instance { get; private set; }

        // ============================================
        // SPAWN CONFIGURATION
        // ============================================

        [TitleGroup("Starting Workers")]
        [SerializeField]
        [InfoBox("Trascina qui i WorkerData (es. Villager) da Assets/_Gameplay/Workers/Data", InfoMessageType.Info)]
        private List<WorkerData> availableWorkerTypes = new List<WorkerData>();

        [SerializeField]
        [Range(1, 10)]
        private int startingWorkerCount = 3;

        // ============================================
        // JOB DATABASE
        // ============================================

        [TitleGroup("Job System")]
        [SerializeField]
        [Tooltip("Riferimento opzionale al JobDatabase. Se vuoto, carica da Resources.")]
        private JobDatabase jobDatabaseOverride;

        [ShowInInspector, ReadOnly]
        private JobDatabase ActiveJobDatabase => jobDatabaseOverride != null ? jobDatabaseOverride : JobDatabase.Instance;

        [ShowInInspector, ReadOnly]
        private bool IsJobSystemEnabled => ActiveJobDatabase != null && ActiveJobDatabase.IsConfigured;

        // ============================================
        // RUNTIME LISTS
        // ============================================

        [TitleGroup("Runtime Lists")]
        [ShowInInspector, ReadOnly]
        private List<StructureController> activeStructures = new List<StructureController>();

        [ShowInInspector, ReadOnly]
        private List<WorkerController> physicalWorkers = new List<WorkerController>();

        [ShowInInspector, ReadOnly]
        private List<WorkerInstance> allWorkerInstances = new List<WorkerInstance>();

        [ShowInInspector, ReadOnly]
        private List<WorkerInstance> availableWorkers = new List<WorkerInstance>();

        [ShowInInspector, ReadOnly]
        private List<WorkerInstance> assignedWorkers = new List<WorkerInstance>();

        // Cached lists to avoid LINQ allocations in auto-assignment
        private readonly List<StructureController> cachedBuildingStructures = new List<StructureController>(32);
        private readonly List<WorkerInstance> cachedWorkersToAssign = new List<WorkerInstance>(64);

        [TitleGroup("Global Settings")]
        [SerializeField] private bool paused = false;

        [TitleGroup("Day Night Events")]
        [SerializeField]
        [Tooltip("Reference to OnDayStarted event from DayNightSystem")]
        private GameEvent onDayStartedEvent;

        [TitleGroup("Debug Settings")]
        [SerializeField] private bool debugAutoAssign = true;
        [SerializeField] private bool debugJobSystem = true;
        [SerializeField] private bool debugForeman = false;

        // ============================================
        // AUTO-ASSIGNMENT (EVENT-DRIVEN)
        // ============================================

        [TitleGroup("Auto-Assignment")]
        [SerializeField]
        [PropertyRange(0.5f, 5f)]
        [Tooltip("Throttling interval - prevents auto-assignment from running too frequently")]
        private float autoAssignInterval = 0.5f;

        private float _nextAutoAssignTime;
        private bool _autoAssignDirty = false;

        // Event-driven queues for Foreman
        private readonly List<StructureController> _pendingBuildStructures = new List<StructureController>();
        private readonly List<WorkerInstance> _idleBuilders = new List<WorkerInstance>();

        // Properties
        public int WorkerInstanceCount => allWorkerInstances.Count;
        public int AvailableWorkerCount => availableWorkers.Count;
        public int AssignedInstanceCount => assignedWorkers.Count;
        public IReadOnlyList<WorkerInstance> AllWorkerInstances => allWorkerInstances;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void OnEnable()
        {
            // Subscribe to DayStarted event
            if (onDayStartedEvent != null)
            {
                onDayStartedEvent.AddListener(RefreshIdleBuildersOnDayStart);
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from DayStarted event
            if (onDayStartedEvent != null)
            {
                onDayStartedEvent.RemoveListener(RefreshIdleBuildersOnDayStart);
            }
        }

        private void OnDestroy()
        {
            _pendingBuildStructures.Clear();
            _idleBuilders.Clear();
            _autoAssignDirty = false;
        }

        // ============================================
        // DAYBREAK REFRESH (GHOST FIX PHASE 2)
        // ============================================

        /// <summary>
        /// Called when day starts. Re-populates the idle builders queue with
        /// any workers who were "forgotten" during the night (shelter oversight fix).
        /// </summary>
        private void RefreshIdleBuildersOnDayStart()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"<color=cyan>[WorkerSystem]</color> Daybreak - Refreshing idle builders queue...");
#endif

            int addedCount = 0;

            foreach (var worker in allWorkerInstances)
            {
                if (worker == null) continue;

                // Skip if worker is sheltered (should not happen at day start, but safety first)
                if (worker.IsSheltered) continue;

                // Skip if worker is already assigned to a structure
                if (worker.IsAssigned) continue;

                // Skip if worker is downed
                if (worker.PhysicalWorker != null)
                {
                    var downedStatus = worker.PhysicalWorker.GetComponent<WorkerDownedStatus>();
                    if (downedStatus != null && downedStatus.IsDowned) continue;
                }

                // If worker is effectively idle but NOT in the queue, add them
                if (!_idleBuilders.Contains(worker))
                {
                    _idleBuilders.Add(worker);
                    addedCount++;
                }
            }

            // Mark auto-assign as dirty if we found new candidates
            if (addedCount > 0)
            {
                _autoAssignDirty = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"<color=green>[WorkerSystem]</color> Daybreak - Added {addedCount} workers to idle queue. Total: {_idleBuilders.Count}");
#endif
            }
        }

        private void Start()
        {
#if UNITY_EDITOR
            if (IsJobSystemEnabled)
            {
                Debug.Log($"<color=green>[WorkerSystem]</color> Job System ENABLED. Database has {ActiveJobDatabase.AllJobs?.Count ?? 0} jobs.");
            }
            else
            {
                Debug.LogWarning("[WorkerSystem] Job System DISABLED.");
            }
#endif

            // AUTO-FIND: Try to find day event if not assigned (Stress Test / Zero Setup Fix)
            if (onDayStartedEvent == null)
            {
                TryAutoFindDayEvent();
            }

            SpawnStartingWorkers();
        }

        private void TryAutoFindDayEvent()
        {
            var allEvents = UnityEngine.Resources.FindObjectsOfTypeAll<GameEvent>();
            foreach (var ev in allEvents)
            {
                if (ev.name.Contains("DayStarted") || ev.name.Contains("OnDayStarted"))
                {
                    onDayStartedEvent = ev;
                    onDayStartedEvent.AddListener(RefreshIdleBuildersOnDayStart);
                    Debug.Log($"<color=cyan>[WorkerSystem]</color> Auto-detected Day-Start event: {ev.name}");
                    return;
                }
            }
        }

        private void Update()
        {
            if (paused) return;

            float dt = Time.deltaTime;

            // Structure Tick Loop
            for (int i = activeStructures.Count - 1; i >= 0; i--)
            {
                var structure = activeStructures[i];
                if (structure != null)
                {
                    if (structure.State == StructureState.Building)
                    {
                        structure.TickConstruction(dt);
                    }
                    else if (structure.State == StructureState.Operating)
                    {
                        structure.TickProduction(dt);
                    }
                }
                else
                {
                    activeStructures.RemoveAt(i);
                }
            }

            // Worker Tick Loop
            for (int i = physicalWorkers.Count - 1; i >= 0; i--)
            {
                var worker = physicalWorkers[i];
                if (worker != null)
                {
                    worker.ManualUpdate(dt);
                }
                else
                {
                    physicalWorkers.RemoveAt(i);
                }
            }

            // Auto-Assignment Loop (Event-Driven with Throttling)
            if (Time.time >= _nextAutoAssignTime)
            {
                // FALLBACK PULSE: If we have work but no candidates, do a quick sanity check
                // This recovers workers "lost" due to race conditions or skipped notifications.
                if (_pendingBuildStructures.Count > 0 && _idleBuilders.Count == 0)
                {
                    QuickSanityCheckForIdleBuilders();
                }

                if (_autoAssignDirty)
                {
                    TryAutoAssignBuilders();
                    _nextAutoAssignTime = Time.time + autoAssignInterval;
                }
            }
        }

        /// <summary>
        /// Fallback check specifically for "Stress Test" scenarios where notifications might be missed.
        /// Re-populates the idle builder queue if we have pending work but no idle builders.
        /// </summary>
        private void QuickSanityCheckForIdleBuilders()
        {
            bool foundAny = false;
            foreach (var worker in allWorkerInstances)
            {
                if (worker == null || worker.IsAssigned || worker.IsSheltered) continue;
                
                // If they are effectively idle but not in queue, add them
                if (!_idleBuilders.Contains(worker))
                {
                    _idleBuilders.Add(worker);
                    foundAny = true;
                }
            }

            if (foundAny)
            {
                _autoAssignDirty = true;
            }
        }

        // ============================================
        // SPAWN LOGIC
        // ============================================

        private void SpawnStartingWorkers()
        {
            if (availableWorkerTypes == null || availableWorkerTypes.Count == 0)
            {
#if UNITY_EDITOR
                Debug.LogError("❌ [WorkerSystem] No WorkerData in 'Available Worker Types'!");
#endif
                return;
            }

#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[WorkerSystem]</color> Spawning {startingWorkerCount} starting workers...");
#endif

            for (int i = 0; i < startingWorkerCount; i++)
            {
                try
                {
                    var typeToSpawn = availableWorkerTypes[i % availableWorkerTypes.Count];
                    if (typeToSpawn != null)
                    {
                        CreateWorkerInstance(typeToSpawn);
                        Debug.Log($"<color=green>[WorkerSystem]</color> Successfully spawned worker {i + 1}/{startingWorkerCount}");
                    }
                    else
                    {
                        Debug.LogWarning($"[WorkerSystem] Worker type at index {i % availableWorkerTypes.Count} is null!");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[WorkerSystem] Exception spawning worker {i + 1}: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        public WorkerInstance CreateWorkerInstance(WorkerData data)
        {
            if (data == null) return null;

            WorkerInstance newWorker = new WorkerInstance(data);
            allWorkerInstances.Add(newWorker);
            availableWorkers.Add(newWorker);

            bool spawnSuccessful = false;

            if (data.Prefab != null)
            {
                Vector3 spawnPos = GetRandomSpawnPosition();
                GameObject workerObj = Instantiate(data.Prefab, spawnPos, Quaternion.identity);
                workerObj.name = $"Worker_{data.DisplayName}_{newWorker.InstanceId.Substring(0, 4)}";


                WorkerController controller = workerObj.GetComponent<WorkerController>();
                if (controller != null)
                {
                    newWorker.PhysicalWorker = controller;
                    controller.LinkToInstance(newWorker);
                    RegisterWorker(controller);
                    spawnSuccessful = true;

#if UNITY_EDITOR
                    Debug.Log($"<color=green>[WorkerSystem]</color> Spawned {data.DisplayName} at {spawnPos}");
#endif
                }
                else
                {
                    Debug.LogError($"[WorkerSystem] Prefab {data.Prefab.name} missing WorkerController!");
                }
            }

            // Notify Foreman: newly spawned worker is available as idle builder (event-driven)
            // ONLY if spawn was successful (has valid PhysicalWorker)
            if (spawnSuccessful && !newWorker.IsAssigned && newWorker.PhysicalWorker != null)
            {
                NotifyWorkerBecameIdleBuilder(newWorker);
            }

            return newWorker;
        }

        private Vector3 GetRandomSpawnPosition()
        {
            Vector3 center = Vector3.zero;
            Vector3 randomPoint = center + Random.insideUnitSphere * 5f;
            randomPoint.y = 0;

            if (UnityEngine.AI.NavMesh.SamplePosition(randomPoint, out UnityEngine.AI.NavMeshHit hit, 5.0f, UnityEngine.AI.NavMesh.AllAreas))
            {
                return hit.position;
            }
            return center;
        }

        // ============================================
        // WORKER ASSIGNMENT
        // ============================================

        public List<WorkerInstance> GetAvailableWorkers() => new List<WorkerInstance>(availableWorkers);
        public List<WorkerInstance> GetAssignedWorkers() => new List<WorkerInstance>(assignedWorkers);

        public List<WorkerInstance> GetWorkersAtStructure(StructureController structure)
        {
            return assignedWorkers.FindAll(w => w.AssignedStructure == structure);
        }

        /// <summary>
        /// Assegna un worker a una struttura.
        /// Determina il job appropriato in base allo stato della struttura.
        /// </summary>
        public bool AssignWorker(WorkerInstance worker, StructureController structure)
        {
            if (worker == null || structure == null) return false;

            // Gate: block assignment when worker is downed
            if (worker.PhysicalWorker != null)
            {
                var downedStatus = worker.PhysicalWorker.GetComponent<WorkerDownedStatus>();
                if (downedStatus != null && downedStatus.IsDowned)
                {
#if UNITY_EDITOR
                    Debug.Log($"<color=orange>[WorkerSystem]</color> AssignWorker blocked: {worker.CustomName} is DOWNED");
#endif
                    return false;
                }
            }

            if (worker.IsAssigned) UnassignWorker(worker);

            if (structure.AssignWorker(worker))
            {
                availableWorkers.Remove(worker);
                if (!assignedWorkers.Contains(worker)) assignedWorkers.Add(worker);

                // Notify Foreman that worker is no longer idle (event-driven)
                NotifyWorkerNoLongerIdleBuilder(worker);

                // ═══════════════════════════════════════════════════════════
                // DYNAMIC JOB ASSIGNMENT
                // ═══════════════════════════════════════════════════════════
                AssignJobForStructure(worker, structure);

                return true;
            }
            return false;
        }

        /// <summary>
        /// Assegna il job appropriato in base allo stato della struttura.
        /// Building -> Builder
        /// Operating -> Ruolo della struttura
        /// </summary>
        private void AssignJobForStructure(WorkerInstance worker, StructureController structure)
        {
            if (!IsJobSystemEnabled) return;

            WorkerRole targetRole;

            if (structure.State == StructureState.Building)
            {
                targetRole = WorkerRole.Builder;
                if (debugJobSystem)
                {
                    Debug.Log($"<color=orange>[WorkerSystem]</color> Structure BUILDING - {worker.CustomName} -> BUILDER (pending)");
                }
            }
            else
            {
                targetRole = GetPrimaryRoleFromStructure(structure.Data);
                if (debugJobSystem)
                {
                    Debug.Log($"<color=cyan>[WorkerSystem]</color> Structure OPERATING - {worker.CustomName} -> {targetRole} (pending)");
                }
            }

            WorkerJobData jobData = ActiveJobDatabase.GetJobData(targetRole);
            if (jobData != null)
            {
                // Set as PENDING job - il cambio visual avverrà quando il worker arriva alla struttura
                worker.PendingJob = jobData;
                if (debugJobSystem)
                {
                    Debug.Log($"<color=magenta>[WorkerSystem]</color> {worker.CustomName} pending job: {jobData.JobName} (visual will change on arrival)");
                }
            }
            else
            {
                Debug.LogWarning($"[WorkerSystem] No job found for role {targetRole}");
            }
        }

        private WorkerRole GetPrimaryRoleFromStructure(StructureData structureData)
        {
            if (structureData == null) return WorkerRole.None;

            WorkerRole roles = structureData.AllowedRoles;

            if ((roles & WorkerRole.Gatherer) != 0) return WorkerRole.Gatherer;
            if ((roles & WorkerRole.Builder) != 0) return WorkerRole.Builder;
            if ((roles & WorkerRole.Guard) != 0) return WorkerRole.Guard;
            if ((roles & WorkerRole.Scout) != 0) return WorkerRole.Scout;
            if ((roles & WorkerRole.Crafter) != 0) return WorkerRole.Crafter;
            if ((roles & WorkerRole.Researcher) != 0) return WorkerRole.Researcher;

            return WorkerRole.None;
        }

        /// <summary>
        /// Disassegna un worker.
        /// ORDINE CRITICO:
        /// 1. Rimuovi dalla struttura
        /// 2. Aggiorna liste
        /// 3. Reset job a Villager
        /// 4. ForceIdle mantenendo pending visual
        /// </summary>
        public void UnassignWorker(WorkerInstance worker)
        {
            if (worker == null) return;

            // [NEW] HARDENING: Notify structure BEFORE detaching if worker was at worksite
            // This ensures build/production speed is recalculated even in edge cases:
            // - Manual unassign via UI
            // - Worker downed/death
            // - Night retreat
            // - Structure destroyed
            // - Swap to new structure
            if (worker.IsAtWorksite && worker.AssignedStructure != null)
            {
                var structure = worker.AssignedStructure;
                structure.OnWorkerDepartedFromSite();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"<color=yellow>[WorkerSystem]</color> {worker.CustomName} departed from {structure.name} " +
                    $"(was at worksite, triggering recalculation)");
#endif
            }

            // 1. Rimuovi dalla struttura
            if (worker.AssignedStructure != null)
            {
                worker.AssignedStructure.RemoveWorker(worker);
            }
            else
            {
                worker.Unassign();
            }

            // 2. Aggiorna liste
            assignedWorkers.Remove(worker);
            if (!availableWorkers.Contains(worker)) availableWorkers.Add(worker);

            // 3. Reset job a Villager
            if (IsJobSystemEnabled)
            {
                var defaultJob = ActiveJobDatabase.GetDefaultJob();
                if (defaultJob != null)
                {
                    worker.SetJob(defaultJob);
                    if (debugJobSystem)
                    {
                        Debug.Log($"<color=cyan>[WorkerSystem]</color> {worker.CustomName} reset to: {defaultJob.JobName}");
                    }
                }
            }

            // 4. ForceIdle mantenendo pending visual
            if (worker.PhysicalWorker != null)
            {
                worker.PhysicalWorker.ForceIdleKeepPendingVisual();
            }

            // 5. Notify Foreman if worker can build (event-driven)
            if (!worker.IsAssigned)
            {
                NotifyWorkerBecameIdleBuilder(worker);
            }

#if UNITY_EDITOR
            Debug.Log($"<color=orange>[WorkerSystem]</color> {worker.CustomName} unassigned and reset.");
#endif
        }

        public void UnassignAllWorkersFromStructure(StructureController structure)
        {
            if (structure == null) return;

            // OPTIMIZATION: Manual loop instead of LINQ to avoid GC allocations
            cachedWorkersToAssign.Clear();
            for (int i = 0; i < assignedWorkers.Count; i++)
            {
                var worker = assignedWorkers[i];
                if (worker != null && worker.AssignedStructure == structure)
                {
                    cachedWorkersToAssign.Add(worker);
                }
            }

            for (int i = 0; i < cachedWorkersToAssign.Count; i++)
            {
                UnassignWorker(cachedWorkersToAssign[i]);
            }

#if UNITY_EDITOR
            if (cachedWorkersToAssign.Count > 0)
            {
                Debug.Log($"<color=orange>[WorkerSystem]</color> Unassigned {cachedWorkersToAssign.Count} workers from {structure.name}");
            }
#endif

            cachedWorkersToAssign.Clear();
        }

        // ============================================
        // AUTO-ASSIGNMENT (EVENT-DRIVEN)
        // ============================================

        /// <summary>
        /// Event-driven auto-assignment. Only called when structures or workers are queued.
        /// Throttled by autoAssignInterval to prevent excessive calls.
        /// </summary>
        private void TryAutoAssignBuilders()
        {
            _autoAssignDirty = false;

            // Clean up invalid entries in queues
            for (int i = _pendingBuildStructures.Count - 1; i >= 0; i--)
            {
                var structure = _pendingBuildStructures[i];
                if (structure == null || structure.State != StructureState.Building || !structure.HasFreeWorkerSlot())
                {
                    _pendingBuildStructures.RemoveAt(i);
                }
            }

            for (int i = _idleBuilders.Count - 1; i >= 0; i--)
            {
                var worker = _idleBuilders[i];
                // [GHOST FIX] Skip null, already assigned, or sheltered workers
                if (worker == null || worker.IsAssigned || worker.IsSheltered)
                {
                    _idleBuilders.RemoveAt(i);
                }
            }

            if (_pendingBuildStructures.Count == 0 || _idleBuilders.Count == 0)
            {
                if (debugForeman)
                {
                    Debug.Log($"<color=orange>[WorkerSystem]</color> Foreman: no work to do (structures={_pendingBuildStructures.Count}, idleBuilders={_idleBuilders.Count})");
                }
                return;
            }

            int assignments = 0;

            // Simple 1:1 assignment: match structures with available idle builders
            for (int i = _pendingBuildStructures.Count - 1; i >= 0; i--)
            {
                var structure = _pendingBuildStructures[i];
                if (structure == null || structure.State != StructureState.Building)
                {
                    _pendingBuildStructures.RemoveAt(i);
                    continue;
                }

                // Assign workers while structure has free slots and we have idle builders
                while (structure.HasFreeWorkerSlot() && _idleBuilders.Count > 0)
                {
                    var worker = _idleBuilders[_idleBuilders.Count - 1];
                    _idleBuilders.RemoveAt(_idleBuilders.Count - 1);

                    if (worker == null || worker.IsAssigned)
                        continue;

                    // Use existing AssignWorker logic
                    if (AssignWorker(worker, structure))
                    {
                        assignments++;
                        if (debugForeman)
                        {
                            Debug.Log($"<color=green>[WorkerSystem]</color> Foreman auto-assigned {worker.CustomName} to {structure.name}");
                        }
                    }
                }

                // If structure is full, remove from queue
                if (!structure.HasFreeWorkerSlot())
                {
                    _pendingBuildStructures.RemoveAt(i);
                }
            }

            if (debugForeman)
            {
                Debug.Log($"<color=orange>[WorkerSystem]</color> Foreman: {assignments} assignments, remaining structures={_pendingBuildStructures.Count}, idleBuilders={_idleBuilders.Count}");
            }

            // If there's still work to be done, keep dirty flag set
            if (_pendingBuildStructures.Count > 0 && _idleBuilders.Count > 0)
            {
                _autoAssignDirty = true;
            }
        }

        // ============================================
        // FOREMAN EVENT NOTIFICATIONS (PUBLIC API)
        // ============================================

        /// <summary>
        /// Register a structure that needs builders (called when entering Building state).
        /// </summary>
        public void RegisterStructureNeedingBuilders(StructureController structure)
        {
            if (structure == null)
                return;

            if (!_pendingBuildStructures.Contains(structure))
            {
                _pendingBuildStructures.Add(structure);
                _autoAssignDirty = true;

                if (debugForeman)
                {
                    Debug.Log($"<color=cyan>[WorkerSystem]</color> Foreman: Structure {structure.name} registered for builders");
                }
            }
        }

        /// <summary>
        /// Unregister a structure that no longer needs builders (completed/destroyed).
        /// </summary>
        public void UnregisterStructureNeedingBuilders(StructureController structure)
        {
            if (structure == null)
                return;

            if (_pendingBuildStructures.Remove(structure))
            {
                if (debugForeman)
                {
                    Debug.Log($"<color=cyan>[WorkerSystem]</color> Foreman: Structure {structure.name} unregistered from builders queue");
                }
            }
        }

        /// <summary>
        /// Notify that a worker became idle and is available as a builder.
        /// </summary>
        public void NotifyWorkerBecameIdleBuilder(WorkerInstance worker)
        {
            if (worker == null)
                return;

            // Gate: don't add downed workers to idle builder queue
            if (worker.PhysicalWorker != null)
            {
                var downedStatus = worker.PhysicalWorker.GetComponent<WorkerDownedStatus>();
                if (downedStatus != null && downedStatus.IsDowned)
                {
                    return;
                }
            }

            if (!_idleBuilders.Contains(worker))
            {
                _idleBuilders.Add(worker);
                _autoAssignDirty = true;

                if (debugForeman)
                {
                    Debug.Log($"<color=yellow>[WorkerSystem]</color> Foreman: Worker {worker.CustomName} is now idle builder");
                }
            }
        }

        /// <summary>
        /// Notify that a worker is no longer an idle builder (assigned or changed role).
        /// </summary>
        public void NotifyWorkerNoLongerIdleBuilder(WorkerInstance worker)
        {
            if (worker == null)
                return;

            if (_idleBuilders.Remove(worker))
            {
                if (debugForeman)
                {
                    Debug.Log($"<color=yellow>[WorkerSystem]</color> Foreman: Worker {worker.CustomName} no longer idle builder");
                }
            }
        }

        // ============================================
        // HOUSING ASSIGNMENT API
        // ============================================

        /// <summary>
        /// Assigns a worker to a shelter as their permanent home.
        /// Worker will automatically retreat to this shelter at night.
        /// </summary>
        /// <param name="worker">Worker to assign</param>
        /// <param name="shelter">Shelter to assign as home</param>
        /// <returns>True if assignment successful</returns>
        public bool AssignWorkerHome(WorkerInstance worker, WildernessSurvival.Gameplay.Structures.Housing.ShelterHome shelter)
        {
            if (worker == null || shelter == null) return false;

            return shelter.AssignResident(worker);
        }

        /// <summary>
        /// Removes a worker's home assignment.
        /// Worker will retreat to Waystone at night instead.
        /// </summary>
        public void UnassignWorkerHome(WorkerInstance worker)
        {
            if (worker == null) return;

            if (worker.AssignedHome != null)
            {
                worker.AssignedHome.UnassignResident(worker);
            }
        }

        // ============================================
        // LEGACY AUTO-ASSIGNMENT (DEPRECATED)
        // ============================================

        /// <summary>
        /// Legacy polling-based auto-assignment (DEPRECATED - use event-driven system).
        /// Kept for backward compatibility but not called by Update anymore.
        /// </summary>
        [System.Obsolete("Use event-driven system with RegisterStructureNeedingBuilders/NotifyWorkerBecameIdleBuilder instead")]
        private void CheckAutoAssignments()
        {
            // Clear cached lists
            cachedBuildingStructures.Clear();
            cachedWorkersToAssign.Clear();

            // 1) Collect structures that are in Building state and have free worker slots
            for (int i = 0; i < activeStructures.Count; i++)
            {
                var structure = activeStructures[i];
                if (structure == null) continue;
                if (structure.State == StructureState.Building && structure.HasFreeWorkerSlot())
                {
                    cachedBuildingStructures.Add(structure);
                }
            }

            // 2) Collect all available workers that are not currently assigned
            for (int i = 0; i < availableWorkers.Count; i++)
            {
                var worker = availableWorkers[i];
                if (worker == null) continue;
                if (worker.AssignedStructure == null)
                {
                    cachedWorkersToAssign.Add(worker);
                }
            }

            if (debugAutoAssign)
            {
                Debug.Log($"<color=orange>[WorkerSystem]</color> Auto-Assign: " +
                          $"{cachedBuildingStructures.Count} structures, " +
                          $"{cachedWorkersToAssign.Count} available workers");
            }

            if (cachedBuildingStructures.Count == 0 || cachedWorkersToAssign.Count == 0)
                return;

            int workerIndex = 0;

            // 3) Assign workers round-robin to structures that need them
            for (int s = 0; s < cachedBuildingStructures.Count; s++)
            {
                var structure = cachedBuildingStructures[s];
                if (structure == null) continue;

                while (structure.HasFreeWorkerSlot() && workerIndex < cachedWorkersToAssign.Count)
                {
                    var worker = cachedWorkersToAssign[workerIndex];
                    workerIndex++;

                    if (worker == null) continue;

                    if (AssignWorker(worker, structure))
                    {
                        if (debugAutoAssign)
                        {
                            Debug.Log($"<color=green>[WorkerSystem]</color> Auto-assigned {worker.CustomName} to {structure.name}");
                        }
                    }
                }

                if (workerIndex >= cachedWorkersToAssign.Count)
                    break;
            }
        }

        // ============================================
        // REGISTRATION
        // ============================================

        public void RegisterStructure(StructureController structure)
        {
            if (structure != null && !activeStructures.Contains(structure)) activeStructures.Add(structure);
        }

        public void UnregisterStructure(StructureController structure)
        {
            if (activeStructures.Contains(structure)) activeStructures.Remove(structure);
        }

        public void RegisterWorker(WorkerController worker)
        {
            if (worker != null && !physicalWorkers.Contains(worker)) physicalWorkers.Add(worker);
        }

        public void UnregisterWorker(WorkerController worker)
        {
            if (physicalWorkers.Contains(worker)) physicalWorkers.Remove(worker);
        }

        // ============================================
        // DEBUG TOOLS
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Editor Tools")]
        [Button("Spawn Random Worker", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
        private void DebugSpawnRandom()
        {
            if (availableWorkerTypes.Count > 0)
            {
                var randomType = availableWorkerTypes[Random.Range(0, availableWorkerTypes.Count)];
                CreateWorkerInstance(randomType);
            }
        }

        [Button("🛑 Force All Workers Idle", ButtonSizes.Medium), GUIColor(1f, 0.5f, 0.3f)]
        private void DebugForceAllIdle()
        {
            foreach (var worker in physicalWorkers)
            {
                if (worker != null) worker.ForceIdle();
            }
        }

        [Button("📊 Print Worker Status", ButtonSizes.Medium)]
        private void DebugPrintWorkerStatus()
        {
            Debug.Log($"=== WORKER STATUS ===\n" +
                     $"Total: {allWorkerInstances.Count}\n" +
                     $"Available: {availableWorkers.Count}\n" +
                     $"Assigned: {assignedWorkers.Count}");

            foreach (var worker in allWorkerInstances)
            {
                string status = worker.IsAssigned ? $"Assigned to {worker.AssignedStructure?.name}" : "Available";
                string job = worker.CurrentJob != null ? worker.CurrentJob.JobName : "None";
                Debug.Log($"  - {worker.CustomName}: {status} [Job: {job}]");
            }
        }

        [TitleGroup("Job System Debug")]
        [Button("Print Job Database", ButtonSizes.Medium), GUIColor(0.8f, 0.6f, 1f)]
        private void DebugPrintJobDatabase()
        {
            if (ActiveJobDatabase != null)
            {
                Debug.Log($"=== JOB DATABASE ===\n" +
                         $"Jobs: {ActiveJobDatabase.AllJobs?.Count ?? 0}\n" +
                         $"Default: {ActiveJobDatabase.DefaultVillagerJob?.JobName ?? "NOT SET"}");

                foreach (var job in ActiveJobDatabase.AllJobs)
                {
                    if (job != null)
                    {
                        string visualStatus = job.HasValidVisualSet ? "MeshSwap" : (job.UseLegacySystem ? "Legacy" : "NONE");
                        Debug.Log($"  - {job.JobName} ({job.Role}): Visual [{visualStatus}]");
                    }
                }
            }
        }

        [Button("🔄 Reset All Jobs to Default", ButtonSizes.Medium), GUIColor(1f, 0.7f, 0.3f)]
        private void DebugResetAllJobs()
        {
            foreach (var worker in allWorkerInstances)
            {
                worker.ResetToDefaultJob();
            }
            Debug.Log($"[WorkerSystem] Reset jobs for {allWorkerInstances.Count} workers.");
        }
#endif
    }
}