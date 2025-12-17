using UnityEngine;
using UnityEngine.AI;
using Sirenix.OdinInspector;

namespace WildernessSurvival.Gameplay.DebugTools
{
    /// <summary>
    /// Debug movement script for testing enemy movement towards a target.
    /// Attach to root of any GameObject (e.g., test Capsule prefab).
    /// Supports both NavMesh-based and simple transform-based movement.
    /// </summary>
    public class DebugMoveToTarget : MonoBehaviour
    {
        // ============================================
        // INSPECTOR FIELDS
        // ============================================

        [TitleGroup("Target")]
        [Tooltip("Target to move towards. If null, script disables itself.")]
        [SerializeField]
        public Transform target;

        [TitleGroup("Movement Settings")]
        [Tooltip("Movement speed in units/second")]
        [SerializeField]
        [Range(0.5f, 10f)]
        public float moveSpeed = 2.0f;

        [TitleGroup("Movement Settings")]
        [Tooltip("Distance at which target is considered reached")]
        [SerializeField]
        [Range(0.1f, 5f)]
        public float stopDistance = 1.0f;

        [TitleGroup("Movement Settings")]
        [Tooltip("Rotate to face target while moving")]
        [SerializeField]
        public bool faceTarget = true;

        [TitleGroup("Loop Settings")]
        [Tooltip("Loop between start position and target")]
        [SerializeField]
        public bool loop = true;

        [TitleGroup("Loop Settings")]
        [Tooltip("Wait time at target before returning")]
        [SerializeField]
        [Range(0f, 10f)]
        public float waitAtTarget = 1.0f;

        [TitleGroup("Loop Settings")]
        [Tooltip("Wait time at start before going to target again")]
        [SerializeField]
        [Range(0f, 10f)]
        public float waitAtStart = 1.0f;

        [TitleGroup("NavMesh")]
        [Tooltip("Use NavMeshAgent if available")]
        [SerializeField]
        public bool useNavMeshIfAvailable = true;

        // ============================================
        // RUNTIME STATE
        // ============================================

        private Vector3 startPosition;
        private NavMeshAgent agent;
        private bool hasLoggedNullTarget = false;
        private bool isReturningToStart = false;
        private float waitTimer = 0f;
        private bool isWaiting = false;

        [TitleGroup("Debug Status")]
        [ShowInInspector, ReadOnly]
        private string CurrentState => GetCurrentStateString();

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Start()
        {
            startPosition = transform.position;

            // Try to get NavMeshAgent
            agent = GetComponent<NavMeshAgent>();

            if (agent != null && useNavMeshIfAvailable)
            {
                agent.speed = moveSpeed;
                agent.stoppingDistance = stopDistance;
                UnityEngine.Debug.Log($"<color=cyan>[DebugMove]</color> {gameObject.name} using NavMeshAgent (speed={moveSpeed}, stop={stopDistance})");
            }
            else
            {
                UnityEngine.Debug.Log($"<color=cyan>[DebugMove]</color> {gameObject.name} using transform-based movement");
            }

            // Check target
            if (target != null)
            {
                UnityEngine.Debug.Log($"<color=cyan>[DebugMove]</color> {gameObject.name} target assigned: {target.name}");
            }
            else
            {
                LogNullTargetOnce();
            }
        }

        private void Update()
        {
            // Null-safe: disable if no target
            if (target == null)
            {
                LogNullTargetOnce();
                return;
            }

            // If waiting, count down
            if (isWaiting)
            {
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0f)
                {
                    isWaiting = false;
                }
                return;
            }

            // Determine current destination
            Vector3 destination = isReturningToStart ? startPosition : target.position;

            // Check if reached destination
            float distance = Vector3.Distance(transform.position, destination);
            if (distance <= stopDistance)
            {
                OnReachedDestination();
                return;
            }

            // Move towards destination
            if (agent != null && useNavMeshIfAvailable && agent.isOnNavMesh)
            {
                MoveWithNavMesh(destination);
            }
            else
            {
                MoveWithTransform(destination);
            }
        }

        // ============================================
        // MOVEMENT
        // ============================================

        private void MoveWithNavMesh(Vector3 destination)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = stopDistance;

            if (!agent.hasPath || agent.destination != destination)
            {
                agent.SetDestination(destination);
            }

            // Face direction of movement
            if (faceTarget && agent.velocity.sqrMagnitude > 0.01f)
            {
                Vector3 direction = agent.velocity.normalized;
                direction.y = 0;
                if (direction.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        Quaternion.LookRotation(direction),
                        Time.deltaTime * 5f
                    );
                }
            }
        }

        private void MoveWithTransform(Vector3 destination)
        {
            Vector3 direction = (destination - transform.position).normalized;
            direction.y = 0;

            // Move
            transform.position = Vector3.MoveTowards(
                transform.position,
                destination,
                moveSpeed * Time.deltaTime
            );

            // Face target
            if (faceTarget && direction.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(direction),
                    Time.deltaTime * 5f
                );
            }
        }

        // ============================================
        // ARRIVAL LOGIC
        // ============================================

        private void OnReachedDestination()
        {
            if (isReturningToStart)
            {
                UnityEngine.Debug.Log($"<color=cyan>[DebugMove]</color> {gameObject.name} returned to START");

                if (loop)
                {
                    isReturningToStart = false;
                    isWaiting = true;
                    waitTimer = waitAtStart;

                    // Stop NavMesh movement during wait
                    if (agent != null && agent.isOnNavMesh)
                    {
                        agent.ResetPath();
                    }
                }
            }
            else
            {
                UnityEngine.Debug.Log($"<color=cyan>[DebugMove]</color> {gameObject.name} reached TARGET");

                if (loop)
                {
                    isReturningToStart = true;
                    isWaiting = true;
                    waitTimer = waitAtTarget;

                    // Stop NavMesh movement during wait
                    if (agent != null && agent.isOnNavMesh)
                    {
                        agent.ResetPath();
                    }
                }
            }
        }

        // ============================================
        // HELPERS
        // ============================================

        private void LogNullTargetOnce()
        {
            if (!hasLoggedNullTarget)
            {
                hasLoggedNullTarget = true;
                UnityEngine.Debug.LogWarning($"<color=orange>[DebugMove]</color> {gameObject.name} has NO TARGET assigned! Movement disabled.");
            }
        }

        private string GetCurrentStateString()
        {
            if (target == null) return "No Target";
            if (isWaiting) return isReturningToStart ? "Waiting at Target" : "Waiting at Start";
            return isReturningToStart ? "Returning to Start" : "Moving to Target";
        }

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Set target at runtime.
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            hasLoggedNullTarget = false;

            if (newTarget != null)
            {
                UnityEngine.Debug.Log($"<color=cyan>[DebugMove]</color> {gameObject.name} target set to: {newTarget.name}");
            }
        }

        /// <summary>
        /// Reset to start position.
        /// </summary>
        public void ResetToStart()
        {
            transform.position = startPosition;
            isReturningToStart = false;
            isWaiting = false;

            if (agent != null && agent.isOnNavMesh)
            {
                agent.Warp(startPosition);
                agent.ResetPath();
            }

            UnityEngine.Debug.Log($"<color=cyan>[DebugMove]</color> {gameObject.name} reset to start position");
        }

        // ============================================
        // DEBUG
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug Actions")]
        [Button("Reset to Start", ButtonSizes.Medium)]
        [GUIColor(0.4f, 0.8f, 1f)]
        private void DebugResetToStart()
        {
            if (Application.isPlaying) ResetToStart();
        }

        [Button("Log Status", ButtonSizes.Medium)]
        private void DebugLogStatus()
        {
            UnityEngine.Debug.Log($"[DebugMove] {gameObject.name}\n" +
                $"  Target: {(target != null ? target.name : "NULL")}\n" +
                $"  State: {GetCurrentStateString()}\n" +
                $"  Distance to target: {(target != null ? Vector3.Distance(transform.position, target.position).ToString("F2") : "N/A")}\n" +
                $"  Using NavMesh: {(agent != null && useNavMeshIfAvailable)}");
        }

        private void OnDrawGizmosSelected()
        {
            // Draw line to target
            if (target != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, target.position);
                Gizmos.DrawWireSphere(target.position, stopDistance);
            }

            // Draw start position
            Vector3 start = Application.isPlaying ? startPosition : transform.position;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(start, 0.5f);
        }
#endif
    }
}
