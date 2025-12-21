using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using WildernessSurvival.Gameplay.Workers;
using WildernessSurvival.Gameplay.Structures;
using WildernessSurvival.Gameplay.Structures.Housing;
using WildernessSurvival.Gameplay.Core;

namespace WildernessSurvival.UI
{
    /// <summary>
    /// UI per assegnare worker alle strutture.
    /// Si apre quando si clicca su una struttura.
    /// VERSIONE OTTIMIZZATA: Object Pooling per eliminare GC da Instantiate/Destroy.
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
        [Required]
        [SerializeField] private Transform workerSlotsContainer;
        [Required]
        [SerializeField] private GameObject workerSlotPrefab;
        [SerializeField] private TextMeshProUGUI slotsHeaderText;

        // ============================================
        // RIFERIMENTI UI - AVAILABLE WORKERS
        // ============================================

        [TitleGroup("Available Workers")]
        [Required]
        [SerializeField] private Transform availableWorkersContainer;
        [Required]
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
        // RIFERIMENTI UI - RECRUIT (WAYSTONE ONLY)
        // ============================================

        [TitleGroup("Recruit (Waystone)")]
        [Tooltip("Sezione UI per il reclutamento, visibile solo sul Waystone")]
        [SerializeField] private GameObject recruitSection;
        [SerializeField] private RecruitUI recruitUIComponent;

        // ============================================
        // CONFIGURAZIONE
        // ============================================

        [TitleGroup("Configurazione")]
        [SerializeField] private KeyCode closeKey = KeyCode.Escape;
        #pragma warning disable CS0414 // Reserved for future keyboard shortcut features
        [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
        [SerializeField] private bool closeOnClickOutside = true;
        #pragma warning restore CS0414

        [TitleGroup("Audio")]
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;
        [SerializeField] private AudioClip assignSound;
        [SerializeField] private AudioClip unassignSound;
        [SerializeField] private AudioClip errorSound;

        [TitleGroup("Debug")]
        [SerializeField] private bool debugMode = true;

        // ============================================
        // RUNTIME
        // ============================================

        private StructureController currentStructure;
        private ShelterHome currentShelter; // Cached ShelterHome for Housing structures
        private bool isHousingMode = false; // True when showing Housing UI
        private bool isWaystoneMode = false; // True when showing Waystone UI
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

            // Start closed
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

                // Update displays in real-time
                UpdateProductionInfo();
                UpdateStructureStats();
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

            // Determine if this is a Housing structure
            bool isHousing = structure.Data != null && structure.Data.Category == StructureCategory.Housing;
            bool isBuilding = structure.State == StructureState.Building;

            // For Housing structures, get ShelterHome component
            ShelterHome shelter = null;
            if (isHousing)
            {
                shelter = structure.GetComponent<ShelterHome>();
            }

            // Check if structure supports workers/residents
            if (isHousing)
            {
                // Housing: allow if Building (1 builder slot) OR if Operating with ShelterHome
                if (isBuilding)
                {
                    // Building mode: show builder slot
                    isHousingMode = false; // Use standard builder logic
                }
                else
                {
                    // Operating mode: need ShelterHome for residents
                    if (shelter == null)
                    {
                        Debug.LogWarning($"[WorkerAssignmentUI] {structure.Data.DisplayName} is Housing but has no ShelterHome component!");
                        PlaySound(errorSound);
                        return;
                    }
                    isHousingMode = true;
                }
            }
            else
            {
                // Non-Housing: standard WorkerSlots check
                if (structure.Data.WorkerSlots <= 0)
                {
                    Debug.Log($"[WorkerAssignmentUI] {structure.Data.DisplayName} has no worker slots");
                    PlaySound(errorSound);
                    return;
                }
                isHousingMode = false;
            }

            currentStructure = structure;
            currentShelter = shelter;

            if (isHousingMode && currentShelter != null)
            {
                currentShelter.SyncResidents();
            }

            // Check if this is the Waystone
            isWaystoneMode = structure.GetComponent<WaystoneBeaconController>() != null;
            UpdateRecruitSection();

            // Update all UI elements
            UpdateStructureInfo();
            RefreshWorkerSlots();
            RefreshAvailableWorkers();
            UpdateProductionInfo();

            // Show panel
            if (assignmentPanel != null)
            {
                assignmentPanel.SetActive(true);
            }

            PlaySound(openSound);

            if (debugMode)
            {
                string mode = isHousingMode ? "HOUSING-RESIDENTS" : (isHousing && isBuilding ? "HOUSING-BUILDER" : "STANDARD");
                Debug.Log($"<color=green>[WorkerAssignmentUI]</color> Opened for {structure.Data.DisplayName} ({mode})");
            }
        }

        /// <summary>
        /// Chiude il panel
        /// </summary>
        [Button("Close Panel", ButtonSizes.Medium)]
        public void Close()
        {
            if (assignmentPanel != null)
            {
                assignmentPanel.SetActive(false);
            }

            currentStructure = null;
            currentShelter = null;
            isHousingMode = false;
            isWaystoneMode = false;

            // Hide recruit section
            if (recruitSection != null)
            {
                recruitSection.SetActive(false);
            }

            // Disattiva tutti gli elementi pooled invece di distruggerli
            ClearSlotUIs();
            ClearAvailableUIs();

            PlaySound(closeSound);

            if (debugMode)
            {
                Debug.Log("<color=yellow>[WorkerAssignmentUI]</color> Closed");
            }
        }

        /// <summary>
        /// Toggle panel per struttura corrente o ultima selezionata
        /// </summary>
        public void Toggle()
        {
            if (IsOpen)
            {
                Close();
            }
            else if (currentStructure != null)
            {
                OpenForStructure(currentStructure);
            }
        }

        // ============================================
        // UI UPDATES
        // ============================================

        private void UpdateStructureInfo()
        {
            if (currentStructure == null || currentStructure.Data == null) return;

            var data = currentStructure.Data;

            // Structure name with level
            if (structureNameText != null)
            {
                structureNameText.text = $"{data.DisplayName} (Lv.{currentStructure.CurrentLevel})";
            }

            // Structure icon
            if (structureIconImage != null)
            {
                if (data.Icon != null)
                {
                    structureIconImage.sprite = data.Icon;
                    structureIconImage.enabled = true;
                    structureIconImage.color = Color.white;
                }
                else
                {
                    // Use category color as placeholder
                    structureIconImage.sprite = null;
                    structureIconImage.enabled = true;
                    structureIconImage.color = GetCategoryColor(data.Category);
                }
            }

            UpdateStructureStats();
        }

        private void UpdateStructureStats()
        {
            if (currentStructure == null || structureStatsText == null) return;

            var data = currentStructure.Data;
            string stats = $"HP: {currentStructure.CurrentHealth}/{data.MaxHealth}\n";

            int assignedCount;
            int maxSlots;
            string slotLabel;

            if (isHousingMode && currentShelter != null)
            {
                // Housing Operating mode: show residents
                assignedCount = currentShelter.ResidentCount;
                maxSlots = currentShelter.Capacity;
                slotLabel = "Residents";
            }
            else if (currentStructure.State == StructureState.Building)
            {
                // Building mode (including Housing): show builder
                assignedCount = currentStructure.CurrentBuilder != null ? 1 : 0;
                maxSlots = 1;
                slotLabel = "Builder";
            }
            else
            {
                // Standard Operating mode
                assignedCount = currentStructure.WorkerCount;
                maxSlots = data.WorkerSlots;
                slotLabel = "Workers";
            }

            stats += $"{slotLabel}: {assignedCount}/{maxSlots}";
            structureStatsText.text = stats;

            // Update slots header
            if (slotsHeaderText != null)
            {
                slotsHeaderText.text = $"Assigned {slotLabel} ({assignedCount}/{maxSlots})";
            }
        }

        /// <summary>
        /// Refresh worker slots con Object Pooling (zero allocazioni dopo warm-up)
        /// Supports: Standard structures, Building mode, and Housing residents mode
        /// </summary>
        private void RefreshWorkerSlots()
        {
            if (currentStructure == null || workerSlotPrefab == null || workerSlotsContainer == null)
            {
                // Disattiva tutti se non c'è struttura
                for (int i = 0; i < slotUIList.Count; i++)
                {
                    if (slotUIList[i] != null)
                    {
                        slotUIList[i].gameObject.SetActive(false);
                    }
                }
                return;
            }

            int totalSlots;
            List<WorkerInstance> assignedWorkers = new List<WorkerInstance>();

            if (isHousingMode && currentShelter != null)
            {
                // Housing Operating mode: show residents
                totalSlots = currentShelter.Capacity;
                foreach (var resident in currentShelter.Residents)
                {
                    assignedWorkers.Add(resident);
                }
            }
            else if (currentStructure.State == StructureState.Building)
            {
                // Building mode (including Housing): show 1 builder slot
                totalSlots = 1;
                if (currentStructure.CurrentBuilder != null)
                {
                    assignedWorkers.Add(currentStructure.CurrentBuilder);
                }
            }
            else
            {
                // Standard Operating mode
                totalSlots = currentStructure.Data.WorkerSlots;
                assignedWorkers.AddRange(currentStructure.GetAssignedWorkerInstances());
            }

            // Choose correct callback based on mode
            System.Action<WorkerInstance> unassignCallback = isHousingMode ? OnResidentUnassigned : OnWorkerUnassigned;

            // Riutilizza o crea slot UI
            for (int i = 0; i < totalSlots; i++)
            {
                WorkerSlotUI slotUI;

                if (i < slotUIList.Count)
                {
                    // Ricicla elemento esistente
                    slotUI = slotUIList[i];
                    if (slotUI != null)
                    {
                        slotUI.gameObject.SetActive(true);
                    }
                    else
                    {
                        // Elemento nella lista era null, ricrea
                        GameObject slotObj = Instantiate(workerSlotPrefab, workerSlotsContainer);
                        slotObj.name = $"WorkerSlot_{i}";
                        slotUI = slotObj.GetComponent<WorkerSlotUI>();
                        if (slotUI == null)
                        {
                            slotUI = slotObj.AddComponent<WorkerSlotUI>();
                        }
                        slotUIList[i] = slotUI;
                    }
                }
                else
                {
                    // Crea nuovo elemento e aggiungi al pool
                    GameObject slotObj = Instantiate(workerSlotPrefab, workerSlotsContainer);
                    slotObj.name = $"WorkerSlot_{i}";
                    slotUI = slotObj.GetComponent<WorkerSlotUI>();
                    if (slotUI == null)
                    {
                        slotUI = slotObj.AddComponent<WorkerSlotUI>();
                    }
                    slotUIList.Add(slotUI);
                }

                // Inizializza con dati corretti
                WorkerInstance worker = i < assignedWorkers.Count ? assignedWorkers[i] : null;
                slotUI.Initialize(worker, unassignCallback);
            }

            // Disattiva gli slot in eccesso (oltre totalSlots)
            for (int i = totalSlots; i < slotUIList.Count; i++)
            {
                if (slotUIList[i] != null)
                {
                    slotUIList[i].gameObject.SetActive(false);
                }
            }

            if (debugMode)
            {
                string mode = isHousingMode ? "RESIDENTS" : "WORKERS";
                Debug.Log($"[WorkerAssignmentUI] Pooled Slots ({mode}): {totalSlots} active, {slotUIList.Count - totalSlots} pooled inactive");
            }
        }

        /// <summary>
        /// Refresh available workers con Object Pooling (zero allocazioni dopo warm-up)
        /// Supports both standard worker assignment and Housing resident assignment
        /// </summary>
        private void RefreshAvailableWorkers()
        {
            if (WorkerSystem.Instance == null || availableWorkerPrefab == null || availableWorkersContainer == null)
            {
                // Disattiva tutti se sistema non disponibile
                for (int i = 0; i < availableUIList.Count; i++)
                {
                    if (availableUIList[i] != null)
                    {
                        availableUIList[i].gameObject.SetActive(false);
                    }
                }
                return;
            }

            var availableWorkers = WorkerSystem.Instance.GetAvailableWorkers();

            // For Housing residents mode, also include assigned workers (they can have a home AND a job)
            List<WorkerInstance> eligibleWorkers;
            if (isHousingMode && currentShelter != null)
            {
                // For housing, we show all workers who don't already live elsewhere.
                // We ALSO filter out workers who live HERE (they are in the assigned list).
                eligibleWorkers = new List<WorkerInstance>();
                eligibleWorkers.AddRange(availableWorkers);
                eligibleWorkers.AddRange(WorkerSystem.Instance.GetAssignedWorkers());

                // Filter out workers who already have a home (any home)
                // This prevents assigning a worker to multiple houses or accidental re-assignment.
                // If the user wants to MOVE a worker, they should unassign from House A first,
                // or we could show them but marked. For now, let's just show homeless workers + current residents (to be filtered).
                
                eligibleWorkers.RemoveAll(w => w.AssignedHome != null);
                
                // [Optional] If you want to show workers with homes but let them be reassigned,
                // you would only filter currentShelter. But removing all assignedHome != null is safer for now.
            }
            else
            {
                eligibleWorkers = availableWorkers;
            }

            int neededCount = eligibleWorkers.Count;

            // Update count text
            if (availableCountText != null)
            {
                string label = isHousingMode ? "Available for Residence" : "Available Workers";
                availableCountText.text = $"{label} ({neededCount})";
            }

            // Check if structure/shelter has free slots
            bool hasFreeSlots;
            if (isHousingMode && currentShelter != null)
            {
                hasFreeSlots = currentShelter.HasFreeResidentSlot;
            }
            else if (currentStructure != null && currentStructure.State == StructureState.Building)
            {
                hasFreeSlots = currentStructure.CurrentBuilder == null;
            }
            else
            {
                hasFreeSlots = currentStructure != null && currentStructure.HasFreeWorkerSlot();
            }

            // Choose correct callback based on mode
            System.Action<WorkerInstance> assignCallback = isHousingMode ? OnResidentAssigned : OnWorkerAssigned;

            // Riutilizza o crea worker UI
            for (int i = 0; i < neededCount; i++)
            {
                AvailableWorkerUI workerUI;
                WorkerInstance worker = eligibleWorkers[i];

                if (i < availableUIList.Count)
                {
                    // Ricicla elemento esistente
                    workerUI = availableUIList[i];
                    if (workerUI != null)
                    {
                        workerUI.gameObject.SetActive(true);
                    }
                    else
                    {
                        // Elemento nella lista era null, ricrea
                        GameObject workerObj = Instantiate(availableWorkerPrefab, availableWorkersContainer);
                        workerObj.name = $"AvailableWorker_{worker.CustomName}";
                        workerUI = workerObj.GetComponent<AvailableWorkerUI>();
                        if (workerUI == null)
                        {
                            workerUI = workerObj.AddComponent<AvailableWorkerUI>();
                        }
                        availableUIList[i] = workerUI;
                    }
                }
                else
                {
                    // Crea nuovo elemento e aggiungi al pool
                    GameObject workerObj = Instantiate(availableWorkerPrefab, availableWorkersContainer);
                    workerObj.name = $"AvailableWorker_{worker.CustomName}";
                    workerUI = workerObj.GetComponent<AvailableWorkerUI>();
                    if (workerUI == null)
                    {
                        workerUI = workerObj.AddComponent<AvailableWorkerUI>();
                    }
                    availableUIList.Add(workerUI);
                }

                // Inizializza con dati corretti
                workerUI.Initialize(worker, hasFreeSlots, assignCallback);
            }

            // Disattiva gli elementi in eccesso (oltre neededCount)
            for (int i = neededCount; i < availableUIList.Count; i++)
            {
                if (availableUIList[i] != null)
                {
                    availableUIList[i].gameObject.SetActive(false);
                }
            }

            if (debugMode)
            {
                string mode = isHousingMode ? "RESIDENTS" : "WORKERS";
                Debug.Log($"[WorkerAssignmentUI] Pooled Available ({mode}): {neededCount} active, {availableUIList.Count - neededCount} pooled inactive");
            }
        }

        private void UpdateProductionInfo()
        {
            if (currentStructure == null || productionPanel == null) return;

            var data = currentStructure.Data;

            // Hide for Housing (no production) and non-Resource structures
            if (isHousingMode || data.Category == StructureCategory.Housing ||
                data.Category != StructureCategory.Resource || string.IsNullOrEmpty(data.ProducesResourceId))
            {
                productionPanel.SetActive(false);
                return;
            }

            productionPanel.SetActive(true);

            float baseRate = data.BaseProductionRate;
            
            // Calculate worker bonus locally
            float bonusMultiplier = 0f;
            foreach (var w in currentStructure.GetAssignedWorkerInstances())
            {
                bonusMultiplier += w.GetProductionBonus(currentStructure.Data);
            }
            
            float bonusPercent = bonusMultiplier * 100f;
            float totalRate = baseRate * (1f + bonusMultiplier);

            if (baseProductionText != null)
            {
                baseProductionText.text = $"Base: {baseRate:F1}/min";
            }

            if (bonusProductionText != null)
            {
                string color = bonusPercent > 0 ? "#44FF44" : "#AAAAAA";
                bonusProductionText.text = $"<color={color}>Bonus: +{bonusPercent:F0}%</color>";
            }

            if (totalProductionText != null)
            {
                totalProductionText.text = $"<b>Total: {totalRate:F1}/min</b>";
            }
        }

        /// <summary>
        /// Mostra/nasconde la sezione Recruit in base a isWaystoneMode.
        /// </summary>
        private void UpdateRecruitSection()
        {
            if (recruitSection == null) return;

            if (isWaystoneMode)
            {
                recruitSection.SetActive(true);

                // Force refresh del RecruitUI
                if (recruitUIComponent != null)
                {
                    recruitUIComponent.enabled = true;
                    recruitUIComponent.Bind(); // Immediate refresh
                }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (debugMode)
                {
                    Debug.Log("<color=cyan>[WorkerAssignmentUI]</color> Waystone detected - Recruit section VISIBLE");
                }
#endif
            }
            else
            {
                recruitSection.SetActive(false);
            }
        }

        /// <summary>
        /// Disattiva tutti gli slot UI senza distruggerli (Object Pooling)
        /// </summary>
        private void ClearSlotUIs()
        {
            for (int i = 0; i < slotUIList.Count; i++)
            {
                if (slotUIList[i] != null)
                {
                    slotUIList[i].gameObject.SetActive(false);
                }
            }
            // NON chiamiamo slotUIList.Clear() - manteniamo il pool!
        }

        /// <summary>
        /// Disattiva tutti gli available worker UI senza distruggerli (Object Pooling)
        /// </summary>
        private void ClearAvailableUIs()
        {
            for (int i = 0; i < availableUIList.Count; i++)
            {
                if (availableUIList[i] != null)
                {
                    availableUIList[i].gameObject.SetActive(false);
                }
            }
            // NON chiamiamo availableUIList.Clear() - manteniamo il pool!
        }

        // ============================================
        // CALLBACKS
        // ============================================

        private void OnWorkerAssigned(WorkerInstance worker)
        {
            if (worker == null || currentStructure == null) return;

            if (WorkerSystem.Instance == null)
            {
                Debug.LogError("[WorkerAssignmentUI] WorkerSystem.Instance is null!");
                return;
            }

            bool success = WorkerSystem.Instance.AssignWorker(worker, currentStructure);

            if (success)
            {
                PlaySound(assignSound);
                
                // Refresh both lists
                RefreshWorkerSlots();
                RefreshAvailableWorkers();
                UpdateStructureStats();

                if (debugMode)
                {
                    float bonus = worker.GetCurrentBonus() * 100f;
                    Debug.Log($"<color=green>[WorkerAssignmentUI]</color> Assigned {worker.CustomName} to {currentStructure.Data.DisplayName} (+{bonus:F0}%)");
                }
            }
            else
            {
                PlaySound(errorSound);
                Debug.LogWarning($"[WorkerAssignmentUI] Failed to assign {worker.CustomName}");
            }
        }

        private void OnWorkerUnassigned(WorkerInstance worker)
        {
            if (worker == null) return;

            if (WorkerSystem.Instance == null)
            {
                Debug.LogError("[WorkerAssignmentUI] WorkerSystem.Instance is null!");
                return;
            }

            WorkerSystem.Instance.UnassignWorker(worker);

            PlaySound(unassignSound);

            // Refresh both lists
            RefreshWorkerSlots();
            RefreshAvailableWorkers();
            UpdateStructureStats();

            if (debugMode)
            {
                Debug.Log($"<color=yellow>[WorkerAssignmentUI]</color> Unassigned {worker.CustomName}");
            }
        }

        // ============================================
        // HOUSING RESIDENT CALLBACKS
        // ============================================

        /// <summary>
        /// Called when assigning a worker as resident to a Housing structure.
        /// Does NOT create a production job - only sets home for night retreat.
        /// </summary>
        private void OnResidentAssigned(WorkerInstance worker)
        {
            if (worker == null || currentShelter == null) return;

            bool success = currentShelter.AssignResident(worker);

            if (success)
            {
                PlaySound(assignSound);

                // Refresh both lists
                RefreshWorkerSlots();
                RefreshAvailableWorkers();
                UpdateStructureStats();

                if (debugMode)
                {
                    Debug.Log($"<color=cyan>[WorkerAssignmentUI]</color> {worker.CustomName} assigned as RESIDENT to {currentStructure.Data.DisplayName}");
                }
            }
            else
            {
                PlaySound(errorSound);
                Debug.LogWarning($"[WorkerAssignmentUI] Failed to assign {worker.CustomName} as resident (shelter full?)");
            }
        }

        /// <summary>
        /// Called when removing a worker from Housing residence.
        /// Does NOT affect their production job assignment.
        /// </summary>
        private void OnResidentUnassigned(WorkerInstance worker)
        {
            if (worker == null || currentShelter == null) return;

            currentShelter.UnassignResident(worker);

            PlaySound(unassignSound);

            // Refresh both lists
            RefreshWorkerSlots();
            RefreshAvailableWorkers();
            UpdateStructureStats();

            if (debugMode)
            {
                Debug.Log($"<color=orange>[WorkerAssignmentUI]</color> {worker.CustomName} removed from residence");
            }
        }

        // ============================================
        // HELPERS
        // ============================================

        private Color GetCategoryColor(StructureCategory category)
        {
            return category switch
            {
                StructureCategory.Resource => new Color(0.4f, 0.7f, 0.3f),
                StructureCategory.Defense => new Color(0.7f, 0.3f, 0.3f),
                StructureCategory.Utility => new Color(0.3f, 0.5f, 0.7f),
                StructureCategory.Tech => new Color(0.6f, 0.3f, 0.7f),
                StructureCategory.Housing => new Color(0.9f, 0.7f, 0.4f), // Warm orange for homes
                _ => Color.gray
            };
        }

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
        private void DebugRefreshAll()
        {
            if (currentStructure != null)
            {
                UpdateStructureInfo();
                RefreshWorkerSlots();
                RefreshAvailableWorkers();
                UpdateProductionInfo();
                Debug.Log("[WorkerAssignmentUI] Refreshed all UI elements");
            }
            else
            {
                Debug.LogWarning("[WorkerAssignmentUI] No structure selected");
            }
        }

        [TitleGroup("Debug Actions")]
        [Button("Print Pool Stats", ButtonSizes.Medium)]
        private void DebugPrintPoolStats()
        {
            int activeSlots = 0;
            int activeAvailable = 0;

            foreach (var slot in slotUIList)
            {
                if (slot != null && slot.gameObject.activeSelf) activeSlots++;
            }
            foreach (var ui in availableUIList)
            {
                if (ui != null && ui.gameObject.activeSelf) activeAvailable++;
            }

            Debug.Log($"=== POOL STATS ===\n" +
                      $"Slot Pool: {slotUIList.Count} total, {activeSlots} active\n" +
                      $"Available Pool: {availableUIList.Count} total, {activeAvailable} active");
        }
    }
}
