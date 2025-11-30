using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using WildernessSurvival.Gameplay.Workers;

namespace WildernessSurvival.UI
{
    /// <summary>
    /// UI per un singolo slot worker in una struttura.
    /// Mostra worker assegnato o stato vuoto.
    /// </summary>
    public class WorkerSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        // ============================================
        // RIFERIMENTI UI
        // ============================================

        [Header("Riferimenti")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image workerIconImage;
        [SerializeField] private TextMeshProUGUI workerNameText;
        [SerializeField] private TextMeshProUGUI workerRoleText;
        [SerializeField] private TextMeshProUGUI bonusText;
        [SerializeField] private Button removeButton;
        [SerializeField] private TextMeshProUGUI removeButtonText;
        
        [Header("State Objects")]
        [SerializeField] private GameObject emptyStateObject;
        [SerializeField] private GameObject filledStateObject;

        [Header("Colori")]
        [SerializeField] private Color emptyColor = new Color(0.15f, 0.15f, 0.2f, 0.7f);
        [SerializeField] private Color filledColor = new Color(0.2f, 0.35f, 0.25f, 0.9f);
        [SerializeField] private Color hoverColor = new Color(0.25f, 0.4f, 0.3f, 1f);
        [SerializeField] private Color idealMatchColor = new Color(0.3f, 0.5f, 0.3f, 0.95f);

        // ============================================
        // RUNTIME
        // ============================================

        private WorkerInstance worker;
        private System.Action<WorkerInstance> onRemoveCallback;
        private bool isHovered = false;

        public WorkerInstance Worker => worker;
        public bool IsEmpty => worker == null;

        // ============================================
        // INIZIALIZZAZIONE
        // ============================================

        public void Initialize(WorkerInstance workerInstance, System.Action<WorkerInstance> onRemove)
        {
            worker = workerInstance;
            onRemoveCallback = onRemove;

            // Setup remove button
            if (removeButton != null)
            {
                removeButton.onClick.RemoveAllListeners();
                removeButton.onClick.AddListener(OnRemoveClicked);
            }

            // Auto-find components if not assigned
            AutoFindComponents();

            UpdateVisuals();
        }

        private void AutoFindComponents()
        {
            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();
            
            if (workerNameText == null)
                workerNameText = transform.Find("WorkerName")?.GetComponent<TextMeshProUGUI>();
            
            if (workerRoleText == null)
                workerRoleText = transform.Find("WorkerRole")?.GetComponent<TextMeshProUGUI>();
            
            if (bonusText == null)
                bonusText = transform.Find("BonusText")?.GetComponent<TextMeshProUGUI>();
            
            if (removeButton == null)
                removeButton = transform.Find("RemoveButton")?.GetComponent<Button>();
            
            if (workerIconImage == null)
                workerIconImage = transform.Find("Icon")?.GetComponent<Image>();
        }

        // ============================================
        // UPDATE VISUALS
        // ============================================

        private void UpdateVisuals()
        {
            bool isEmpty = worker == null;

            // Toggle state objects
            if (emptyStateObject != null)
                emptyStateObject.SetActive(isEmpty);

            if (filledStateObject != null)
                filledStateObject.SetActive(!isEmpty);

            // Background color
            UpdateBackgroundColor();

            if (isEmpty)
            {
                UpdateEmptyState();
            }
            else
            {
                UpdateFilledState();
            }
        }

        private void UpdateBackgroundColor()
        {
            if (backgroundImage == null) return;

            if (IsEmpty)
            {
                backgroundImage.color = emptyColor;
            }
            else if (isHovered)
            {
                backgroundImage.color = hoverColor;
            }
            else if (worker.IsIdealMatch())
            {
                backgroundImage.color = idealMatchColor;
            }
            else
            {
                backgroundImage.color = filledColor;
            }
        }

        private void UpdateEmptyState()
        {
            if (workerNameText != null)
            {
                workerNameText.text = "Empty Slot";
                workerNameText.color = new Color(0.5f, 0.5f, 0.5f);
            }

            if (workerRoleText != null)
            {
                workerRoleText.text = "Click worker to assign";
                workerRoleText.color = new Color(0.4f, 0.4f, 0.4f);
            }

            if (bonusText != null)
            {
                bonusText.text = "";
            }

            if (workerIconImage != null)
            {
                workerIconImage.enabled = false;
            }

            if (removeButton != null)
            {
                removeButton.gameObject.SetActive(false);
            }
        }

        private void UpdateFilledState()
        {
            // Worker name
            if (workerNameText != null)
            {
                workerNameText.text = worker.CustomName;
                workerNameText.color = Color.white;
            }

            // Worker role with icon
            if (workerRoleText != null)
            {
                workerRoleText.text = GetRoleDisplayName(worker.Data.Role);
                workerRoleText.color = worker.Data.RoleColor;
            }

            // Bonus display
            if (bonusText != null)
            {
                float bonus = worker.GetCurrentBonus() * 100f;
                if (bonus > 0)
                {
                    bonusText.text = $"+{bonus:F0}%";
                    bonusText.color = worker.IsIdealMatch() ? Color.green : Color.yellow;
                }
                else
                {
                    bonusText.text = "+0%";
                    bonusText.color = Color.gray;
                }
            }

            // Worker icon
            if (workerIconImage != null)
            {
                if (worker.Data.Icon != null)
                {
                    workerIconImage.sprite = worker.Data.Icon;
                    workerIconImage.enabled = true;
                    workerIconImage.color = Color.white;
                }
                else
                {
                    // Use role color as placeholder
                    workerIconImage.sprite = null;
                    workerIconImage.enabled = true;
                    workerIconImage.color = worker.Data.RoleColor;
                }
            }

            // Remove button
            if (removeButton != null)
            {
                removeButton.gameObject.SetActive(true);
                
                if (removeButtonText != null)
                {
                    removeButtonText.text = "Remove";
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

        private void OnRemoveClicked()
        {
            if (worker != null)
            {
                onRemoveCallback?.Invoke(worker);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            UpdateBackgroundColor();
            
            // Slight scale up
            transform.localScale = Vector3.one * 1.02f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            UpdateBackgroundColor();
            
            // Reset scale
            transform.localScale = Vector3.one;
        }

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Aggiorna i dati visualizzati (chiamato quando bonus cambia)
        /// </summary>
        public void Refresh()
        {
            UpdateVisuals();
        }

        /// <summary>
        /// Imposta un nuovo worker in questo slot
        /// </summary>
        public void SetWorker(WorkerInstance newWorker)
        {
            worker = newWorker;
            UpdateVisuals();
        }

        /// <summary>
        /// Pulisce lo slot
        /// </summary>
        public void Clear()
        {
            worker = null;
            UpdateVisuals();
        }
    }
}
