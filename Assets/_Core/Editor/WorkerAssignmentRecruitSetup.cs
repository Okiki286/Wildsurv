using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using WildernessSurvival.Gameplay.Resources;

namespace WildernessSurvival.Core.Editor
{
    /// <summary>
    /// Editor tool per creare/aggiornare il RecruitButton con costo integrato.
    /// Il costo (icona + valore) è direttamente dentro il bottone.
    /// </summary>
    public static class WorkerAssignmentRecruitSetup
    {
        [MenuItem("Tools/Wilderness/Population/Create Recruit Button (Merged Cost)")]
        public static void CreateRecruitButtonWithMergedCost()
        {
            // 1. Trova il WorkerAssignmentUI nella scena
            var workerAssignmentUI = Object.FindFirstObjectByType<UI.WorkerAssignmentUI>();
            
            if (workerAssignmentUI == null)
            {
                Debug.LogError("[RecruitSetup] WorkerAssignmentUI not found in scene! " +
                    "Open the prefab or ensure it's in the scene.");
                return;
            }

            // 2. Trova il pannello principale
            Transform panelTransform = workerAssignmentUI.transform.Find("AssignmentPanel");
            if (panelTransform == null)
            {
                if (workerAssignmentUI.transform.childCount > 0)
                {
                    panelTransform = workerAssignmentUI.transform.GetChild(0);
                }
                else
                {
                    Debug.LogError("[RecruitSetup] No AssignmentPanel found!");
                    return;
                }
            }

            // 3. Elimina vecchia RecruitSection se esiste
            var oldSection = panelTransform.Find("RecruitSection");
            if (oldSection != null)
            {
                Object.DestroyImmediate(oldSection.gameObject);
                Debug.Log("[RecruitSetup] Old RecruitSection deleted.");
            }

            // Elimina vecchio RecruitButton se esiste
            var oldButton = panelTransform.Find("RecruitButton");
            if (oldButton != null)
            {
                Object.DestroyImmediate(oldButton.gameObject);
                Debug.Log("[RecruitSetup] Old RecruitButton deleted.");
            }

            // 4. Carica icona Food da ResourceData
            Sprite foodSprite = null;
            var foodData = AssetDatabase.LoadAssetAtPath<ResourceData>("Assets/_Content/Data/Resources/Food.asset");
            if (foodData != null && foodData.Icon != null)
            {
                foodSprite = foodData.Icon;
                Debug.Log($"[RecruitSetup] Food icon loaded: {foodSprite.name}");
            }
            else
            {
                Debug.LogWarning("[RecruitSetup] Could not load Food icon from ResourceData!");
            }

            // ═══════════════════════════════════════════════════════════
            // 5. CREA RECRUIT BUTTON (con costo integrato)
            // ═══════════════════════════════════════════════════════════

            GameObject buttonObj = new GameObject("RecruitButton");
            buttonObj.transform.SetParent(panelTransform, false);
            
            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0);
            buttonRect.anchorMax = new Vector2(0.5f, 0);
            buttonRect.pivot = new Vector2(0.5f, 0);
            buttonRect.anchoredPosition = new Vector2(0, 30);
            buttonRect.sizeDelta = new Vector2(280, 50);
            
            Image buttonImg = buttonObj.AddComponent<Image>();
            buttonImg.color = new Color(0.2f, 0.55f, 0.35f, 1f);
            
            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonImg;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.55f, 0.35f);
            colors.highlightedColor = new Color(0.3f, 0.65f, 0.45f);
            colors.pressedColor = new Color(0.15f, 0.45f, 0.25f);
            colors.disabledColor = new Color(0.3f, 0.3f, 0.3f);
            button.colors = colors;

            // HorizontalLayoutGroup sul bottone
            HorizontalLayoutGroup layout = buttonObj.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(15, 15, 5, 5);
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // ═══════════════════════════════════════════════════════════
            // 6. FIGLI: [ButtonText] -> [FoodIcon] -> [CostText]
            // ═══════════════════════════════════════════════════════════

            // A) Button Text
            GameObject buttonTextObj = new GameObject("ButtonText");
            buttonTextObj.transform.SetParent(buttonObj.transform, false);
            
            RectTransform buttonTextRect = buttonTextObj.AddComponent<RectTransform>();
            buttonTextRect.sizeDelta = new Vector2(130, 40);
            
            TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
            buttonText.raycastTarget = false;
            buttonText.text = "Recruit Worker";
            buttonText.fontSize = 18;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;

            // B) Food Icon (l'icona dinamica!)
            GameObject iconObj = new GameObject("CostIcon");
            iconObj.transform.SetParent(buttonObj.transform, false);
            
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(24, 24);
            
            Image foodIcon = iconObj.AddComponent<Image>();
            foodIcon.raycastTarget = false;
            foodIcon.color = Color.white; // Bianco per mostrare il colore originale della sprite
            foodIcon.preserveAspect = true;
            if (foodSprite != null)
            {
                foodIcon.sprite = foodSprite;
            }

            // C) Cost Text
            GameObject costTextObj = new GameObject("CostText");
            costTextObj.transform.SetParent(buttonObj.transform, false);
            
            RectTransform costTextRect = costTextObj.AddComponent<RectTransform>();
            costTextRect.sizeDelta = new Vector2(50, 40);
            
            TextMeshProUGUI costText = costTextObj.AddComponent<TextMeshProUGUI>();
            costText.raycastTarget = false;
            costText.text = "40";
            costText.fontSize = 20;
            costText.fontStyle = FontStyles.Bold;
            costText.alignment = TextAlignmentOptions.Left;
            costText.color = Color.white;

            // ═══════════════════════════════════════════════════════════
            // 7. AGGIUNGI RECRUIT UI COMPONENT
            // ═══════════════════════════════════════════════════════════

            var recruitUI = buttonObj.AddComponent<UI.RecruitUI>();

            // Assegna riferimenti via SerializedObject
            SerializedObject so = new SerializedObject(recruitUI);
            so.FindProperty("recruitButton").objectReferenceValue = button;
            so.FindProperty("buttonText").objectReferenceValue = buttonText;
            so.FindProperty("costText").objectReferenceValue = costText;
            so.FindProperty("costIconImage").objectReferenceValue = foodIcon;
            so.ApplyModifiedPropertiesWithoutUndo();

            // ═══════════════════════════════════════════════════════════
            // 8. AGGIORNA WORKER ASSIGNMENT UI
            // ═══════════════════════════════════════════════════════════

            SerializedObject uiSo = new SerializedObject(workerAssignmentUI);
            uiSo.FindProperty("recruitButton").objectReferenceValue = buttonObj;
            uiSo.FindProperty("recruitUIComponent").objectReferenceValue = recruitUI;
            uiSo.ApplyModifiedPropertiesWithoutUndo();

            // 9. Seleziona e ping
            Selection.activeGameObject = buttonObj;
            EditorGUIUtility.PingObject(buttonObj);

            Debug.Log("<color=green>[RecruitSetup]</color> ✅ RecruitButton created with merged cost!\n" +
                "Structure:\n" +
                "  └─ RecruitButton (Button + HorizontalLayoutGroup)\n" +
                "      ├─ ButtonText ('Recruit Worker')\n" +
                "      ├─ CostIcon (Food sprite)\n" +
                "      └─ CostText ('40')\n\n" +
                "⚠️ Remember to SAVE the prefab! (Ctrl+S or Apply)");
        }

        [MenuItem("Tools/Wilderness/Population/Remove Old BottomBar")]
        public static void RemoveBottomBar()
        {
            // Cerca BottomBar nella scena
            var allRoots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in allRoots)
            {
                var bottomBar = root.transform.Find("BottomBar");
                if (bottomBar != null)
                {
                    Object.DestroyImmediate(bottomBar.gameObject);
                    Debug.Log("<color=green>[RecruitSetup]</color> BottomBar deleted from " + root.name);
                    return;
                }

                // Cerca ricorsivamente
                var bars = root.GetComponentsInChildren<Transform>(true);
                foreach (var t in bars)
                {
                    if (t.name.Contains("BottomBar") || t.name.Contains("RecruitBar"))
                    {
                        Object.DestroyImmediate(t.gameObject);
                        Debug.Log("<color=green>[RecruitSetup]</color> " + t.name + " deleted from " + root.name);
                        return;
                    }
                }
            }
            Debug.Log("[RecruitSetup] No BottomBar found in scene.");
        }
    }
}

