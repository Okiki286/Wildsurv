using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Linq;
using WildernessSurvival.UI;

namespace WildernessSurvival.Editor
{
    /// <summary>
    /// Editor Tool per automatizzare il setup completo del BuildModeUI.
    ///
    /// USAGE:
    /// 1. Menu: Tools → Wilderness Survival → Setup Build Mode UI
    /// 2. Click "Auto Setup Complete System" nel window
    /// 3. Done! Il sistema è configurato e pronto all'uso.
    ///
    /// FEATURES:
    /// - Crea automaticamente Canvas se mancante
    /// - Crea Button UI con struttura corretta
    /// - Trova e assegna sprite X-Delete-Close-Error dal Modular Game UI Kit
    /// - Configura colori, layout, anchors per mobile
    /// - Collega tutti i riferimenti automaticamente
    /// - Configura evento onClick
    /// </summary>
    public class BuildModeUISetupTool : EditorWindow
    {
        // ============================================
        // CONSTANTS & DEFAULTS
        // ============================================

        private const string SPRITE_SEARCH_FILTER = "X-Delete-Close-Error";
        private const string SPRITE_PATH_HINT = "Assets/ModularGameUIKit/Common/Sprites/Icon-Symbols/Basics/X-Delete-Close-Error.png";
        private static readonly Color DEFAULT_CANCEL_COLOR = new Color(1f, 0.267f, 0.267f, 1f); // #FF4444

        // Button sizing - UPDATED: Half size (40x40) per richiesta utente
        private const float BUTTON_SIZE = 40f;
        private const float BUTTON_MARGIN = 20f;

        // ============================================
        // WINDOW STATE
        // ============================================

        private Canvas targetCanvas;
        private Sprite cancelSprite;
        private Color cancelColor = DEFAULT_CANCEL_COLOR;
        private bool autoFindSprite = true;
        private bool setupComplete = false;
        private string statusMessage = "";

        // Layout options - UPDATED: Default TopCenter per richiesta utente
        private ButtonPosition buttonPosition = ButtonPosition.TopCenter;
        private bool hideWhenInactive = true;
        private bool debugMode = false;

        private enum ButtonPosition
        {
            TopLeft,
            TopCenter,
            TopRight,
            BottomLeft,
            BottomRight
        }

        // ============================================
        // MENU ITEM
        // ============================================

        [MenuItem("Tools/Wilderness Survival/Setup Build Mode UI", priority = 100)]
        public static void ShowWindow()
        {
            BuildModeUISetupTool window = GetWindow<BuildModeUISetupTool>("Build Mode UI Setup");
            window.minSize = new Vector2(400, 550);
            window.Show();
        }

        // ============================================
        // GUI
        // ============================================

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space(10);

            DrawCanvasSection();
            EditorGUILayout.Space(10);

            DrawSpriteSection();
            EditorGUILayout.Space(10);

            DrawLayoutSection();
            EditorGUILayout.Space(10);

            DrawOptionsSection();
            EditorGUILayout.Space(10);

            DrawActionButtons();
            EditorGUILayout.Space(10);

            DrawStatusSection();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Build Mode UI - Automated Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Questo tool crea automaticamente il pulsante 'Annulla Costruzione' con tutti i riferimenti configurati.\n\n" +
                "✓ Crea Button UI con layout mobile-friendly\n" +
                "✓ Assegna sprite dal Modular Game UI Kit\n" +
                "✓ Configura colori, eventi e riferimenti\n" +
                "✓ One-click setup completo!",
                MessageType.Info
            );
            EditorGUILayout.EndVertical();
        }

        private void DrawCanvasSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("1. Target Canvas", EditorStyles.boldLabel);

            targetCanvas = (Canvas)EditorGUILayout.ObjectField("Canvas", targetCanvas, typeof(Canvas), true);

            if (targetCanvas == null)
            {
                EditorGUILayout.HelpBox("Nessuna Canvas assegnata. Il tool ne creerà una automaticamente.", MessageType.Warning);

                if (GUILayout.Button("🔍 Find Canvas in Scene", GUILayout.Height(25)))
                {
                    targetCanvas = FindFirstObjectByType<Canvas>();
                    if (targetCanvas != null)
                    {
                        statusMessage = "✓ Canvas trovata: " + targetCanvas.name;
                    }
                    else
                    {
                        statusMessage = "⚠ Nessuna Canvas trovata. Verrà creata automaticamente.";
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("✓ Canvas assegnata: " + targetCanvas.name, MessageType.None);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSpriteSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("2. Sprite Icon (Modular Game UI Kit)", EditorStyles.boldLabel);

            autoFindSprite = EditorGUILayout.Toggle("Auto-Find Sprite", autoFindSprite);

            if (autoFindSprite)
            {
                EditorGUILayout.HelpBox(
                    $"Lo sprite verrà cercato automaticamente:\n'{SPRITE_SEARCH_FILTER}'",
                    MessageType.Info
                );

                if (GUILayout.Button("🔍 Search Sprite Now", GUILayout.Height(25)))
                {
                    cancelSprite = FindCancelSprite();
                    if (cancelSprite != null)
                    {
                        statusMessage = "✓ Sprite trovato: " + cancelSprite.name;
                    }
                    else
                    {
                        statusMessage = "❌ Sprite non trovato! Verifica il path:\n" + SPRITE_PATH_HINT;
                    }
                }
            }
            else
            {
                cancelSprite = (Sprite)EditorGUILayout.ObjectField("Cancel Sprite", cancelSprite, typeof(Sprite), false);
            }

            // Preview dello sprite
            if (cancelSprite != null)
            {
                EditorGUILayout.LabelField("✓ Sprite assegnato:", cancelSprite.name);
                Rect rect = GUILayoutUtility.GetRect(50, 50, GUILayout.ExpandWidth(false));
                EditorGUI.DrawPreviewTexture(rect, cancelSprite.texture);
            }
            else
            {
                EditorGUILayout.HelpBox("⚠ Sprite non assegnato", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawLayoutSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("3. Layout & Positioning", EditorStyles.boldLabel);

            buttonPosition = (ButtonPosition)EditorGUILayout.EnumPopup("Button Position", buttonPosition);
            cancelColor = EditorGUILayout.ColorField("Cancel Color (UI Kit Red)", cancelColor);

            EditorGUILayout.HelpBox(
                GetPositionDescription(),
                MessageType.Info
            );

            EditorGUILayout.EndVertical();
        }

        private void DrawOptionsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("4. Behavior Options", EditorStyles.boldLabel);

            hideWhenInactive = EditorGUILayout.Toggle("Hide When Inactive", hideWhenInactive);
            debugMode = EditorGUILayout.Toggle("Debug Mode", debugMode);

            EditorGUILayout.HelpBox(
                hideWhenInactive
                    ? "Il pulsante scompare quando non in build mode"
                    : "Il pulsante rimane visibile ma disabilitato",
                MessageType.None
            );

            EditorGUILayout.EndVertical();
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("5. Setup Actions", EditorStyles.boldLabel);

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("🚀 AUTO SETUP COMPLETE SYSTEM", GUILayout.Height(40)))
            {
                PerformCompleteSetup();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Create Button Only", GUILayout.Height(30)))
            {
                CreateButtonOnly();
            }

            if (GUILayout.Button("Find & Assign Sprite", GUILayout.Height(30)))
            {
                cancelSprite = FindCancelSprite();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawStatusSection()
        {
            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("Status", EditorStyles.boldLabel);

                MessageType msgType = statusMessage.StartsWith("✓") ? MessageType.Info :
                                     statusMessage.StartsWith("❌") ? MessageType.Error :
                                     MessageType.Warning;

                EditorGUILayout.HelpBox(statusMessage, msgType);
                EditorGUILayout.EndVertical();
            }

            if (setupComplete)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.backgroundColor = Color.green;
                EditorGUILayout.HelpBox(
                    "✅ SETUP COMPLETATO CON SUCCESSO!\n\n" +
                    "Il pulsante 'Cancel Build' è stato creato e configurato.\n" +
                    "Entra in Play Mode e seleziona una struttura per testare!",
                    MessageType.Info
                );
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndVertical();
            }
        }

        // ============================================
        // SETUP LOGIC
        // ============================================

        private void PerformCompleteSetup()
        {
            try
            {
                statusMessage = "⏳ Setup in corso...";
                Repaint();

                // Step 0: Clean up existing CancelBuildButton if found
                GameObject existingButton = GameObject.Find("CancelBuildButton");
                if (existingButton != null)
                {
                    if (EditorUtility.DisplayDialog(
                        "Button Esistente Trovato",
                        "Un pulsante 'CancelBuildButton' esiste già nella scena.\n\nVuoi eliminarlo e crearne uno nuovo?",
                        "Sì, Elimina e Ricrea",
                        "Annulla"))
                    {
                        Undo.DestroyObjectImmediate(existingButton);
                        statusMessage += "\n✓ Button esistente eliminato";
                    }
                    else
                    {
                        statusMessage = "❌ Setup annullato dall'utente.";
                        return;
                    }
                }

                // Step 1: Find or Create Canvas
                if (targetCanvas == null)
                {
                    targetCanvas = FindOrCreateCanvas();
                }

                // Step 2: Find Sprite
                if (autoFindSprite && cancelSprite == null)
                {
                    cancelSprite = FindCancelSprite();
                }

                if (cancelSprite == null)
                {
                    statusMessage = "❌ ERRORE: Sprite 'X-Delete-Close-Error' non trovato!\n\n" +
                                   $"Path atteso: {SPRITE_PATH_HINT}\n\n" +
                                   "Verifica che il Modular Game UI Kit sia importato correttamente.";
                    return;
                }

                // Step 3: Create Button Hierarchy
                GameObject buttonObj = CreateCancelButton(targetCanvas);

                // Step 4: Setup BuildModeUI Component
                BuildModeUI buildModeUI = SetupBuildModeUIComponent(buttonObj);

                // Step 5: Configure Button References
                ConfigureButtonReferences(buttonObj, buildModeUI);

                // Step 6: Configure Event
                ConfigureButtonEvent(buttonObj.GetComponent<Button>(), buildModeUI);

                // Success!
                setupComplete = true;
                statusMessage = $"✅ Setup completato con successo!\n\n" +
                               $"GameObject creato: {buttonObj.name}\n" +
                               $"Sprite assegnato: {cancelSprite.name}\n" +
                               $"Posizione: {buttonPosition}\n\n" +
                               $"Seleziona '{buttonObj.name}' nella Hierarchy per vedere la configurazione.";

                // Select the created object
                Selection.activeGameObject = buttonObj;
                EditorGUIUtility.PingObject(buttonObj);
            }
            catch (System.Exception e)
            {
                statusMessage = $"❌ ERRORE durante il setup:\n{e.Message}";
                Debug.LogError($"[BuildModeUISetupTool] Setup failed: {e}");
            }
        }

        private Canvas FindOrCreateCanvas()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();

            if (canvas == null)
            {
                // Create Canvas
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                // Add CanvasScaler for mobile support
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                // Add GraphicRaycaster
                canvasObj.AddComponent<GraphicRaycaster>();

                // Create EventSystem if missing
                if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                {
                    GameObject eventSystemObj = new GameObject("EventSystem");
                    eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }

                Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
                statusMessage += "\n✓ Canvas creata";
            }

            return canvas;
        }

        private Sprite FindCancelSprite()
        {
            // Search by exact path first
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_PATH_HINT);

            if (sprite != null)
            {
                return sprite;
            }

            // Fallback: Search all assets
            string[] guids = AssetDatabase.FindAssets($"{SPRITE_SEARCH_FILTER} t:Sprite");

            if (guids.Length > 0)
            {
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

                    if (sprite != null && sprite.name.Contains("Delete") && sprite.name.Contains("Close"))
                    {
                        Debug.Log($"[BuildModeUISetupTool] Found sprite at: {path}");
                        return sprite;
                    }
                }
            }

            Debug.LogWarning($"[BuildModeUISetupTool] Sprite '{SPRITE_SEARCH_FILTER}' not found. Expected path: {SPRITE_PATH_HINT}");
            return null;
        }

        private GameObject CreateCancelButton(Canvas canvas)
        {
            // Create Button GameObject
            GameObject buttonObj = new GameObject("CancelBuildButton");
            buttonObj.transform.SetParent(canvas.transform, false);

            // Add RectTransform
            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(BUTTON_SIZE, BUTTON_SIZE);

            // Position based on selection
            SetButtonAnchors(buttonRect);

            // ═══════════════════════════════════════════════════════════
            // CRITICAL FIX: Add CanvasGroup to block raycasts
            // Questo previene che il click sul button piazzi la struttura
            // ═══════════════════════════════════════════════════════════
            CanvasGroup canvasGroup = buttonObj.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = true; // Block clicks from passing through
            canvasGroup.interactable = true;   // Button still clickable

            // Add Button Component
            Button button = buttonObj.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;

            // Add background Image
            Image buttonBg = buttonObj.AddComponent<Image>();
            buttonBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Semi-transparent dark background

            // Create Icon child
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(buttonObj.transform, false);

            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(5, 5); // Reduced padding for smaller button
            iconRect.offsetMax = new Vector2(-5, -5);

            Image iconImage = iconObj.AddComponent<Image>();
            iconImage.sprite = cancelSprite;
            iconImage.color = cancelColor;
            iconImage.raycastTarget = false; // Don't block button clicks

            Undo.RegisterCreatedObjectUndo(buttonObj, "Create Cancel Build Button");

            return buttonObj;
        }

        private void CreateButtonOnly()
        {
            if (targetCanvas == null)
            {
                targetCanvas = FindOrCreateCanvas();
            }

            GameObject buttonObj = CreateCancelButton(targetCanvas);
            statusMessage = $"✓ Button creato: {buttonObj.name}\n⚠ Configura manualmente il BuildModeUI component.";
            Selection.activeGameObject = buttonObj;
        }

        private BuildModeUI SetupBuildModeUIComponent(GameObject buttonObj)
        {
            // Add BuildModeUI component to Canvas or create a manager object
            BuildModeUI buildModeUI = targetCanvas.GetComponent<BuildModeUI>();

            if (buildModeUI == null)
            {
                // Check if there's already a BuildModeUI_Manager
                GameObject manager = GameObject.Find("BuildModeUI_Manager");

                if (manager == null)
                {
                    manager = new GameObject("BuildModeUI_Manager");
                    manager.transform.SetParent(targetCanvas.transform, false);
                    Undo.RegisterCreatedObjectUndo(manager, "Create BuildModeUI Manager");
                }

                buildModeUI = manager.GetComponent<BuildModeUI>();
                if (buildModeUI == null)
                {
                    buildModeUI = manager.AddComponent<BuildModeUI>();
                    Undo.RegisterCreatedObjectUndo(buildModeUI, "Add BuildModeUI Component");
                }
            }

            return buildModeUI;
        }

        private void ConfigureButtonReferences(GameObject buttonObj, BuildModeUI buildModeUI)
        {
            Button button = buttonObj.GetComponent<Button>();
            Image iconImage = buttonObj.transform.Find("Icon").GetComponent<Image>();

            // Use SerializedObject to set private fields
            SerializedObject serializedUI = new SerializedObject(buildModeUI);

            serializedUI.FindProperty("cancelButton").objectReferenceValue = button;
            serializedUI.FindProperty("buttonIcon").objectReferenceValue = iconImage;
            serializedUI.FindProperty("cancelSprite").objectReferenceValue = cancelSprite;
            serializedUI.FindProperty("cancelColor").colorValue = cancelColor;
            serializedUI.FindProperty("hideWhenInactive").boolValue = hideWhenInactive;
            serializedUI.FindProperty("debugMode").boolValue = debugMode;

            serializedUI.ApplyModifiedProperties();

            EditorUtility.SetDirty(buildModeUI);
        }

        private void ConfigureButtonEvent(Button button, BuildModeUI buildModeUI)
        {
            // Remove all existing listeners first (safe way)
            int listenerCount = button.onClick.GetPersistentEventCount();
            for (int i = listenerCount - 1; i >= 0; i--)
            {
                UnityEditor.Events.UnityEventTools.RemovePersistentListener(button.onClick, i);
            }

            // Add persistent listener
            UnityEditor.Events.UnityEventTools.AddPersistentListener(
                button.onClick,
                buildModeUI.OnCancelClicked
            );

            EditorUtility.SetDirty(button);
        }

        private void SetButtonAnchors(RectTransform rect)
        {
            switch (buttonPosition)
            {
                case ButtonPosition.TopLeft:
                    rect.anchorMin = new Vector2(0, 1);
                    rect.anchorMax = new Vector2(0, 1);
                    rect.pivot = new Vector2(0, 1);
                    rect.anchoredPosition = new Vector2(BUTTON_MARGIN, -BUTTON_MARGIN);
                    break;

                case ButtonPosition.TopCenter:
                    rect.anchorMin = new Vector2(0.5f, 1);
                    rect.anchorMax = new Vector2(0.5f, 1);
                    rect.pivot = new Vector2(0.5f, 1);
                    rect.anchoredPosition = new Vector2(0, -BUTTON_MARGIN);
                    break;

                case ButtonPosition.TopRight:
                    rect.anchorMin = new Vector2(1, 1);
                    rect.anchorMax = new Vector2(1, 1);
                    rect.pivot = new Vector2(1, 1);
                    rect.anchoredPosition = new Vector2(-BUTTON_MARGIN, -BUTTON_MARGIN);
                    break;

                case ButtonPosition.BottomLeft:
                    rect.anchorMin = new Vector2(0, 0);
                    rect.anchorMax = new Vector2(0, 0);
                    rect.pivot = new Vector2(0, 0);
                    rect.anchoredPosition = new Vector2(BUTTON_MARGIN, BUTTON_MARGIN);
                    break;

                case ButtonPosition.BottomRight:
                    rect.anchorMin = new Vector2(1, 0);
                    rect.anchorMax = new Vector2(1, 0);
                    rect.pivot = new Vector2(1, 0);
                    rect.anchoredPosition = new Vector2(-BUTTON_MARGIN, BUTTON_MARGIN);
                    break;
            }
        }

        private string GetPositionDescription()
        {
            switch (buttonPosition)
            {
                case ButtonPosition.TopLeft:
                    return "📍 Angolo superiore sinistro";
                case ButtonPosition.TopCenter:
                    return "📍 Centro superiore (consigliato - facile accesso)";
                case ButtonPosition.TopRight:
                    return "📍 Angolo superiore destro";
                case ButtonPosition.BottomLeft:
                    return "📍 Angolo inferiore sinistro";
                case ButtonPosition.BottomRight:
                    return "📍 Angolo inferiore destro";
                default:
                    return "";
            }
        }
    }
}
