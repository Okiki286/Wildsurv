using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

namespace WildernessSurvival.Gameplay.Structures
{
    /// <summary>
    /// Definisce una struttura costruibile.
    /// Ogni edificio, torre, risorsa ha il suo StructureData.
    /// </summary>
    [CreateAssetMenu(fileName = "NewStructure", menuName = "Wilderness Survival/Data/Structure Definition")]
    public class StructureData : ScriptableObject
    {
        // ============================================
        // IDENTIFICAZIONE
        // ============================================

        [TitleGroup("Identificazione")]
        [HorizontalGroup("Identificazione/Row1", 0.65f)]
        [VerticalGroup("Identificazione/Row1/Left")]
        [LabelWidth(100)]
        [Tooltip("ID univoco della struttura")]
        [SerializeField] private string structureId;

        [VerticalGroup("Identificazione/Row1/Left")]
        [LabelWidth(100)]
        [Tooltip("Nome visualizzato")]
        [SerializeField] private string displayName;

        [HorizontalGroup("Identificazione/Row1", 0.35f)]
        [VerticalGroup("Identificazione/Row1/Right")]
        [PreviewField(60, ObjectFieldAlignment.Right)]
        [HideLabel]
        [SerializeField] private Sprite icon;

        [VerticalGroup("Identificazione/Row1/Right")]
        [PreviewField(60, ObjectFieldAlignment.Right)]
        [HideLabel]
        [Tooltip("Prefab della struttura")]
        [SerializeField] private GameObject prefab;

        [TextArea(2, 4)]
        [SerializeField] private string description;

        // ============================================
        // CLASSIFICAZIONE
        // ============================================

        [TitleGroup("Classificazione")]
        [HorizontalGroup("Classificazione/Row1")]
        [LabelWidth(80)]
        [EnumToggleButtons]
        [SerializeField] private StructureCategory category = StructureCategory.Resource;

        [HorizontalGroup("Classificazione/Row1")]
        [LabelWidth(60)]
        [PropertyRange(1, 5)]
        [Tooltip("Tier tecnologico richiesto")]
        [SerializeField] private int tier = 1;

        [HorizontalGroup("Classificazione/Row2")]
        [LabelWidth(80)]
        [PropertyRange(1, 5)]
        [Tooltip("Livello massimo upgradabile")]
        [SerializeField] private int maxLevel = 3;

        [HorizontalGroup("Classificazione/Row2")]
        [LabelWidth(80)]
        [ToggleLeft]
        [Tooltip("Ãˆ unica? (una sola nel campo)")]
        [SerializeField] private bool isUnique = false;

        // ============================================
        // COSTI COSTRUZIONE
        // ============================================

        [TitleGroup("Costi Costruzione")]
        [InfoBox("Costo per costruire questa struttura", InfoMessageType.None)]
        [TableList(AlwaysExpanded = true)]
        [SerializeField] private StructureCost[] buildCosts;

        [FoldoutGroup("Costi Costruzione/Tempi")]
        [LabelWidth(120)]
        [SuffixLabel("sec", Overlay = true)]
        [SerializeField] private float buildTime = 5f;

        [FoldoutGroup("Costi Costruzione/Tempi")]
        [LabelWidth(120)]
        [ToggleLeft]
        [Tooltip("Richiede worker per costruire?")]
        [SerializeField] private bool requiresBuilder = true;

        // ============================================
        // WORKER SLOTS
        // ============================================

        [TitleGroup("Worker Assignment")]
        [BoxGroup("Worker Assignment/Slots")]
        [HorizontalGroup("Worker Assignment/Slots/Row1")]
        [LabelWidth(100)]
        [PropertyRange(0, 10)]
        [Tooltip("Numero di worker assegnabili")]
        [SerializeField] private int workerSlots = 2;

        [HorizontalGroup("Worker Assignment/Slots/Row1")]
        [LabelWidth(100)]
        [PropertyRange(0, 3)]
        [Tooltip("Numero di eroi assegnabili")]
        [SerializeField] private int heroSlots = 0;

        [BoxGroup("Worker Assignment/Roles")]
        [Tooltip("Ruoli worker che danno bonus qui")]
        [EnumToggleButtons]
        [SerializeField] private WorkerRole allowedRoles = WorkerRole.Gatherer;

        // ============================================
        // EFFETTI
        // ============================================

        [TitleGroup("Effetti Struttura")]
        [BoxGroup("Effetti Struttura/Produzione")]
        [ShowIf("@category == StructureCategory.Resource")]
        [LabelWidth(120)]
        [Tooltip("ID risorsa prodotta")]
        [SerializeField] private string producesResourceId;

        [BoxGroup("Effetti Struttura/Produzione")]
        [ShowIf("@category == StructureCategory.Resource")]
        [LabelWidth(120)]
        [SuffixLabel("/min", Overlay = true)]
        [SerializeField] private float baseProductionRate = 5f;

        [BoxGroup("Effetti Struttura/Difesa")]
        [ShowIf("@category == StructureCategory.Defense")]
        [LabelWidth(100)]
        [SerializeField] private float attackDamage = 10f;

        [BoxGroup("Effetti Struttura/Difesa")]
        [ShowIf("@category == StructureCategory.Defense")]
        [LabelWidth(100)]
        [SuffixLabel("sec", Overlay = true)]
        [SerializeField] private float attackInterval = 1f;

        [BoxGroup("Effetti Struttura/Difesa")]
        [ShowIf("@category == StructureCategory.Defense")]
        [LabelWidth(100)]
        [SerializeField] private float attackRange = 5f;

        [BoxGroup("Effetti Struttura/Utility")]
        [ShowIf("@category == StructureCategory.Utility")]
        [LabelWidth(100)]
        #pragma warning disable CS0414 // Reserved for future utility structure bonus radius feature
        [SerializeField] private float bonusRadius = 5f;
        #pragma warning restore CS0414

        [BoxGroup("Effetti Struttura/Utility")]
        [ShowIf("@category == StructureCategory.Utility")]
        [TextArea(2, 3)]
        [SerializeField] private string utilityEffect;

        // ============================================
        // STATS
        // ============================================

        [TitleGroup("Statistiche")]
        [HorizontalGroup("Statistiche/Row1")]
        [LabelWidth(80)]
        [SerializeField] private int maxHealth = 100;

        [HorizontalGroup("Statistiche/Row1")]
        [LabelWidth(80)]
        [SerializeField] private int armor = 0;

        [HorizontalGroup("Statistiche/Row2")]
        [LabelWidth(80)]
        [Tooltip("Dimensione griglia (1 = 1x1)")]
        [SerializeField] private Vector2Int gridSize = Vector2Int.one;

        // ============================================
        // UPGRADES
        // ============================================

        [TitleGroup("Upgrade Path")]
        [ShowIf("@maxLevel > 1")]
        [TableList(ShowIndexLabels = true)]
        [SerializeField] private StructureUpgrade[] upgrades;

        // ============================================
        // VISUALS
        // ============================================

        [TitleGroup("Visual Settings")]
        [FoldoutGroup("Visual Settings/Materials")]
        [SerializeField] private Material baseMaterial;

        [FoldoutGroup("Visual Settings/Materials")]
        [SerializeField] private Material damagedMaterial;

        [FoldoutGroup("Visual Settings/Materials")]
        [SerializeField] private Material constructionMaterial;

        [FoldoutGroup("Visual Settings/Effects")]
        [SerializeField] private GameObject constructionVFX;

        [FoldoutGroup("Visual Settings/Effects")]
        [SerializeField] private GameObject completionVFX;

        [FoldoutGroup("Visual Settings/Effects")]
        [SerializeField] private GameObject destructionVFX;

        // ============================================
        // PROPERTIES
        // ============================================

        public string StructureId => structureId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public GameObject Prefab => prefab;
        public StructureCategory Category => category;
        public int Tier => tier;
        public int MaxLevel => maxLevel;
        public bool IsUnique => isUnique;
        public StructureCost[] BuildCosts => buildCosts;
        public float BuildTime => buildTime;
        public bool RequiresBuilder => requiresBuilder;
        public int WorkerSlots => workerSlots;
        public int HeroSlots => heroSlots;
        public WorkerRole AllowedRoles => allowedRoles;
        public string ProducesResourceId => producesResourceId;
        public float BaseProductionRate => baseProductionRate;
        public float AttackDamage => attackDamage;
        public float AttackInterval => attackInterval;
        public float AttackRange => attackRange;
        public int MaxHealth => maxHealth;
        public int Armor => armor;
        public Vector2Int GridSize => gridSize;
        public StructureUpgrade[] Upgrades => upgrades;

        // ============================================
        // METODI
        // ============================================

        /// <summary>
        /// Ottiene il costo per un livello specifico
        /// </summary>
        public StructureCost[] GetUpgradeCost(int toLevel)
        {
            if (upgrades == null || toLevel <= 1 || toLevel > maxLevel)
                return null;

            int index = toLevel - 2; // Level 2 = index 0
            if (index >= 0 && index < upgrades.Length)
            {
                return upgrades[index].costs;
            }
            return null;
        }

        /// <summary>
        /// Calcola produzione con bonus per livello
        /// </summary>
        public float GetProductionAtLevel(int level)
        {
            float production = baseProductionRate;

            // +20% per livello
            production *= (1f + (level - 1) * 0.2f);

            return production;
        }

        /// <summary>
        /// Calcola danno con bonus per livello
        /// </summary>
        public float GetDamageAtLevel(int level)
        {
            return attackDamage * (1f + (level - 1) * 0.25f);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(structureId))
            {
                structureId = name.ToLower().Replace(" ", "_");
            }
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = name;
            }
        }

        [TitleGroup("Debug")]
        [Button("ðŸ“Š Print Stats", ButtonSizes.Medium)]
        private void DebugPrintStats()
        {
            Debug.Log($"=== {displayName} ===\n" +
                $"Category: {category}\n" +
                $"Tier: {tier}\n" +
                $"Worker Slots: {workerSlots}\n" +
                $"Health: {maxHealth}\n" +
                $"Grid Size: {gridSize}");
        }

        [Button("ðŸ“ˆ Preview Levels", ButtonSizes.Medium)]
        private void DebugPreviewLevels()
        {
            Debug.Log($"=== {displayName} Level Progression ===");
            for (int i = 1; i <= maxLevel; i++)
            {
                if (category == StructureCategory.Resource)
                {
                    Debug.Log($"  Lv.{i}: {GetProductionAtLevel(i):F1}/min");
                }
                else if (category == StructureCategory.Defense)
                {
                    Debug.Log($"  Lv.{i}: {GetDamageAtLevel(i):F1} DMG");
                }
            }
        }
#endif
    }

    // ============================================
    // ENUMS
    // ============================================

    public enum StructureCategory
    {
        [LabelText("ðŸŒ² Resource")]
        Resource,

        [LabelText("ðŸ›¡ï¸ Defense")]
        Defense,

        [LabelText("âš™ï¸ Utility")]
        Utility,

        [LabelText("ðŸ”¬ Tech")]
        Tech,

        [LabelText("ðŸ  Meta")]
        Meta
    }

    [System.Flags]
    public enum WorkerRole
    {
        None = 0,

        [LabelText("â›ï¸ Gatherer")]
        Gatherer = 1 << 0,

        [LabelText("ðŸ”¨ Builder")]
        Builder = 1 << 1,

        [LabelText("âš”ï¸ Guard")]
        Guard = 1 << 2,

        [LabelText("ðŸ”­ Scout")]
        Scout = 1 << 3,

        [LabelText("âš—ï¸ Crafter")]
        Crafter = 1 << 4,

        [LabelText("ðŸŽ“ Researcher")]
        Researcher = 1 << 5
    }

    // ============================================
    // STRUCTS
    // ============================================

    [System.Serializable]
    public struct StructureCost
    {
        [HorizontalGroup("Row", 0.65f)]
        [HideLabel]
        [ValueDropdown("GetResourceIds")]
        public string resourceId;

        [HorizontalGroup("Row")]
        [HideLabel]
        [SuffixLabel("x", Overlay = true)]
        public int amount;

#if UNITY_EDITOR
        private static IEnumerable<string> GetResourceIds()
        {
            return new List<string> { "warmwood", "food", "shards", "bronze_ore", "iron_ore" };
        }
#endif
    }

    [System.Serializable]
    public struct StructureUpgrade
    {
        [HorizontalGroup("Row", 0.2f)]
        [ReadOnly]
        [HideLabel]
        [DisplayAsString]
        public string levelLabel;

        [HorizontalGroup("Row")]
        [TableList]
        public StructureCost[] costs;

        [HorizontalGroup("Row", 0.15f)]
        [SuffixLabel("sec")]
        [HideLabel]
        public float upgradeTime;
    }
}