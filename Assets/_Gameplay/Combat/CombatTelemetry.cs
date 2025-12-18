using UnityEngine;
using Sirenix.OdinInspector;

namespace WildernessSurvival.Gameplay.Combat
{
    /// <summary>
    /// Minimal combat telemetry for balancing and debugging.
    /// Logs a summary every N seconds when enabled.
    /// Mobile-friendly: no allocations, no LINQ.
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
        /// Reset session stats (call at night start or new wave)
        /// </summary>
        [Button("Reset Session Stats")]
        public void ResetSession()
        {
            enemiesKilled = 0;
            totalTowerDamageDealt = 0f;
            totalEnemyDamageDealt = 0f;
            shardsGainedThisSession = 0;
        }

        // ============================================
        // TELEMETRY OUTPUT
        // ============================================

        private void LogTelemetrySummary()
        {
            // Count alive enemies and towers (no LINQ, use FindObjectsByType)
            int enemiesAlive = 0;
            int towersAlive = 0;

            var enemies = FindObjectsByType<Enemies.EnemyInstance>(FindObjectsSortMode.None);
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i].IsAlive) enemiesAlive++;
            }

            var towers = FindObjectsByType<Structures.TowerAttack>(FindObjectsSortMode.None);
            towersAlive = towers.Length;  // TowerAttack only exists on operational towers

            // Calculate DPS
            float sessionTime = Mathf.Max(Time.time, 1f);
            float towerDPS = totalTowerDamageDealt / sessionTime;
            float enemyDPS = totalEnemyDamageDealt / sessionTime;

            Debug.Log($"<color=magenta>[CombatTelemetry]</color> " +
                $"Enemies: {enemiesAlive} alive, {enemiesKilled} killed | " +
                $"Towers: {towersAlive} | " +
                $"DPS: Tower={towerDPS:F1}, Enemy={enemyDPS:F1} | " +
                $"Shards: +{shardsGainedThisSession}");
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
