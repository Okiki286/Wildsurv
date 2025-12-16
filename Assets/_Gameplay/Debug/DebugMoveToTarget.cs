using UnityEngine;
using UnityEngine.AI;
using Sirenix.OdinInspector;

namespace WildernessSurvival.Gameplay.DevTools
{
    /// <summary>
    /// Debug script per muovere automaticamente un oggetto verso un target.
    /// Utile per testare trigger enter/exit (es. WaystoneDebuffAura).
    /// Supporta NavMeshAgent se disponibile, altrimenti usa MoveTowards.
    /// </summary>
    public class DebugMoveToTarget : MonoBehaviour
    {
        // ============================================
        // SETTINGS
        // ============================================

        [TitleGroup("Target")]
        [Tooltip("Transform del target (es. Waystone). Se nullo, cerca automaticamente.")]
        [SerializeField] private Transform target;

        [TitleGroup("Movement")]
        [Tooltip("Velocità di movimento")]
        [Range(0.5f, 20f)]
        [SerializeField] private float moveSpeed = 2.0f;

        [TitleGroup("Movement")]
        [Tooltip("Distanza a cui fermarsi dal target")]
        [Range(0.1f, 10f)]
        [SerializeField] private float stopDistance = 1.0f;

        [TitleGroup("Movement")]
        [Tooltip("Ruota verso la direzione di movimento")]
        [SerializeField] private bool faceTarget = true;

        [TitleGroup("Movement")]
        [Tooltip("Velocità di rotazione (gradi/sec)")]
        [Range(1f, 720f)]
        [SerializeField] private float rotationSpeed = 360f;

        [TitleGroup("Loop")]
        [Tooltip("Ripete il percorso: target -> start -> target...")]
        [SerializeField] private bool loop = true;

        [TitleGroup("Loop")]
        [Tooltip("Tempo di attesa al target prima di tornare")]
        [Range(0f, 10f)]
        [SerializeField] private float waitAtTarget = 1.0f;

        [TitleGroup("Loop")]
        [Tooltip("Tempo di attesa allo start prima di ripartire")]
        [Range(0f, 10f)]
        [SerializeField] private float waitAtStart = 1.0f;

        [TitleGroup("NavMesh")]
        [Tooltip("Usa NavMeshAgent se presente sul GameObject")]
        [SerializeField] private bool useNavMeshIfAvailable = true;

        [TitleGroup("Debug")]
        [SerializeField] private bool debugLogs = true;

        // ============================================
        // RUNTIME STATE
        // ============================================

        private Vector3 startPosition;
        private Quaternion startRotation;
        private NavMeshAgent navAgent;
        private bool useNavMesh = false;

        private enum MoveState { MovingToTarget, WaitingAtTarget, MovingToStart, WaitingAtStart, Stopped }
        private MoveState currentState = MoveState.MovingToTarget;
        private float waitTimer = 0f;

        private bool hasLoggedNullTarget = false;

        // ============================================
        // PROPERTIES
        // ============================================

        [TitleGroup("Runtime Status")]
        [ShowInInspector]
        [ReadOnly]
        public string CurrentStateName => currentState.ToString();

        [ShowInInspector]
        [ReadOnly]
        public float DistanceToTarget => target != null ? Vector3.Distance(transform.position, target.position) : -1f;

        [ShowInInspector]
        [ReadOnly]
        public float DistanceToStart => Vector3.Distance(transform.position, startPosition);

        /// <summary>
        /// Permette di assegnare il target da altri script (es. DebugWaystoneTargetFinder).
        /// </summary>
        public Transform Target
        {
            get => target;
            set
            {
                target = value;
                hasLoggedNullTarget = false;
                if (target != null && debugLogs)
                {
                    Log($"Target assigned: {target.name}");
                }
            }
        }

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Start()
        {
            startPosition = transform.position;
            startRotation = transform.rotation;

            navAgent = GetComponent<NavMeshAgent>();
            useNavMesh = useNavMeshIfAvailable && navAgent != null && navAgent.isOnNavMesh;

            if (useNavMesh)
            {
                navAgent.speed = moveSpeed;
                navAgent.stoppingDistance = stopDistance;
                if (debugLogs)
                {
                    Log($"Using NavMeshAgent (speed: {moveSpeed}, stopDist: {stopDistance})");
                }
            }
            else if (debugLogs)
            {
                Log("Using transform.MoveTowards (no NavMesh)");
            }

            if (target == null)
            {
                LogWarning("No target assigned! Waiting for DebugWaystoneTargetFinder or manual assignment.");
            }
            else if (debugLogs)
            {
                Log($"Target: {target.name} at {target.position}");
            }

            currentState = MoveState.MovingToTarget;
        }

        private void Update()
        {
            if (target == null)
            {
                if (!hasLoggedNullTarget)
                {
                    LogWarning("Target is null! Movement disabled.");
                    hasLoggedNullTarget = true;
                }
                StopMovement();
                return;
            }

            switch (currentState)
            {
                case MoveState.MovingToTarget:
                    MoveTowards(target.position);
                    if (HasReachedDestination(target.position))
                    {
                        if (debugLogs) Log($"Reached TARGET ({target.name})");
                        if (loop)
                        {
                            currentState = MoveState.WaitingAtTarget;
                            waitTimer = waitAtTarget;
                        }
                        else
                        {
                            currentState = MoveState.Stopped;
                            StopMovement();
                        }
                    }
                    break;

                case MoveState.WaitingAtTarget:
                    StopMovement();
                    waitTimer -= Time.deltaTime;
                    if (waitTimer <= 0f)
                    {
                        if (debugLogs) Log("Returning to START...");
                        currentState = MoveState.MovingToStart;
                    }
                    break;

                case MoveState.MovingToStart:
                    MoveTowards(startPosition);
                    if (HasReachedDestination(startPosition))
                    {
                        if (debugLogs) Log("Reached START");
                        currentState = MoveState.WaitingAtStart;
                        waitTimer = waitAtStart;
                    }
                    break;

                case MoveState.WaitingAtStart:
                    StopMovement();
                    waitTimer -= Time.deltaTime;
                    if (waitTimer <= 0f)
                    {
                        if (debugLogs) Log($"Moving to TARGET ({target.name})...");
                        currentState = MoveState.MovingToTarget;
                    }
                    break;

                case MoveState.Stopped:
                    break;
            }
        }

        // ============================================
        // MOVEMENT
        // ============================================

        private void MoveTowards(Vector3 destination)
        {
            if (useNavMesh && navAgent != null && navAgent.isOnNavMesh)
            {
                navAgent.isStopped = false;
                navAgent.SetDestination(destination);
            }
            else
            {
                Vector3 direction = (destination - transform.position).normalized;
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    destination,
                    moveSpeed * Time.deltaTime
                );

                if (faceTarget && direction.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation,
                        targetRotation,
                        rotationSpeed * Time.deltaTime
                    );
                }
            }
        }

        private void StopMovement()
        {
            if (useNavMesh && navAgent != null && navAgent.isOnNavMesh)
            {
                navAgent.isStopped = true;
                navAgent.ResetPath();
            }
        }

        private bool HasReachedDestination(Vector3 destination)
        {
            float distance = Vector3.Distance(transform.position, destination);

            if (useNavMesh && navAgent != null)
            {
                return !navAgent.pathPending &&
                       navAgent.remainingDistance <= stopDistance;
            }

            return distance <= stopDistance;
        }

        // ============================================
        // LOGGING
        // ============================================

        private void Log(string message)
        {
            UnityEngine.Debug.Log($"<color=cyan>[DebugMove]</color> {gameObject.name}: {message}");
        }

        private void LogWarning(string message)
        {
            UnityEngine.Debug.LogWarning($"<color=orange>[DebugMove]</color> {gameObject.name}: {message}");
        }

        // ============================================
        // DEBUG ACTIONS
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug Actions")]
        [Button("Reset to Start Position", ButtonSizes.Medium)]
        [GUIColor(0.4f, 0.8f, 1f)]
        private void DebugResetToStart()
        {
            if (Application.isPlaying)
            {
                transform.position = startPosition;
                transform.rotation = startRotation;
                currentState = MoveState.MovingToTarget;
                StopMovement();
                Log("Reset to start position");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[DebugMove] Only works in Play Mode");
            }
        }

        [Button("Teleport to Target", ButtonSizes.Medium)]
        [GUIColor(1f, 0.8f, 0.4f)]
        private void DebugTeleportToTarget()
        {
            if (target != null)
            {
                Vector3 offset = (transform.position - target.position).normalized * stopDistance;
                transform.position = target.position + offset;
                Log("Teleported near target");
            }
        }

        [Button("Toggle Loop", ButtonSizes.Medium)]
        private void DebugToggleLoop()
        {
            loop = !loop;
            Log($"Loop: {loop}");
        }

        private void OnDrawGizmosSelected()
        {
            if (target != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, target.position);
                Gizmos.DrawWireSphere(target.position, stopDistance);
            }

            Vector3 start = Application.isPlaying ? startPosition : transform.position;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(start, 0.5f);
        }
#endif
    }
}
