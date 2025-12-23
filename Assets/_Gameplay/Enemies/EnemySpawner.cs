using UnityEngine;
using Sirenix.OdinInspector;
using WildernessSurvival.Core.Systems;
using WildernessSurvival.Gameplay.Structures;

namespace WildernessSurvival.Gameplay.Enemies
{
    /// <summary>
    /// Utility per spawnare nemici con stats scalate.
    /// Istanzia prefab e applica moltiplicatori di wave.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        // ============================================
        // SINGLETON
        // ============================================

        public static EnemySpawner Instance { get; private set; }

        // ============================================
        // SETTINGS
        // ============================================

        [TitleGroup("Settings")]
        [Tooltip("Parent transform per nemici spawnati (per organizzazione gerarchia)")]
        [SerializeField] private Transform enemyContainer;

        [TitleGroup("Debug")]
        [SerializeField] private bool debugMode = false;

        // ============================================
        // STATS
        // ============================================

        [TitleGroup("Runtime Stats")]
        [ShowInInspector]
        [ReadOnly]
        private int totalSpawnedThisWave = 0;

        [ShowInInspector]
        [ReadOnly]
        private int totalSpawnedEver = 0;

        // ============================================
        // CACHED REFERENCES (PERF OPTIMIZATION)
        // ============================================

        [TitleGroup("Cached References")]
        [ShowInInspector]
        [ReadOnly]
        private Transform cachedWaystoneTarget;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[EnemySpawner] Multiple instances found!");
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Create container if not assigned
            if (enemyContainer == null)
            {
                GameObject container = new GameObject("_SpawnedEnemies");
                enemyContainer = container.transform;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            // Cache waystone target ONCE at startup to avoid runtime FindObjectByType
            CacheWaystoneTarget();
        }

        /// <summary>
        /// Caches the waystone target transform once at startup.
        /// Priority: BaseCenterSystem > WaystoneDebuffAura fallback search.
        /// </summary>
        private void CacheWaystoneTarget()
        {
            // Priority 1: Try BaseCenterSystem
            if (BaseCenterSystem.Instance != null && BaseCenterSystem.Instance.HasCenter)
            {
                cachedWaystoneTarget = BaseCenterSystem.Instance.CurrentCenter;
                if (debugMode)
                {
                    Debug.Log($"<color=red>[EnemySpawner]</color> Cached waystone target from BaseCenterSystem: {cachedWaystoneTarget?.name}");
                }
                return;
            }

            // Priority 2: Fallback search for WaystoneDebuffAura (once at startup, not per-spawn)
            var aura = FindAnyObjectByType<WaystoneDebuffAura>();
            if (aura != null)
            {
                cachedWaystoneTarget = aura.transform;
                if (debugMode)
                {
                    Debug.Log($"<color=yellow>[EnemySpawner]</color> Cached waystone target from WaystoneDebuffAura: {cachedWaystoneTarget?.name}");
                }
                return;
            }

            if (debugMode)
            {
                Debug.LogWarning("[EnemySpawner] Could not find waystone target at startup!");
            }
        }

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Spawna un nemico con stats scalate
        /// </summary>
        /// <param name="data">Dati del nemico</param>
        /// <param name="position">Posizione spawn</param>
        /// <param name="hpMultiplier">Moltiplicatore HP (wave)</param>
        /// <param name="dmgMultiplier">Moltiplicatore danno (wave)</param>
        /// <param name="speedMultiplier">Moltiplicatore velocità (wave)</param>
        /// <param name="rewardMultiplier">Moltiplicatore ricompense (wave)</param>
        /// <returns>GameObject del nemico spawnato, null se fallito</returns>
        public GameObject Spawn(
            EnemyData data,
            Vector3 position,
            float hpMultiplier = 1f,
            float dmgMultiplier = 1f,
            float speedMultiplier = 1f,
            float rewardMultiplier = 1f)
        {
            // Validate
            if (data == null)
            {
                Debug.LogError("[EnemySpawner] Cannot spawn: EnemyData is null!");
                return null;
            }

            if (data.Prefab == null)
            {
                Debug.LogError($"[EnemySpawner] Cannot spawn '{data.DisplayName}': Prefab is null!");
                return null;
            }

            // === NAVMESH SNAP ===
            // Validate spawn position against NavMesh before instantiation
            Vector3 validSpawnPos = position;
            if (!UnityEngine.AI.NavMesh.SamplePosition(position, out UnityEngine.AI.NavMeshHit hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
            {
                if (debugMode)
                {
                    Debug.LogWarning($"[EnemySpawner] Spawn position {position} not on NavMesh within 5m, trying 10m...");
                }
                
                if (!UnityEngine.AI.NavMesh.SamplePosition(position, out hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    Debug.LogError($"[EnemySpawner] Cannot find NavMesh for {data.DisplayName} at {position}! Skipping spawn.");
                    return null;
                }
            }
            validSpawnPos = hit.position;

            if (debugMode && Vector3.Distance(position, validSpawnPos) > 0.1f)
            {
                Debug.Log($"<color=yellow>[EnemySpawner]</color> Snapped {data.DisplayName} from {position} to NavMesh at {validSpawnPos} (delta={Vector3.Distance(position, validSpawnPos):F2}m)");
            }

            // [MODIFY] POOLING: Get from pool instead of Instantiate
            GameObject enemy = null;
            if (EnemyPooler.Instance != null)
            {
                enemy = EnemyPooler.Instance.GetEnemy(data.Prefab, validSpawnPos, Quaternion.identity);
            }
            else
            {
                // Fallback: Instantiate if pooler not available (backward compatibility)
                enemy = Instantiate(data.Prefab, validSpawnPos, Quaternion.identity, enemyContainer);
                Debug.LogWarning("[EnemySpawner] EnemyPooler not found! Using Instantiate fallback (not optimal for performance).");
            }

            if (enemy == null)
            {
                Debug.LogError($"[EnemySpawner] Failed to spawn {data.DisplayName}!");
                return null;
            }

            enemy.name = $"{data.DisplayName}_{totalSpawnedEver}";

            // Apply scaled stats
            ApplyScaledStats(enemy, data, hpMultiplier, dmgMultiplier, speedMultiplier, rewardMultiplier);

            // Update counters
            totalSpawnedThisWave++;
            totalSpawnedEver++;

            if (debugMode)
            {
                Debug.Log($"<color=red>[EnemySpawner]</color> Spawned {data.DisplayName} at {validSpawnPos} " +
                    $"(HP:{hpMultiplier:F2}x, DMG:{dmgMultiplier:F2}x, SPD:{speedMultiplier:F2}x)");
            }

            return enemy;
        }


        /// <summary>
        /// Spawna un nemico da un Transform point
        /// </summary>
        public GameObject SpawnAt(EnemyData data, Transform spawnPoint,
            float hpMul = 1f, float dmgMul = 1f, float spdMul = 1f, float rwdMul = 1f)
        {
            if (spawnPoint == null)
            {
                Debug.LogError("[EnemySpawner] SpawnPoint is null!");
                return null;
            }

            return Spawn(data, spawnPoint.position, hpMul, dmgMul, spdMul, rwdMul);
        }

        /// <summary>
        /// Resetta contatore wave (chiamato da WaveManager a inizio wave)
        /// </summary>
        public void ResetWaveCounter()
        {
            totalSpawnedThisWave = 0;
        }

        /// <summary>
        /// Numero di nemici spawnati in questa wave
        /// </summary>
        public int TotalSpawnedThisWave => totalSpawnedThisWave;

        /// <summary>
        /// Numero totale di nemici spawnati in questa partita
        /// </summary>
        public int TotalSpawnedEver => totalSpawnedEver;

        // ============================================
        // STATS APPLICATION
        // ============================================

        private void ApplyScaledStats(GameObject enemy, EnemyData data,
            float hpMul, float dmgMul, float spdMul, float rwdMul)
        {
            // Priority 1: EnemyController (new system with IDebuffable + NavMesh)
            var enemyController = enemy.GetComponent<EnemyController>();
            if (enemyController != null)
            {
                // [PERF] Pass cached target to avoid runtime FindAnyObjectByType
                enemyController.Initialize(data, hpMul, dmgMul, spdMul, rwdMul, cachedWaystoneTarget);
                return;
            }

            // Priority 2: Legacy EnemyInstance
            var enemyInstance = enemy.GetComponent<EnemyInstance>();
            if (enemyInstance != null)
            {
                enemyInstance.Initialize(data, hpMul, dmgMul, spdMul, rwdMul);
                return;
            }

            // Priority 3: Generic IHealth component
            var healthComponent = enemy.GetComponent<IHealth>();
            if (healthComponent != null)
            {
                float scaledHealth = data.BaseHealth * hpMul;
                healthComponent.SetMaxHealth(scaledHealth);
            }

            // If no recognized components found, log warning
            if (enemyController == null && enemyInstance == null && healthComponent == null)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"[EnemySpawner] No stat components found on '{enemy.name}'. " +
                        "Stats will use prefab defaults. Add EnemyController, EnemyInstance, or IHealth component.");
                }
            }
        }

        // ============================================
        // DEBUG
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug")]
        [Button("Clear All Enemies", ButtonSizes.Medium)]
        [GUIColor(1f, 0.4f, 0.4f)]
        private void DebugClearAllEnemies()
        {
            if (enemyContainer == null) return;

            int count = enemyContainer.childCount;
            while (enemyContainer.childCount > 0)
            {
                DestroyImmediate(enemyContainer.GetChild(0).gameObject);
            }

            Debug.Log($"[EnemySpawner] Cleared {count} enemies");
        }
#endif
    }

    // ============================================
    // INTERFACES (for extensibility)
    // ============================================

    /// <summary>
    /// Interfaccia per componenti con salute
    /// </summary>
    public interface IHealth
    {
        void SetMaxHealth(float health);
        float GetCurrentHealth();
        void TakeDamage(float damage);
    }
}
