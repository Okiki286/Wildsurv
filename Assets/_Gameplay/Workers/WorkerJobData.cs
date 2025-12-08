using UnityEngine;
using Sirenix.OdinInspector;
using WildernessSurvival.Gameplay.Structures;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// Definisce i dati di un Job per i worker.
    /// Ogni job ha un ruolo, statistiche gameplay e un WorkerVisualSet per la skin.
    /// Sistema data-driven: basta creare nuovi JobData per aggiungere job.
    /// </summary>
    [CreateAssetMenu(fileName = "NewJobData", menuName = "Wilderness Survival/Worker Job Data")]
    public class WorkerJobData : ScriptableObject
    {
        // ============================================
        // IDENTITY
        // ============================================

        [TitleGroup("Identity")]
        [SerializeField]
        private string jobId;
        public string JobId => jobId;

        [SerializeField]
        private string jobName;
        public string JobName => jobName;

        [SerializeField]
        [PreviewField(50)]
        private Sprite icon;
        public Sprite Icon => icon;

        [SerializeField]
        [TextArea(2, 4)]
        private string description;
        public string Description => description;

        // ============================================
        // ROLE
        // ============================================

        [TitleGroup("Role")]
        [SerializeField]
        [Tooltip("Il ruolo associato a questo job (chiave per lookup)")]
        private WorkerRole role;
        public WorkerRole Role => role;

        [SerializeField]
        [ColorPalette]
        private Color jobColor = Color.white;
        public Color JobColor => jobColor;

        // ============================================
        // VISUAL SET (Nuovo sistema mesh swap)
        // ============================================

        [TitleGroup("Visual Set")]
        [SerializeField]
        [Tooltip("Preset visivo completo per questo job (mesh, tool, animator, colori)")]
        [InlineProperty]
        [HideLabel]
        private WorkerVisualSet visualSet = new WorkerVisualSet();
        public WorkerVisualSet VisualSet => visualSet;

        // ============================================
        // LEGACY SUPPORT (da rimuovere dopo migrazione)
        // ============================================

        [TitleGroup("Legacy (Deprecated)")]
        [SerializeField]
        [Tooltip("DEPRECATED: Usa VisualSet.animatorController invece")]
        [InfoBox("Questo campo è deprecato. Migra a VisualSet.", InfoMessageType.Warning)]
        private GameObject visualModelPrefab;
        public GameObject VisualModelPrefab => visualModelPrefab;

        [SerializeField]
        [Tooltip("DEPRECATED: Usa VisualSet.animatorController invece")]
        private RuntimeAnimatorController animatorController;
        public RuntimeAnimatorController AnimatorController => visualSet?.animatorController ?? animatorController;

        // ============================================
        // STATS
        // ============================================

        [TitleGroup("Stats")]
        [SerializeField]
        [PropertyRange(0.5f, 3f)]
        [Tooltip("Moltiplicatore produttività (1.0 = base)")]
        private float productivityBonus = 1f;
        public float ProductivityBonus => productivityBonus;

        [SerializeField]
        [PropertyRange(0.5f, 3f)]
        [Tooltip("Moltiplicatore velocità costruzione (1.0 = base)")]
        private float buildSpeedBonus = 1f;
        public float BuildSpeedBonus => buildSpeedBonus;

        [SerializeField]
        [PropertyRange(1f, 10f)]
        [Tooltip("Velocità di movimento")]
        private float movementSpeed = 3.5f;
        public float MovementSpeed => movementSpeed;

        // ============================================
        // COMBAT
        // ============================================

        [TitleGroup("Combat")]
        [SerializeField]
        [PropertyRange(0f, 100f)]
        private float attackDamage = 10f;
        public float AttackDamage => attackDamage;

        [SerializeField]
        [PropertyRange(0.5f, 5f)]
        private float attackInterval = 1.5f;
        public float AttackInterval => attackInterval;

        [SerializeField]
        [PropertyRange(1f, 10f)]
        private float attackRange = 2f;
        public float AttackRange => attackRange;

        // ============================================
        // EDITOR VALIDATION
        // ============================================

        // ============================================
        // HELPER METHODS
        // ============================================

        /// <summary>
        /// Verifica se questo job ha un visual set valido per il mesh swap.
        /// </summary>
        public bool HasValidVisualSet => visualSet != null && visualSet.HasValidMesh;

        /// <summary>
        /// Verifica se usare il sistema legacy (prefab swap) o il nuovo (mesh swap).
        /// </summary>
        public bool UseLegacySystem => !HasValidVisualSet && visualModelPrefab != null;

        // ============================================
        // EDITOR VALIDATION
        // ============================================

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-genera jobId dal nome se vuoto
            if (string.IsNullOrEmpty(jobId))
            {
                jobId = name.ToLower().Replace(" ", "_");
            }

            // Auto-genera jobName dal nome se vuoto
            if (string.IsNullOrEmpty(jobName))
            {
                jobName = name;
            }
        }

        [TitleGroup("Debug")]
        [Button("Print Job Info", ButtonSizes.Medium)]
        private void DebugPrintInfo()
        {
            string visualInfo = HasValidVisualSet
                ? $"VisualSet (Body: {(visualSet.bodyMesh != null ? visualSet.bodyMesh.name : "None")})"
                : (visualModelPrefab != null ? $"Legacy Prefab: {visualModelPrefab.name}" : "NONE");

            Debug.Log($"=== JOB: {jobName} ===\n" +
                     $"Role: {role}\n" +
                     $"Visual: {visualInfo}\n" +
                     $"Animator: {(AnimatorController != null ? AnimatorController.name : "None")}\n" +
                     $"Tool: {(visualSet?.toolPrefab != null ? visualSet.toolPrefab.name : "None")}\n" +
                     $"Productivity: {productivityBonus:F2}x\n" +
                     $"Build Speed: {buildSpeedBonus:F2}x");
        }

        [TitleGroup("Debug")]
        [Button("Validate Visual Set", ButtonSizes.Medium)]
        private void DebugValidateVisualSet()
        {
            if (HasValidVisualSet)
            {
                Debug.Log($"<color=green>[{jobName}] VisualSet is valid and ready for mesh swap.</color>");
            }
            else if (UseLegacySystem)
            {
                Debug.LogWarning($"<color=yellow>[{jobName}] Using LEGACY prefab swap. Consider migrating to VisualSet.</color>");
            }
            else
            {
                Debug.LogError($"<color=red>[{jobName}] No valid visual configuration! Add VisualSet meshes or legacy prefab.</color>");
            }
        }
#endif
    }
}
