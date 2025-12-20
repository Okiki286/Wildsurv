using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

namespace WildernessSurvival.Gameplay.Combat
{
    /// <summary>
    /// Minimal combat telemetry for balancing and debugging.
    /// Logs a summary every N seconds when enabled.
    /// Mobile-friendly: no allocations, no LINQ, no FindObjects.
    /// Pooling-safe: uses HashSet to prevent double-registration.
    /// </summary>
    public class CombatTelemetry : MonoBehaviour
    {
        // ============================================
        // SINGLETON
        // ============================================

        public static CombatTelemetry Instance { get; private set; }

        // ============================================
        // CONFIGURATION
        // ============================================

        [TitleGroup("Settings")]
        [Tooltip("Enable telemetry logging")]
        [SerializeField]
        private bool enableTelemetry = false;

        [Tooltip("Log interval in seconds")]
        [SerializeField]
        [PropertyRange(1f, 10f)]
        private float logIntervalSeconds = 2f;

        // ============================================
        // TELEMETRY DATA (reset each night/session)
        // ============================================

        [TitleGroup("Current Session Stats")]
        [ShowInInspector, ReadOnly]
        private int enemiesKilled;

        [ShowInInspector, ReadOnly]
        private float totalTowerDamageDealt;

        [ShowInInspector, ReadOnly]
        private float totalEnemyDamageDealt;

        [ShowInInspector, ReadOnly]
        private int shardsGainedThisSession;

        // [NEW] POOLING-SAFE: Use HashSet to track unique instances
        // Prevents double-registration when OnEnable is called multiple times
        private readonly HashSet<int> registeredEnemyIds = new HashSet<int>();
        private readonly HashSet<int> registeredTowerIds = new HashSet<int>();

        [ShowInInspector, ReadOnly]
        private int EnemiesAliveCount => registeredEnemyIds.Count;

        [ShowInInspector, ReadOnly]
        private int TowersAliveCount => registeredTowerIds.Count;

        // ============================================
        // RUNTIME
        // ============================================

        private float nextLogTime;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (!enableTelemetry) return;

            if (Time.time >= nextLogTime)
            {
                LogTelemetrySummary();
                nextLogTime = Time.time + logIntervalSeconds;
            }
        }

        // ============================================
        // PUBLIC API - Called by combat systems
        // ============================================

        /// <summary>
        /// Record tower damage dealt (called from TowerAttack)
        /// </summary>
        public void RecordTowerDamage(float damage)
        {
            totalTowerDamageDealt += damage;
        }

        /// <summary>
        /// Record enemy damage dealt (called from EnemyInstance)
        /// </summary>
        public void RecordEnemyDamage(float damage)
        {
            totalEnemyDamageDealt += damage;
        }

        /// <summary>
        /// Record enemy kill (called from EnemyInstance.Die)
        /// </summary>
        public void RecordEnemyKill()
        {
            enemiesKilled++;
        }

        /// <summary>
        /// Record shards gained (called from EnemyInstance.DropRewards)
        /// </summary>
        public void RecordShardsGained(int amount)
        {
            shardsGainedThisSession += amount;
        }

        /// <summary>
        /// [MODIFY] Register a new enemy (called from EnemyInstance.OnEnable).
        /// Pooling-safe: uses HashSet to prevent double-registration.
        /// </summary>
        public void RegisterEnemy(GameObject enemyObj)
        {
            if (enemyObj == null) return;

            int id = enemyObj.GetInstanceID();
            if (registeredEnemyIds.Add(id))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"<color=cyan>[CombatTelemetry]</color> Enemy registered: {enemyObj.name} (total: {registeredEnemyIds.Count})");
#endif
            }
        }

        /// <summary>
        /// [MODIFY] Unregister an enemy (called from EnemyInstance.OnDisable).
        /// Pooling-safe: only decrements if instance was registered.
        /// </summary>
        public void UnregisterEnemy(GameObject enemyObj)
        {
            if (enemyObj == null) return;

            int id = enemyObj.GetInstanceID();
            if (registeredEnemyIds.Remove(id))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"<color=orange>[CombatTelemetry]</color> Enemy unregistered: {enemyObj.name} (total: {registeredEnemyIds.Count})");
#endif
            }
        }

        /// <summary>
        /// [MODIFY] Register a new tower (called from TowerAttack.OnEnable).
        /// Pooling-safe: uses HashSet to prevent double-registration.
        /// </summary>
        public void RegisterTower(GameObject towerObj)
        {
            if (towerObj == null) return;

            int id = towerObj.GetInstanceID();
            if (registeredTowerIds.Add(id))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"<color=cyan>[CombatTelemetry]</color> Tower registered: {towerObj.name} (total: {registeredTowerIds.Count})");
#endif
            }
        }

        /// <summary>
        /// [MODIFY] Unregister a tower (called from TowerAttack.OnDisable).
        /// Pooling-safe: only decrements if instance was registered.
        /// </summary>
        public void UnregisterTower(GameObject towerObj)
        {
            if (towerObj == null) return;

            int id = towerObj.GetInstanceID();
            if (registeredTowerIds.Remove(id))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"<color=orange>[CombatTelemetry]</color> Tower unregistered: {towerObj.name} (total: {registeredTowerIds.Count})");
#endif
            }
        }

        /// <summary>
        /// Reset session stats (call at night start or new wave)
        /// </summary>
        [Button("Reset Session Stats")]
        public void ResetSession()
        {
            enemiesKilled = 0;
            totalTowerDamageDealt = 0f;
            totalEnemyDamageDealt = 0f;
            shardsGainedThisSession = 0;
            // Note: registeredEnemyIds and registeredTowerIds are NOT reset (they persist)
        }

        // ============================================
        // TELEMETRY OUTPUT
        // ============================================

        private void LogTelemetrySummary()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // OPTIMIZATION: Use cached counters instead of FindObjectsByType (mobile-friendly)
            // Counters are updated via Register/Unregister calls from components

            // Calculate DPS
            float sessionTime = Mathf.Max(Time.time, 1f);
            float towerDPS = totalTowerDamageDealt / sessionTime;
            float enemyDPS = totalEnemyDamageDealt / sessionTime;

            Debug.Log($"<color=magenta>[CombatTelemetry]</color> " +
                $"Enemies: {EnemiesAliveCount} alive, {enemiesKilled} killed | " +
                $"Towers: {TowersAliveCount} | " +
                $"DPS: Tower={towerDPS:F1}, Enemy={enemyDPS:F1} | " +
                $"Shards: +{shardsGainedThisSession}");
#endif
        }

        // ============================================
        // DEBUG BUTTONS
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug")]
        [Button("Force Log Now")]
        private void DebugForceLog()
        {
            LogTelemetrySummary();
        }
#endif
    }
}
