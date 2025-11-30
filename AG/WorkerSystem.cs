using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using WildernessSurvival.Gameplay.Structures;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// Sistema centrale per gestire tutti i worker del gioco.
    /// Singleton che gestisce spawn, assignment, e query sui worker.
    /// </summary>
    public class WorkerSystem : MonoBehaviour
    {
        // ============================================
        // SINGLETON
        // ============================================

        public static WorkerSystem Instance { get; private set; }

        // ============================================
        // CONFIGURAZIONE
        // ============================================

        [TitleGroup("Configurazione")]
        [LabelWidth(150)]
        [SerializeField] private int maxWorkers = 20;

        [TitleGroup("Worker Templates")]
        [InfoBox("Trascina qui i WorkerData disponibili oppure lascia vuoto per auto-load")]
        [SerializeField] private WorkerData[] workerTemplates;

        [TitleGroup("Starting Workers")]
        [LabelWidth(150)]
        [SerializeField] private int startingWorkerCount = 3;
        
        [SerializeField] private WorkerData defaultWorkerType;

        [TitleGroup("Debug")]
        [SerializeField] private bool debugMode = true;

        // ============================================
        // RUNTIME
        // ============================================

        [TitleGroup("Runtime - Workers")]
        [ShowInInspector]
        [ReadOnly]
        [ListDrawerSettings(ShowIndexLabels = true)]
        private List<WorkerInstance> allWorkers = new List<WorkerInstance>();

        // Names for random worker generation
        private static readonly string[] workerNames = new string[]
        {
            "John", "Mary", "Bob", "Alice", "Tom", "Emma", "Jack", "Lily",
            "James", "Sophie", "Oliver", "Chloe", "Harry", "Emily", "Charlie",
            "William", "Grace", "George", "Ruby", "Noah", "Ella", "Leo", "Mia"
        };

        private int nameIndex = 0;

        // ============================================
        // PROPERTIES
        // ============================================

        public int WorkerCount => allWorkers.Count;
        public int MaxWorkers => maxWorkers;
        public int AvailableWorkerCount => allWorkers.Count(w => !w.IsAssigned);
        public int AssignedWorkerCount => allWorkers.Count(w => w.IsAssigned);
        public bool CanAddWorker => allWorkers.Count < maxWorkers;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[WorkerSystem] Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            LoadWorkerTemplates();
            SpawnStartingWorkers();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ============================================
        // INITIALIZATION
        // ============================================

        private void LoadWorkerTemplates()
        {
            if (workerTemplates == null || workerTemplates.Length == 0)
            {
                // Try to load from Resources
                workerTemplates = Resources.LoadAll<WorkerData>("Data/Workers");
                
                #if UNITY_EDITOR
                if (workerTemplates == null || workerTemplates.Length == 0)
                {
                    // Try AssetDatabase
                    string[] guids = UnityEditor.AssetDatabase.FindAssets("t:WorkerData");
                    var loadedTemplates = new List<WorkerData>();
                    
                    foreach (string guid in guids)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                        WorkerData data = UnityEditor.AssetDatabase.LoadAssetAtPath<WorkerData>(path);
                        if (data != null)
                        {
                            loadedTemplates.Add(data);
                        }
                    }
                    
                    workerTemplates = loadedTemplates.ToArray();
                }
                #endif
            }

            if (debugMode)
            {
                Debug.Log($"<color=cyan>[WorkerSystem]</color> Loaded {workerTemplates?.Length ?? 0} worker templates");
            }
        }

        private void SpawnStartingWorkers()
        {
            // Determine which worker type to use
            WorkerData workerType = defaultWorkerType;
            
            if (workerType == null && workerTemplates != null && workerTemplates.Length > 0)
            {
                // Use first available template (preferably Gatherer)
                workerType = workerTemplates.FirstOrDefault(w => w.Role == WorkerRole.Gatherer) 
                          ?? workerTemplates[0];
            }

            if (workerType == null)
            {
                Debug.LogWarning("[WorkerSystem] No worker type available for starting workers!");
                return;
            }

            for (int i = 0; i < startingWorkerCount; i++)
            {
                CreateWorker(workerType);
            }

            if (debugMode)
            {
                Debug.Log($"<color=green>[WorkerSystem]</color> Spawned {startingWorkerCount} starting workers");
            }
        }

        // ============================================
        // WORKER CREATION
        // ============================================

        /// <summary>
        /// Crea un nuovo worker di un tipo specifico
        /// </summary>
        public WorkerInstance CreateWorker(WorkerData data, string customName = null)
        {
            if (data == null)
            {
                Debug.LogError("[WorkerSystem] Cannot create worker with null data");
                return null;
            }

            if (!CanAddWorker)
            {
                Debug.LogWarning($"[WorkerSystem] Max worker limit ({maxWorkers}) reached!");
                return null;
            }

            // Generate unique name if not provided
            if (string.IsNullOrEmpty(customName))
            {
                customName = GetNextWorkerName();
            }

            WorkerInstance worker = new WorkerInstance(data, customName);
            allWorkers.Add(worker);

            if (debugMode)
            {
                Debug.Log($"<color=green>[WorkerSystem]</color> Created worker: {worker.CustomName} ({data.Role})");
            }

            return worker;
        }

        /// <summary>
        /// Crea un worker di un ruolo specifico (usa template matching)
        /// </summary>
        public WorkerInstance CreateWorkerByRole(WorkerRole role, string customName = null)
        {
            if (workerTemplates == null || workerTemplates.Length == 0)
            {
                Debug.LogWarning("[WorkerSystem] No worker templates available");
                return null;
            }

            WorkerData matchingData = workerTemplates.FirstOrDefault(w => w.Role == role);
            
            if (matchingData == null)
            {
                Debug.LogWarning($"[WorkerSystem] No template found for role: {role}");
                return null;
            }

            return CreateWorker(matchingData, customName);
        }

        /// <summary>
        /// Rimuove un worker dal gioco
        /// </summary>
        public void RemoveWorker(WorkerInstance worker)
        {
            if (worker == null) return;

            // Unassign first if needed
            if (worker.IsAssigned)
            {
                UnassignWorker(worker);
            }

            allWorkers.Remove(worker);

            // Destroy world object if exists
            if (worker.WorldObject != null)
            {
                Destroy(worker.WorldObject);
            }

            if (debugMode)
            {
                Debug.Log($"<color=red>[WorkerSystem]</color> Removed worker: {worker.CustomName}");
            }
        }

        private string GetNextWorkerName()
        {
            string name = workerNames[nameIndex % workerNames.Length];
            nameIndex++;
            
            // Add number suffix if name is used
            int count = allWorkers.Count(w => w.CustomName.StartsWith(name));
            if (count > 0)
            {
                name = $"{name} {count + 1}";
            }
            
            return name;
        }

        // ============================================
        // ASSIGNMENT
        // ============================================

        /// <summary>
        /// Assegna un worker a una struttura
        /// </summary>
        public bool AssignWorker(WorkerInstance worker, StructureController structure)
        {
            if (worker == null)
            {
                Debug.LogWarning("[WorkerSystem] AssignWorker: worker is null");
                return false;
            }

            if (structure == null)
            {
                Debug.LogWarning("[WorkerSystem] AssignWorker: structure is null");
                return false;
            }

            if (worker.IsAssigned)
            {
                Debug.LogWarning($"[WorkerSystem] Worker {worker.CustomName} is already assigned to {worker.AssignedStructure.Data.DisplayName}");
                return false;
            }

            if (!structure.HasFreeWorkerSlot())
            {
                Debug.LogWarning($"[WorkerSystem] Structure {structure.Data.DisplayName} has no free worker slots");
                return false;
            }

            // Perform assignment
            if (worker.AssignTo(structure))
            {
                structure.AddWorker(worker);
                
                if (debugMode)
                {
                    float bonus = worker.GetCurrentBonus() * 100f;
                    Debug.Log($"<color=green>[WorkerSystem]</color> {worker.CustomName} assigned to {structure.Data.DisplayName} (+{bonus:F0}% bonus)");
                }
                
                return true;
            }

            return false;
        }

        /// <summary>
        /// Rimuove un worker dalla sua struttura assegnata
        /// </summary>
        public void UnassignWorker(WorkerInstance worker)
        {
            if (worker == null) return;

            if (worker.IsAssigned)
            {
                StructureController structure = worker.AssignedStructure;
                
                if (structure != null)
                {
                    structure.RemoveWorker(worker);
                }
                
                worker.Unassign();

                if (debugMode)
                {
                    Debug.Log($"<color=yellow>[WorkerSystem]</color> {worker.CustomName} unassigned");
                }
            }
        }

        /// <summary>
        /// Rimuove tutti i worker da una struttura specifica
        /// </summary>
        public void UnassignAllFromStructure(StructureController structure)
        {
            if (structure == null) return;

            var workersToUnassign = allWorkers.Where(w => w.AssignedStructure == structure).ToList();
            
            foreach (var worker in workersToUnassign)
            {
                UnassignWorker(worker);
            }

            if (debugMode && workersToUnassign.Count > 0)
            {
                Debug.Log($"<color=yellow>[WorkerSystem]</color> Unassigned {workersToUnassign.Count} workers from {structure.Data.DisplayName}");
            }
        }

        // ============================================
        // QUERIES
        // ============================================

        /// <summary>
        /// Ottiene tutti i worker disponibili (non assegnati)
        /// </summary>
        public List<WorkerInstance> GetAvailableWorkers()
        {
            return allWorkers.Where(w => !w.IsAssigned).ToList();
        }

        /// <summary>
        /// Ottiene tutti i worker assegnati
        /// </summary>
        public List<WorkerInstance> GetAssignedWorkers()
        {
            return allWorkers.Where(w => w.IsAssigned).ToList();
        }

        /// <summary>
        /// Ottiene i worker assegnati a una struttura specifica
        /// </summary>
        public List<WorkerInstance> GetWorkersAtStructure(StructureController structure)
        {
            if (structure == null) return new List<WorkerInstance>();
            return allWorkers.Where(w => w.AssignedStructure == structure).ToList();
        }

        /// <summary>
        /// Ottiene tutti i worker
        /// </summary>
        public List<WorkerInstance> GetAllWorkers()
        {
            return new List<WorkerInstance>(allWorkers);
        }

        /// <summary>
        /// Trova un worker per ID
        /// </summary>
        public WorkerInstance GetWorkerById(string instanceId)
        {
            return allWorkers.FirstOrDefault(w => w.InstanceId == instanceId);
        }

        /// <summary>
        /// Ottiene worker per ruolo
        /// </summary>
        public List<WorkerInstance> GetWorkersByRole(WorkerRole role)
        {
            return allWorkers.Where(w => w.Data != null && w.Data.Role == role).ToList();
        }

        /// <summary>
        /// Ottiene worker disponibili per ruolo
        /// </summary>
        public List<WorkerInstance> GetAvailableWorkersByRole(WorkerRole role)
        {
            return allWorkers.Where(w => !w.IsAssigned && w.Data != null && w.Data.Role == role).ToList();
        }

        /// <summary>
        /// Conta worker per stato
        /// </summary>
        public int CountWorkersByState(WorkerState state)
        {
            return allWorkers.Count(w => w.CurrentState == state);
        }

        // ============================================
        // DEBUG
        // ============================================

        [TitleGroup("Debug Actions")]
        [ButtonGroup("Debug Actions/Row1")]
        [Button("Add Gatherer", ButtonSizes.Medium)]
        [GUIColor(0.4f, 0.8f, 0.4f)]
        private void DebugAddGatherer()
        {
            var gatherer = workerTemplates?.FirstOrDefault(w => w.Role == WorkerRole.Gatherer);
            if (gatherer != null)
            {
                CreateWorker(gatherer);
            }
            else if (defaultWorkerType != null)
            {
                CreateWorker(defaultWorkerType);
            }
            else
            {
                Debug.LogWarning("[WorkerSystem] No Gatherer template or default type available");
            }
        }

        [ButtonGroup("Debug Actions/Row1")]
        [Button("Add Builder", ButtonSizes.Medium)]
        [GUIColor(0.7f, 0.5f, 0.2f)]
        private void DebugAddBuilder()
        {
            var builder = workerTemplates?.FirstOrDefault(w => w.Role == WorkerRole.Builder);
            if (builder != null)
            {
                CreateWorker(builder);
            }
            else
            {
                Debug.LogWarning("[WorkerSystem] No Builder template available");
            }
        }

        [ButtonGroup("Debug Actions/Row1")]
        [Button("Add Guard", ButtonSizes.Medium)]
        [GUIColor(0.7f, 0.3f, 0.3f)]
        private void DebugAddGuard()
        {
            var guard = workerTemplates?.FirstOrDefault(w => w.Role == WorkerRole.Guard);
            if (guard != null)
            {
                CreateWorker(guard);
            }
            else
            {
                Debug.LogWarning("[WorkerSystem] No Guard template available");
            }
        }

        [ButtonGroup("Debug Actions/Row2")]
        [Button("Log All Workers", ButtonSizes.Medium)]
        private void DebugLogWorkers()
        {
            Debug.Log($"<color=cyan>[WorkerSystem]</color> === WORKER STATUS ===");
            Debug.Log($"  Total: {WorkerCount}/{MaxWorkers}");
            Debug.Log($"  Available: {AvailableWorkerCount}");
            Debug.Log($"  Assigned: {AssignedWorkerCount}");
            Debug.Log($"  ---");
            
            foreach (var worker in allWorkers)
            {
                string status = worker.IsAssigned 
                    ? $"@ {worker.AssignedStructure.Data.DisplayName} (+{worker.GetCurrentBonus() * 100f:F0}%)" 
                    : "Available";
                Debug.Log($"  - {worker.CustomName} ({worker.Data?.Role}) - {status}");
            }
        }

        [ButtonGroup("Debug Actions/Row2")]
        [Button("Unassign All", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.4f, 0.4f)]
        private void DebugUnassignAll()
        {
            var assigned = GetAssignedWorkers();
            int count = assigned.Count;
            
            foreach (var worker in assigned)
            {
                UnassignWorker(worker);
            }
            
            Debug.Log($"<color=yellow>[WorkerSystem]</color> Unassigned {count} workers");
        }

        [ButtonGroup("Debug Actions/Row3")]
        [Button("Clear All Workers", ButtonSizes.Medium)]
        [GUIColor(0.9f, 0.2f, 0.2f)]
        private void DebugClearAll()
        {
            int count = allWorkers.Count;
            
            // Unassign all first
            foreach (var worker in allWorkers.ToList())
            {
                RemoveWorker(worker);
            }
            
            allWorkers.Clear();
            nameIndex = 0;
            
            Debug.Log($"<color=red>[WorkerSystem]</color> Cleared {count} workers");
        }
    }
}
