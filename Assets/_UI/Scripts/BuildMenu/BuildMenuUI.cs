using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using WildernessSurvival.Gameplay.Structures;
using WildernessSurvival.Gameplay.Resources;
using WildernessSurvival.UI.Pooling;

namespace WildernessSurvival.UI
{
    /// <summary>
    /// UI principale per il menu di costruzione.
    /// Mostra strutture disponibili, costi, e gestisce selezione.
    /// OPTIMIZATION: Uses UIElementPool for button lifecycle (zero-allocation menu open/close).
    /// </summary>
    public class BuildMenuUI : MonoBehaviour
    {
        // ============================================
        // SINGLETON
        // ============================================

        public static BuildMenuUI Instance { get; private set; }

        // ============================================
        // RIFERIMENTI UI
        // ============================================

        [TitleGroup("Riferimenti UI")]
        [Required]
        [SerializeField] private GameObject buildMenuPanel;

        [Required]
        [SerializeField] private Transform structureButtonsContainer;

        [Required]
        [SerializeField] private GameObject structureButtonPrefab;

        [TitleGroup("Pooling (GC Optimization)")]
        [InfoBox("UIElementPool elimina allocazioni GC quando ricarichi strutture o filtri categorie", InfoMessageType.Info)]
        [SerializeField]
        [ChildGameObjectsOnly]
        [Tooltip("Pool component per i bottoni (auto-created se non assegnato)")]
        private UIElementPool buttonPool;

        [TitleGroup("Tooltip")]
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private TextMeshProUGUI tooltipTitle;
        [SerializeField] private TextMeshProUGUI tooltipDescription;
        [SerializeField] private TextMeshProUGUI tooltipCosts;
        [SerializeField] private TextMeshProUGUI tooltipStats;

        [TitleGroup("Header")]
        [SerializeField] private TextMeshProUGUI headerText;
        [SerializeField] private Button closeButton;

        [TitleGroup("Filtri Categoria")]
        [SerializeField] private Transform categoryButtonsContainer;
        [SerializeField] private Button allCategoryButton;
        [SerializeField] private Button resourceCategoryButton;
        [SerializeField] private Button defenseCategoryButton;
        [SerializeField] private Button utilityCategoryButton;

        [TitleGroup("Configurazione")]
        [SerializeField] private bool showOnStart = false;
        [SerializeField] private KeyCode toggleKey = KeyCode.B;
        [SerializeField] private bool useHotkeys = true;

        [TitleGroup("Audio")]
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;
        [SerializeField] private AudioClip selectSound;
        [SerializeField] private AudioClip errorSound;

        [TitleGroup("Debug")]
        [SerializeField] private bool debugMode = true;

        [TitleGroup("Mobile Build Fallback")]
        [InfoBox("Se Resources.LoadAll fallisce, usa questa lista manuale (assegna nell'Inspector)", InfoMessageType.Warning)]
        [SerializeField]
        [AssetsOnly]
        [Tooltip("Lista manuale di StructureData per Android build - RIEMPI QUESTA LISTA NELL'INSPECTOR!")]
        private List<StructureData> manualStructuresList = new List<StructureData>();

        // ============================================
        // RUNTIME
        // ============================================

        private List<StructureData> allStructures = new List<StructureData>();
        private List<BuildMenuButton> structureButtons = new List<BuildMenuButton>();
        private StructureCategory? currentFilter = null;
        private StructureData selectedStructure = null;
        private AudioSource audioSource;

        public bool IsOpen => buildMenuPanel != null && buildMenuPanel.activeSelf;

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
            LoadStructures();
            CreateButtons();
            SetupCategoryButtons();
            
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }

            if (showOnStart)
            {
                Show();
            }
            else
            {
                Hide();
            }

            HideTooltip();
        }

        private void Update()
        {
            // Toggle menu con tasto
            if (Input.GetKeyDown(toggleKey))
            {
                Toggle();
            }

            // Hotkeys numeriche per selezione rapida
            if (useHotkeys && IsOpen)
            {
                HandleHotkeyInput();
            }

            // Aggiorna affordability in tempo reale
            if (IsOpen)
            {
                UpdateButtonAffordability();
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
        // CARICAMENTO STRUTTURE
        // ============================================

        /// <summary>
        /// Carica le strutture con strategia multi-fallback per Android.
        /// 1. Resources.LoadAll("Data/Structures") - Percorso specifico
        /// 2. Resources.LoadAll("") - Cerca in TUTTE le cartelle Resources
        /// 3. manualStructuresList - Lista manuale dall'Inspector
        /// 4. AssetDatabase (solo Editor) - Ultimo fallback
        /// </summary>
        private void LoadStructures()
        {
            allStructures.Clear();

            Debug.Log("<color=cyan>[BuildMenu]</color> === LOADING STRUCTURES ===");

            // STRATEGIA 1: Carica da Resources.LoadAll("Data/Structures")
            StructureData[] loaded = Resources.LoadAll<StructureData>("Data/Structures");
            Debug.Log($"<color=cyan>[BuildMenu]</color> Strategy 1 (Data/Structures): Found {loaded?.Length ?? 0} structures");

            if (loaded != null && loaded.Length > 0)
            {
                allStructures.AddRange(loaded);
                Debug.Log($"<color=green>[BuildMenu]</color> ✓ Loaded {loaded.Length} structures from Resources/Data/Structures");
            }
            else
            {
                // STRATEGIA 2: Cerca in TUTTE le cartelle Resources (fallback per Android)
                Debug.LogWarning("<color=yellow>[BuildMenu]</color> Strategy 1 failed, trying Strategy 2 (all Resources folders)...");
                StructureData[] allResources = Resources.LoadAll<StructureData>("");
                Debug.Log($"<color=cyan>[BuildMenu]</color> Strategy 2 (all Resources): Found {allResources?.Length ?? 0} structures");

                if (allResources != null && allResources.Length > 0)
                {
                    allStructures.AddRange(allResources);
                    Debug.Log($"<color=green>[BuildMenu]</color> ✓ Loaded {allResources.Length} structures from ALL Resources folders");
                }
                else
                {
                    // STRATEGIA 3: Lista manuale dall'Inspector (Android build fallback)
                    Debug.LogWarning("<color=yellow>[BuildMenu]</color> Strategy 2 failed, trying Strategy 3 (manual list)...");
                    if (manualStructuresList != null && manualStructuresList.Count > 0)
                    {
                        allStructures.AddRange(manualStructuresList);
                        Debug.Log($"<color=green>[BuildMenu]</color> ✓ Loaded {manualStructuresList.Count} structures from MANUAL LIST (Inspector)");
                    }
                    else
                    {
                        // STRATEGIA 4: AssetDatabase (solo Editor)
                        #if UNITY_EDITOR
                        Debug.LogWarning("<color=yellow>[BuildMenu]</color> Strategy 3 failed, trying Strategy 4 (AssetDatabase - EDITOR ONLY)...");
                        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:StructureData");
                        Debug.Log($"<color=cyan>[BuildMenu]</color> Strategy 4 (AssetDatabase): Found {guids?.Length ?? 0} GUIDs");

                        foreach (string guid in guids)
                        {
                            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                            StructureData data = UnityEditor.AssetDatabase.LoadAssetAtPath<StructureData>(path);
                            if (data != null)
                            {
                                allStructures.Add(data);
                            }
                        }

                        if (allStructures.Count > 0)
                        {
                            Debug.Log($"<color=green>[BuildMenu]</color> ✓ Loaded {allStructures.Count} structures from AssetDatabase (EDITOR)");
                        }
                        #else
                        Debug.LogError("<color=red>[BuildMenu]</color> ✗ ALL STRATEGIES FAILED! NO STRUCTURES LOADED!");
                        Debug.LogError("<color=red>[BuildMenu]</color> ANDROID BUILD FIX REQUIRED:");
                        Debug.LogError("<color=red>[BuildMenu]</color> 1. Move StructureData files to: Assets/Resources/Data/Structures/");
                        Debug.LogError("<color=red>[BuildMenu]</color> 2. OR assign structures manually in BuildMenuUI Inspector (manualStructuresList)");
                        #endif
                    }
                }
            }

            // Ordina per tier e poi per nome
            allStructures.Sort((a, b) =>
            {
                int tierCompare = a.Tier.CompareTo(b.Tier);
                if (tierCompare != 0) return tierCompare;
                return a.DisplayName.CompareTo(b.DisplayName);
            });

            // FINAL DEBUG LOG (critical for Android logcat)
            Debug.Log($"<color=cyan>[BuildMenu]</color> === FINAL RESULT: {allStructures.Count} STRUCTURES LOADED ===");

            if (allStructures.Count == 0)
            {
                Debug.LogError("<color=red>[BuildMenu]</color> ⚠️ WARNING: BUILD MENU WILL BE EMPTY!");
            }
            else
            {
                // Log structure names for debugging
                for (int i = 0; i < allStructures.Count; i++)
                {
                    Debug.Log($"<color=cyan>[BuildMenu]</color>   [{i}] {allStructures[i].DisplayName} ({allStructures[i].Category}) - Tier {allStructures[i].Tier}");
                }
            }
        }

        // ============================================
        // CREAZIONE BOTTONI
        // ============================================

        /// <summary>
        /// Creates or reuses buttons from pool for all structures.
        /// OPTIMIZATION: Uses UIElementPool instead of Destroy + Instantiate.
        /// </summary>
        private void CreateButtons()
        {
            // Initialize pool if not assigned
            if (buttonPool == null)
            {
                // Check if pool component already exists on container
                buttonPool = structureButtonsContainer.GetComponent<UIElementPool>();
                if (buttonPool == null)
                {
                    // Create pool component automatically
                    GameObject poolObj = new GameObject("ButtonPool");
                    poolObj.transform.SetParent(structureButtonsContainer, false);
                    buttonPool = poolObj.AddComponent<UIElementPool>();
                    Debug.Log("<color=yellow>[BuildMenuUI]</color> Auto-created UIElementPool for buttons");
                }
            }

            // ✅ RETURN ALL BUTTONS TO POOL (instead of Destroy)
            if (buttonPool != null)
            {
                buttonPool.ReturnAll();
            }
            structureButtons.Clear();

            if (structureButtonPrefab == null)
            {
                Debug.LogError("[BuildMenuUI] structureButtonPrefab non assegnato!");
                return;
            }

            if (structureButtonsContainer == null)
            {
                Debug.LogError("[BuildMenuUI] structureButtonsContainer non assegnato!");
                return;
            }

            // ✅ GET BUTTONS FROM POOL (instead of Instantiate)
            for (int i = 0; i < allStructures.Count; i++)
            {
                StructureData data = allStructures[i];

                // Get from pool
                BuildMenuButton button = null;
                if (buttonPool != null)
                {
                    button = buttonPool.Get<BuildMenuButton>();
                }
                else
                {
                    // Fallback to Instantiate if pool unavailable
                    GameObject buttonObj = Instantiate(structureButtonPrefab, structureButtonsContainer);
                    button = buttonObj.GetComponent<BuildMenuButton>();
                }

                if (button != null)
                {
                    button.gameObject.SetActive(true);
                    button.gameObject.name = $"Btn_{data.DisplayName}";
                    button.Initialize(data, i + 1, OnStructureSelected, OnStructureHover, OnStructureHoverExit);
                    structureButtons.Add(button);
                }
                else
                {
                    Debug.LogError($"[BuildMenuUI] Failed to create button for {data.DisplayName}");
                }
            }

            if (debugMode)
            {
                Debug.Log($"<color=cyan>[BuildMenuUI]</color> Created {structureButtons.Count} buttons (pooled)");
            }
        }

        private void SetupCategoryButtons()
        {
            if (allCategoryButton != null)
                allCategoryButton.onClick.AddListener(() => FilterByCategory(null));
            
            if (resourceCategoryButton != null)
                resourceCategoryButton.onClick.AddListener(() => FilterByCategory(StructureCategory.Resource));
            
            if (defenseCategoryButton != null)
                defenseCategoryButton.onClick.AddListener(() => FilterByCategory(StructureCategory.Defense));
            
            if (utilityCategoryButton != null)
                utilityCategoryButton.onClick.AddListener(() => FilterByCategory(StructureCategory.Utility));
        }

        // ============================================
        // FILTRI
        // ============================================

        public void FilterByCategory(StructureCategory? category)
        {
            currentFilter = category;
            
            foreach (var button in structureButtons)
            {
                if (button == null) continue;
                
                if (category == null)
                {
                    button.gameObject.SetActive(true);
                }
                else
                {
                    button.gameObject.SetActive(button.Data.Category == category.Value);
                }
            }

            if (debugMode)
            {
                string filterName = category?.ToString() ?? "All";
                Debug.Log($"<color=cyan>[BuildMenuUI]</color> Filtro: {filterName}");
            }
        }

        // ============================================
        // SHOW / HIDE
        // ============================================

        [TitleGroup("Azioni")]
        [Button("Show Menu", ButtonSizes.Medium)]
        [GUIColor(0.4f, 0.8f, 0.4f)]
        public void Show()
        {
            if (buildMenuPanel != null)
            {
                buildMenuPanel.SetActive(true);
                UpdateButtonAffordability();
                PlaySound(openSound);

                if (debugMode)
                {
                    Debug.Log("<color=green>[BuildMenuUI]</color> Menu aperto");
                }
            }
        }

        [Button("Hide Menu", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.4f, 0.4f)]
        public void Hide()
        {
            if (buildMenuPanel != null)
            {
                buildMenuPanel.SetActive(false);
                HideTooltip();
                PlaySound(closeSound);

                if (debugMode)
                {
                    Debug.Log("<color=yellow>[BuildMenuUI]</color> Menu chiuso");
                }
            }
        }

        public void Toggle()
        {
            if (IsOpen)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        // ============================================
        // SELEZIONE STRUTTURA
        // ============================================

        private void OnStructureSelected(StructureData data)
        {
            if (data == null) return;

            // Verifica affordability
            if (!CanAffordStructure(data))
            {
                PlaySound(errorSound);
                if (debugMode)
                {
                    Debug.Log($"<color=red>[BuildMenuUI]</color> Risorse insufficienti per {data.DisplayName}");
                }
                return;
            }

            selectedStructure = data;
            PlaySound(selectSound);

            // Attiva build mode con questa struttura
            if (BuildModeController.Instance != null)
            {
                BuildModeController.Instance.SelectStructure(data);
            }
            else
            {
                Debug.LogWarning("[BuildMenuUI] BuildModeController.Instance è null!");
            }

            // Chiudi menu dopo selezione
            Hide();

            if (debugMode)
            {
                Debug.Log($"<color=green>[BuildMenuUI]</color> Selezionata: {data.DisplayName}");
            }
        }

        // ============================================
        // TOOLTIP
        // ============================================

        private void OnStructureHover(StructureData data)
        {
            if (data == null || tooltipPanel == null) return;

            tooltipPanel.SetActive(true);

            if (tooltipTitle != null)
            {
                tooltipTitle.text = data.DisplayName;
            }

            if (tooltipDescription != null)
            {
                tooltipDescription.text = data.Description;
            }

            if (tooltipCosts != null)
            {
                tooltipCosts.text = FormatCosts(data);
            }

            if (tooltipStats != null)
            {
                tooltipStats.text = FormatStats(data);
            }
        }

        private void OnStructureHoverExit(StructureData data)
        {
            HideTooltip();
        }

        private void HideTooltip()
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }

        private string FormatCosts(StructureData data)
        {
            if (data.BuildCosts == null || data.BuildCosts.Length == 0)
            {
                return "<color=green>Gratis</color>";
            }

            List<string> costStrings = new List<string>();
            foreach (var cost in data.BuildCosts)
            {
                string resourceName = GetResourceDisplayName(cost.resourceId);
                string icon = GetResourceIcon(cost.resourceId);
                
                bool canAfford = ResourceSystem.Instance != null && 
                                 ResourceSystem.Instance.HasResource(cost.resourceId, cost.amount);
                
                string color = canAfford ? "#FFFFFF" : "#FF4444";
                costStrings.Add($"<color={color}>{icon} {resourceName}: {cost.amount}</color>");
            }

            return string.Join("\n", costStrings);
        }

        private string FormatStats(StructureData data)
        {
            List<string> stats = new List<string>();
            
            stats.Add($"<b>Categoria:</b> {GetCategoryIcon(data.Category)} {data.Category}");
            stats.Add($"<b>Tier:</b> {data.Tier}");
            stats.Add($"<b>HP:</b> {data.MaxHealth}");
            
            if (data.WorkerSlots > 0)
            {
                stats.Add($"<b>Worker Slots:</b> {data.WorkerSlots}");
            }

            if (data.Category == StructureCategory.Defense)
            {
                stats.Add($"<b>Danno:</b> {data.AttackDamage}");
                stats.Add($"<b>Range:</b> {data.AttackRange}m");
            }

            if (data.Category == StructureCategory.Resource && !string.IsNullOrEmpty(data.ProducesResourceId))
            {
                string resName = GetResourceDisplayName(data.ProducesResourceId);
                stats.Add($"<b>Produce:</b> {resName}");
                stats.Add($"<b>Rate:</b> {data.BaseProductionRate}/min");
            }

            return string.Join("\n", stats);
        }

        private string GetResourceDisplayName(string resourceId)
        {
            return resourceId?.ToLower() switch
            {
                "warmwood" => "Warmwood",
                "shard" => "Shards",
                "food" => "Food",
                _ => resourceId ?? "Unknown"
            };
        }

        private string GetResourceIcon(string resourceId)
        {
            return resourceId?.ToLower() switch
            {
                "warmwood" => "W",
                "shard" => "S",
                "food" => "F",
                _ => "?"
            };
        }

        private string GetCategoryIcon(StructureCategory category)
        {
            return category switch
            {
                StructureCategory.Resource => "[Res]",
                StructureCategory.Defense => "[Def]",
                StructureCategory.Utility => "[Util]",
                StructureCategory.Tech => "[Tech]",
                _ => "[Bld]"
            };
        }

        // ============================================
        // AFFORDABILITY
        // ============================================

        private void UpdateButtonAffordability()
        {
            foreach (var button in structureButtons)
            {
                if (button != null && button.Data != null)
                {
                    bool canAfford = CanAffordStructure(button.Data);
                    button.SetAffordable(canAfford);
                }
            }
        }

        private bool CanAffordStructure(StructureData data)
        {
            if (data.BuildCosts == null || data.BuildCosts.Length == 0)
            {
                return true;
            }

            if (ResourceSystem.Instance == null)
            {
                return false;
            }

            foreach (var cost in data.BuildCosts)
            {
                if (!ResourceSystem.Instance.HasResource(cost.resourceId, cost.amount))
                {
                    return false;
                }
            }

            return true;
        }

        // ============================================
        // HOTKEYS
        // ============================================

        private void HandleHotkeyInput()
        {
            for (int i = 0; i < 9 && i < structureButtons.Count; i++)
            {
                KeyCode key = KeyCode.Alpha1 + i;
                if (Input.GetKeyDown(key))
                {
                    if (structureButtons[i] != null && structureButtons[i].gameObject.activeSelf)
                    {
                        OnStructureSelected(structureButtons[i].Data);
                    }
                }
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

        [TitleGroup("Debug")]
        [Button("Reload Structures", ButtonSizes.Medium)]
        private void DebugReloadStructures()
        {
            LoadStructures();
            CreateButtons();
        }

        [Button("Log Structure Count", ButtonSizes.Medium)]
        private void DebugLogCount()
        {
            Debug.Log($"<color=cyan>[BuildMenuUI]</color> Strutture caricate: {allStructures.Count}");
            foreach (var s in allStructures)
            {
                Debug.Log($"  - {s.DisplayName} ({s.Category}) - Tier {s.Tier}");
            }
        }

        [Button("Test Select First", ButtonSizes.Medium)]
        private void DebugSelectFirst()
        {
            if (allStructures.Count > 0)
            {
                OnStructureSelected(allStructures[0]);
            }
        }
    }
}
