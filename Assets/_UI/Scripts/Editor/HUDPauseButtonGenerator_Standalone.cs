#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using WildernessSurvival.UI;

namespace WildernessSurvival.Editor
{
    /// <summary>
    /// Standalone utility to quickly generate a HUD Pause Button for mobile.
    /// This is a compact version without Odin dependencies.
    /// Menu: Tools/Wilderness/Quick Generate HUD Pause Button
    /// </summary>
    public static class HUDPauseButtonGenerator_Standalone
    {
        // Color constants (matches your Cream/Wood theme)
        private static readonly Color BUTTON_BG_CREAM = new Color(240f / 255f, 230f / 255f, 210f / 255f, 1f);
        private static readonly Color BUTTON_HIGHLIGHT = new Color(255f / 255f, 245f / 255f, 220f / 255f, 1f);
        private static readonly Color BUTTON_PRESSED = new Color(220f / 255f, 210f / 255f, 190f / 255f, 1f);
        private static readonly Color TEXT_DARK_BROWN = new Color(60f / 255f, 40f / 255f, 20f / 255f, 1f);

        [MenuItem("Tools/Wilderness/Quick Generate HUD Pause Button")]
        public static void GenerateHUDPauseButton()
        {
            // Find Canvas
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Error", "No Canvas found in scene! Create a Canvas first.", "OK");
                return;
            }

            // Find or create BottomLeft_Anchor
            Transform bottomLeftAnchor = canvas.transform.Find("BottomLeft_Anchor");
            if (bottomLeftAnchor == null)
            {
                bottomLeftAnchor = CreateBottomLeftAnchor(canvas.transform);
            }

            // Check if button already exists
            Transform existingBtn = bottomLeftAnchor.Find("Btn_Pause");
            if (existingBtn != null)
            {
                bool replace = EditorUtility.DisplayDialog(
                    "Replace Existing?",
                    "A Pause button already exists. Replace it?",
                    "Yes, Replace",
                    "Cancel"
                );

                if (replace)
                {
                    Undo.DestroyObjectImmediate(existingBtn.gameObject);
                }
                else
                {
                    return;
                }
            }

            // Create the button
            GameObject pauseButton = CreatePauseButton(bottomLeftAnchor);

            // Wire to PauseMenuUI
            WireToPauseMenuUI(pauseButton);

            // Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            Debug.Log("<color=green>[HUD Pause Button]</color> ✅ Created successfully!");
            Selection.activeGameObject = pauseButton; // Select it in hierarchy
        }

        private static Transform CreateBottomLeftAnchor(Transform canvasTransform)
        {
            GameObject anchorObj = new GameObject("BottomLeft_Anchor");
            Undo.RegisterCreatedObjectUndo(anchorObj, "Create BottomLeft_Anchor");
            anchorObj.transform.SetParent(canvasTransform, false);

            RectTransform rect = anchorObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0, 0);
            rect.pivot = new Vector2(0, 0);
            rect.anchoredPosition = new Vector2(30, 30);
            rect.sizeDelta = new Vector2(150, 150);

            return anchorObj.transform;
        }

        private static GameObject CreatePauseButton(Transform parent)
        {
            // Create button GameObject
            GameObject buttonObj = new GameObject("Btn_Pause");
            Undo.RegisterCreatedObjectUndo(buttonObj, "Create Btn_Pause");
            buttonObj.transform.SetParent(parent, false);

            // RectTransform (positioned to right of Build button)
            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0, 0);
            buttonRect.anchorMax = new Vector2(0, 0);
            buttonRect.pivot = new Vector2(0, 0);
            buttonRect.anchoredPosition = new Vector2(150, 0); // Build button is at (0,0) with 120px width
            buttonRect.sizeDelta = new Vector2(100, 100);

            // Image (background)
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = BUTTON_BG_CREAM;

            // Button component
            Button button = buttonObj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = BUTTON_BG_CREAM;
            colors.highlightedColor = BUTTON_HIGHLIGHT;
            colors.pressedColor = BUTTON_PRESSED;
            colors.selectedColor = BUTTON_HIGHLIGHT;
            button.colors = colors;

            // Outline (wood border)
            Outline outline = buttonObj.AddComponent<Outline>();
            outline.effectColor = TEXT_DARK_BROWN;
            outline.effectDistance = new Vector2(3, -3);

            // Create pause icon (||)
            GameObject iconObj = new GameObject("Icon");
            Undo.RegisterCreatedObjectUndo(iconObj, "Create Icon");
            iconObj.transform.SetParent(buttonObj.transform, false);

            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(10, 10);
            iconRect.offsetMax = new Vector2(-10, -10);

            TextMeshProUGUI iconText = iconObj.AddComponent<TextMeshProUGUI>();
            iconText.text = "||";
            iconText.fontSize = 72;
            iconText.fontStyle = FontStyles.Bold;
            iconText.alignment = TextAlignmentOptions.Center;
            iconText.color = TEXT_DARK_BROWN;

            // Try to find a TMP font asset
            TMP_FontAsset font = FindTMPFont();
            if (font != null)
            {
                iconText.font = font;
            }

            return buttonObj;
        }

        private static void WireToPauseMenuUI(GameObject buttonObj)
        {
            Button button = buttonObj.GetComponent<Button>();
            if (button == null) return;

            PauseMenuUI pauseMenuUI = Object.FindFirstObjectByType<PauseMenuUI>();

            if (pauseMenuUI != null)
            {
                // Clear and add persistent listener
                button.onClick.RemoveAllListeners();
                UnityEditor.Events.UnityEventTools.RemovePersistentListener(button.onClick, 0);
                UnityEditor.Events.UnityEventTools.AddPersistentListener(
                    button.onClick,
                    pauseMenuUI.TogglePause
                );

                EditorUtility.SetDirty(button);
                Debug.Log("<color=green>[HUD Pause Button]</color> ✅ Wired to PauseMenuUI.TogglePause()");
            }
            else
            {
                Debug.LogWarning("<color=yellow>[HUD Pause Button]</color> ⚠️ PauseMenuUI not found!\n" +
                    "Generate the Pause Menu first, or wire manually in Inspector.");
            }
        }

        private static TMP_FontAsset FindTMPFont()
        {
            // Try to find any TMP font in the project
            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            }
            return null;
        }
    }
}
#endif
