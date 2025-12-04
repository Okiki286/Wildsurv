using UnityEngine;
using UnityEngine.AI;
using Sirenix.OdinInspector;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// Controller fisico per i worker nella scena.
    /// Gestisce movimento, animazioni e interazioni.
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
        private Animator animator;

        // ============================================
        // LINKED INSTANCE
        // ============================================

        private WorkerInstance linkedInstance;

        // ============================================
        // RUNTIME STATE
        // ============================================

        [TitleGroup("Runtime")]
        [ShowInInspector, ReadOnly]
        private bool isMoving = false;

        [ShowInInspector, ReadOnly]
        private Vector3 targetPosition;

        // ============================================
        // PROPERTIES
        // ============================================

        public WorkerData Data => workerData;
        public bool IsAlive => linkedInstance?.IsAlive ?? true;
        public bool IsMoving => isMoving;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
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
            if (agent == null || linkedInstance == null) return;

            UpdateMovementState();
            UpdateAnimations();
        }

        private void UpdateMovementState()
        {
            if (agent == null || linkedInstance == null) return;

            bool wasMoving = isMoving;
            isMoving = agent.velocity.sqrMagnitude > 0.01f;

            if (isMoving)
            {
                linkedInstance.IsAtWorksite = false;
                linkedInstance.SetState(WorkerState.Moving);
            }
            else if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                if (linkedInstance.IsAssigned && !linkedInstance.IsAtWorksite)
                {
                    linkedInstance.IsAtWorksite = true;
                    linkedInstance.SetState(WorkerState.Working);

                    if (linkedInstance.AssignedStructure != null)
                    {
                        Vector3 direction = linkedInstance.AssignedStructure.transform.position - transform.position;
                        direction.y = 0;
                        if (direction.sqrMagnitude > 0.01f)
                        {
                            transform.rotation = Quaternion.LookRotation(direction);
                        }
                    }

                    linkedInstance.AssignedStructure?.RecalculateBuildSpeed();
                    linkedInstance.AssignedStructure?.RecalculateProduction();

                    Debug.Log($"<color=green>[WorkerController]</color> {linkedInstance.CustomName} arrived at worksite!");
                }
            }
        }

        private void UpdateAnimations()
        {
            if (animator == null) return;

            animator.SetBool("IsMoving", isMoving);
            animator.SetBool("IsWorking", linkedInstance?.CurrentState == WorkerState.Working);
        }

        // ============================================
        // MOVEMENT COMMANDS
        // ============================================

        public void CommandMoveTo(Vector3 position)
        {
            if (agent == null) return;

            targetPosition = position;
            agent.SetDestination(position);
            isMoving = true;

            if (linkedInstance != null)
            {
                linkedInstance.IsAtWorksite = false;
                linkedInstance.SetState(WorkerState.Moving);
            }

            Debug.Log($"<color=cyan>[WorkerController]</color> {gameObject.name} moving to {position}");
        }

        public void StopMovement()
        {
            if (agent == null) return;

            agent.ResetPath();
            isMoving = false;
        }

        // ============================================
        // DEBUG
        // ============================================

#if UNITY_EDITOR
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
        }
#endif
    }
}