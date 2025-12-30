#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using WildernessSurvival.UI;

namespace WildernessSurvival.Editor
{
    /// <summary>
    /// Odin Editor Window per generare automaticamente il Pause Menu UI.
    /// Menu: Tools/Wilderness/Pause Menu Generator
    /// </summary>
    public class PauseMenuGenerator : OdinEditorWindow
    {
        // ============================================
        // MENU ITEM
        // ============================================

        [MenuItem("Tools/Wilderness/Pause Menu Generator")]
        private static void OpenWindow()
        {
            var window = GetWindow<PauseMenuGenerator>();
            window.titleContent = new GUIContent("Pause Menu Generator");
            window.Show();
        }

        // ============================================
        // CONFIGURATION FIELDS
        // ============================================

        [BoxGroup("References")]
        [InfoBox("Auto-finds Canvas if left empty. Drag your UI Canvas here if you have multiple.")]
        [SerializeField] private Canvas parentCanvas;

        [BoxGroup("References")]
        [Required]
        [InfoBox("Drag your Pixel/Retro font here (TMP_FontAsset).")]
        [SerializeField] private TMP_FontAsset mainFont;

        [BoxGroup("Styling")]
        [InfoBox("Overlay color for the pause menu background panel.")]
        [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 200f / 255f);

        [BoxGroup("Styling")]
        [InfoBox("Container background color (dark brown/wood theme).")]
        [SerializeField] private Color containerColor = new Color(40f / 255f, 30f / 255f, 20f / 255f, 1f);

        [BoxGroup("Styling/Button Colors")]
        [HorizontalGroup("Styling/Button Colors/Row1")]
        [LabelWidth(100)]
        [SerializeField] private Color buttonNormalColor = new Color(240f / 255f, 230f / 255f, 210f / 255f, 1f); // Cream

        [HorizontalGroup("Styling/Button Colors/Row1")]
        [LabelWidth(100)]
        [SerializeField] private Color buttonHighlightedColor = new Color(255f / 255f, 245f / 255f, 220f / 255f, 1f); // Light yellow

        [HorizontalGroup("Styling/Button Colors/Row2")]
        [LabelWidth(100)]
        [SerializeField] private Color buttonPressedColor = new Color(220f / 255f, 210f / 255f, 190f / 255f, 1f); // Darker cream

        [HorizontalGroup("Styling/Button Colors/Row2")]
        [LabelWidth(100)]
        [SerializeField] private Color buttonTextColor = new Color(60f / 255f, 40f / 255f, 20f / 255f, 1f); // Dark brown

        [BoxGroup("Styling")]
        [SerializeField] private int buttonFontSize = 36;

        [BoxGroup("Styling")]
        [SerializeField] private int titleFontSize = 72;

        [BoxGroup("Styling")]
        [SerializeField] private int feedbackFontSize = 24;

        [BoxGroup("Options")]
        [InfoBox("If enabled, destroys existing Pause Menu objects before generating new ones.")]
        [SerializeField] private bool replaceExisting = true;

        // ============================================
        // GENERATION
        // ============================================

        [TitleGroup("Actions")]
        [Button("Generate Pause Menu", ButtonSizes.Large)]
        [GUIColor(0.4f, 1f, 0.4f)]
        private void GeneratePauseMenu()
        {
            // Validate
            if (mainFont == null)
            {
                EditorUtility.DisplayDialog("Error", "Main Font is required! Drag a TMP_FontAsset into the 'Main Font' field.", "OK");
                return;
            }

            // Auto-find Canvas if null
            if (parentCanvas == null)
            {
                parentCanvas = FindFirstObjectByType<Canvas>();
                if (parentCanvas == null)
                {
                    EditorUtility.DisplayDialog("Error", "No Canvas found in scene! Create a Canvas first.", "OK");
                    return;
                }
            }

            // Clean up existing (idempotent)
            if (replaceExisting)
            {
                CleanupExisting();
            }

            // Create UI hierarchy
            CreatePauseMenu();

            Debug.Log("<color=green>[PauseMenuGenerator]</color> ✅ Pause Menu generated successfully!");
        }

        // ============================================
        // CLEANUP (IDEMPOTENT)
        // ============================================

        private void CleanupExisting()
        {
            // Find and destroy existing objects
            Transform existingPanel = parentCanvas.transform.Find("PauseMenuPanel");
            if (existingPanel != null)
            {
                Undo.DestroyObjectImmediate(existingPanel.gameObject);
                Debug.Log("<color=yellow>[PauseMenuGenerator]</color> Destroyed existing PauseMenuPanel");
            }

            // Find existing PauseMenuManager
            PauseMenuUI existingManager = FindFirstObjectByType<PauseMenuUI>();
            if (existingManager != null)
            {
                Undo.DestroyObjectImmediate(existingManager.gameObject);
                Debug.Log("<color=yellow>[PauseMenuGenerator]</color> Destroyed existing PauseMenuManager");
            }
        }

        // ============================================
        // UI HIERARCHY CREATION
        // ============================================

        private void CreatePauseMenu()
        {
            // ============================
            // 1. CREATE PAUSE MENU PANEL
            // ============================
            GameObject panelObj = new GameObject("PauseMenuPanel");
            Undo.RegisterCreatedObjectUndo(panelObj, "Create PauseMenuPanel");
            panelObj.transform.SetParent(parentCanvas.transform, false);

            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            SetStretchAnchors(panelRect); // Full screen stretch
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = overlayColor;

            // ============================
            // 2. CREATE MENU CONTAINER
            // ============================
            GameObject containerObj = new GameObject("MenuContainer");
            Undo.RegisterCreatedObjectUndo(containerObj, "Create MenuContainer");
            containerObj.transform.SetParent(panelObj.transform, false);

            RectTransform containerRect = containerObj.AddComponent<RectTransform>();
            SetCenterAnchors(containerRect);
            containerRect.sizeDelta = new Vector2(500, 600);
            containerRect.anchoredPosition = Vector2.zero;

            Image containerImage = containerObj.AddComponent<Image>();
            containerImage.color = containerColor;

            // ============================
            // 3. CREATE TITLE TEXT
            // ============================
            GameObject titleObj = CreateText("TitleText", containerObj.transform, "PAUSA", titleFontSize, TextAlignmentOptions.Center, Color.white);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            SetCenterAnchors(titleRect);
            titleRect.sizeDelta = new Vector2(450, 100);
            titleRect.anchoredPosition = new Vector2(0, 200);

            // ============================
            // 4. CREATE BUTTONS
            // ============================
            GameObject resumeBtn = CreateButton("ResumeButton", containerObj.transform, "Riprendi", new Vector2(0, 100));
            GameObject saveBtn = CreateButton("SaveButton", containerObj.transform, "Salva Gioco", new Vector2(0, 20));
            GameObject loadBtn = CreateButton("LoadButton", containerObj.transform, "Carica Gioco", new Vector2(0, -60));
            GameObject quitBtn = CreateButton("QuitButton", containerObj.transform, "Esci", new Vector2(0, -140));

            // ============================
            // 5. CREATE FEEDBACK TEXT
            // ============================
            GameObject feedbackObj = CreateText("FeedbackText", containerObj.transform, "", feedbackFontSize, TextAlignmentOptions.Center, Color.green);
            RectTransform feedbackRect = feedbackObj.GetComponent<RectTransform>();
            SetCenterAnchors(feedbackRect);
            feedbackRect.sizeDelta = new Vector2(450, 50);
            feedbackRect.anchoredPosition = new Vector2(0, -200);
            feedbackObj.SetActive(false); // Initially disabled

            // ============================
            // 6. CREATE PAUSE MENU MANAGER
            // ============================
            GameObject managerObj = new GameObject("PauseMenuManager");
            Undo.RegisterCreatedObjectUndo(managerObj, "Create PauseMenuManager");

            PauseMenuUI pauseMenuUI = managerObj.AddComponent<PauseMenuUI>();

            // ============================
            // 7. AUTO-WIRE REFERENCES
            // ============================
            AutoWireReferences(pauseMenuUI, panelObj, resumeBtn, saveBtn, loadBtn, quitBtn, titleObj, feedbackObj);

            // ============================
            // 8. MARK SCENE DIRTY
            // ============================
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            Debug.Log("<color=cyan>[PauseMenuGenerator]</color> UI Hierarchy created and references wired!");
        }

        // ============================================
        // HELPER: CREATE TEXT
        // ============================================

        private GameObject CreateText(string name, Transform parent, string text, int fontSize, TextAlignmentOptions alignment, Color color)
        {
            GameObject textObj = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(textObj, $"Create {name}");
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();

            tmp.text = text;
            tmp.font = mainFont;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;

            return textObj;
        }

        // ============================================
        // HELPER: CREATE BUTTON
        // ============================================

        private GameObject CreateButton(string name, Transform parent, string labelText, Vector2 position)
        {
            GameObject buttonObj = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(buttonObj, $"Create {name}");
            buttonObj.transform.SetParent(parent, false);

            // RectTransform
            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            SetCenterAnchors(buttonRect);
            buttonRect.sizeDelta = new Vector2(400, 60);
            buttonRect.anchoredPosition = position;

            // Image (background)
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = buttonNormalColor;

            // Button
            Button button = buttonObj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = buttonNormalColor;
            colors.highlightedColor = buttonHighlightedColor;
            colors.pressedColor = buttonPressedColor;
            colors.selectedColor = buttonHighlightedColor;
            button.colors = colors;

            // Button Text (child)
            GameObject textObj = new GameObject("Text");
            Undo.RegisterCreatedObjectUndo(textObj, $"Create {name} Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            SetStretchAnchors(textRect);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = labelText;
            tmp.font = mainFont;
            tmp.fontSize = buttonFontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = buttonTextColor;

            return buttonObj;
        }

        // ============================================
        // HELPER: AUTO-WIRE REFERENCES
        // ============================================

        private void AutoWireReferences(
            PauseMenuUI pauseMenuUI,
            GameObject panelObj,
            GameObject resumeBtn,
            GameObject saveBtn,
            GameObject loadBtn,
            GameObject quitBtn,
            GameObject titleObj,
            GameObject feedbackObj)
        {
            SerializedObject so = new SerializedObject(pauseMenuUI);

            // Wire panel
            SerializedProperty pauseMenuPanelProp = so.FindProperty("pauseMenuPanel");
            pauseMenuPanelProp.objectReferenceValue = panelObj;

            // Wire buttons
            SerializedProperty resumeButtonProp = so.FindProperty("resumeButton");
            resumeButtonProp.objectReferenceValue = resumeBtn.GetComponent<Button>();

            SerializedProperty saveButtonProp = so.FindProperty("saveButton");
            saveButtonProp.objectReferenceValue = saveBtn.GetComponent<Button>();

            SerializedProperty loadButtonProp = so.FindProperty("loadButton");
            loadButtonProp.objectReferenceValue = loadBtn.GetComponent<Button>();

            SerializedProperty quitButtonProp = so.FindProperty("quitButton");
            quitButtonProp.objectReferenceValue = quitBtn.GetComponent<Button>();

            // Wire texts
            SerializedProperty titleTextProp = so.FindProperty("titleText");
            titleTextProp.objectReferenceValue = titleObj.GetComponent<TextMeshProUGUI>();

            SerializedProperty feedbackTextProp = so.FindProperty("feedbackText");
            feedbackTextProp.objectReferenceValue = feedbackObj.GetComponent<TextMeshProUGUI>();

            // Apply changes
            so.ApplyModifiedProperties();

            Debug.Log("<color=green>[PauseMenuGenerator]</color> All references auto-wired to PauseMenuUI!");
        }

        // ============================================
        // HELPER: SET ANCHORS
        // ============================================

        private void SetStretchAnchors(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private void SetCenterAnchors(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        // ============================================
        // HUD PAUSE BUTTON GENERATION
        // ============================================

        [TitleGroup("HUD Pause Button")]
        [Button("Generate HUD Pause Button", ButtonSizes.Large)]
        [GUIColor(0.4f, 1f, 1f)]
        [InfoBox("Creates a permanent Pause button in the bottom-left HUD (for mobile). " +
                 "Positioned to the right of the Build button. Auto-wires to PauseMenuUI.TogglePause().",
                 InfoMessageType.Info)]
        private void GenerateHUDPauseButton()
        {
            // Validate
            if (mainFont == null)
            {
                EditorUtility.DisplayDialog("Error", "Main Font is required! Drag a TMP_FontAsset into the 'Main Font' field.", "OK");
                return;
            }

            // Auto-find Canvas if null
            if (parentCanvas == null)
            {
                parentCanvas = FindFirstObjectByType<Canvas>();
                if (parentCanvas == null)
                {
                    EditorUtility.DisplayDialog("Error", "No Canvas found in scene! Create a Canvas first.", "OK");
                    return;
                }
            }

            // Find or create BottomLeft_Anchor
            Transform bottomLeftAnchor = FindOrCreateBottomLeftAnchor();
            if (bottomLeftAnchor == null)
            {
                Debug.LogError("[PauseMenuGenerator] Failed to create BottomLeft_Anchor!");
                return;
            }

            // Clean up existing pause button
            Transform existingPauseBtn = bottomLeftAnchor.Find("Btn_Pause");
            if (existingPauseBtn != null)
            {
                if (replaceExisting)
                {
                    Undo.DestroyObjectImmediate(existingPauseBtn.gameObject);
                    Debug.Log("<color=yellow>[PauseMenuGenerator]</color> Destroyed existing Btn_Pause");
                }
                else
                {
                    Debug.LogWarning("[PauseMenuGenerator] Btn_Pause already exists! Enable 'Replace Existing' to regenerate.");
                    return;
                }
            }

            // Create the Pause Button
            GameObject pauseButton = CreateHUDPauseButton(bottomLeftAnchor);

            // Wire to PauseMenuUI
            WirePauseButtonToPauseMenuUI(pauseButton);

            // Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            Debug.Log("<color=green>[PauseMenuGenerator]</color> ✅ HUD Pause Button generated successfully!");
            EditorUtility.DisplayDialog("HUD Pause Button Created",
                "Pause button has been created:\n\n" +
                "• Location: BottomLeft_Anchor/Btn_Pause\n" +
                "• Position: To the right of Build button\n" +
                "• Size: 100x100 pixels\n" +
                "• Icon: || (Pause symbol)\n\n" +
                "The button is wired to PauseMenuUI.TogglePause().\n\n" +
                "Save the scene to keep changes.",
                "OK");
        }

        private Transform FindOrCreateBottomLeftAnchor()
        {
            Transform anchor = parentCanvas.transform.Find("BottomLeft_Anchor");
            if (anchor != null)
            {
                return anchor;
            }

            // Create new anchor (matches BottomHUDSetup.cs pattern)
            GameObject anchorObj = new GameObject("BottomLeft_Anchor");
            Undo.RegisterCreatedObjectUndo(anchorObj, "Create BottomLeft_Anchor");
            anchorObj.transform.SetParent(parentCanvas.transform, false);

            RectTransform anchorRect = anchorObj.AddComponent<RectTransform>();
            anchorRect.anchorMin = new Vector2(0, 0);
            anchorRect.anchorMax = new Vector2(0, 0);
            anchorRect.pivot = new Vector2(0, 0);
            anchorRect.anchoredPosition = new Vector2(30, 30); // 30px padding
            anchorRect.sizeDelta = new Vector2(150, 150);

            Debug.Log("<color=cyan>[PauseMenuGenerator]</color> Created BottomLeft_Anchor");
            return anchorObj.transform;
        }

        private GameObject CreateHUDPauseButton(Transform parent)
        {
            // ============================
            // 1. CREATE BUTTON GAMEOBJECT
            // ============================
            GameObject buttonObj = new GameObject("Btn_Pause");
            Undo.RegisterCreatedObjectUndo(buttonObj, "Create Btn_Pause");
            buttonObj.transform.SetParent(parent, false);

            // ============================
            // 2. SETUP RECTTRANSFORM
            // Position to the right of Build button (Build is at 0,0 with 120x120 size)
            // Place Pause button at X: 150 (120 + 30 spacing)
            // ============================
            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0, 0);
            buttonRect.anchorMax = new Vector2(0, 0);
            buttonRect.pivot = new Vector2(0, 0);
            buttonRect.anchoredPosition = new Vector2(150, 0); // To the right of Build button
            buttonRect.sizeDelta = new Vector2(100, 100); // Slightly smaller than Build (120x120)

            // ============================
            // 3. ADD IMAGE (BACKGROUND)
            // ============================
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = buttonNormalColor; // Cream color

            // Optional: Add rounded corners effect (if you have a sprite)
            // buttonImage.sprite = yourRoundedSquareSprite;
            // buttonImage.type = Image.Type.Sliced;

            // ============================
            // 4. ADD BUTTON COMPONENT
            // ============================
            Button button = buttonObj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = buttonNormalColor;
            colors.highlightedColor = buttonHighlightedColor;
            colors.pressedColor = buttonPressedColor;
            colors.selectedColor = buttonHighlightedColor;
            button.colors = colors;

            // ============================
            // 5. ADD OUTLINE (WOOD BORDER)
            // ============================
            Outline outline = buttonObj.AddComponent<Outline>();
            outline.effectColor = new Color(60f / 255f, 40f / 255f, 20f / 255f, 1f); // Dark brown
            outline.effectDistance = new Vector2(3, -3);

            // ============================
            // 6. CREATE PAUSE ICON (||)
            // ============================
            GameObject iconObj = new GameObject("Icon");
            Undo.RegisterCreatedObjectUndo(iconObj, "Create Pause Icon");
            iconObj.transform.SetParent(buttonObj.transform, false);

            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            SetStretchAnchors(iconRect);
            iconRect.offsetMin = new Vector2(10, 10); // Small padding
            iconRect.offsetMax = new Vector2(-10, -10);

            TextMeshProUGUI iconText = iconObj.AddComponent<TextMeshProUGUI>();
            iconText.text = "||"; // Pause symbol
            iconText.font = mainFont;
            iconText.fontSize = 72; // Large pause symbol
            iconText.fontStyle = FontStyles.Bold;
            iconText.alignment = TextAlignmentOptions.Center;
            iconText.color = buttonTextColor; // Dark brown

            Debug.Log("<color=cyan>[PauseMenuGenerator]</color> HUD Pause Button created");
            return buttonObj;
        }

        private void WirePauseButtonToPauseMenuUI(GameObject buttonObj)
        {
            Button button = buttonObj.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError("[PauseMenuGenerator] Button component not found!");
                return;
            }

            // Find PauseMenuUI in scene
            PauseMenuUI pauseMenuUI = FindFirstObjectByType<PauseMenuUI>();

            if (pauseMenuUI != null)
            {
                // Clear existing listeners
                button.onClick.RemoveAllListeners();

                // Use UnityEvent persistent listener for serialization (survives play mode)
                UnityEditor.Events.UnityEventTools.RemovePersistentListener(button.onClick, 0);
                UnityEditor.Events.UnityEventTools.AddPersistentListener(
                    button.onClick,
                    pauseMenuUI.TogglePause
                );

                EditorUtility.SetDirty(button);
                Debug.Log("<color=green>[PauseMenuGenerator]</color> ✅ Pause button wired to PauseMenuUI.TogglePause()");
            }
            else
            {
                Debug.LogWarning("<color=yellow>[PauseMenuGenerator]</color> ⚠️ PauseMenuUI not found in scene!\n" +
                    "The button has been created but not wired.\n" +
                    "Please generate the Pause Menu first, then regenerate the HUD button, or wire manually:\n" +
                    "  OnClick() → PauseMenuUI.TogglePause()");
            }
        }

        // ============================================
        // HELPER: TEST PAUSE MENU
        // ============================================

        [TitleGroup("Actions")]
        [Button("Test: Find Canvas", ButtonSizes.Medium)]
        [GUIColor(0.4f, 0.8f, 1f)]
        private void TestFindCanvas()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Debug.Log($"<color=cyan>[PauseMenuGenerator]</color> Found Canvas: {canvas.gameObject.name}");
                parentCanvas = canvas;
            }
            else
            {
                Debug.LogWarning("<color=yellow>[PauseMenuGenerator]</color> No Canvas found in scene!");
            }
        }

        [Button("Test: Cleanup Existing", ButtonSizes.Medium)]
        [GUIColor(1f, 0.6f, 0.4f)]
        private void TestCleanup()
        {
            if (parentCanvas == null)
            {
                parentCanvas = FindFirstObjectByType<Canvas>();
            }

            if (parentCanvas != null)
            {
                CleanupExisting();
                Debug.Log("<color=yellow>[PauseMenuGenerator]</color> Cleanup complete!");
            }
            else
            {
                Debug.LogWarning("<color=yellow>[PauseMenuGenerator]</color> No Canvas found!");
            }
        }
    }
}
#endif
