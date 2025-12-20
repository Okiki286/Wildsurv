using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Sirenix.OdinInspector;
using WildernessSurvival.Gameplay.Combat;
using WildernessSurvival.Gameplay.Resources;
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
        // Staggered scan: ogni nemico inizia con delay random per distribuire il carico CPU
        private bool hasInitializedScanDelay = false;

        // Throttling for NavMesh destination updates (mobile optimization)
        private Vector3 lastSetDestination = Vector3.positiveInfinity;
        private const float DESTINATION_UPDATE_THRESHOLD = 0.5f; // Update only if target moved >0.5m

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
        // REWARDS CONFIG
        // ============================================

        /// <summary>
        /// How shards are granted to the player on death
        /// </summary>
        public enum ShardDropMode
        {
            DirectToInventory,  // Immediately add to player's ResourceSystem
            WorldDropLater      // Spawn pickup in world (future feature)
        }

        public const string RES_SHARDS = "shard";  // Must match Shard.asset resourceId

        [TitleGroup("Rewards")]
        [SerializeField]
        private ShardDropMode shardDropMode = ShardDropMode.DirectToInventory;

        private bool hasDroppedRewards = false;

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
        [SerializeField] private bool debugNav = false;  // Extended NavMesh diagnostics

        // Stuck detection state
        private float stuckTimer = 0f;
        private const float STUCK_THRESHOLD = 0.75f;
        private const float STUCK_VELOCITY_MIN = 0.05f;

        // Melee attack constants
        private const float MELEE_RANGE_TOLERANCE = 0.1f;  // Tight tolerance for true melee
        [TitleGroup("Debug")]
        [SerializeField] private bool debugCombat = false;  // Combat range logging

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        // [NEW] POOLING-SAFE: Register with telemetry on enable + RESET STATE
        private void OnEnable()
        {
            CombatTelemetry.Instance?.RegisterEnemy(gameObject);

            // ✅ CRITICAL: Reset completo dello stato per pooling
            // Solo se già inizializzato (non alla prima spawn)
            if (isInitialized)
            {
                ResetStateForPooling();
            }
        }

        // [NEW] POOLING-SAFE: Unregister from telemetry on disable
        private void OnDisable()
        {
            CombatTelemetry.Instance?.UnregisterEnemy(gameObject);
        }

        /// <summary>
        /// [POOLING-SAFE] Resetta TUTTO lo stato del nemico per il riutilizzo.
        /// Chiamato in OnEnable() ad ogni riattivazione dal pool.
        /// </summary>
        private void ResetStateForPooling()
        {
            // ===== RESET COMBAT STATE =====
            currentHealth = maxHealth;  // ✅ Reset HP
            attackCooldown = 0f;
            targetScanTimer = hasInitializedScanDelay 
                ? UnityEngine.Random.Range(0f, TARGET_SCAN_INTERVAL) 
                : 0f;
            currentTarget = null;
            currentTargetTransform = null;
            lastSetDestination = Vector3.positiveInfinity;

            // ===== RESET DEBUFF STATE =====
            moveMultiplier = 1f;
            attackMultiplier = 1f;
            HasWaystoneDebuff = false;

            // ===== RESET FLAGS =====
            hasDroppedRewards = false;  // ✅ Permette drop rewards
            stuckTimer = 0f;

            // ===== RESET NAVMESH AGENT =====
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.velocity = Vector3.zero;
                agent.ResetPath();
                
                // Re-apply speed (potrebbe essere stato debuffato)
                agent.speed = moveSpeed;
            }

            // ===== ACQUIRE INITIAL TARGET =====
            FallbackToWaystone();
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

            // Combat logic - MELEE ATTACK SYSTEM
            if (currentTargetTransform != null && currentTarget != null)
            {
                // Calculate real distance using sqrMagnitude (more performant)
                Vector3 toTarget = currentTargetTransform.position - transform.position;
                float sqrDistanceToTarget = toTarget.sqrMagnitude;
                float effectiveAttackRange = enemyData != null ? enemyData.AttackRange : 1.0f;  // Fallback 1m melee
                float attackRangeWithTolerance = effectiveAttackRange + MELEE_RANGE_TOLERANCE;
                float sqrAttackRange = attackRangeWithTolerance * attackRangeWithTolerance;

                // Also check agent.remainingDistance as backup (handles NavMesh path length)
                float agentRemainingDist = (agent != null && agent.hasPath && !agent.pathPending) 
                    ? agent.remainingDistance 
                    : Mathf.Sqrt(sqrDistanceToTarget);

                // In melee range check: BOTH direct distance AND agent path must be close
                bool isInMeleeRange = sqrDistanceToTarget <= sqrAttackRange && agentRemainingDist <= attackRangeWithTolerance;

                // Debug logging for combat diagnostics
                if (debugCombat)
                {
                    float realDist = Mathf.Sqrt(sqrDistanceToTarget);
                    Debug.Log($"<color=magenta>[Combat]</color> {name}: dist={realDist:F2}m, remaining={agentRemainingDist:F2}m, range={effectiveAttackRange:F1}m, inRange={isInMeleeRange}, cooldown={attackCooldown:F2}s");
                }

                if (isInMeleeRange)
                {
                    // IN MELEE RANGE - Stop and attack
                    if (agent != null && agent.isOnNavMesh)
                    {
                        agent.isStopped = true;
                    }

                    // Face target
                    if (toTarget.sqrMagnitude > 0.01f)
                    {
                        Vector3 lookDir = toTarget;
                        lookDir.y = 0f;
                        if (lookDir.sqrMagnitude > 0.01f)
                        {
                            transform.rotation = Quaternion.Slerp(
                                transform.rotation,
                                Quaternion.LookRotation(lookDir),
                                Time.deltaTime * 10f
                            );
                        }
                    }

                    // Try melee attack (respects cooldown)
                    TryAttack();
                }
                else
                {
                    // OUT OF RANGE - Chase target
                    if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
                    {
                        agent.isStopped = false;

                        // OPTIMIZATION: Only update destination if target moved significantly
                        Vector3 targetPos = currentTargetTransform.position;
                        float sqrDistToLastDest = (targetPos - lastSetDestination).sqrMagnitude;
                        float threshold = DESTINATION_UPDATE_THRESHOLD * DESTINATION_UPDATE_THRESHOLD;

                        if (sqrDistToLastDest > threshold)
                        {
                            agent.SetDestination(targetPos);
                            lastSetDestination = targetPos;
                        }
                    }
                }
            }
            else
            {
                // No valid target - find Waystone as fallback
                FallbackToWaystone();
            }

            // Stuck detection - ALWAYS runs for recovery
            DetectAndRecoverFromStuck();
        }

        // ============================================
        // INITIALIZATION
        // ============================================

        /// <summary>
        /// Inizializza il nemico con stats scalate.
        /// Chiamato SOLO una volta dal pooler alla creazione.
        /// </summary>
        public void Initialize(EnemyData data, float hpMul, float dmgMul, float spdMul, float rwdMul)
        {
            enemyData = data;

            // Calculate scaled stats (ONE-TIME)
            maxHealth = data.BaseHealth * hpMul;
            damage = data.AttackDamage * dmgMul;
            moveSpeed = data.MoveSpeed * spdMul;
            rewardMultiplier = rwdMul;

            // Apply NavMeshAgent settings (ONE-TIME)
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.speed = moveSpeed;
                agent.stoppingDistance = data.AttackRange * 0.9f;
            }

            // Ensure we're on NavMesh
            EnsureOnNavMesh();

            // Staggered scan: ogni nemico inizia con delay random per distribuire il carico CPU
            if (!hasInitializedScanDelay)
            {
                targetScanTimer = UnityEngine.Random.Range(0f, TARGET_SCAN_INTERVAL);
                hasInitializedScanDelay = true;
            }

            // Mark as initialized BEFORE ResetState (check in OnEnable)
            isInitialized = true;

            // ✅ PRIMO RESET (per la prima spawn)
            // I successivi reset avverranno in OnEnable()
            ResetStateForPooling();

            // Debug diagnostics (extended NavMesh logging)
            if (debugNav)
            {
                StartCoroutine(LogNavDiagnostics());
            }
            else if (debugMode)
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

        /// <summary>
        /// Extended NavMesh diagnostics - logs detailed agent state for 3 seconds after spawn.
        /// </summary>
        private IEnumerator LogNavDiagnostics()
        {
            float elapsed = 0f;
            while (elapsed < 3f)
            {
                if (agent != null)
                {
                    string targetName = currentTargetTransform != null ? currentTargetTransform.name : "NULL";
                    float targetDist = currentTargetTransform != null ? Vector3.Distance(transform.position, currentTargetTransform.position) : -1f;
                    
                    Debug.Log($"<color=cyan>[NavDiag]</color> {name} t={elapsed:F1}s " +
                        $"enabled={agent.enabled} onMesh={agent.isOnNavMesh} " +
                        $"hasPath={agent.hasPath} status={agent.pathStatus} pending={agent.pathPending} " +
                        $"stopped={agent.isStopped} speed={agent.speed:F1} accel={agent.acceleration} " +
                        $"remaining={agent.remainingDistance:F1} stopDist={agent.stoppingDistance:F1} " +
                        $"vel={agent.velocity.magnitude:F2} " +
                        $"pos={transform.position} nextPos={agent.nextPosition} " +
                        $"updatePos={agent.updatePosition} updateRot={agent.updateRotation} " +
                        $"target={targetName} dist={targetDist:F1}");
                }
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
            }
        }

        /// <summary>
        /// Detects if the agent is stuck and attempts recovery.
        /// Stuck = hasPath, remainingDistance > stoppingDistance, velocity near 0 for > 0.75s
        /// </summary>
        private void DetectAndRecoverFromStuck()
        {
            if (agent == null || !agent.isOnNavMesh) return;

            bool isStuck = agent.hasPath
                && !agent.pathPending
                && agent.remainingDistance > agent.stoppingDistance + 0.5f
                && agent.velocity.magnitude < STUCK_VELOCITY_MIN;

            if (isStuck)
            {
                stuckTimer += Time.deltaTime;

                if (stuckTimer >= STUCK_THRESHOLD)
                {
                    string targetName = currentTargetTransform != null ? currentTargetTransform.name : "NULL";
                    Debug.LogWarning($"<color=red>[STUCK]</color> {name} stuck for {stuckTimer:F1}s! " +
                        $"pos={transform.position} hasPath={agent.hasPath} remaining={agent.remainingDistance:F1} " +
                        $"vel={agent.velocity.magnitude:F2} stopped={agent.isStopped} " +
                        $"updatePos={agent.updatePosition} updateRot={agent.updateRotation} " +
                        $"target={targetName}");

                    // Recovery attempt
                    TryRecoverFromStuck();
                    stuckTimer = 0f;
                }
            }
            else
            {
                stuckTimer = 0f;
            }
        }

        /// <summary>
        /// Attempts to recover from a stuck state by re-warping and re-pathing.
        /// </summary>
        private void TryRecoverFromStuck()
        {
            if (agent == null) return;

            // Step 1: Reset path
            agent.ResetPath();

            // Step 2: Re-warp to nearest NavMesh point (small nudge)
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }

            // Step 3: Ensure agent is not stopped
            agent.isStopped = false;

            // Step 4: Re-acquire target
            if (currentTargetTransform != null)
            {
                bool success = agent.SetDestination(currentTargetTransform.position);
                Debug.Log($"<color=yellow>[RECOVERY]</color> {name} recovery attempted: warped to {hit.position}, SetDestination={success}");
            }
            else
            {
                Debug.Log($"<color=yellow>[RECOVERY]</color> {name} recovery attempted: warped to {hit.position}, no target to re-path");
                // Try to find waystone again
                FallbackToWaystone();
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

#if UNITY_EDITOR
            if (debugMode)
            {
                Debug.Log($"<color=yellow>[Scan]</color> {name}: found {hitCount} colliders in {aggroRange}m range");
            }
#endif

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

#if UNITY_EDITOR
                if (debugMode && damageable == null)
                {
                    // Log only potential targets (not ground, etc)
                    var workerCtrl = col.GetComponent<WildernessSurvival.Gameplay.Workers.WorkerController>();
                    if (workerCtrl != null)
                    {
                        Debug.Log($"<color=orange>[Scan]</color> {name}: Worker {col.name} has NO IDamageable component! Add WorkerDamageable.");
                    }
                }
#endif

                if (damageable == null || !damageable.IsAlive) continue;

                // Non attaccare se stessi
                if (col.gameObject == gameObject) continue;

                // Non attaccare altri nemici
                if (col.GetComponent<EnemyInstance>() != null) continue;

                // Skip workers that are sheltered (in house during night)
                var workerController = col.GetComponent<WildernessSurvival.Gameplay.Workers.WorkerController>();
                if (workerController != null && workerController.IsInShelter)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (debugMode)
                    {
                        Debug.Log($"<color=gray>[Scan]</color> {name}: Skipping {col.name} - worker is SHELTERED");
                    }
#endif
                    continue;
                }

                // Calcola priorità (più basso = meglio)
                float priority = GetTargetPriority(col);
                float dist = Vector3.Distance(transform.position, col.transform.position);

#if UNITY_EDITOR
                if (debugMode)
                {
                    Debug.Log($"<color=green>[Scan]</color> {name}: Valid target {col.name} - priority={priority}, dist={dist:F1}m");
                }
#endif

                // Selezione: priorità più bassa, poi distanza
                if (priority < bestPriority || (priority == bestPriority && dist < bestDistance))
                {
                    bestTarget = damageable;
                    bestTransform = col.transform;
                    bestPriority = priority;
                    bestDistance = dist;
                }
            }

            // Pulizia buffer per evitare riferimenti stale (safe per GC)
            for (int i = 0; i < hitCount; i++)
            {
                scanBuffer[i] = null;
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

            // Telemetry
            CombatTelemetry.Instance?.RecordEnemyDamage(effectiveDamage);

            // Reset cooldown
            float interval = enemyData != null ? enemyData.AttackInterval : 1.5f;
            attackCooldown = interval;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugCombat)
            {
                Debug.Log($"<color=red>[Enemy]</color> {gameObject.name} attacked {currentTargetTransform?.name} | " +
                    $"baseDmg={damage:F1} × atkMult={attackMultiplier:F2} = finalDmg={effectiveDamage:F1} | " +
                    $"debuffed={HasWaystoneDebuff}");
            }
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
            // ========== REWARD DROP (with guard against double-drop) ==========
            if (!hasDroppedRewards)
            {
                hasDroppedRewards = true;
                DropRewards();

                // Telemetry - record kill
                CombatTelemetry.Instance?.RecordEnemyKill();
                // [REMOVED] UnregisterEnemy - now handled by OnDisable (pooling-safe)
            }

            // Stop NavMesh
            if (agent != null)
            {
                agent.isStopped = true;
                agent.enabled = false;
            }

            // [POOLING] Return to pool instead of destroying
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

        /// <summary>
        /// Handles shard reward crediting. Called once from Die().
        /// </summary>
        private void DropRewards()
        {
            if (enemyData == null) return;

            int shardDrop = Mathf.RoundToInt(enemyData.BaseShardDrop * rewardMultiplier);
            string enemyName = enemyData.DisplayName;

            if (shardDropMode == ShardDropMode.DirectToInventory)
            {
                if (ResourceSystem.Instance != null)
                {
                    // Validate resource key exists
                    if (ResourceSystem.Instance.GetResourceData(RES_SHARDS) == null)
                    {
                        Debug.LogWarning($"<color=orange>[EnemyReward]</color> WARNING: resource key '{RES_SHARDS}' not found in ResourceSystem");
                        return;
                    }

                    float shardsBefore = ResourceSystem.Instance.GetResourceAmount(RES_SHARDS);
                    ResourceSystem.Instance.AddResource(RES_SHARDS, shardDrop);
                    float shardsAfter = ResourceSystem.Instance.GetResourceAmount(RES_SHARDS);

                    Debug.Log($"<color=green>[EnemyReward]</color> {enemyName} shards BEFORE={shardsBefore:F0} +{shardDrop} AFTER={shardsAfter:F0}");

                    // Telemetry
                    CombatTelemetry.Instance?.RecordShardsGained(shardDrop);
                }
                else
                {
                    Debug.LogWarning($"<color=orange>[EnemyReward]</color> ResourceSystem.Instance is null! Cannot credit {shardDrop} shards.");
                }
            }
            else // WorldDropLater
            {
                Debug.Log($"<color=yellow>[EnemyReward]</color> TODO WorldDropLater shards={shardDrop}");
            }
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

        // ============================================
        // DEBUG DIAGNOSTICS
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug Actions")]
        [Button("🔍 Dump Nav Debug", ButtonSizes.Large)]
        [GUIColor(1f, 0.6f, 0.2f)]
#endif
        public void DumpNavDebug()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("═══════════════════════════════════════════════════════");
            sb.AppendLine($"[NAV DEBUG] {name} @ Frame {Time.frameCount}");
            sb.AppendLine("═══════════════════════════════════════════════════════");

            // ENEMY POSITIONS
            sb.AppendLine("\n▶ ENEMY POSITIONS:");
            sb.AppendLine($"   transform.position (WORLD):  {transform.position}");
            sb.AppendLine($"   transform.localPosition:     {transform.localPosition}");
            if (transform.parent != null)
            {
                sb.AppendLine($"   parent.name:                 {transform.parent.name}");
                sb.AppendLine($"   parent.position:             {transform.parent.position}");
            }

            // NAVMESH AGENT STATE
            sb.AppendLine("\n▶ NAVMESH AGENT:");
            if (agent != null)
            {
                sb.AppendLine($"   agent.enabled:           {agent.enabled}");
                sb.AppendLine($"   agent.isOnNavMesh:       {agent.isOnNavMesh}");
                sb.AppendLine($"   agent.isActiveAndEnabled:{agent.isActiveAndEnabled}");
                sb.AppendLine($"   agent.isStopped:         {agent.isStopped}");
                sb.AppendLine($"   agent.hasPath:           {agent.hasPath}");
                sb.AppendLine($"   agent.pathPending:       {agent.pathPending}");
                sb.AppendLine($"   agent.pathStatus:        {agent.pathStatus}");
                sb.AppendLine($"   agent.velocity:          {agent.velocity} (mag={agent.velocity.magnitude:F2})");
                sb.AppendLine($"   agent.desiredVelocity:   {agent.desiredVelocity}");
                sb.AppendLine($"   agent.speed:             {agent.speed}");
                sb.AppendLine($"   agent.stoppingDistance:  {agent.stoppingDistance}");
                sb.AppendLine($"   agent.remainingDistance: {agent.remainingDistance:F2}");
                sb.AppendLine($"   agent.destination:       {agent.destination}");
                sb.AppendLine($"   agent.nextPosition:      {agent.nextPosition}");
                sb.AppendLine($"   agent.updatePosition:    {agent.updatePosition}");
                sb.AppendLine($"   agent.updateRotation:    {agent.updateRotation}");
            }
            else
            {
                sb.AppendLine("   [NULL AGENT!]");
            }

            // TARGET INFO
            sb.AppendLine("\n▶ TARGET:");
            if (currentTargetTransform != null)
            {
                sb.AppendLine($"   target.name:              {currentTargetTransform.name}");
                sb.AppendLine($"   target.position (WORLD):  {currentTargetTransform.position}");
                sb.AppendLine($"   target.localPosition:     {currentTargetTransform.localPosition}");
                if (currentTargetTransform.parent != null)
                {
                    sb.AppendLine($"   target.parent.name:       {currentTargetTransform.parent.name}");
                }

                // Try to get collider bounds
                var targetCol = currentTargetTransform.GetComponent<Collider>();
                if (targetCol != null)
                {
                    sb.AppendLine($"   target.collider.bounds.center: {targetCol.bounds.center}");
                    sb.AppendLine($"   target.collider.bounds.size:   {targetCol.bounds.size}");
                }
            }
            else
            {
                sb.AppendLine("   [NULL TARGET TRANSFORM!]");
            }
            sb.AppendLine($"   currentTarget (IDamageable): {(currentTarget != null ? "VALID" : "NULL")}");

            // DISTANCE CALCULATIONS
            sb.AppendLine("\n▶ DISTANCES:");
            if (currentTargetTransform != null)
            {
                Vector3 enemyPos = transform.position;
                Vector3 targetPos = currentTargetTransform.position;

                float dist3D = Vector3.Distance(enemyPos, targetPos);
                Vector3 diff = targetPos - enemyPos;
                float distXZ = Mathf.Sqrt(diff.x * diff.x + diff.z * diff.z);
                float deltaY = Mathf.Abs(diff.y);

                sb.AppendLine($"   Vector3.Distance (3D):    {dist3D:F2}m");
                sb.AppendLine($"   Distance XZ (ignore Y):   {distXZ:F2}m");
                sb.AppendLine($"   Delta Y:                  {deltaY:F2}m");

                if (agent != null && agent.hasPath)
                {
                    sb.AppendLine($"   agent.remainingDistance:  {agent.remainingDistance:F2}m");
                }

                float attackRange = enemyData != null ? enemyData.AttackRange : 1.0f;
                float rangeWithTol = attackRange + MELEE_RANGE_TOLERANCE;
                bool inRange = dist3D <= rangeWithTol;
                sb.AppendLine($"\n   attackRange (from data):  {attackRange:F2}m");
                sb.AppendLine($"   rangeWithTolerance:       {rangeWithTol:F2}m");
                sb.AppendLine($"   IS IN MELEE RANGE?        {(inRange ? "✅ YES" : "❌ NO")}");
            }

            // ENEMY COLLIDER
            sb.AppendLine("\n▶ ENEMY COLLIDER:");
            var myCol = GetComponent<Collider>();
            if (myCol != null)
            {
                sb.AppendLine($"   collider.bounds.center: {myCol.bounds.center}");
                sb.AppendLine($"   collider.bounds.size:   {myCol.bounds.size}");
            }
            else
            {
                sb.AppendLine($"   [NO COLLIDER ON ROOT]");
            }

            sb.AppendLine("═══════════════════════════════════════════════════════");

            Debug.Log(sb.ToString());
        }

        // ============================================
        // GIZMOS (Editor only)
        // ============================================

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw melee attack range
            float attackRange = enemyData != null ? enemyData.AttackRange : 1.5f;
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // Draw aggro range
            float aggroRange = enemyData != null ? enemyData.AggroRange : 10f;
            Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
            Gizmos.DrawWireSphere(transform.position, aggroRange);

            // Draw line to current target (only if debugCombat enabled)
            if (debugCombat && currentTargetTransform != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, currentTargetTransform.position);
            }
        }
#endif
    }
}
