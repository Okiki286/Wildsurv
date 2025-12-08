using UnityEngine;
using Sirenix.OdinInspector;
using WildernessSurvival.Gameplay.Structures;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// Definisce i dati di un Job per i worker.
    /// Ogni job ha un ruolo, un modello visivo, statistiche e proprietà di combattimento.
    /// NOTA: visualModelPrefab deve essere un modello 3D PURO, non un worker completo.
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
        // ROLE & VISUAL
        // ============================================

        [TitleGroup("Role & Visual")]
        [SerializeField]
        [Tooltip("Il ruolo associato a questo job (chiave per lookup)")]
        private WorkerRole role;
        public WorkerRole Role => role;

        [SerializeField]
        [ColorPalette]
        private Color jobColor = Color.white;
        public Color JobColor => jobColor;

        [SerializeField]
        [Tooltip("Il modello 3D PURO da istanziare come figlio di visualRoot. NON un worker completo!")]
        [Required("Assegna un modello 3D visivo per questo job")]
        private GameObject visualModelPrefab;
        public GameObject VisualModelPrefab => visualModelPrefab;

        [SerializeField]
        [Tooltip("AnimatorController da usare. Se null, usa quello del modello.")]
        private RuntimeAnimatorController animatorController;
        public RuntimeAnimatorController AnimatorController => animatorController;

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
        [Button("📋 Print Job Info", ButtonSizes.Medium)]
        private void DebugPrintInfo()
        {
            Debug.Log($"=== JOB: {jobName} ===\n" +
                     $"Role: {role}\n" +
                     $"Visual: {(visualModelPrefab != null ? visualModelPrefab.name : "NONE")}\n" +
                     $"Animator: {(animatorController != null ? animatorController.name : "From Model")}\n" +
                     $"Productivity: {productivityBonus:F2}x\n" +
                     $"Build Speed: {buildSpeedBonus:F2}x");
        }
#endif
    }
}
