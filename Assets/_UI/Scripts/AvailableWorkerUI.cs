using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using WildernessSurvival.Gameplay.Workers;
using WildernessSurvival.Gameplay.Structures;

namespace WildernessSurvival.UI
{
    /// <summary>
    /// UI per un worker disponibile (non assegnato) nella lista.
    /// Mostra info worker e pulsante per assegnarlo.
    /// </summary>
    public class AvailableWorkerUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        // ============================================
        // RIFERIMENTI UI
        // ============================================

        [Header("Riferimenti")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI roleText;
        [SerializeField] private TextMeshProUGUI bonusPreviewText;
        [SerializeField] private Button assignButton;
        [SerializeField] private TextMeshProUGUI assignButtonText;

        [Header("Ideal Match Indicator")]
        [SerializeField] private GameObject idealMatchIndicator;
        [SerializeField] private Image idealMatchBorder;

        [Header("Colori")]
        [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.25f, 0.9f);
        [SerializeField] private Color hoverColor = new Color(0.3f, 0.3f, 0.35f, 1f);
        [SerializeField] private Color disabledColor = new Color(0.15f, 0.15f, 0.15f, 0.6f);
        [SerializeField] private Color idealMatchBorderColor = new Color(0.4f, 0.8f, 0.4f, 1f);

        // ============================================
        // RUNTIME
        // ============================================

        private WorkerInstance worker;
        private bool canAssign;
        private bool isIdealMatch;
        private System.Action<WorkerInstance> onAssignCallback;

        public WorkerInstance Worker => worker;

        // ============================================
        // INIZIALIZZAZIONE
        // ============================================

        public void Initialize(WorkerInstance workerInstance, bool canBeAssigned, System.Action<WorkerInstance> onAssign)
        {
            worker = workerInstance;
            canAssign = canBeAssigned;
            onAssignCallback = onAssign;

            // Check if this worker is an ideal match for current structure
            CheckIdealMatch();

            // Setup button
            if (assignButton != null)
            {
                assignButton.onClick.RemoveAllListeners();
                assignButton.onClick.AddListener(OnAssignClicked);
                assignButton.interactable = canAssign;
            }

            // Auto-find components
            AutoFindComponents();

            UpdateVisuals();
        }

        private void AutoFindComponents()
        {
            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();
            
            if (nameText == null)
                nameText = transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            
            if (roleText == null)
                roleText = transform.Find("RoleText")?.GetComponent<TextMeshProUGUI>();
            
            if (bonusPreviewText == null)
                bonusPreviewText = transform.Find("BonusPreview")?.GetComponent<TextMeshProUGUI>();
            
            if (assignButton == null)
                assignButton = transform.Find("AssignButton")?.GetComponent<Button>();
            
            if (iconImage == null)
                iconImage = transform.Find("Icon")?.GetComponent<Image>();
        }

        private void CheckIdealMatch()
        {
            isIdealMatch = false;
            
            var currentStructure = WorkerAssignmentUI.Instance?.CurrentStructure;
            if (currentStructure != null && worker != null && worker.Data != null)
            {
                isIdealMatch = worker.Data.IsIdealForStructure(currentStructure.Data);
            }
        }

        // ============================================
        // UPDATE VISUALS
        // ============================================

        private void UpdateVisuals()
        {
            if (worker == null || worker.Data == null) return;

            // Worker name
            if (nameText != null)
            {
                nameText.text = worker.CustomName;
                nameText.color = canAssign ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            }

            // Worker role
            if (roleText != null)
            {
                roleText.text = GetRoleDisplayName(worker.Data.DefaultRole);
                roleText.color = worker.Data.RoleColor;
            }

            // Worker icon
            if (iconImage != null)
            {
                if (worker.Data.Icon != null)
                {
                    iconImage.sprite = worker.Data.Icon;
                    iconImage.enabled = true;
                    iconImage.color = canAssign ? Color.white : new Color(0.5f, 0.5f, 0.5f);
                }
                else
                {
                    // Use role color as placeholder
                    iconImage.sprite = null;
                    iconImage.enabled = true;
                    iconImage.color = canAssign ? worker.Data.RoleColor : new Color(0.3f, 0.3f, 0.3f);
                }
            }

            // Bonus preview
            UpdateBonusPreview();

            // Ideal match indicator
            if (idealMatchIndicator != null)
            {
                idealMatchIndicator.SetActive(isIdealMatch && canAssign);
            }

            if (idealMatchBorder != null)
            {
                idealMatchBorder.enabled = isIdealMatch && canAssign;
                idealMatchBorder.color = idealMatchBorderColor;
            }

            // Background
            if (backgroundImage != null)
            {
                backgroundImage.color = canAssign ? normalColor : disabledColor;
            }

            // Assign button
            UpdateAssignButton();
        }

        private void UpdateBonusPreview()
        {
            if (bonusPreviewText == null) return;

            var currentStructure = WorkerAssignmentUI.Instance?.CurrentStructure;
            
            if (currentStructure != null && worker.Data != null)
            {
                float bonus = worker.Data.GetBonusForStructure(currentStructure.Data) * 100f;
                
                if (bonus > 0)
                {
                    bonusPreviewText.text = $"+{bonus:F0}%";
                    
                    if (isIdealMatch)
                    {
                        bonusPreviewText.color = Color.green;
                    }
                    else
                    {
                        bonusPreviewText.color = Color.yellow;
                    }
                }
                else
                {
                    bonusPreviewText.text = "+0%";
                    bonusPreviewText.color = Color.gray;
                }
            }
            else
            {
                bonusPreviewText.text = "";
            }
        }

        private void UpdateAssignButton()
        {
            if (assignButton == null) return;

            assignButton.interactable = canAssign;

            if (assignButtonText != null)
            {
                if (canAssign)
                {
                    assignButtonText.text = isIdealMatch ? "Assign ★" : "Assign";
                    assignButtonText.color = Color.white;
                }
                else
                {
                    assignButtonText.text = "Full";
                    assignButtonText.color = new Color(0.5f, 0.5f, 0.5f);
                }
            }

            // Button color
            var buttonImage = assignButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                if (canAssign)
                {
                    buttonImage.color = isIdealMatch 
                        ? new Color(0.3f, 0.6f, 0.3f) 
                        : new Color(0.3f, 0.4f, 0.5f);
                }
                else
                {
                    buttonImage.color = new Color(0.2f, 0.2f, 0.2f);
                }
            }
        }

        private string GetRoleDisplayName(WorkerRole role)
        {
            return role switch
            {
                WorkerRole.Gatherer => "Gatherer",
                WorkerRole.Builder => "Builder",
                WorkerRole.Guard => "Guard",
                WorkerRole.Scout => "Scout",
                WorkerRole.Crafter => "Crafter",
                WorkerRole.Researcher => "Researcher",
                _ => role.ToString()
            };
        }

        // ============================================
        // EVENTS
        // ============================================

        private void OnAssignClicked()
        {
            if (worker != null && canAssign)
            {
                onAssignCallback?.Invoke(worker);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (canAssign && backgroundImage != null)
            {
                backgroundImage.color = hoverColor;
            }

            // Scale up slightly
            transform.localScale = Vector3.one * 1.03f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = canAssign ? normalColor : disabledColor;
            }

            // Reset scale
            transform.localScale = Vector3.one;
        }

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Aggiorna stato del pulsante assign
        /// </summary>
        public void SetCanAssign(bool value)
        {
            canAssign = value;
            UpdateVisuals();
        }

        /// <summary>
        /// Rinfresca tutti i visual
        /// </summary>
        public void Refresh()
        {
            CheckIdealMatch();
            UpdateVisuals();
        }
    }
}
