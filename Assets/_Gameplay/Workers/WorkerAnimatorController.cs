using UnityEngine;
using Sirenix.OdinInspector;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// WorkerAnimatorController v2 - Mobile Optimized
    ///
    /// Responsabilità:
    /// - Gestione Animator swap (default → job-specific)
    /// - Aggiornamento parametri animazione (Speed, IsWorking, IsAttacking, ecc.)
    /// - Zero allocations, zero root motion
    /// - Data-driven via WorkerJobData e WorkerVisualSet
    /// - Editor preview compatible (JobAuthoringWindow)
    ///
    /// Integrazione:
    /// - Chiamato da WorkerVisualController (orchestrator)
    /// - Aggiornato da WorkerController (movimento)
    /// - Configurato via WorkerJobData.VisualSet.animatorController
    /// </summary>
    public class WorkerAnimatorController : MonoBehaviour
    {
        // ============================================
        // ANIMATOR REFERENCE
        // ============================================

        [TitleGroup("Animator")]
        [SerializeField]
        [Tooltip("Animator del worker (auto-detected se null)")]
        [ChildGameObjectsOnly]
        private Animator animator;

        [TitleGroup("Animator")]
        [SerializeField]
        [AssetsOnly]
        [Tooltip("AnimatorController di default (Idle/Villager). Usato se job non ha override.")]
        private RuntimeAnimatorController defaultAnimatorController;

        // ============================================
        // ANIMATOR PARAMETER NAMES (CONFIGURABLE)
        // ============================================

        [TitleGroup("Parameter Names")]
        [InfoBox("Configura i nomi dei parametri dell'Animator. Lascia vuoto per disabilitare.", InfoMessageType.Info)]
        [SerializeField]
        [Tooltip("Nome del parametro Float per la velocità (es. 'Speed')")]
        private string speedParamName = "Speed";

        [SerializeField]
        [Tooltip("Nome del parametro Bool per movimento (es. 'IsMoving')")]
        private string isMovingParamName = "IsMoving";

        [SerializeField]
        [Tooltip("Nome del parametro Bool per lavoro (es. 'IsWorking')")]
        private string isWorkingParamName = "IsWorking";

        [SerializeField]
        [Tooltip("Nome del parametro Bool per attacco (es. 'IsAttacking')")]
        private string isAttackingParamName = "IsAttacking";

        [SerializeField]
        [Tooltip("Nome del parametro Bool per arma equipaggiata (es. 'HasWeapon')")]
        private string hasWeaponParamName = "HasWeapon";

        // ============================================
        // CACHED PARAMETER HASHES (MOBILE OPTIMIZATION)
        // ============================================

        private int speedHash;
        private int isMovingHash;
        private int isWorkingHash;
        private int isAttackingHash;
        private int hasWeaponHash;

        // Parameter existence flags (to avoid setting non-existent params)
        private bool hasSpeedParam = false;
        private bool hasIsMovingParam = false;
        private bool hasIsWorkingParam = false;
        private bool hasIsAttackingParam = false;
        private bool hasWeaponParam = false;

        // ============================================
        // STATE
        // ============================================

        private bool isInitialized = false;
        private bool loggedMissingAnimatorWarning = false;

        // ============================================
        // PROPERTIES
        // ============================================

        public Animator Animator => animator;
        public RuntimeAnimatorController CurrentController => animator?.runtimeAnimatorController;
        public bool IsValid => animator != null && animator.runtimeAnimatorController != null;

        // ============================================
        // INITIALIZATION
        // ============================================

        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// Inizializza il controller.
        /// Chiamato automaticamente in Awake, ma può essere chiamato manualmente per re-init.
        /// </summary>
        /// <param name="animatorOverride">Animator opzionale da usare invece del field assegnato</param>
        public void Initialize(Animator animatorOverride = null)
        {
            // Auto-find animator se non assegnato
            if (animatorOverride != null)
            {
                animator = animatorOverride;
            }
            else if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(includeInactive: true);
            }

            if (animator == null)
            {
                if (!loggedMissingAnimatorWarning)
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"[WorkerAnimatorController] No Animator found on {gameObject.name}. Animation will not work.", this);
#endif
                    loggedMissingAnimatorWarning = true;
                }
                return;
            }

            // GARANTIRE ZERO ROOT MOTION (critical for NavMeshAgent)
            animator.applyRootMotion = false;

            // Cache parameter hashes
            CacheParameterHashes();

            // Check which parameters exist in current controller
            CheckAnimatorParameters();

            // Apply default controller if none is set
            if (animator.runtimeAnimatorController == null && defaultAnimatorController != null)
            {
                animator.runtimeAnimatorController = defaultAnimatorController;
                animator.applyRootMotion = false;
                CheckAnimatorParameters();
            }

            isInitialized = true;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // Editor preview mode - silent
                return;
            }
#endif
        }

        // ============================================
        // PARAMETER HASH CACHING
        // ============================================

        private void CacheParameterHashes()
        {
            // Cache StringToHash ONCE at initialization (mobile optimization)
            if (!string.IsNullOrEmpty(speedParamName))
                speedHash = Animator.StringToHash(speedParamName);

            if (!string.IsNullOrEmpty(isMovingParamName))
                isMovingHash = Animator.StringToHash(isMovingParamName);

            if (!string.IsNullOrEmpty(isWorkingParamName))
                isWorkingHash = Animator.StringToHash(isWorkingParamName);

            if (!string.IsNullOrEmpty(isAttackingParamName))
                isAttackingHash = Animator.StringToHash(isAttackingParamName);

            if (!string.IsNullOrEmpty(hasWeaponParamName))
                hasWeaponHash = Animator.StringToHash(hasWeaponParamName);
        }

        /// <summary>
        /// Verifica quali parametri esistono nell'AnimatorController corrente.
        /// Chiamato dopo ogni cambio di controller.
        /// </summary>
        private void CheckAnimatorParameters()
        {
            hasSpeedParam = false;
            hasIsMovingParam = false;
            hasIsWorkingParam = false;
            hasIsAttackingParam = false;
            hasWeaponParam = false;

            if (animator == null || animator.runtimeAnimatorController == null) return;

            // Check parameter existence (zero allocations - uses array iterator)
            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                int hash = parameters[i].nameHash;

                if (hash == speedHash) hasSpeedParam = true;
                else if (hash == isMovingHash) hasIsMovingParam = true;
                else if (hash == isWorkingHash) hasIsWorkingParam = true;
                else if (hash == isAttackingHash) hasIsAttackingParam = true;
                else if (hash == hasWeaponHash) hasWeaponParam = true;
            }
        }

        // ============================================
        // ANIMATOR CONTROLLER SWAP
        // ============================================

        /// <summary>
        /// Cambia l'AnimatorController.
        /// Se controller è null, usa defaultAnimatorController.
        /// </summary>
        public void SetAnimatorController(RuntimeAnimatorController controller)
        {
            if (animator == null)
            {
                if (!loggedMissingAnimatorWarning)
                {
#if UNITY_EDITOR
                    Debug.LogWarning("[WorkerAnimatorController] Cannot set controller: Animator is null.", this);
#endif
                    loggedMissingAnimatorWarning = true;
                }
                return;
            }

            RuntimeAnimatorController targetController = controller ?? defaultAnimatorController;

            if (targetController != null)
            {
                animator.runtimeAnimatorController = targetController;
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning("[WorkerAnimatorController] No controller to set (both provided and default are null).", this);
#endif
                return;
            }

            // GARANTIRE ZERO ROOT MOTION dopo cambio controller
            animator.applyRootMotion = false;

            // Re-check parameters for new controller
            CheckAnimatorParameters();

            // Reset state to idle
            ResetState();

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                Debug.Log($"<color=cyan>[WorkerAnimatorController]</color> Controller changed to: {targetController.name}", this);
            }
#endif
        }

        /// <summary>
        /// Applica l'AnimatorController dal WorkerJobData.
        /// Integrazione data-driven con WorkerVisualSet.
        /// </summary>
        public void ApplyJobAnimator(WorkerJobData jobData)
        {
            if (jobData == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[WorkerAnimatorController] ApplyJobAnimator called with null jobData.", this);
#endif
                return;
            }

            RuntimeAnimatorController controllerToUse = defaultAnimatorController;

            // Use job-specific animator if available
            if (jobData.VisualSet != null && jobData.VisualSet.animatorController != null)
            {
                controllerToUse = jobData.VisualSet.animatorController;
            }

            SetAnimatorController(controllerToUse);

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                string source = (jobData.VisualSet != null && jobData.VisualSet.animatorController != null)
                    ? "job-specific"
                    : "default";
                Debug.Log($"<color=cyan>[WorkerAnimatorController]</color> Applied {source} animator for job: {jobData.JobName}", this);
            }
#endif
        }

        // ============================================
        // PARAMETER UPDATES (CALLED BY WORKERCONTROLLER)
        // ============================================

        /// <summary>
        /// Aggiorna il parametro di velocità movimento.
        /// Chiamato ogni frame da WorkerController con NavMeshAgent.velocity.magnitude.
        /// </summary>
        public void UpdateMovement(float speedMagnitude)
        {
            if (animator == null) return;

            if (hasSpeedParam)
                animator.SetFloat(speedHash, speedMagnitude);

            if (hasIsMovingParam)
                animator.SetBool(isMovingHash, speedMagnitude > 0.1f);
        }

        /// <summary>
        /// Legacy overload - aggiorna tutti i parametri base.
        /// Usato da WorkerController esistente.
        /// </summary>
        public void UpdateAnimator(float speed, bool isMoving, bool isWorking)
        {
            if (animator == null) return;

            if (hasSpeedParam)
                animator.SetFloat(speedHash, speed);

            if (hasIsMovingParam)
                animator.SetBool(isMovingHash, isMoving);

            if (hasIsWorkingParam)
                animator.SetBool(isWorkingHash, isWorking);
        }

        /// <summary>
        /// Imposta lo stato "sta lavorando".
        /// </summary>
        public void SetWorking(bool isWorking)
        {
            if (animator == null) return;

            if (hasIsWorkingParam)
                animator.SetBool(isWorkingHash, isWorking);
        }

        /// <summary>
        /// Imposta lo stato "sta attaccando".
        /// </summary>
        public void SetAttacking(bool isAttacking)
        {
            if (animator == null) return;

            if (hasIsAttackingParam)
                animator.SetBool(isAttackingHash, isAttacking);
        }

        /// <summary>
        /// Imposta se il worker ha un'arma equipaggiata.
        /// </summary>
        public void SetHasWeapon(bool hasWeapon)
        {
            if (animator == null) return;

            if (hasWeaponParam)
                animator.SetBool(hasWeaponHash, hasWeapon);
        }

        // ============================================
        // TRIGGERS (ONE-SHOT ANIMATIONS)
        // ============================================

        /// <summary>
        /// Esegue un trigger one-shot (es. "Build", "Hit", "Die").
        /// Safe: non crasha se il trigger non esiste.
        /// </summary>
        public void PlayOneShotTrigger(string triggerName)
        {
            if (animator == null || string.IsNullOrEmpty(triggerName)) return;

            int triggerHash = Animator.StringToHash(triggerName);

            // Check if trigger exists (zero allocations)
            bool triggerExists = false;
            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].nameHash == triggerHash && parameters[i].type == AnimatorControllerParameterType.Trigger)
                {
                    triggerExists = true;
                    break;
                }
            }

            if (triggerExists)
            {
                animator.SetTrigger(triggerHash);
            }
            else
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                {
                    Debug.LogWarning($"[WorkerAnimatorController] Trigger '{triggerName}' not found in current animator.", this);
                }
#endif
            }
        }

        // ============================================
        // STATE RESET
        // ============================================

        /// <summary>
        /// Resetta lo stato dell'animator a idle.
        /// Chiamato dopo cambio job o quando worker torna a idle.
        /// </summary>
        public void ResetState()
        {
            if (animator == null) return;

            // Reset all boolean and float parameters to idle state
            if (hasSpeedParam)
                animator.SetFloat(speedHash, 0f);

            if (hasIsMovingParam)
                animator.SetBool(isMovingHash, false);

            if (hasIsWorkingParam)
                animator.SetBool(isWorkingHash, false);

            if (hasIsAttackingParam)
                animator.SetBool(isAttackingHash, false);

            // Force update to apply changes immediately
            animator.Update(0f);
        }

        /// <summary>
        /// Legacy method name - alias for ResetState.
        /// </summary>
        public void ResetAnimatorState()
        {
            ResetState();
        }

        /// <summary>
        /// Forza lo stato Idle dell'animator.
        /// Usa Play() per forzare lo stato "Idle" se esiste.
        /// </summary>
        public void ForceIdleAnimation()
        {
            ResetState();

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                // Try to play "Idle" state if it exists
                // This is a best-effort attempt - won't crash if state doesn't exist
                try
                {
                    animator.Play("Idle", 0, 0f);
                    animator.Update(0f);
                }
                catch
                {
                    // State doesn't exist - ignore
                }
            }
        }

        /// <summary>
        /// Full reset con Rebind (più pesante).
        /// Usato per reset completo (es. pooling, scene change).
        /// </summary>
        public void FullReset()
        {
            if (animator == null) return;

            animator.Rebind();
            animator.Update(0f);

            ResetState();
        }

        // ============================================
        // EDITOR DEBUG TOOLS
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug")]
        [Button("Print Animator State", ButtonSizes.Medium)]
        private void DebugPrintState()
        {
            if (animator == null)
            {
                Debug.Log("[WorkerAnimatorController] Animator is NULL", this);
                return;
            }

            string controllerName = animator.runtimeAnimatorController != null
                ? animator.runtimeAnimatorController.name
                : "None";

            string paramInfo = $"Speed: {hasSpeedParam}, IsMoving: {hasIsMovingParam}, IsWorking: {hasIsWorkingParam}, " +
                             $"IsAttacking: {hasIsAttackingParam}, HasWeapon: {hasWeaponParam}";

            Debug.Log($"=== ANIMATOR STATE ===\n" +
                     $"Controller: {controllerName}\n" +
                     $"Root Motion: {animator.applyRootMotion}\n" +
                     $"Parameters: {paramInfo}\n" +
                     $"Is Valid: {IsValid}", this);
        }

        [TitleGroup("Debug")]
        [Button("Test Set Working", ButtonSizes.Small)]
        private void DebugTestWorking()
        {
            SetWorking(true);
            Debug.Log("[WorkerAnimatorController] Set IsWorking = true", this);
        }

        [TitleGroup("Debug")]
        [Button("Test Set Attacking", ButtonSizes.Small)]
        private void DebugTestAttacking()
        {
            SetAttacking(true);
            Debug.Log("[WorkerAnimatorController] Set IsAttacking = true", this);
        }

        [TitleGroup("Debug")]
        [Button("Force Reset State", ButtonSizes.Small)]
        private void DebugResetState()
        {
            ResetState();
            Debug.Log("[WorkerAnimatorController] State reset to idle", this);
        }

        [TitleGroup("Debug")]
        [Button("List All Parameters", ButtonSizes.Medium)]
        private void DebugListParameters()
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning("[WorkerAnimatorController] No animator or controller to inspect.", this);
                return;
            }

            string output = "=== ANIMATOR PARAMETERS ===\n";
            AnimatorControllerParameter[] parameters = animator.parameters;

            if (parameters.Length == 0)
            {
                output += "No parameters found.\n";
            }
            else
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    var param = parameters[i];
                    output += $"• {param.name} ({param.type}) - Hash: {param.nameHash}\n";
                }
            }

            Debug.Log(output, this);
        }
#endif
    }
}
