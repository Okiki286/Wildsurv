using UnityEngine;
using Sirenix.OdinInspector;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// Definisce un tipo di worker (villager o hero).
    /// Contiene statistiche base e comportamenti.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWorker", menuName = "Wilderness Survival/Data/Worker Definition")]
    public class WorkerData : ScriptableObject
    {
        // ============================================
        // IDENTIFICAZIONE
        // ============================================

        [TitleGroup("Identificazione")]
        [HorizontalGroup("Identificazione/Row1", 0.65f)]
        [VerticalGroup("Identificazione/Row1/Left")]
        [LabelWidth(100)]
        [Tooltip("ID univoco del worker")]
        [SerializeField] private string workerId;

        [VerticalGroup("Identificazione/Row1/Left")]
        [LabelWidth(100)]
        [Tooltip("Nome visualizzato")]
        [SerializeField] private string displayName;

        [HorizontalGroup("Identificazione/Row1", 0.35f)]
        [VerticalGroup("Identificazione/Row1/Right")]
        [PreviewField(60, ObjectFieldAlignment.Right)]
        [HideLabel]
        [SerializeField] private Sprite portrait;

        [VerticalGroup("Identificazione/Row1/Right")]
        [PreviewField(60, ObjectFieldAlignment.Right)]
        [HideLabel]
        [Tooltip("Prefab del worker")]
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
        [SerializeField] private WorkerType workerType = WorkerType.Villager;

        [HorizontalGroup("Classificazione/Row1")]
        [LabelWidth(80)]
        [EnumToggleButtons]
        [Tooltip("Ruolo principale")]
        [SerializeField] private WildernessSurvival.Gameplay.Structures.WorkerRole defaultRole;

        // ============================================
        // STATISTICHE BASE
        // ============================================

        [TitleGroup("Statistiche")]
        [BoxGroup("Statistiche/Vitality")]
        [HorizontalGroup("Statistiche/Vitality/Row1")]
        [LabelWidth(100)]
        [PropertyRange(50, 500)]
        [SerializeField] private int baseHealth = 100;

        [HorizontalGroup("Statistiche/Vitality/Row1")]
        [LabelWidth(100)]
        [PropertyRange(0, 50)]
        [SerializeField] private int baseArmor = 5;

        [BoxGroup("Statistiche/Movement")]
        [HorizontalGroup("Statistiche/Movement/Row1")]
        [LabelWidth(120)]
        [PropertyRange(1f, 10f)]
        [SuffixLabel("m/s", Overlay = true)]
        [SerializeField] private float movementSpeed = 3.5f;

        [BoxGroup("Statistiche/Work")]
        [HorizontalGroup("Statistiche/Work/Row1")]
        [LabelWidth(120)]
        [PropertyRange(0.5f, 2f)]
        [Tooltip("Moltiplicatore produttività")]
        [SerializeField] private float productivityMultiplier = 1f;

        [HorizontalGroup("Statistiche/Work/Row1")]
        [LabelWidth(120)]
        [PropertyRange(0.5f, 3f)]
        [Tooltip("Velocità costruzione")]
        [SerializeField] private float buildSpeedMultiplier = 1f;

        // ============================================
        // AFFINITÀ RUOLI
        // ============================================

        [TitleGroup("Affinità Ruoli")]
        [InfoBox("Bonus quando assegnato ai vari ruoli (+0% = normale, +50% = ottimo)", InfoMessageType.Info)]
        [TableList(ShowIndexLabels = false, AlwaysExpanded = true)]
        [SerializeField] private RoleAffinity[] roleAffinities;

        // ============================================
        // COMBAT (per Hero e Guard)
        // ============================================

        [TitleGroup("Combattimento")]
        [ShowIf("@workerType == WorkerType.Hero || defaultRole == WildernessSurvival.Gameplay.Structures.WorkerRole.Guard")]
        [BoxGroup("Combattimento/Attack")]
        [HorizontalGroup("Combattimento/Attack/Row1")]
        [LabelWidth(100)]
        [PropertyRange(5f, 100f)]
        [SerializeField] private float attackDamage = 10f;

        [HorizontalGroup("Combattimento/Attack/Row1")]
        [LabelWidth(100)]
        [PropertyRange(0.5f, 3f)]
        [SuffixLabel("sec", Overlay = true)]
        [SerializeField] private float attackInterval = 1.5f;

        [HorizontalGroup("Combattimento/Attack/Row2")]
        [LabelWidth(100)]
        [PropertyRange(1f, 10f)]
        [SuffixLabel("m", Overlay = true)]
        [SerializeField] private float attackRange = 2f;

        // ============================================
        // NEEDS & MORALE
        // ============================================

        [TitleGroup("Bisogni & Morale")]
        [BoxGroup("Bisogni & Morale/Stats")]
        [HorizontalGroup("Bisogni & Morale/Stats/Row1")]
        [LabelWidth(120)]
        [PropertyRange(50, 200)]
        [Tooltip("Stamina massima")]
        [SerializeField] private float maxStamina = 100f;

        [HorizontalGroup("Bisogni & Morale/Stats/Row1")]
        [LabelWidth(120)]
        [PropertyRange(1f, 20f)]
        [SuffixLabel("/min", Overlay = true)]
        [Tooltip("Consumo stamina lavorando")]
        [SerializeField] private float staminaDrainRate = 5f;

        [HorizontalGroup("Bisogni & Morale/Stats/Row2")]
        [LabelWidth(120)]
        [PropertyRange(1f, 30f)]
        [SuffixLabel("/min", Overlay = true)]
        [Tooltip("Rigenera stamina riposando")]
        [SerializeField] private float staminaRegenRate = 10f;

        // ============================================
        // PROPERTIES
        // ============================================

        public string WorkerId => workerId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Portrait => portrait;
        public GameObject Prefab => prefab;
        public WorkerType WorkerType => workerType;
        public WildernessSurvival.Gameplay.Structures.WorkerRole DefaultRole => defaultRole;
        public int BaseHealth => baseHealth;
        public int BaseArmor => baseArmor;
        public float MovementSpeed => movementSpeed;
        public float ProductivityMultiplier => productivityMultiplier;
        public float BuildSpeedMultiplier => buildSpeedMultiplier;
        public RoleAffinity[] RoleAffinities => roleAffinities;
        public float AttackDamage => attackDamage;
        public float AttackInterval => attackInterval;
        public float AttackRange => attackRange;
        public float MaxStamina => maxStamina;
        public float StaminaDrainRate => staminaDrainRate;
        public float StaminaRegenRate => staminaRegenRate;

        // ============================================
        // METODI
        // ============================================

        /// <summary>
        /// Ottiene il bonus per un ruolo specifico
        /// </summary>
        public float GetRoleBonus(WildernessSurvival.Gameplay.Structures.WorkerRole role)
        {
            if (roleAffinities == null) return 1f;

            foreach (var affinity in roleAffinities)
            {
                if (affinity.role == role)
                {
                    return 1f + (affinity.bonusPercent / 100f);
                }
            }
            return 1f;
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

        [TitleGroup("Debug")]
        [Button("📊 Print Stats", ButtonSizes.Medium)]
        private void DebugPrintStats()
        {
            Debug.Log($"=== {displayName} ({workerType}) ===\n" +
                $"Role: {defaultRole}\n" +
                $"Health: {baseHealth} | Armor: {baseArmor}\n" +
                $"Speed: {movementSpeed}m/s\n" +
                $"Productivity: {productivityMultiplier}x\n" +
                $"Stamina: {maxStamina} (Drain: {staminaDrainRate}/min)");
        }

        [Button("🎯 Print Role Affinities", ButtonSizes.Medium)]
        private void DebugPrintAffinities()
        {
            if (roleAffinities == null || roleAffinities.Length == 0)
            {
                Debug.Log($"{displayName} has no role affinities defined.");
                return;
            }

            Debug.Log($"=== {displayName} Role Bonuses ===");
            foreach (var affinity in roleAffinities)
            {
                Debug.Log($"  {affinity.role}: +{affinity.bonusPercent}%");
            }
        }
#endif
    }

    // ============================================
    // ENUMS
    // ============================================

    public enum WorkerType
    {
        [LabelText("👷 Villager")]
        Villager,

        [LabelText("⚔️ Hero")]
        Hero
    }

    // ============================================
    // STRUCTS
    // ============================================

    [System.Serializable]
    public struct RoleAffinity
    {
        [HorizontalGroup("Row", 0.5f)]
        [HideLabel]
        [EnumToggleButtons]
        public WildernessSurvival.Gameplay.Structures.WorkerRole role;

        [HorizontalGroup("Row")]
        [HideLabel]
        [PropertyRange(-50, 100)]
        [SuffixLabel("%", Overlay = true)]
        public int bonusPercent;
    }
}