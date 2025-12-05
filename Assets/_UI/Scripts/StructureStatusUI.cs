using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using WildernessSurvival.Gameplay.Structures;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WildernessSurvival.UI
{
    /// <summary>
    /// Barra di progresso in stile RTS che fluttua sopra la struttura.
    /// Mostra il progresso di costruzione con effetto billboard.
    /// 
    /// SETUP: Aggiungi questo componente alla Structure, poi clicca
    /// "🔥 Create & Setup UI Hierarchy" per creare automaticamente la UI.
    /// </summary>
    public class StructureStatusUI : MonoBehaviour
    {
        // ============================================
        // REFERENCES
        // ============================================

        [TitleGroup("References")]
        [SerializeField]
        [Tooltip("Riferimento al StructureController (trovato automaticamente)")]
        private StructureController structure;

        [SerializeField]
        [Tooltip("GameObject contenitore della UI (Canvas)")]
        private GameObject statusUIContainer;

        [SerializeField]
        [Tooltip("Image di sfondo della barra (nera)")]
        private Image background;

        [SerializeField]
        [Tooltip("Image di riempimento della barra (verde)")]
        private Image fill;

        [SerializeField]
        [Tooltip("Testo percentuale al centro della barra")]
        private TextMeshProUGUI percentageText;

        // ============================================
        // SETTINGS
        // ============================================

        [TitleGroup("Settings")]
        [SerializeField]
        [Tooltip("Offset verticale sopra la struttura")]
        [PropertyRange(0.5f, 5f)]
        private float heightOffset = 1.5f;

        [SerializeField]
        [Tooltip("Larghezza della barra (in unità UI)")]
        [PropertyRange(50f, 300f)]
        private float barWidth = 150f;

        [SerializeField]
        [Tooltip("Altezza della barra (in unità UI)")]
        [PropertyRange(5f, 50f)]
        private float barHeight = 15f;

        [SerializeField]
        [Tooltip("Colore del background")]
        private Color backgroundColor = new Color(0f, 0f, 0f, 0.8f);

        [SerializeField]
        [Tooltip("Colore del fill (Verde RTS)")]
        private Color fillColor = new Color(0.3f, 0.69f, 0.31f, 1f); // #4CAF50

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
            // ═══════════════════════════════════════════════════════════
            // CRITICO: Disattiva la UI immediatamente per evitare che appaia
            // durante il piazzamento (preview mode)
            // ═══════════════════════════════════════════════════════════
            if (statusUIContainer != null)
            {
                statusUIContainer.SetActive(false);
            }
            
            // Resetta il fill a 0 (barra vuota)
            if (fill != null)
            {
                fill.fillAmount = 0f;
            }

            // Trova StructureController
            if (structure == null)
            {
                structure = GetComponent<StructureController>();
                if (structure == null)
                {
                    structure = GetComponentInParent<StructureController>();
                }
            }

            if (structure == null)
            {
                Debug.LogError($"[StructureStatusUI] StructureController non trovato!");
                enabled = false;
                return;
            }

            // Cache camera
            mainCamera = Camera.main;

            isInitialized = true;
        }

        private void Start()
        {
            // Inizializza la barra a 0 (vuota)
            if (fill != null)
            {
                fill.fillAmount = 0f;
            }
            
            UpdateVisibility();
        }

        private void OnEnable()
        {
            // Quando il componente viene riabilitato (da StructureController.Start),
            // reinizializza se necessario
            if (!isInitialized)
            {
                // Trova StructureController
                if (structure == null)
                {
                    structure = GetComponent<StructureController>();
                    if (structure == null)
                    {
                        structure = GetComponentInParent<StructureController>();
                    }
                }

                // Cache camera
                if (mainCamera == null)
                {
                    mainCamera = Camera.main;
                }

                isInitialized = structure != null;
            }

            // Forza update della visibilità
            if (isInitialized)
            {
                UpdateVisibility();
            }
        }

        private void LateUpdate()
        {
            if (!isInitialized) return;

            // Aggiorna camera reference se necessario
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null) return;
            }

            // Aggiorna visibilità
            UpdateVisibility();

            // Se visibile, aggiorna posizione e contenuto
            if (statusUIContainer != null && statusUIContainer.activeSelf)
            {
                UpdatePosition();
                UpdateBillboard();
                UpdateProgressBar();
            }
        }

        // ============================================
        // VISIBILITY
        // ============================================

        private void UpdateVisibility()
        {
            if (structure == null || statusUIContainer == null) return;

            // Visibile SOLO durante Building E progress < 100%
            bool shouldShow = structure.State == StructureState.Building && 
                              structure.BuildProgress < 1.0f;

            if (statusUIContainer.activeSelf != shouldShow)
            {
                statusUIContainer.SetActive(shouldShow);
            }
        }

        // ============================================
        // POSITION & BILLBOARD
        // ============================================

        private void UpdatePosition()
        {
            if (structure == null || statusUIContainer == null) return;

            // Posiziona sopra la struttura
            statusUIContainer.transform.position = structure.transform.position + Vector3.up * heightOffset;
        }

        private void UpdateBillboard()
        {
            if (mainCamera == null || statusUIContainer == null) return;

            // Billboard: guarda sempre la camera
            statusUIContainer.transform.LookAt(
                statusUIContainer.transform.position + mainCamera.transform.rotation * Vector3.forward,
                mainCamera.transform.rotation * Vector3.up
            );
        }

        // ============================================
        // PROGRESS BAR
        // ============================================

        private void UpdateProgressBar()
        {
            if (fill == null || structure == null) return;

            float progress = structure.BuildProgress;
            fill.fillAmount = progress;

            // Aggiorna testo percentuale
            if (percentageText != null)
            {
                int percent = Mathf.RoundToInt(progress * 100f);
                percentageText.text = $"{percent}%";
            }
        }

        // ============================================
        // PUBLIC API
        // ============================================

        public void SetStructure(StructureController newStructure)
        {
            structure = newStructure;
        }

        public void ForceUpdate()
        {
            if (isInitialized)
            {
                UpdateVisibility();
                UpdatePosition();
                UpdateBillboard();
                UpdateProgressBar();
            }
        }

        // ============================================
        // EDITOR TOOLS
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("🛠 Editor Setup")]
        [Button("🔥 Create & Setup UI Hierarchy", ButtonSizes.Large), GUIColor(1f, 0.5f, 0f)]
        [InfoBox("Crea automaticamente la gerarchia UI completa (Canvas + Background + Fill).")]
        private void CreateAndSetupUIHierarchy()
        {
            // Registra undo
            Undo.RecordObject(this, "Create Status UI Hierarchy");

            // Trova o crea StructureController reference
            if (structure == null)
            {
                structure = GetComponent<StructureController>();
                if (structure == null)
                {
                    structure = GetComponentInParent<StructureController>();
                }
            }

            // ═══════════════════════════════════════════════════════════
            // 1. CREA/TROVA CONTAINER "StatusUI"
            // ═══════════════════════════════════════════════════════════
            Transform existingContainer = transform.Find("StatusUI");
            
            if (existingContainer != null)
            {
                statusUIContainer = existingContainer.gameObject;
                Debug.Log("[StructureStatusUI] Found existing StatusUI container.");
            }
            else
            {
                // Crea nuovo GameObject per la Canvas
                statusUIContainer = new GameObject("StatusUI");
                statusUIContainer.transform.SetParent(transform, false);
                statusUIContainer.transform.localPosition = Vector3.up * heightOffset;
                
                Undo.RegisterCreatedObjectUndo(statusUIContainer, "Create StatusUI Container");
                Debug.Log("[StructureStatusUI] Created StatusUI container.");
            }

            // ═══════════════════════════════════════════════════════════
            // 2. SETUP CANVAS
            // ═══════════════════════════════════════════════════════════
            Canvas canvas = statusUIContainer.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = statusUIContainer.AddComponent<Canvas>();
            }
            canvas.renderMode = RenderMode.WorldSpace;

            // Setup RectTransform
            RectTransform canvasRect = statusUIContainer.GetComponent<RectTransform>();
            canvasRect.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            canvasRect.sizeDelta = new Vector2(barWidth, barHeight);
            canvasRect.localPosition = Vector3.up * heightOffset;

            EditorUtility.SetDirty(canvas);
            Debug.Log("[StructureStatusUI] Canvas configured.");

            // ═══════════════════════════════════════════════════════════
            // 3. CREA/SETUP BACKGROUND
            // ═══════════════════════════════════════════════════════════
            Transform bgTransform = statusUIContainer.transform.Find("Background");
            GameObject bgObj;
            
            if (bgTransform != null)
            {
                bgObj = bgTransform.gameObject;
            }
            else
            {
                bgObj = new GameObject("Background");
                bgObj.transform.SetParent(statusUIContainer.transform, false);
                Undo.RegisterCreatedObjectUndo(bgObj, "Create Background");
            }

            // Setup Background Image
            background = bgObj.GetComponent<Image>();
            if (background == null)
            {
                background = bgObj.AddComponent<Image>();
            }
            background.color = backgroundColor;
            background.raycastTarget = false;

            // Setup Background RectTransform
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.5f, 0.5f);
            bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            bgRect.sizeDelta = new Vector2(barWidth, barHeight);
            bgRect.anchoredPosition = Vector2.zero;

            EditorUtility.SetDirty(background);
            Debug.Log("[StructureStatusUI] Background configured.");

            // ═══════════════════════════════════════════════════════════
            // 4. CREA/SETUP FILL
            // ═══════════════════════════════════════════════════════════
            Transform fillTransform = bgObj.transform.Find("Fill");
            GameObject fillObj;
            
            if (fillTransform != null)
            {
                fillObj = fillTransform.gameObject;
            }
            else
            {
                fillObj = new GameObject("Fill");
                fillObj.transform.SetParent(bgObj.transform, false);
                Undo.RegisterCreatedObjectUndo(fillObj, "Create Fill");
            }

            // Setup Fill Image
            fill = fillObj.GetComponent<Image>();
            if (fill == null)
            {
                fill = fillObj.AddComponent<Image>();
            }
            
            // Carica lo sprite sfo1 dal progetto
            Sprite sfo1Sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sfo1.jpg");
            if (sfo1Sprite != null)
            {
                fill.sprite = sfo1Sprite;
            }
            else
            {
                // Fallback: usa UISprite se sfo1 non trovato
                Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                fill.sprite = uiSprite;
                Debug.LogWarning("[StructureStatusUI] sfo1.jpg non trovato, uso UISprite come fallback");
            }
            
            fill.color = fillColor;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0; // Left
            fill.fillAmount = 0f; // INIZIA VUOTO
            fill.raycastTarget = false;

            // Setup Fill RectTransform - Stretch COMPLETO senza padding
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero; // Nessun margine sinistro/basso
            fillRect.offsetMax = Vector2.zero; // Nessun margine destro/alto

            EditorUtility.SetDirty(fill);
            Debug.Log("[StructureStatusUI] Fill configured.");

            // ═══════════════════════════════════════════════════════════
            // 5. CREA/SETUP PERCENTAGE TEXT
            // ═══════════════════════════════════════════════════════════
            Transform textTransform = bgObj.transform.Find("PercentageText");
            GameObject textObj;
            
            if (textTransform != null)
            {
                textObj = textTransform.gameObject;
            }
            else
            {
                textObj = new GameObject("PercentageText");
                textObj.transform.SetParent(bgObj.transform, false);
                Undo.RegisterCreatedObjectUndo(textObj, "Create PercentageText");
            }

            // Setup TextMeshPro
            percentageText = textObj.GetComponent<TextMeshProUGUI>();
            if (percentageText == null)
            {
                percentageText = textObj.AddComponent<TextMeshProUGUI>();
            }
            percentageText.text = "0%";
            percentageText.fontSize = barHeight * 0.7f;
            percentageText.fontStyle = FontStyles.Bold;
            percentageText.color = Color.white;
            percentageText.alignment = TextAlignmentOptions.Center;
            percentageText.raycastTarget = false;
            
            // Aggiungi outline per visibilità
            percentageText.outlineWidth = 0.2f;
            percentageText.outlineColor = Color.black;

            // Setup Text RectTransform - Stretch per riempire il parent
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            EditorUtility.SetDirty(percentageText);
            Debug.Log("[StructureStatusUI] Percentage text configured.");

            // ═══════════════════════════════════════════════════════════
            // 6. DISATTIVA UI (non deve apparire durante il piazzamento!)
            // ═══════════════════════════════════════════════════════════
            statusUIContainer.SetActive(false);

            // ═══════════════════════════════════════════════════════════
            // 7. SALVA RIFERIMENTI
            // ═══════════════════════════════════════════════════════════
            EditorUtility.SetDirty(this);

            Debug.Log("<color=green>[StructureStatusUI] ✅ UI Hierarchy created! (Disabled by default - will show only during building)</color>");
            
            // Forza repaint
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        [TitleGroup("🛠 Editor Setup")]
        [Button("🎨 Update Colors & Sizes", ButtonSizes.Medium), GUIColor(0.3f, 0.8f, 0.3f)]
        private void UpdateColorsAndSizes()
        {
            if (background != null)
            {
                background.color = backgroundColor;
                RectTransform bgRect = background.GetComponent<RectTransform>();
                if (bgRect != null)
                {
                    bgRect.sizeDelta = new Vector2(barWidth, barHeight);
                }
                EditorUtility.SetDirty(background);
            }

            if (fill != null)
            {
                // Assegna sfo1 se mancante
                if (fill.sprite == null)
                {
                    Sprite sfo1Sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sfo1.jpg");
                    if (sfo1Sprite != null)
                    {
                        fill.sprite = sfo1Sprite;
                    }
                    else
                    {
                        Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                        fill.sprite = uiSprite;
                    }
                }
                
                fill.color = fillColor;
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.fillOrigin = 0;
                
                // Fix RectTransform - nessun padding
                RectTransform fillRect = fill.GetComponent<RectTransform>();
                if (fillRect != null)
                {
                    fillRect.offsetMin = Vector2.zero;
                    fillRect.offsetMax = Vector2.zero;
                }
                
                EditorUtility.SetDirty(fill);
            }

            if (statusUIContainer != null)
            {
                RectTransform canvasRect = statusUIContainer.GetComponent<RectTransform>();
                if (canvasRect != null)
                {
                    canvasRect.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                    canvasRect.localPosition = Vector3.up * heightOffset;
                }
            }

            Debug.Log("<color=cyan>[StructureStatusUI] Colors and sizes updated!</color>");
        }

        [TitleGroup("🛠 Editor Setup")]
        [Button("🗑 Remove UI Hierarchy", ButtonSizes.Medium), GUIColor(1f, 0.3f, 0.3f)]
        private void RemoveUIHierarchy()
        {
            Transform existingContainer = transform.Find("StatusUI");
            if (existingContainer != null)
            {
                Undo.DestroyObjectImmediate(existingContainer.gameObject);
                statusUIContainer = null;
                background = null;
                fill = null;
                EditorUtility.SetDirty(this);
                Debug.Log("[StructureStatusUI] UI Hierarchy removed.");
            }
            else
            {
                Debug.LogWarning("[StructureStatusUI] No StatusUI container found to remove.");
            }
        }

        [TitleGroup("Debug")]
        [Button("👁 Toggle Visibility", ButtonSizes.Medium)]
        private void DebugToggle()
        {
            if (statusUIContainer != null)
            {
                statusUIContainer.SetActive(!statusUIContainer.activeSelf);
            }
        }

        [Button("📊 Test 50%", ButtonSizes.Medium)]
        private void DebugTest50()
        {
            if (fill != null) fill.fillAmount = 0.5f;
            if (statusUIContainer != null) statusUIContainer.SetActive(true);
        }

        [Button("📊 Test 100%", ButtonSizes.Medium)]
        private void DebugTest100()
        {
            if (fill != null) fill.fillAmount = 1.0f;
            if (statusUIContainer != null) statusUIContainer.SetActive(true);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 pos = transform.position;
            
            Gizmos.color = Color.cyan;
            Vector3 uiPos = pos + Vector3.up * heightOffset;
            
            float worldWidth = barWidth * 0.01f;
            float worldHeight = barHeight * 0.01f;
            Gizmos.DrawWireCube(uiPos, new Vector3(worldWidth, worldHeight, 0.01f));
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pos, uiPos);
        }

        [TitleGroup("Status")]
        [ShowInInspector, ReadOnly]
        private bool HasUISetup => statusUIContainer != null && background != null && fill != null;

        [ShowInInspector, ReadOnly]
        private bool HasStructureRef => structure != null;
#endif
    }
}
