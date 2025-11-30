using UnityEngine;
using Sirenix.OdinInspector;

namespace WildernessSurvival.Gameplay.Resources
{
    /// <summary>
    /// Definisce una singola risorsa del gioco.
    /// Creane una per ogni tipo: Warmwood, Food, Shards, Bronze, etc.
    /// </summary>
    [CreateAssetMenu(fileName = "NewResource", menuName = "Wilderness Survival/Data/Resource Definition")]
    public class ResourceData : ScriptableObject
    {
        // ============================================
        // IDENTIFICAZIONE
        // ============================================

        [TitleGroup("Identificazione")]
        [HorizontalGroup("Identificazione/Row1", 0.7f)]
        [LabelWidth(100)]
        [Tooltip("ID univoco della risorsa (es: warmwood, food, shards)")]
        [SerializeField] private string resourceId;

        [HorizontalGroup("Identificazione/Row1")]
        [LabelWidth(40)]
        [PreviewField(50, ObjectFieldAlignment.Right)]
        [SerializeField] private Sprite icon;

        [HorizontalGroup("Identificazione/Row2")]
        [LabelWidth(100)]
        [Tooltip("Nome visualizzato in UI")]
        [SerializeField] private string displayName;

        [HorizontalGroup("Identificazione/Row2")]
        [LabelWidth(60)]
        [SerializeField] private Color uiColor = Color.white;

        [TextArea(2, 3)]
        [SerializeField] private string description;

        // ============================================
        // CLASSIFICAZIONE
        // ============================================

        [TitleGroup("Classificazione")]
        [HorizontalGroup("Classificazione/Row1")]
        [LabelWidth(80)]
        [Tooltip("Tier tecnologico: 1=Base, 2=Bronze, 3=Iron, 4=Crystal")]
        [Range(1, 5)]
        [SerializeField] private int tier = 1;

        [HorizontalGroup("Classificazione/Row1")]
        [LabelWidth(60)]
        [EnumToggleButtons]
        [Tooltip("RaritÃ  della risorsa")]
        [SerializeField] private ResourceRarity rarity = ResourceRarity.Common;

        // ============================================
        // VALORI E PRODUZIONE
        // ============================================

        [TitleGroup("Valori e Produzione")]
        [BoxGroup("Valori e Produzione/Base")]
        [HorizontalGroup("Valori e Produzione/Base/Row1")]
        [LabelWidth(120)]
        [Tooltip("Valore base per scambi/conversioni")]
        [SerializeField] private int baseValue = 10;

        [HorizontalGroup("Valori e Produzione/Base/Row1")]
        [LabelWidth(140)]
        [SuffixLabel("/min", Overlay = true)]
        [Tooltip("Produzione base al minuto (senza bonus)")]
        [SerializeField] private float baseProductionPerMinute = 5f;

        [BoxGroup("Valori e Produzione/Bonus")]
        [HorizontalGroup("Valori e Produzione/Bonus/Row1")]
        [LabelWidth(100)]
        [SuffixLabel("%", Overlay = true)]
        [PropertyRange(0, 200)]
        [Tooltip("Bonus % quando un worker Ã¨ assegnato")]
        [SerializeField] private int workerBonusPercent = 25;

        [HorizontalGroup("Valori e Produzione/Bonus/Row1")]
        [LabelWidth(100)]
        [SuffixLabel("%", Overlay = true)]
        [PropertyRange(0, 300)]
        [Tooltip("Bonus % quando un eroe Ã¨ assegnato")]
        [SerializeField] private int heroBonusPercent = 50;

        [BoxGroup("Valori e Produzione/Storage")]
        [LabelWidth(120)]
        [Tooltip("CapacitÃ  massima storage (0 = illimitato)")]
        [SerializeField] private int maxStorage = 0;

        [BoxGroup("Valori e Produzione/Storage")]
        [ShowIf("@maxStorage > 0")]
        [ProgressBar(0, "maxStorage", ColorGetter = "GetStorageBarColor")]
        [ReadOnly]
        [SerializeField] private int currentStoragePreview = 0;

        // ============================================
        // GATHERING
        // ============================================

        [TitleGroup("Raccolta")]
        [EnumToggleButtons]
        [Tooltip("Metodi per ottenere questa risorsa")]
        [SerializeField] private GatherMethod[] gatherMethods;

        // ============================================
        // CRAFTING
        // ============================================

        [TitleGroup("Crafting")]
        [ToggleLeft]
        [Tooltip("PuÃ² essere craftata/convertita?")]
        [SerializeField] private bool isCraftable = false;

        [ShowIf("isCraftable")]
        [TableList(ShowIndexLabels = true, AlwaysExpanded = true)]
        [Tooltip("Requisiti per craftare questa risorsa")]
        [SerializeField] private CraftRequirement[] craftRequirements;

        // ============================================
        // PREVIEW CALCOLATA
        // ============================================

        [TitleGroup("Preview Produzione")]
        [InfoBox("Calcolo produzione con worker/eroi assegnati")]
        [HorizontalGroup("Preview Produzione/Calc")]
        [LabelWidth(80)]
        [PropertyRange(0, 10)]
        [OnValueChanged("UpdateProductionPreview")]
        [SerializeField] private int previewWorkers = 1;

        [HorizontalGroup("Preview Produzione/Calc")]
        [LabelWidth(80)]
        [PropertyRange(0, 3)]
        [OnValueChanged("UpdateProductionPreview")]
        [SerializeField] private int previewHeroes = 0;

        [HorizontalGroup("Preview Produzione/Result")]
        [DisplayAsString]
        [HideLabel]
        [SerializeField] private string productionPreviewText = "";

        // ============================================
        // PROPERTIES (Read-Only per altri sistemi)
        // ============================================

        public string ResourceId => resourceId;
        public string DisplayName => displayName;
        public string Description => description;
        public int Tier => tier;
        public ResourceRarity Rarity => rarity;
        public int BaseValue => baseValue;
        public float BaseProductionPerMinute => baseProductionPerMinute;
        public int WorkerBonusPercent => workerBonusPercent;
        public int HeroBonusPercent => heroBonusPercent;
        public int MaxStorage => maxStorage;
        public bool HasMaxStorage => maxStorage > 0;
        public GatherMethod[] GatherMethods => gatherMethods;
        public bool IsCraftable => isCraftable;
        public CraftRequirement[] CraftRequirements => craftRequirements;
        public Sprite Icon => icon;
        public Color UIColor => uiColor;

        // ============================================
        // METODI UTILITÃ€
        // ============================================

        /// <summary>
        /// Calcola produzione con bonus worker/hero
        /// </summary>
        public float CalculateProduction(int workerCount, int heroCount)
        {
            float production = baseProductionPerMinute;
            production += production * (workerBonusPercent / 100f) * workerCount;
            production += production * (heroBonusPercent / 100f) * heroCount;
            return production;
        }

        /// <summary>
        /// Verifica se puÃ² essere raccolta con un certo metodo
        /// </summary>
        public bool CanGatherWith(GatherMethod method)
        {
            if (gatherMethods == null) return false;

            foreach (var m in gatherMethods)
            {
                if (m == method) return true;
            }
            return false;
        }

        // ============================================
        // ODIN HELPERS
        // ============================================

        private Color GetStorageBarColor()
        {
            float ratio = maxStorage > 0 ? (float)currentStoragePreview / maxStorage : 0f;
            if (ratio < 0.5f) return Color.green;
            if (ratio < 0.8f) return Color.yellow;
            return Color.red;
        }

        private void UpdateProductionPreview()
        {
            float prod = CalculateProduction(previewWorkers, previewHeroes);
            productionPreviewText = $"ðŸ“Š Produzione: {prod:F1}/min";
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-genera ID dal nome se vuoto
            if (string.IsNullOrEmpty(resourceId))
            {
                resourceId = name.ToLower().Replace(" ", "_");
            }

            // Auto-genera display name se vuoto
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = name;
            }

            // Aggiorna preview
            UpdateProductionPreview();
        }

        [TitleGroup("Debug")]
        [Button("Test Production Calculation", ButtonSizes.Medium)]
        [GUIColor(0.4f, 0.8f, 0.4f)]
        private void TestProduction()
        {
            Debug.Log($"=== {displayName} Production Test ===");
            Debug.Log($"Base: {baseProductionPerMinute}/min");
            Debug.Log($"With 1 Worker: {CalculateProduction(1, 0):F1}/min");
            Debug.Log($"With 2 Workers: {CalculateProduction(2, 0):F1}/min");
            Debug.Log($"With 1 Hero: {CalculateProduction(0, 1):F1}/min");
            Debug.Log($"With 2 Workers + 1 Hero: {CalculateProduction(2, 1):F1}/min");
        }
#endif
    }

    // ============================================
    // ENUM E STRUCT DI SUPPORTO
    // ============================================

    public enum ResourceRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    [System.Flags]
    public enum GatherMethod
    {
        None = 0,
        Camp = 1 << 0,           // Raccolto al campo base
        Mining = 1 << 1,         // Estratto da miniere
        Hunting = 1 << 2,        // Ottenuto cacciando
        Foraging = 1 << 3,       // Raccolto in natura
        Crafting = 1 << 4,       // Prodotto tramite craft
        Combat = 1 << 5,         // Drop da nemici
        Trading = 1 << 6,        // Scambio/acquisto
        Special = 1 << 7         // Eventi speciali
    }

    [System.Serializable]
    public struct CraftRequirement
    {
        [HorizontalGroup("Row", 0.7f)]
        [HideLabel]
        [Tooltip("Risorsa richiesta")]
        public ResourceData resource;

        [HorizontalGroup("Row")]
        [HideLabel]
        [SuffixLabel("x", Overlay = true)]
        [Tooltip("QuantitÃ  necessaria")]
        public int amount;
    }
}