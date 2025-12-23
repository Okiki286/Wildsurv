using UnityEngine;
using UnityEngine.AI;
using Sirenix.OdinInspector;
using WildernessSurvival.Core.Systems;

namespace WildernessSurvival.Gameplay.Enemies
{
    /// <summary>
    /// Controller principale per i nemici.
    /// Gestisce stats, movimento NavMesh verso Waystone, e debuff aura.
    /// Layer: 11 (Enemy)
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EnemyController : MonoBehaviour, IDebuffable
    {
        // ============================================
        // BASE STATS (set da EnemySpawner.Initialize)
        // ============================================

        [TitleGroup("Stats (Runtime)")]
        [ShowInInspector]
        [ReadOnly]
        private EnemyData enemyData;

        [ShowInInspector]
        [ReadOnly]
        private float currentHealth;

        [ShowInInspector]
        [ReadOnly]
        private float maxHealth;

        [ShowInInspector]
        [ReadOnly]
        private float baseDamage;

        [ShowInInspector]
        [ReadOnly]
        private float baseMoveSpeed;

        [ShowInInspector]
        [ReadOnly]
        private float rewardMultiplier;

        // ============================================
        // CURRENT STATS (after debuffs)
        // ============================================

        [TitleGroup("Current Stats")]
        [ShowInInspector]
        [ReadOnly]
        public float CurrentMoveSpeed => baseMoveSpeed * moveSpeedMultiplier;

        [ShowInInspector]
        [ReadOnly]
        public float CurrentDamage => baseDamage * attackMultiplier;

        // ============================================
        // DEBUFF STATE
        // ============================================

        [TitleGroup("Debuff State")]
        [ShowInInspector]
        [ReadOnly]
        private bool hasWaystoneDebuff = false;

        [ShowInInspector]
        [ReadOnly]
        private float moveSpeedMultiplier = 1f;

        [ShowInInspector]
        [ReadOnly]
        private float attackMultiplier = 1f;

        // ============================================
        // MOVEMENT
        // ============================================

        [TitleGroup("Movement")]
        [Tooltip("Usa NavMeshAgent se disponibile")]
        [SerializeField] private bool useNavMesh = true;

        [Tooltip("Distanza a cui fermarsi dal target")]
        [SerializeField] private float stoppingDistance = 1.5f;

        [Tooltip("Velocità di rotazione (gradi/sec) se senza NavMesh")]
        [SerializeField] private float rotationSpeed = 360f;

        [Tooltip("Frequenza aggiornamento destinazione (sec)")]
        [SerializeField] private float destinationUpdateInterval = 0.5f;

        // ============================================
        // DEBUG
        // ============================================

        [TitleGroup("Debug")]
        [SerializeField] private bool debugMode = false;

        // ============================================
        // RUNTIME
        // ============================================

        private NavMeshAgent navAgent;
        private Transform targetTransform;
        private Transform injectedTarget;  // [PERF] Cached target from spawner
        private float destinationUpdateTimer;
        private bool isInitialized = false;

        // ============================================
        // PROPERTIES
        // ============================================

        public bool HasWaystoneDebuff => hasWaystoneDebuff;
        public EnemyData Data => enemyData;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsAlive => currentHealth > 0;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            navAgent = GetComponent<NavMeshAgent>();

            // Ensure layer is correct
            if (gameObject.layer != 11)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"<color=red>[EnemyController]</color> {name} is on layer {gameObject.layer}, should be 11 (Enemy)");
                }
            }
        }

        // [POOLING-SAFE] Reset state on enable for respawned enemies
        private void OnEnable()
        {
            // Only reset if already initialized (not first spawn)
            if (isInitialized)
            {
                ResetStateForPooling();
            }
        }

        private void Start()
        {
            // If not initialized by spawner, use defaults
            if (!isInitialized)
            {
                InitializeDefaults();
            }

            // Find target from BaseCenterSystem
            UpdateTarget();
        }

        private void Update()
        {
            if (!isInitialized) return;

            // Update destination periodically
            destinationUpdateTimer += Time.deltaTime;
            if (destinationUpdateTimer >= destinationUpdateInterval)
            {
                UpdateDestination();
                destinationUpdateTimer = 0f;
            }

            // If not using NavMesh, do manual movement
            if (!useNavMesh || navAgent == null || !navAgent.isOnNavMesh)
            {
                MoveTowardsTarget();
            }
        }

        // ============================================
        // INITIALIZATION
        // ============================================

        /// <summary>
        /// Inizializza il nemico con stats scalate (chiamato da EnemySpawner)
        /// </summary>
        /// <param name="data">Enemy data asset</param>
        /// <param name="hpMul">HP multiplier from wave</param>
        /// <param name="dmgMul">Damage multiplier from wave</param>
        /// <param name="spdMul">Speed multiplier from wave</param>
        /// <param name="rwdMul">Reward multiplier from wave</param>
        /// <param name="cachedTarget">[PERF] Optional pre-cached target from spawner to avoid FindAnyObjectByType</param>
        public void Initialize(EnemyData data, float hpMul, float dmgMul, float spdMul, float rwdMul, Transform cachedTarget = null)
        {
            enemyData = data;
            injectedTarget = cachedTarget;  // Store cached target if provided

            maxHealth = data.BaseHealth * hpMul;
            baseDamage = data.AttackDamage * dmgMul;
            baseMoveSpeed = data.MoveSpeed * spdMul;
            rewardMultiplier = rwdMul;

            // Setup NavMeshAgent
            SetupNavMeshAgent();

            isInitialized = true;

            // Reset state for first spawn
            ResetStateForPooling();

            if (debugMode)
            {
                Debug.Log($"<color=red>[EnemyController]</color> {name} initialized: HP={maxHealth:F0}, DMG={baseDamage:F1}, SPD={baseMoveSpeed:F1}");
            }
        }

        /// <summary>
        /// [POOLING-SAFE] Reset all mutable state for reuse from pool.
        /// </summary>
        private void ResetStateForPooling()
        {
            // Reset health
            currentHealth = maxHealth;

            // Reset debuff state
            hasWaystoneDebuff = false;
            moveSpeedMultiplier = 1f;
            attackMultiplier = 1f;

            // Reset targeting
            targetTransform = null;
            destinationUpdateTimer = 0f;

            // Reset NavMeshAgent
            if (navAgent != null && navAgent.isOnNavMesh)
            {
                navAgent.isStopped = false;
                navAgent.velocity = Vector3.zero;
                navAgent.ResetPath();
                navAgent.speed = CurrentMoveSpeed;
            }

            // Find initial target
            UpdateTarget();
        }

        /// <summary>
        /// Inizializzazione con valori di default (se non spawned via EnemySpawner)
        /// </summary>
        private void InitializeDefaults()
        {
            if (enemyData != null)
            {
                Initialize(enemyData, 1f, 1f, 1f, 1f);
            }
            else
            {
                // Fallback stats for testing
                maxHealth = 100f;
                currentHealth = maxHealth;
                baseDamage = 10f;
                baseMoveSpeed = 3f;
                rewardMultiplier = 1f;

                SetupNavMeshAgent();
                isInitialized = true;

                if (debugMode)
                {
                    Debug.Log($"<color=red>[EnemyController]</color> {name} using default stats (no EnemyData)");
                }
            }
        }

        private void SetupNavMeshAgent()
        {
            if (navAgent == null) return;

            navAgent.speed = CurrentMoveSpeed;
            navAgent.stoppingDistance = stoppingDistance;
            navAgent.angularSpeed = rotationSpeed;
            navAgent.acceleration = 8f;
            navAgent.autoBraking = true;
        }

        // ============================================
        // TARGETING
        // ============================================

        /// <summary>
        /// Aggiorna il target dal BaseCenterSystem o injected target
        /// </summary>
        private void UpdateTarget()
        {
            // [PERF] Priority 1: Use injected target from spawner (no runtime search)
            if (injectedTarget != null)
            {
                targetTransform = injectedTarget;

                if (debugMode && targetTransform != null)
                {
                    Debug.Log($"<color=red>[EnemyController]</color> {name} using injected target: {targetTransform.name}");
                }
                return;
            }

            // Priority 2: BaseCenterSystem singleton
            if (BaseCenterSystem.Instance != null && BaseCenterSystem.Instance.HasCenter)
            {
                targetTransform = BaseCenterSystem.Instance.CurrentCenter;

                if (debugMode && targetTransform != null)
                {
                    Debug.Log($"<color=red>[EnemyController]</color> {name} targeting: {targetTransform.name}");
                }
            }
            // [PERF] REMOVED: FindAnyObjectByType fallback - expensive O(n) search
            // Enemies without proper initialization will just not move
        }

        private void UpdateDestination()
        {
            if (targetTransform == null)
            {
                UpdateTarget();
                return;
            }

            if (useNavMesh && navAgent != null && navAgent.isOnNavMesh)
            {
                navAgent.SetDestination(targetTransform.position);
                navAgent.speed = CurrentMoveSpeed;
            }
        }

        private void MoveTowardsTarget()
        {
            if (targetTransform == null) return;

            Vector3 direction = (targetTransform.position - transform.position);
            direction.y = 0f; // Stay on ground plane
            float distance = direction.magnitude;

            if (distance <= stoppingDistance) return;

            direction.Normalize();

            // Move
            transform.position += direction * CurrentMoveSpeed * Time.deltaTime;

            // Rotate
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }

        // ============================================
        // IDebuffable IMPLEMENTATION
        // ============================================

        public void ApplyWaystoneDebuff(float moveMul, float atkMul)
        {
            // Skip if already applied with same values
            if (hasWaystoneDebuff &&
                Mathf.Approximately(moveSpeedMultiplier, moveMul) &&
                Mathf.Approximately(attackMultiplier, atkMul))
            {
                return;
            }

            hasWaystoneDebuff = true;
            moveSpeedMultiplier = moveMul;
            attackMultiplier = atkMul;

            // Update NavMeshAgent speed
            if (navAgent != null && navAgent.isOnNavMesh)
            {
                navAgent.speed = CurrentMoveSpeed;
            }

            if (debugMode)
            {
                Debug.Log($"<color=yellow>[EnemyController]</color> {name} DEBUFF APPLIED: " +
                    $"Speed {baseMoveSpeed:F1} -> {CurrentMoveSpeed:F1}, " +
                    $"Damage {baseDamage:F1} -> {CurrentDamage:F1}");
            }
        }

        public void RemoveWaystoneDebuff()
        {
            if (!hasWaystoneDebuff) return;

            hasWaystoneDebuff = false;
            moveSpeedMultiplier = 1f;
            attackMultiplier = 1f;

            // Update NavMeshAgent speed
            if (navAgent != null && navAgent.isOnNavMesh)
            {
                navAgent.speed = CurrentMoveSpeed;
            }

            if (debugMode)
            {
                Debug.Log($"<color=green>[EnemyController]</color> {name} DEBUFF REMOVED: " +
                    $"Speed restored to {CurrentMoveSpeed:F1}, Damage restored to {CurrentDamage:F1}");
            }
        }

        // ============================================
        // COMBAT
        // ============================================

        /// <summary>
        /// Applica danno al nemico
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;

            currentHealth -= damage;

            if (debugMode)
            {
                Debug.Log($"<color=red>[EnemyController]</color> {name} took {damage:F1} damage. HP: {currentHealth:F0}/{maxHealth:F0}");
            }

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Gestisce morte del nemico
        /// </summary>
        protected virtual void Die()
        {
            // Stop movement
            if (navAgent != null && navAgent.isOnNavMesh)
            {
                navAgent.isStopped = true;
            }

            // Calculate rewards
            int shardDrop = 0;
            if (enemyData != null)
            {
                shardDrop = Mathf.RoundToInt(enemyData.BaseShardDrop * rewardMultiplier);
            }

            if (debugMode)
            {
                string enemyName = enemyData != null ? enemyData.DisplayName : name;
                Debug.Log($"<color=red>[EnemyController]</color> {enemyName} DIED, dropping {shardDrop} shards");
            }

            // TODO: Drop resources, notify systems, play VFX/SFX

            // [MODIFY] POOLING: Return to pool instead of Destroy
            if (EnemyPooler.Instance != null)
            {
                EnemyPooler.Instance.ReturnEnemy(gameObject);
            }
            else
            {
                // Fallback: Destroy if pooler not available
                Destroy(gameObject);
            }
        }

        // ============================================
        // CLEANUP
        // ============================================

        private void OnDestroy()
        {
            // Debuff cleanup is handled by WaystoneDebuffAura.OnTriggerExit
            // or WaystoneDebuffAura.OnDestroy
        }

        // ============================================
        // DEBUG
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug Actions")]
        [Button("Force Update Target", ButtonSizes.Medium)]
        private void DebugUpdateTarget()
        {
            UpdateTarget();
            UpdateDestination();
        }

        [Button("Apply Test Debuff (75%/85%)", ButtonSizes.Medium)]
        [GUIColor(1f, 0.8f, 0.4f)]
        private void DebugApplyDebuff()
        {
            ApplyWaystoneDebuff(0.75f, 0.85f);
        }

        [Button("Remove Debuff", ButtonSizes.Medium)]
        [GUIColor(0.4f, 1f, 0.4f)]
        private void DebugRemoveDebuff()
        {
            RemoveWaystoneDebuff();
        }

        [Button("Take 25 Damage", ButtonSizes.Medium)]
        [GUIColor(1f, 0.4f, 0.4f)]
        private void DebugTakeDamage()
        {
            TakeDamage(25f);
        }

        [Button("Log Status", ButtonSizes.Medium)]
        private void DebugLogStatus()
        {
            Debug.Log($"<color=red>[EnemyController]</color> {name} Status:\n" +
                $"  HP: {currentHealth:F0}/{maxHealth:F0}\n" +
                $"  Speed: {CurrentMoveSpeed:F1} (base: {baseMoveSpeed:F1}, mul: {moveSpeedMultiplier:P0})\n" +
                $"  Damage: {CurrentDamage:F1} (base: {baseDamage:F1}, mul: {attackMultiplier:P0})\n" +
                $"  Debuffed: {hasWaystoneDebuff}\n" +
                $"  Target: {(targetTransform != null ? targetTransform.name : "NULL")}\n" +
                $"  NavMesh: {(navAgent != null && navAgent.isOnNavMesh)}");
        }

        private void OnDrawGizmosSelected()
        {
            // Draw line to target
            if (targetTransform != null)
            {
                Gizmos.color = hasWaystoneDebuff ? Color.yellow : Color.red;
                Gizmos.DrawLine(transform.position, targetTransform.position);
            }

            // Draw stopping distance
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, stoppingDistance);
        }
#endif
    }
}
