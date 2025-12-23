using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using WildernessSurvival.Gameplay.Structures;

namespace WildernessSurvival.EditorTools
{
    /// <summary>
    /// Editor tool to create and setup the Bottom HUD with Build Mode button.
    /// Uses "Cream & Wood" visual style.
    /// </summary>
    public class BottomHUDSetup : EditorWindow
    {
        private const string KIT_PATH = "Assets/ModularGameUIKit/Common";
        
        // =============================================
        // CREAM & WOOD COLOR PALETTE
        // =============================================
        private static readonly Color BUTTON_BG_CREAM = new Color(0.949f, 0.902f, 0.847f, 1f);    // #F2E6D8
        private static readonly Color BUTTON_OUTLINE_WOOD = new Color(0.365f, 0.251f, 0.216f, 1f); // #5D4037
        private static readonly Color TEXT_DEEP_BROWN = new Color(0.243f, 0.153f, 0.137f, 1f);     // #3E2723
        private static readonly Color BUTTON_HIGHLIGHT = new Color(1f, 0.973f, 0.933f, 1f);        // Slightly lighter cream
        private static readonly Color BUTTON_PRESSED = new Color(0.878f, 0.831f, 0.776f, 1f);      // Darker cream

        [MenuItem("Tools/UI Kit/Create Bottom HUD (Build Button)")]
        public static void CreateBottomHUD()
        {
            Debug.Log("<color=orange>[Bottom HUD]</color> Creating Bottom HUD with Build Mode button...");

            // Find or create Canvas
            Canvas canvas = FindOrCreateHUDCanvas();
            if (canvas == null)
            {
                Debug.LogError("[Bottom HUD] Failed to find or create Canvas!");
                return;
            }

            // Load assets
            Sprite buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{KIT_PATH}/Sprites/Shapes/Background.png");
            Sprite hammerIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{KIT_PATH}/Sprites/Icons/Hammer.png");
            TMP_FontAsset fontBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{KIT_PATH}/Fonts/Inter-SemiBold SDF.asset");

            if (buttonSprite == null)
            {
                Debug.LogWarning("[Bottom HUD] Background.png not found, using default Unity sprite");
            }

            // Create BottomLeft anchor
            Transform bottomLeftAnchor = canvas.transform.Find("BottomLeft_Anchor");
            if (bottomLeftAnchor == null)
            {
                GameObject anchorObj = new GameObject("BottomLeft_Anchor");
                anchorObj.transform.SetParent(canvas.transform, false);
                
                RectTransform anchorRect = anchorObj.AddComponent<RectTransform>();
                anchorRect.anchorMin = new Vector2(0, 0);
                anchorRect.anchorMax = new Vector2(0, 0);
                anchorRect.pivot = new Vector2(0, 0);
                anchorRect.anchoredPosition = new Vector2(30, 30); // 30px padding
                anchorRect.sizeDelta = new Vector2(150, 150);
                
                bottomLeftAnchor = anchorObj.transform;
            }

            // Check if button already exists
            Transform existingButton = bottomLeftAnchor.Find("Btn_BuildMode");
            if (existingButton != null)
            {
                Debug.Log("[Bottom HUD] Btn_BuildMode already exists, updating style...");
                UpdateButtonStyle(existingButton.gameObject, buttonSprite, hammerIcon, fontBold);
            }
            else
            {
                // Create Build Mode Button
                GameObject buttonObj = CreateBuildModeButton(bottomLeftAnchor, buttonSprite, hammerIcon, fontBold);
                
                // Wire to BuildModeController
                WireButtonToBuildMode(buttonObj);
            }

            // Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("<color=green>[Bottom HUD]</color> ✅ Bottom HUD created successfully!");
            EditorUtility.DisplayDialog("Bottom HUD Created",
                "Build Mode button has been created:\n\n" +
                "• Location: BottomLeft_Anchor/Btn_BuildMode\n" +
                "• Size: 120x120 pixels\n" +
                "• Style: Cream & Wood\n" +
                "• Icon: Hammer\n\n" +
                "The button is wired to BuildModeController.ToggleBuildMode().\n\n" +
                "Save the scene to keep changes.",
                "OK");
        }

        private static Canvas FindOrCreateHUDCanvas()
        {
            // Look for existing HUD canvas
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                if (canvas.name.Contains("HUD") || canvas.name.Contains("UI"))
                {
                    return canvas;
                }
            }

            // Look for any canvas
            if (canvases.Length > 0)
            {
                return canvases[0];
            }

            // Create new canvas
            GameObject canvasObj = new GameObject("HUD_Canvas");
            Canvas newCanvas = canvasObj.AddComponent<Canvas>();
            newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            newCanvas.sortingOrder = 100;
            
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            return newCanvas;
        }

        private static GameObject CreateBuildModeButton(Transform parent, Sprite buttonSprite, Sprite hammerIcon, TMP_FontAsset font)
        {
            // Create button GameObject
            GameObject buttonObj = new GameObject("Btn_BuildMode");
            buttonObj.transform.SetParent(parent, false);

            // RectTransform - 120x120 for mobile tapping
            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0, 0);
            buttonRect.anchorMax = new Vector2(0, 0);
            buttonRect.pivot = new Vector2(0, 0);
            buttonRect.anchoredPosition = Vector2.zero;
            buttonRect.sizeDelta = new Vector2(120, 120);

            // Image component (background)
            Image buttonImage = buttonObj.AddComponent<Image>();
            if (buttonSprite != null)
            {
                buttonImage.sprite = buttonSprite;
                buttonImage.type = Image.Type.Sliced;
            }
            buttonImage.color = BUTTON_BG_CREAM;

            // Button component
            Button button = buttonObj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = BUTTON_BG_CREAM;
            colors.highlightedColor = BUTTON_HIGHLIGHT;
            colors.pressedColor = BUTTON_PRESSED;
            colors.selectedColor = BUTTON_HIGHLIGHT;
            colors.disabledColor = new Color(0.8f, 0.75f, 0.7f, 0.5f);
            button.colors = colors;

            // Add outline for wood border effect
            Outline outline = buttonObj.AddComponent<Outline>();
            outline.effectColor = BUTTON_OUTLINE_WOOD;
            outline.effectDistance = new Vector2(3, -3);

            // Create icon container
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(buttonObj.transform, false);
            
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.1f, 0.35f);
            iconRect.anchorMax = new Vector2(0.9f, 0.95f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            Image iconImage = iconObj.AddComponent<Image>();
            if (hammerIcon != null)
            {
                iconImage.sprite = hammerIcon;
                iconImage.preserveAspect = true;
                Debug.Log("<color=green>[Bottom HUD]</color> ✅ Hammer icon applied!");
            }
            else
            {
                iconImage.color = TEXT_DEEP_BROWN;
                Debug.LogWarning("[Bottom HUD] Hammer icon not found");
            }

            // Create text label
            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(buttonObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0.05f);
            textRect.anchorMax = new Vector2(1, 0.35f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = textObj.AddComponent<TextMeshProUGUI>();
            label.text = "BUILD";
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 18;
            label.fontStyle = FontStyles.Bold;
            label.color = TEXT_DEEP_BROWN;
            
            if (font != null)
            {
                label.font = font;
            }

            EditorUtility.SetDirty(buttonObj);
            return buttonObj;
        }

        private static void UpdateButtonStyle(GameObject buttonObj, Sprite buttonSprite, Sprite hammerIcon, TMP_FontAsset font)
        {
            // Update background
            Image buttonImage = buttonObj.GetComponent<Image>();
            if (buttonImage != null)
            {
                if (buttonSprite != null)
                {
                    buttonImage.sprite = buttonSprite;
                    buttonImage.type = Image.Type.Sliced;
                }
                buttonImage.color = BUTTON_BG_CREAM;
                EditorUtility.SetDirty(buttonImage);
            }

            // Update button colors
            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                ColorBlock colors = button.colors;
                colors.normalColor = BUTTON_BG_CREAM;
                colors.highlightedColor = BUTTON_HIGHLIGHT;
                colors.pressedColor = BUTTON_PRESSED;
                colors.selectedColor = BUTTON_HIGHLIGHT;
                button.colors = colors;
                EditorUtility.SetDirty(button);
            }

            // Update outline
            Outline outline = buttonObj.GetComponent<Outline>();
            if (outline == null)
            {
                outline = buttonObj.AddComponent<Outline>();
            }
            outline.effectColor = BUTTON_OUTLINE_WOOD;
            outline.effectDistance = new Vector2(3, -3);
            EditorUtility.SetDirty(outline);

            // Update icon
            Transform iconTransform = buttonObj.transform.Find("Icon");
            if (iconTransform != null && hammerIcon != null)
            {
                Image iconImage = iconTransform.GetComponent<Image>();
                if (iconImage != null)
                {
                    iconImage.sprite = hammerIcon;
                    iconImage.preserveAspect = true;
                    EditorUtility.SetDirty(iconImage);
                }
            }

            // Update label
            Transform labelTransform = buttonObj.transform.Find("Label");
            if (labelTransform != null && font != null)
            {
                TextMeshProUGUI label = labelTransform.GetComponent<TextMeshProUGUI>();
                if (label != null)
                {
                    label.font = font;
                    label.color = TEXT_DEEP_BROWN;
                    EditorUtility.SetDirty(label);
                }
            }

            // Re-wire button (in case it was lost)
            WireButtonToBuildMode(buttonObj);
        }

        private static void WireButtonToBuildMode(GameObject buttonObj)
        {
            Button button = buttonObj.GetComponent<Button>();
            if (button == null) return;

            // Find BuildModeController in scene
            BuildModeController buildController = Object.FindFirstObjectByType<BuildModeController>();
            
            if (buildController != null)
            {
                // Clear existing listeners and add new one
                button.onClick.RemoveAllListeners();
                
                // Use UnityEvent persistent listener for serialization
                UnityEditor.Events.UnityEventTools.RemovePersistentListener(button.onClick, 0);
                UnityEditor.Events.UnityEventTools.AddPersistentListener(
                    button.onClick, 
                    buildController.ToggleBuildMode
                );
                
                EditorUtility.SetDirty(button);
                Debug.Log("<color=green>[Bottom HUD]</color> ✅ Button wired to BuildModeController.ToggleBuildMode()");
            }
            else
            {
                Debug.LogWarning("[Bottom HUD] ⚠️ BuildModeController not found in scene!\n" +
                    "The button has been created but not wired.\n" +
                    "Please wire it manually in the Inspector:\n" +
                    "  OnClick() → BuildModeController.ToggleBuildMode()");
            }
        }
    }
}
