using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;
using WildernessSurvival.Core.Systems;
using WildernessSurvival.Gameplay.Resources;

namespace WildernessSurvival.UI
{
    /// <summary>
    /// UI component for the Recruit Worker button.
    /// Displays cost, enables/disables based on CanRecruit, shows tooltip reason.
    /// Attach to a Button in the Waystone UI or HUD.
    /// </summary>
    public class RecruitUI : MonoBehaviour
    {
        // ============================================
        // CONFIGURATION
        // ============================================

        /// <summary>
        /// The resource ID used for recruitment costs.
        /// Change this to use a different resource for recruitment.
        /// </summary>
        private const string RECRUIT_COST_RESOURCE_ID = "food";

        // ============================================
        // REFERENCES
        // ============================================

        [TitleGroup("UI References")]
        [SerializeField, Required] private Button recruitButton;
        [SerializeField] private TextMeshProUGUI buttonText;
        [SerializeField] private TextMeshProUGUI costText;
        
        [TitleGroup("UI References")]
        [Tooltip("Icon image that displays the cost resource (e.g., Food icon)")]
        [SerializeField] private Image costIconImage;

        [TitleGroup("Tooltip")]
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private TextMeshProUGUI tooltipText;

        [TitleGroup("Visual")]
        [SerializeField] private Color enabledColor = Color.white;
        [SerializeField] private Color disabledColor = new Color(0.5f, 0.5f, 0.5f);

        [TitleGroup("Settings")]
        [Tooltip("Aggiorna UI ogni N secondi (0 = ogni frame)")]
        [SerializeField] private float updateInterval = 0.25f;

        // ============================================
        // RUNTIME
        // ============================================

        private float updateTimer = 0f;
        private RecruitBlockReason lastBlockReason = RecruitBlockReason.None;
        
        // Cached resource data for the cost icon
        private ResourceData cachedCostResourceData;
        private bool iconInitialized = false;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            if (recruitButton != null)
            {
                recruitButton.onClick.AddListener(OnRecruitClicked);
            }

            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (recruitButton != null)
            {
                recruitButton.onClick.RemoveListener(OnRecruitClicked);
            }
        }

        private void Update()
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                updateTimer = 0f;
                RefreshUI();
            }
        }

        private void OnEnable()
        {
            // Reset icon cache so it refreshes on enable
            iconInitialized = false;
            RefreshUI();
        }

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Call this when the parent panel opens to force an immediate refresh.
        /// WorkerAssignmentUI calls this when opening for a Waystone.
        /// </summary>
        public void Bind()
        {
            updateTimer = 0f;
            iconInitialized = false; // Force icon refresh
            RefreshUI();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("<color=cyan>[RecruitUI]</color> Bind() called - UI refreshed");
#endif
        }

        // ============================================
        // ICON INITIALIZATION
        // ============================================

        /// <summary>
        /// Initializes the cost icon from ResourceData.
        /// Called once on first refresh, cached for performance.
        /// </summary>
        private void InitializeCostIcon()
        {
            if (iconInitialized) return;
            iconInitialized = true;

            if (ResourceSystem.Instance == null)
            {
                HideCostIcon();
                return;
            }

            // Get ResourceData for the recruit cost resource
            cachedCostResourceData = ResourceSystem.Instance.GetResourceData(RECRUIT_COST_RESOURCE_ID);

            if (cachedCostResourceData != null && cachedCostResourceData.Icon != null)
            {
                if (costIconImage != null)
                {
                    costIconImage.sprite = cachedCostResourceData.Icon;
                    costIconImage.enabled = true;
                    costIconImage.preserveAspect = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"<color=cyan>[RecruitUI]</color> Cost icon set to: {cachedCostResourceData.DisplayName}");
#endif
                }
            }
            else
            {
                HideCostIcon();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (cachedCostResourceData == null)
                {
                    Debug.LogWarning($"<color=orange>[RecruitUI]</color> ResourceData not found for ID: {RECRUIT_COST_RESOURCE_ID}");
                }
                else
                {
                    Debug.LogWarning($"<color=orange>[RecruitUI]</color> ResourceData '{cachedCostResourceData.DisplayName}' has no Icon assigned");
                }
#endif
            }
        }

        private void HideCostIcon()
        {
            if (costIconImage != null)
            {
                costIconImage.enabled = false;
            }
        }

        // ============================================
        // UI UPDATE
        // ============================================

        private void RefreshUI()
        {
            // Initialize icon on first refresh (cached)
            InitializeCostIcon();

            if (PopulationSystem.Instance == null)
            {
                SetButtonState(false, "System not ready");
                return;
            }

            bool canRecruit = PopulationSystem.Instance.CanRecruit(out RecruitBlockReason reason);
            lastBlockReason = reason;

            int cost = PopulationSystem.Instance.RecruitCost();

            // Update cost text
            if (costText != null)
            {
                costText.text = cost.ToString();

                // Color based on affordability
                if (ResourceSystem.Instance != null)
                {
                    int currentResource = Mathf.FloorToInt(ResourceSystem.Instance.GetResourceAmount(RECRUIT_COST_RESOURCE_ID));
                    costText.color = currentResource >= cost ? enabledColor : disabledColor;
                }
            }

            // Update button text
            if (buttonText != null)
            {
                if (PopulationSystem.Instance.HasQueuedRecruits)
                {
                    buttonText.text = $"Recruit ({PopulationSystem.Instance.QueuedRecruits} queued)";
                }
                else
                {
                    buttonText.text = "Recruit Worker";
                }
            }

            // Update button state
            string tooltipReason = canRecruit ? "" : PopulationSystem.Instance.GetBlockReasonText(reason);
            SetButtonState(canRecruit, tooltipReason);
        }

        private void SetButtonState(bool enabled, string tooltipReason)
        {
            if (recruitButton != null)
            {
                recruitButton.interactable = enabled;
            }

            // Update icon color based on state
            if (costIconImage != null && costIconImage.enabled)
            {
                costIconImage.color = enabled ? enabledColor : disabledColor;
            }

            // Update tooltip
            if (!string.IsNullOrEmpty(tooltipReason))
            {
                if (tooltipText != null)
                {
                    tooltipText.text = tooltipReason;
                }
            }
        }

        // ============================================
        // BUTTON CALLBACK
        // ============================================

        private void OnRecruitClicked()
        {
            if (PopulationSystem.Instance == null) return;

            if (PopulationSystem.Instance.RequestRecruit())
            {
                // Success feedback
                RefreshUI();

                // Optional: play SFX, show popup, etc.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("<color=green>[RecruitUI]</color> Recruit requested successfully!");
#endif
            }
            else
            {
                // Failure feedback (should rarely happen since button is disabled)
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"<color=orange>[RecruitUI]</color> Recruit failed: {lastBlockReason}");
#endif
            }
        }

        // ============================================
        // TOOLTIP HOVER (optional)
        // ============================================

        public void OnPointerEnter()
        {
            if (tooltipPanel != null && lastBlockReason != RecruitBlockReason.None)
            {
                tooltipPanel.SetActive(true);
            }
        }

        public void OnPointerExit()
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }

        // ============================================
        // DEBUG
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug")]
        [Button("Force Refresh")]
        private void DebugForceRefresh()
        {
            iconInitialized = false;
            RefreshUI();
        }

        [TitleGroup("Debug")]
        [ShowInInspector, ReadOnly]
        private string CurrentCostResource => cachedCostResourceData != null 
            ? $"{cachedCostResourceData.DisplayName} (Icon: {(cachedCostResourceData.Icon != null ? "✓" : "✗")})"
            : "Not initialized";
#endif
    }
}
