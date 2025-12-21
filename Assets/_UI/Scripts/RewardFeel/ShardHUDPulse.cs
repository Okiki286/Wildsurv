using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace WildernessSurvival.UI.RewardFeel
{
    /// <summary>
    /// Gestisce il micro-pulse dell'HUD shards quando vengono guadagnati.
    /// Attaccare al container del display shards nell'HUD.
    /// </summary>
    public class ShardHUDPulse : MonoBehaviour
    {
        // ============================================
        // SINGLETON (opzionale, per accesso diretto)
        // ============================================

        public static ShardHUDPulse Instance { get; private set; }

        // ============================================
        // CONFIGURAZIONE
        // ============================================

        [TitleGroup("References")]
        [SerializeField] private RectTransform pulseTarget;
        [SerializeField] private Image iconImage;

        [TitleGroup("Scale Pulse")]
        [SerializeField] private float pulseScale = 1.08f;
        [SerializeField] private float pulseDuration = 0.25f;

        [TitleGroup("Color Flash")]
        [SerializeField] private bool enableColorFlash = true;
        [SerializeField] private Color flashColor = new Color(1f, 0.9f, 0.4f, 1f);
        [SerializeField] private float flashDuration = 0.15f;

        [TitleGroup("Debug")]
        [SerializeField] private bool debugMode = false;

        // ============================================
        // RUNTIME STATE
        // ============================================

        private Vector3 originalScale = Vector3.one;
        private Color originalIconColor = Color.white;

        // Stato animazione
        private bool isAnimating = false;
        private float animElapsed = 0f;
        private int animPhase = 0; // 0=scale up, 1=scale down

        private bool isFlashing = false;
        private float flashElapsed = 0f;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            CacheOriginalValues();
        }

        private void OnEnable()
        {
            ShardRewardEvents.OnShardsGained += HandleShardsGained;
        }

        private void OnDisable()
        {
            ShardRewardEvents.OnShardsGained -= HandleShardsGained;
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
            if (isAnimating)
            {
                UpdateScalePulse();
            }

            if (isFlashing)
            {
                UpdateColorFlash();
            }
        }

        // ============================================
        // INITIALIZATION
        // ============================================

        private void CacheOriginalValues()
        {
            if (pulseTarget == null)
            {
                pulseTarget = GetComponent<RectTransform>();
            }

            if (pulseTarget != null)
            {
                originalScale = pulseTarget.localScale;
            }

            if (iconImage != null)
            {
                originalIconColor = iconImage.color;
            }
        }

        // ============================================
        // EVENT HANDLER
        // ============================================

        private void HandleShardsGained(int amount, Vector3 worldPosition)
        {
            // Ignora posizione, ci interessa solo il pulse
            TriggerPulse();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
            {
                Debug.Log($"<color=yellow>[ShardHUDPulse]</color> Pulse triggered for +{amount} shards");
            }
#endif
        }

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Attiva manualmente il pulse (per testing o chiamate dirette).
        /// </summary>
        [TitleGroup("Debug")]
        [Button("Trigger Pulse", ButtonSizes.Medium)]
        public void TriggerPulse()
        {
            // Reset e avvia animazione scale
            isAnimating = true;
            animElapsed = 0f;
            animPhase = 0;

            // Flash colore
            if (enableColorFlash && iconImage != null)
            {
                isFlashing = true;
                flashElapsed = 0f;
                iconImage.color = flashColor;
            }
        }

        // ============================================
        // ANIMATION
        // ============================================

        private void UpdateScalePulse()
        {
            if (pulseTarget == null)
            {
                isAnimating = false;
                return;
            }

            animElapsed += Time.deltaTime;
            float halfDuration = pulseDuration * 0.5f;

            if (animPhase == 0)
            {
                // Fase 1: Scale up
                float t = animElapsed / halfDuration;
                if (t >= 1f)
                {
                    t = 1f;
                    animPhase = 1;
                    animElapsed = 0f;
                }

                float scale = Mathf.Lerp(1f, pulseScale, EaseOutQuad(t));
                pulseTarget.localScale = originalScale * scale;
            }
            else
            {
                // Fase 2: Scale down
                float t = animElapsed / halfDuration;
                if (t >= 1f)
                {
                    t = 1f;
                    isAnimating = false;
                }

                float scale = Mathf.Lerp(pulseScale, 1f, EaseInQuad(t));
                pulseTarget.localScale = originalScale * scale;
            }
        }

        private void UpdateColorFlash()
        {
            if (iconImage == null)
            {
                isFlashing = false;
                return;
            }

            flashElapsed += Time.deltaTime;
            float t = flashElapsed / flashDuration;

            if (t >= 1f)
            {
                iconImage.color = originalIconColor;
                isFlashing = false;
                return;
            }

            // Lerp back to original
            iconImage.color = Color.Lerp(flashColor, originalIconColor, t);
        }

        // ============================================
        // EASING (inline, no GC)
        // ============================================

        private static float EaseOutQuad(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }

        private static float EaseInQuad(float t)
        {
            return t * t;
        }

        // ============================================
        // EDITOR VALIDATION
        // ============================================

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (pulseDuration <= 0f) pulseDuration = 0.25f;
            if (pulseScale < 1f) pulseScale = 1.08f;
            if (flashDuration <= 0f) flashDuration = 0.15f;
        }

        [TitleGroup("Debug")]
        [Button("Test Pulse x3", ButtonSizes.Medium)]
        private void DebugTestMultiple()
        {
            for (int i = 0; i < 3; i++)
            {
                Invoke(nameof(TriggerPulse), i * 0.15f);
            }
        }
#endif
    }
}
