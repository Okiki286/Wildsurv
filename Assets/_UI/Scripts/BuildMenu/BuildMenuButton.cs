using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using WildernessSurvival.Gameplay.Structures;

namespace WildernessSurvival.UI
{
    /// <summary>
    /// Singolo bottone nel menu costruzione.
    /// Gestisce visualizzazione, hover, click.
    /// </summary>
    public class BuildMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        // ============================================
        // RIFERIMENTI UI
        // ============================================

        [Header("Riferimenti")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI hotkeyText;
        [SerializeField] private GameObject lockedOverlay;
        [SerializeField] private GameObject selectedIndicator;

        [Header("Colori")]
        [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.25f, 0.95f);
        [SerializeField] private Color hoverColor = new Color(0.3f, 0.35f, 0.4f, 1f);
        [SerializeField] private Color selectedColor = new Color(0.2f, 0.5f, 0.3f, 1f);
        [SerializeField] private Color unaffordableColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        [SerializeField] private Color unaffordableTextColor = new Color(1f, 0.3f, 0.3f, 1f);

        // ============================================
        // RUNTIME
        // ============================================

        private StructureData data;
        private int hotkeyNumber;
        private bool isAffordable = true;
        private bool isSelected = false;

        private System.Action<StructureData> onSelectCallback;
        private System.Action<StructureData> onHoverCallback;
        private System.Action<StructureData> onHoverExitCallback;

        public StructureData Data => data;

        // ============================================
        // INIZIALIZZAZIONE
        // ============================================

        public void Initialize(
            StructureData structureData, 
            int hotkey,
            System.Action<StructureData> onSelect,
            System.Action<StructureData> onHover,
            System.Action<StructureData> onHoverExit)
        {
            data = structureData;
            hotkeyNumber = hotkey;
            onSelectCallback = onSelect;
            onHoverCallback = onHover;
            onHoverExitCallback = onHoverExit;

            // Auto-find components se non assegnati
            if (iconImage == null) iconImage = transform.Find("Icon")?.GetComponent<Image>();
            if (backgroundImage == null) backgroundImage = GetComponent<Image>();
            if (nameText == null) nameText = transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            if (costText == null) costText = transform.Find("CostText")?.GetComponent<TextMeshProUGUI>();
            if (hotkeyText == null) hotkeyText = transform.Find("HotkeyText")?.GetComponent<TextMeshProUGUI>();

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (data == null) return;

            // Nome
            if (nameText != null)
            {
                nameText.text = data.DisplayName;
            }

            // Icona
            if (iconImage != null)
            {
                if (data.Icon != null)
                {
                    iconImage.sprite = data.Icon;
                    iconImage.enabled = true;
                    iconImage.color = Color.white;
                }
                else
                {
                    // Usa colore categoria come placeholder
                    iconImage.enabled = true;
                    iconImage.sprite = null;
                    iconImage.color = GetCategoryColor(data.Category);
                }
            }

            // Hotkey
            if (hotkeyText != null)
            {
                if (hotkeyNumber <= 9)
                {
                    hotkeyText.text = hotkeyNumber.ToString();
                    hotkeyText.gameObject.SetActive(true);
                }
                else
                {
                    hotkeyText.gameObject.SetActive(false);
                }
            }

            // Costo breve
            if (costText != null)
            {
                costText.text = GetShortCostString();
            }

            // Overlay locked
            if (lockedOverlay != null)
            {
                lockedOverlay.SetActive(false); // TODO: tech tree lock
            }

            // Selected indicator
            if (selectedIndicator != null)
            {
                selectedIndicator.SetActive(isSelected);
            }

            UpdateAffordabilityVisuals();
        }

        private string GetShortCostString()
        {
            if (data == null || data.BuildCosts == null || data.BuildCosts.Length == 0)
            {
                return "<color=#44FF44>Free</color>";
            }

            // Mostra tutti i costi in formato compatto
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < data.BuildCosts.Length && i < 3; i++)
            {
                var cost = data.BuildCosts[i];
                string icon = GetResourceIcon(cost.resourceId);
                if (i > 0) sb.Append(" ");
                sb.Append($"{icon}{cost.amount}");
            }
            return sb.ToString();
        }

        private string GetResourceIcon(string resourceId)
        {
            return resourceId?.ToLower() switch
            {
                "warmwood" => "W",
                "shard" => "S",
                "food" => "F",
                _ => "?"
            };
        }

        private Color GetCategoryColor(StructureCategory category)
        {
            return category switch
            {
                StructureCategory.Resource => new Color(0.4f, 0.7f, 0.3f),  // Verde
                StructureCategory.Defense => new Color(0.7f, 0.3f, 0.3f),   // Rosso
                StructureCategory.Utility => new Color(0.3f, 0.5f, 0.7f),   // Blu
                StructureCategory.Tech => new Color(0.6f, 0.3f, 0.7f),      // Viola
                _ => new Color(0.5f, 0.5f, 0.5f)                            // Grigio
            };
        }

        // ============================================
        // AFFORDABILITY
        // ============================================

        public void SetAffordable(bool affordable)
        {
            if (isAffordable == affordable) return;
            
            isAffordable = affordable;
            UpdateAffordabilityVisuals();
        }

        private void UpdateAffordabilityVisuals()
        {
            if (backgroundImage != null)
            {
                if (!isAffordable)
                {
                    backgroundImage.color = unaffordableColor;
                }
                else if (isSelected)
                {
                    backgroundImage.color = selectedColor;
                }
                else
                {
                    backgroundImage.color = normalColor;
                }
            }

            if (costText != null)
            {
                costText.color = isAffordable ? Color.white : unaffordableTextColor;
            }

            if (iconImage != null)
            {
                float alpha = isAffordable ? 1f : 0.5f;
                Color c = iconImage.color;
                c.a = alpha;
                iconImage.color = c;
            }

            if (nameText != null)
            {
                nameText.color = isAffordable ? Color.white : new Color(0.6f, 0.6f, 0.6f);
            }
        }

        // ============================================
        // SELEZIONE
        // ============================================

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            
            if (backgroundImage != null)
            {
                backgroundImage.color = selected ? selectedColor : (isAffordable ? normalColor : unaffordableColor);
            }

            if (selectedIndicator != null)
            {
                selectedIndicator.SetActive(selected);
            }
        }

        // ============================================
        // POINTER EVENTS
        // ============================================

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (backgroundImage != null && isAffordable && !isSelected)
            {
                backgroundImage.color = hoverColor;
            }

            // Scala leggermente
            transform.localScale = Vector3.one * 1.05f;

            onHoverCallback?.Invoke(data);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (backgroundImage != null && !isSelected)
            {
                backgroundImage.color = isAffordable ? normalColor : unaffordableColor;
            }

            // Reset scala
            transform.localScale = Vector3.one;

            onHoverExitCallback?.Invoke(data);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                onSelectCallback?.Invoke(data);
            }
        }

        // ============================================
        // ANIMATION HELPERS
        // ============================================

        public void PlaySelectAnimation()
        {
            // TODO: Animazione selezione (pulse, glow, etc)
            StartCoroutine(PulseAnimation());
        }

        private System.Collections.IEnumerator PulseAnimation()
        {
            Vector3 originalScale = Vector3.one;
            Vector3 targetScale = Vector3.one * 1.1f;
            
            float duration = 0.1f;
            float elapsed = 0f;
            
            // Scale up
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
                yield return null;
            }
            
            // Scale down
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
                yield return null;
            }
            
            transform.localScale = originalScale;
        }
    }
}
