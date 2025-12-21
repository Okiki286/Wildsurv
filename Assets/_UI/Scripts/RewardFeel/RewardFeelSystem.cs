using UnityEngine;
using Sirenix.OdinInspector;

namespace WildernessSurvival.UI.RewardFeel
{
    /// <summary>
    /// Componente manager unificato per il sistema Reward Feel.
    /// Attaccare a un GameObject in scena per setup rapido.
    /// Gestisce: spawner popup, HUD pulse, SFX player.
    /// </summary>
    public class RewardFeelSystem : MonoBehaviour
    {
        // ============================================
        // SINGLETON
        // ============================================

        public static RewardFeelSystem Instance { get; private set; }

        // ============================================
        // REFERENCES
        // ============================================

        [TitleGroup("Componenti")]
        [InfoBox("Assegna i componenti già presenti in scena, oppure lascia vuoto per auto-search")]
        [SerializeField] private ShardPopupSpawner popupSpawner;
        [SerializeField] private ShardHUDPulse hudPulse;
        [SerializeField] private ShardSfxPlayer sfxPlayer;

        [TitleGroup("Status")]
        [ShowInInspector, ReadOnly]
        private bool isInitialized = false;

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

            AutoFindComponents();
            isInitialized = true;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ============================================
        // INITIALIZATION
        // ============================================

        private void AutoFindComponents()
        {
            if (popupSpawner == null)
            {
                popupSpawner = GetComponentInChildren<ShardPopupSpawner>();
                if (popupSpawner == null)
                {
                    popupSpawner = FindFirstObjectByType<ShardPopupSpawner>();
                }
            }

            if (hudPulse == null)
            {
                hudPulse = GetComponentInChildren<ShardHUDPulse>();
                if (hudPulse == null)
                {
                    hudPulse = FindFirstObjectByType<ShardHUDPulse>();
                }
            }

            if (sfxPlayer == null)
            {
                sfxPlayer = GetComponentInChildren<ShardSfxPlayer>();
                if (sfxPlayer == null)
                {
                    sfxPlayer = FindFirstObjectByType<ShardSfxPlayer>();
                }
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            LogInitializationStatus();
#endif
        }

        private void LogInitializationStatus()
        {
            string status = "<color=cyan>[RewardFeelSystem]</color> Status:\n";
            status += $"  PopupSpawner: {(popupSpawner != null ? "OK" : "MISSING")}\n";
            status += $"  HUDPulse: {(hudPulse != null ? "OK" : "MISSING")}\n";
            status += $"  SfxPlayer: {(sfxPlayer != null ? "OK" : "MISSING")}";
            Debug.Log(status);
        }

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Attiva manualmente tutti gli effetti reward feel.
        /// Utile per testing o eventi custom.
        /// </summary>
        [TitleGroup("Debug")]
        [Button("Simulate Shard Reward", ButtonSizes.Large)]
        public void SimulateShardReward()
        {
            SimulateShardReward(Random.Range(5, 25), Camera.main != null ? Camera.main.transform.position + Camera.main.transform.forward * 5f : Vector3.zero);
        }

        public void SimulateShardReward(int amount, Vector3 worldPosition)
        {
            ShardRewardEvents.RaiseShardsGained(amount, worldPosition);
        }

        /// <summary>
        /// Verifica se il sistema è completamente configurato.
        /// </summary>
        public bool IsFullyConfigured()
        {
            return popupSpawner != null && hudPulse != null && sfxPlayer != null;
        }

        // ============================================
        // EDITOR
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug")]
        [Button("Stress Test (20 rewards)", ButtonSizes.Medium)]
        private void DebugStressTest()
        {
            for (int i = 0; i < 20; i++)
            {
                Vector3 randomPos = Vector3.zero;
                if (Camera.main != null)
                {
                    randomPos = Camera.main.transform.position +
                                Camera.main.transform.forward * Random.Range(3f, 10f) +
                                Camera.main.transform.right * Random.Range(-5f, 5f);
                }
                ShardRewardEvents.RaiseShardsGained(Random.Range(1, 15), randomPos);
            }
        }

        [Button("Re-scan Components", ButtonSizes.Medium)]
        private void DebugRescan()
        {
            AutoFindComponents();
        }
#endif
    }
}
