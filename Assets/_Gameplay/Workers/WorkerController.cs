using UnityEngine;
using UnityEngine.AI;
using Sirenix.OdinInspector;
using WildernessSurvival.Gameplay.Structures;
using WildernessSurvival.Gameplay.Structures.Housing;
using WildernessSurvival.Gameplay.Workers.Housing;

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
    /// Gestisce SOLO movimento, navigazione e work wandering.
    /// La logica visuale è delegata a WorkerVisualController.
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
        [Tooltip("Riferimento al WorkerVisualController (auto-find se null)")]
        private WorkerVisualController visualController;

        [SerializeField, ReadOnly]
        private WorkerDownedStatus downedStatus;

        // ============================================
        // MOVEMENT SETTINGS
        // ============================================

        [BoxGroup("Movement Settings")]
        [SerializeField]
        [PropertyRange(1f, 10f)]
        private float workWanderRadius = 3f;

        [BoxGroup("Movement Settings")]
        [SerializeField]
        [PropertyRange(1f, 5f)]
        private float changeSpotInterval = 2.5f;

        [BoxGroup("Movement Settings")]
        [SerializeField]
        [PropertyRange(1f, 5f)]
        private float navMeshSampleDistance = 2f;

        // ============================================
        // ANIMATION SETTINGS
        // ============================================

        [BoxGroup("Animation Settings")]
        [SerializeField]
        [PropertyRange(0.01f, 0.5f)]
        private float workAnimationSpeedThreshold = 0.1f;

        [BoxGroup("Animation Settings")]
        [SerializeField]
        [PropertyRange(1f, 10f)]
        private float lookAtRotationSpeed = 5f;

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
        private bool isPatrollingWorksite = false;

        [ShowInInspector, ReadOnly]
        private bool isForcedIdle = false;

        [ShowInInspector, ReadOnly]
        private bool isMoving = false;

        [ShowInInspector, ReadOnly]
        private Vector3 targetPosition;

        [ShowInInspector, ReadOnly]
        private bool isPlayingWorkAnimation = false;

        // Work Wandering State
        private Vector3 currentWorkTargetCenter;
        private Vector3 structurePosition;
        private float workTimer;
        private float currentSpeed;

        // ============================================
        // STUCK DETECTION (Mobile-Friendly)
        // ============================================

        private Vector3 stuckCheckLastPosition;
        private float stuckTimer;
        private int stuckRecoveryAttempts;

        [BoxGroup("Stuck Detection")]
        [SerializeField]
        [PropertyRange(1f, 5f)]
        [Tooltip("Seconds without movement before considering stuck")]
        private float stuckThresholdSeconds = 2.5f;

        [BoxGroup("Stuck Detection")]
        [SerializeField]
        [PropertyRange(0.5f, 3f)]
        [Tooltip("Random offset radius for recovery fallback")]
        private float stuckRecoveryOffsetRadius = 1.5f;

        private const float MIN_MOVEMENT_DELTA_SQR = 0.01f; // 0.1m squared
        private const float MIN_VELOCITY_MOVING = 0.1f;     // Velocity > this = agent is moving
        private const int MAX_RECOVERY_ATTEMPTS = 3;

        // ============================================
        // SHELTER / NIGHT RETREAT STATE
        // ============================================

        [ShowInInspector, ReadOnly]
        private bool isGoingToShelter = false;

        [ShowInInspector, ReadOnly]
        private bool isInShelter = false;

        [ShowInInspector, ReadOnly]
        private ShelterHome targetShelter;

        [ShowInInspector, ReadOnly]
        private bool isRetreatingToWaystone = false;

        // Cached visual root for hide/show
        private GameObject visualRootObject;

        // ============================================
        // PROPERTIES
        // ============================================

        public WorkerData Data => workerData;
        public bool IsAlive => linkedInstance?.IsAlive ?? true;
        public bool IsMoving => isMoving;
        public MovementState CurrentMovementState => currentMovementState;
        public WorkerVisualController VisualController => visualController;
        public WorkerDownedStatus DownedStatus => downedStatus;
        public bool IsInShelter => isInShelter;
        public bool IsGoingToShelter => isGoingToShelter;
        public bool IsRetreatingToWaystone => isRetreatingToWaystone;

        /// <summary>
        /// [NEW] Warps the worker to a specific position, handling both NavMesh and Transform.
        /// </summary>
        public void Warp(Vector3 position)
        {
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.Warp(position);
            }
            else
            {
                transform.position = position;
            }
        }

        /// <summary>
        /// [NEW] Enables or disables the NavMesh agent safely.
        /// </summary>
        public void SetNavMeshActive(bool active)
        {
            if (agent != null)
            {
                agent.enabled = active;
            }
        }

        /// <summary>
        /// Verifica se il worker è in transizione visiva.
        /// </summary>
        public bool IsChangingOutfit => visualController != null && visualController.IsTransitioning;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();

            // Auto-find WorkerDownedStatus
            downedStatus = GetComponent<WorkerDownedStatus>();

            // Auto-find WorkerVisualController
            if (visualController == null)
            {
                visualController = GetComponent<WorkerVisualController>();
                if (visualController == null)
                {
                    visualController = GetComponentInChildren<WorkerVisualController>();
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

        /// <summary>
        /// Collega questo controller a un WorkerInstance.
        /// Inizializza anche il WorkerVisualController.
        /// </summary>
        public void LinkToInstance(WorkerInstance instance)
        {
            linkedInstance = instance;

            // Initialize the visual controller
            if (visualController != null)
            {
                visualController.Initialize(instance);
            }

            // Wire up the damageable bridge (for enemy targeting)
            var damageable = GetComponent<WorkerDamageable>();
            if (damageable != null)
            {
                damageable.LinkToInstance(instance);
            }

#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[WorkerController]</color> Linked to instance: {instance?.CustomName}");
#endif
        }

        // ============================================
        // UPDATE (chiamato da WorkerSystem)
        // ============================================

        public void ManualUpdate(float deltaTime)
        {
            // Skip se downed (authoritative block)
            if (downedStatus != null && downedStatus.IsDowned) return;

            // Skip se in transizione visiva
            if (IsChangingOutfit) return;

            if (isForcedIdle)
            {
                UpdateAnimations(0f, false, false);
                return;
            }

            if (agent == null || linkedInstance == null) return;

            currentSpeed = agent.velocity.magnitude;

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

            // Aggiorna animazioni tramite VisualController
            bool shouldPlayWorkAnim = isPatrollingWorksite &&
                                      currentMovementState == MovementState.WorkingOnSite &&
                                      isPlayingWorkAnimation;
            UpdateAnimations(currentSpeed, isMoving && !shouldPlayWorkAnim, shouldPlayWorkAnim);
        }

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

            // Stuck detection check while traveling
            CheckIfStuck(deltaTime);
        }

        private void UpdateWorkingOnSiteState(float deltaTime)
        {
            if (agent == null) return;

            if (!isPatrollingWorksite)
            {
                currentMovementState = MovementState.Idle;
                return;
            }

            // [MODIFY] Mobile optimization: only play work anim/rotate if nearly stationary
            bool isStationaryEnough = currentSpeed < workAnimationSpeedThreshold;
            isPlayingWorkAnimation = isStationaryEnough;

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

        private void RotateTowardsStructure(float deltaTime)
        {
            Vector3 direction = structurePosition - transform.position;
            direction.y = 0;
            if (direction.sqrMagnitude < 0.01f) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lookAtRotationSpeed * deltaTime);
        }

        // ============================================
        // ARRIVAL LOGIC
        // ============================================

        private void OnArrivedAtDestination()
        {
            // Check if this is a shelter/retreat arrival first
            if (isGoingToShelter || isRetreatingToWaystone)
            {
                OnArrivedAtRetreat();
                return;
            }

            // Reset stuck detection on successful arrival
            ResetStuckDetection();

            currentMovementState = MovementState.WorkingOnSite;
            currentWorkTargetCenter = transform.position;

            if (linkedInstance?.AssignedStructure != null)
            {
                structurePosition = linkedInstance.AssignedStructure.transform.position;
            }
            else
            {
                structurePosition = targetPosition;
            }

            isPatrollingWorksite = true;
            workTimer = 0.5f;


            if (linkedInstance != null && linkedInstance.IsAssigned && !linkedInstance.IsAtWorksite)
            {
                linkedInstance.IsAtWorksite = true;
                linkedInstance.SetState(WorkerState.Working);

                // Attiva il pending job ora che il worker è arrivato (cambio visual)
                if (linkedInstance.PendingJob != null)
                {
                    linkedInstance.SetJob(linkedInstance.PendingJob);
                    linkedInstance.PendingJob = null;
                }

                // [MODIFY] Trigger event-driven recalculation (mobile optimization)
                linkedInstance.AssignedStructure?.OnWorkerArrivedAtSite();

#if UNITY_EDITOR
                Debug.Log($"<color=green>[WorkerController]</color> {linkedInstance.CustomName} arrived at worksite!");
#endif
            }
        }

        private void MoveToRandomWorkPoint()
        {
            if (!isPatrollingWorksite || isForcedIdle || IsChangingOutfit) return;

            // Gate: block if downed
            if (downedStatus != null && downedStatus.IsDowned) return;

            Vector2 randomCircle = Random.insideUnitCircle * workWanderRadius;
            Vector3 randomPoint = currentWorkTargetCenter + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas))
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

        public void CommandMoveTo(Vector3 position)
        {
            if (agent == null) return;

            // Gate: block movement orders when downed
            if (downedStatus != null && downedStatus.IsDowned)
            {
#if UNITY_EDITOR
                Debug.Log($"<color=orange>[WorkerController]</color> {gameObject.name} blocked CommandMoveTo: worker is DOWNED");
#endif
                return;
            }

            // ════════════════════════════════════════════════════════════════════
            // [GHOST FIX] Reset shelter/retreat flags & restore visuals
            // This prevents "invisible worker" bugs and ensures proper state reset
            // ════════════════════════════════════════════════════════════════════
            bool wasSheltered = isInShelter;
            isGoingToShelter = false;
            isInShelter = false;
            isRetreatingToWaystone = false;
            targetShelter = null;

            if (wasSheltered)
            {
                ShowVisual();
            }

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

#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[WorkerController]</color> {gameObject.name} traveling to {position}");
#endif
        }

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

        public void ResetToIdle()
        {
            ForceIdle();
        }

        /// <summary>
        /// Forza idle completo.
        /// </summary>
        public void ForceIdle()
        {
            ForceIdleInternal();
        }

        /// <summary>
        /// Forza idle (compatibilità con vecchio sistema).
        /// </summary>
        public void ForceIdleKeepPendingVisual()
        {
            ForceIdleInternal();
        }

        private void ForceIdleInternal()
        {
            isForcedIdle = true;
            isPatrollingWorksite = false;

            if (agent != null)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }

            currentMovementState = MovementState.Idle;
            isMoving = false;
            isPlayingWorkAnimation = false;
            currentWorkTargetCenter = Vector3.zero;
            structurePosition = Vector3.zero;
            workTimer = 0f;
            currentSpeed = 0f;

            // Forza idle animation tramite visual controller
            if (visualController != null)
            {
                visualController.ForceIdleAnimation();
            }

            // Notify Foreman: worker is now idle and available (event-driven)
            if (linkedInstance != null && !linkedInstance.IsAssigned && WorkerSystem.Instance != null)
            {
                WorkerSystem.Instance.NotifyWorkerBecameIdleBuilder(linkedInstance);
            }
        }

        /// <summary>
        /// Sblocca il worker dallo stato idle forzato.
        /// </summary>
        public void Unlock()
        {
            isForcedIdle = false;
            if (agent != null) agent.isStopped = false;
        }

        // ============================================
        // STUCK DETECTION & RECOVERY
        // ============================================

        /// <summary>
        /// Checks if worker is stuck (not moving while in Traveling state).
        /// Uses velocity check first (authoritative), then position delta as fallback.
        /// Zero allocations per frame.
        /// </summary>
        private void CheckIfStuck(float deltaTime)
        {
            // Skip if not actively traveling
            if (currentMovementState != MovementState.Traveling) return;
            if (agent == null || !agent.hasPath) return;

            // ════════════════════════════════════════════════════════════════════
            // PRIMARY CHECK: NavMesh velocity (authoritative)
            // If agent has velocity above threshold, it IS moving - never stuck
            // ════════════════════════════════════════════════════════════════════
            float velocityMag = agent.velocity.magnitude;
            if (velocityMag > MIN_VELOCITY_MOVING)
            {
                // Agent is definitely moving - reset all stuck state
                stuckTimer = 0f;
                stuckRecoveryAttempts = 0;
                stuckCheckLastPosition = transform.position;
                return;
            }

            // ════════════════════════════════════════════════════════════════════
            // SECONDARY CHECK: Position delta (for edge cases where velocity == 0
            // but agent moved via other means, e.g., Warp)
            // ════════════════════════════════════════════════════════════════════
            Vector3 currentPos = transform.position;
            float deltaSqr = (currentPos - stuckCheckLastPosition).sqrMagnitude;

            if (deltaSqr >= MIN_MOVEMENT_DELTA_SQR)
            {
                // Position changed - reset timer
                stuckTimer = 0f;
                stuckRecoveryAttempts = 0;
            }
            else
            {
                // Both velocity is near-zero AND position hasn't changed
                // This is a genuine stuck situation - accumulate timer
                stuckTimer += deltaTime;

                if (stuckTimer >= stuckThresholdSeconds)
                {
                    RecoverFromStuck();
                }
            }

            stuckCheckLastPosition = currentPos;
        }

        /// <summary>
        /// Attempts recovery when worker is stuck.
        /// Strategy: 1) Re-path, 2) Request alternate slot, 3) Random offset.
        /// </summary>
        private void RecoverFromStuck()
        {
            stuckTimer = 0f;
            stuckRecoveryAttempts++;

            if (stuckRecoveryAttempts > MAX_RECOVERY_ATTEMPTS)
            {
                // Give up - force idle to prevent infinite loop
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"<color=red>[StuckRecovery]</color> {gameObject.name} exceeded max recovery attempts, forcing idle");
#endif
                ForceIdle();
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"<color=orange>[StuckRecovery]</color> {gameObject.name} stuck detected, attempt {stuckRecoveryAttempts}/{MAX_RECOVERY_ATTEMPTS}");
#endif

            // Attempt 1 & 2: Re-path to target (may work if obstruction cleared)
            if (stuckRecoveryAttempts <= 2)
            {
                // ════════════════════════════════════════════════════════════════════
                // [GHOST FIX] If worker is retreating to shelter, do NOT redirect to worksite
                // Just re-path to current target (shelter position)
                // ════════════════════════════════════════════════════════════════════
                if (isGoingToShelter || isRetreatingToWaystone)
                {
                    agent.SetDestination(targetPosition);
                    return;
                }

                // Request new slot from structure if assigned
                if (linkedInstance?.AssignedStructure != null)
                {
                    Vector3 newTarget = linkedInstance.AssignedStructure.GetWorkPositionForWorker(linkedInstance);
                    agent.SetDestination(newTarget);
                    targetPosition = newTarget;
                    return;
                }

                // Otherwise just re-path to current target
                agent.SetDestination(targetPosition);
                return;
            }

            // Attempt 3: Random NavMesh offset as fallback
            Vector2 randomCircle = Random.insideUnitCircle * stuckRecoveryOffsetRadius;
            Vector3 offsetPos = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (NavMesh.SamplePosition(offsetPos, out NavMeshHit hit, stuckRecoveryOffsetRadius, NavMesh.AllAreas))
            {
                // Move to safe offset first, then re-path to target
                agent.SetDestination(hit.position);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"<color=yellow>[StuckRecovery]</color> {gameObject.name} applying random offset to {hit.position}");
#endif
            }
        }

        /// <summary>
        /// Resets stuck detection state.
        /// Called when worker successfully arrives or changes state.
        /// </summary>
        private void ResetStuckDetection()
        {
            stuckTimer = 0f;
            stuckRecoveryAttempts = 0;
            stuckCheckLastPosition = transform.position;
        }

        // ============================================
        // ANIMATIONS (delegate a VisualController)
        // ============================================

        private void UpdateAnimations(float speed, bool isMovingAnim, bool isWorkingAnim)
        {
            if (visualController != null)
            {
                visualController.UpdateAnimator(speed, isMovingAnim, isWorkingAnim);
            }
        }

        // ============================================
        // SHELTER / NIGHT RETREAT COMMANDS
        // ============================================

        /// <summary>
        /// Commands the worker to move to a shelter for night retreat.
        /// When worker arrives, they will enter the shelter and become hidden/non-targetable.
        /// </summary>
        public void CommandMoveToShelter(Vector3 shelterPosition, ShelterHome shelter)
        {
            if (agent == null) return;

            // Gate: block if downed
            if (downedStatus != null && downedStatus.IsDowned)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"<color=orange>[WorkerController]</color> {gameObject.name} blocked CommandMoveToShelter: worker is DOWNED");
#endif
                return;
            }

            // Reset shelter-related states
            isGoingToShelter = true;
            isInShelter = false;
            isRetreatingToWaystone = false;
            targetShelter = shelter;

            // Clear work-related states
            isForcedIdle = false;
            isPatrollingWorksite = false;
            currentMovementState = MovementState.Traveling;
            targetPosition = shelterPosition;

            // Start navigation
            agent.isStopped = false;
            agent.SetDestination(shelterPosition);
            isMoving = true;
            isPlayingWorkAnimation = false;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"<color=cyan>[WorkerController]</color> {gameObject.name} moving to shelter at {shelterPosition}");
#endif
        }

        /// <summary>
        /// Commands the worker to retreat to Waystone area (for homeless workers at night).
        /// Worker will remain visible but be in Retreating state.
        /// </summary>
        public void CommandMoveToRetreat(Vector3 waystonePosition)
        {
            if (agent == null) return;

            // Gate: block if downed
            if (downedStatus != null && downedStatus.IsDowned)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"<color=orange>[WorkerController]</color> {gameObject.name} blocked CommandMoveToRetreat: worker is DOWNED");
#endif
                return;
            }

            // Reset shelter-related states
            isGoingToShelter = false;
            isInShelter = false;
            isRetreatingToWaystone = true;
            targetShelter = null;

            // Clear work-related states
            isForcedIdle = false;
            isPatrollingWorksite = false;
            currentMovementState = MovementState.Traveling;
            targetPosition = waystonePosition;

            // Start navigation
            agent.isStopped = false;
            agent.SetDestination(waystonePosition);
            isMoving = true;
            isPlayingWorkAnimation = false;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"<color=yellow>[WorkerController]</color> {gameObject.name} retreating to Waystone at {waystonePosition}");
#endif
        }

        /// <summary>
        /// Called when worker physically enters shelter.
        /// Hides the visual representation and stops all movement.
        /// </summary>
        public void EnterShelter()
        {
            isGoingToShelter = false;
            isInShelter = true;
            isRetreatingToWaystone = false;

            // Stop all movement
            if (agent != null)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }

            currentMovementState = MovementState.Idle;
            isMoving = false;
            isPatrollingWorksite = false;
            isPlayingWorkAnimation = false;

            // Hide visual
            HideVisual();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"<color=green>[WorkerController]</color> {gameObject.name} entered shelter - hidden");
#endif
        }

        /// <summary>
        /// Called when worker exits shelter (day start or shelter destroyed).
        /// Restores visual representation and positions worker at exit point.
        /// </summary>
        public void ExitShelter(Vector3 exitPosition)
        {
            isGoingToShelter = false;
            isInShelter = false;
            isRetreatingToWaystone = false;
            targetShelter = null;

            // Teleport to exit position
            Warp(exitPosition);

            if (agent != null)
            {
                agent.isStopped = false;
            }

            currentMovementState = MovementState.Idle;
            isForcedIdle = false;

            // Show visual
            ShowVisual();

            // Restore job from instance
            if (linkedInstance != null)
            {
                linkedInstance.RestoreJobAfterRetreat();
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"<color=yellow>[WorkerController]</color> {gameObject.name} exited shelter at {exitPosition}");
#endif
        }

        /// <summary>
        /// Hides the worker's visual representation (for shelter entry).
        /// </summary>
        private void HideVisual()
        {
            bool rootHidden = false;

            // Cache and hide visual root if available
            if (visualController != null)
            {
                visualRootObject = visualController.gameObject;
                // Only deactive if it's NOT this object (to keep this script running)
                if (visualRootObject != null && visualRootObject != gameObject)
                {
                    visualRootObject.SetActive(false);
                    rootHidden = true;
                }
            }

            // ALWAYS disable all renderers in children as a reliable fail-safe
            // This handles cases where visualRootObject == gameObject
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                r.enabled = false;
            }

            // Disable colliders so enemies can't target
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                // Never disable our own collider if it's the main one, but usually it's in children
                col.enabled = false;
            }
        }

        /// <summary>
        /// Shows the worker's visual representation (for shelter exit).
        /// </summary>
        private void ShowVisual()
        {
            // Show visual root if cached
            if (visualRootObject != null && visualRootObject != gameObject)
            {
                visualRootObject.SetActive(true);
            }

            // Re-enable all renderers (matching the HideVisual fail-safe)
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                r.enabled = true;
            }

            // Re-enable colliders
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = true;
            }
        }

        /// <summary>
        /// Called when worker arrives at retreat destination (shelter or waystone).
        /// Notifies appropriate system.
        /// </summary>
        private void OnArrivedAtRetreat()
        {
            if (isGoingToShelter && targetShelter != null)
            {
                // Arrived at shelter - notify system to enter
                if (WorkerNightRetreatSystem.Instance != null && linkedInstance != null)
                {
                    WorkerNightRetreatSystem.Instance.NotifyWorkerArrivedAtShelter(linkedInstance, targetShelter);
                }
            }
            else if (isRetreatingToWaystone)
            {
                // Arrived at waystone - just idle in place
                isRetreatingToWaystone = false;
                currentMovementState = MovementState.Idle;

                if (linkedInstance != null)
                {
                    linkedInstance.SetState(WorkerState.Idle);
                }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"<color=yellow>[WorkerController]</color> {gameObject.name} arrived at Waystone retreat spot");
#endif
            }
        }

        // ============================================
        // DEBUG
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug Info")]
        [ShowInInspector, ReadOnly]
        private float DebugCurrentSpeed => currentSpeed;

        [ShowInInspector, ReadOnly]
        private string DebugLinkedInstance => linkedInstance?.CustomName ?? "None";

        [TitleGroup("Debug")]
        [Button("Force Work Patrol", ButtonSizes.Medium)]
        private void DebugForceWorkWander()
        {
            if (!Application.isPlaying) return;
            isForcedIdle = false;
            if (agent != null) agent.isStopped = false;
            currentWorkTargetCenter = transform.position;
            structurePosition = transform.position + transform.forward * 3f;
            currentMovementState = MovementState.WorkingOnSite;
            isPatrollingWorksite = true;
            workTimer = 0.1f;
        }

        [Button("Force Idle", ButtonSizes.Medium), GUIColor(1f, 0.3f, 0.3f)]
        private void DebugForceIdle()
        {
            if (Application.isPlaying) ForceIdle();
        }

        [Button("Unlock Worker", ButtonSizes.Medium), GUIColor(0.3f, 1f, 0.3f)]
        private void DebugUnlock()
        {
            if (!Application.isPlaying) return;
            Unlock();
        }

        private void OnDrawGizmosSelected()
        {
            if (currentMovementState == MovementState.WorkingOnSite && isPatrollingWorksite)
            {
                Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
                Gizmos.DrawWireSphere(currentWorkTargetCenter, workWanderRadius);
            }

            if (isForcedIdle)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(transform.position + Vector3.up * 3f, Vector3.one * 0.5f);
            }

            if (IsChangingOutfit)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(transform.position + Vector3.up * 2.5f, Vector3.one * 0.4f);
            }
        }
#endif
    }
}
