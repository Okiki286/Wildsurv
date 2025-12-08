using UnityEngine;
using UnityEngine.AI;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;

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
    /// Gestisce movimento, animazioni, work wandering e cambio visuale.
    /// NOTA: Il NavMeshAgent è sulla root e NON viene mai toccato dal visual swap.
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
        [Tooltip("Animator del modello 3D. Viene aggiornato automaticamente dopo visual swap.")]
        private Animator animator;

        [TitleGroup("Visual Root")]
        [SerializeField]
        [Required("Assegna il Transform contenitore dei modelli visuali")]
        [Tooltip("Transform contenitore sotto cui vengono istanziati i modelli 3D. NON è la root del worker!")]
        private Transform visualRoot;

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

        [BoxGroup("Outfit Change")]
        [SerializeField]
        [PropertyRange(0.5f, 3f)]
        private float outfitChangeDuration = 1.0f;

        [BoxGroup("Outfit Change")]
        [SerializeField]
        [Tooltip("VFX 'Puff' da riprodurre durante il cambio outfit (opzionale)")]
        private ParticleSystem changeJobVFX;

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

        // ============================================
        // VISUAL CHANGE STATE
        // ============================================

        [TitleGroup("Visual Change")]
        [ShowInInspector, ReadOnly]
        private GameObject pendingModelPrefab;

        [ShowInInspector, ReadOnly]
        private RuntimeAnimatorController pendingAnimatorController;

        [ShowInInspector, ReadOnly]
        private bool isChangingOutfit = false;

        [ShowInInspector, ReadOnly]
        private string currentVisualName = null;

        private Coroutine outfitChangeCoroutine;

        // Work Wandering State
        private Vector3 currentWorkTargetCenter;
        private Vector3 structurePosition;
        private float workTimer;
        private float currentSpeed;

        // Animator parameter hashes
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsWorkingHash = Animator.StringToHash("IsWorking");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        
        private bool hasIsMovingParam = false;
        private bool hasIsWorkingParam = false;
        private bool hasSpeedParam = false;

        // ============================================
        // PROPERTIES
        // ============================================

        public WorkerData Data => workerData;
        public bool IsAlive => linkedInstance?.IsAlive ?? true;
        public bool IsMoving => isMoving;
        public MovementState CurrentMovementState => currentMovementState;
        public bool IsChangingOutfit => isChangingOutfit;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            
            // Auto-find or create visualRoot
            if (visualRoot == null)
            {
                visualRoot = transform.Find("VisualRoot");
                if (visualRoot == null)
                {
                    // Create VisualRoot
                    GameObject visualRootObj = new GameObject("VisualRoot");
                    visualRootObj.transform.SetParent(transform);
                    visualRootObj.transform.localPosition = Vector3.zero;
                    visualRootObj.transform.localRotation = Quaternion.identity;
                    visualRoot = visualRootObj.transform;
                    
                    // ═══════════════════════════════════════════════════════════
                    // CRITICO: Sposta i figli visuali esistenti sotto visualRoot
                    // Altrimenti rimangono fuori e non vengono distrutti
                    // ═══════════════════════════════════════════════════════════
                    List<Transform> childrenToMove = new List<Transform>();
                    foreach (Transform child in transform)
                    {
                        // Non spostare visualRoot stesso, e skip componenti NavMesh
                        if (child != visualRoot && child.GetComponent<NavMeshAgent>() == null)
                        {
                            // Cerca se è un oggetto visuale (ha Renderer o Animator)
                            if (child.GetComponentInChildren<Renderer>() != null || 
                                child.GetComponentInChildren<Animator>() != null)
                            {
                                childrenToMove.Add(child);
                            }
                        }
                    }
                    
                    foreach (Transform child in childrenToMove)
                    {
                        child.SetParent(visualRoot);
                        Debug.Log($"<color=cyan>[WorkerController]</color> Moved '{child.name}' under VisualRoot");
                    }
                    
                    Debug.Log($"<color=yellow>[WorkerController]</color> Created VisualRoot for {gameObject.name}, moved {childrenToMove.Count} children");
                }
            }

            // Auto-find animator
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
            
            CheckAnimatorParameters();

            // ═══════════════════════════════════════════════════════════
            // INITIALIZE currentVisualName from existing visual model
            // This enables Smart Check to work from the first spawn
            // ═══════════════════════════════════════════════════════════
            if (visualRoot != null && visualRoot.childCount > 0)
            {
                Transform firstChild = visualRoot.GetChild(0);
                // Clean up name: remove "Model_" prefix, "(Clone)" suffix for consistent comparison
                string childName = firstChild.name;
                if (childName.StartsWith("Model_"))
                    childName = childName.Substring(6); // Remove "Model_"
                childName = childName.Replace("(Clone)", "").Trim();
                currentVisualName = childName;
                Debug.Log($"<color=cyan>[WorkerController]</color> Initialized currentVisualName: '{currentVisualName}'");
            }

            if (WorkerSystem.Instance != null)
            {
                WorkerSystem.Instance.RegisterWorker(this);
            }
        }
        
        private void CheckAnimatorParameters()
        {
            hasSpeedParam = false;
            hasIsWorkingParam = false;
            hasIsMovingParam = false;

            if (animator == null || animator.runtimeAnimatorController == null) return;
            
            foreach (var param in animator.parameters)
            {
                if (param.nameHash == SpeedHash) hasSpeedParam = true;
                if (param.nameHash == IsWorkingHash) hasIsWorkingParam = true;
                if (param.nameHash == IsMovingHash) hasIsMovingParam = true;
            }
        }

        private void OnDestroy()
        {
            if (WorkerSystem.Instance != null)
            {
                WorkerSystem.Instance.UnregisterWorker(this);
            }
        }

        private void LateUpdate()
        {
            if (isForcedIdle && animator != null)
            {
                if (hasSpeedParam) animator.SetFloat(SpeedHash, 0f);
                if (hasIsWorkingParam) animator.SetBool(IsWorkingHash, false);
                if (hasIsMovingParam) animator.SetBool(IsMovingHash, false);
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
        // VISUAL CHANGE SYSTEM
        // ============================================

        /// <summary>
        /// Schedula un cambio di modello visivo.
        /// Se il worker è fermo, avvia subito. Altrimenti aspetta l'arrivo.
        /// </summary>
        public void ScheduleVisualChange(GameObject modelPrefab, RuntimeAnimatorController animController = null)
        {
            if (modelPrefab == null)
            {
                Debug.LogWarning($"[WorkerController] {gameObject.name}: ScheduleVisualChange called with null prefab.");
                return;
            }

            // ═══════════════════════════════════════════════════════════
            // SMART CHECK: Skip if already using this visual model
            // ═══════════════════════════════════════════════════════════
            if (!string.IsNullOrEmpty(currentVisualName) && modelPrefab.name == currentVisualName)
            {
                Debug.Log($"<color=gray>[WorkerController]</color> {gameObject.name}: Visual change skipped - same prefab '{currentVisualName}'");
                return;
            }

            pendingModelPrefab = modelPrefab;
            pendingAnimatorController = animController;
            
            Debug.Log($"<color=cyan>[WorkerController]</color> {gameObject.name}: Visual change scheduled to {modelPrefab.name} - will apply on arrival");

            // ═══════════════════════════════════════════════════════════
            // DELAYED CHANGE: Il cambio visivo avviene in OnArrivedAtDestination()
            // NON avviamo subito anche se il worker è fermo
            // ═══════════════════════════════════════════════════════════
        }

        private void StartOutfitChange()
        {
            if (isChangingOutfit || pendingModelPrefab == null) return;

            if (outfitChangeCoroutine != null)
            {
                StopCoroutine(outfitChangeCoroutine);
            }

            outfitChangeCoroutine = StartCoroutine(ChangeOutfitRoutine());
        }

        /// <summary>
        /// Coroutine per il cambio outfit.
        /// 1. Disabilita visualRoot
        /// 2. Attendi
        /// 3. Distruggi figli di visualRoot
        /// 4. Istanzia nuovo modello
        /// 5. Rebind animator
        /// 6. Abilita visualRoot
        /// </summary>
        private IEnumerator ChangeOutfitRoutine()
        {
            isChangingOutfit = true;

            Debug.Log($"<color=magenta>[WorkerController]</color> {gameObject.name}: Starting outfit change...");

            // ═══════════════════════════════════════════════════════════
            // 1. VFX PUFF 1 (Sparizione)
            // ═══════════════════════════════════════════════════════════
            if (changeJobVFX != null)
            {
                changeJobVFX.Play();
            }

            // ═══════════════════════════════════════════════════════════
            // 2. DISABILITA VISUAL ROOT (Effetto Sparizione)
            // ═══════════════════════════════════════════════════════════
            if (visualRoot != null)
            {
                visualRoot.gameObject.SetActive(false);
            }

            // ═══════════════════════════════════════════════════════════
            // 3. ATTENDI
            // ═══════════════════════════════════════════════════════════
            yield return new WaitForSeconds(outfitChangeDuration);

            // ═══════════════════════════════════════════════════════════
            // 4. CLEAN: Distruggi tutti i figli di visualRoot
            // ═══════════════════════════════════════════════════════════
            if (visualRoot != null)
            {
                for (int i = visualRoot.childCount - 1; i >= 0; i--)
                {
                    Destroy(visualRoot.GetChild(i).gameObject);
                }
            }

            // ═══════════════════════════════════════════════════════════
            // 5. SPAWN: Istanzia nuovo modello come figlio di visualRoot
            // ═══════════════════════════════════════════════════════════
            if (pendingModelPrefab != null && visualRoot != null)
            {
                // CRITICAL: Reset visualRoot position to ensure alignment with worker root
                visualRoot.localPosition = Vector3.zero;
                visualRoot.localRotation = Quaternion.identity;

                // ═══════════════════════════════════════════════════════════
                // CRITICAL FIX: Instantiate as INACTIVE to prevent Awake()
                // Then remove stray components, THEN activate
                // ═══════════════════════════════════════════════════════════
                
                // Temporarily disable prefab before instantiation
                bool wasActive = pendingModelPrefab.activeSelf;
                pendingModelPrefab.SetActive(false);
                
                GameObject newModel = Instantiate(pendingModelPrefab, visualRoot);
                
                // Restore prefab state
                pendingModelPrefab.SetActive(wasActive);
                
                // Remove stray WorkerControllers BEFORE activating
                var strayControllers = newModel.GetComponentsInChildren<WorkerController>(true);
                foreach (var strayCtrl in strayControllers)
                {
                    if (strayCtrl != this)
                    {
                        Debug.LogWarning($"<color=red>[WorkerController]</color> REMOVED stray WorkerController from '{newModel.name}'. Visual prefabs should be PURE 3D models!");
                        #if UNITY_EDITOR
                        DestroyImmediate(strayCtrl);
                        #else
                        Destroy(strayCtrl);
                        #endif
                    }
                }
                
                // NOW activate the model (Awake will run but no stray WorkerController)
                newModel.SetActive(true);
                
                // Force local transform reset
                newModel.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                newModel.transform.localScale = Vector3.one;
                newModel.name = $"Model_{pendingModelPrefab.name}";

                Debug.Log($"<color=yellow>[WorkerController]</color> Spawned '{newModel.name}' at local pos {newModel.transform.localPosition}");

                // ═══════════════════════════════════════════════════════════
                // 6. UPDATE CURRENT VISUAL NAME (per Smart Check)
                // ═══════════════════════════════════════════════════════════
                currentVisualName = pendingModelPrefab.name;

                // ═══════════════════════════════════════════════════════════
                // 7. REBIND: Cerca e configura Animator
                // ═══════════════════════════════════════════════════════════
                animator = newModel.GetComponentInChildren<Animator>();

                if (animator != null)
                {
                    // Se c'è un controller override, usalo
                    if (pendingAnimatorController != null)
                    {
                        animator.runtimeAnimatorController = pendingAnimatorController;
                    }

                    // Re-check parameters
                    CheckAnimatorParameters();

                    // Reset animator state to prevent T-pose
                    if (hasSpeedParam) animator.SetFloat(SpeedHash, 0f);
                    if (hasIsMovingParam) animator.SetBool(IsMovingHash, false);
                    if (hasIsWorkingParam) animator.SetBool(IsWorkingHash, false);

                    animator.Update(0f);

                    Debug.Log($"<color=green>[WorkerController]</color> Animator rebound: {animator.gameObject.name}");
                }
                else
                {
                    Debug.LogWarning($"[WorkerController] No Animator found in new model {pendingModelPrefab.name}");
                }
            }

            pendingModelPrefab = null;
            pendingAnimatorController = null;

            // ═══════════════════════════════════════════════════════════
            // 8. ABILITA VISUAL ROOT (Effetto Apparizione)
            // ═══════════════════════════════════════════════════════════
            if (visualRoot != null)
            {
                visualRoot.gameObject.SetActive(true);
            }

            // ═══════════════════════════════════════════════════════════
            // 9. VFX PUFF 2 (Riapparizione)
            // ═══════════════════════════════════════════════════════════
            if (changeJobVFX != null)
            {
                changeJobVFX.Play();
            }

            isChangingOutfit = false;
            outfitChangeCoroutine = null;

            Debug.Log($"<color=green>[WorkerController]</color> {gameObject.name}: Outfit change complete!");

            // Riprendi pattugliamento se assegnato
            if (linkedInstance != null && linkedInstance.IsAssigned)
            {
                isPatrollingWorksite = true;
                workTimer = 0.5f;
            }
        }

        // ============================================
        // UPDATE (chiamato da WorkerSystem)
        // ============================================

        public void ManualUpdate(float deltaTime)
        {
            if (isChangingOutfit) return;

            if (isForcedIdle)
            {
                if (animator != null)
                {
                    if (hasSpeedParam) animator.SetFloat(SpeedHash, 0f);
                    if (hasIsWorkingParam) animator.SetBool(IsWorkingHash, false);
                    if (hasIsMovingParam) animator.SetBool(IsMovingHash, false);
                }
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

            UpdateAnimations();
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
            
            // Check for pending visual change
            if (pendingModelPrefab != null && !isChangingOutfit)
            {
                Debug.Log($"<color=cyan>[WorkerController]</color> Arrived - starting scheduled outfit change");
                isPatrollingWorksite = false;
                isForcedIdle = false;
                StartOutfitChange();
                return;
            }

            isPatrollingWorksite = true;
            workTimer = 0.5f;

            if (linkedInstance != null && linkedInstance.IsAssigned && !linkedInstance.IsAtWorksite)
            {
                linkedInstance.IsAtWorksite = true;
                linkedInstance.SetState(WorkerState.Working);
                linkedInstance.AssignedStructure?.RecalculateBuildSpeed();
                linkedInstance.AssignedStructure?.RecalculateProduction();
                Debug.Log($"<color=green>[WorkerController]</color> {linkedInstance.CustomName} arrived at worksite!");
            }
        }

        private void MoveToRandomWorkPoint()
        {
            if (!isPatrollingWorksite || isForcedIdle || isChangingOutfit) return;

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
        /// Forza idle MANTENENDO pending visual per permettere il cambio outfit.
        /// </summary>
        public void ForceIdleKeepPendingVisual()
        {
            if (outfitChangeCoroutine != null)
            {
                StopCoroutine(outfitChangeCoroutine);
                outfitChangeCoroutine = null;
            }
            isChangingOutfit = false;

            if (visualRoot != null && !visualRoot.gameObject.activeSelf)
            {
                visualRoot.gameObject.SetActive(true);
            }

            ForceIdleInternal();

            // Se c'è pending visual, sblocca e avvia cambio
            if (pendingModelPrefab != null)
            {
                Debug.Log($"<color=yellow>[WorkerController]</color> Pending visual detected - unlocking and starting outfit change");
                isForcedIdle = false;
                StartOutfitChange();
            }
        }

        /// <summary>
        /// Forza idle completo e CANCELLA pending visual.
        /// </summary>
        public void ForceIdle()
        {
            if (outfitChangeCoroutine != null)
            {
                StopCoroutine(outfitChangeCoroutine);
                outfitChangeCoroutine = null;
            }
            isChangingOutfit = false;
            pendingModelPrefab = null;
            pendingAnimatorController = null;

            if (visualRoot != null && !visualRoot.gameObject.activeSelf)
            {
                visualRoot.gameObject.SetActive(true);
            }

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

            if (animator != null)
            {
                if (hasIsWorkingParam) animator.SetBool(IsWorkingHash, false);
                if (hasIsMovingParam) animator.SetBool(IsMovingHash, false);
                if (hasSpeedParam) animator.SetFloat(SpeedHash, 0f);
                animator.Play("Idle", 0, 0f);
                animator.Update(0f);
            }
        }

        // ============================================
        // ANIMATIONS
        // ============================================

        private void UpdateAnimations()
        {
            if (animator == null || isForcedIdle || isChangingOutfit) return;

            if (hasSpeedParam) animator.SetFloat(SpeedHash, currentSpeed);

            bool shouldPlayWorkAnim = isPatrollingWorksite && 
                                      currentMovementState == MovementState.WorkingOnSite && 
                                      isPlayingWorkAnimation;
            if (hasIsWorkingParam) animator.SetBool(IsWorkingHash, shouldPlayWorkAnim);
            if (hasIsMovingParam) animator.SetBool(IsMovingHash, isMoving && !shouldPlayWorkAnim);
        }

        // ============================================
        // DEBUG
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug Info")]
        [ShowInInspector, ReadOnly]
        private float DebugCurrentSpeed => currentSpeed;

        [ShowInInspector, ReadOnly]
        private string DebugVisualRoot => visualRoot != null ? visualRoot.name : "NULL";

        [ShowInInspector, ReadOnly]
        private string DebugPendingVisual => pendingModelPrefab != null ? pendingModelPrefab.name : "None";

        [TitleGroup("Debug")]
        [Button("🎯 Force Work Patrol", ButtonSizes.Medium)]
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

        [Button("🛑 Force Idle", ButtonSizes.Medium), GUIColor(1f, 0.3f, 0.3f)]
        private void DebugForceIdle()
        {
            if (Application.isPlaying) ForceIdle();
        }

        [Button("🔓 Unlock Worker", ButtonSizes.Medium), GUIColor(0.3f, 1f, 0.3f)]
        private void DebugUnlock()
        {
            if (!Application.isPlaying) return;
            isForcedIdle = false;
            if (agent != null) agent.isStopped = false;
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

            if (isChangingOutfit)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(transform.position + Vector3.up * 2.5f, Vector3.one * 0.4f);
            }
        }
#endif
    }
}