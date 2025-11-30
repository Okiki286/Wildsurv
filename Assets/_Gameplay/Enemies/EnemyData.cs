using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

namespace WildernessSurvival.Gameplay.Enemies
{
    /// <summary>
    /// Definisce un tipo di nemico.
    /// Ogni nemico ha stats, comportamento, e loot table.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "Wilderness Survival/Data/Enemy Definition")]
    public class EnemyData : ScriptableObject
    {
        // ============================================
        // IDENTIFICAZIONE
        // ============================================

        [TitleGroup("Identificazione")]
        [HorizontalGroup("Identificazione/Row1", 0.65f)]
        [VerticalGroup("Identificazione/Row1/Left")]
        [LabelWidth(100)]
        [SerializeField] private string enemyId;

        [VerticalGroup("Identificazione/Row1/Left")]
        [LabelWidth(100)]
        [SerializeField] private string displayName;

        [HorizontalGroup("Identificazione/Row1", 0.35f)]
        [VerticalGroup("Identificazione/Row1/Right")]
        [PreviewField(60, ObjectFieldAlignment.Right)]
        [HideLabel]
        [SerializeField] private Sprite icon;

        [VerticalGroup("Identificazione/Row1/Right")]
        [PreviewField(60, ObjectFieldAlignment.Right)]
        [HideLabel]
        [SerializeField] private GameObject prefab;

        [TextArea(2, 3)]
        [SerializeField] private string description;

        // ============================================
        // CLASSIFICAZIONE
        // ============================================

        [TitleGroup("Classificazione")]
        [HorizontalGroup("Classificazione/Row1")]
        [LabelWidth(80)]
        [EnumToggleButtons]
        [SerializeField] private EnemyType enemyType = EnemyType.Rusher;

        [HorizontalGroup("Classificazione/Row1")]
        [LabelWidth(60)]
        [PropertyRange(1, 5)]
        [Tooltip("Tier di difficoltà")]
        [SerializeField] private int tier = 1;

        [HorizontalGroup("Classificazione/Row2")]
        [LabelWidth(80)]
        [EnumToggleButtons]
        [SerializeField] private EnemyRarity rarity = EnemyRarity.Common;

        [HorizontalGroup("Classificazione/Row2")]
        [LabelWidth(60)]
        [ToggleLeft]
        [SerializeField] private bool isBoss = false;

        // ============================================
        // STATS BASE
        // ============================================

        [TitleGroup("Stats Base")]
        [BoxGroup("Stats Base/Health")]
        [HorizontalGroup("Stats Base/Health/Row")]
        [LabelWidth(80)]
        [SerializeField] private int baseHealth = 100;

        [HorizontalGroup("Stats Base/Health/Row")]
        [LabelWidth(80)]
        [SerializeField] private int armor = 0;

        [BoxGroup("Stats Base/Movement")]
        [LabelWidth(100)]
        [PropertyRange(0.5f, 10f)]
        [SerializeField] private float moveSpeed = 3f;

        [BoxGroup("Stats Base/Combat")]
        [HorizontalGroup("Stats Base/Combat/Row1")]
        [LabelWidth(80)]
        [SerializeField] private float attackDamage = 10f;

        [HorizontalGroup("Stats Base/Combat/Row1")]
        [LabelWidth(100)]
        [SuffixLabel("sec", Overlay = true)]
        [SerializeField] private float attackInterval = 1.5f;

        [HorizontalGroup("Stats Base/Combat/Row2")]
        [LabelWidth(80)]
        [SerializeField] private float attackRange = 1.5f;

        // ============================================
        // COMPORTAMENTO
        // ============================================

        [TitleGroup("Comportamento AI")]
        [BoxGroup("Comportamento AI/Targeting")]
        [EnumToggleButtons]
        [LabelWidth(100)]
        [SerializeField] private TargetPriority targetPriority = TargetPriority.Nearest;

        [BoxGroup("Comportamento AI/Targeting")]
        [LabelWidth(100)]
        [SerializeField] private float aggroRange = 10f;

        [BoxGroup("Comportamento AI/Special")]
        [ToggleLeft]
        [SerializeField] private bool canBurrow = false;

        [BoxGroup("Comportamento AI/Special")]
        [ShowIf("canBurrow")]
        [LabelWidth(120)]
        [SerializeField] private float burrowDuration = 2f;

        [BoxGroup("Comportamento AI/Special")]
        [ToggleLeft]
        [SerializeField] private bool isRanged = false;

        [BoxGroup("Comportamento AI/Special")]
        [ShowIf("isRanged")]
        [LabelWidth(120)]
        [SerializeField] private GameObject projectilePrefab;

        // ============================================
        // ABILITÀ SPECIALI
        // ============================================

        [TitleGroup("Abilità Speciali")]
        [TableList(AlwaysExpanded = true)]
        [SerializeField] private EnemyAbility[] abilities;

        // ============================================
        // DEBOLEZZE E RESISTENZE (FIX)
        // ============================================

        [TitleGroup("Debolezze e Resistenze")]
        [BoxGroup("Debolezze e Resistenze/Weaknesses")]
        [EnumToggleButtons]
        [Tooltip("Tipi di danno a cui è debole (+50% danni)")]
        [SerializeField] private DamageType weaknesses = DamageType.None;

        [BoxGroup("Debolezze e Resistenze/Resistances")]
        [EnumToggleButtons]
        [Tooltip("Tipi di danno a cui è resistente (-50% danni)")]
        [SerializeField] private DamageType resistances = DamageType.None;

        // ============================================
        // REWARDS
        // ============================================

        [TitleGroup("Ricompense")]
        [BoxGroup("Ricompense/Base")]
        [HorizontalGroup("Ricompense/Base/Row")]
        [LabelWidth(100)]
        [SerializeField] private int baseShardDrop = 5;

        [HorizontalGroup("Ricompense/Base/Row")]
        [LabelWidth(80)]
        [SerializeField] private int baseXP = 10;

        [BoxGroup("Ricompense/Loot Table")]
        [TableList]
        [SerializeField] private LootDrop[] lootTable;

        // ============================================
        // WAVE SCALING
        // ============================================

        [TitleGroup("Scaling per Wave")]
        [InfoBox("Come le stats scalano nelle wave successive")]
        [BoxGroup("Scaling per Wave/Multipliers")]
        [HorizontalGroup("Scaling per Wave/Multipliers/Row")]
        [LabelWidth(100)]
        [PropertyRange(1f, 2f)]
        [SerializeField] private float healthScaling = 1.1f;

        [HorizontalGroup("Scaling per Wave/Multipliers/Row")]
        [LabelWidth(100)]
        [PropertyRange(1f, 1.5f)]
        [SerializeField] private float damageScaling = 1.05f;

        // ============================================
        // AUDIO / VISUAL
        // ============================================

        [TitleGroup("Audio & Visual")]
        [FoldoutGroup("Audio & Visual/Sounds")]
        [SerializeField] private AudioClip spawnSound;
        [FoldoutGroup("Audio & Visual/Sounds")]
        [SerializeField] private AudioClip attackSound;
        [FoldoutGroup("Audio & Visual/Sounds")]
        [SerializeField] private AudioClip deathSound;

        [FoldoutGroup("Audio & Visual/VFX")]
        [SerializeField] private GameObject spawnVFX;
        [FoldoutGroup("Audio & Visual/VFX")]
        [SerializeField] private GameObject hitVFX;
        [FoldoutGroup("Audio & Visual/VFX")]
        [SerializeField] private GameObject deathVFX;

        // ============================================
        // PROPERTIES
        // ============================================

        public string EnemyId => enemyId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public GameObject Prefab => prefab;
        public EnemyType Type => enemyType;
        public int Tier => tier;
        public EnemyRarity Rarity => rarity;
        public bool IsBoss => isBoss;
        public int BaseHealth => baseHealth;
        public int Armor => armor;
        public float MoveSpeed => moveSpeed;
        public float AttackDamage => attackDamage;
        public float AttackInterval => attackInterval;
        public float AttackRange => attackRange;
        public TargetPriority TargetPriority => targetPriority;
        public float AggroRange => aggroRange;
        public int BaseShardDrop => baseShardDrop;
        public int BaseXP => baseXP;
        public DamageType Weaknesses => weaknesses;
        public DamageType Resistances => resistances;
        public LootDrop[] LootTable => lootTable;

        // ============================================
        // METODI
        // ============================================

        /// <summary>
        /// Calcola HP scalato per wave
        /// </summary>
        public int GetHealthForWave(int waveNumber)
        {
            return Mathf.RoundToInt(baseHealth * Mathf.Pow(healthScaling, waveNumber - 1));
        }

        /// <summary>
        /// Calcola danno scalato per wave
        /// </summary>
        public float GetDamageForWave(int waveNumber)
        {
            return attackDamage * Mathf.Pow(damageScaling, waveNumber - 1);
        }

        /// <summary>
        /// Calcola modificatore danno basato su debolezze/resistenze
        /// </summary>
        public float GetDamageMultiplier(DamageType incomingType)
        {
            if ((weaknesses & incomingType) != 0)
                return 1.5f; // +50% danni

            if ((resistances & incomingType) != 0)
                return 0.5f; // -50% danni

            return 1f;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(enemyId))
            {
                enemyId = name.ToLower().Replace(" ", "_");
            }
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = name;
            }
        }

        [TitleGroup("Debug")]
        [Button("📊 Print Stats", ButtonSizes.Medium)]
        private void DebugPrintStats()
        {
            Debug.Log($"=== {displayName} ===\n" +
                $"Type: {enemyType} | Tier: {tier}\n" +
                $"HP: {baseHealth} | Armor: {armor}\n" +
                $"DMG: {attackDamage} | Speed: {moveSpeed}\n" +
                $"Weaknesses: {weaknesses}\n" +
                $"Resistances: {resistances}");
        }

        [Button("📈 Preview Wave Scaling", ButtonSizes.Medium)]
        private void DebugWaveScaling()
        {
            Debug.Log($"=== {displayName} Wave Scaling ===");
            for (int i = 1; i <= 10; i += 3)
            {
                Debug.Log($"  Wave {i}: HP={GetHealthForWave(i)}, DMG={GetDamageForWave(i):F1}");
            }
        }
#endif
    }

    // ============================================
    // ENUMS
    // ============================================

    public enum EnemyType
    {
        [LabelText("🏃 Rusher")]
        Rusher,

        [LabelText("🛡️ Tank")]
        Tank,

        [LabelText("🏹 Ranged")]
        Ranged,

        [LabelText("🕳️ Burrower")]
        Burrower,

        [LabelText("✨ Caster")]
        Caster,

        [LabelText("🐜 Swarm")]
        Swarm,

        [LabelText("👑 Boss")]
        Boss
    }

    public enum EnemyRarity
    {
        Common,
        Uncommon,
        Rare,
        Elite,
        Boss
    }

    public enum TargetPriority
    {
        [LabelText("🔍 Nearest")]
        Nearest,

        [LabelText("❤️ Lowest HP")]
        LowestHealth,

        [LabelText("⚔️ Highest DPS")]
        HighestDPS,

        [LabelText("🔥 Bonfire")]
        Bonfire,

        [LabelText("🎲 Random")]
        Random
    }

    [System.Flags]
    public enum DamageType
    {
        None = 0,
        Physical = 1 << 0,
        Fire = 1 << 1,
        Ice = 1 << 2,
        Lightning = 1 << 3,
        Shard = 1 << 4,
        Poison = 1 << 5,
        Light = 1 << 6
    }

    // ============================================
    // STRUCTS
    // ============================================

    [System.Serializable]
    public struct EnemyAbility
    {
        [HorizontalGroup("Row", 0.3f)]
        [HideLabel]
        public string abilityName;

        [HorizontalGroup("Row", 0.15f)]
        [HideLabel]
        [SuffixLabel("sec CD")]
        public float cooldown;

        [HorizontalGroup("Row")]
        [HideLabel]
        [TextArea(1, 2)]
        public string description;
    }

    [System.Serializable]
    public struct LootDrop
    {
        [HorizontalGroup("Row", 0.4f)]
        [HideLabel]
        [ValueDropdown("GetResourceIds")]
        public string resourceId;

        [HorizontalGroup("Row", 0.2f)]
        [HideLabel]
        [LabelText("Min")]
        public int minAmount;

        [HorizontalGroup("Row", 0.2f)]
        [HideLabel]
        [LabelText("Max")]
        public int maxAmount;

        [HorizontalGroup("Row", 0.2f)]
        [HideLabel]
        [SuffixLabel("%")]
        [PropertyRange(0, 100)]
        public float dropChance;

#if UNITY_EDITOR
        private static IEnumerable<string> GetResourceIds()
        {
            return new List<string> { "warmwood", "food", "shards", "bronze_ore", "iron_ore", "crystal_essence" };
        }
#endif
    }
}