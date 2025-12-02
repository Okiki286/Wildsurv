using UnityEngine;
using UnityEngine.AI;
using Sirenix.OdinInspector;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// Controller fisico del worker (The Puppet).
    /// Gestisce solo movimento e animazioni.
    /// NON ha logica di gioco (spostata in WorkerInstance).
    /// NON ha Update() (gestito da WorkerSystem).
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    public class WorkerController : MonoBehaviour
    {
        [TitleGroup("References")]
        [SerializeField, Required] private WorkerData data;
        [SerializeField] private Animator animator;
        [SerializeField] private NavMeshAgent agent;

        [TitleGroup("Runtime State")]
        [ShowInInspector, ReadOnly] private bool isMoving;
        [ShowInInspector, ReadOnly] private bool isWorking;

        // Accessor per WorkerInstance
        public WorkerData Data => data;
        
        // SAFETY PROPERTIES (Fix CS1061 Errors)
        public bool IsAlive => this != null && gameObject.activeInHierarchy;

        private void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (animator == null) animator = GetComponent<Animator>();

            // Mobile Optimization
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.autoBraking = true;
        }

        /// <summary>
        /// Metodo chiamato dal WorkerSystem nel loop centrale.
        /// Sostituisce Update().
        /// </summary>
        public void ManualUpdate(float deltaTime)
        {
            if (agent == null) return;

            // Check arrivo destinazione
            if (isMoving && !agent.pathPending)
            {
                if (agent.remainingDistance <= agent.stoppingDistance)
                {
                    if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                    {
                        OnDestinationReached();
                    }
                }
            }

            // Update Animation Parameters
            UpdateAnimations();
        }

        /// <summary>
        /// Comanda al worker di muoversi verso una posizione.
        /// </summary>
        public void CommandMoveTo(Vector3 targetPosition, System.Action onComplete = null)
        {
            if (agent == null) return;

            agent.SetDestination(targetPosition);
            agent.isStopped = false;
            isMoving = true;
            isWorking = false;
            
            // Nota: onComplete callback non è persistito qui per semplicità, 
            // ma potrebbe essere gestito se necessario.
        }

        private void OnDestinationReached()
        {
            isMoving = false;
            isWorking = true; // Assume working when arrived (simplified)
        }

        private void UpdateAnimations()
        {
            if (animator == null) return;
            
            // Check if animator has a controller assigned (prevents warning spam)
            if (animator.runtimeAnimatorController == null) return;

            float speed = agent.velocity.magnitude;
            animator.SetFloat("Speed", speed);
            animator.SetBool("IsWorking", isWorking);
        }

        // ============================================
        // EDITOR TOOLS
        // ============================================

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (animator == null) animator = GetComponent<Animator>();
        }
#endif
    }
}