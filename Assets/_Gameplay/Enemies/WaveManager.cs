using UnityEngine;
using System.Collections;
using Sirenix.OdinInspector;
using WildernessSurvival.Core.Events;
using WildernessSurvival.Core.Systems;

namespace WildernessSurvival.Gameplay.Enemies
{
    /// <summary>
    /// Gestisce le wave notturne di nemici.
    /// Si attiva su NightStarted, spawna secondo WaveData, si ferma su DayStarted.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        // ============================================
        // SINGLETON
        // ============================================

        public static WaveManager Instance { get; private set; }

        // ============================================
        // REFERENCES
        // ============================================

        [TitleGroup("References")]
        [Tooltip("Riferimento a EnemySpawner. Se vuoto, cerca automaticamente.")]
        [SerializeField] private EnemySpawner enemySpawner;

        [Tooltip("Riferimento a EnemySpawnPointProvider. Se vuoto, cerca automaticamente.")]
        [SerializeField] private EnemySpawnPointProvider spawnPointProvider;

        [TitleGroup("Game Events")]
        [Tooltip("Evento che indica inizio notte")]
        [SerializeField] private GameEvent onNightStarted;

        [Tooltip("Evento che indica inizio giorno (ferma spawning)")]
        [SerializeField] private GameEvent onDayStarted;

        [Tooltip("Evento fine notte (alternativa stop) - opzionale")]
        [SerializeField] private GameEvent onNightEnding;

        // ============================================
        // WAVE DATA
        // ============================================

        [TitleGroup("Wave Configuration")]
        [InfoBox("Lista di wave. L'indice viene determinato da CurrentDayNumber-1 (clampato).")]
        [SerializeField] private WaveData[] waves;

        [TitleGroup("Settings")]
        [Tooltip("Ferma spawning anche su NightEnding (oltre che DayStarted)")]
        [SerializeField] private bool stopOnNightEnding = false;

        [Tooltip("Se non ci sono wave definite, usa wave[0] come fallback")]
        [SerializeField] private bool loopWaves = true;

        // ============================================
        // DEBUG
        // ============================================

        [TitleGroup("Debug")]
        [SerializeField] private bool debugMode = true;

        // ============================================
        // RUNTIME STATE
        // ============================================

        [TitleGroup("Runtime Status")]
        [ShowInInspector]
        [ReadOnly]
        private WaveState currentWaveState = WaveState.Idle;

        [ShowInInspector]
        [ReadOnly]
        private int currentWaveIndex = -1;

        [ShowInInspector]
        [ReadOnly]
        private int enemiesSpawnedThisWave = 0;

        [ShowInInspector]
        [ReadOnly]
        private int totalEnemiesThisWave = 0;

        private Coroutine waveCoroutine;
        private bool isInitialized = false;

        // ============================================
        // ENUMS
        // ============================================

        public enum WaveState
        {
            Idle,           // No wave in progress
            Preparing,      // Waiting preparationTime
            Spawning,       // Actively spawning enemies
            Completed,      // Wave finished
            Interrupted     // Stopped early (day started)
        }

        // ============================================
        // PROPERTIES
        // ============================================

        public WaveState CurrentState => currentWaveState;
        public int CurrentWaveIndex => currentWaveIndex;
        public bool IsWaveActive => currentWaveState == WaveState.Preparing || currentWaveState == WaveState.Spawning;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            // Singleton
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[WaveManager] Multiple instances found!");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Auto-find references
            if (enemySpawner == null)
            {
                enemySpawner = FindFirstObjectByType<EnemySpawner>();
            }

            if (spawnPointProvider == null)
            {
                spawnPointProvider = FindFirstObjectByType<EnemySpawnPointProvider>();
            }
        }

        private void Start()
        {
            // Validate
            if (!ValidateSetup())
            {
                return;
            }

            isInitialized = true;

            // Subscribe to events
            SubscribeToEvents();

            if (debugMode)
            {
                Debug.Log("<color=magenta>[WaveManager]</color> Initialized successfully");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            UnsubscribeFromEvents();
        }

        // ============================================
        // VALIDATION
        // ============================================

        private bool ValidateSetup()
        {
            bool valid = true;

            if (enemySpawner == null)
            {
                Debug.LogError("[WaveManager] EnemySpawner not found! WaveManager disabled.");
                valid = false;
            }

            if (spawnPointProvider == null)
            {
                Debug.LogWarning("[WaveManager] SpawnPointProvider not found. Will need WaveData.specificSpawnPoints.");
            }

            if (waves == null || waves.Length == 0)
            {
                Debug.LogWarning("[WaveManager] No waves configured! Add WaveData assets to the waves array.");
            }

            if (onNightStarted == null)
            {
                Debug.LogError("[WaveManager] onNightStarted event not assigned! WaveManager disabled.");
                valid = false;
            }

            if (!valid)
            {
                enabled = false;
            }

            return valid;
        }

        // ============================================
        // EVENT SUBSCRIPTION
        // ============================================

        private void SubscribeToEvents()
        {
            if (onNightStarted != null)
            {
                onNightStarted.AddListener(OnNightStarted);
            }

            if (onDayStarted != null)
            {
                onDayStarted.AddListener(OnDayStarted);
            }

            if (onNightEnding != null && stopOnNightEnding)
            {
                onNightEnding.AddListener(OnNightEnding);
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (onNightStarted != null)
            {
                onNightStarted.RemoveListener(OnNightStarted);
            }

            if (onDayStarted != null)
            {
                onDayStarted.RemoveListener(OnDayStarted);
            }

            if (onNightEnding != null)
            {
                onNightEnding.RemoveListener(OnNightEnding);
            }
        }

        // ============================================
        // EVENT HANDLERS
        // ============================================

        private void OnNightStarted()
        {
            if (!isInitialized) return;

            if (debugMode)
            {
                Debug.Log("<color=magenta>[WaveManager]</color> Night started - beginning wave");
            }

            StartWave();
        }

        private void OnDayStarted()
        {
            if (!isInitialized) return;

            if (debugMode)
            {
                Debug.Log("<color=magenta>[WaveManager]</color> Day started - stopping wave");
            }

            StopWave();
        }

        private void OnNightEnding()
        {
            if (!isInitialized || !stopOnNightEnding) return;

            if (debugMode)
            {
                Debug.Log("<color=magenta>[WaveManager]</color> Night ending - stopping wave");
            }

            StopWave();
        }

        // ============================================
        // WAVE CONTROL
        // ============================================

        /// <summary>
        /// Avvia una nuova wave
        /// </summary>
        public void StartWave()
        {
            if (waveCoroutine != null)
            {
                StopCoroutine(waveCoroutine);
            }

            // Determine wave index based on day number
            int dayNumber = DayNightSystem.Instance != null ? DayNightSystem.Instance.CurrentDayNumber : 1;
            currentWaveIndex = GetWaveIndexForDay(dayNumber);

            WaveData wave = GetCurrentWaveData();

            if (wave == null)
            {
                Debug.LogWarning("[WaveManager] No WaveData available for this night!");
                return;
            }

            // Reset spawner counter
            if (enemySpawner != null)
            {
                enemySpawner.ResetWaveCounter();
            }

            enemiesSpawnedThisWave = 0;
            totalEnemiesThisWave = wave.GetTotalEnemyCount();

            waveCoroutine = StartCoroutine(RunWaveCoroutine(wave));
        }

        /// <summary>
        /// Ferma la wave corrente
        /// </summary>
        public void StopWave()
        {
            if (waveCoroutine != null)
            {
                StopCoroutine(waveCoroutine);
                waveCoroutine = null;
            }

            if (currentWaveState == WaveState.Spawning || currentWaveState == WaveState.Preparing)
            {
                currentWaveState = WaveState.Interrupted;

                if (debugMode)
                {
                    Debug.Log($"<color=magenta>[WaveManager]</color> Wave interrupted. Spawned {enemiesSpawnedThisWave}/{totalEnemiesThisWave} enemies.");
                }
            }
        }

        // ============================================
        // WAVE COROUTINE
        // ============================================

        private IEnumerator RunWaveCoroutine(WaveData wave)
        {
            // === PREPARATION PHASE ===
            currentWaveState = WaveState.Preparing;

            if (debugMode)
            {
                Debug.Log($"<color=magenta>[WaveManager]</color> Wave {wave.WaveNumber} preparation: {wave.PreparationTime}s");
            }

            yield return new WaitForSeconds(wave.PreparationTime);

            // === SPAWNING PHASE ===
            currentWaveState = WaveState.Spawning;

            if (debugMode)
            {
                Debug.Log($"<color=magenta>[WaveManager]</color> Wave {wave.WaveNumber} spawning started! " +
                    $"({wave.GetTotalEnemyCount()} enemies over {wave.SpawnDuration}s)");
            }

            // Process each enemy group
            if (wave.EnemyGroups != null)
            {
                foreach (var group in wave.EnemyGroups)
                {
                    // Wait for group delay
                    if (group.spawnDelay > 0)
                    {
                        yield return new WaitForSeconds(group.spawnDelay);
                    }

                    // Spawn enemies in this group
                    yield return StartCoroutine(SpawnGroupCoroutine(group, wave));
                }
            }

            // Spawn boss if configured
            if (wave.HasBoss && wave.BossEnemy != null)
            {
                SpawnEnemy(wave.BossEnemy, wave);
                enemiesSpawnedThisWave++;

                if (debugMode)
                {
                    Debug.Log($"<color=red>[WaveManager]</color> BOSS SPAWNED: {wave.BossEnemy.DisplayName}");
                }
            }

            // === COMPLETED ===
            currentWaveState = WaveState.Completed;

            if (debugMode)
            {
                Debug.Log($"<color=magenta>[WaveManager]</color> Wave {wave.WaveNumber} completed! " +
                    $"Spawned {enemiesSpawnedThisWave} enemies.");
            }

            waveCoroutine = null;
        }

        private IEnumerator SpawnGroupCoroutine(WaveEnemyGroup group, WaveData wave)
        {
            if (group.enemyData == null)
            {
                Debug.LogWarning("[WaveManager] EnemyGroup has null enemyData, skipping.");
                yield break;
            }

            float interval = group.spawnInterval > 0 ? group.spawnInterval : 0.5f;

            for (int i = 0; i < group.count; i++)
            {
                // Check if wave was interrupted
                if (currentWaveState != WaveState.Spawning)
                {
                    yield break;
                }

                SpawnEnemy(group.enemyData, wave);
                enemiesSpawnedThisWave++;

                // Wait interval (except for last enemy)
                if (i < group.count - 1)
                {
                    yield return new WaitForSeconds(interval);
                }
            }
        }

        // ============================================
        // SPAWNING
        // ============================================

        private void SpawnEnemy(EnemyData enemyData, WaveData wave)
        {
            // Get spawn position
            Vector3 spawnPos = GetSpawnPosition();

            if (spawnPos == Vector3.zero && spawnPointProvider == null)
            {
                Debug.LogError("[WaveManager] No spawn position available! Cannot spawn enemy.");
                return;
            }

            // Get wave multipliers
            float hpMul = wave.HealthMultiplier;
            float dmgMul = wave.DamageMultiplier;
            float spdMul = wave.SpeedMultiplier;
            float rwdMul = wave.RewardMultiplier;

            // Spawn via EnemySpawner
            if (enemySpawner != null)
            {
                enemySpawner.Spawn(enemyData, spawnPos, hpMul, dmgMul, spdMul, rwdMul);
            }
        }

        private Vector3 GetSpawnPosition()
        {
            // Priority: SpawnPointProvider
            if (spawnPointProvider != null && spawnPointProvider.SpawnPointCount > 0)
            {
                return spawnPointProvider.GetRandomSpawnPosition();
            }

            // Fallback: random position around center (not ideal)
            Debug.LogWarning("[WaveManager] No spawn points available, using random fallback position.");
            Vector2 randomCircle = Random.insideUnitCircle * 20f;
            return new Vector3(randomCircle.x, 0f, randomCircle.y);
        }

        // ============================================
        // HELPERS
        // ============================================

        private int GetWaveIndexForDay(int dayNumber)
        {
            if (waves == null || waves.Length == 0)
            {
                return -1;
            }

            int index = dayNumber - 1; // Day 1 = Wave 0

            if (loopWaves)
            {
                index = index % waves.Length;
            }
            else
            {
                index = Mathf.Clamp(index, 0, waves.Length - 1);
            }

            return index;
        }

        private WaveData GetCurrentWaveData()
        {
            if (currentWaveIndex < 0 || waves == null || waves.Length == 0)
            {
                return null;
            }

            if (currentWaveIndex >= waves.Length)
            {
                return loopWaves ? waves[0] : null;
            }

            return waves[currentWaveIndex];
        }

        // ============================================
        // DEBUG
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug Actions")]
        [ButtonGroup("Debug Actions/Row")]
        [Button("Force Start Wave", ButtonSizes.Medium)]
        [GUIColor(0.4f, 0.9f, 0.4f)]
        private void DebugForceStartWave()
        {
            StartWave();
        }

        [ButtonGroup("Debug Actions/Row")]
        [Button("Stop Wave", ButtonSizes.Medium)]
        [GUIColor(0.9f, 0.4f, 0.4f)]
        private void DebugStopWave()
        {
            StopWave();
        }

        [Button("Log Wave Info", ButtonSizes.Medium)]
        private void DebugLogWaveInfo()
        {
            WaveData wave = GetCurrentWaveData();
            if (wave != null)
            {
                Debug.Log($"[WaveManager] Current Wave: {wave.WaveId}\n" +
                    $"  Enemies: {wave.GetTotalEnemyCount()}\n" +
                    $"  Prep Time: {wave.PreparationTime}s\n" +
                    $"  Spawn Duration: {wave.SpawnDuration}s\n" +
                    $"  Has Boss: {wave.HasBoss}");
            }
            else
            {
                Debug.Log("[WaveManager] No wave data available");
            }
        }
#endif
    }
}
