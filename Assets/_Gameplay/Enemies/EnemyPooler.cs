using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace WildernessSurvival.Gameplay.Enemies
{
    /// <summary>
    /// Mobile-optimized object pooler for enemies.
    /// Eliminates Instantiate/Destroy spikes during waves.
    /// Integrates with CombatTelemetry via OnEnable/OnDisable.
    /// </summary>
    public class EnemyPooler : MonoBehaviour
    {
        // ============================================
        // SINGLETON
        // ============================================

        public static EnemyPooler Instance { get; private set; }

        // ============================================
        // CONFIGURATION
        // ============================================

        [TitleGroup("Pool Settings")]
        [Tooltip("Default enemy prefab for pool (can be override per EnemyData)")]
        [SerializeField] private GameObject defaultEnemyPrefab;

        [Tooltip("Initial pool size (pre-warmed at start)")]
        [SerializeField]
        [Range(5, 50)]
        private int initialPoolSize = 20;

        [Tooltip("Allow pool to expand if all instances are active")]
        [SerializeField] private bool canExpand = true;

        [Tooltip("Max pool size (0 = unlimited)")]
        [SerializeField]
        [Range(0, 100)]
        private int maxPoolSize = 50;

        [TitleGroup("Performance")]
        [Tooltip("Parent transform for pooled enemies (for organization)")]
        [SerializeField] private Transform poolContainer;

        [TitleGroup("Debug")]
        [SerializeField] private bool debugMode = false;

        // ============================================
        // RUNTIME STATE
        // ============================================

        // Multi-pool system: one pool per prefab
        private readonly Dictionary<GameObject, Queue<GameObject>> prefabPools = new Dictionary<GameObject, Queue<GameObject>>();
        private readonly Dictionary<GameObject, GameObject> activeEnemies = new Dictionary<GameObject, GameObject>(); // instance -> prefab

        [TitleGroup("Runtime Stats")]
        [ShowInInspector, ReadOnly]
        private int TotalPooledInstances => GetTotalPooledCount();

        [ShowInInspector, ReadOnly]
        private int ActiveInstances => activeEnemies.Count;

        [ShowInInspector, ReadOnly]
        private int AvailableInstances => TotalPooledInstances - ActiveInstances;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[EnemyPooler] Duplicate instance found! Destroying.");
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Create container if not assigned
            if (poolContainer == null)
            {
                GameObject container = new GameObject("_EnemyPool");
                poolContainer = container.transform;
                poolContainer.SetParent(transform);
            }

            // Pre-warm default pool
            if (defaultEnemyPrefab != null)
            {
                PrewarmPool(defaultEnemyPrefab, initialPoolSize);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Get an enemy from the pool at specified position.
        /// Creates pool on-demand if prefab not seen before.
        /// </summary>
        public GameObject GetEnemy(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                Debug.LogError("[EnemyPooler] Cannot get enemy: prefab is null!");
                return null;
            }

            // Ensure pool exists for this prefab
            if (!prefabPools.ContainsKey(prefab))
            {
                CreatePoolForPrefab(prefab);
            }

            GameObject enemy = null;
            Queue<GameObject> pool = prefabPools[prefab];

            // Try to get inactive instance from pool
            while (pool.Count > 0)
            {
                enemy = pool.Dequeue();
                
                // Check if instance still valid (not destroyed)
                if (enemy != null)
                {
                    break;
                }
            }

            // Expand pool if needed
            if (enemy == null)
            {
                if (canExpand && (maxPoolSize == 0 || GetPoolSizeForPrefab(prefab) < maxPoolSize))
                {
                    enemy = CreateNewInstance(prefab);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (debugMode)
                    {
                        Debug.Log($"<color=yellow>[EnemyPooler]</color> Pool expanded for {prefab.name} (new size: {GetPoolSizeForPrefab(prefab)})");
                    }
#endif
                }
                else
                {
                    Debug.LogWarning($"[EnemyPooler] Pool exhausted for {prefab.name} and cannot expand! Consider increasing pool size.");
                    return null;
                }
            }

            // Activate and configure
            return ActivateEnemy(enemy, prefab, position, rotation);
        }

        /// <summary>
        /// Return enemy to pool (called by EnemyInstance when dying).
        /// </summary>
        public void ReturnEnemy(GameObject enemy)
        {
            if (enemy == null) return;

            // Find which prefab this instance belongs to
            if (!activeEnemies.TryGetValue(enemy, out GameObject prefab))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (debugMode)
                {
                    Debug.LogWarning($"[EnemyPooler] Tried to return enemy {enemy.name} but it's not tracked as active!");
                }
#endif
                return;
            }

            // Remove from active tracking
            activeEnemies.Remove(enemy);

            // Deactivate (triggers OnDisable -> unregister from telemetry)
            enemy.SetActive(false);

            // Return to pool
            if (prefabPools.ContainsKey(prefab))
            {
                prefabPools[prefab].Enqueue(enemy);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (debugMode)
                {
                    Debug.Log($"<color=cyan>[EnemyPooler]</color> {enemy.name} returned to pool (available: {AvailableInstances})");
                }
#endif
            }
        }

        /// <summary>
        /// Pre-warm pool for a specific prefab.
        /// Call this during loading screen to avoid spawn spikes.
        /// </summary>
        public void PrewarmPool(GameObject prefab, int count)
        {
            if (prefab == null) return;

            if (!prefabPools.ContainsKey(prefab))
            {
                CreatePoolForPrefab(prefab);
            }

            var pool = prefabPools[prefab];
            int currentSize = GetPoolSizeForPrefab(prefab);
            int toCreate = Mathf.Max(0, count - currentSize);

            for (int i = 0; i < toCreate; i++)
            {
                GameObject instance = CreateNewInstance(prefab);
                instance.SetActive(false);
                pool.Enqueue(instance);
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"<color=green>[EnemyPooler]</color> Pre-warmed {count} instances for {prefab.name}");
#endif
        }

        // ============================================
        // PRIVATE HELPERS
        // ============================================

        private void CreatePoolForPrefab(GameObject prefab)
        {
            prefabPools[prefab] = new Queue<GameObject>();
        }

        private GameObject CreateNewInstance(GameObject prefab)
        {
            GameObject instance = Instantiate(prefab, poolContainer);
            instance.name = $"{prefab.name}_pooled";
            return instance;
        }

        private GameObject ActivateEnemy(GameObject enemy, GameObject prefab, Vector3 position, Quaternion rotation)
        {
            // Set transform
            enemy.transform.position = position;
            enemy.transform.rotation = rotation;

            // CRITICAL: Reset NavMeshAgent for mobile (prevents "sliding" bug)
            if (enemy.TryGetComponent(out NavMeshAgent agent))
            {
                agent.enabled = false; // Disable first to force Warp
                enemy.SetActive(true); // Activate (triggers OnEnable -> register telemetry)
                agent.enabled = true;
                agent.Warp(position); // Force immediate position
            }
            else
            {
                enemy.SetActive(true);
            }

            // Track as active
            activeEnemies[enemy] = prefab;

            return enemy;
        }

        private int GetTotalPooledCount()
        {
            int total = 0;
            foreach (var queue in prefabPools.Values)
            {
                total += queue.Count;
            }
            return total + activeEnemies.Count;
        }

        private int GetPoolSizeForPrefab(GameObject prefab)
        {
            if (!prefabPools.ContainsKey(prefab))
                return 0;

            int inPool = prefabPools[prefab].Count;
            int active = 0;

            foreach (var kvp in activeEnemies)
            {
                if (kvp.Value == prefab)
                    active++;
            }

            return inPool + active;
        }

        // ============================================
        // DEBUG
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug Actions")]
        [Button("Force Return All Enemies", ButtonSizes.Medium)]
        [GUIColor(0.9f, 0.4f, 0.4f)]
        private void DebugReturnAllEnemies()
        {
            // Copy to avoid modification during iteration
            var activeList = new List<GameObject>(activeEnemies.Keys);
            
            foreach (var enemy in activeList)
            {
                ReturnEnemy(enemy);
            }

            Debug.Log($"[EnemyPooler] Returned {activeList.Count} enemies to pool");
        }

        [Button("Log Pool Stats", ButtonSizes.Medium)]
        private void DebugLogPoolStats()
        {
            Debug.Log($"[EnemyPooler] Pool Stats:\n" +
                $"  Total Instances: {TotalPooledInstances}\n" +
                $"  Active: {ActiveInstances}\n" +
                $"  Available: {AvailableInstances}\n" +
                $"  Prefab Pools: {prefabPools.Count}");

            foreach (var kvp in prefabPools)
            {
                Debug.Log($"  - {kvp.Key.name}: {kvp.Value.Count} available, {GetPoolSizeForPrefab(kvp.Key)} total");
            }
        }
#endif
    }
}
