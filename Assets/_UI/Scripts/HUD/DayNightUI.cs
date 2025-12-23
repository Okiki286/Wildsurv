using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;
using WildernessSurvival.Core.Systems;

namespace WildernessSurvival.UI
{
    /// <summary>
    /// UI component that displays the Day/Night cycle progress.
    /// Shows current day number and a visual progress bar for the current phase.
    /// </summary>
    public class DayNightUI : MonoBehaviour
    {
        // ============================================
        // REFERENCES
        // ============================================

        [TitleGroup("UI References")]
        [Required]
        [SerializeField] private Slider timeSlider;
        
        [Required]
        [SerializeField] private TextMeshProUGUI dayText;

        [SerializeField] private TextMeshProUGUI timerText;

        [TitleGroup("Phase Icon")]
        [SerializeField] private Image phaseIcon;
        [SerializeField] private Sprite sunSprite;
        [SerializeField] private Sprite moonSprite;

        [TitleGroup("Colors")]
        [SerializeField] private Color dayFillColor = new Color(0.957f, 0.635f, 0.380f, 1f);    // #F4A261 Sunset Orange
        [SerializeField] private Color nightFillColor = new Color(0.302f, 0.435f, 0.690f, 1f); // #4D6FB0 Night Blue

        [TitleGroup("Settings")]
        [SerializeField] private float updateInterval = 0.1f;
        [SerializeField] private bool showTimeRemaining = true;

        [TitleGroup("Debug")]
        [SerializeField] private bool debugMode = false;

        // ============================================
        // RUNTIME
        // ============================================

        private Image fillImage;
        private float updateTimer = 0f;
        private int lastDayNumber = -1;
        private bool lastWasNight = false;
        private bool firstUpdate = true;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            // Cache fill image reference
            if (timeSlider != null)
            {
                Transform fillArea = timeSlider.fillRect;
                if (fillArea != null)
                {
                    fillImage = fillArea.GetComponent<Image>();
                }
            }
        }

        private void Start()
        {
            // Initial update
            UpdateDisplay();
        }

        private void Update()
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                updateTimer = 0f;
                UpdateDisplay();
            }
        }

        // ============================================
        // DISPLAY UPDATE
        // ============================================

        private void UpdateDisplay()
        {
            if (DayNightSystem.Instance == null) return;

            DayNightSystem dns = DayNightSystem.Instance;

            // Update slider value (0 to 1)
            if (timeSlider != null)
            {
                timeSlider.value = dns.PhaseProgress;
            }

            // Update day text
            if (dayText != null)
            {
                int currentDay = dns.CurrentDayNumber;
                if (currentDay != lastDayNumber)
                {
                    lastDayNumber = currentDay;
                    
                    if (dns.IsNight)
                    {
                        dayText.text = $"Night {currentDay}";
                    }
                    else
                    {
                        dayText.text = $"Day {currentDay}";
                    }

                    if (debugMode)
                    {
                        Debug.Log($"<color=cyan>[DayNightUI]</color> Updated to: {dayText.text}");
                    }
                }
                else if (dns.IsNight && !dayText.text.StartsWith("Night"))
                {
                    dayText.text = $"Night {currentDay}";
                }
                else if (dns.IsDay && !dayText.text.StartsWith("Day"))
                {
                    dayText.text = $"Day {currentDay}";
                }
            }

            // Update timer text (optional)
            if (timerText != null && showTimeRemaining)
            {
                timerText.text = dns.GetFormattedTimeRemaining();
            }

            // Update fill color based on day/night
            if (fillImage != null)
            {
                Color targetColor = dns.IsNight ? nightFillColor : dayFillColor;
                fillImage.color = Color.Lerp(fillImage.color, targetColor, Time.deltaTime * 5f);
            }

            // Update phase icon (Sun/Moon swap)
            UpdatePhaseIcon(dns.IsNight);
        }

        private void UpdatePhaseIcon(bool isNight)
        {
            if (phaseIcon == null) return;

            // Only swap sprite if phase changed or it's the first update
            if (isNight != lastWasNight || firstUpdate)
            {
                lastWasNight = isNight;
                firstUpdate = false;
                
                Sprite targetSprite = isNight ? moonSprite : sunSprite;
                
                if (targetSprite != null)
                {
                    phaseIcon.sprite = targetSprite;
                    phaseIcon.enabled = true; // Show only if we have a sprite
                    if (debugMode) Debug.Log($"<color=cyan>[DayNightUI]</color> Icon -> {(isNight ? "Moon" : "Sun")}");
                }
                else
                {
                    // If no sprite assigned, hide the white square!
                    phaseIcon.enabled = false;
                }
            }
        }

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Force an immediate display refresh.
        /// </summary>
        [Button("Force Update")]
        public void ForceUpdate()
        {
            lastDayNumber = -1; // Force text update
            UpdateDisplay();
        }

        /// <summary>
        /// Set custom fill colors.
        /// </summary>
        public void SetFillColors(Color dayColor, Color nightColor)
        {
            dayFillColor = dayColor;
            nightFillColor = nightColor;
        }
    }
}
