#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Collections.Generic;

namespace WildernessSurvival.Editor
{
    /// <summary>
    /// Odin Editor Window to create a unified visual container (Bottom Action Bar)
    /// that groups Build and Pause buttons in a styled panel.
    /// Menu: Tools/Wilderness/HUD Containerizer
    /// </summary>
    public class HUDContainerizer : OdinEditorWindow
    {
        // ============================================
        // MENU ITEM
        // ============================================

        [MenuItem("Tools/Wilderness/HUD Containerizer")]
        private static void OpenWindow()
        {
            var window = GetWindow<HUDContainerizer>();
            window.titleContent = new GUIContent("HUD Containerizer");
            window.minSize = new Vector2(450, 500);
            window.Show();
        }

        // ============================================
        // COLOR CONSTANTS
        // ============================================

        private static readonly Color CREAM_COLOR = new Color(240f / 255f, 230f / 255f, 210f / 255f, 1f); // #F0E6D2
        private static readonly Color DARK_BROWN_COLOR = new Color(60f / 255f, 40f / 255f, 20f / 255f, 1f); // #3C280D

        // ============================================
        // CONFIGURATION
        // ============================================

        [TitleGroup("Panel Settings")]
        [InfoBox("Configure the visual style and layout of the Bottom Action Bar.", InfoMessageType.Info)]

        [BoxGroup("Panel Settings/Identity")]
        [LabelWidth(150)]
        [SerializeField] private string panelName = "HUD_ActionPanel";

        [BoxGroup("Panel Settings/Position")]
        [LabelWidth(150)]
        [SerializeField] private Vector2 panelPosition = new Vector2(50, 50);

        [BoxGroup("Panel Settings/Colors")]
        [LabelWidth(150)]
        [SerializeField] private Color backgroundColor = new Color(240f / 255f, 230f / 255f, 210f / 255f, 1f);

        [BoxGroup("Panel Settings/Colors")]
        [LabelWidth(150)]
        [SerializeField] private Color borderColor = new Color(60f / 255f, 40f / 255f, 20f / 255f, 1f);

        [BoxGroup("Panel Settings/Colors")]
        [LabelWidth(150)]
        [Tooltip("Shadow/outline effect distance")]
        [SerializeField] private Vector2 borderOffset = new Vector2(3, -3);

        [BoxGroup("Panel Settings/Layout")]
        [LabelWidth(150)]
        [SerializeField] private int paddingLeft = 25;

        [BoxGroup("Panel Settings/Layout")]
        [LabelWidth(150)]
        [SerializeField] private int paddingRight = 25;

        [BoxGroup("Panel Settings/Layout")]
        [LabelWidth(150)]
        [SerializeField] private int paddingTop = 15;

        [BoxGroup("Panel Settings/Layout")]
        [LabelWidth(150)]
        [SerializeField] private int paddingBottom = 15;

        [BoxGroup("Panel Settings/Layout")]
        [LabelWidth(150)]
        [SerializeField] private float buttonSpacing = 25f;

        [BoxGroup("Panel Settings/Layout")]
        [LabelWidth(150)]
        [SerializeField] private TextAnchor childAlignment = TextAnchor.MiddleCenter;

        [BoxGroup("Panel Settings/Layout")]
        [LabelWidth(150)]
        [Tooltip("Control child button width")]
        [SerializeField] private bool childControlWidth = true;

        [BoxGroup("Panel Settings/Layout")]
        [LabelWidth(150)]
        [Tooltip("Control child button height")]
        [SerializeField] private bool childControlHeight = true;

        [BoxGroup("Panel Settings/Layout")]
        [LabelWidth(150)]
        [Tooltip("Use child button scale")]
        [SerializeField] private bool childScaleWidth = true;

        [BoxGroup("Panel Settings/Layout")]
        [LabelWidth(150)]
        [Tooltip("Use child button scale")]
        [SerializeField] private bool childScaleHeight = true;

        [BoxGroup("Panel Settings/Layout")]
        [LabelWidth(150)]
        [Tooltip("Force expand child width")]
        [SerializeField] private bool childForceExpandWidth = false;

        [BoxGroup("Panel Settings/Layout")]
        [LabelWidth(150)]
        [Tooltip("Force expand child height")]
        [SerializeField] private bool childForceExpandHeight = false;

        // ============================================
        // SPRITE CONFIGURATION
        // ============================================

        [TitleGroup("Sprite Settings")]
        [InfoBox("Optional: Assign a sprite for rounded corners. Leave empty for solid color.", InfoMessageType.None)]
        [LabelWidth(150)]
        [SerializeField] private Sprite panelSprite;

        [LabelWidth(150)]
        [SerializeField] private Image.Type spriteType = Image.Type.Sliced;

        // ============================================
        // BUTTON DETECTION
        // ============================================

        [TitleGroup("Button Detection")]
        [InfoBox("The tool will auto-detect these buttons. Override manually if needed.", InfoMessageType.None)]

        [ShowInInspector]
        [ReadOnly]
        [LabelText("Build Button")]
        private GameObject detectedBuildButton;

        [ShowInInspector]
        [ReadOnly]
        [LabelText("Pause Button")]
        private GameObject detectedPauseButton;

        [ShowInInspector]
        [ReadOnly]
        [LabelText("Canvas")]
        private Canvas detectedCanvas;

        // ============================================
        // MANUAL OVERRIDE
        // ============================================

        [TitleGroup("Manual Override")]
        [InfoBox("If auto-detection fails, manually assign buttons here.", InfoMessageType.Warning)]
        [SerializeField] private GameObject manualBuildButton;
        [SerializeField] private GameObject manualPauseButton;

        // ============================================
        // DEBUG
        // ============================================

        [TitleGroup("Debug Options")]
        [SerializeField] private bool verboseLogging = true;

        // ============================================
        // MAIN ACTIONS
        // ============================================

        [TitleGroup("Actions")]
        [Button("🔍 Detect Buttons", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.8f, 1f)]
        private void DetectButtons()
        {
            Log("=== Starting Button Detection ===", "cyan");

            // Find Canvas
            detectedCanvas = FindFirstObjectByType<Canvas>();
            if (detectedCanvas != null)
            {
                Log($"✅ Found Canvas: {detectedCanvas.gameObject.name}", "green");
            }
            else
            {
                LogError("❌ No Canvas found in scene!");
                return;
            }

            // Find Build Button
            detectedBuildButton = FindBuildButton();
            if (detectedBuildButton != null)
            {
                Log($"✅ Found Build Button: {detectedBuildButton.name}", "green");
            }
            else
            {
                LogWarning("⚠️ Build Button not found!");
            }

            // Find Pause Button
            detectedPauseButton = FindPauseButton();
            if (detectedPauseButton != null)
            {
                Log($"✅ Found Pause Button: {detectedPauseButton.name}", "green");
            }
            else
            {
                LogWarning("⚠️ Pause Button not found!");
            }

            Log("=== Detection Complete ===", "cyan");
        }

        [TitleGroup("Actions")]
        [Button("📦 Group Buttons into Panel", ButtonSizes.Large)]
        [GUIColor(0.4f, 1f, 0.4f)]
        [InfoBox("Creates a unified Bottom Action Bar panel and groups both buttons inside it.", InfoMessageType.Info)]
        private void GroupButtonsIntoPanel()
        {
            // Run detection first
            DetectButtons();

            // Use manual overrides if provided
            if (manualBuildButton != null) detectedBuildButton = manualBuildButton;
            if (manualPauseButton != null) detectedPauseButton = manualPauseButton;

            // Validation
            if (detectedCanvas == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "Canvas not found! Please create a Canvas first.",
                    "OK");
                return;
            }

            if (detectedBuildButton == null && detectedPauseButton == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "No buttons found!\n\n" +
                    "Please create at least one button (Build or Pause) first,\n" +
                    "or assign them manually in the 'Manual Override' section.",
                    "OK");
                return;
            }

            Log("=== Starting Containerization ===", "lime");

            // Step 1: Create the panel
            GameObject panel = CreateActionPanel();

            // Step 2: Move buttons into panel
            List<GameObject> movedButtons = new List<GameObject>();
            if (detectedBuildButton != null)
            {
                MoveButtonToPanel(detectedBuildButton, panel);
                movedButtons.Add(detectedBuildButton);
            }
            if (detectedPauseButton != null)
            {
                MoveButtonToPanel(detectedPauseButton, panel);
                movedButtons.Add(detectedPauseButton);
            }

            // Step 3: Ensure correct sibling order
            if (detectedBuildButton != null) detectedBuildButton.transform.SetSiblingIndex(0);
            if (detectedPauseButton != null) detectedPauseButton.transform.SetSiblingIndex(1);

            // Step 4: Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            Log("=== Containerization Complete ===", "lime");

            // Success dialog
            EditorUtility.DisplayDialog("Success!",
                $"Bottom Action Bar Created!\n\n" +
                $"✅ Panel: {panel.name}\n" +
                $"✅ Buttons Grouped: {movedButtons.Count}\n" +
                $"✅ Position: ({panelPosition.x}, {panelPosition.y})\n\n" +
                "The panel uses HorizontalLayoutGroup for automatic spacing.\n\n" +
                "Save the scene to keep changes.",
                "OK");

            // Select the panel in hierarchy
            Selection.activeGameObject = panel;
        }

        // ============================================
        // PANEL CREATION
        // ============================================

        private GameObject CreateActionPanel()
        {
            Log("Creating HUD Action Panel...", "cyan");

            // Check if panel already exists
            GameObject existingPanel = GameObject.Find(panelName);
            if (existingPanel != null)
            {
                bool replace = EditorUtility.DisplayDialog(
                    "Panel Already Exists",
                    $"A panel named '{panelName}' already exists.\n\nReplace it?",
                    "Yes, Replace",
                    "Cancel"
                );

                if (replace)
                {
                    Undo.DestroyObjectImmediate(existingPanel);
                    Log($"Destroyed existing panel: {panelName}", "yellow");
                }
                else
                {
                    return existingPanel;
                }
            }

            // Create panel GameObject
            GameObject panel = new GameObject(panelName);
            Undo.RegisterCreatedObjectUndo(panel, "Create HUD Action Panel");
            panel.transform.SetParent(detectedCanvas.transform, false);

            // Setup RectTransform
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0); // Bottom-Left
            panelRect.anchorMax = new Vector2(0, 0);
            panelRect.pivot = new Vector2(0, 0);
            panelRect.anchoredPosition = panelPosition;

            // Add Image component (background)
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = backgroundColor;
            if (panelSprite != null)
            {
                panelImage.sprite = panelSprite;
                panelImage.type = spriteType; // Use Sliced for rounded corners
            }
            else
            {
                // No sprite - use simple color fill
                panelImage.sprite = null;
                panelImage.type = Image.Type.Simple;
            }

            // Add Outline component (border)
            Outline outline = panel.AddComponent<Outline>();
            outline.effectColor = borderColor;
            outline.effectDistance = borderOffset;

            // Add HorizontalLayoutGroup
            HorizontalLayoutGroup layoutGroup = panel.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.padding = new RectOffset(paddingLeft, paddingRight, paddingTop, paddingBottom);
            layoutGroup.spacing = buttonSpacing;
            layoutGroup.childAlignment = childAlignment;
            layoutGroup.childControlWidth = childControlWidth;
            layoutGroup.childControlHeight = childControlHeight;
            layoutGroup.childScaleWidth = childScaleWidth;
            layoutGroup.childScaleHeight = childScaleHeight;
            layoutGroup.childForceExpandWidth = childForceExpandWidth;
            layoutGroup.childForceExpandHeight = childForceExpandHeight;

            // Add ContentSizeFitter (auto-size based on children)
            ContentSizeFitter sizeFitter = panel.AddComponent<ContentSizeFitter>();
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Log($"✅ Panel created: {panelName}", "green");
            Log($"   - Background: #{ColorUtility.ToHtmlStringRGBA(backgroundColor)}", "cyan");
            Log($"   - Border: #{ColorUtility.ToHtmlStringRGBA(borderColor)}", "cyan");
            Log($"   - Padding: L{paddingLeft} R{paddingRight} T{paddingTop} B{paddingBottom}", "cyan");
            Log($"   - Spacing: {buttonSpacing}px", "cyan");
            Log($"   - Child Control: W={childControlWidth} H={childControlHeight}", "cyan");

            return panel;
        }

        // ============================================
        // BUTTON MOVING
        // ============================================

        private void MoveButtonToPanel(GameObject button, GameObject panel)
        {
            if (button == null || panel == null) return;

            Log($"Moving {button.name} to panel...", "cyan");

            // Re-parent
            Undo.SetTransformParent(button.transform, panel.transform, "Move Button to Panel");

            // Reset local position (HorizontalLayoutGroup will take control)
            RectTransform buttonRect = button.GetComponent<RectTransform>();
            if (buttonRect != null)
            {
                buttonRect.anchoredPosition = Vector2.zero;
                buttonRect.localPosition = Vector3.zero;
            }

            Log($"✅ {button.name} moved to panel", "green");
        }

        // ============================================
        // BUTTON DETECTION HELPERS
        // ============================================

        private GameObject FindBuildButton()
        {
            string[] possibleNames = new string[]
            {
                "Btn_BuildMode",
                "BuildButton",
                "Btn_Build",
                "Build Button"
            };

            // Search by name
            foreach (string name in possibleNames)
            {
                GameObject obj = GameObject.Find(name);
                if (obj != null)
                {
                    Log($"Found Build Button: {name}", "cyan");
                    return obj;
                }
            }

            // Search in common containers
            if (detectedCanvas != null)
            {
                Transform[] children = detectedCanvas.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in children)
                {
                    if (child.name.ToLower().Contains("build") && child.GetComponent<Button>() != null)
                    {
                        Log($"Found Build Button via search: {child.name}", "cyan");
                        return child.gameObject;
                    }
                }
            }

            return null;
        }

        private GameObject FindPauseButton()
        {
            string[] possibleNames = new string[]
            {
                "Btn_Pause",
                "PauseButton",
                "Pause Button"
            };

            // Search by name
            foreach (string name in possibleNames)
            {
                GameObject obj = GameObject.Find(name);
                if (obj != null)
                {
                    Log($"Found Pause Button: {name}", "cyan");
                    return obj;
                }
            }

            // Search in common containers
            if (detectedCanvas != null)
            {
                Transform[] children = detectedCanvas.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in children)
                {
                    if (child.name.ToLower().Contains("pause") && child.GetComponent<Button>() != null)
                    {
                        Log($"Found Pause Button via search: {child.name}", "cyan");
                        return child.gameObject;
                    }
                }
            }

            return null;
        }

        // ============================================
        // CLEANUP
        // ============================================

        [TitleGroup("Cleanup")]
        [Button("🗑️ Remove Panel (Keep Buttons)", ButtonSizes.Medium)]
        [GUIColor(1f, 0.4f, 0.4f)]
        private void RemovePanel()
        {
            GameObject panel = GameObject.Find(panelName);
            if (panel != null)
            {
                bool confirm = EditorUtility.DisplayDialog(
                    "Confirm Removal",
                    $"This will destroy '{panelName}' and move its children back to Canvas.\n\nContinue?",
                    "Yes, Remove",
                    "Cancel"
                );

                if (confirm)
                {
                    // Move children back to Canvas
                    Canvas canvas = FindFirstObjectByType<Canvas>();
                    if (canvas != null)
                    {
                        while (panel.transform.childCount > 0)
                        {
                            Transform child = panel.transform.GetChild(0);
                            Undo.SetTransformParent(child, canvas.transform, "Move button to Canvas");
                        }
                    }

                    Undo.DestroyObjectImmediate(panel);
                    Log($"Panel '{panelName}' removed", "yellow");

                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                        UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
                    );
                }
            }
            else
            {
                LogWarning($"Panel '{panelName}' not found!");
            }
        }

        // ============================================
        // RESET COLORS
        // ============================================

        [TitleGroup("Quick Actions")]
        [Button("🎨 Reset to Default Colors", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.8f, 1f)]
        private void ResetToDefaultColors()
        {
            backgroundColor = CREAM_COLOR;
            borderColor = DARK_BROWN_COLOR;
            borderOffset = new Vector2(3, -3);
            Log("Colors reset to default (Cream/Dark Brown)", "cyan");
        }

        [TitleGroup("Quick Actions")]
        [Button("📏 Reset to Default Layout", ButtonSizes.Medium)]
        [GUIColor(0.8f, 1f, 0.8f)]
        private void ResetToDefaultLayout()
        {
            paddingLeft = 25;
            paddingRight = 25;
            paddingTop = 15;
            paddingBottom = 15;
            buttonSpacing = 25f;
            childAlignment = TextAnchor.MiddleCenter;
            childControlWidth = true;
            childControlHeight = true;
            childScaleWidth = true;
            childScaleHeight = true;
            childForceExpandWidth = false;
            childForceExpandHeight = false;
            panelPosition = new Vector2(50, 50);
            Log("Layout settings reset to defaults (polished spacing)", "cyan");
        }

        // ============================================
        // LOGGING
        // ============================================

        private void Log(string message, string color)
        {
            if (verboseLogging)
            {
                Debug.Log($"<color={color}>[HUDContainerizer]</color> {message}");
            }
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"<color=yellow>[HUDContainerizer]</color> {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"<color=red>[HUDContainerizer]</color> {message}");
        }

        // ============================================
        // PREVIEW (EDITOR ONLY)
        // ============================================

        [TitleGroup("Preview")]
        [InfoBox("This shows a visual preview of the panel colors.", InfoMessageType.None)]
        [ShowInInspector]
        [PreviewField(100, ObjectFieldAlignment.Center)]
        [HideLabel]
        private Texture2D ColorPreview => GenerateColorPreview();

        private Texture2D GenerateColorPreview()
        {
            Texture2D tex = new Texture2D(200, 100);
            Color[] pixels = new Color[200 * 100];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = backgroundColor;
            }

            // Draw border
            for (int x = 0; x < 200; x++)
            {
                for (int y = 0; y < 100; y++)
                {
                    if (x < 3 || x > 196 || y < 3 || y > 96)
                    {
                        pixels[y * 200 + x] = borderColor;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
#endif
