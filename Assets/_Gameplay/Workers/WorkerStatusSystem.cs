using UnityEngine;
using Sirenix.OdinInspector;
using WildernessSurvival.Core.Events;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// Sistema che gestisce il ciclo downed/revive di tutti i worker.
    /// Deve vivere in scena (es. su --- MANAGERS ---).
    /// Ascolta OnDayStarted per rilanciare i worker e decrementare injury.
    /// </summary>
    public class WorkerStatusSystem : MonoBehaviour
    {
        // ============================================
        // SINGLETON
        // ============================================

        public static WorkerStatusSystem Instance { get; private set; }

        // ============================================
        // SETTINGS
        // ============================================

        [TitleGroup("Event References")]
        [Required("Assegna l'evento OnDayStarted per attivare il revive!")]
        [SerializeField] private GameEvent onDayStarted;

        [TitleGroup("Debug")]
        [SerializeField] private bool debugMode = true;

        // ============================================
        // STATE
        // ============================================

        [TitleGroup("Runtime Stats")]
        [ShowInInspector, ReadOnly]
        private int downedWorkerCount = 0;

        [ShowInInspector, ReadOnly]
        private int injuredWorkerCount = 0;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[WorkerStatusSystem] Duplicate instance found!");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Subscribe to day started event
            if (onDayStarted != null)
            {
                onDayStarted.AddListener(OnDayStartedHandler);
                if (debugMode)
                {
                    Debug.Log("<color=cyan>[WorkerStatusSystem]</color> Subscribed to OnDayStarted");
                }
            }
            else
            {
                Debug.LogError("[WorkerStatusSystem] OnDayStarted event not assigned! Worker revive won't work.");
            }

            // Auto-find if not assigned
            if (onDayStarted == null)
            {
                TryAutoFindDayEvent();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (onDayStarted != null)
            {
                onDayStarted.RemoveListener(OnDayStartedHandler);
            }
        }

        // ============================================
        // EVENT HANDLERS
        // ============================================

        private void OnDayStartedHandler()
        {
            if (debugMode)
            {
                Debug.Log("<color=cyan>[WorkerStatusSystem]</color> Day started - processing downed/injured workers");
            }

            ProcessAllWorkers();
        }

        // ============================================
        // CORE LOGIC
        // ============================================

        /// <summary>
        /// Processa tutti i worker: rilancia i downed, tick injury per tutti.
        /// </summary>
        private void ProcessAllWorkers()
        {
            // Trova tutti i WorkerDownedStatus in scena
            var allDownedStatuses = FindObjectsByType<WorkerDownedStatus>(FindObjectsSortMode.None);

            int revivedCount = 0;
            int tickedCount = 0;

            for (int i = 0; i < allDownedStatuses.Length; i++)
            {
                var status = allDownedStatuses[i];
                if (status == null) continue;

                // Prima: revive i downed
                if (status.IsDowned)
                {
                    status.ReviveAtDawn();
                    revivedCount++;
                }

                // Poi: tick injury (include i nuovi injured dal revive)
                if (status.IsInjured)
                {
                    status.TickDay();
                    tickedCount++;
                }
            }

            // Aggiorna contatori
            UpdateStats();

            if (debugMode && (revivedCount > 0 || tickedCount > 0))
            {
                Debug.Log($"<color=cyan>[WorkerStatusSystem]</color> Revived {revivedCount}, Ticked injury {tickedCount}");
            }
        }

        /// <summary>
        /// Aggiorna le statistiche runtime.
        /// </summary>
        private void UpdateStats()
        {
            var allDownedStatuses = FindObjectsByType<WorkerDownedStatus>(FindObjectsSortMode.None);

            downedWorkerCount = 0;
            injuredWorkerCount = 0;

            for (int i = 0; i < allDownedStatuses.Length; i++)
            {
                var status = allDownedStatuses[i];
                if (status == null) continue;

                if (status.IsDowned) downedWorkerCount++;
                if (status.IsInjured) injuredWorkerCount++;
            }
        }

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Ottiene il numero di worker attualmente downed.
        /// </summary>
        public int GetDownedCount()
        {
            UpdateStats();
            return downedWorkerCount;
        }

        /// <summary>
        /// Ottiene il numero di worker attualmente injured.
        /// </summary>
        public int GetInjuredCount()
        {
            UpdateStats();
            return injuredWorkerCount;
        }

        /// <summary>
        /// Forza revive di tutti i downed (cheat/debug).
        /// </summary>
        public void ForceReviveAll()
        {
            var allDownedStatuses = FindObjectsByType<WorkerDownedStatus>(FindObjectsSortMode.None);

            for (int i = 0; i < allDownedStatuses.Length; i++)
            {
                var status = allDownedStatuses[i];
                if (status != null && status.IsDowned)
                {
                    status.ReviveAtDawn();
                }
            }

            Debug.Log("<color=lime>[WorkerStatusSystem]</color> Force revived all downed workers");
        }

        /// <summary>
        /// Forza recovery totale di tutti i worker (cheat/debug).
        /// </summary>
        public void ForceRecoverAll()
        {
            var allDownedStatuses = FindObjectsByType<WorkerDownedStatus>(FindObjectsSortMode.None);

            for (int i = 0; i < allDownedStatuses.Length; i++)
            {
                var status = allDownedStatuses[i];
                if (status != null)
                {
                    status.ForceFullRecovery();
                }
            }

            Debug.Log("<color=lime>[WorkerStatusSystem]</color> Force recovered all workers");
        }

        // ============================================
        // AUTO-FIND
        // ============================================

        private void TryAutoFindDayEvent()
        {
            // Prova a trovare l'evento da Resources o da altri manager
            var allEvents = UnityEngine.Resources.FindObjectsOfTypeAll<GameEvent>();
            for (int i = 0; i < allEvents.Length; i++)
            {
                if (allEvents[i].name.Contains("DayStarted") || allEvents[i].name.Contains("OnDay"))
                {
                    onDayStarted = allEvents[i];
                    onDayStarted.AddListener(OnDayStartedHandler);
                    Debug.Log($"<color=yellow>[WorkerStatusSystem]</color> Auto-found day event: {onDayStarted.name}");
                    return;
                }
            }
        }

        // ============================================
        // DEBUG
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug Actions")]
        [Button("Force Revive All", ButtonSizes.Medium)]
        [GUIColor(0.4f, 1f, 0.4f)]
        private void DebugForceReviveAll()
        {
            if (Application.isPlaying) ForceReviveAll();
        }

        [Button("Force Recover All", ButtonSizes.Medium)]
        [GUIColor(0.4f, 0.8f, 1f)]
        private void DebugForceRecoverAll()
        {
            if (Application.isPlaying) ForceRecoverAll();
        }

        [Button("Update Stats", ButtonSizes.Medium)]
        private void DebugUpdateStats()
        {
            if (Application.isPlaying)
            {
                UpdateStats();
                Debug.Log($"[WorkerStatusSystem] Downed: {downedWorkerCount}, Injured: {injuredWorkerCount}");
            }
        }

        [Button("Simulate Day Started", ButtonSizes.Medium)]
        [GUIColor(1f, 0.8f, 0.4f)]
        private void DebugSimulateDayStarted()
        {
            if (Application.isPlaying) OnDayStartedHandler();
        }
#endif
    }
}
