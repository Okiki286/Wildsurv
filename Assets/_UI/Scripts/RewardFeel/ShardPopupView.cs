using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace WildernessSurvival.UI.RewardFeel
{
    /// <summary>
    /// Componente singolo popup per visualizzare "+X shards".
    /// Gestisce animazione pop-in, salita e fade-out.
    /// Pooled - non usa Instantiate/Destroy durante gameplay.
    /// </summary>
    public class ShardPopupView : MonoBehaviour
    {
        // ============================================
        // CONFIGURAZIONE
        // ============================================

        [Header("References")]
        [SerializeField] private TextMeshProUGUI amountText;
        [SerializeField] private Image shardIcon;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform container;

        [Header("Animation Settings")]
        [SerializeField] private float duration = 0.9f;
        [SerializeField] private float riseDistance = 80f;
        [SerializeField] private float popInScale = 0.5f;
        [SerializeField] private float popPeakScale = 1.15f;
        [SerializeField] private float popInDuration = 0.12f;
        [SerializeField] private float fadeStartPercent = 0.6f;

        [Header("Offset")]
        [SerializeField] private Vector2 randomOffsetRange = new Vector2(20f, 10f);

        // ============================================
        // RUNTIME STATE
        // ============================================

        private float elapsed;
        private bool isAnimating;
        private Vector3 startPosition;
        private Vector3 endPosition;
        private Vector2 randomOffset;

        // Cache per evitare GetComponent in Update
        private Transform cachedTransform;
        private RectTransform rectTransform;

        // Callback per notificare completamento (pool return)
        private System.Action<ShardPopupView> onComplete;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            cachedTransform = transform;
            rectTransform = GetComponent<RectTransform>();

            if (container == null)
                container = rectTransform;

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Update()
        {
            if (!isAnimating) return;

            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (t >= 1f)
            {
                CompleteAnimation();
                return;
            }

            UpdateAnimation(t);
        }

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Inizializza e avvia l'animazione del popup.
        /// </summary>
        /// <param name="amount">Quantità shards da mostrare</param>
        /// <param name="screenPosition">Posizione iniziale in screen space</param>
        /// <param name="completeCallback">Callback quando animazione finisce</param>
        public void Show(int amount, Vector2 screenPosition, System.Action<ShardPopupView> completeCallback)
        {
            onComplete = completeCallback;

            // Setup testo
            if (amountText != null)
            {
                amountText.text = $"+{amount}";
            }

            // Random offset per evitare sovrapposizioni
            randomOffset = new Vector2(
                Random.Range(-randomOffsetRange.x, randomOffsetRange.x),
                Random.Range(-randomOffsetRange.y, randomOffsetRange.y)
            );

            // Posizione iniziale
            Vector2 startPos = screenPosition + randomOffset;
            rectTransform.anchoredPosition = startPos;
            startPosition = startPos;
            endPosition = startPos + Vector2.up * riseDistance;

            // Reset stato animazione
            elapsed = 0f;
            isAnimating = true;

            // Stato iniziale: piccolo e visibile
            container.localScale = Vector3.one * popInScale;
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;

            gameObject.SetActive(true);
        }

        /// <summary>
        /// Reset per pooling - chiamato prima di restituire al pool.
        /// </summary>
        public void ResetForPool()
        {
            isAnimating = false;
            elapsed = 0f;
            onComplete = null;

            container.localScale = Vector3.one;
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;

            rectTransform.anchoredPosition = Vector2.zero;
            gameObject.SetActive(false);
        }

        // ============================================
        // ANIMATION
        // ============================================

        private void UpdateAnimation(float t)
        {
            // === SCALE: Pop-in (0 -> popInDuration) ===
            float popT = elapsed / popInDuration;
            float scale;

            if (popT < 1f)
            {
                // Fase pop-in: da piccolo a peak
                scale = Mathf.Lerp(popInScale, popPeakScale, EaseOutBack(popT));
            }
            else if (popT < 1.5f)
            {
                // Fase settle: da peak a 1.0
                float settleT = (popT - 1f) / 0.5f;
                scale = Mathf.Lerp(popPeakScale, 1f, settleT);
            }
            else
            {
                scale = 1f;
            }

            container.localScale = Vector3.one * scale;

            // === POSIZIONE: Salita lineare ===
            float riseT = EaseOutQuad(t);
            Vector2 currentPos = Vector2.Lerp(startPosition, endPosition, riseT);
            rectTransform.anchoredPosition = currentPos;

            // === ALPHA: Fade-out nella parte finale ===
            if (canvasGroup != null)
            {
                if (t > fadeStartPercent)
                {
                    float fadeT = (t - fadeStartPercent) / (1f - fadeStartPercent);
                    canvasGroup.alpha = 1f - fadeT;
                }
                else
                {
                    canvasGroup.alpha = 1f;
                }
            }
        }

        private void CompleteAnimation()
        {
            isAnimating = false;

            // Nascondi
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            // Notifica pool
            onComplete?.Invoke(this);
        }

        // ============================================
        // EASING FUNCTIONS (inline, no GC)
        // ============================================

        private static float EaseOutQuad(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        // ============================================
        // EDITOR VALIDATION
        // ============================================

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (duration <= 0f) duration = 0.9f;
            if (popInDuration <= 0f) popInDuration = 0.12f;
            if (riseDistance <= 0f) riseDistance = 80f;
        }
#endif
    }
}
