# 🎯 ANTIGRAVITY TASK: Implementare UI Build Menu Completa

**Progetto:** Wilderness Survival Camp  
**Unity Version:** 6000.0.61f1 LTS  
**Dipendenze:** Odin Inspector, TextMeshPro  
**Priorità:** Alta

---

## 📋 OBIETTIVO

Creare un sistema UI completo per la costruzione di strutture che includa:
1. Panel laterale con lista strutture disponibili
2. Bottoni con icone, nomi e costi
3. Tooltip con descrizioni
4. Feedback visivo per affordability
5. Integrazione con BuildModeController esistente
6. Hotkeys numeriche (1-9) per selezione rapida

---

## 📁 FILE DA CREARE

### 1. `Assets/_UI/BuildMenu/BuildMenuUI.cs`

```csharp
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
            StructureData[] loaded = UnityEngine.Resources.LoadAll<StructureData>("Data/Structures");
            
            if (loaded == null || loaded.Length == 0)
            {
                // Fallback: cerca in tutto il progetto
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

            // Crea bottone per ogni struttura
            for (int i = 0; i < allStructures.Count; i++)
            {
                StructureData data = allStructures[i];
                GameObject buttonObj = Instantiate(structureButtonPrefab, structureButtonsContainer);
                
                BuildMenuButton button = buttonObj.GetComponent<BuildMenuButton>();
                if (button == null)
                {
                    button = buttonObj.AddComponent<BuildMenuButton>();
                }

                button.Initialize(data, i + 1, OnStructureSelected, OnStructureHover, OnStructureHoverExit);
                structureButtons.Add(button);
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

        [Button("Show Menu")]
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

        [Button("Hide Menu")]
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
                return "Gratis";
            }

            List<string> costStrings = new List<string>();
            foreach (var cost in data.BuildCosts)
            {
                string resourceName = cost.resourceId;
                bool canAfford = ResourceSystem.Instance != null && 
                                 ResourceSystem.Instance.HasResource(cost.resourceId, cost.amount);
                
                string color = canAfford ? "white" : "red";
                costStrings.Add($"<color={color}>{resourceName}: {cost.amount}</color>");
            }

            return string.Join("\n", costStrings);
        }

        private string FormatStats(StructureData data)
        {
            List<string> stats = new List<string>();
            
            stats.Add($"Categoria: {data.Category}");
            stats.Add($"Tier: {data.Tier}");
            stats.Add($"HP: {data.MaxHealth}");
            
            if (data.WorkerSlots > 0)
            {
                stats.Add($"Worker Slots: {data.WorkerSlots}");
            }

            if (data.Category == StructureCategory.Defense)
            {
                stats.Add($"Danno: {data.AttackDamage}");
                stats.Add($"Range: {data.AttackRange}");
            }

            if (data.Category == StructureCategory.Resource)
            {
                stats.Add($"Produce: {data.ProducesResourceId}");
                stats.Add($"Rate: {data.BaseProductionRate}/min");
            }

            return string.Join("\n", stats);
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
                    if (structureButtons[i].gameObject.activeSelf)
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

        [TitleGroup("Debug Actions")]
        [Button("Reload Structures")]
        private void DebugReloadStructures()
        {
            LoadStructures();
            CreateButtons();
        }

        [Button("Log Structure Count")]
        private void DebugLogCount()
        {
            Debug.Log($"Strutture caricate: {allStructures.Count}");
            foreach (var s in allStructures)
            {
                Debug.Log($"  - {s.DisplayName} ({s.Category})");
            }
        }
    }
}
```

---

### 2. `Assets/_UI/BuildMenu/BuildMenuButton.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using WildernessSurvival.Gameplay.Structures;

namespace WildernessSurvival.UI
{
    /// <summary>
    /// Singolo bottone nel menu costruzione.
    /// Gestisce visualizzazione, hover, click.
    /// </summary>
    public class BuildMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        // ============================================
        // RIFERIMENTI UI
        // ============================================

        [Header("Riferimenti")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI hotkeyText;
        [SerializeField] private GameObject lockedOverlay;
        [SerializeField] private GameObject selectedIndicator;

        [Header("Colori")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = new Color(0.9f, 0.95f, 1f);
        [SerializeField] private Color selectedColor = new Color(0.7f, 0.9f, 0.7f);
        [SerializeField] private Color unaffordableColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private Color unaffordableTextColor = new Color(1f, 0.3f, 0.3f);

        // ============================================
        // RUNTIME
        // ============================================

        private StructureData data;
        private int hotkeyNumber;
        private bool isAffordable = true;
        private bool isSelected = false;

        private System.Action<StructureData> onSelectCallback;
        private System.Action<StructureData> onHoverCallback;
        private System.Action<StructureData> onHoverExitCallback;

        public StructureData Data => data;

        // ============================================
        // INIZIALIZZAZIONE
        // ============================================

        public void Initialize(
            StructureData structureData, 
            int hotkey,
            System.Action<StructureData> onSelect,
            System.Action<StructureData> onHover,
            System.Action<StructureData> onHoverExit)
        {
            data = structureData;
            hotkeyNumber = hotkey;
            onSelectCallback = onSelect;
            onHoverCallback = onHover;
            onHoverExitCallback = onHoverExit;

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (data == null) return;

            // Nome
            if (nameText != null)
            {
                nameText.text = data.DisplayName;
            }

            // Icona
            if (iconImage != null && data.Icon != null)
            {
                iconImage.sprite = data.Icon;
                iconImage.enabled = true;
            }
            else if (iconImage != null)
            {
                // Placeholder se non c'è icona
                iconImage.enabled = false;
            }

            // Hotkey
            if (hotkeyText != null)
            {
                if (hotkeyNumber <= 9)
                {
                    hotkeyText.text = hotkeyNumber.ToString();
                    hotkeyText.gameObject.SetActive(true);
                }
                else
                {
                    hotkeyText.gameObject.SetActive(false);
                }
            }

            // Costo breve
            if (costText != null)
            {
                costText.text = GetShortCostString();
            }

            // Overlay locked
            if (lockedOverlay != null)
            {
                lockedOverlay.SetActive(false); // TODO: tech tree lock
            }

            // Selected indicator
            if (selectedIndicator != null)
            {
                selectedIndicator.SetActive(isSelected);
            }

            UpdateAffordabilityVisuals();
        }

        private string GetShortCostString()
        {
            if (data.BuildCosts == null || data.BuildCosts.Length == 0)
            {
                return "Free";
            }

            // Mostra solo il costo principale
            var cost = data.BuildCosts[0];
            string icon = GetResourceIcon(cost.resourceId);
            return $"{icon}{cost.amount}";
        }

        private string GetResourceIcon(string resourceId)
        {
            return resourceId.ToLower() switch
            {
                "warmwood" => "🪵",
                "shards" => "💎",
                "food" => "🍖",
                _ => "•"
            };
        }

        // ============================================
        // AFFORDABILITY
        // ============================================

        public void SetAffordable(bool affordable)
        {
            isAffordable = affordable;
            UpdateAffordabilityVisuals();
        }

        private void UpdateAffordabilityVisuals()
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = isAffordable ? normalColor : unaffordableColor;
            }

            if (costText != null)
            {
                costText.color = isAffordable ? Color.white : unaffordableTextColor;
            }

            if (iconImage != null)
            {
                iconImage.color = isAffordable ? Color.white : new Color(0.6f, 0.6f, 0.6f);
            }
        }

        // ============================================
        // SELEZIONE
        // ============================================

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            
            if (backgroundImage != null)
            {
                backgroundImage.color = selected ? selectedColor : normalColor;
            }

            if (selectedIndicator != null)
            {
                selectedIndicator.SetActive(selected);
            }
        }

        // ============================================
        // POINTER EVENTS
        // ============================================

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (backgroundImage != null && isAffordable && !isSelected)
            {
                backgroundImage.color = hoverColor;
            }

            onHoverCallback?.Invoke(data);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (backgroundImage != null && !isSelected)
            {
                backgroundImage.color = isAffordable ? normalColor : unaffordableColor;
            }

            onHoverExitCallback?.Invoke(data);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                onSelectCallback?.Invoke(data);
            }
        }
    }
}
```

---

### 3. `Assets/_UI/BuildMenu/ResourceDisplayUI.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;
using WildernessSurvival.Gameplay.Resources;

namespace WildernessSurvival.UI
{
    /// <summary>
    /// Display delle risorse nella HUD.
    /// Mostra quantità corrente, max storage, e production rate.
    /// </summary>
    public class ResourceDisplayUI : MonoBehaviour
    {
        // ============================================
        // SINGLETON
        // ============================================
        
        public static ResourceDisplayUI Instance { get; private set; }

        // ============================================
        // CONFIGURAZIONE
        // ============================================

        [TitleGroup("Resource Displays")]
        [SerializeField] private ResourceDisplayItem warmwoodDisplay;
        [SerializeField] private ResourceDisplayItem shardsDisplay;
        [SerializeField] private ResourceDisplayItem foodDisplay;

        [TitleGroup("Animazione")]
        [SerializeField] private float updateInterval = 0.25f;
        [SerializeField] private float changeAnimationDuration = 0.3f;
        [SerializeField] private Color increaseColor = Color.green;
        [SerializeField] private Color decreaseColor = Color.red;

        [TitleGroup("Debug")]
        [SerializeField] private bool debugMode = false;

        // ============================================
        // RUNTIME
        // ============================================

        private float lastWarmwood = 0;
        private float lastShards = 0;
        private float lastFood = 0;
        private float updateTimer = 0;

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

        private void Update()
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                updateTimer = 0;
                UpdateAllDisplays();
            }
        }

        // ============================================
        // UPDATE DISPLAYS
        // ============================================

        private void UpdateAllDisplays()
        {
            if (ResourceSystem.Instance == null) return;

            UpdateSingleDisplay(warmwoodDisplay, "warmwood", ref lastWarmwood);
            UpdateSingleDisplay(shardsDisplay, "shards", ref lastShards);
            UpdateSingleDisplay(foodDisplay, "food", ref lastFood);
        }

        private void UpdateSingleDisplay(ResourceDisplayItem display, string resourceId, ref float lastValue)
        {
            if (display == null) return;

            float current = ResourceSystem.Instance.GetResourceAmount(resourceId);
            float max = ResourceSystem.Instance.GetMaxStorage(resourceId);
            
            // Detect change
            float delta = current - lastValue;
            if (Mathf.Abs(delta) > 0.1f)
            {
                display.AnimateChange(delta > 0);
            }
            lastValue = current;

            // Update text
            display.SetValues(current, max);
        }

        // ============================================
        // PUBLIC API
        // ============================================

        public void ForceUpdate()
        {
            UpdateAllDisplays();
        }

        public void ShowChangeAnimation(string resourceId, float amount)
        {
            ResourceDisplayItem display = resourceId.ToLower() switch
            {
                "warmwood" => warmwoodDisplay,
                "shards" => shardsDisplay,
                "food" => foodDisplay,
                _ => null
            };

            if (display != null)
            {
                display.ShowFloatingText(amount);
            }
        }
    }

    // ============================================
    // DISPLAY ITEM (NESTED CLASS)
    // ============================================

    [System.Serializable]
    public class ResourceDisplayItem
    {
        [Header("Riferimenti")]
        public Image iconImage;
        public TextMeshProUGUI amountText;
        public TextMeshProUGUI maxText;
        public Slider progressBar;
        public Image progressFill;
        public TextMeshProUGUI floatingTextPrefab;
        public Transform floatingTextSpawn;

        [Header("Colori")]
        public Color normalColor = Color.white;
        public Color lowColor = Color.yellow;
        public Color criticalColor = Color.red;
        public Color fullColor = Color.cyan;

        public void SetValues(float current, float max)
        {
            int currentInt = Mathf.FloorToInt(current);
            int maxInt = Mathf.FloorToInt(max);

            if (amountText != null)
            {
                amountText.text = currentInt.ToString();
            }

            if (maxText != null)
            {
                maxText.text = $"/ {maxInt}";
            }

            if (progressBar != null)
            {
                progressBar.maxValue = max;
                progressBar.value = current;
            }

            // Colore basato su riempimento
            if (progressFill != null)
            {
                float ratio = current / max;
                if (ratio >= 1f)
                {
                    progressFill.color = fullColor;
                }
                else if (ratio < 0.2f)
                {
                    progressFill.color = criticalColor;
                }
                else if (ratio < 0.4f)
                {
                    progressFill.color = lowColor;
                }
                else
                {
                    progressFill.color = normalColor;
                }
            }
        }

        public void AnimateChange(bool increase)
        {
            // Pulsa l'icona
            if (iconImage != null)
            {
                Color targetColor = increase ? Color.green : Color.red;
                // TODO: DOTween o coroutine per animazione
            }
        }

        public void ShowFloatingText(float amount)
        {
            if (floatingTextPrefab == null || floatingTextSpawn == null) return;

            TextMeshProUGUI floating = Object.Instantiate(floatingTextPrefab, floatingTextSpawn);
            string sign = amount >= 0 ? "+" : "";
            floating.text = $"{sign}{amount:F0}";
            floating.color = amount >= 0 ? Color.green : Color.red;
            
            // TODO: Animazione float up e fade out
            Object.Destroy(floating.gameObject, 1f);
        }
    }
}
```

---

## 📁 FILE DA MODIFICARE

### 4. Aggiorna `BuildModeController.cs`

Aggiungi questo metodo se non esiste:

```csharp
/// <summary>
/// Seleziona una struttura per il build mode (chiamato da UI)
/// </summary>
public void SelectStructure(StructureData data)
{
    if (data == null)
    {
        Debug.LogWarning("[BuildMode] SelectStructure: data is null");
        return;
    }

    currentStructureData = data;
    isInBuildMode = true;
    
    // Crea preview
    CreatePreview();
    
    Debug.Log($"<color=green>[BuildMode]</color> Struttura selezionata: {data.DisplayName}");
}

/// <summary>
/// Verifica se siamo in build mode
/// </summary>
public bool IsInBuildMode => isInBuildMode;

/// <summary>
/// Struttura correntemente selezionata
/// </summary>
public StructureData CurrentStructure => currentStructureData;
```

---

## 🎨 PREFAB DA CREARE IN UNITY

### BuildMenuPanel Hierarchy:

```
Canvas
└── BuildMenuPanel (Panel)
    ├── Header (HorizontalLayoutGroup)
    │   ├── TitleText (TMP) "🏗️ BUILD MENU"
    │   └── CloseButton (Button) "✕"
    │
    ├── CategoryFilters (HorizontalLayoutGroup)
    │   ├── AllButton "All"
    │   ├── ResourceButton "🌲 Resource"
    │   ├── DefenseButton "🛡️ Defense"
    │   └── UtilityButton "⚙️ Utility"
    │
    ├── ScrollView
    │   └── Viewport
    │       └── StructureButtonsContainer (GridLayoutGroup)
    │           └── [Bottoni generati dinamicamente]
    │
    └── TooltipPanel (Panel) [inizialmente nascosto]
        ├── TooltipTitle (TMP)
        ├── TooltipDescription (TMP)
        ├── TooltipCosts (TMP)
        └── TooltipStats (TMP)
```

### StructureButtonPrefab Hierarchy:

```
StructureButton (Button)
├── Background (Image)
├── Icon (Image)
├── NameText (TMP)
├── CostText (TMP)
├── HotkeyBadge
│   └── HotkeyText (TMP)
├── LockedOverlay (Image) [inizialmente disattivo]
└── SelectedIndicator (Image) [inizialmente disattivo]
```

### ResourceDisplayUI Hierarchy:

```
ResourceHUD (Panel, top of screen)
├── WarmwoodDisplay
│   ├── Icon (Image) 🪵
│   ├── AmountText (TMP)
│   ├── MaxText (TMP)
│   └── ProgressBar (Slider)
│
├── ShardsDisplay
│   ├── Icon (Image) 💎
│   ├── AmountText (TMP)
│   ├── MaxText (TMP)
│   └── ProgressBar (Slider)
│
└── FoodDisplay
    ├── Icon (Image) 🍖
    ├── AmountText (TMP)
    ├── MaxText (TMP)
    └── ProgressBar (Slider)
```

---

## ⚙️ CONFIGURAZIONI UNITY

### Layer Setup
Nessun layer aggiuntivo richiesto per UI.

### Canvas Settings
```
Canvas
├── Render Mode: Screen Space - Overlay
├── UI Scale Mode: Scale With Screen Size
├── Reference Resolution: 1920 x 1080
└── Match Width Or Height: 0.5
```

### GridLayoutGroup (StructureButtonsContainer)
```
Cell Size: 120 x 140
Spacing: 10 x 10
Start Corner: Upper Left
Start Axis: Horizontal
Child Alignment: Upper Left
Constraint: Fixed Column Count
Constraint Count: 4
```

---

## 🧪 TEST CHECKLIST

Dopo implementazione, verifica:

- [ ] Premi B → Menu si apre
- [ ] Premi B di nuovo → Menu si chiude
- [ ] Bottoni strutture visibili con icone
- [ ] Hover su bottone → Tooltip appare
- [ ] Hover fuori → Tooltip scompare
- [ ] Click su bottone affordable → Build mode attivo, menu chiude
- [ ] Click su bottone unaffordable → Nulla succede (o feedback errore)
- [ ] Bottoni unaffordable hanno colore grigio
- [ ] Hotkey 1-9 selezionano strutture
- [ ] Filtri categoria funzionano
- [ ] ResourceHUD mostra valori corretti
- [ ] ResourceHUD si aggiorna in tempo reale

---

## 📋 COMANDI TERMINALE

```bash
# Nessun comando terminale necessario
# Tutto il lavoro è in Unity Editor
```

---

## ⚠️ NOTE IMPORTANTI

1. **Namespace**: Tutti i file usano `WildernessSurvival.UI`
2. **Dipendenze**: Richiede TextMeshPro e Odin Inspector
3. **BuildModeController**: Deve avere metodo `SelectStructure(StructureData)`
4. **ResourceSystem**: Deve avere metodo `GetMaxStorage(string)`
5. **StructureData**: Deve avere proprietà `Icon` (Sprite)

---

## 🎯 RISULTATO ATTESO

Un menu costruzione professionale con:
- Apertura/chiusura fluida con tasto B
- Lista strutture con icone, nomi, costi
- Feedback visivo chiaro per affordability
- Tooltip informativi
- Hotkeys per power users
- Filtri per categoria
- HUD risorse sempre visibile
