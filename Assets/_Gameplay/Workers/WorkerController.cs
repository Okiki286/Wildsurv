using UnityEngine;
using Sirenix.OdinInspector;
using WildernessSurvival.Gameplay.Structures;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// Controlla il comportamento runtime di un singolo worker.
    /// Gestisce movimento, stato, stamina, assignment.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class WorkerController : MonoBehaviour
    {
        // ============================================
        // RIFERIMENTI
        // ============================================

        [TitleGroup("Setup")]
        [Required("WorkerData è richiesto!")]
        [AssetsOnly]
        [SerializeField] private WorkerData workerData;

        [BoxGroup("Setup/Components")]
        [ChildGameObjectsOnly]
        [SerializeField] private Animator animator;

        [BoxGroup("Setup/Components")]
        [ChildGameObjectsOnly]
        [SerializeField] private Transform visualRoot;

        private Rigidbody rb;

        // ============================================
        // STATO RUNTIME
        // ============================================

        [TitleGroup("Runtime State")]
        [BoxGroup("Runtime State/Core")]
        [ReadOnly]
        [ShowInInspector]
        [EnumToggleButtons]
        private WorkerState currentState = WorkerState.Idle;

        [BoxGroup("Runtime State/Core")]
        [ReadOnly]
        [ShowInInspector]
        [EnumToggleButtons]
        private WorkerRole assignedRole;

        [BoxGroup("Runtime State/Assignment")]
        [ReadOnly]
        [ShowInInspector]
        private GameObject assignedStructure;

        [BoxGroup("Runtime State/Assignment")]
        [ReadOnly]
        [ShowInInspector]
        private Vector3 workPosition;

        // ============================================
        // STATS RUNTIME
        // ============================================

        [TitleGroup("Current Stats")]
        [BoxGroup("Current Stats/Health")]
        [ReadOnly]
        [ShowInInspector]
        [ProgressBar(0, 100, ColorGetter = "GetHealthColor")]
        private float currentHealth;

        [BoxGroup("Current Stats/Stamina")]
        [ReadOnly]
        [ShowInInspector]
        [ProgressBar(0, 100, ColorGetter = "GetStaminaColor")]
        private float currentStamina;

        [BoxGroup("Current Stats/Morale")]
        [ReadOnly]
        [ShowInInspector]
        [PropertyRange(0, 100)]
        private float currentMorale = 75f;

        // ============================================
        // MOVEMENT
        // ============================================

        private Vector3 moveTarget;
        private bool isMoving = false;
        private float movementThreshold = 0.2f;

        // ============================================
        // PROPERTIES
        // ============================================

        public WorkerData Data => workerData;
        public WorkerState State => currentState;
        public WorkerRole AssignedRole => assignedRole;
        public GameObject AssignedStructure => assignedStructure;
        public float CurrentHealth => currentHealth;
        public float CurrentStamina => currentStamina;
        public float CurrentMorale => currentMorale;
        public bool IsAlive => currentHealth > 0;
        public bool IsTired => currentStamina < 20f;

        // ============================================
        // UNITY LIFECYCLE
        // ============================================

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            if (workerData != null)
            {
                InitializeStats();
            }
        }

        private void Start()
        {
            if (workerData == null)
            {
                Debug.LogError($"[WorkerController] {gameObject.name} has no WorkerData assigned!", this);
                enabled = false;
                return;
            }

            Debug.Log($"<color=cyan>[Worker]</color> {workerData.DisplayName} spawned as {workerData.DefaultRole}");
        }

        private void Update()
        {
            if (!IsAlive) return;

            HandleMovement();
            UpdateStamina();
            UpdateState();
            UpdateAnimations();
        }

        // ============================================
        // INIZIALIZZAZIONE
        // ============================================

        private void InitializeStats()
        {
            currentHealth = workerData.BaseHealth;
            currentStamina = workerData.MaxStamina;
            currentMorale = 75f;
            assignedRole = workerData.DefaultRole;
        }

        /// <summary>
        /// Setup iniziale con WorkerData (chiamato da WorkerSystem)
        /// </summary>
        public void Initialize(WorkerData data)
        {
            workerData = data;
            InitializeStats();
            gameObject.name = $"Worker_{data.DisplayName}";
        }

        // ============================================
        // STATE MACHINE
        // ============================================

        private void UpdateState()
        {
            switch (currentState)
            {
                case WorkerState.Idle:
                    // In attesa di assignment
                    break;

                case WorkerState.MovingToWork:
                    if (!isMoving)
                    {
                        ChangeState(WorkerState.Working);
                    }
                    break;

                case WorkerState.Working:
                    DrainStamina();
                    if (IsTired)
                    {
                        ChangeState(WorkerState.Resting);
                    }
                    break;

                case WorkerState.Resting:
                    RegenStamina();
                    if (currentStamina >= workerData.MaxStamina * 0.8f)
                    {
                        ChangeState(WorkerState.Idle);
                    }
                    break;

                case WorkerState.Retreating:
                    // Ritorna alla base durante la notte
                    break;

                case WorkerState.Combat:
                    // Combatte contro nemici
                    break;
            }
        }

        private void ChangeState(WorkerState newState)
        {
            if (currentState == newState) return;

            Debug.Log($"<color=cyan>[Worker]</color> {workerData.DisplayName}: {currentState} → {newState}");
            currentState = newState;
        }

        // ============================================
        // ASSIGNMENT
        // ============================================

        /// <summary>
        /// Assegna questo worker a una struttura
        /// </summary>
        public void AssignToStructure(GameObject structure, WorkerRole role, Vector3 workPos)
        {
            assignedStructure = structure;
            assignedRole = role;
            workPosition = workPos;

            Debug.Log($"<color=cyan>[Worker]</color> {workerData.DisplayName} assigned to {structure.name} as {role}");

            MoveTo(workPosition);
            ChangeState(WorkerState.MovingToWork);
        }

        /// <summary>
        /// Rimuove l'assignment corrente
        /// </summary>
        public void Unassign()
        {
            assignedStructure = null;
            assignedRole = WorkerRole.None;
            ChangeState(WorkerState.Idle);

            Debug.Log($"<color=cyan>[Worker]</color> {workerData.DisplayName} unassigned");
        }

        // ============================================
        // MOVEMENT
        // ============================================

        /// <summary>
        /// Muovi verso una posizione target
        /// </summary>
        public void MoveTo(Vector3 target)
        {
            moveTarget = target;
            isMoving = true;
        }

        private void HandleMovement()
        {
            if (!isMoving) return;

            Vector3 direction = (moveTarget - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, moveTarget);

            if (distance > movementThreshold)
            {
                // Muovi verso il target
                Vector3 movement = direction * workerData.MovementSpeed * Time.deltaTime;
                rb.MovePosition(transform.position + movement);

                // Ruota verso la direzione
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
                }
            }
            else
            {
                // Arrivato al target
                isMoving = false;
                rb.linearVelocity = Vector3.zero;
            }
        }

        // ============================================
        // STAMINA & NEEDS
        // ============================================

        private void UpdateStamina()
        {
            currentStamina = Mathf.Clamp(currentStamina, 0f, workerData.MaxStamina);
        }

        private void DrainStamina()
        {
            currentStamina -= workerData.StaminaDrainRate * Time.deltaTime / 60f;
        }

        private void RegenStamina()
        {
            currentStamina += workerData.StaminaRegenRate * Time.deltaTime / 60f;
        }

        /// <summary>
        /// Calcola la produttività effettiva (0-1)
        /// </summary>
        public float GetEffectiveProductivity()
        {
            float base_productivity = workerData.ProductivityMultiplier;

            // Bonus ruolo
            float roleBonus = workerData.GetRoleBonus(assignedRole);

            // Penalty stamina
            float staminaPenalty = Mathf.Clamp01(currentStamina / workerData.MaxStamina);

            // Penalty morale
            float moralePenalty = Mathf.Clamp01(currentMorale / 100f);

            return base_productivity * roleBonus * staminaPenalty * moralePenalty;
        }

        // ============================================
        // COMBAT
        // ============================================

        /// <summary>
        /// Infliggi danno a questo worker
        /// </summary>
        public void TakeDamage(float damage)
        {
            float actualDamage = Mathf.Max(0, damage - workerData.BaseArmor);
            currentHealth -= actualDamage;

            Debug.Log($"<color=red>[Worker]</color> {workerData.DisplayName} took {actualDamage:F1} damage ({currentHealth:F1} HP remaining)");

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            Debug.Log($"<color=red>[Worker]</color> {workerData.DisplayName} died!");
            ChangeState(WorkerState.Dead);

            // Notifica WorkerSystem
            // WorkerSystem.Instance?.OnWorkerDied(this);

            // TODO: Death animation, VFX
            Destroy(gameObject, 2f);
        }

        // ============================================
        // ANIMATIONS
        // ============================================

        private void UpdateAnimations()
        {
            if (animator == null) return;

            // Parametri base
            animator.SetBool("IsMoving", isMoving);
            animator.SetBool("IsWorking", currentState == WorkerState.Working);
            animator.SetFloat("MoveSpeed", isMoving ? 1f : 0f);
        }

        // ============================================
        // DEBUG & ODIN
        // ============================================

#if UNITY_EDITOR
        private Color GetHealthColor()
        {
            float percent = currentHealth / workerData.BaseHealth;
            if (percent > 0.6f) return Color.green;
            if (percent > 0.3f) return Color.yellow;
            return Color.red;
        }

        private Color GetStaminaColor()
        {
            float percent = currentStamina / workerData.MaxStamina;
            if (percent > 0.6f) return Color.cyan;
            if (percent > 0.3f) return Color.yellow;
            return Color.red;
        }

        [TitleGroup("Debug Actions")]
        [ButtonGroup("Debug Actions/Row1")]
        [Button("🏃 Move to (0,0,5)", ButtonSizes.Medium)]
        private void DebugMove()
        {
            MoveTo(new Vector3(0, 0, 5));
            ChangeState(WorkerState.MovingToWork);
        }

        [ButtonGroup("Debug Actions/Row1")]
        [Button("💼 Start Working", ButtonSizes.Medium)]
        private void DebugWork()
        {
            ChangeState(WorkerState.Working);
        }

        [ButtonGroup("Debug Actions/Row2")]
        [Button("😴 Rest", ButtonSizes.Medium)]
        private void DebugRest()
        {
            ChangeState(WorkerState.Resting);
        }

        [ButtonGroup("Debug Actions/Row2")]
        [Button("💥 Take 20 Damage", ButtonSizes.Medium)]
        [GUIColor(1f, 0.5f, 0.5f)]
        private void DebugDamage()
        {
            TakeDamage(20f);
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            // Disegna linea verso target
            if (isMoving)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, moveTarget);
                Gizmos.DrawWireSphere(moveTarget, 0.5f);
            }

            // Disegna struttura assegnata
            if (assignedStructure != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, assignedStructure.transform.position);
            }
        }
#endif
    }

    // ============================================
    // ENUMS
    // ============================================

    public enum WorkerState
    {
        Idle,
        MovingToWork,
        Working,
        Resting,
        Retreating,
        Combat,
        Dead
    }
}