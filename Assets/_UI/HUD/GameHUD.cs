using UnityEngine;
using TMPro;
using Sirenix.OdinInspector;
using WildernessSurvival.Gameplay.Resources;
using WildernessSurvival.Core.Systems;

namespace WildernessSurvival.UI
{
    /// <summary>
    /// HUD principale di gioco - mostra risorse, tempo, e info critiche
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        // ============================================
        // RESOURCE DISPLAY
        // ============================================

        [TitleGroup("Resource Display")]
        [BoxGroup("Resource Display/Text References")]
        [Required("Text per mostrare Warmwood")]
        [ChildGameObjectsOnly]
        [SerializeField] private TextMeshProUGUI warmwoodText;

        [BoxGroup("Resource Display/Text References")]
        [Required("Text per mostrare Food")]
        [ChildGameObjectsOnly]
        [SerializeField] private TextMeshProUGUI foodText;

        [BoxGroup("Resource Display/Text References")]
        [Required("Text per mostrare Shards")]
        [ChildGameObjectsOnly]
        [SerializeField] private TextMeshProUGUI shardsText;

        // ============================================
        // TIME DISPLAY
        // ============================================

        [TitleGroup("Time Display")]
        [BoxGroup("Time Display/References")]
        [Required("Text per mostrare ora del giorno")]
        [ChildGameObjectsOnly]
        [SerializeField] private TextMeshProUGUI timeText;

        [BoxGroup("Time Display/References")]
        [ChildGameObjectsOnly]
        [SerializeField] private TextMeshProUGUI dayNumberText;

        // ============================================
        // SETTINGS
        // ============================================

        [TitleGroup("Settings")]
        [BoxGroup("Settings/Update")]
        [LabelWidth(150)]
        [Tooltip("Aggiorna risorse ogni X secondi invece che ogni frame")]
        [Range(0.1f, 1f)]
        [SerializeField] private float resourceUpdateInterval = 0.5f;

        [BoxGroup("Settings/Formatting")]
        [LabelWidth(150)]
        [Tooltip("Mostra decimali nelle risorse?")]
        [SerializeField] private bool showDecimals = false;

        // ============================================
        // RUNTIME
        // ============================================

        private float updateTimer = 0f;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Start()
        {
            // Aggiorna subito all'avvio
            UpdateResourceDisplay();
            UpdateTimeDisplay();
        }

        private void Update()
        {
            // Aggiorna tempo ogni frame (smooth)
            UpdateTimeDisplay();

            // Aggiorna risorse con interval
            updateTimer += Time.deltaTime;
            if (updateTimer >= resourceUpdateInterval)
            {
                UpdateResourceDisplay();
                updateTimer = 0f;
            }
        }

        // ============================================
        // UPDATE METHODS
        // ============================================

        private void UpdateResourceDisplay()
        {
            if (ResourceSystem.Instance == null)
            {
                Debug.LogWarning("[GameHUD] ResourceSystem not found!");
                return;
            }

            // Ottieni risorse
            float warmwood = ResourceSystem.Instance.GetResourceAmount("warmwood");
            float food = ResourceSystem.Instance.GetResourceAmount("food");
            float shards = ResourceSystem.Instance.GetResourceAmount("shards");

            // Format string
            string format = showDecimals ? "F1" : "F0";

            // Aggiorna UI
            if (warmwoodText != null)
                warmwoodText.text = $"🪵 {warmwood.ToString(format)}";

            if (foodText != null)
                foodText.text = $"🍖 {food.ToString(format)}";

            if (shardsText != null)
                shardsText.text = $"💎 {shards.ToString(format)}";
        }

        private void UpdateTimeDisplay()
        {
            if (DayNightSystem.Instance == null)
            {
                if (timeText != null)
                    timeText.text = "--:--";
                return;
            }

            float currentTime = DayNightSystem.Instance.CurrentTime;
            int hours = Mathf.FloorToInt(currentTime);
            int minutes = Mathf.FloorToInt((currentTime - hours) * 60f);

            if (timeText != null)
                timeText.text = $"{hours:00}:{minutes:00}";

            if (dayNumberText != null)
                dayNumberText.text = $"Day {DayNightSystem.Instance.CurrentDayNumber}";
        }

        // ============================================
        // PUBLIC API (per eventi o animazioni)
        // ============================================

        /// <summary>
        /// Forza aggiornamento immediato (chiamato da eventi)
        /// </summary>
        public void ForceUpdate()
        {
            UpdateResourceDisplay();
            UpdateTimeDisplay();
        }

        /// <summary>
        /// Lampeggia UI risorsa quando insufficiente
        /// </summary>
        public void FlashResourceInsufficient(string resourceId)
        {
            // TODO: Implementa animazione lampeggio rosso
            Debug.Log($"[GameHUD] ⚠️ Insufficient resource: {resourceId}");
        }

        // ============================================
        // DEBUG
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug")]
        [Button("Force Update All", ButtonSizes.Medium)]
        [GUIColor(0.4f, 0.8f, 0.4f)]
        private void DebugForceUpdate()
        {
            ForceUpdate();
        }
#endif
    }
}