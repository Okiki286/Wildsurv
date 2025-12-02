using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using WildernessSurvival.Gameplay.Structures;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// Sistema centrale per la gestione dei worker.
    /// Implementa il pattern "Manager Loop" per ottimizzare le performance su mobile.
    /// Gestisce il tick di produzione e il movimento dei worker fisici.
    /// </summary>
    public class WorkerSystem : MonoBehaviour
    {
        public static WorkerSystem Instance { get; private set; }

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

        // Properties
        public int WorkerInstanceCount => allWorkerInstances.Count;
        public int AvailableWorkerCount => availableWorkers.Count;
        public int AssignedInstanceCount => assignedWorkers.Count;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Update()
        {
            if (paused) return;

            float dt = Time.deltaTime;

            // 1. Production Tick Loop
            // Itera su tutte le strutture attive per calcolare la produzione
            for (int i = activeStructures.Count - 1; i >= 0; i--)
            {
                var structure = activeStructures[i];
                if (structure != null && structure.IsOperational)
                {
                    structure.TickProduction(dt);
                }
                else
                {
                    // Rimuovi se null o distrutta (lazy cleanup)
                    if (structure == null) activeStructures.RemoveAt(i);
                }
            }

            // 2. Worker Movement/Anim Loop
            // Itera su tutti i worker fisici per aggiornare movimento e animazioni
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
        }

        // ============================================
        // WORKER INSTANCE MANAGEMENT
        // ============================================

        public WorkerInstance CreateWorkerInstance(WorkerData data)
        {
            if (data == null) return null;

            WorkerInstance newWorker = new WorkerInstance(data);
            allWorkerInstances.Add(newWorker);
            availableWorkers.Add(newWorker);

            // **FIX**: Instantiate the physical prefab GameObject
            if (data.Prefab != null)
            {
                GameObject workerObj = Instantiate(data.Prefab);
                WorkerController controller = workerObj.GetComponent<WorkerController>();
                
                if (controller != null)
                {
                    // Link the physical controller to the instance
                    newWorker.PhysicalWorker = controller;
                    
                    // Register the physical worker in the system
                    RegisterWorker(controller);
                    
                    Debug.Log($"<color=green>[WorkerSystem]</color> Spawned physical worker: {data.DisplayName} at {workerObj.transform.position}");
                }
                else
                {
                    Debug.LogError($"<color=red>[WorkerSystem]</color> Prefab {data.Prefab.name} has no WorkerController component!");
                    Destroy(workerObj);
                }
            }
            else
            {
                Debug.LogWarning($"<color=yellow>[WorkerSystem]</color> WorkerData '{data.DisplayName}' has no prefab assigned. Creating virtual worker.");
            }

            return newWorker;
        }

        public List<WorkerInstance> GetAvailableWorkers()
        {
            return new List<WorkerInstance>(availableWorkers);
        }

        public List<WorkerInstance> GetAssignedWorkers()
        {
            return new List<WorkerInstance>(assignedWorkers);
        }

        public List<WorkerInstance> GetWorkersAtStructure(StructureController structure)
        {
            List<WorkerInstance> result = new List<WorkerInstance>();
            foreach (var worker in assignedWorkers)
            {
                if (worker.AssignedStructure == structure)
                {
                    result.Add(worker);
                }
            }
            return result;
        }

        public bool AssignWorker(WorkerInstance worker, StructureController structure)
        {
            if (worker == null || structure == null) return false;
            
            if (worker.IsAssigned)
            {
                UnassignWorker(worker);
            }

            // Usa il metodo che accetta WorkerInstance
            if (structure.AssignWorker(worker))
            {
                availableWorkers.Remove(worker);
                if (!assignedWorkers.Contains(worker))
                {
                    assignedWorkers.Add(worker);
                }
                return true;
            }

            return false;
        }

        public void UnassignWorker(WorkerInstance worker)
        {
            if (worker == null) return;

            StructureController structure = worker.AssignedStructure;
            if (structure != null)
            {
                // Usa il metodo che accetta WorkerInstance
                structure.RemoveWorker(worker);
            }
            else
            {
                // Fallback se la struttura è null ma il worker pensa di essere assegnato
                worker.Unassign();
            }

            assignedWorkers.Remove(worker);
            if (!availableWorkers.Contains(worker))
            {
                availableWorkers.Add(worker);
            }
        }

        // ============================================
        // REGISTRATION API (Physical Objects)
        // ============================================

        public void RegisterStructure(StructureController structure)
        {
            if (structure != null && !activeStructures.Contains(structure))
            {
                activeStructures.Add(structure);
            }
        }

        public void UnregisterStructure(StructureController structure)
        {
            if (activeStructures.Contains(structure))
            {
                activeStructures.Remove(structure);
            }
        }

        public void RegisterWorker(WorkerController worker)
        {
            if (worker != null && !physicalWorkers.Contains(worker))
            {
                physicalWorkers.Add(worker);
            }
        }

        public void UnregisterWorker(WorkerController worker)
        {
            if (physicalWorkers.Contains(worker))
            {
                physicalWorkers.Remove(worker);
            }
        }

        // ============================================
        // EDITOR TOOLS
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Editor Tools")]
        [Button("Find All Actors in Scene", ButtonSizes.Large)]
        [GUIColor(0.5f, 1f, 0.5f)]
        private void FindAllActors()
        {
            // Trova Strutture
            activeStructures.Clear();
            var structures = FindObjectsByType<StructureController>(FindObjectsSortMode.None);
            foreach (var s in structures)
            {
                if (s.enabled) activeStructures.Add(s);
            }
            Debug.Log($"[WorkerSystem] Found {activeStructures.Count} active structures.");

            // Trova Worker
            physicalWorkers.Clear();
            var workers = FindObjectsByType<WorkerController>(FindObjectsSortMode.None);
            foreach (var w in workers)
            {
                if (w.enabled) physicalWorkers.Add(w);
            }
            Debug.Log($"[WorkerSystem] Found {physicalWorkers.Count} physical workers.");
        }
#endif
    }
}