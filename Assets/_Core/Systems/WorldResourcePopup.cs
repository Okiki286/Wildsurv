using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace WildernessSurvival.Core.Systems
{
    /// <summary>
    /// Popup Screen-Space per visualizzare cambio risorse ("+5", "-10").
    /// Pooled - niente Instantiate/Destroy durante gameplay.
    /// Usa approccio Screen Space Overlay: segue posizione mondo convertita in screen coords.
    /// Sempre orizzontale, niente tilt da billboard.
    ///
    /// OUTLINE: Usa TMP Material Preset con Outline configurato nel prefab.
    /// Non modifichiamo material a runtime per evitare leaks.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class WorldResourcePopup : MonoBehaviour
    {
        // ============================================
        // CONFIGURAZIONE
        // ============================================

        [Header("References")]
        [SerializeField] private TextMeshProUGUI amountText;
        [SerializeField] private Image iconImage;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform container;

        [Header("Animation Timing")]
        [Tooltip("Durata totale dell'animazione (più lungo = più leggibile)")]
        [SerializeField] private float duration = 1.8f;
        [Tooltip("Distanza di salita in pixel screen-space")]
        [SerializeField] private float riseDistancePixels = 100f;
        [Tooltip("Durata del pop-in iniziale")]
        [SerializeField] private float popInDuration = 0.12f;
        [Tooltip("Scala massima durante il pop")]
        [SerializeField] private float popPeakScale = 1.25f;
        [Tooltip("Quando inizia il fade-out (0.7 = ultimo 30% dell'animazione)")]
        [SerializeField] private float fadeStartPercent = 0.7f;

        [Header("Colors")]
        [SerializeField] private Color gainColor = new Color(0.3f, 1f, 0.3f);
        [SerializeField] private Color spendColor = new Color(1f, 0.4f, 0.4f);

        // ============================================
        // RUNTIME STATE
        // ============================================

        private float elapsed;
        private bool isAnimating;
        private Vector3 worldAnchorPos;
        private Vector2 screenStartOffset;
        private RectTransform rectTransform;
        private Camera cachedCamera;
        private Canvas parentCanvas;
        private RectTransform canvasRectTransform;

        private System.Action<WorldResourcePopup> onComplete;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            EnsureReferences();
        }

        private void EnsureReferences()
        {
            if (rectTransform != null) return;

            rectTransform = GetComponent<RectTransform>();

            if (container == null)
                container = rectTransform;

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
        }

        /// <summary>
        /// Chiamato da EconomyFeedbackSystem per impostare il canvas parent.
        /// </summary>
        public void SetParentCanvas(Canvas canvas, Camera camera)
        {
            parentCanvas = canvas;
            cachedCamera = camera;
            if (canvas != null)
            {
                canvasRectTransform = canvas.GetComponent<RectTransform>();
            }
        }

        private void LateUpdate()
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
        /// Setup e avvia l'animazione del popup.
        /// </summary>
        /// <param name="icon">Sprite risorsa (può essere null)</param>
        /// <param name="delta">Quantità (positiva = gain, negativa = spend)</param>
        /// <param name="worldPosition">Posizione mondo da tracciare</param>
        /// <param name="stackOffsetPixels">Offset in pixel per stacking</param>
        /// <param name="completeCallback">Callback al termine</param>
        public void Setup(Sprite icon, int delta, Vector3 worldPosition, Vector2 stackOffsetPixels, System.Action<WorldResourcePopup> completeCallback)
        {
            EnsureReferences();

            // === FULL STATE RESET (robust pooling) ===
            elapsed = 0f;
            isAnimating = false; // Will be set true at the end
            onComplete = completeCallback;

            // Reset visual state immediately
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;

            if (container != null)
                container.localScale = Vector3.one * 0.5f; // Start small for pop-in

            // Cache camera se non già impostata
            if (cachedCamera == null)
            {
                cachedCamera = Camera.main;
            }

            // Salva posizione mondo e offset stacking
            worldAnchorPos = worldPosition;
            screenStartOffset = stackOffsetPixels;

            // Testo (outline è gestito dal Material Preset nel prefab)
            if (amountText != null)
            {
                string sign = delta >= 0 ? "+" : "";
                amountText.text = $"{sign}{delta}";
                amountText.color = delta >= 0 ? gainColor : spendColor;
            }

            // Icona
            if (iconImage != null)
            {
                if (icon != null)
                {
                    iconImage.sprite = icon;
                    iconImage.enabled = true;
                }
                else
                {
                    iconImage.enabled = false;
                }
            }

            // Posizione iniziale
            UpdateScreenPosition(0f);

            // Start animation
            isAnimating = true;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Overload per retrocompatibilità (senza stackOffset).
        /// </summary>
        public void Setup(Sprite icon, int delta, Vector3 worldPosition, System.Action<WorldResourcePopup> completeCallback)
        {
            Setup(icon, delta, worldPosition, Vector2.zero, completeCallback);
        }

        /// <summary>
        /// Forza ritorno al pool (interrompe animazione).
        /// </summary>
        public void ForceReturn()
        {
            // Full state cleanup
            isAnimating = false;
            elapsed = 0f;
            onComplete = null;

            if (container != null)
                container.localScale = Vector3.one;

            if (canvasGroup != null)
                canvasGroup.alpha = 1f;

            gameObject.SetActive(false);
        }

        // ============================================
        // ANIMATION
        // ============================================

        private void UpdateAnimation(float t)
        {
            // === SCALE: Pop-in con settle ===
            float scale;
            float popT = elapsed / popInDuration;

            if (popT < 1f)
            {
                // Pop up veloce
                scale = Mathf.Lerp(0.5f, popPeakScale, EaseOutBack(popT));
            }
            else if (popT < 2f)
            {
                // Settle più morbido
                float settleT = (popT - 1f);
                scale = Mathf.Lerp(popPeakScale, 1f, EaseOutQuad(settleT));
            }
            else
            {
                scale = 1f;
            }

            if (container != null)
                container.localScale = Vector3.one * scale;

            // === POSIZIONE: Segue mondo + salita in screen space ===
            UpdateScreenPosition(t);

            // === ALPHA: Fade-out ritardato ===
            if (canvasGroup != null)
            {
                if (t > fadeStartPercent)
                {
                    float fadeT = (t - fadeStartPercent) / (1f - fadeStartPercent);
                    // Ease-out per fade più naturale
                    canvasGroup.alpha = 1f - EaseOutQuad(fadeT);
                }
                else
                {
                    canvasGroup.alpha = 1f;
                }
            }
        }

        private void UpdateScreenPosition(float t)
        {
            if (cachedCamera == null || rectTransform == null) return;

            // Converti posizione mondo in screen position
            Vector3 screenPos = cachedCamera.WorldToScreenPoint(worldAnchorPos);

            // Se dietro la camera, nascondi
            if (screenPos.z < 0)
            {
                if (canvasGroup != null) canvasGroup.alpha = 0f;
                return;
            }

            // Calcola offset di salita con easing più morbido
            float riseT = EaseOutQuad(t);
            float riseOffset = riseT * riseDistancePixels;

            // Posizione finale = screen position + stacking offset + rise offset
            Vector2 finalScreenPos = new Vector2(screenPos.x, screenPos.y);
            finalScreenPos += screenStartOffset;
            finalScreenPos.y += riseOffset;

            // Converti screen position in local position del canvas
            if (parentCanvas != null && canvasRectTransform != null)
            {
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRectTransform,
                    finalScreenPos,
                    parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cachedCamera,
                    out localPoint
                );
                rectTransform.anchoredPosition = localPoint;
            }
            else
            {
                // Fallback: usa screen position direttamente
                rectTransform.position = new Vector3(finalScreenPos.x, finalScreenPos.y, 0f);
            }
        }

        private void CompleteAnimation()
        {
            isAnimating = false;

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            onComplete?.Invoke(this);
        }

        // ============================================
        // EASING (inline, no GC)
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
        // EDITOR
        // ============================================

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (duration <= 0f) duration = 1.8f;
            if (popInDuration <= 0f) popInDuration = 0.12f;
            if (riseDistancePixels <= 0f) riseDistancePixels = 100f;
            if (fadeStartPercent < 0.3f) fadeStartPercent = 0.7f;
        }
#endif
    }
}
