using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Sirenix.OdinInspector;
using WildernessSurvival.Gameplay.Combat;
using WildernessSurvival.Core.Systems;

namespace WildernessSurvival.Gameplay.Enemies
{
    /// <summary>
    /// Componente per istanza nemico con AI combat.
    /// Gestisce pathfinding, target selection e attacco.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyInstance : MonoBehaviour, IDamageable, IDebuffable
    {
        // ============================================
        // STATS (RUNTIME)
        // ============================================

        [TitleGroup("Stats (Runtime)")]
        [ShowInInspector, ReadOnly]
        private EnemyData enemyData;

        [ShowInInspector, ReadOnly]
        private float currentHealth;

        [ShowInInspector, ReadOnly]
        private float maxHealth;

        [ShowInInspector, ReadOnly]
        private float damage;

        [ShowInInspector, ReadOnly]
        private float moveSpeed;

        [ShowInInspector, ReadOnly]
        private float rewardMultiplier;

        // ============================================
        // COMBAT STATE
        // ============================================

        [TitleGroup("Combat")]
        [ShowInInspector, ReadOnly]
        private IDamageable currentTarget;

        [ShowInInspector, ReadOnly]
        private Transform currentTargetTransform;

        [ShowInInspector, ReadOnly]
        private float attackCooldown;

        private float targetScanTimer;
        private const float TARGET_SCAN_INTERVAL = 0.5f;

        // ============================================
        // DEBUFF STATE
        // ============================================

        [TitleGroup("Debuff")]
        [ShowInInspector, ReadOnly]
        private float moveMultiplier = 1f;

        [ShowInInspector, ReadOnly]
        private float attackMultiplier = 1f;

        public bool HasWaystoneDebuff { get; private set; }

        // ============================================
        // COMPONENTS
        // ============================================

        private NavMeshAgent agent;

        // Buffer per OverlapSphereNonAlloc (riuso per evitare GC)
        private static readonly Collider[] scanBuffer = new Collider[32];

        // Flag di inizializzazione
        private bool isInitialized = false;

        [TitleGroup("Debug")]
        [SerializeField] private bool debugMode = true;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            if (!IsAlive || !isInitialized) return;

            // Update cooldown
            if (attackCooldown > 0f)
            {
                attackCooldown -= Time.deltaTime;
            }

            // Periodic target scan
            targetScanTimer -= Time.deltaTime;
            if (targetScanTimer <= 0f)
            {
                targetScanTimer = TARGET_SCAN_INTERVAL;
                ScanForTarget();
            }

            // Combat logic
            if (currentTargetTransform != null && currentTarget != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, currentTargetTransform.position);
                float effectiveAttackRange = enemyData != null ? enemyData.AttackRange : 1.5f;

                if (distanceToTarget <= effectiveAttackRange)
                {
                    // In range - attacca
                    TryAttack();
                }
                else
                {
                    // Muovi verso target
                    if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
                    {
                        agent.SetDestination(currentTargetTransform.position);
                    }
                }
            }
            else
            {
                // Nessun target valido - cerca Waystone come fallback
                FallbackToWaystone();
            }
        }

        // ============================================
        // INITIALIZATION
        // ============================================

        /// <summary>
        /// Inizializza il nemico con stats scalate
        /// </summary>
        public void Initialize(EnemyData data, float hpMul, float dmgMul, float spdMul, float rwdMul)
        {
            enemyData = data;

            // Calculate scaled stats
            maxHealth = data.BaseHealth * hpMul;
            currentHealth = maxHealth;
            damage = data.AttackDamage * dmgMul;
            moveSpeed = data.MoveSpeed * spdMul;
            rewardMultiplier = rwdMul;

            // Apply NavMeshAgent settings
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.speed = moveSpeed;
                agent.stoppingDistance = data.AttackRange * 0.9f;
                agent.isStopped = false;
            }

            // Ensure we're on NavMesh
            EnsureOnNavMesh();

            // Initial target scan
            targetScanTimer = 0f; // Force immediate scan
            isInitialized = true;

            // Force immediate target acquisition and movement
            FallbackToWaystone();

            // Debug diagnostics (one-time)
            if (debugMode)
            {
                StartCoroutine(LogInitDiagnosticsDelayed());
            }
        }

        /// <summary>
        /// Assicura che l'enemy sia su NavMesh. Se non lo è, prova a warpare.
        /// </summary>
        private void EnsureOnNavMesh()
        {
            if (agent == null) return;

            if (!agent.isOnNavMesh)
            {
                // Prova a trovare punto NavMesh vicino
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                    if (debugMode)
                    {
                        Debug.Log($"<color=yellow>[EnemyInstance]</color> {name} warped from {transform.position} to NavMesh at {hit.position}");
                    }
                }
                else
                {
                    Debug.LogError($"<color=red>[EnemyInstance]</color> {name} spawned OFF NavMesh at {transform.position} and cannot find valid NavMesh point within 5m!");
                }
            }
        }

        private IEnumerator LogInitDiagnosticsDelayed()
        {
            // Log immediato
            Debug.Log($"<color=cyan>[EnemyInit]</color> {name} pos={transform.position} " +
                $"enabled={agent?.enabled} onNavMesh={agent?.isOnNavMesh} stopped={agent?.isStopped} " +
                $"speed={agent?.speed} stopDist={agent?.stoppingDistance}");

            string targetName = currentTargetTransform != null ? currentTargetTransform.name : "NULL";
            float dist = currentTargetTransform != null ? Vector3.Distance(transform.position, currentTargetTransform.position) : -1f;
            Debug.Log($"<color=cyan>[EnemyTarget]</color> {name} target={targetName} dist={dist:F1}");

            // Aspetta 1 frame
            yield return null;

            // Log dopo 1 frame
            if (agent != null)
            {
                Debug.Log($"<color=cyan>[EnemyFrame+1]</color> {name} hasPath={agent.hasPath} pathStatus={agent.pathStatus} " +
                    $"remainingDist={agent.remainingDistance:F1} velocity={agent.velocity.magnitude:F1}");
            }
        }

        // ============================================
        // TARGET SELECTION (B4)
        // ============================================

        private void ScanForTarget()
        {
            if (enemyData == null) return;

            float aggroRange = enemyData.AggroRange;

            // Se aggroRange <= 0, vai solo al Waystone
            if (aggroRange <= 0f)
            {
                FallbackToWaystone();
                return;
            }

            // Scan per target in range
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, aggroRange, scanBuffer);

            IDamageable bestTarget = null;
            Transform bestTransform = null;
            float bestPriority = float.MaxValue;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                Collider col = scanBuffer[i];
                if (col == null) continue;

                // Cerca IDamageable
                IDamageable damageable = col.GetComponent<IDamageable>();
                if (damageable == null || !damageable.IsAlive) continue;

                // Non attaccare se stessi
                if (col.gameObject == gameObject) continue;

                // Non attaccare altri nemici
                if (col.GetComponent<EnemyInstance>() != null) continue;

                // Calcola priorità (più basso = meglio)
                float priority = GetTargetPriority(col);
                float dist = Vector3.Distance(transform.position, col.transform.position);

                // Selezione: priorità più bassa, poi distanza
                if (priority < bestPriority || (priority == bestPriority && dist < bestDistance))
                {
                    bestTarget = damageable;
                    bestTransform = col.transform;
                    bestPriority = priority;
                    bestDistance = dist;
                }
            }

            // Se trovato un target migliore, aggiorna
            if (bestTarget != null)
            {
                SetTarget(bestTarget, bestTransform);
            }
            else
            {
                // Nessun target in aggro range - fallback a Waystone
                FallbackToWaystone();
            }
        }

        private float GetTargetPriority(Collider col)
        {
            // Priorità di default basata su TargetPriority di EnemyData
            // Per ora implementazione semplice:
            // Worker = 1, Structure = 2, Waystone = 3

            if (col.GetComponent<WildernessSurvival.Gameplay.Workers.WorkerController>() != null)
                return 1f;

            var structure = col.GetComponent<WildernessSurvival.Gameplay.Structures.StructureController>();
            if (structure != null)
            {
                // Waystone/BaseCenter ha priorità più bassa (va attaccato per ultimo se ci sono altri target)
                if (structure.Data != null && structure.Data.IsBaseCenter)
                    return 3f;
                return 2f;
            }

            // WaystoneBeaconController (legacy)
            if (col.GetComponent<WildernessSurvival.Gameplay.Core.WaystoneBeaconController>() != null)
                return 3f;

            return 10f; // Altri target sconosciuti, bassa priorità
        }

        private void SetTarget(IDamageable target, Transform targetTransform)
        {
            // Evita spam di log se target non cambia
            if (currentTarget == target) return;

            currentTarget = target;
            currentTargetTransform = targetTransform;

            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh && targetTransform != null)
            {
                bool success = agent.SetDestination(targetTransform.position);
                if (debugMode && !success)
                {
                    Debug.LogWarning($"<color=orange>[EnemyInstance]</color> {name} SetDestination failed to {targetTransform.name}");
                }
            }

#if UNITY_EDITOR
            Debug.Log($"<color=red>[Enemy]</color> {gameObject.name} target: {targetTransform?.name ?? "None"}");
#endif
        }

        private void FallbackToWaystone()
        {
            Transform waystoneTransform = null;
            IDamageable waystoneTarget = null;

            // Priorità 1: BaseCenterSystem
            if (BaseCenterSystem.Instance != null && BaseCenterSystem.Instance.HasCenter)
            {
                waystoneTransform = BaseCenterSystem.Instance.CurrentCenter;
                if (waystoneTransform != null)
                {
                    waystoneTarget = waystoneTransform.GetComponent<IDamageable>();
                    if (waystoneTarget == null)
                        waystoneTarget = waystoneTransform.GetComponentInParent<IDamageable>();
                    if (waystoneTarget == null)
                        waystoneTarget = waystoneTransform.GetComponentInChildren<IDamageable>();
                }
            }

            // Fallback 2: Cerca WaystoneBeaconController direttamente in scena
            if (waystoneTarget == null)
            {
                var beacon = FindAnyObjectByType<WildernessSurvival.Gameplay.Core.WaystoneBeaconController>();
                if (beacon != null)
                {
                    waystoneTransform = beacon.transform;
                    waystoneTarget = beacon as IDamageable;

                    if (debugMode)
                    {
                        Debug.Log($"<color=yellow>[EnemyInstance]</color> {name} using fallback: found WaystoneBeaconController directly at {waystoneTransform.position}");
                    }

                    // Registra nel BaseCenterSystem per future ricerche
                    if (BaseCenterSystem.Instance != null && !BaseCenterSystem.Instance.HasCenter)
                    {
                        BaseCenterSystem.Instance.SetCenter(waystoneTransform);
                    }
                }
            }

            // Fallback 3: Cerca qualsiasi oggetto con tag "Waystone" o nome contenente "Waystone"
            if (waystoneTarget == null)
            {
                GameObject waystoneGO = GameObject.FindWithTag("Waystone");
                if (waystoneGO == null)
                {
                    // Cerca per nome (meno efficiente, usato solo come ultima risorsa)
                    var allDamageables = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
                    for (int i = 0; i < allDamageables.Length; i++)
                    {
                        if (allDamageables[i] is IDamageable dmg && allDamageables[i].name.Contains("Waystone"))
                        {
                            waystoneGO = allDamageables[i].gameObject;
                            waystoneTarget = dmg;
                            break;
                        }
                    }
                }
                else
                {
                    waystoneTransform = waystoneGO.transform;
                    waystoneTarget = waystoneGO.GetComponent<IDamageable>();
                }

                if (waystoneTarget != null && debugMode)
                {
                    Debug.Log($"<color=yellow>[EnemyInstance]</color> {name} using fallback: found Waystone by tag/name");
                }
            }

            // Se trovato, imposta target
            if (waystoneTarget != null && waystoneTarget.IsAlive && waystoneTransform != null)
            {
                SetTarget(waystoneTarget, waystoneTransform);
            }
            else if (debugMode)
            {
                Debug.LogWarning($"<color=red>[EnemyInstance]</color> {name} could not find ANY Waystone target!");
            }
        }


        // ============================================
        // COMBAT (B2)
        // ============================================

        private void TryAttack()
        {
            if (attackCooldown > 0f) return;
            if (currentTarget == null || !currentTarget.IsAlive) return;

            // Calcola danno effettivo
            float effectiveDamage = damage * attackMultiplier;

            // Determina tipo danno (usa quello dell'EnemyData se disponibile, altrimenti Physical)
            DamageType damageType = DamageType.Physical;
            // Nota: EnemyData non ha un campo DamageType di attacco, solo weaknesses/resistances
            // Per ora usiamo Physical di default

            // Infliggi danno
            currentTarget.TakeDamage(effectiveDamage, damageType);

            // Reset cooldown
            float interval = enemyData != null ? enemyData.AttackInterval : 1.5f;
            attackCooldown = interval;

#if UNITY_EDITOR
            Debug.Log($"<color=red>[Enemy]</color> {gameObject.name} attacked {currentTargetTransform?.name} for {effectiveDamage:F1} damage");
#endif

            // Se target è morto, rescan
            if (!currentTarget.IsAlive)
            {
                currentTarget = null;
                currentTargetTransform = null;
                targetScanTimer = 0f; // Force immediate rescan
            }
        }

        // ============================================
        // DAMAGEABLE (B1)
        // ============================================

        public EnemyData Data => enemyData;
        public float MoveSpeed => moveSpeed * moveMultiplier;
        public float RewardMultiplier => rewardMultiplier;

        float IDamageable.CurrentHealth => currentHealth;
        float IDamageable.MaxHealth => maxHealth;
        public bool IsAlive => currentHealth > 0;

        public void TakeDamage(float amount)
        {
            TakeDamage(amount, DamageType.None);
        }

        public void TakeDamage(float amount, DamageType damageType)
        {
            if (!IsAlive) return;

            float multiplier = enemyData != null ? enemyData.GetDamageMultiplier(damageType) : 1f;
            float finalDamage = amount * multiplier;

            currentHealth -= finalDamage;

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        protected virtual void Die()
        {
            // Stop NavMesh
            if (agent != null)
            {
                agent.isStopped = true;
                agent.enabled = false;
            }

            // Drop rewards
            int shardDrop = Mathf.RoundToInt(enemyData.BaseShardDrop * rewardMultiplier);
            Debug.Log($"<color=red>[Enemy]</color> {enemyData.DisplayName} died, dropping {shardDrop} shards");

            // Destroy
            Destroy(gameObject);
        }

        // ============================================
        // DEBUFFABLE (IDebuffable)
        // ============================================

        public void ApplyWaystoneDebuff(float moveMul, float atkMul)
        {
            moveMultiplier = moveMul;
            attackMultiplier = atkMul;
            HasWaystoneDebuff = true;

            // Aggiorna NavMeshAgent speed
            if (agent != null)
            {
                agent.speed = moveSpeed * moveMultiplier;
            }
        }

        public void RemoveWaystoneDebuff()
        {
            moveMultiplier = 1f;
            attackMultiplier = 1f;
            HasWaystoneDebuff = false;

            // Ripristina NavMeshAgent speed
            if (agent != null)
            {
                agent.speed = moveSpeed;
            }
        }
    }
}
