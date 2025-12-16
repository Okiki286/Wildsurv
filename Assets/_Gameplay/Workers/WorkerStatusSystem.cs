using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using WildernessSurvival.Core.Events;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// Manager per il sistema Downed/Injured dei worker.
    /// Ascolta OnDayStarted per rialzare i worker downed e tick-are le injuries.
    /// Mettere in scena (es. sotto --- MANAGERS ---).
    /// </summary>
    public class WorkerStatusSystem : MonoBehaviour
    {
        // ============================================
        // SINGLETON
        // ============================================

        public static WorkerStatusSystem Instance { get; private set; }

        // ============================================
        // EVENTS
        // ============================================

        [TitleGroup("Events")]
        [Tooltip("Evento OnDayStarted dal DayNightSystem")]
        [SerializeField]
        [Required]
        private GameEvent onDayStarted;

        // ============================================
        // RUNTIME STATE
        // ============================================

        [TitleGroup("Runtime Status")]
        [ShowInInspector, ReadOnly]
        private List<WorkerDownedStatus> trackedWorkers = new List<WorkerDownedStatus>();

        [ShowInInspector, ReadOnly]
        public int DownedCount => GetDownedCount();

        [ShowInInspector, ReadOnly]
        public int InjuredCount => GetInjuredCount();

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[WorkerStatusSystem] Duplicate instance destroyed!");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            // Subscribe to day started event
            if (onDayStarted != null)
            {
                onDayStarted.AddListener(OnDayStartedHandler);
                Debug.Log("<color=cyan>[WorkerStatusSystem]</color> Subscribed to OnDayStarted");
            }
            else
            {
                Debug.LogError("[WorkerStatusSystem] OnDayStarted event not assigned!");
            }
        }

        private void OnDisable()
        {
            // Unsubscribe
            if (onDayStarted != null)
            {
                onDayStarted.RemoveListener(OnDayStartedHandler);
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
        // EVENT HANDLERS
        // ============================================

        /// <summary>
        /// Chiamato all'inizio di ogni giorno.
        /// 1. Rialza tutti i worker downed
        /// 2. Tick-a le injuries (riduce giorni rimanenti)
        /// </summary>
        private void OnDayStartedHandler()
        {
            Debug.Log("<color=yellow>[WorkerStatusSystem]</color> Day started - processing downed/injured workers...");

            // Aggiorna lista tracked workers
            RefreshTrackedWorkers();

            int revivedCount = 0;
            int injuryTickedCount = 0;

            foreach (var status in trackedWorkers)
            {
                if (status == null) continue;

                // 1. Rialza i downed
                if (status.IsDowned)
                {
                    status.ReviveAtDawn();
                    revivedCount++;
                }
                // 2. Tick injuries (anche quelli appena revived, ma il tick conta dal giorno DOPO)
                // In realtà vogliamo tick-are solo quelli già injured da prima
                // ReviveAtDawn imposta isInjured ma non dovremmo decrementare subito
                // Soluzione: il tick avviene PRIMA del revive
            }

            // Ricalcola: tick injuries per chi era GIA' injured (non appena downato)
            // Approccio: prima tick, poi revive
            // Ma abbiamo già fatto revive... facciamo tick in un secondo passaggio
            // I newly revived hanno injuredDaysRemaining = X, il tick lo decrementerà domani
            // Per chiarezza: il giorno in cui ti rialzi NON conta come giorno di injury
            // L'injury inizia a decrementare dal PROSSIMO OnDayStarted

            // Quindi NON facciamo tick per i newly revived
            // Facciamo tick solo per chi era già injured PRIMA di questo OnDayStarted
            // Ma come sappiamo chi era injured prima?
            // Soluzione: tick PRIMA di revive

            // Correggiamo la logica: processare in ordine
            // 1. Per tutti: se isInjured e NON isDowned -> TickDay
            // 2. Per tutti: se isDowned -> ReviveAtDawn

            // Reset e riprocessa correttamente
            RefreshTrackedWorkers();

            revivedCount = 0;
            injuryTickedCount = 0;

            foreach (var status in trackedWorkers)
            {
                if (status == null) continue;

                // Tick injuries PRIMA (solo se non downed - i downed si rialzano freschi)
                if (status.IsInjured && !status.IsDowned)
                {
                    status.TickDay();
                    injuryTickedCount++;
                }
            }

            foreach (var status in trackedWorkers)
            {
                if (status == null) continue;

                // Revive downed
                if (status.IsDowned)
                {
                    status.ReviveAtDawn();
                    revivedCount++;
                }
            }

            if (revivedCount > 0 || injuryTickedCount > 0)
            {
                Debug.Log($"<color=yellow>[WorkerStatusSystem]</color> Day summary: " +
                    $"{revivedCount} revived, {injuryTickedCount} injuries ticked");
            }
        }

        // ============================================
        // REGISTRATION
        // ============================================

        /// <summary>
        /// Registra un WorkerDownedStatus per il tracking.
        /// Chiamato automaticamente o da WorkerDownedStatus.Start().
        /// </summary>
        public void RegisterWorker(WorkerDownedStatus status)
        {
            if (status != null && !trackedWorkers.Contains(status))
            {
                trackedWorkers.Add(status);
            }
        }

        /// <summary>
        /// Deregistra un WorkerDownedStatus.
        /// Chiamato quando il worker viene distrutto.
        /// </summary>
        public void UnregisterWorker(WorkerDownedStatus status)
        {
            if (status != null)
            {
                trackedWorkers.Remove(status);
            }
        }

        /// <summary>
        /// Trova tutti i WorkerDownedStatus in scena e li registra.
        /// </summary>
        private void RefreshTrackedWorkers()
        {
            trackedWorkers.Clear();
            var allStatus = FindObjectsByType<WorkerDownedStatus>(FindObjectsSortMode.None);
            trackedWorkers.AddRange(allStatus);
        }

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Ottiene tutti i worker attualmente downed.
        /// </summary>
        public List<WorkerDownedStatus> GetDownedWorkers()
        {
            RefreshTrackedWorkers();
            var result = new List<WorkerDownedStatus>();
            foreach (var status in trackedWorkers)
            {
                if (status != null && status.IsDowned)
                {
                    result.Add(status);
                }
            }
            return result;
        }

        /// <summary>
        /// Ottiene tutti i worker attualmente injured.
        /// </summary>
        public List<WorkerDownedStatus> GetInjuredWorkers()
        {
            RefreshTrackedWorkers();
            var result = new List<WorkerDownedStatus>();
            foreach (var status in trackedWorkers)
            {
                if (status != null && status.IsInjured)
                {
                    result.Add(status);
                }
            }
            return result;
        }

        private int GetDownedCount()
        {
            int count = 0;
            foreach (var status in trackedWorkers)
            {
                if (status != null && status.IsDowned) count++;
            }
            return count;
        }

        private int GetInjuredCount()
        {
            int count = 0;
            foreach (var status in trackedWorkers)
            {
                if (status != null && status.IsInjured) count++;
            }
            return count;
        }

        // ============================================
        // DEBUG
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug Actions")]
        [Button("Refresh Tracked Workers", ButtonSizes.Medium)]
        private void DebugRefresh()
        {
            RefreshTrackedWorkers();
            Debug.Log($"[WorkerStatusSystem] Tracking {trackedWorkers.Count} workers");
        }

        [Button("Simulate Day Started", ButtonSizes.Large)]
        [GUIColor(1f, 0.9f, 0.4f)]
        private void DebugSimulateDayStarted()
        {
            if (Application.isPlaying)
            {
                OnDayStartedHandler();
            }
        }

        [Button("Down All Workers", ButtonSizes.Medium)]
        [GUIColor(1f, 0.4f, 0.4f)]
        private void DebugDownAll()
        {
            if (!Application.isPlaying) return;
            RefreshTrackedWorkers();
            foreach (var status in trackedWorkers)
            {
                if (status != null && !status.IsDowned)
                {
                    status.Down();
                }
            }
        }

        [Button("Recover All Workers", ButtonSizes.Medium)]
        [GUIColor(0.4f, 1f, 0.4f)]
        private void DebugRecoverAll()
        {
            if (!Application.isPlaying) return;
            RefreshTrackedWorkers();
            foreach (var status in trackedWorkers)
            {
                if (status != null)
                {
                    status.ForceFullRecovery();
                }
            }
        }

        [Button("Print Status Report", ButtonSizes.Medium)]
        private void DebugPrintStatus()
        {
            RefreshTrackedWorkers();
            Debug.Log($"=== WORKER STATUS REPORT ===\n" +
                $"Total tracked: {trackedWorkers.Count}\n" +
                $"Downed: {GetDownedCount()}\n" +
                $"Injured: {GetInjuredCount()}");

            foreach (var status in trackedWorkers)
            {
                if (status == null) continue;
                string state = status.IsDowned ? "DOWNED" :
                              (status.IsInjured ? $"INJURED ({status.InjuredDaysRemaining}d)" : "OK");
                Debug.Log($"  - {status.gameObject.name}: {state}");
            }
        }
#endif
    }
}
