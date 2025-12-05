using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;
using WildernessSurvival.Gameplay.Structures;

namespace WildernessSurvival.UI
{
    /// <summary>
    /// Gestisce la UI world-space per mostrare il progresso di costruzione.
    /// Mostra una barra di progresso sopra la struttura mentre è in stato Building.
    /// </summary>
    public class StructureStatusUI : MonoBehaviour
    {
        // ============================================
        // REFERENCES
        // ============================================

        [TitleGroup("References")]
        [SerializeField, ReadOnly]
        [Tooltip("Riferimento al StructureController (trovato automaticamente)")]
        private StructureController structure;

        [SerializeField]
        [Tooltip("Canvas World Space che contiene la UI")]
        private GameObject uiCanvas;

        [SerializeField]
        [Tooltip("Image della barra di progresso (deve essere di tipo Filled)")]
        private Image progressBarFill;

        [SerializeField]
        [Tooltip("Background della barra di progresso")]
        private Image progressBarBackground;

        [SerializeField]
        [Tooltip("Testo percentuale (es. '45%')")]
        private TextMeshProUGUI progressText;

        // ============================================
        // SETTINGS
        // ============================================

        [TitleGroup("Settings")]
        [SerializeField]
        [Tooltip("Offset verticale della UI rispetto alla struttura")]
        [PropertyRange(1f, 10f)]
        private float heightOffset = 3f;

        [SerializeField]
        [Tooltip("Scala della UI")]
        [PropertyRange(0.001f, 0.05f)]
        private float uiScale = 0.01f;

        [SerializeField]
        [Tooltip("Colore della barra quando inizia (0%)")]
        private Color startColor = new Color(1f, 0.3f, 0.3f); // Rosso

        [SerializeField]
        [Tooltip("Colore della barra quando finisce (100%)")]
        private Color endColor = new Color(0.3f, 1f, 0.3f); // Verde

        [SerializeField]
        [Tooltip("Mostra la percentuale come testo")]
        private bool showPercentageText = true;

        // ============================================
        // RUNTIME
        // ============================================

        private Camera mainCamera;
        private bool isInitialized = false;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            // Trova il StructureController sullo stesso GameObject o nei parent
            structure = GetComponent<StructureController>();
            if (structure == null)
            {
                structure = GetComponentInParent<StructureController>();
            }

            if (structure == null)
            {
                Debug.LogError($"[StructureStatusUI] StructureController non trovato su {gameObject.name}!");
                enabled = false;
                return;
            }

            // Cache della camera
            mainCamera = Camera.main;

            // Inizializza la UI
            InitializeUI();

            isInitialized = true;
        }

        private void Start()
        {
            // Nascondi all'inizio se non siamo in Building
            UpdateVisibility();
        }

        private void LateUpdate()
        {
            if (!isInitialized || structure == null) return;

            // Aggiorna la camera se necessario
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null) return;
            }

            // Aggiorna visibilità
            UpdateVisibility();

            // Se la UI è attiva, aggiornala
            if (uiCanvas != null && uiCanvas.activeSelf)
            {
                UpdatePosition();
                UpdateProgressBar();
                UpdateBillboard();
            }
        }

        // ============================================
        // INITIALIZATION
        // ============================================

        private void InitializeUI()
        {
            // Se non c'è una Canvas, prova a crearla automaticamente
            if (uiCanvas == null)
            {
                CreateDefaultUI();
            }

            // Imposta la scala iniziale
            if (uiCanvas != null)
            {
                uiCanvas.transform.localScale = Vector3.one * uiScale;
            }
        }

        private void CreateDefaultUI()
        {
            // Crea Canvas
            GameObject canvasObj = new GameObject("ProgressCanvas");
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.localPosition = Vector3.up * heightOffset;
            canvasObj.transform.localScale = Vector3.one * uiScale;

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(200, 30);

            // Crea Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            progressBarBackground = bgObj.AddComponent<Image>();
            progressBarBackground.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            // Crea Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(canvasObj.transform);
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2, 2);
            fillRect.offsetMax = new Vector2(-2, -2);
            fillRect.pivot = new Vector2(0, 0.5f);
            progressBarFill = fillObj.AddComponent<Image>();
            progressBarFill.type = Image.Type.Filled;
            progressBarFill.fillMethod = Image.FillMethod.Horizontal;
            progressBarFill.fillOrigin = 0;
            progressBarFill.color = startColor;

            // Crea Text
            GameObject textObj = new GameObject("PercentText");
            textObj.transform.SetParent(canvasObj.transform);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            progressText = textObj.AddComponent<TextMeshProUGUI>();
            progressText.alignment = TextAlignmentOptions.Center;
            progressText.fontSize = 20;
            progressText.color = Color.white;
            progressText.text = "0%";

            uiCanvas = canvasObj;

            Debug.Log($"[StructureStatusUI] Created default progress UI for {structure.name}");
        }

        // ============================================
        // UI UPDATES
        // ============================================

        private void UpdateVisibility()
        {
            if (uiCanvas == null || structure == null) return;

            // Mostra SOLO se in costruzione E progresso < 100%
            bool shouldShow = structure.State == StructureState.Building && 
                              structure.BuildProgress < 1.0f;

            if (uiCanvas.activeSelf != shouldShow)
            {
                uiCanvas.SetActive(shouldShow);
            }
        }

        private void UpdatePosition()
        {
            if (uiCanvas == null || structure == null) return;

            // Posiziona la canvas sopra la struttura
            Vector3 worldPos = structure.transform.position + Vector3.up * heightOffset;
            uiCanvas.transform.position = worldPos;
        }

        private void UpdateProgressBar()
        {
            if (progressBarFill == null || structure == null) return;

            // Ottieni il progresso (0-1)
            float progress = structure.BuildProgress;

            // Aggiorna il fill amount
            progressBarFill.fillAmount = progress;

            // Interpola il colore
            progressBarFill.color = Color.Lerp(startColor, endColor, progress);

            // Aggiorna il testo
            if (progressText != null && showPercentageText)
            {
                int percentage = Mathf.RoundToInt(progress * 100f);
                progressText.text = $"{percentage}%";
            }
        }

        private void UpdateBillboard()
        {
            if (uiCanvas == null || mainCamera == null) return;

            // Fai ruotare la Canvas per guardare sempre la camera
            Vector3 directionToCamera = uiCanvas.transform.position - mainCamera.transform.position;
            if (directionToCamera.sqrMagnitude > 0.01f)
            {
                uiCanvas.transform.rotation = Quaternion.LookRotation(directionToCamera);
            }
        }

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Forza l'aggiornamento della barra di progresso.
        /// </summary>
        public void ForceUpdate()
        {
            if (isInitialized)
            {
                UpdateVisibility();
                UpdatePosition();
                UpdateProgressBar();
            }
        }

        /// <summary>
        /// Imposta manualmente il progresso (0-1).
        /// </summary>
        public void SetProgress(float progress)
        {
            if (progressBarFill != null)
            {
                progressBarFill.fillAmount = Mathf.Clamp01(progress);
                progressBarFill.color = Color.Lerp(startColor, endColor, progress);
            }

            if (progressText != null && showPercentageText)
            {
                int percentage = Mathf.RoundToInt(progress * 100f);
                progressText.text = $"{percentage}%";
            }
        }

        // ============================================
        // DEBUG
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug")]
        [Button("🔧 Setup Default UI", ButtonSizes.Medium)]
        private void DebugSetupUI()
        {
            if (uiCanvas == null)
            {
                CreateDefaultUI();
            }
        }

        [Button("👁 Toggle Visibility", ButtonSizes.Medium)]
        private void DebugToggleVisibility()
        {
            if (uiCanvas != null)
            {
                uiCanvas.SetActive(!uiCanvas.activeSelf);
            }
        }

        [Button("📊 Test Progress 50%", ButtonSizes.Medium)]
        private void DebugTestProgress50()
        {
            SetProgress(0.5f);
            if (uiCanvas != null) uiCanvas.SetActive(true);
        }

        [Button("📊 Test Progress 100%", ButtonSizes.Medium)]
        private void DebugTestProgress100()
        {
            SetProgress(1.0f);
            // Dovrebbe nascondersi automaticamente
        }

        private void OnDrawGizmosSelected()
        {
            // Disegna la posizione della UI
            Gizmos.color = Color.cyan;
            Vector3 uiPosition = transform.position + Vector3.up * heightOffset;
            Gizmos.DrawWireSphere(uiPosition, 0.3f);
            Gizmos.DrawLine(transform.position, uiPosition);
        }
#endif
    }
}
