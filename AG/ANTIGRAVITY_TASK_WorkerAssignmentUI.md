# 🎯 ANTIGRAVITY TASK: Worker Assignment UI

**Progetto:** Wilderness Survival Camp  
**Unity Version:** 6000.0.61f1 LTS  
**Dipendenze:** Odin Inspector, TextMeshPro  
**Priorità:** Alta  
**Tempo Stimato:** 2-3 ore

---

## 📋 OBIETTIVO

Creare un sistema UI completo per assegnare Worker alle Strutture:
1. Panel che mostra struttura selezionata con slot worker
2. Lista worker disponibili (non assegnati)
3. Drag & drop OPPURE click-to-assign
4. Visualizzazione bonus produzione quando worker assegnato
5. Bottone per rimuovere worker da slot
6. Integrazione con StructureController e WorkerSystem

---

## 🏗️ ARCHITETTURA

```
┌─────────────────────────────────────────────────────────┐
│                    STRUTTURA SELEZIONATA                │
│  ┌─────────────────────────────────────────────────┐   │
│  │  🏭 Sawmill (Lv.1)           HP: 100/100        │   │
│  │  Produce: Warmwood  Rate: 5/min (+25% bonus)    │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  WORKER SLOTS (2/2)                                    │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐             │
│  │ 👷 John  │  │ 👷 Mary  │  │  Empty   │             │
│  │ Gatherer │  │ Builder  │  │  Slot    │             │
│  │ [Remove] │  │ [Remove] │  │          │             │
│  └──────────┘  └──────────┘  └──────────┘             │
│                                                         │
│  ════════════════════════════════════════════════════  │
│                                                         │
│  AVAILABLE WORKERS (3)                                 │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐             │
│  │ 👷 Bob   │  │ 👷 Alice │  │ 👷 Tom   │             │
│  │ Guard    │  │ Gatherer │  │ Crafter  │             │
│  │ [Assign] │  │ [Assign] │  │ [Assign] │             │
│  └──────────┘  └──────────┘  └──────────┘             │
│                                                         │
│                              [Close]                    │
└─────────────────────────────────────────────────────────┘
```

---

## 📁 FILE DA CREARE

### 1. `Assets/_Gameplay/Workers/WorkerData.cs`

```csharp
using UnityEngine;
using Sirenix.OdinInspector;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// Definisce un tipo di worker (dati statici).
    /// </summary>
    [CreateAssetMenu(fileName = "NewWorker", menuName = "Wilderness Survival/Data/Worker Definition")]
    public class WorkerData : ScriptableObject
    {
        // ============================================
        // IDENTIFICAZIONE
        // ============================================

        [TitleGroup("Identificazione")]
        [HorizontalGroup("Identificazione/Row1", 0.7f)]
        [LabelWidth(100)]
        [SerializeField] private string workerId;

        [HorizontalGroup("Identificazione/Row1")]
        [PreviewField(50, ObjectFieldAlignment.Right)]
        [HideLabel]
        [SerializeField] private Sprite icon;

        [LabelWidth(100)]
        [SerializeField] private string displayName;

        [TextArea(2, 3)]
        [SerializeField] private string description;

        // ============================================
        // RUOLO E STATS
        // ============================================

        [TitleGroup("Ruolo")]
        [EnumToggleButtons]
        [SerializeField] private WorkerRole role = WorkerRole.Gatherer;

        [TitleGroup("Stats Base")]
        [HorizontalGroup("Stats Base/Row1")]
        [LabelWidth(100)]
        [SerializeField] private float moveSpeed = 3f;

        [HorizontalGroup("Stats Base/Row1")]
        [LabelWidth(100)]
        [SerializeField] private float workSpeed = 1f;

        [TitleGroup("Bonus")]
        [InfoBox("Bonus applicato quando lavora in struttura compatibile")]
        [LabelWidth(150)]
        [SuffixLabel("%", Overlay = true)]
        [Range(0, 100)]
        [SerializeField] private int productionBonus = 25;

        [LabelWidth(150)]
        [SuffixLabel("%", Overlay = true)]
        [Range(0, 100)]
        [SerializeField] private int buildSpeedBonus = 0;

        [LabelWidth(150)]
        [SuffixLabel("%", Overlay = true)]
        [Range(0, 100)]
        [SerializeField] private int combatBonus = 0;

        // ============================================
        // VISUALS
        // ============================================

        [TitleGroup("Visuals")]
        [SerializeField] private GameObject prefab;
        [SerializeField] private Color roleColor = Color.white;

        // ============================================
        // PROPERTIES
        // ============================================

        public string WorkerId => workerId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public WorkerRole Role => role;
        public float MoveSpeed => moveSpeed;
        public float WorkSpeed => workSpeed;
        public int ProductionBonus => productionBonus;
        public int BuildSpeedBonus => buildSpeedBonus;
        public int CombatBonus => combatBonus;
        public GameObject Prefab => prefab;
        public Color RoleColor => roleColor;

        // ============================================
        // METHODS
        // ============================================

        /// <summary>
        /// Calcola bonus totale per una struttura specifica
        /// </summary>
        public float GetBonusForStructure(Structures.StructureData structure)
        {
            if (structure == null) return 0f;

            // Check if role matches structure's allowed roles
            if ((structure.AllowedRoles & role) != 0)
            {
                return productionBonus / 100f;
            }

            // Partial bonus if not ideal role
            return (productionBonus / 100f) * 0.5f;
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(workerId))
            {
                workerId = name.ToLower().Replace(" ", "_");
            }
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = name;
            }
        }
        #endif
    }

    // ============================================
    // WORKER ROLE ENUM
    // ============================================

    [System.Flags]
    public enum WorkerRole
    {
        None = 0,

        [LabelText("⛏️ Gatherer")]
        Gatherer = 1 << 0,

        [LabelText("🔨 Builder")]
        Builder = 1 << 1,

        [LabelText("⚔️ Guard")]
        Guard = 1 << 2,

        [LabelText("🔭 Scout")]
        Scout = 1 << 3,

        [LabelText("⚗️ Crafter")]
        Crafter = 1 << 4,

        [LabelText("🎓 Researcher")]
        Researcher = 1 << 5
    }
}
```

---

### 2. `Assets/_Gameplay/Workers/WorkerInstance.cs`

```csharp
using UnityEngine;
using Sirenix.OdinInspector;
using WildernessSurvival.Gameplay.Structures;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// Istanza runtime di un worker nel gioco.
    /// </summary>
    [System.Serializable]
    public class WorkerInstance
    {
        // ============================================
        // DATA
        // ============================================

        [ShowInInspector]
        [ReadOnly]
        public string InstanceId { get; private set; }

        [ShowInInspector]
        [ReadOnly]
        public WorkerData Data { get; private set; }

        [ShowInInspector]
        [ReadOnly]
        public string CustomName { get; private set; }

        [ShowInInspector]
        [ReadOnly]
        public StructureController AssignedStructure { get; private set; }

        [ShowInInspector]
        [ReadOnly]
        public WorkerState CurrentState { get; private set; }

        // ============================================
        // RUNTIME
        // ============================================

        public GameObject WorldObject { get; set; }
        public Vector3 Position => WorldObject != null ? WorldObject.transform.position : Vector3.zero;
        public bool IsAssigned => AssignedStructure != null;
        public bool IsIdle => CurrentState == WorkerState.Idle && !IsAssigned;

        // ============================================
        // CONSTRUCTOR
        // ============================================

        public WorkerInstance(WorkerData data, string customName = null)
        {
            InstanceId = System.Guid.NewGuid().ToString().Substring(0, 8);
            Data = data;
            CustomName = string.IsNullOrEmpty(customName) ? data.DisplayName : customName;
            CurrentState = WorkerState.Idle;
            AssignedStructure = null;
        }

        // ============================================
        // ASSIGNMENT
        // ============================================

        /// <summary>
        /// Assegna questo worker a una struttura
        /// </summary>
        public bool AssignTo(StructureController structure)
        {
            if (structure == null)
            {
                Debug.LogWarning($"[Worker {CustomName}] Cannot assign to null structure");
                return false;
            }

            if (IsAssigned)
            {
                Debug.LogWarning($"[Worker {CustomName}] Already assigned to {AssignedStructure.Data.DisplayName}");
                return false;
            }

            if (!structure.HasFreeWorkerSlot())
            {
                Debug.LogWarning($"[Worker {CustomName}] Structure {structure.Data.DisplayName} has no free slots");
                return false;
            }

            AssignedStructure = structure;
            CurrentState = WorkerState.Working;

            Debug.Log($"<color=green>[Worker]</color> {CustomName} assigned to {structure.Data.DisplayName}");
            return true;
        }

        /// <summary>
        /// Rimuove questo worker dalla struttura
        /// </summary>
        public void Unassign()
        {
            if (AssignedStructure != null)
            {
                Debug.Log($"<color=yellow>[Worker]</color> {CustomName} unassigned from {AssignedStructure.Data.DisplayName}");
            }

            AssignedStructure = null;
            CurrentState = WorkerState.Idle;
        }

        /// <summary>
        /// Calcola il bonus che questo worker dà alla struttura assegnata
        /// </summary>
        public float GetCurrentBonus()
        {
            if (!IsAssigned || Data == null) return 0f;
            return Data.GetBonusForStructure(AssignedStructure.Data);
        }

        // ============================================
        // STATE
        // ============================================

        public void SetState(WorkerState newState)
        {
            CurrentState = newState;
        }
    }

    // ============================================
    // WORKER STATE ENUM
    // ============================================

    public enum WorkerState
    {
        Idle,
        Moving,
        Working,
        Resting,
        Fleeing
    }
}
```

---

### 3. `Assets/_Gameplay/Workers/WorkerSystem.cs`

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using WildernessSurvival.Gameplay.Structures;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// Sistema centrale per gestire tutti i worker.
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
        [SerializeField] private int maxWorkers = 20;

        [TitleGroup("Worker Templates")]
        [AssetList(Path = "_Content/Data/Workers")]
        [SerializeField] private WorkerData[] workerTemplates;

        [TitleGroup("Starting Workers")]
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
            "James", "Sophie", "Oliver", "Chloe", "Harry", "Emily", "Charlie"
        };

        // ============================================
        // PROPERTIES
        // ============================================

        public int WorkerCount => allWorkers.Count;
        public int MaxWorkers => maxWorkers;
        public int AvailableWorkerCount => allWorkers.Count(w => !w.IsAssigned);
        public int AssignedWorkerCount => allWorkers.Count(w => w.IsAssigned);

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
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
        // SPAWN
        // ============================================

        private void SpawnStartingWorkers()
        {
            if (defaultWorkerType == null)
            {
                Debug.LogWarning("[WorkerSystem] No default worker type set!");
                return;
            }

            for (int i = 0; i < startingWorkerCount; i++)
            {
                CreateWorker(defaultWorkerType);
            }

            if (debugMode)
            {
                Debug.Log($"<color=cyan>[WorkerSystem]</color> Spawned {startingWorkerCount} starting workers");
            }
        }

        /// <summary>
        /// Crea un nuovo worker
        /// </summary>
        public WorkerInstance CreateWorker(WorkerData data, string customName = null)
        {
            if (data == null)
            {
                Debug.LogError("[WorkerSystem] Cannot create worker with null data");
                return null;
            }

            if (allWorkers.Count >= maxWorkers)
            {
                Debug.LogWarning("[WorkerSystem] Max worker limit reached!");
                return null;
            }

            // Generate random name if not provided
            if (string.IsNullOrEmpty(customName))
            {
                customName = workerNames[Random.Range(0, workerNames.Length)];
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
        /// Rimuove un worker
        /// </summary>
        public void RemoveWorker(WorkerInstance worker)
        {
            if (worker == null) return;

            // Unassign first
            if (worker.IsAssigned)
            {
                UnassignWorker(worker);
            }

            allWorkers.Remove(worker);

            if (debugMode)
            {
                Debug.Log($"<color=red>[WorkerSystem]</color> Removed worker: {worker.CustomName}");
            }
        }

        // ============================================
        // ASSIGNMENT
        // ============================================

        /// <summary>
        /// Assegna un worker a una struttura
        /// </summary>
        public bool AssignWorker(WorkerInstance worker, StructureController structure)
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

            // Assign worker
            if (worker.AssignTo(structure))
            {
                structure.AddWorker(worker);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Rimuove un worker da una struttura
        /// </summary>
        public void UnassignWorker(WorkerInstance worker)
        {
            if (worker == null) return;

            if (worker.IsAssigned)
            {
                StructureController structure = worker.AssignedStructure;
                structure?.RemoveWorker(worker);
                worker.Unassign();
            }
        }

        /// <summary>
        /// Rimuove tutti i worker da una struttura
        /// </summary>
        public void UnassignAllFromStructure(StructureController structure)
        {
            if (structure == null) return;

            var workersToUnassign = allWorkers.Where(w => w.AssignedStructure == structure).ToList();
            foreach (var worker in workersToUnassign)
            {
                UnassignWorker(worker);
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
        /// Ottiene worker assegnati a una struttura specifica
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
        /// Trova worker per ID
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
            return allWorkers.Where(w => w.Data.Role == role).ToList();
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
        }

        [ButtonGroup("Debug Actions/Row1")]
        [Button("Add Builder", ButtonSizes.Medium)]
        [GUIColor(0.4f, 0.6f, 0.8f)]
        private void DebugAddBuilder()
        {
            var builder = workerTemplates?.FirstOrDefault(w => w.Role == WorkerRole.Builder);
            if (builder != null)
            {
                CreateWorker(builder);
            }
        }

        [ButtonGroup("Debug Actions/Row2")]
        [Button("Log All Workers", ButtonSizes.Medium)]
        private void DebugLogWorkers()
        {
            Debug.Log($"<color=cyan>[WorkerSystem]</color> Total: {WorkerCount}, Available: {AvailableWorkerCount}, Assigned: {AssignedWorkerCount}");
            foreach (var worker in allWorkers)
            {
                string status = worker.IsAssigned ? $"@ {worker.AssignedStructure.Data.DisplayName}" : "Available";
                Debug.Log($"  - {worker.CustomName} ({worker.Data.Role}) - {status}");
            }
        }

        [ButtonGroup("Debug Actions/Row2")]
        [Button("Unassign All", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.4f, 0.4f)]
        private void DebugUnassignAll()
        {
            var assigned = GetAssignedWorkers();
            foreach (var worker in assigned)
            {
                UnassignWorker(worker);
            }
            Debug.Log($"[WorkerSystem] Unassigned {assigned.Count} workers");
        }
    }
}
```

---

### 4. `Assets/_UI/WorkerAssignment/WorkerAssignmentUI.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using WildernessSurvival.Gameplay.Workers;
using WildernessSurvival.Gameplay.Structures;

namespace WildernessSurvival.UI
{
    /// <summary>
    /// UI per assegnare worker alle strutture.
    /// Si apre quando si clicca su una struttura.
    /// </summary>
    public class WorkerAssignmentUI : MonoBehaviour
    {
        // ============================================
        // SINGLETON
        // ============================================

        public static WorkerAssignmentUI Instance { get; private set; }

        // ============================================
        // RIFERIMENTI UI - PANEL PRINCIPALE
        // ============================================

        [TitleGroup("Panel Principale")]
        [Required]
        [SerializeField] private GameObject assignmentPanel;

        [SerializeField] private TextMeshProUGUI structureNameText;
        [SerializeField] private TextMeshProUGUI structureStatsText;
        [SerializeField] private Image structureIconImage;
        [SerializeField] private Button closeButton;

        // ============================================
        // RIFERIMENTI UI - SLOT WORKERS
        // ============================================

        [TitleGroup("Worker Slots")]
        [SerializeField] private Transform workerSlotsContainer;
        [SerializeField] private GameObject workerSlotPrefab;

        // ============================================
        // RIFERIMENTI UI - AVAILABLE WORKERS
        // ============================================

        [TitleGroup("Available Workers")]
        [SerializeField] private Transform availableWorkersContainer;
        [SerializeField] private GameObject availableWorkerPrefab;
        [SerializeField] private TextMeshProUGUI availableCountText;

        // ============================================
        // RIFERIMENTI UI - PRODUCTION INFO
        // ============================================

        [TitleGroup("Production Info")]
        [SerializeField] private GameObject productionPanel;
        [SerializeField] private TextMeshProUGUI baseProductionText;
        [SerializeField] private TextMeshProUGUI bonusProductionText;
        [SerializeField] private TextMeshProUGUI totalProductionText;

        // ============================================
        // CONFIGURAZIONE
        // ============================================

        [TitleGroup("Configurazione")]
        [SerializeField] private KeyCode closeKey = KeyCode.Escape;
        [SerializeField] private bool closeOnClickOutside = true;

        [TitleGroup("Audio")]
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;
        [SerializeField] private AudioClip assignSound;
        [SerializeField] private AudioClip unassignSound;

        [TitleGroup("Debug")]
        [SerializeField] private bool debugMode = true;

        // ============================================
        // RUNTIME
        // ============================================

        private StructureController currentStructure;
        private List<WorkerSlotUI> slotUIList = new List<WorkerSlotUI>();
        private List<AvailableWorkerUI> availableUIList = new List<AvailableWorkerUI>();
        private AudioSource audioSource;

        public bool IsOpen => assignmentPanel != null && assignmentPanel.activeSelf;
        public StructureController CurrentStructure => currentStructure;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        private void Start()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            Close();
        }

        private void Update()
        {
            if (IsOpen)
            {
                // Close with escape
                if (Input.GetKeyDown(closeKey))
                {
                    Close();
                }

                // Update production display
                UpdateProductionInfo();
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
        // OPEN / CLOSE
        // ============================================

        /// <summary>
        /// Apre il panel per una struttura specifica
        /// </summary>
        public void OpenForStructure(StructureController structure)
        {
            if (structure == null)
            {
                Debug.LogWarning("[WorkerAssignmentUI] Cannot open for null structure");
                return;
            }

            currentStructure = structure;

            // Update header
            UpdateStructureInfo();

            // Create slot UIs
            RefreshWorkerSlots();

            // Create available workers list
            RefreshAvailableWorkers();

            // Update production
            UpdateProductionInfo();

            // Show panel
            assignmentPanel.SetActive(true);
            PlaySound(openSound);

            if (debugMode)
            {
                Debug.Log($"<color=green>[WorkerAssignmentUI]</color> Opened for {structure.Data.DisplayName}");
            }
        }

        /// <summary>
        /// Chiude il panel
        /// </summary>
        [Button("Close Panel")]
        public void Close()
        {
            if (assignmentPanel != null)
            {
                assignmentPanel.SetActive(false);
            }

            currentStructure = null;
            PlaySound(closeSound);

            if (debugMode)
            {
                Debug.Log("<color=yellow>[WorkerAssignmentUI]</color> Closed");
            }
        }

        // ============================================
        // UI UPDATES
        // ============================================

        private void UpdateStructureInfo()
        {
            if (currentStructure == null) return;

            var data = currentStructure.Data;

            if (structureNameText != null)
            {
                structureNameText.text = $"{data.DisplayName} (Lv.{currentStructure.CurrentLevel})";
            }

            if (structureStatsText != null)
            {
                string stats = $"HP: {currentStructure.CurrentHealth}/{data.MaxHealth}";
                stats += $"\nWorkers: {currentStructure.AssignedWorkerCount}/{data.WorkerSlots}";
                structureStatsText.text = stats;
            }

            if (structureIconImage != null && data.Icon != null)
            {
                structureIconImage.sprite = data.Icon;
                structureIconImage.enabled = true;
            }
        }

        private void RefreshWorkerSlots()
        {
            // Clear existing
            foreach (var slot in slotUIList)
            {
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }
            slotUIList.Clear();

            if (currentStructure == null || workerSlotPrefab == null || workerSlotsContainer == null)
            {
                return;
            }

            int totalSlots = currentStructure.Data.WorkerSlots;
            var assignedWorkers = WorkerSystem.Instance?.GetWorkersAtStructure(currentStructure) ?? new List<WorkerInstance>();

            // Create slot for each capacity
            for (int i = 0; i < totalSlots; i++)
            {
                GameObject slotObj = Instantiate(workerSlotPrefab, workerSlotsContainer);
                WorkerSlotUI slotUI = slotObj.GetComponent<WorkerSlotUI>();

                if (slotUI == null)
                {
                    slotUI = slotObj.AddComponent<WorkerSlotUI>();
                }

                // Assign worker if exists for this slot
                WorkerInstance worker = i < assignedWorkers.Count ? assignedWorkers[i] : null;
                slotUI.Initialize(worker, OnWorkerUnassigned);

                slotUIList.Add(slotUI);
            }
        }

        private void RefreshAvailableWorkers()
        {
            // Clear existing
            foreach (var ui in availableUIList)
            {
                if (ui != null)
                {
                    Destroy(ui.gameObject);
                }
            }
            availableUIList.Clear();

            if (WorkerSystem.Instance == null || availableWorkerPrefab == null || availableWorkersContainer == null)
            {
                return;
            }

            var availableWorkers = WorkerSystem.Instance.GetAvailableWorkers();

            // Update count text
            if (availableCountText != null)
            {
                availableCountText.text = $"Available Workers ({availableWorkers.Count})";
            }

            // Create UI for each available worker
            foreach (var worker in availableWorkers)
            {
                GameObject workerObj = Instantiate(availableWorkerPrefab, availableWorkersContainer);
                AvailableWorkerUI workerUI = workerObj.GetComponent<AvailableWorkerUI>();

                if (workerUI == null)
                {
                    workerUI = workerObj.AddComponent<AvailableWorkerUI>();
                }

                bool canAssign = currentStructure != null && currentStructure.HasFreeWorkerSlot();
                workerUI.Initialize(worker, canAssign, OnWorkerAssigned);

                availableUIList.Add(workerUI);
            }
        }

        private void UpdateProductionInfo()
        {
            if (currentStructure == null || productionPanel == null) return;

            var data = currentStructure.Data;

            // Solo per strutture Resource
            if (data.Category != StructureCategory.Resource)
            {
                productionPanel.SetActive(false);
                return;
            }

            productionPanel.SetActive(true);

            float baseRate = data.BaseProductionRate;
            float bonusPercent = currentStructure.GetTotalWorkerBonus() * 100f;
            float totalRate = baseRate * (1f + currentStructure.GetTotalWorkerBonus());

            if (baseProductionText != null)
            {
                baseProductionText.text = $"Base: {baseRate:F1}/min";
            }

            if (bonusProductionText != null)
            {
                string bonusColor = bonusPercent > 0 ? "#44FF44" : "#FFFFFF";
                bonusProductionText.text = $"<color={bonusColor}>Bonus: +{bonusPercent:F0}%</color>";
            }

            if (totalProductionText != null)
            {
                totalProductionText.text = $"Total: {totalRate:F1}/min";
            }
        }

        // ============================================
        // CALLBACKS
        // ============================================

        private void OnWorkerAssigned(WorkerInstance worker)
        {
            if (worker == null || currentStructure == null) return;

            bool success = WorkerSystem.Instance.AssignWorker(worker, currentStructure);

            if (success)
            {
                PlaySound(assignSound);
                RefreshWorkerSlots();
                RefreshAvailableWorkers();
                UpdateStructureInfo();

                if (debugMode)
                {
                    Debug.Log($"<color=green>[WorkerAssignmentUI]</color> Assigned {worker.CustomName} to {currentStructure.Data.DisplayName}");
                }
            }
        }

        private void OnWorkerUnassigned(WorkerInstance worker)
        {
            if (worker == null) return;

            WorkerSystem.Instance.UnassignWorker(worker);

            PlaySound(unassignSound);
            RefreshWorkerSlots();
            RefreshAvailableWorkers();
            UpdateStructureInfo();

            if (debugMode)
            {
                Debug.Log($"<color=yellow>[WorkerAssignmentUI]</color> Unassigned {worker.CustomName}");
            }
        }

        // ============================================
        // AUDIO
        // ============================================

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        // ============================================
        // DEBUG
        // ============================================

        [TitleGroup("Debug Actions")]
        [Button("Refresh All", ButtonSizes.Medium)]
        private void DebugRefresh()
        {
            if (currentStructure != null)
            {
                RefreshWorkerSlots();
                RefreshAvailableWorkers();
                UpdateProductionInfo();
            }
        }
    }
}
```

---

### 5. `Assets/_UI/WorkerAssignment/WorkerSlotUI.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WildernessSurvival.Gameplay.Workers;

namespace WildernessSurvival.UI
{
    /// <summary>
    /// UI per un singolo slot worker in una struttura.
    /// </summary>
    public class WorkerSlotUI : MonoBehaviour
    {
        // ============================================
        // RIFERIMENTI UI
        // ============================================

        [Header("Riferimenti")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image workerIconImage;
        [SerializeField] private TextMeshProUGUI workerNameText;
        [SerializeField] private TextMeshProUGUI workerRoleText;
        [SerializeField] private TextMeshProUGUI bonusText;
        [SerializeField] private Button removeButton;
        [SerializeField] private GameObject emptyStateObject;
        [SerializeField] private GameObject filledStateObject;

        [Header("Colori")]
        [SerializeField] private Color emptyColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        [SerializeField] private Color filledColor = new Color(0.3f, 0.4f, 0.3f, 0.9f);

        // ============================================
        // RUNTIME
        // ============================================

        private WorkerInstance worker;
        private System.Action<WorkerInstance> onRemoveCallback;

        public WorkerInstance Worker => worker;
        public bool IsEmpty => worker == null;

        // ============================================
        // INIZIALIZZAZIONE
        // ============================================

        public void Initialize(WorkerInstance workerInstance, System.Action<WorkerInstance> onRemove)
        {
            worker = workerInstance;
            onRemoveCallback = onRemove;

            if (removeButton != null)
            {
                removeButton.onClick.RemoveAllListeners();
                removeButton.onClick.AddListener(OnRemoveClicked);
            }

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            bool isEmpty = worker == null;

            // Toggle states
            if (emptyStateObject != null)
            {
                emptyStateObject.SetActive(isEmpty);
            }

            if (filledStateObject != null)
            {
                filledStateObject.SetActive(!isEmpty);
            }

            // Background
            if (backgroundImage != null)
            {
                backgroundImage.color = isEmpty ? emptyColor : filledColor;
            }

            if (isEmpty)
            {
                // Empty slot
                if (workerNameText != null)
                {
                    workerNameText.text = "Empty Slot";
                }

                if (workerRoleText != null)
                {
                    workerRoleText.text = "";
                }

                if (bonusText != null)
                {
                    bonusText.text = "";
                }

                if (workerIconImage != null)
                {
                    workerIconImage.enabled = false;
                }

                if (removeButton != null)
                {
                    removeButton.gameObject.SetActive(false);
                }
            }
            else
            {
                // Filled slot
                if (workerNameText != null)
                {
                    workerNameText.text = worker.CustomName;
                }

                if (workerRoleText != null)
                {
                    workerRoleText.text = GetRoleDisplayName(worker.Data.Role);
                }

                if (bonusText != null)
                {
                    float bonus = worker.GetCurrentBonus() * 100f;
                    bonusText.text = bonus > 0 ? $"+{bonus:F0}%" : "";
                    bonusText.color = bonus > 0 ? Color.green : Color.gray;
                }

                if (workerIconImage != null)
                {
                    if (worker.Data.Icon != null)
                    {
                        workerIconImage.sprite = worker.Data.Icon;
                        workerIconImage.enabled = true;
                    }
                    else
                    {
                        workerIconImage.enabled = false;
                    }
                }

                if (removeButton != null)
                {
                    removeButton.gameObject.SetActive(true);
                }
            }
        }

        private string GetRoleDisplayName(WorkerRole role)
        {
            return role switch
            {
                WorkerRole.Gatherer => "⛏️ Gatherer",
                WorkerRole.Builder => "🔨 Builder",
                WorkerRole.Guard => "⚔️ Guard",
                WorkerRole.Scout => "🔭 Scout",
                WorkerRole.Crafter => "⚗️ Crafter",
                WorkerRole.Researcher => "🎓 Researcher",
                _ => role.ToString()
            };
        }

        private void OnRemoveClicked()
        {
            if (worker != null)
            {
                onRemoveCallback?.Invoke(worker);
            }
        }
    }
}
```

---

### 6. `Assets/_UI/WorkerAssignment/AvailableWorkerUI.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using WildernessSurvival.Gameplay.Workers;

namespace WildernessSurvival.UI
{
    /// <summary>
    /// UI per un worker disponibile nella lista.
    /// </summary>
    public class AvailableWorkerUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        // ============================================
        // RIFERIMENTI UI
        // ============================================

        [Header("Riferimenti")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI roleText;
        [SerializeField] private TextMeshProUGUI bonusPreviewText;
        [SerializeField] private Button assignButton;

        [Header("Colori")]
        [SerializeField] private Color normalColor = new Color(0.25f, 0.25f, 0.3f, 0.9f);
        [SerializeField] private Color hoverColor = new Color(0.35f, 0.35f, 0.4f, 1f);
        [SerializeField] private Color disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

        // ============================================
        // RUNTIME
        // ============================================

        private WorkerInstance worker;
        private bool canAssign;
        private System.Action<WorkerInstance> onAssignCallback;

        public WorkerInstance Worker => worker;

        // ============================================
        // INIZIALIZZAZIONE
        // ============================================

        public void Initialize(WorkerInstance workerInstance, bool canBeAssigned, System.Action<WorkerInstance> onAssign)
        {
            worker = workerInstance;
            canAssign = canBeAssigned;
            onAssignCallback = onAssign;

            if (assignButton != null)
            {
                assignButton.onClick.RemoveAllListeners();
                assignButton.onClick.AddListener(OnAssignClicked);
                assignButton.interactable = canAssign;
            }

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (worker == null) return;

            // Name
            if (nameText != null)
            {
                nameText.text = worker.CustomName;
            }

            // Role
            if (roleText != null)
            {
                roleText.text = GetRoleDisplayName(worker.Data.Role);
                roleText.color = worker.Data.RoleColor;
            }

            // Icon
            if (iconImage != null)
            {
                if (worker.Data.Icon != null)
                {
                    iconImage.sprite = worker.Data.Icon;
                    iconImage.enabled = true;
                }
                else
                {
                    // Colore placeholder basato sul ruolo
                    iconImage.enabled = true;
                    iconImage.sprite = null;
                    iconImage.color = worker.Data.RoleColor;
                }
            }

            // Bonus preview (mostra il bonus potenziale)
            if (bonusPreviewText != null)
            {
                var currentStructure = WorkerAssignmentUI.Instance?.CurrentStructure;
                if (currentStructure != null)
                {
                    float bonus = worker.Data.GetBonusForStructure(currentStructure.Data) * 100f;
                    if (bonus > 0)
                    {
                        bonusPreviewText.text = $"+{bonus:F0}%";
                        bonusPreviewText.color = Color.green;
                    }
                    else
                    {
                        bonusPreviewText.text = "+0%";
                        bonusPreviewText.color = Color.gray;
                    }
                }
            }

            // Background
            if (backgroundImage != null)
            {
                backgroundImage.color = canAssign ? normalColor : disabledColor;
            }

            // Button text
            if (assignButton != null)
            {
                var buttonText = assignButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = canAssign ? "Assign" : "Full";
                }
            }
        }

        private string GetRoleDisplayName(WorkerRole role)
        {
            return role switch
            {
                WorkerRole.Gatherer => "⛏️ Gatherer",
                WorkerRole.Builder => "🔨 Builder",
                WorkerRole.Guard => "⚔️ Guard",
                WorkerRole.Scout => "🔭 Scout",
                WorkerRole.Crafter => "⚗️ Crafter",
                WorkerRole.Researcher => "🎓 Researcher",
                _ => role.ToString()
            };
        }

        // ============================================
        // EVENTS
        // ============================================

        private void OnAssignClicked()
        {
            if (worker != null && canAssign)
            {
                onAssignCallback?.Invoke(worker);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (backgroundImage != null && canAssign)
            {
                backgroundImage.color = hoverColor;
            }

            transform.localScale = Vector3.one * 1.02f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = canAssign ? normalColor : disabledColor;
            }

            transform.localScale = Vector3.one;
        }
    }
}
```

---

## 📁 FILE DA MODIFICARE

### 7. Aggiorna `StructureController.cs`

Aggiungi questi metodi e campi:

```csharp
// Aggiungi questi using
using WildernessSurvival.Gameplay.Workers;
using System.Collections.Generic;

// Aggiungi questi campi
[TitleGroup("Workers")]
[ShowInInspector]
[ReadOnly]
private List<WorkerInstance> assignedWorkers = new List<WorkerInstance>();

// Aggiungi queste properties
public int AssignedWorkerCount => assignedWorkers.Count;
public int CurrentLevel { get; private set; } = 1;
public int CurrentHealth { get; private set; }

// Aggiungi questi metodi

private void Start()
{
    CurrentHealth = Data != null ? Data.MaxHealth : 100;
}

/// <summary>
/// Verifica se c'è uno slot worker libero
/// </summary>
public bool HasFreeWorkerSlot()
{
    if (Data == null) return false;
    return assignedWorkers.Count < Data.WorkerSlots;
}

/// <summary>
/// Aggiunge un worker a questa struttura
/// </summary>
public void AddWorker(WorkerInstance worker)
{
    if (worker == null) return;
    if (!HasFreeWorkerSlot()) return;
    if (assignedWorkers.Contains(worker)) return;

    assignedWorkers.Add(worker);
    Debug.Log($"<color=cyan>[Structure]</color> {Data.DisplayName} now has {assignedWorkers.Count} workers");
}

/// <summary>
/// Rimuove un worker da questa struttura
/// </summary>
public void RemoveWorker(WorkerInstance worker)
{
    if (worker == null) return;
    assignedWorkers.Remove(worker);
    Debug.Log($"<color=cyan>[Structure]</color> {Data.DisplayName} now has {assignedWorkers.Count} workers");
}

/// <summary>
/// Ottiene tutti i worker assegnati
/// </summary>
public List<WorkerInstance> GetAssignedWorkers()
{
    return new List<WorkerInstance>(assignedWorkers);
}

/// <summary>
/// Calcola il bonus totale dei worker assegnati
/// </summary>
public float GetTotalWorkerBonus()
{
    float totalBonus = 0f;
    foreach (var worker in assignedWorkers)
    {
        totalBonus += worker.GetCurrentBonus();
    }
    return totalBonus;
}

/// <summary>
/// Chiamato quando si clicca sulla struttura
/// </summary>
public void OnClick()
{
    // Apre il panel di assegnazione worker
    if (WorkerAssignmentUI.Instance != null)
    {
        WorkerAssignmentUI.Instance.OpenForStructure(this);
    }
}
```

---

### 8. Aggiungi Click Detection alle Strutture

Crea o modifica `StructureClickHandler.cs`:

```csharp
using UnityEngine;
using WildernessSurvival.Gameplay.Structures;

namespace WildernessSurvival.Gameplay
{
    /// <summary>
    /// Gestisce i click sulle strutture per aprire il panel assignment.
    /// Aggiungi questo component ai prefab delle strutture.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class StructureClickHandler : MonoBehaviour
    {
        private StructureController structureController;

        private void Awake()
        {
            structureController = GetComponent<StructureController>();
            if (structureController == null)
            {
                structureController = GetComponentInParent<StructureController>();
            }
        }

        private void OnMouseDown()
        {
            // Ignora se siamo in build mode
            if (BuildModeController.Instance != null && BuildModeController.Instance.IsInBuildMode)
            {
                return;
            }

            if (structureController != null)
            {
                structureController.OnClick();
            }
        }
    }
}
```

---

## 🎨 PREFAB DA CREARE

### WorkerAssignmentPanel Hierarchy:

```
Canvas
└── WorkerAssignmentPanel (Panel, center screen)
    ├── Background (Image, semi-transparent)
    │
    ├── Header
    │   ├── StructureIcon (Image)
    │   ├── StructureName (TMP) "Sawmill (Lv.1)"
    │   ├── StructureStats (TMP) "HP: 100/100"
    │   └── CloseButton (Button) "X"
    │
    ├── Divider (Image, line)
    │
    ├── WorkerSlotsSection
    │   ├── SectionTitle (TMP) "Assigned Workers"
    │   └── WorkerSlotsContainer (HorizontalLayoutGroup)
    │       └── [WorkerSlotPrefab instances]
    │
    ├── ProductionPanel
    │   ├── BaseProduction (TMP) "Base: 5/min"
    │   ├── BonusProduction (TMP) "Bonus: +25%"
    │   └── TotalProduction (TMP) "Total: 6.25/min"
    │
    ├── Divider2 (Image, line)
    │
    └── AvailableWorkersSection
        ├── SectionTitle (TMP) "Available Workers (3)"
        └── AvailableWorkersContainer (GridLayoutGroup)
            └── [AvailableWorkerPrefab instances]
```

### WorkerSlotPrefab:

```
WorkerSlot (Panel, 100x120)
├── Background (Image)
├── EmptyState (active when empty)
│   └── EmptyText (TMP) "Empty"
├── FilledState (active when worker assigned)
│   ├── WorkerIcon (Image)
│   ├── WorkerName (TMP)
│   ├── WorkerRole (TMP)
│   ├── BonusText (TMP) "+25%"
│   └── RemoveButton (Button) "Remove"
```

### AvailableWorkerPrefab:

```
AvailableWorker (Panel, 150x80)
├── Background (Image)
├── Icon (Image)
├── NameText (TMP)
├── RoleText (TMP)
├── BonusPreview (TMP) "+25%"
└── AssignButton (Button) "Assign"
```

---

## ⚙️ LAYOUT SETTINGS

### WorkerSlotsContainer (HorizontalLayoutGroup):
```
Spacing: 10
Child Alignment: Middle Center
Child Force Expand Width: false
Child Force Expand Height: false
```

### AvailableWorkersContainer (GridLayoutGroup):
```
Cell Size: 150 x 80
Spacing: 8 x 8
Start Corner: Upper Left
Constraint: Fixed Column Count
Constraint Count: 3
```

---

## 🧪 TEST CHECKLIST

Dopo implementazione, verifica:

- [ ] WorkerSystem crea 3 worker iniziali
- [ ] Clicca su struttura → Panel si apre
- [ ] Panel mostra nome/stats struttura corretti
- [ ] Slot vuoti mostrano "Empty"
- [ ] Lista available workers mostra worker liberi
- [ ] Click "Assign" → Worker appare nello slot
- [ ] Click "Remove" → Worker torna in available
- [ ] Bonus production si aggiorna correttamente
- [ ] ESC chiude il panel
- [ ] Worker non può essere assegnato a 2 strutture
- [ ] Slot pieni disabilitano "Assign" buttons

---

## 📋 ORDINE DI IMPLEMENTAZIONE

1. **WorkerData.cs** - ScriptableObject definition
2. **WorkerInstance.cs** - Runtime worker class
3. **WorkerSystem.cs** - Manager singleton
4. **Crea WorkerData assets** - Almeno Gatherer, Builder, Guard
5. **Modifica StructureController.cs** - Aggiungi worker management
6. **StructureClickHandler.cs** - Click detection
7. **WorkerSlotUI.cs** - Slot UI component
8. **AvailableWorkerUI.cs** - Available worker UI
9. **WorkerAssignmentUI.cs** - Main panel
10. **Crea prefabs** - Panel e slot prefabs
11. **Test completo**

---

## ⚠️ NOTE IMPORTANTI

1. **WorkerRole in StructureData**: Assicurati che `StructureData.cs` abbia la proprietà `AllowedRoles` di tipo `WorkerRole` (flags enum)

2. **Namespace consistency**: Tutti i file Workers in `WildernessSurvival.Gameplay.Workers`

3. **Prefab strutture**: Aggiungi `StructureClickHandler` ai prefab esistenti

4. **ResourceSystem integration**: Il bonus worker dovrebbe influenzare la produzione in `ResourceSystem.ProduceResource()`

---

## 🎯 RISULTATO ATTESO

Un sistema completo di assegnazione worker con:
- Click su struttura → apre panel
- Visualizzazione chiara slot occupati/liberi
- Lista worker disponibili con preview bonus
- Assegnazione/rimozione con un click
- Feedback visivo bonus produzione
- Integrazione completa con StructureController
