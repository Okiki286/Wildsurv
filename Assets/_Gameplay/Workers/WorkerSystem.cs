using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using WildernessSurvival.Gameplay.Structures;
using System.Linq; // Necessario per Linq

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// Sistema centrale per la gestione dei worker.
    /// Include Debugging avanzato per Auto-Assignment.
    /// </summary>
    public class WorkerSystem : MonoBehaviour
    {
        public static WorkerSystem Instance { get; private set; }

        // ============================================
        // CONFIGURAZIONE SPAWN
        // ============================================
        [TitleGroup("Starting Workers")]
        [SerializeField]
        [InfoBox("Trascina qui i WorkerData (es. Builder, Gatherer) da Assets/_Gameplay/Workers/Data", InfoMessageType.Info)]
        private List<WorkerData> availableWorkerTypes = new List<WorkerData>();

        [SerializeField]
        [Range(1, 10)]
        private int startingWorkerCount = 3;

        // ============================================
        // RUNTIME LISTS
        // ============================================

        [TitleGroup("Runtime Lists")]
        [ShowInInspector, ReadOnly]
        private List<StructureController> activeStructures = new List<StructureController>();

        [ShowInInspector, ReadOnly]
        private List<WorkerController> physicalWorkers = new List<WorkerController>();

        // Liste per la gestione logica (WorkerInstance)
        [ShowInInspector, ReadOnly]
        private List<WorkerInstance> allWorkerInstances = new List<WorkerInstance>();

        [ShowInInspector, ReadOnly]
        private List<WorkerInstance> availableWorkers = new List<WorkerInstance>();

        [ShowInInspector, ReadOnly]
        private List<WorkerInstance> assignedWorkers = new List<WorkerInstance>();

        [TitleGroup("Global Settings")]
        [SerializeField] private bool paused = false;

        [TitleGroup("Debug Settings")]
        [SerializeField]
        [Tooltip("Abilita log dettagliati per il sistema di auto-assegnazione")]
        private bool debugAutoAssign = true;

        // ============================================
        // AUTO-ASSIGNMENT (FOREMAN)
        // ============================================

        [TitleGroup("Auto-Assignment")]
        [SerializeField]
        [PropertyRange(0.5f, 5f)]
        [Tooltip("Intervallo in secondi per controllare auto-assegnazioni Builder")]
        private float autoAssignInterval = 1.0f;

        private float autoAssignTimer = 0f;

        // Properties
        public int WorkerInstanceCount => allWorkerInstances.Count;
        public int AvailableWorkerCount => availableWorkers.Count;
        public int AssignedInstanceCount => assignedWorkers.Count;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            // Spawn dei worker iniziali al primo frame
            SpawnStartingWorkers();
        }

        private void Update()
        {
            if (paused) return;

            float dt = Time.deltaTime;

            // 1. Structure Tick Loop (Construction + Production)
            for (int i = activeStructures.Count - 1; i >= 0; i--)
            {
                var structure = activeStructures[i];
                if (structure != null)
                {
                    // Se in costruzione, chiama TickConstruction
                    if (structure.State == StructureState.Building)
                    {
                        structure.TickConstruction(dt);
                    }
                    // Se operativa, chiama TickProduction
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

            // 2. Worker Movement/Anim Loop
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

            // 3. Auto-Assignment Loop (Foreman)
            autoAssignTimer += dt;
            if (autoAssignTimer >= autoAssignInterval)
            {
                autoAssignTimer = 0f;
                CheckAutoAssignments();
            }
        }

        // ============================================
        // SPAWN LOGIC
        // ============================================

        private void SpawnStartingWorkers()
        {
            if (availableWorkerTypes == null || availableWorkerTypes.Count == 0)
            {
                Debug.LogError("❌ [WorkerSystem] Nessun WorkerData assegnato in 'Available Worker Types'! Non posso spawnare worker.");
                return;
            }

            Debug.Log($"<color=cyan>[WorkerSystem]</color> Spawning {startingWorkerCount} starting workers...");

            for (int i = 0; i < startingWorkerCount; i++)
            {
                // Prendi un tipo a caso o ciclico
                var typeToSpawn = availableWorkerTypes[i % availableWorkerTypes.Count];
                if (typeToSpawn != null)
                {
                    CreateWorkerInstance(typeToSpawn);
                }
            }
        }

        public WorkerInstance CreateWorkerInstance(WorkerData data)
        {
            if (data == null) return null;

            WorkerInstance newWorker = new WorkerInstance(data);
            allWorkerInstances.Add(newWorker);
            availableWorkers.Add(newWorker);

            // Spawn Fisico
            if (data.Prefab != null)
            {
                // Posizione di spawn casuale attorno al centro
                Vector3 spawnPos = GetRandomSpawnPosition();

                GameObject workerObj = Instantiate(data.Prefab, spawnPos, Quaternion.identity);
                workerObj.name = $"Worker_{data.DisplayName}_{newWorker.InstanceId.Substring(0, 4)}";

                WorkerController controller = workerObj.GetComponent<WorkerController>();

                if (controller != null)
                {
                    newWorker.PhysicalWorker = controller;
                    RegisterWorker(controller);
                    Debug.Log($"<color=green>[WorkerSystem]</color> Spawned {data.DisplayName} at {spawnPos}");
                }
                else
                {
                    Debug.LogError($"<color=red>[WorkerSystem]</color> Prefab {data.Prefab.name} missing WorkerController!");
                }
            }
            else
            {
                Debug.LogWarning($"<color=yellow>[WorkerSystem]</color> {data.DisplayName} has no prefab. Virtual worker created.");
            }

            return newWorker;
        }

        private Vector3 GetRandomSpawnPosition()
        {
            Vector3 center = Vector3.zero;
            Vector3 randomPoint = center + Random.insideUnitSphere * 5f;
            randomPoint.y = 0;

            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(randomPoint, out hit, 5.0f, UnityEngine.AI.NavMesh.AllAreas))
            {
                return hit.position;
            }
            return center;
        }

        // ============================================
        // WORKER MANAGEMENT
        // ============================================

        public List<WorkerInstance> GetAvailableWorkers() => new List<WorkerInstance>(availableWorkers);
        public List<WorkerInstance> GetAssignedWorkers() => new List<WorkerInstance>(assignedWorkers);

        public List<WorkerInstance> GetWorkersAtStructure(StructureController structure)
        {
            return assignedWorkers.FindAll(w => w.AssignedStructure == structure);
        }

        public bool AssignWorker(WorkerInstance worker, StructureController structure)
        {
            if (worker == null || structure == null) return false;

            if (worker.IsAssigned) UnassignWorker(worker);

            if (structure.AssignWorker(worker))
            {
                availableWorkers.Remove(worker);
                if (!assignedWorkers.Contains(worker)) assignedWorkers.Add(worker);
                return true;
            }
            return false;
        }

        public void UnassignWorker(WorkerInstance worker)
        {
            if (worker == null) return;

            if (worker.AssignedStructure != null)
            {
                worker.AssignedStructure.RemoveWorker(worker);
            }
            else
            {
                worker.Unassign();
            }

            assignedWorkers.Remove(worker);
            if (!availableWorkers.Contains(worker)) availableWorkers.Add(worker);
        }

        // ============================================
        // AUTO-ASSIGNMENT LOGIC (DEBUGGED)
        // ============================================

        /// <summary>
        /// Controlla e assegna automaticamente Builder disponibili a strutture in costruzione.
        /// Chiamato periodicamente dal loop Update.
        /// </summary>
        private void CheckAutoAssignments()
        {
            // 1. Trova strutture prioritarie: State == Building E ha slot liberi
            var buildingStructures = activeStructures
                .Where(s => s != null && s.State == StructureState.Building && s.HasFreeWorkerSlot())
                .ToList();

            // 2. Trova worker disponibili: non assegnati E ruolo Builder
            var availableBuilders = availableWorkers
                .Where(w => w != null &&
                            w.AssignedStructure == null &&
                            w.Data != null &&
                            w.Data.DefaultRole == WorkerRole.Builder)
                .ToList();

            // LOG DIAGNOSTICO INIZIALE
            if (debugAutoAssign)
            {
                Debug.Log($"<color=orange>[WorkerSystem]</color> Checking Auto-Assign... " +
                          $"Found <b>{buildingStructures.Count}</b> structures needing workers, " +
                          $"<b>{availableBuilders.Count}</b> idle builders.");
            }

            if (buildingStructures.Count == 0 || availableBuilders.Count == 0) return;

            if (debugAutoAssign) Debug.Log("<color=orange>[WorkerSystem]</color> Attempting match...");

            // 3. Match: Assegna builder a strutture
            int assignmentCount = 0;

            foreach (var structure in buildingStructures)
            {
                // Se non ci sono più builder disponibili, esci
                if (availableBuilders.Count == 0) break;

                // Controlla se la struttura ha ancora slot liberi
                while (structure.HasFreeWorkerSlot() && availableBuilders.Count > 0)
                {
                    var builder = availableBuilders[0];
                    availableBuilders.RemoveAt(0);

                    // Tenta l'assegnazione
                    bool success = AssignWorker(builder, structure);

                    if (success)
                    {
                        assignmentCount++;
                        if (debugAutoAssign)
                        {
                            Debug.Log($"<color=green>[WorkerSystem]</color> SUCCESS: Auto-assigned {builder.CustomName} to {structure.name}");
                        }
                    }
                    else
                    {
                        // LOG DI FALLIMENTO CRITICO
                        if (debugAutoAssign)
                        {
                            Debug.LogWarning($"<color=red>[WorkerSystem]</color> FAILED to assign worker {builder.CustomName} to {structure.name} via logic! " +
                                             $"Check StructureController.AssignWorker() conditions (Max workers reached? Resource check?).");
                        }
                    }
                }
            }

            if (assignmentCount > 0 && debugAutoAssign)
            {
                Debug.Log($"<color=green>[WorkerSystem]</color> Cycle Complete: Auto-assigned {assignmentCount} builders this tick.");
            }
        }

        // ============================================
        // REGISTRATION API
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

        [TitleGroup("Editor Tools")]
        [Button("Spawn Random Worker", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
        private void DebugSpawnRandom()
        {
            if (availableWorkerTypes.Count > 0)
            {
                var randomType = availableWorkerTypes[Random.Range(0, availableWorkerTypes.Count)];
                CreateWorkerInstance(randomType);
            }
            else
            {
                Debug.LogError("No worker types configured!");
            }
        }

#if UNITY_EDITOR
        [Button("Find All Actors in Scene", ButtonSizes.Medium)]
        private void FindAllActors()
        {
            activeStructures = new List<StructureController>(FindObjectsByType<StructureController>(FindObjectsSortMode.None));
            physicalWorkers = new List<WorkerController>(FindObjectsByType<WorkerController>(FindObjectsSortMode.None));
            Debug.Log($"Found {activeStructures.Count} structures and {physicalWorkers.Count} workers.");
        }
#endif
    }
}