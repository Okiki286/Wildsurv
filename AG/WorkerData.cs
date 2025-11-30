using UnityEngine;
using Sirenix.OdinInspector;
using WildernessSurvival.Gameplay.Structures;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// Definisce un tipo di worker (dati statici).
    /// Crea uno per ogni tipo: Gatherer, Builder, Guard, etc.
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
        public float GetBonusForStructure(StructureData structure)
        {
            if (structure == null) return 0f;

            // Check if role matches structure's allowed roles
            if ((structure.AllowedRoles & role) != 0)
            {
                return productionBonus / 100f;
            }

            // Partial bonus if not ideal role (50% efficiency)
            return (productionBonus / 100f) * 0.5f;
        }

        /// <summary>
        /// Verifica se questo worker è ideale per una struttura
        /// </summary>
        public bool IsIdealForStructure(StructureData structure)
        {
            if (structure == null) return false;
            return (structure.AllowedRoles & role) != 0;
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
            
            // Set default role color if not set
            if (roleColor == Color.white)
            {
                roleColor = GetDefaultRoleColor(role);
            }
        }

        private Color GetDefaultRoleColor(WorkerRole r)
        {
            return r switch
            {
                WorkerRole.Gatherer => new Color(0.4f, 0.7f, 0.3f),  // Verde
                WorkerRole.Builder => new Color(0.7f, 0.5f, 0.2f),   // Arancione
                WorkerRole.Guard => new Color(0.7f, 0.3f, 0.3f),     // Rosso
                WorkerRole.Scout => new Color(0.3f, 0.5f, 0.7f),     // Blu
                WorkerRole.Crafter => new Color(0.6f, 0.4f, 0.7f),   // Viola
                WorkerRole.Researcher => new Color(0.8f, 0.8f, 0.3f),// Giallo
                _ => Color.gray
            };
        }

        [TitleGroup("Debug")]
        [Button("Test Bonus Calculation", ButtonSizes.Medium)]
        private void DebugTestBonus()
        {
            Debug.Log($"=== {displayName} ({role}) ===");
            Debug.Log($"Production Bonus: {productionBonus}%");
            Debug.Log($"Build Speed Bonus: {buildSpeedBonus}%");
            Debug.Log($"Combat Bonus: {combatBonus}%");
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

        [LabelText("Gatherer")]
        Gatherer = 1 << 0,

        [LabelText("Builder")]
        Builder = 1 << 1,

        [LabelText("Guard")]
        Guard = 1 << 2,

        [LabelText("Scout")]
        Scout = 1 << 3,

        [LabelText("Crafter")]
        Crafter = 1 << 4,

        [LabelText("Researcher")]
        Researcher = 1 << 5,

        // Combinazioni comuni
        All = Gatherer | Builder | Guard | Scout | Crafter | Researcher
    }
}
