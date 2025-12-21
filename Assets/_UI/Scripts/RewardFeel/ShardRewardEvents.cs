using UnityEngine;
using System;
using WildernessSurvival.Gameplay.Resources;

namespace WildernessSurvival.UI.RewardFeel
{
    /// <summary>
    /// [LEGACY] Evento per shards guadagnati.
    /// NOTA: Questo sistema è stato sostituito da EconomyFeedbackSystem.
    /// Mantenuto per compatibilità con componenti esistenti (ShardPopupSpawner, ShardHUDPulse, etc.)
    /// che si sottoscrivono a OnShardsGained.
    ///
    /// Il bridge automatico si sottoscrive a ResourceSystem.OnResourceChanged e
    /// inoltra gli eventi shard a questo sistema legacy.
    /// </summary>
    public static class ShardRewardEvents
    {
        private static bool bridgeInitialized = false;
        // ============================================
        // EVENTO PRINCIPALE
        // ============================================

        /// <summary>
        /// Evento invocato quando shards vengono guadagnati.
        /// Parametri: amount, worldPosition
        /// </summary>
        public static event Action<int, Vector3> OnShardsGained;

        // ============================================
        // BATCHING STATE
        // ============================================

        private static int batchedAmount = 0;
        private static Vector3 batchedPosition = Vector3.zero;
        private static float lastRaiseTime = -1f;
        private const float BATCH_WINDOW = 0.05f; // 50ms batch window

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Invoca l'evento OnShardsGained.
        /// Chiamato da EnemyInstance.DropRewards() dopo l'accredito.
        /// </summary>
        /// <param name="amount">Quantità di shards guadagnati</param>
        /// <param name="worldPosition">Posizione mondo del nemico morto</param>
        public static void RaiseShardsGained(int amount, Vector3 worldPosition)
        {
            if (amount <= 0) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"<color=magenta>[ShardRewardEvents]</color> RaiseShardsGained amount={amount} pos={worldPosition}");
#endif

            OnShardsGained?.Invoke(amount, worldPosition);
        }

        /// <summary>
        /// Versione batched - accumula shards in finestra temporale.
        /// Utile per kill multipli quasi simultanei.
        /// </summary>
        public static void RaiseShardsGainedBatched(int amount, Vector3 worldPosition)
        {
            if (amount <= 0) return;

            float currentTime = Time.time;

            // Se siamo dentro la finestra di batch, accumula
            if (currentTime - lastRaiseTime < BATCH_WINDOW && lastRaiseTime > 0)
            {
                batchedAmount += amount;
                // Mantieni la posizione media (o ultima)
                batchedPosition = worldPosition;
            }
            else
            {
                // Nuova finestra - invia batch precedente se presente
                if (batchedAmount > 0)
                {
                    OnShardsGained?.Invoke(batchedAmount, batchedPosition);
                }

                // Inizia nuovo batch
                batchedAmount = amount;
                batchedPosition = worldPosition;
                lastRaiseTime = currentTime;

                // Schedule flush (via coroutine esterna o immediate)
                // Per semplicità, flush immediato dopo finestra
            }
        }

        /// <summary>
        /// Flush manuale del batch (chiamato a fine frame se necessario).
        /// </summary>
        public static void FlushBatch()
        {
            if (batchedAmount > 0)
            {
                OnShardsGained?.Invoke(batchedAmount, batchedPosition);
                batchedAmount = 0;
                lastRaiseTime = -1f;
            }
        }

        /// <summary>
        /// Reset completo (per testing/scene reload).
        /// </summary>
        public static void Reset()
        {
            batchedAmount = 0;
            batchedPosition = Vector3.zero;
            lastRaiseTime = -1f;
        }

        // ============================================
        // BRIDGE DA NUOVO SISTEMA
        // ============================================

        /// <summary>
        /// Inizializza il bridge per ricevere eventi dal nuovo EconomyFeedbackSystem.
        /// Chiamato automaticamente quando qualcuno si sottoscrive a OnShardsGained.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeBridge()
        {
            if (bridgeInitialized) return;

            ResourceSystem.OnResourceChanged += HandleResourceChanged;
            bridgeInitialized = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("<color=yellow>[ShardRewardEvents]</color> Bridge initialized - forwarding from ResourceSystem.OnResourceChanged");
#endif
        }

        private static void HandleResourceChanged(ResourceChangeInfo info)
        {
            // Inoltra solo eventi shard positivi (guadagni)
            if (info.ResourceId == "shard" && info.Delta > 0 && info.HasWorldPos)
            {
                // Non chiamare RaiseShardsGained per evitare loop, invoca direttamente
                OnShardsGained?.Invoke(info.Delta, info.WorldPos);
            }
        }
    }
}
