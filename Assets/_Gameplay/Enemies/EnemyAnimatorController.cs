using UnityEngine;
using Sirenix.OdinInspector;

namespace WildernessSurvival.Gameplay.Enemies
{
    /// <summary>
    /// Gestisce animazioni per nemici.
    /// Aggiorna parametri Animator basato su stato del nemico (velocità, morte, attacco).
    /// Similar to WorkerAnimatorController but for enemies.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class EnemyAnimatorController : MonoBehaviour
    {
        // ============================================
        // REFERENCES
        // ============================================

        [TitleGroup("References")]
        [SerializeField]
        [Tooltip("Animator component (auto-assigned if null)")]
        private Animator animator;

        // ============================================
        // PARAMETER NAMES
        // ============================================

        [TitleGroup("Animator Parameters")]
        [InfoBox("Questi nomi devono corrispondere ai parametri nel RuntimeAnimatorController", InfoMessageType.Info)]

        [SerializeField]
        [Tooltip("Nome parametro Float per velocità (blend tree locomotion)")]
        private string speedParamName = "Speed";

        [SerializeField]
        [Tooltip("Nome parametro Bool per stato movimento (Idle/Walk transition)")]
        private string isMovingParamName = "IsMoving";

        [SerializeField]
        [Tooltip("Nome parametro Bool per stato morte")]
        private string isDeadParamName = "IsDead";

        [SerializeField]
        [Tooltip("Nome parametro Bool per stato attacco")]
        private string isAttackingParamName = "IsAttacking";

        // ============================================
        // RUNTIME STATE
        // ============================================

        // Cached parameter hashes (performance)
        private int speedHash;
        private int isMovingHash;
        private int isDeadHash;
        private int isAttackingHash;
        private int onHitHash;

        // Flags per parametri esistenti (evita warning se parametro non esiste)
        private bool hasSpeedParam;
        private bool hasIsMovingParam;
        private bool hasIsDeadParam;
        private bool hasIsAttackingParam;
        private bool hasOnHitParam;

        private bool isInitialized = false;

        // ============================================
        // PROPERTIES
        // ============================================

        public bool IsInitialized => isInitialized;
        public bool IsValid => animator != null && animator.runtimeAnimatorController != null;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            Initialize();
        }

        // ============================================
        // INITIALIZATION
        // ============================================

        /// <summary>
        /// Inizializza controller: cache parametri e verifica esistenza.
        /// </summary>
        public void Initialize()
        {
            if (isInitialized) return;

            if (animator == null)
            {
                Debug.LogWarning($"<color=red>[EnemyAnimatorController]</color> {name}: Animator not found!", this);
                return;
            }

            if (animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning($"<color=red>[EnemyAnimatorController]</color> {name}: Animator has no RuntimeAnimatorController!", this);
                return;
            }

            CacheParameters();
            isInitialized = true;

#if UNITY_EDITOR
            Debug.Log($"<color=red>[EnemyAnimatorController]</color> {name} initialized. " +
                $"Params: Speed={hasSpeedParam}, IsMoving={hasIsMovingParam}, IsDead={hasIsDeadParam}, IsAttacking={hasIsAttackingParam}, OnHit={hasOnHitParam}", this);
#endif
        }

        /// <summary>
        /// Cache parameter hashes e verifica esistenza nel controller.
        /// </summary>
        private void CacheParameters()
        {
            // Cache hash IDs (performance: evita string lookup ogni frame)
            speedHash = Animator.StringToHash(speedParamName);
            isMovingHash = Animator.StringToHash(isMovingParamName);
            isDeadHash = Animator.StringToHash(isDeadParamName);
            isAttackingHash = Animator.StringToHash(isAttackingParamName);
            onHitHash = Animator.StringToHash("OnHit"); // Trigger per hit reaction

            // Check se parametri esistono nel controller
            hasSpeedParam = false;
            hasIsMovingParam = false;
            hasIsDeadParam = false;
            hasIsAttackingParam = false;
            hasOnHitParam = false;

            if (animator.runtimeAnimatorController != null)
            {
                foreach (AnimatorControllerParameter param in animator.parameters)
                {
                    if (param.nameHash == speedHash) hasSpeedParam = true;
                    if (param.nameHash == isMovingHash) hasIsMovingParam = true;
                    if (param.nameHash == isDeadHash) hasIsDeadParam = true;
                    if (param.nameHash == isAttackingHash) hasIsAttackingParam = true;
                    if (param.nameHash == onHitHash) hasOnHitParam = true;
                }
            }
        }

        // ============================================
        // PUBLIC API - MOVEMENT ANIMATION
        // ============================================

        /// <summary>
        /// Aggiorna velocità per blend tree locomotion.
        /// Usato per blend Idle/Walk/Run basato su NavMeshAgent.velocity.magnitude
        /// </summary>
        /// <param name="speed">Velocità corrente (es. navAgent.velocity.magnitude)</param>
        public void SetSpeed(float speed)
        {
            if (animator == null || !hasSpeedParam) return;
            animator.SetFloat(speedHash, speed);
        }

        /// <summary>
        /// Setta stato movimento (Idle/Walk transition).
        /// Usato per transition Idle → Walk quando nemico inizia a muoversi.
        /// </summary>
        /// <param name="isMoving">True se nemico si sta muovendo</param>
        public void SetMoving(bool isMoving)
        {
            if (animator == null || !hasIsMovingParam) return;
            animator.SetBool(isMovingHash, isMoving);
        }

        // ============================================
        // PUBLIC API - COMBAT ANIMATION
        // ============================================

        /// <summary>
        /// Setta stato attacco (Bool).
        /// Chiamare quando nemico inizia/finisce attacco.
        /// </summary>
        /// <param name="isAttacking">True se sta attaccando</param>
        public void SetAttacking(bool isAttacking)
        {
            if (animator == null || !hasIsAttackingParam) return;
            animator.SetBool(isAttackingHash, isAttacking);

#if UNITY_EDITOR
            Debug.Log($"<color=red>[EnemyAnimatorController]</color> {name} SetAttacking: {isAttacking}", this);
#endif
        }

        /// <summary>
        /// Trigger hit reaction (one-shot).
        /// Chiamare quando nemico riceve danno ma non muore.
        /// </summary>
        public void TriggerHitReaction()
        {
            if (animator == null || !hasOnHitParam) return;
            animator.SetTrigger(onHitHash);

#if UNITY_EDITOR
            Debug.Log($"<color=red>[EnemyAnimatorController]</color> {name} triggered OnHit", this);
#endif
        }

        /// <summary>
        /// Setta stato morto.
        /// Chiamare quando nemico muore (HP <= 0).
        /// </summary>
        /// <param name="isDead">True se morto</param>
        public void SetDead(bool isDead)
        {
            if (animator == null || !hasIsDeadParam) return;
            animator.SetBool(isDeadHash, isDead);

#if UNITY_EDITOR
            Debug.Log($"<color=red>[EnemyAnimatorController]</color> {name} SetDead: {isDead}", this);
#endif
        }

        // ============================================
        // UTILITY
        // ============================================

        /// <summary>
        /// Reset a stato idle (per pooling).
        /// </summary>
        public void ResetToIdle()
        {
            SetSpeed(0f);
            SetMoving(false);
            SetDead(false);
        }

        // ============================================
        // DEBUG
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug")]
        [Button("Print Animator State", ButtonSizes.Medium)]
        private void DebugPrintState()
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[EnemyAnimatorController] Not initialized yet!", this);
                return;
            }

            string controllerName = animator.runtimeAnimatorController != null
                ? animator.runtimeAnimatorController.name
                : "None";

            string paramInfo = $"Speed: {hasSpeedParam}, IsMoving: {hasIsMovingParam}, IsDead: {hasIsDeadParam}, IsAttacking: {hasIsAttackingParam}, OnHit: {hasOnHitParam}";

            // Read current values from animator
            string currentValues = "";
            if (animator.runtimeAnimatorController != null)
            {
                if (hasSpeedParam) currentValues += $"  Speed = {animator.GetFloat(speedHash)}\n";
                if (hasIsMovingParam) currentValues += $"  IsMoving = {animator.GetBool(isMovingHash)}\n";
                if (hasIsDeadParam) currentValues += $"  IsDead = {animator.GetBool(isDeadHash)}\n";
            }

            Debug.Log($"=== ENEMY ANIMATOR STATE ===\n" +
                     $"Controller: {controllerName}\n" +
                     $"Root Motion: {animator.applyRootMotion}\n" +
                     $"Parameters Available: {paramInfo}\n" +
                     $"Current Values:\n{currentValues}" +
                     $"Is Valid: {IsValid}", this);
        }

        [TitleGroup("Debug")]
        [Button("Test Walk Animation", ButtonSizes.Small)]
        [GUIColor(0.4f, 1f, 0.4f)]
        private void DebugTestWalk()
        {
            SetSpeed(3.5f);
            SetMoving(true);
            Debug.Log("[EnemyAnimatorController] Test: Walking at speed 3.5", this);
        }

        [TitleGroup("Debug")]
        [Button("Test Idle Animation", ButtonSizes.Small)]
        [GUIColor(0.4f, 0.8f, 1f)]
        private void DebugTestIdle()
        {
            SetSpeed(0f);
            SetMoving(false);
            Debug.Log("[EnemyAnimatorController] Test: Idle", this);
        }

        [TitleGroup("Debug")]
        [Button("Test Attack Animation (Start)", ButtonSizes.Small)]
        [GUIColor(1f, 0.8f, 0.3f)]
        private void DebugTestAttackStart()
        {
            SetAttacking(true);
            Debug.Log("[EnemyAnimatorController] Test: Attack started", this);
        }

        [TitleGroup("Debug")]
        [Button("Test Attack Animation (Stop)", ButtonSizes.Small)]
        [GUIColor(1f, 0.8f, 0.3f)]
        private void DebugTestAttackStop()
        {
            SetAttacking(false);
            Debug.Log("[EnemyAnimatorController] Test: Attack stopped", this);
        }

        [TitleGroup("Debug")]
        [Button("Test Hit Reaction", ButtonSizes.Small)]
        [GUIColor(1f, 0.5f, 0.3f)]
        private void DebugTestHitReaction()
        {
            TriggerHitReaction();
            Debug.Log("[EnemyAnimatorController] Test: Hit reaction triggered", this);
        }

        [TitleGroup("Debug")]
        [Button("Test Death Animation", ButtonSizes.Small)]
        [GUIColor(1f, 0.3f, 0.3f)]
        private void DebugTestDeath()
        {
            SetDead(true);
            Debug.Log("[EnemyAnimatorController] Test: Death", this);
        }

        [TitleGroup("Debug")]
        [Button("Reset to Idle", ButtonSizes.Small)]
        private void DebugResetToIdle()
        {
            ResetToIdle();
            Debug.Log("[EnemyAnimatorController] Reset to Idle", this);
        }
#endif
    }
}
