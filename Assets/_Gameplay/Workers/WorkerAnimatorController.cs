using UnityEngine;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// Gestisce l'Animator del worker.
    /// Responsabile di animator controller swap, aggiornamento parametri e root motion.
    /// </summary>
    public class WorkerAnimatorController : MonoBehaviour
    {
        // ============================================
        // ANIMATOR
        // ============================================

        [Header("Animator")]
        [SerializeField]
        [Tooltip("Animator del worker")]
        private Animator animator;

        [SerializeField]
        [Tooltip("AnimatorController di default (Idle/Villager)")]
        private RuntimeAnimatorController defaultAnimatorController;

        // ============================================
        // ANIMATOR PARAMETERS (cached hashes)
        // ============================================

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsWorkingHash = Animator.StringToHash("IsWorking");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

        private bool hasSpeedParam = false;
        private bool hasIsWorkingParam = false;
        private bool hasIsMovingParam = false;

        // ============================================
        // PROPERTIES
        // ============================================

        public Animator Animator => animator;
        public RuntimeAnimatorController CurrentController => animator?.runtimeAnimatorController;

        // ============================================
        // INITIALIZATION
        // ============================================

        private void Awake()
        {
            // Auto-find animator se non assegnato
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            // GARANTIRE ZERO ROOT MOTION
            if (animator != null)
            {
                animator.applyRootMotion = false;
            }

            CheckAnimatorParameters();
        }

        // ============================================
        // ANIMATOR CONTROLLER SWAP
        // ============================================

        /// <summary>
        /// Cambia l'AnimatorController.
        /// </summary>
        public void SetAnimatorController(RuntimeAnimatorController controller)
        {
            if (animator == null) return;

            if (controller != null)
            {
                animator.runtimeAnimatorController = controller;
            }
            else if (defaultAnimatorController != null)
            {
                animator.runtimeAnimatorController = defaultAnimatorController;
            }

            // GARANTIRE ZERO ROOT MOTION dopo cambio controller
            animator.applyRootMotion = false;

            CheckAnimatorParameters();
            ResetAnimatorState();

#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[WorkerAnimatorController]</color> Animator controller changed to: {controller?.name ?? "default"}");
#endif
        }

        // ============================================
        // PARAMETER CHECKING
        // ============================================

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

        // ============================================
        // ANIMATOR UPDATE
        // ============================================

        /// <summary>
        /// Aggiorna i parametri dell'animator.
        /// Chiamato dal WorkerController ogni frame.
        /// </summary>
        public void UpdateAnimator(float speed, bool isMoving, bool isWorking)
        {
            if (animator == null) return;

            if (hasSpeedParam) animator.SetFloat(SpeedHash, speed);
            if (hasIsMovingParam) animator.SetBool(IsMovingHash, isMoving);
            if (hasIsWorkingParam) animator.SetBool(IsWorkingHash, isWorking);
        }

        // ============================================
        // ANIMATOR RESET
        // ============================================

        /// <summary>
        /// Resetta tutti i parametri dell'animator a valori idle.
        /// </summary>
        public void ResetAnimatorState()
        {
            if (animator == null) return;

            if (hasSpeedParam) animator.SetFloat(SpeedHash, 0f);
            if (hasIsMovingParam) animator.SetBool(IsMovingHash, false);
            if (hasIsWorkingParam) animator.SetBool(IsWorkingHash, false);

            animator.Update(0f);
        }

        /// <summary>
        /// Forza lo stato idle dell'animator.
        /// </summary>
        public void ForceIdleAnimation()
        {
            ResetAnimatorState();

            if (animator != null)
            {
                // Prova a giocare lo stato "Idle" se esiste
                animator.Play("Idle", 0, 0f);
                animator.Update(0f);
            }
        }

        // ============================================
        // UTILITY
        // ============================================

        /// <summary>
        /// Verifica se l'animator è valido e pronto.
        /// </summary>
        public bool IsValid()
        {
            return animator != null && animator.runtimeAnimatorController != null;
        }
    }
}
