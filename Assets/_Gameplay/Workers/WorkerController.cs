using UnityEngine;
using UnityEngine.AI;
using Sirenix.OdinInspector;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// Stati di movimento del worker.
    /// </summary>
    public enum MovementState
    {
        Idle,           // Non sta facendo nulla
        Traveling,      // Si sta muovendo verso una destinazione lontana
        WorkingOnSite   // Arrivato al worksite, gironzola localmente
    }

    /// <summary>
    /// Controller fisico per i worker nella scena.
    /// Gestisce movimento, animazioni e work wandering.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class WorkerController : MonoBehaviour
    {
        // ============================================
        // RIFERIMENTI
        // ============================================

        [TitleGroup("Data")]
        [SerializeField, Required]
        private WorkerData workerData;

        [TitleGroup("Components")]
        [SerializeField, ReadOnly]
        private NavMeshAgent agent;

        [SerializeField]
        [Tooltip("Animator del modello 3D. Se vuoto, cerca in children.")]
        private Animator animator;

        // ============================================
        // MOVEMENT SETTINGS
        // ============================================

        [BoxGroup("Movement Settings")]
        [SerializeField]
        [Tooltip("Raggio entro cui il worker gironzola mentre lavora")]
        [PropertyRange(1f, 10f)]
        private float workWanderRadius = 3f;

        [BoxGroup("Movement Settings")]
        [SerializeField]
        [Tooltip("Intervallo in secondi tra un cambio di posizione e l'altro durante il lavoro")]
        [PropertyRange(1f, 5f)]
        private float changeSpotInterval = 2.5f;

        [BoxGroup("Movement Settings")]
        [SerializeField]
        [Tooltip("Distanza massima per il campionamento NavMesh")]
        [PropertyRange(1f, 5f)]
        private float navMeshSampleDistance = 2f;

        // ============================================
        // ANIMATION SETTINGS
        // ============================================

        [BoxGroup("Animation Settings")]
        [SerializeField]
        [Tooltip("Soglia di velocità sotto la quale l'animazione di lavoro si attiva")]
        [PropertyRange(0.01f, 0.5f)]
        private float workAnimationSpeedThreshold = 0.1f;

        [BoxGroup("Animation Settings")]
        [SerializeField]
        [Tooltip("Velocità di rotazione verso la struttura mentre lavora")]
        [PropertyRange(1f, 10f)]
        private float lookAtRotationSpeed = 5f;

        [BoxGroup("Animation Settings")]
        [SerializeField]
        [Tooltip("Nome dello stato Idle nell'Animator")]
        private string idleStateName = "Idle";

        // ============================================
        // LINKED INSTANCE
        // ============================================

        private WorkerInstance linkedInstance;

        // ============================================
        // RUNTIME STATE
        // ============================================

        [TitleGroup("Runtime")]
        [ShowInInspector, ReadOnly]
        private MovementState currentMovementState = MovementState.Idle;

        [ShowInInspector, ReadOnly]
        [LabelText("Is Patrolling Worksite")]
        private bool isPatrollingWorksite = false;

        [ShowInInspector, ReadOnly]
        [LabelText("Is Forced Idle")]
        private bool isForcedIdle = false;

        [ShowInInspector, ReadOnly]
        private bool isMoving = false;

        [ShowInInspector, ReadOnly]
        private Vector3 targetPosition;

        [ShowInInspector, ReadOnly]
        [LabelText("Is Playing Work Anim")]
        private bool isPlayingWorkAnimation = false;

        // Work Wandering State
        private Vector3 currentWorkTargetCenter;
        private Vector3 structurePosition;
        private float workTimer;

        // Cached speed for animation
        private float currentSpeed;

        // ============================================
        // PROPERTIES
        // ============================================

        public WorkerData Data => workerData;
        public bool IsAlive => linkedInstance?.IsAlive ?? true;
        public bool IsMoving => isMoving;
        public MovementState CurrentMovementState => currentMovementState;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            
            // Fallback: cerca l'Animator nei children se non è assegnato
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    Debug.Log($"<color=yellow>[WorkerController]</color> Animator trovato in children: {animator.gameObject.name}");
                }
            }

            if (WorkerSystem.Instance != null)
            {
                WorkerSystem.Instance.RegisterWorker(this);
            }
        }

        private void OnDestroy()
        {
            if (WorkerSystem.Instance != null)
            {
                WorkerSystem.Instance.UnregisterWorker(this);
            }
        }

        // ============================================
        // INSTANCE LINKING
        // ============================================

        public void LinkToInstance(WorkerInstance instance)
        {
            linkedInstance = instance;
            Debug.Log($"<color=cyan>[WorkerController]</color> Linked to instance: {instance?.CustomName}");
        }

        // ============================================
        // UPDATE (chiamato da WorkerSystem)
        // ============================================

        public void ManualUpdate(float deltaTime)
        {
            // ═══════════════════════════════════════════════════════════
            // BLOCCO CRITICO: Se forzato in idle, non fare NULLA
            // ═══════════════════════════════════════════════════════════
            if (isForcedIdle)
            {
                return; // BLOCCO TOTALE - nessuna logica eseguita
            }

            if (agent == null || linkedInstance == null) return;

            // Calcola la velocità corrente (usata per animazioni)
            currentSpeed = agent.velocity.magnitude;

            // Se non siamo in pattugliamento attivo e siamo idle, non fare nulla
            if (currentMovementState == MovementState.Idle && !isPatrollingWorksite)
            {
                UpdateIdleState();
                UpdateAnimations();
                return;
            }

            switch (currentMovementState)
            {
                case MovementState.Idle:
                    UpdateIdleState();
                    break;

                case MovementState.Traveling:
                    UpdateTravelingState(deltaTime);
                    break;

                case MovementState.WorkingOnSite:
                    if (isPatrollingWorksite)
                    {
                        UpdateWorkingOnSiteState(deltaTime);
                    }
                    else
                    {
                        UpdateIdleState();
                    }
                    break;
            }

            UpdateAnimations();
        }

        // ============================================
        // STATE UPDATES
        // ============================================

        private void UpdateIdleState()
        {
            isMoving = false;
            isPlayingWorkAnimation = false;
        }

        private void UpdateTravelingState(float deltaTime)
        {
            if (agent == null) return;

            isMoving = currentSpeed > 0.01f;
            isPlayingWorkAnimation = false;

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                OnArrivedAtDestination();
            }
            else if (isMoving)
            {
                linkedInstance.IsAtWorksite = false;
                linkedInstance.SetState(WorkerState.Moving);
            }
        }

        private void UpdateWorkingOnSiteState(float deltaTime)
        {
            if (agent == null) return;

            if (!isPatrollingWorksite)
            {
                currentMovementState = MovementState.Idle;
                return;
            }

            isMoving = currentSpeed > 0.01f;
            isPlayingWorkAnimation = currentSpeed < workAnimationSpeedThreshold;

            if (isPlayingWorkAnimation)
            {
                RotateTowardsStructure(deltaTime);
            }

            workTimer -= deltaTime;

            if (workTimer <= 0f)
            {
                MoveToRandomWorkPoint();
                workTimer = changeSpotInterval;
            }

            if (linkedInstance != null && linkedInstance.IsAssigned)
            {
                linkedInstance.SetState(WorkerState.Working);
            }
        }

        // ============================================
        // ROTATION TOWARDS STRUCTURE
        // ============================================

        private void RotateTowardsStructure(float deltaTime)
        {
            Vector3 directionToStructure = structurePosition - transform.position;
            directionToStructure.y = 0;

            if (directionToStructure.sqrMagnitude < 0.01f) return;

            Quaternion targetRotation = Quaternion.LookRotation(directionToStructure);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                lookAtRotationSpeed * deltaTime
            );
        }

        // ============================================
        // ARRIVAL LOGIC
        // ============================================

        private void OnArrivedAtDestination()
        {
            currentMovementState = MovementState.WorkingOnSite;
            isPatrollingWorksite = true;
            currentWorkTargetCenter = transform.position;
            
            if (linkedInstance?.AssignedStructure != null)
            {
                structurePosition = linkedInstance.AssignedStructure.transform.position;
            }
            else
            {
                structurePosition = targetPosition;
            }
            
            workTimer = 0.5f;

            if (linkedInstance != null)
            {
                if (linkedInstance.IsAssigned && !linkedInstance.IsAtWorksite)
                {
                    linkedInstance.IsAtWorksite = true;
                    linkedInstance.SetState(WorkerState.Working);

                    linkedInstance.AssignedStructure?.RecalculateBuildSpeed();
                    linkedInstance.AssignedStructure?.RecalculateProduction();

                    Debug.Log($"<color=green>[WorkerController]</color> {linkedInstance.CustomName} arrived at worksite!");
                }
            }
        }

        // ============================================
        // WORK WANDERING
        // ============================================

        private void MoveToRandomWorkPoint()
        {
            if (!isPatrollingWorksite || isForcedIdle) return;

            Vector2 randomCircle = Random.insideUnitCircle * workWanderRadius;
            Vector3 randomPoint = currentWorkTargetCenter + new Vector3(randomCircle.x, 0f, randomCircle.y);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, navMeshSampleDistance, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            else
            {
                agent.SetDestination(currentWorkTargetCenter);
            }
        }

        // ============================================
        // MOVEMENT COMMANDS
        // ============================================

        /// <summary>
        /// Comanda il worker di muoversi verso una posizione.
        /// SBLOCCA il worker se era in ForceIdle.
        /// </summary>
        public void CommandMoveTo(Vector3 position)
        {
            if (agent == null) return;

            // SBLOCCA IL WORKER
            isForcedIdle = false;
            
            currentMovementState = MovementState.Traveling;
            isPatrollingWorksite = false;
            targetPosition = position;
            structurePosition = position;
            
            agent.isStopped = false;
            agent.SetDestination(position);
            isMoving = true;
            isPlayingWorkAnimation = false;

            if (linkedInstance != null)
            {
                linkedInstance.IsAtWorksite = false;
                linkedInstance.SetState(WorkerState.Moving);
            }

            Debug.Log($"<color=cyan>[WorkerController]</color> {gameObject.name} traveling to {position}");
        }

        /// <summary>
        /// Ferma completamente il worker.
        /// </summary>
        public void StopMovement()
        {
            if (agent == null) return;

            agent.ResetPath();
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.isStopped = false;
            
            isMoving = false;
            isPlayingWorkAnimation = false;
            isPatrollingWorksite = false;
            currentMovementState = MovementState.Idle;
        }

        /// <summary>
        /// Forza il worker a tornare allo stato Idle.
        /// </summary>
        public void ResetToIdle()
        {
            ForceIdle();
        }

        /// <summary>
        /// FORZA IL WORKER IN STATO IDLE COMPLETO.
        /// BRUTALE: Blocca TUTTO - movimento, animazioni, update loop.
        /// Chiamato quando il worker viene disassegnato.
        /// </summary>
        public void ForceIdle()
        {
            // ═══════════════════════════════════════════════════════════
            // 1. BLOCCA L'UPDATE LOOP (CRITICO!)
            // ═══════════════════════════════════════════════════════════
            isForcedIdle = true;
            
            // ═══════════════════════════════════════════════════════════
            // 2. STOP PATTUGLIAMENTO
            // ═══════════════════════════════════════════════════════════
            isPatrollingWorksite = false;
            
            // ═══════════════════════════════════════════════════════════
            // 3. STOP NAVMESH AGENT - BRUTALE
            // ═══════════════════════════════════════════════════════════
            if (agent != null)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero; // FORZA velocità a zero
                agent.isStopped = false;
            }

            // ═══════════════════════════════════════════════════════════
            // 4. RESET STATO INTERNO
            // ═══════════════════════════════════════════════════════════
            currentMovementState = MovementState.Idle;
            isMoving = false;
            isPlayingWorkAnimation = false;
            currentWorkTargetCenter = Vector3.zero;
            structurePosition = Vector3.zero;
            workTimer = 0f;
            currentSpeed = 0f;

            // ═══════════════════════════════════════════════════════════
            // 5. RESET ANIMATOR - INSTANT (ignora transizioni!)
            // ═══════════════════════════════════════════════════════════
            if (animator != null)
            {
                // Forza parametri a zero
                animator.SetFloat("Speed", 0f);
                animator.SetBool("IsWorking", false);
                animator.SetBool("IsMoving", false);
                
                // FORZA lo stato Idle immediatamente (ignora exit time e transizioni)
                animator.Play(idleStateName, 0, 0f);
            }

            Debug.Log($"<color=red>[WorkerController]</color> {gameObject.name} FORCE IDLE - UPDATE BLOCKED");
        }

        // ============================================
        // ANIMATIONS
        // ============================================

        private void UpdateAnimations()
        {
            if (animator == null) return;
            if (isForcedIdle) return; // Non aggiornare se bloccato

            animator.SetFloat("Speed", currentSpeed);

            bool shouldPlayWorkAnim = isPatrollingWorksite && 
                                      currentMovementState == MovementState.WorkingOnSite && 
                                      isPlayingWorkAnimation;
            animator.SetBool("IsWorking", shouldPlayWorkAnim);
            animator.SetBool("IsMoving", isMoving && !shouldPlayWorkAnim);
        }

        // ============================================
        // DEBUG
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug Info")]
        [ShowInInspector, ReadOnly]
        [ProgressBar(0, 5, ColorGetter = "GetSpeedBarColor")]
        private float DebugCurrentSpeed => currentSpeed;

        private Color GetSpeedBarColor(float value)
        {
            return value < workAnimationSpeedThreshold ? Color.green : Color.yellow;
        }

        private void OnDrawGizmosSelected()
        {
            if (agent != null && agent.hasPath)
            {
                Gizmos.color = Color.yellow;
                var path = agent.path;
                for (int i = 0; i < path.corners.Length - 1; i++)
                {
                    Gizmos.DrawLine(path.corners[i], path.corners[i + 1]);
                }
            }

            if (linkedInstance != null)
            {
                Gizmos.color = linkedInstance.IsAtWorksite ? Color.green : Color.red;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.3f);
            }

            if (currentMovementState == MovementState.WorkingOnSite && isPatrollingWorksite)
            {
                Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
                Gizmos.DrawWireSphere(currentWorkTargetCenter, workWanderRadius);
                
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(currentWorkTargetCenter, 0.2f);

                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position + Vector3.up, structurePosition + Vector3.up);
            }

            // Indicatore FORCE IDLE
            if (isForcedIdle)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(transform.position + Vector3.up * 3f, Vector3.one * 0.5f);
            }
        }

        [TitleGroup("Debug")]
        [Button("🎯 Force Work Patrol", ButtonSizes.Medium)]
        private void DebugForceWorkWander()
        {
            if (Application.isPlaying)
            {
                isForcedIdle = false;
                currentWorkTargetCenter = transform.position;
                structurePosition = transform.position + transform.forward * 3f;
                currentMovementState = MovementState.WorkingOnSite;
                isPatrollingWorksite = true;
                workTimer = 0.1f;
                Debug.Log("[WorkerController] Forced work patrol state!");
            }
        }

        [Button("🛑 Force Idle (Brutal)", ButtonSizes.Medium), GUIColor(1f, 0.3f, 0.3f)]
        private void DebugForceIdle()
        {
            if (Application.isPlaying)
            {
                ForceIdle();
            }
        }

        [Button("🔓 Unlock Worker", ButtonSizes.Medium), GUIColor(0.3f, 1f, 0.3f)]
        private void DebugUnlock()
        {
            if (Application.isPlaying)
            {
                isForcedIdle = false;
                Debug.Log("[WorkerController] Worker unlocked!");
            }
        }

        [Button("🔍 Find Animator", ButtonSizes.Medium)]
        private void DebugFindAnimator()
        {
            animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                Debug.Log($"[WorkerController] Found Animator on: {animator.gameObject.name}");
            }
            else
            {
                Debug.LogWarning("[WorkerController] No Animator found in children!");
            }
        }
#endif
    }
}