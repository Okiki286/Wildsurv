using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using WildernessSurvival.Gameplay.Structures;
using WildernessSurvival.Gameplay.Resources;

namespace WildernessSurvival.UI
{
    /// <summary>
    /// UI principale per il menu di costruzione.
    /// Mostra strutture disponibili, costi, e gestisce selezione.
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

        private void LoadStructures()
        {
            allStructures.Clear();
            
            // Carica tutte le StructureData dagli asset
            StructureData[] loaded = Resources.LoadAll<StructureData>("Data/Structures");
            
            if (loaded == null || loaded.Length == 0)
            {
                // Fallback: cerca in tutto il progetto (solo Editor)
                #if UNITY_EDITOR
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:StructureData");
                foreach (string guid in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    StructureData data = UnityEditor.AssetDatabase.LoadAssetAtPath<StructureData>(path);
                    if (data != null)
                    {
                        allStructures.Add(data);
                    }
                }
                #endif
            }
            else
            {
                allStructures.AddRange(loaded);
            }

            // Ordina per tier e poi per nome
            allStructures.Sort((a, b) => 
            {
                int tierCompare = a.Tier.CompareTo(b.Tier);
                if (tierCompare != 0) return tierCompare;
                return a.DisplayName.CompareTo(b.DisplayName);
            });

            if (debugMode)
            {
                Debug.Log($"<color=cyan>[BuildMenuUI]</color> Caricate {allStructures.Count} strutture");
            }
        }

        // ============================================
        // CREAZIONE BOTTONI
        // ============================================

        private void CreateButtons()
        {
            // Pulisci bottoni esistenti
            foreach (var btn in structureButtons)
            {
                if (btn != null)
                {
                    Destroy(btn.gameObject);
                }
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

            // Crea bottone per ogni struttura
            for (int i = 0; i < allStructures.Count; i++)
            {
                StructureData data = allStructures[i];
                GameObject buttonObj = Instantiate(structureButtonPrefab, structureButtonsContainer);
                buttonObj.SetActive(true);
                buttonObj.name = $"Btn_{data.DisplayName}";
                
                BuildMenuButton button = buttonObj.GetComponent<BuildMenuButton>();
                if (button == null)
                {
                    button = buttonObj.AddComponent<BuildMenuButton>();
                }

                button.Initialize(data, i + 1, OnStructureSelected, OnStructureHover, OnStructureHoverExit);
                structureButtons.Add(button);
            }

            if (debugMode)
            {
                Debug.Log($"<color=cyan>[BuildMenuUI]</color> Creati {structureButtons.Count} bottoni");
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
