using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Sirenix.OdinInspector;
using WildernessSurvival.Gameplay.Resources;

namespace WildernessSurvival.UI
{
    /// <summary>
    /// Display delle risorse nella HUD.
    /// Mostra quantità corrente, max storage, e production rate.
    /// </summary>
    public class ResourceDisplayUI : MonoBehaviour
    {
        // ============================================
        // SINGLETON
        // ============================================
        
        public static ResourceDisplayUI Instance { get; private set; }

        // ============================================
        // CONFIGURAZIONE
        // ============================================

        [TitleGroup("Resource Displays")]
        [SerializeField] private ResourceDisplayItem warmwoodDisplay;
        [SerializeField] private ResourceDisplayItem shardDisplay;
        [SerializeField] private ResourceDisplayItem foodDisplay;

        [TitleGroup("Configurazione")]
        [SerializeField] private float updateInterval = 0.25f;
        [SerializeField] private bool showProductionRate = true;

        [TitleGroup("Animazione")]
        [SerializeField] private float changeAnimationDuration = 0.3f;
        [SerializeField] private Color increaseColor = new Color(0.3f, 1f, 0.3f);
        [SerializeField] private Color decreaseColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private float pulseScale = 1.2f;

        [TitleGroup("Low Resource Warning")]
        [SerializeField] private bool enableLowWarning = true;
        [SerializeField] private float lowThresholdPercent = 0.2f;
        [SerializeField] private Color lowWarningColor = new Color(1f, 0.5f, 0f);
        [SerializeField] private float warningPulseSpeed = 2f;

        [TitleGroup("Debug")]
        [SerializeField] private bool debugMode = false;

        // ============================================
        // RUNTIME
        // ============================================

        private float lastWarmwood = 0;
        private float lastShard = 0;
        private float lastFood = 0;
        private float updateTimer = 0;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Inizializza valori
            if (ResourceSystem.Instance != null)
            {
                lastWarmwood = ResourceSystem.Instance.GetResourceAmount("warmwood");
                lastShard = ResourceSystem.Instance.GetResourceAmount("shard");
                lastFood = ResourceSystem.Instance.GetResourceAmount("food");
            }

            UpdateAllDisplays();
        }

        private void Update()
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                updateTimer = 0;
                UpdateAllDisplays();
            }

            // Low resource warning pulse
            if (enableLowWarning)
            {
                UpdateLowResourceWarnings();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ============================================
        // UPDATE DISPLAYS
        // ============================================

        private void UpdateAllDisplays()
        {
            if (ResourceSystem.Instance == null) return;

            UpdateSingleDisplay(warmwoodDisplay, "warmwood", ref lastWarmwood);
            UpdateSingleDisplay(shardDisplay, "shard", ref lastShard);
            UpdateSingleDisplay(foodDisplay, "food", ref lastFood);
        }

        private void UpdateSingleDisplay(ResourceDisplayItem display, string resourceId, ref float lastValue)
        {
            if (display == null || !display.IsValid()) return;

            float current = ResourceSystem.Instance.GetResourceAmount(resourceId);
            
            // FIX: GetMaxStorage non esiste in ResourceSystem, uso GetResourceData
            float max = float.MaxValue;
            var data = ResourceSystem.Instance.GetResourceData(resourceId);
            if (data != null && data.HasMaxStorage)
            {
                max = data.MaxStorage;
            }
            
            // Detect change
            float delta = current - lastValue;
            if (Mathf.Abs(delta) > 0.1f)
            {
                display.AnimateChange(delta > 0, delta, increaseColor, decreaseColor, pulseScale);
                
                if (debugMode)
                {
                    string sign = delta > 0 ? "+" : "";
                    Debug.Log($"<color=cyan>[ResourceUI]</color> {resourceId}: {sign}{delta:F0}");
                }
            }
            lastValue = current;

            // Update values
            display.SetValues(current, max);
        }

        private void UpdateLowResourceWarnings()
        {
            if (ResourceSystem.Instance == null) return;

            float pulse = (Mathf.Sin(Time.time * warningPulseSpeed) + 1f) / 2f;

            CheckLowWarning(warmwoodDisplay, "warmwood", pulse);
            CheckLowWarning(shardDisplay, "shard", pulse);
            CheckLowWarning(foodDisplay, "food", pulse);
        }

        private void CheckLowWarning(ResourceDisplayItem display, string resourceId, float pulse)
        {
            if (display == null || !display.IsValid()) return;

            float current = ResourceSystem.Instance.GetResourceAmount(resourceId);
            
            // FIX: GetMaxStorage non esiste
            float max = float.MaxValue;
            var data = ResourceSystem.Instance.GetResourceData(resourceId);
            if (data != null && data.HasMaxStorage)
            {
                max = data.MaxStorage;
            }

            float ratio = max > 0 ? current / max : 1f;

            if (ratio < lowThresholdPercent && current > 0)
            {
                display.SetWarningPulse(pulse, lowWarningColor);
            }
            else
            {
                display.ClearWarning();
            }
        }

        // ============================================
        // PUBLIC API
        // ============================================

        [TitleGroup("Azioni")]
        [Button("Force Update", ButtonSizes.Medium)]
        public void ForceUpdate()
        {
            UpdateAllDisplays();
        }

        /// <summary>
        /// Mostra animazione cambio risorsa (chiamato esternamente)
        /// </summary>
        public void ShowChangeAnimation(string resourceId, float amount)
        {
            ResourceDisplayItem display = GetDisplayForResource(resourceId);
            
            if (display != null && display.IsValid())
            {
                display.ShowFloatingText(amount, increaseColor, decreaseColor);
            }
        }

        private ResourceDisplayItem GetDisplayForResource(string resourceId)
        {
            return resourceId?.ToLower() switch
            {
                "warmwood" => warmwoodDisplay,
                "shard" => shardDisplay,
                "food" => foodDisplay,
                _ => null
            };
        }

        // ============================================
        // DEBUG
        // ============================================

        [TitleGroup("Debug")]
        [Button("Test Increase Animation")]
        private void DebugTestIncrease()
        {
            if (warmwoodDisplay != null)
            {
                warmwoodDisplay.AnimateChange(true, 50, increaseColor, decreaseColor, pulseScale);
                warmwoodDisplay.ShowFloatingText(50, increaseColor, decreaseColor);
            }
        }

        [Button("Test Decrease Animation")]
        private void DebugTestDecrease()
        {
            if (warmwoodDisplay != null)
            {
                warmwoodDisplay.AnimateChange(false, -30, increaseColor, decreaseColor, pulseScale);
                warmwoodDisplay.ShowFloatingText(-30, increaseColor, decreaseColor);
            }
        }
    }

    // ============================================
    // RESOURCE DISPLAY ITEM
    // ============================================

    [System.Serializable]
    public class ResourceDisplayItem
    {
        [Header("Riferimenti")]
        public Image iconImage;
        public TextMeshProUGUI amountText;
        public TextMeshProUGUI maxText;
        public Slider progressBar;
        public Image progressFill;
        public RectTransform container;

        [Header("Floating Text")]
        public TextMeshProUGUI floatingTextPrefab;
        public Transform floatingTextSpawn;

        [Header("Colori Progress Bar")]
        public Color normalColor = new Color(0.3f, 0.7f, 0.3f);
        public Color lowColor = new Color(0.9f, 0.7f, 0.2f);
        public Color criticalColor = new Color(0.9f, 0.3f, 0.2f);
        public Color fullColor = new Color(0.3f, 0.8f, 0.9f);

        // Runtime
        private Coroutine pulseCoroutine;
        private Color originalIconColor = Color.white;
        private bool isWarning = false;

        public bool IsValid()
        {
            return amountText != null;
        }

        public void SetValues(float current, float max)
        {
            int currentInt = Mathf.FloorToInt(current);
            int maxInt = Mathf.FloorToInt(max);

            if (amountText != null)
            {
                amountText.text = FormatNumber(currentInt);
            }

            if (maxText != null)
            {
                if (max < 1000000) // Non mostrare max se è enorme (es. inf)
                    maxText.text = $"/{FormatNumber(maxInt)}";
                else
                    maxText.text = "";
            }

            if (progressBar != null)
            {
                progressBar.maxValue = max;
                progressBar.value = current;
            }

            // Colore basato su riempimento
            if (progressFill != null && !isWarning)
            {
                float ratio = max > 0 ? current / max : 0;
                progressFill.color = GetColorForRatio(ratio);
            }
        }

        private string FormatNumber(int value)
        {
            if (value >= 1000000)
            {
                return $"{value / 1000000f:F1}M";
            }
            if (value >= 10000)
            {
                return $"{value / 1000f:F1}K";
            }
            return value.ToString();
        }

        private Color GetColorForRatio(float ratio)
        {
            if (ratio >= 0.95f) return fullColor;
            if (ratio < 0.1f) return criticalColor;
            if (ratio < 0.25f) return lowColor;
            return normalColor;
        }

        public void AnimateChange(bool increase, float delta, Color incColor, Color decColor, float scale)
        {
            if (container == null && iconImage == null) return;

            // Store original color
            if (iconImage != null)
            {
                originalIconColor = iconImage.color;
            }

            // Start pulse animation
            if (container != null)
            {
                // Usa MonoBehaviour.StartCoroutine tramite un helper
                var helper = container.GetComponent<CoroutineHelper>();
                if (helper == null)
                {
                    helper = container.gameObject.AddComponent<CoroutineHelper>();
                }
                
                if (pulseCoroutine != null)
                {
                    helper.StopCoroutine(pulseCoroutine);
                }
                pulseCoroutine = helper.StartCoroutine(PulseRoutine(increase, incColor, decColor, scale));
            }
        }

        private IEnumerator PulseRoutine(bool increase, Color incColor, Color decColor, float scale)
        {
            float duration = 0.15f;
            float elapsed = 0;
            Vector3 originalScale = Vector3.one;
            Vector3 targetScale = Vector3.one * scale;
            Color flashColor = increase ? incColor : decColor;

            // Flash icon
            if (iconImage != null)
            {
                iconImage.color = flashColor;
            }

            // Scale up
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                if (container != null)
                {
                    container.localScale = Vector3.Lerp(originalScale, targetScale, t);
                }
                yield return null;
            }

            // Scale down
            elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                if (container != null)
                {
                    container.localScale = Vector3.Lerp(targetScale, originalScale, t);
                }
                if (iconImage != null)
                {
                    iconImage.color = Color.Lerp(flashColor, originalIconColor, t);
                }
                yield return null;
            }

            // Reset
            if (container != null)
            {
                container.localScale = originalScale;
            }
            if (iconImage != null)
            {
                iconImage.color = originalIconColor;
            }
        }

        public void ShowFloatingText(float amount, Color incColor, Color decColor)
        {
            if (floatingTextPrefab == null) return;
            
            Transform spawnPoint = floatingTextSpawn != null ? floatingTextSpawn : 
                                   (container != null ? container : null);
            
            if (spawnPoint == null) return;

            TextMeshProUGUI floating = Object.Instantiate(floatingTextPrefab, spawnPoint.position, Quaternion.identity, spawnPoint.parent);
            string sign = amount >= 0 ? "+" : "";
            floating.text = $"{sign}{amount:F0}";
            floating.color = amount >= 0 ? incColor : decColor;
            floating.gameObject.SetActive(true);

            // Animate and destroy
            var helper = floating.GetComponent<CoroutineHelper>();
            if (helper == null)
            {
                helper = floating.gameObject.AddComponent<CoroutineHelper>();
            }
            helper.StartCoroutine(FloatAndFade(floating));
        }

        private IEnumerator FloatAndFade(TextMeshProUGUI text)
        {
            float duration = 1f;
            float elapsed = 0;
            Vector3 startPos = text.transform.position;
            Color startColor = text.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Float up
                text.transform.position = startPos + Vector3.up * (t * 50f);
                
                // Fade out
                Color c = startColor;
                c.a = 1f - t;
                text.color = c;

                yield return null;
            }

            Object.Destroy(text.gameObject);
        }

        public void SetWarningPulse(float pulse, Color warningColor)
        {
            isWarning = true;
            
            if (progressFill != null)
            {
                progressFill.color = Color.Lerp(criticalColor, warningColor, pulse);
            }

            if (iconImage != null)
            {
                float scale = 1f + (pulse * 0.1f);
                iconImage.transform.localScale = Vector3.one * scale;
            }
        }

        public void ClearWarning()
        {
            if (!isWarning) return;
            
            isWarning = false;
            
            if (iconImage != null)
            {
                iconImage.transform.localScale = Vector3.one;
            }
        }
    }

    // ============================================
    // COROUTINE HELPER
    // ============================================
    
    /// <summary>
    /// Helper per eseguire coroutine da classi non-MonoBehaviour
    /// </summary>
    public class CoroutineHelper : MonoBehaviour
    {
        // Classe vuota, usata solo per StartCoroutine
    }
}
