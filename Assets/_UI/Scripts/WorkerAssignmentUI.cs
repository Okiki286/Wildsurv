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

            // Check if structure supports workers
            if (structure.Data.WorkerSlots <= 0)
            {
                Debug.Log($"[WorkerAssignmentUI] {structure.Data.DisplayName} has no worker slots");
                PlaySound(errorSound);
                return;
            }

            currentStructure = structure;

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
                Debug.Log($"<color=green>[WorkerAssignmentUI]</color> Opened for {structure.Data.DisplayName}");
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
            int assignedCount = currentStructure.WorkerCount;
            int maxSlots = data.WorkerSlots;

            string stats = $"HP: {currentStructure.CurrentHealth}/{data.MaxHealth}\n";
            stats += $"Workers: {assignedCount}/{maxSlots}";

            structureStatsText.text = stats;

            // Update slots header
            if (slotsHeaderText != null)
            {
                slotsHeaderText.text = $"Assigned Workers ({assignedCount}/{maxSlots})";
            }
        }

        /// <summary>
        /// Refresh worker slots con Object Pooling (zero allocazioni dopo warm-up)
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

            int totalSlots = currentStructure.Data.WorkerSlots;
            var assignedWorkers = currentStructure.GetAssignedWorkerInstances();

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
                slotUI.Initialize(worker, OnWorkerUnassigned);
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
                Debug.Log($"[WorkerAssignmentUI] Pooled Slots: {totalSlots} active, {slotUIList.Count - totalSlots} pooled inactive");
            }
        }

        /// <summary>
        /// Refresh available workers con Object Pooling (zero allocazioni dopo warm-up)
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
            int neededCount = availableWorkers.Count;

            // Update count text
            if (availableCountText != null)
            {
                availableCountText.text = $"Available Workers ({neededCount})";
            }

            // Check if structure has free slots
            bool hasFreeSslots = currentStructure != null && currentStructure.HasFreeWorkerSlot();

            // Riutilizza o crea worker UI
            for (int i = 0; i < neededCount; i++)
            {
                AvailableWorkerUI workerUI;
                WorkerInstance worker = availableWorkers[i];

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
                workerUI.Initialize(worker, hasFreeSslots, OnWorkerAssigned);
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
                Debug.Log($"[WorkerAssignmentUI] Pooled Available: {neededCount} active, {availableUIList.Count - neededCount} pooled inactive");
            }
        }

        private void UpdateProductionInfo()
        {
            if (currentStructure == null || productionPanel == null) return;

            var data = currentStructure.Data;

            // Only show for Resource structures
            if (data.Category != StructureCategory.Resource || string.IsNullOrEmpty(data.ProducesResourceId))
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
