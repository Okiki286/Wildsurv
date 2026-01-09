using UnityEngine;
using TMPro;
using Sirenix.OdinInspector;
using WildernessSurvival.Gameplay.Core;
using WildernessSurvival.Core.Systems;

#if DOTWEEN_ENABLED
using DG.Tweening;
#endif

namespace WildernessSurvival.UI
{
    /// <summary>
    /// ObjectiveHUD - Visualizza il progresso dell'obiettivo di sopravvivenza.
    /// Mostra "Obiettivo: Sopravvivi X/Y Giorni" con animazione punch al cambio giorno.
    /// </summary>
    public class ObjectiveHUD : MonoBehaviour
    {
        // ============================================
        // REFERENCES
        // ============================================

        [TitleGroup("UI References")]
        [Required]
        [Tooltip("TextMeshProUGUI che mostra il testo dell'obiettivo")]
        [SerializeField] private TextMeshProUGUI objectiveText;

        [TitleGroup("Colors")]
        [Tooltip("Colore del testo quando non sei all'ultimo giorno")]
        [SerializeField] private Color normalColor = new Color(0.8f, 0.8f, 0.8f, 1f); // Grigio chiaro

        [Tooltip("Colore del testo all'ultimo giorno (tensione!)")]
        [SerializeField] private Color lastDayColor = new Color(1f, 0.85f, 0.2f, 1f); // Giallo/Oro

        [TitleGroup("Animation Settings")]
        [Tooltip("Abilita animazione punch quando cambia il giorno")]
        [SerializeField] private bool enablePunchAnimation = true;

        [ShowIf("enablePunchAnimation")]
        [Tooltip("Scala massima del punch (1.2 = +20% di size)")]
        [SerializeField] private float punchScale = 1.2f;

        [ShowIf("enablePunchAnimation")]
        [Tooltip("Durata dell'animazione punch in secondi")]
        [SerializeField] private float punchDuration = 0.5f;

        [ShowIf("enablePunchAnimation")]
        [Tooltip("Elasticity del punch (maggiore = più rimbalzante)")]
        [SerializeField] private int punchElasticity = 1;

        [TitleGroup("Debug")]
        [SerializeField] private bool debugMode = false;

        // ============================================
        // RUNTIME STATE
        // ============================================

        private int lastDayNumber = -1;
        private RectTransform textRectTransform;
        private Vector3 originalScale;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            // Cache del RectTransform per le animazioni
            if (objectiveText != null)
            {
                textRectTransform = objectiveText.GetComponent<RectTransform>();
                if (textRectTransform != null)
                {
                    originalScale = textRectTransform.localScale;
                }
            }
        }

        private void Start()
        {
            // Aggiorna immediatamente all'avvio
            UpdateObjectiveText();
        }

        private void Update()
        {
            // Controlla se il giorno è cambiato (polling pattern come SurvivalSystem)
            if (SurvivalSystem.Instance != null && DayNightSystem.Instance != null)
            {
                int currentDay = DayNightSystem.Instance.CurrentDayNumber;

                // Se il giorno è cambiato, aggiorna il testo e anima
                if (currentDay != lastDayNumber)
                {
                    lastDayNumber = currentDay;
                    UpdateObjectiveText();

                    // Trigger animazione punch
                    if (enablePunchAnimation)
                    {
                        PlayPunchAnimation();
                    }
                }
            }
        }

        // ============================================
        // UPDATE LOGIC
        // ============================================

        /// <summary>
        /// Aggiorna il testo dell'obiettivo con il progresso corrente.
        /// Formato: "Obiettivo: Sopravvivi X/Y Giorni"
        /// </summary>
        private void UpdateObjectiveText()
        {
            if (objectiveText == null) return;
            if (SurvivalSystem.Instance == null) return;
            if (DayNightSystem.Instance == null) return;

            int survivalDays = SurvivalSystem.Instance.DaysSurvived;
            int currentDay = DayNightSystem.Instance.CurrentDayNumber;
            int target = SurvivalSystem.Instance.DaysToWin;

            // ═══════════════════════════════════════════════════════════
            // FIX: Il SurvivalSystem parte da 0 ma il DayNightSystem da 1
            // All'inizio: survivalDays = 0, currentDay = 1
            // Dopo primo giorno: survivalDays = 2, currentDay = 2
            // Usiamo il max per mostrare sempre il giorno corretto
            // ═══════════════════════════════════════════════════════════
            int current = Mathf.Max(survivalDays, currentDay);

            // Formato testo
            objectiveText.text = $"Obiettivo: Sopravvivi {current}/{target} Giorni";

            // Cambia colore se siamo all'ultimo giorno
            bool isLastDay = (current == target - 1) || (current >= target);
            objectiveText.color = isLastDay ? lastDayColor : normalColor;

            if (debugMode)
            {
                string status = isLastDay ? "ULTIMO GIORNO!" : "Normale";
                Debug.Log($"<color=cyan>[ObjectiveHUD]</color> Aggiornato: {current}/{target} (Survival:{survivalDays}, Day:{currentDay}) - Status: {status}");
            }
        }

        // ============================================
        // ANIMATION
        // ============================================

        /// <summary>
        /// Anima il testo con un effetto "punch" (scale up/down).
        /// Usa DOTween se disponibile, altrimenti usa una Coroutine semplice.
        /// </summary>
        private void PlayPunchAnimation()
        {
            if (textRectTransform == null) return;

#if DOTWEEN_ENABLED
            // ═══════════════════════════════════════════════════════════
            // DOTWEEN ANIMATION - Punch scale con elasticity
            // ═══════════════════════════════════════════════════════════

            // Kill animazioni precedenti per evitare sovrapposizioni
            textRectTransform.DOKill();

            // Reset alla scala originale
            textRectTransform.localScale = originalScale;

            // Punch animation (scale up & bounce back)
            Vector3 punchVector = new Vector3(punchScale - 1f, punchScale - 1f, 0f);
            textRectTransform.DOPunchScale(punchVector, punchDuration, punchElasticity)
                .SetEase(Ease.OutElastic);

            if (debugMode)
            {
                Debug.Log("<color=green>[ObjectiveHUD]</color> 🎬 Punch animation (DOTween) triggered!");
            }
#else
            // ═══════════════════════════════════════════════════════════
            // FALLBACK - Coroutine animation semplice (no DOTween)
            // ═══════════════════════════════════════════════════════════

            StopAllCoroutines(); // Stop animazioni precedenti
            StartCoroutine(SimplePunchAnimation());

            if (debugMode)
            {
                Debug.Log("<color=yellow>[ObjectiveHUD]</color> 🎬 Punch animation (Coroutine) triggered!");
            }
#endif
        }

#if !DOTWEEN_ENABLED
        /// <summary>
        /// Animazione punch semplice senza DOTween (fallback).
        /// Scale up smooth e poi down con bounce.
        /// </summary>
        private System.Collections.IEnumerator SimplePunchAnimation()
        {
            if (textRectTransform == null) yield break;

            float elapsed = 0f;
            Vector3 startScale = originalScale;
            Vector3 targetScale = originalScale * punchScale;

            // FASE 1: Scale UP (0 -> 50% del tempo)
            float scaleUpDuration = punchDuration * 0.4f;
            while (elapsed < scaleUpDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / scaleUpDuration;
                textRectTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            // FASE 2: Scale DOWN con overshoot (50% -> 100% del tempo)
            elapsed = 0f;
            float scaleDownDuration = punchDuration * 0.6f;
            while (elapsed < scaleDownDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / scaleDownDuration;

                // Overshoot (bounce effect)
                float overshoot = Mathf.Sin(t * Mathf.PI); // Smooth bounce
                Vector3 currentScale = Vector3.Lerp(targetScale, startScale, t);
                currentScale += startScale * overshoot * 0.1f; // Bounce amplitude

                textRectTransform.localScale = currentScale;
                yield return null;
            }

            // FASE 3: Assicura ritorno esatto alla scala originale
            textRectTransform.localScale = originalScale;
        }
#endif

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Force un aggiornamento immediato del testo (utile per debug).
        /// </summary>
        [TitleGroup("Debug Actions")]
        [Button("🔄 Force Update")]
        [GUIColor(0.6f, 0.8f, 1f)]
        public void ForceUpdate()
        {
            lastDayNumber = -1; // Reset per forzare update
            UpdateObjectiveText();
        }

        /// <summary>
        /// Testa l'animazione punch manualmente.
        /// </summary>
        [TitleGroup("Debug Actions")]
        [Button("🎬 Test Punch Animation")]
        [GUIColor(0.8f, 1f, 0.6f)]
        [ShowIf("enablePunchAnimation")]
        public void TestPunchAnimation()
        {
            PlayPunchAnimation();
        }

        /// <summary>
        /// Imposta colori personalizzati per normale/ultimo giorno.
        /// </summary>
        public void SetColors(Color normal, Color lastDay)
        {
            normalColor = normal;
            lastDayColor = lastDay;
            UpdateObjectiveText(); // Riapplica
        }

        /// <summary>
        /// Abilita/disabilita l'animazione punch a runtime.
        /// </summary>
        public void SetPunchAnimationEnabled(bool enabled)
        {
            enablePunchAnimation = enabled;

            if (debugMode)
            {
                Debug.Log($"[ObjectiveHUD] Punch animation: {(enabled ? "Enabled" : "Disabled")}");
            }
        }
    }
}
