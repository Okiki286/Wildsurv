using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using WildernessSurvival.UI;

namespace WildernessSurvival.EditorTools
{
    /// <summary>
    /// Editor tool to set up the Day/Night Cycle Indicator UI in the Top-Right corner.
    /// Creates a pill-shaped progress bar with the Cream & Wood theme.
    /// </summary>
    public class DayNightUISetup : EditorWindow
    {
        private const string KIT_PATH = "Assets/ModularGameUIKit/Common";

        // =============================================
        // WILDERNESS PALETTE (CREAM & WOOD)
        // =============================================
        private static readonly Color CREAM_BG = new Color(0.949f, 0.902f, 0.847f, 1f);           // #F2E6D8
        private static readonly Color DARK_WOOD = new Color(0.365f, 0.251f, 0.216f, 1f);          // #5D4037
        private static readonly Color DEEP_BROWN = new Color(0.243f, 0.153f, 0.137f, 1f);         // #3E2723
        private static readonly Color SUNSET_ORANGE = new Color(0.957f, 0.635f, 0.380f, 1f);      // #F4A261

        [MenuItem("Tools/UI Kit/Create Day-Night Clock")]
        public static void CreateDayNightClock()
        {
            Debug.Log("<color=orange>[DayNight UI Setup]</color> Creating Day/Night Cycle Indicator...");

            // Load assets
            Sprite capsuleSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{KIT_PATH}/Sprites/Shapes/Capsule.png");
            Sprite backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{KIT_PATH}/Sprites/Shapes/Background.png");
            TMP_FontAsset fontSemiBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{KIT_PATH}/Fonts/Inter-SemiBold SDF.asset");

            // Use Background.png if Capsule not found
            Sprite pillSprite = capsuleSprite != null ? capsuleSprite : backgroundSprite;

            if (pillSprite == null)
            {
                EditorUtility.DisplayDialog("Assets Missing",
                    "Could not load sprites from:\n" +
                    $"{KIT_PATH}/Sprites/Shapes/\n\n" +
                    "Check if the Modular Game UI Kit is imported.",
                    "OK");
                return;
            }

            // Find the HUD Canvas
            Canvas hudCanvas = FindHUDCanvas();
            if (hudCanvas == null)
            {
                Debug.LogWarning("[DayNight UI Setup] No Canvas found. Creating a new HUD Canvas...");
                hudCanvas = CreateHUDCanvas();
            }

            // Check if already exists
            Transform existing = hudCanvas.transform.Find("DayNightClock");
            if (existing != null)
            {
                Debug.Log("<color=yellow>[DayNight UI Setup]</color> DayNightClock already exists. Updating styles...");
                StyleExistingClock(existing.gameObject, pillSprite, fontSemiBold);
                return;
            }

            // Create the hierarchy
            GameObject clock = CreateClockHierarchy(hudCanvas.transform, pillSprite, fontSemiBold);

            // Add the DayNightUI component
            DayNightUI uiComponent = clock.AddComponent<DayNightUI>();
            
            // Wire up references via SerializedObject
            SerializedObject so = new SerializedObject(uiComponent);
            
            Slider slider = clock.GetComponentInChildren<Slider>(true);
            TextMeshProUGUI dayText = clock.transform.Find("DayText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI timerText = clock.transform.Find("TimerText")?.GetComponent<TextMeshProUGUI>();
            Image phaseIcon = clock.transform.Find("PhaseIcon")?.GetComponent<Image>();

            so.FindProperty("timeSlider").objectReferenceValue = slider;
            so.FindProperty("dayText").objectReferenceValue = dayText;
            so.FindProperty("timerText").objectReferenceValue = timerText;
            so.FindProperty("phaseIcon").objectReferenceValue = phaseIcon;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(clock);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("<color=green>[DayNight UI Setup]</color> ✅ Day/Night Clock created in Top-Right corner!");
            EditorUtility.DisplayDialog("Day/Night Clock Created",
                "The Day/Night Cycle Indicator has been created:\n\n" +
                "• Position: Top-Right corner\n" +
                "• Style: Pill-shaped Cream background\n" +
                "• Fill Color: Sunset Orange (#F4A261)\n" +
                "• Text: Deep Brown (#3E2723)\n\n" +
                "The DayNightUI component is attached and wired.\n" +
                "Save the scene to keep changes.",
                "OK");
        }

        private static Canvas FindHUDCanvas()
        {
            // Look for existing UI canvas
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (Canvas c in canvases)
            {
                if (c.gameObject.name.Contains("HUD") || c.gameObject.name.Contains("UI"))
                {
                    return c;
                }
            }
            // Return any canvas
            return canvases.Length > 0 ? canvases[0] : null;
        }

        private static Canvas CreateHUDCanvas()
        {
            GameObject canvasObj = new GameObject("HUD_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static GameObject CreateClockHierarchy(Transform parent, Sprite pillSprite, TMP_FontAsset font)
        {
            // 1. MAIN CONTAINER - DayNightClock
            GameObject clock = new GameObject("DayNightClock");
            clock.transform.SetParent(parent, false);

            RectTransform clockRect = clock.AddComponent<RectTransform>();
            clockRect.anchorMin = new Vector2(1, 1); // Top-Right
            clockRect.anchorMax = new Vector2(1, 1);
            clockRect.pivot = new Vector2(1, 1);
            clockRect.anchoredPosition = new Vector2(-20, -20); // 20px padding from corner
            clockRect.sizeDelta = new Vector2(180, 50); // Pill size

            // 2. BACKGROUND IMAGE (Pill shape)
            Image bgImage = clock.AddComponent<Image>();
            bgImage.sprite = pillSprite;
            bgImage.color = CREAM_BG;
            bgImage.type = Image.Type.Sliced;
            bgImage.raycastTarget = false;

            // Add outline
            Outline outline = clock.AddComponent<Outline>();
            outline.effectColor = DARK_WOOD;
            outline.effectDistance = new Vector2(2, -2);

            // 3. SLIDER (Progress Bar)
            GameObject sliderObj = CreateSlider(clock.transform, pillSprite);

            // 4. DAY TEXT (Centered)
            GameObject dayTextObj = new GameObject("DayText");
            dayTextObj.transform.SetParent(clock.transform, false);

            RectTransform dayTextRect = dayTextObj.AddComponent<RectTransform>();
            dayTextRect.anchorMin = new Vector2(0, 0);
            dayTextRect.anchorMax = new Vector2(1, 1);
            dayTextRect.offsetMin = Vector2.zero;
            dayTextRect.offsetMax = Vector2.zero;

            TextMeshProUGUI dayText = dayTextObj.AddComponent<TextMeshProUGUI>();
            dayText.text = "Day 1";
            dayText.font = font;
            dayText.fontSize = 18;
            dayText.fontStyle = FontStyles.Bold;
            dayText.color = DEEP_BROWN;
            dayText.alignment = TextAlignmentOptions.Center;
            dayText.raycastTarget = false;

            // 5. TIMER TEXT (Right side, optional)
            GameObject timerTextObj = new GameObject("TimerText");
            timerTextObj.transform.SetParent(clock.transform, false);

            RectTransform timerRect = timerTextObj.AddComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(1, 0);
            timerRect.anchorMax = new Vector2(1, 1);
            timerRect.pivot = new Vector2(1, 0.5f);
            timerRect.anchoredPosition = new Vector2(-10, 0);
            timerRect.sizeDelta = new Vector2(50, 20);

            TextMeshProUGUI timerText = timerTextObj.AddComponent<TextMeshProUGUI>();
            timerText.text = "2:00";
            timerText.font = font;
            timerText.fontSize = 12;
            timerText.color = DEEP_BROWN;
            timerText.alignment = TextAlignmentOptions.MidlineRight;
            timerText.raycastTarget = false;

            // 6. PHASE ICON (Sun/Moon) - Left side, overlapping the border
            GameObject phaseIconObj = new GameObject("PhaseIcon");
            phaseIconObj.transform.SetParent(clock.transform, false);

            RectTransform iconRect = phaseIconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0.5f); // Middle-Left
            iconRect.anchorMax = new Vector2(0, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(-35, 0); // Offset outside the bar
            iconRect.sizeDelta = new Vector2(50, 50); // Prominent size

            Image phaseIcon = phaseIconObj.AddComponent<Image>();
            phaseIcon.color = Color.white; // Full color for sprite
            phaseIcon.raycastTarget = false;
            // Note: Sprite will be assigned by user in Inspector (sunSprite/moonSprite)

            // Add Shadow for visual polish
            Shadow iconShadow = phaseIconObj.AddComponent<Shadow>();
            iconShadow.effectColor = new Color(0, 0, 0, 0.5f);
            iconShadow.effectDistance = new Vector2(2, -2);

            return clock;
        }

        private static GameObject CreateSlider(Transform parent, Sprite fillSprite)
        {
            // Create slider container
            GameObject sliderObj = new GameObject("TimeSlider");
            sliderObj.transform.SetParent(parent, false);

            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0, 0);
            sliderRect.anchorMax = new Vector2(1, 1);
            sliderRect.offsetMin = new Vector2(8, 8); // Padding inside pill
            sliderRect.offsetMax = new Vector2(-8, -8);

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.interactable = false; // Display only
            slider.transition = Selectable.Transition.None;
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 0.5f; // Preview middle

            // ========================================
            // SLIDER BACKGROUND - "Dark Slot" Effect
            // ========================================
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.sprite = fillSprite; // Use same sprite for consistent rounded edges
            bgImage.color = DEEP_BROWN;  // #3E2723 - Dark Chocolate slot
            bgImage.type = Image.Type.Sliced;
            bgImage.raycastTarget = false;

            // Add Outline to Background for edge definition
            Outline bgOutline = bgObj.AddComponent<Outline>();
            bgOutline.effectColor = DARK_WOOD; // #5D4037 - Lighter Wood border
            bgOutline.effectDistance = new Vector2(2, -2);

            // ========================================
            // FILL AREA - With padding for "inset" look
            // ========================================
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            // Add 3px padding so fill sits INSIDE the dark slot
            fillAreaRect.offsetMin = new Vector2(3, 3);
            fillAreaRect.offsetMax = new Vector2(-3, -3);

            // ========================================
            // FILL BAR - Sunset Orange progress
            // ========================================
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0.5f, 1); // Half filled for preview
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            Image fillImage = fill.AddComponent<Image>();
            fillImage.sprite = fillSprite;
            fillImage.color = SUNSET_ORANGE; // #F4A261
            fillImage.type = Image.Type.Sliced;
            fillImage.raycastTarget = false;

            // Wire slider
            slider.fillRect = fillRect;
            slider.targetGraphic = null;

            return sliderObj;
        }

        private static void StyleExistingClock(GameObject clock, Sprite pillSprite, TMP_FontAsset font)
        {
            Image bgImage = clock.GetComponent<Image>();
            if (bgImage != null)
            {
                bgImage.sprite = pillSprite;
                bgImage.color = CREAM_BG;
                bgImage.type = Image.Type.Sliced;
                EditorUtility.SetDirty(bgImage);
            }

            Outline outline = clock.GetComponent<Outline>();
            if (outline == null) outline = clock.AddComponent<Outline>();
            outline.effectColor = DARK_WOOD;
            outline.effectDistance = new Vector2(2, -2);
            EditorUtility.SetDirty(outline);

            // Style texts
            TextMeshProUGUI[] texts = clock.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var txt in texts)
            {
                if (font != null) txt.font = font;
                txt.color = DEEP_BROWN;
                EditorUtility.SetDirty(txt);
            }

            // Style fill
            Slider slider = clock.GetComponentInChildren<Slider>(true);
            if (slider != null && slider.fillRect != null)
            {
                Image fillImage = slider.fillRect.GetComponent<Image>();
                if (fillImage != null)
                {
                    fillImage.color = SUNSET_ORANGE;
                    EditorUtility.SetDirty(fillImage);
                }
            }

            EditorUtility.SetDirty(clock);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("<color=green>[DayNight UI Setup]</color> ✅ Existing clock restyled!");
        }
    }
}
