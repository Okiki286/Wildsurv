using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// Manager singleton per gestire tutti i worker attivi.
    /// Coordina spawn, assignment, pool, AI globale.
    /// </summary>
    public class WorkerSystem : MonoBehaviour
    {
        // ============================================
        // SINGLETON
        // ============================================

        public static WorkerSystem Instance { get; private set; }

        // ============================================
        // SETUP
        // ============================================

        [TitleGroup("Setup")]
        [BoxGroup("Setup/Worker Definitions")]
        [Required("Serve almeno 1 WorkerData!")]
        [AssetsOnly]
        [SerializeField] private WorkerData[] availableWorkers;

        [BoxGroup("Setup/Spawn Settings")]
        [LabelWidth(150)]
        [PropertyRange(1, 50)]
        [Tooltip("Worker iniziali")]
        [SerializeField] private int startingWorkerCount = 5;

        [BoxGroup("Setup/Spawn Settings")]
        [LabelWidth(150)]
        [ChildGameObjectsOnly]
        [Tooltip("Punto di spawn")]
        [SerializeField] private Transform spawnPoint;

        [BoxGroup("Setup/Spawn Settings")]
        [LabelWidth(150)]
        [Tooltip("Prefab di fallback")]
        [SerializeField] private GameObject fallbackWorkerPrefab;

        // ============================================
        // RUNTIME STATE
        // ============================================

        [TitleGroup("Runtime State")]
        [BoxGroup("Runtime State/Workers")]
        [ReadOnly]
        [ShowInInspector]
        [ListDrawerSettings(IsReadOnly = true, ShowPaging = false)]
        private List<WorkerController> allWorkers = new List<WorkerController>();

        [BoxGroup("Runtime State/Workers")]
        [ReadOnly]
        [ShowInInspector]
        [ListDrawerSettings(IsReadOnly = true, ShowPaging = false)]
        private List<WorkerController> idleWorkers = new List<WorkerController>();

        // ============================================
        // WORKER INSTANCES (for UI Assignment System)
        // ============================================

        [TitleGroup("Worker Instances")]
        [BoxGroup("Worker Instances/List")]
        [ReadOnly]
        [ShowInInspector]
        [ListDrawerSettings(IsReadOnly = true, ShowPaging = false, ShowIndexLabels = true)]
        [InfoBox("WorkerInstance = dati logici per UI assignment. Possono avere WorkerController fisico opzionale.")]
        private List<WorkerInstance> allWorkerInstances = new List<WorkerInstance>();

        [BoxGroup("Runtime State/Stats")]
        [ReadOnly]
        [ShowInInspector]
        [ProgressBar(0, 50, ColorGetter = "GetWorkerCountColor")]
        private int totalWorkerCount = 0;

        [BoxGroup("Runtime State/Stats")]
        [ReadOnly]
        [ShowInInspector]
        private int assignedWorkerCount = 0;

        // ============================================
        // LOOKUP CACHES
        // ============================================

        private Dictionary<string, WorkerData> workerDataLookup = new Dictionary<string, WorkerData>();

        // ============================================
        // PROPERTIES
        // ============================================

        public int TotalWorkers => totalWorkerCount;
        public int IdleWorkers => idleWorkers.Count;
        public int AssignedWorkers => assignedWorkerCount;
        public List<WorkerController> AllWorkers => allWorkers;

        // Worker Instance properties
        public int WorkerInstanceCount => allWorkerInstances.Count;
        public int AvailableWorkerCount => allWorkerInstances.Count(w => !w.IsAssigned);
        public int AssignedInstanceCount => allWorkerInstances.Count(w => w.IsAssigned);

        // ============================================
        // UNITY LIFECYCLE
        // ============================================

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[WorkerSystem] Instance duplicata rilevata! Distruggo questo GameObject.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeLookups();
        }

        private void Start()
        {
            if (availableWorkers == null || availableWorkers.Length == 0)
            {
                Debug.LogError("[WorkerSystem] Nessun WorkerData assegnato! Sistema disabilitato.", this);
                enabled = false;
                return;
            }

            Debug.Log($"<color=green>[WorkerSystem]</color> Initialized with {availableWorkers.Length} worker types");

            // Spawn worker iniziali
            SpawnStartingWorkers();
        }

        private void Update()
        {
            UpdateWorkerCounts();
        }

        // ============================================
        // INIZIALIZZAZIONE
        // ============================================

        private void InitializeLookups()
        {
            workerDataLookup.Clear();

            if (availableWorkers == null) return;

            foreach (var data in availableWorkers)
            {
                if (data == null) continue;

                if (!workerDataLookup.ContainsKey(data.WorkerId))
                {
                    workerDataLookup.Add(data.WorkerId, data);
                }
            }

            Debug.Log($"<color=green>[WorkerSystem]</color> Loaded {workerDataLookup.Count} worker definitions");
        }

        private void SpawnStartingWorkers()
        {
            if (availableWorkers == null || availableWorkers.Length == 0) return;

            // Prendi il primo worker come default
            WorkerData defaultWorker = availableWorkers[0];

            for (int i = 0; i < startingWorkerCount; i++)
            {
                SpawnWorker(defaultWorker);
            }

            Debug.Log($"<color=green>[WorkerSystem]</color> Spawned {startingWorkerCount} starting workers");

            // Crea anche WorkerInstance per ciascuno (dual-system)
            CreateWorkerInstancesForPhysicalWorkers();
        }

        /// <summary>
        /// Crea WorkerInstance per tutti i WorkerController fisici esistenti
        /// </summary>
        private void CreateWorkerInstancesForPhysicalWorkers()
        {
            foreach (var controller in allWorkers)
            {
                if (controller == null || controller.Data == null) continue;

                // Crea WorkerInstance collegato
                WorkerInstance instance = new WorkerInstance(controller.Data, controller.Data.DisplayName);
                instance.PhysicalWorker = controller;
                allWorkerInstances.Add(instance);
            }

            Debug.Log($"<color=cyan>[WorkerSystem]</color> Created {allWorkerInstances.Count} WorkerInstances");
        }

        // ============================================
        // SPAWN SYSTEM
        // ============================================

        /// <summary>
        /// Spawna un worker basato su WorkerData
        /// </summary>
        public WorkerController SpawnWorker(WorkerData data)
        {
            if (data == null)
            {
                Debug.LogError("[WorkerSystem] Cannot spawn worker: data is null!");
                return null;
            }

            // Ottieni prefab (usa fallback se necessario)
            GameObject prefab = data.Prefab != null ? data.Prefab : fallbackWorkerPrefab;
            if (prefab == null)
            {
                Debug.LogError($"[WorkerSystem] No prefab for {data.DisplayName} and no fallback!");
                return null;
            }

            // Calcola posizione spawn
            Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            spawnPos += Random.insideUnitSphere * 2f; // Random offset
            spawnPos.y = 0; // Tieni a terra

            // Instantiate
            GameObject workerObj = Instantiate(prefab, spawnPos, Quaternion.identity);
            workerObj.name = $"Worker_{data.DisplayName}_{allWorkers.Count}";
            workerObj.transform.SetParent(transform);

            // Setup controller
            WorkerController controller = workerObj.GetComponent<WorkerController>();
            if (controller == null)
            {
                controller = workerObj.AddComponent<WorkerController>();
            }
            controller.Initialize(data);

            // Registra
            allWorkers.Add(controller);
            idleWorkers.Add(controller);

            Debug.Log($"<color=green>[WorkerSystem]</color> Spawned {data.DisplayName} at {spawnPos}");
            return controller;
        }

        /// <summary>
        /// Spawna worker per ID
        /// </summary>
        public WorkerController SpawnWorkerById(string workerId)
        {
            if (workerDataLookup.TryGetValue(workerId, out WorkerData data))
            {
                return SpawnWorker(data);
            }

            Debug.LogError($"[WorkerSystem] Worker ID '{workerId}' not found!");
            return null;
        }

        // ============================================
        // WORKER MANAGEMENT
        // ============================================

        /// <summary>
        /// Ottieni un worker idle disponibile
        /// </summary>
        public WorkerController GetIdleWorker()
        {
            if (idleWorkers.Count == 0) return null;
            return idleWorkers[0];
        }

        /// <summary>
        /// Ottieni N worker idle
        /// </summary>
        public List<WorkerController> GetIdleWorkers(int count)
        {
            return idleWorkers.Take(count).ToList();
        }

        /// <summary>
        /// Assegna un worker a una struttura
        /// </summary>
        public bool AssignWorkerToStructure(WorkerController worker, GameObject structure, WildernessSurvival.Gameplay.Structures.WorkerRole role, Vector3 workPosition)
        {
            if (worker == null || structure == null) return false;

            if (!idleWorkers.Contains(worker))
            {
                Debug.LogWarning($"[WorkerSystem] {worker.Data.DisplayName} is not idle!");
                return false;
            }

            // Rimuovi da idle
            idleWorkers.Remove(worker);

            // Assegna
            worker.AssignToStructure(structure, role, workPosition);

            Debug.Log($"<color=yellow>[WorkerSystem]</color> Assigned {worker.Data.DisplayName} to {structure.name}");
            return true;
        }

        /// <summary>
        /// Rimuove l'assignment e torna idle
        /// </summary>
        public void UnassignWorker(WorkerController worker)
        {
            if (worker == null) return;

            worker.Unassign();

            if (!idleWorkers.Contains(worker))
            {
                idleWorkers.Add(worker);
            }

            Debug.Log($"<color=yellow>[WorkerSystem]</color> Unassigned {worker.Data.DisplayName}");
        }

        /// <summary>
        /// Callback quando un worker muore
        /// </summary>
        public void OnWorkerDied(WorkerController worker)
        {
            if (worker == null) return;

            allWorkers.Remove(worker);
            idleWorkers.Remove(worker);

            Debug.Log($"<color=red>[WorkerSystem]</color> Worker {worker.Data.DisplayName} died. Remaining: {allWorkers.Count}");
        }

        // ============================================
        // WORKER INSTANCE MANAGEMENT (for UI Assignment)
        // ============================================

        /// <summary>
        /// Crea un nuovo WorkerInstance (virtuale, senza rappresentazione fisica)
        /// </summary>
        public WorkerInstance CreateWorkerInstance(WorkerData data, string customName = null)
        {
            if (data == null)
            {
                Debug.LogError("[WorkerSystem] Cannot create WorkerInstance with null data");
                return null;
            }

            WorkerInstance instance = new WorkerInstance(data, customName);
            allWorkerInstances.Add(instance);

            Debug.Log($"<color=green>[WorkerSystem]</color> Created virtual WorkerInstance: {instance.CustomName}");
            return instance;
        }

        /// <summary>
        /// Ottiene tutti i worker disponibili (non assegnati)
        /// </summary>
        public List<WorkerInstance> GetAvailableWorkers()
        {
            return allWorkerInstances.Where(w => !w.IsAssigned).ToList();
        }

        /// <summary>
        /// Ottiene tutti i worker assegnati
        /// </summary>
        public List<WorkerInstance> GetAssignedWorkers()
        {
            return allWorkerInstances.Where(w => w.IsAssigned).ToList();
        }

        /// <summary>
        /// Ottiene worker assegnati a una struttura specifica
        /// </summary>
        public List<WorkerInstance> GetWorkersAtStructure(Structures.StructureController structure)
        {
            if (structure == null) return new List<WorkerInstance>();
            return allWorkerInstances.Where(w => w.AssignedStructure == structure).ToList();
        }

        /// <summary>
        /// Assegna un WorkerInstance a una struttura
        /// </summary>
        public bool AssignWorker(WorkerInstance worker, Structures.StructureController structure)
        {
            if (worker == null || structure == null)
            {
                Debug.LogWarning("[WorkerSystem] AssignWorker: null worker or structure");
                return false;
            }

            if (worker.IsAssigned)
            {
                Debug.LogWarning($"[WorkerSystem] Worker {worker.CustomName} is already assigned");
                return false;
            }

            if (!structure.HasFreeWorkerSlot())
            {
                Debug.LogWarning($"[WorkerSystem] Structure {structure.Data.DisplayName} has no free slots");
                return false;
            }

            // Assegna worker
            if (worker.AssignTo(structure))
            {
                structure.AddWorkerInstance(worker);
                Debug.Log($"<color=green>[WorkerSystem]</color> Assigned {worker.CustomName} to {structure.Data.DisplayName}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Rimuove un WorkerInstance da una struttura
        /// </summary>
        public void UnassignWorker(WorkerInstance worker)
        {
            if (worker == null) return;

            if (worker.IsAssigned)
            {
                Structures.StructureController structure = worker.AssignedStructure;
                structure?.RemoveWorkerInstance(worker);
                worker.Unassign();
                Debug.Log($"<color=yellow>[WorkerSystem]</color> Unassigned {worker.CustomName}");
            }
        }

        /// <summary>
        /// Ottiene tutti i WorkerInstance
        /// </summary>
        public List<WorkerInstance> GetAllWorkerInstances()
        {
            return new List<WorkerInstance>(allWorkerInstances);
        }

        // ============================================
        // UTILITY
        // ============================================

        private void UpdateWorkerCounts()
        {
            totalWorkerCount = allWorkers.Count;
            assignedWorkerCount = totalWorkerCount - idleWorkers.Count;
        }

        /// <summary>
        /// Ottieni tutti i worker assegnati a una struttura
        /// </summary>
        public List<WorkerController> GetWorkersAssignedTo(GameObject structure)
        {
            return allWorkers.Where(w => w.AssignedStructure == structure).ToList();
        }

        /// <summary>
        /// Richiama tutti i worker alla base (night phase)
        /// </summary>
        public void RecallAllWorkers()
        {
            foreach (var worker in allWorkers)
            {
                if (worker.State == WorkerState.Working || worker.State == WorkerState.MovingToWork)
                {
                    UnassignWorker(worker);
                    // TODO: Move to base
                }
            }

            Debug.Log($"<color=yellow>[WorkerSystem]</color> Recalled all workers to base");
        }

        // ============================================
        // DEBUG & ODIN
        // ============================================

#if UNITY_EDITOR
        private Color GetWorkerCountColor()
        {
            if (totalWorkerCount >= 20) return Color.green;
            if (totalWorkerCount >= 10) return Color.yellow;
            return Color.red;
        }

        [TitleGroup("Debug Actions")]
        [ButtonGroup("Debug Actions/Row1")]
        [Button("➕ Spawn Worker", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.8f, 0.4f)]
        private void DebugSpawnWorker()
        {
            if (availableWorkers != null && availableWorkers.Length > 0)
            {
                SpawnWorker(availableWorkers[0]);
            }
        }

        [ButtonGroup("Debug Actions/Row1")]
        [Button("📊 Print Stats", ButtonSizes.Large)]
        private void DebugPrintStats()
        {
            Debug.Log($"=== WORKER SYSTEM STATS ===\n" +
                $"Total Workers: {totalWorkerCount}\n" +
                $"Idle: {idleWorkers.Count}\n" +
                $"Assigned: {assignedWorkerCount}\n" +
                $"\nWorker Instances: {allWorkerInstances.Count}\n" +
                $"Available: {AvailableWorkerCount}\n" +
                $"Assigned Instances: {AssignedInstanceCount}");
        }

        [ButtonGroup("Debug Actions/Row2")]
        [Button("🏠 Recall All", ButtonSizes.Large)]
        [GUIColor(1f, 1f, 0.5f)]
        private void DebugRecallAll()
        {
            RecallAllWorkers();
        }

        private void OnDrawGizmos()
        {
            // Disegna spawn point
            if (spawnPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(spawnPoint.position, 1f);
            }

            // Disegna worker count
            if (Application.isPlaying && allWorkers != null)
            {
                Gizmos.color = Color.cyan;
                foreach (var worker in allWorkers)
                {
                    if (worker != null)
                    {
                        Gizmos.DrawWireSphere(worker.transform.position, 0.5f);
                    }
                }
            }
        }
#endif
    }
}