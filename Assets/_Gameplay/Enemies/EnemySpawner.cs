using UnityEngine;
using Sirenix.OdinInspector;

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

            // Instantiate
            GameObject enemy = Instantiate(data.Prefab, position, Quaternion.identity, enemyContainer);
            enemy.name = $"{data.DisplayName}_{totalSpawnedEver}";

            // Apply scaled stats
            ApplyScaledStats(enemy, data, hpMultiplier, dmgMultiplier, speedMultiplier, rewardMultiplier);

            // Update counters
            totalSpawnedThisWave++;
            totalSpawnedEver++;

            if (debugMode)
            {
                Debug.Log($"<color=red>[EnemySpawner]</color> Spawned {data.DisplayName} at {position} " +
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
            // Look for common enemy components and apply stats
            // This is a placeholder - adapt to your actual enemy component structure

            // Try to find EnemyInstance component (if exists)
            var enemyInstance = enemy.GetComponent<EnemyInstance>();
            if (enemyInstance != null)
            {
                enemyInstance.Initialize(data, hpMul, dmgMul, spdMul, rwdMul);
                return;
            }

            // Try generic approach - look for common components
            // Health component
            var healthComponent = enemy.GetComponent<IHealth>();
            if (healthComponent != null)
            {
                float scaledHealth = data.BaseHealth * hpMul;
                healthComponent.SetMaxHealth(scaledHealth);
            }

            // If no recognized components found, log warning (not error - doesn't crash)
            if (enemyInstance == null && healthComponent == null)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"[EnemySpawner] No stat components found on '{enemy.name}'. " +
                        "Stats will use prefab defaults. Add EnemyInstance or IHealth component for scaling.");
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
