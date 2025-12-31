using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using WildernessSurvival.Core.Managers;

namespace WildernessSurvival.Core.Editor
{
    /// <summary>
    /// Editor tool per creare la schermata Victory.
    /// Crea un Canvas overlay con pannello oscurato, titolo, messaggio e bottone restart.
    /// </summary>
    public static class VictoryUISetup
    {
        [MenuItem("Tools/Wilderness/UI/Create Victory Screen")]
        public static void CreateVictoryScreen()
        {
            // 1. Controlla se esiste già
            var existing = Object.FindFirstObjectByType<VictoryUI>(FindObjectsInactive.Include);
            if (existing != null)
            {
                Debug.LogWarning("[VictoryUISetup] Victory UI already exists! Selecting existing one.");
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                return;
            }

            // ═══════════════════════════════════════════════════════════
            // 2. CREA CANVAS (Overlay, render sopra tutto)
            // ═══════════════════════════════════════════════════════════

            GameObject canvasObj = new GameObject("VictoryCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; // Sempre sopra tutto

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // ═══════════════════════════════════════════════════════════
            // 3. SFONDO OSCURATO (Panel semi-trasparente)
            // ═══════════════════════════════════════════════════════════

            GameObject bgPanel = new GameObject("DarkBackground");
            bgPanel.transform.SetParent(canvasObj.transform, false);

            RectTransform bgRect = bgPanel.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            Image bgImage = bgPanel.AddComponent<Image>();
            bgImage.color = new Color(0f, 0.1f, 0.05f, 0.85f); // Verde scuro semi-trasparente
            bgImage.raycastTarget = true; // Blocca click sotto

            // ═══════════════════════════════════════════════════════════
            // 4. CONTAINER CENTRALE
            // ═══════════════════════════════════════════════════════════

            GameObject container = new GameObject("Container");
            container.transform.SetParent(bgPanel.transform, false);

            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.sizeDelta = new Vector2(600, 400);

            VerticalLayoutGroup containerLayout = container.AddComponent<VerticalLayoutGroup>();
            containerLayout.padding = new RectOffset(40, 40, 40, 40);
            containerLayout.spacing = 30;
            containerLayout.childAlignment = TextAnchor.MiddleCenter;
            containerLayout.childControlWidth = true;
            containerLayout.childControlHeight = false;
            containerLayout.childForceExpandWidth = true;
            containerLayout.childForceExpandHeight = false;

            // ═══════════════════════════════════════════════════════════
            // 5. TITOLO "VICTORY!"
            // ═══════════════════════════════════════════════════════════

            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(container.transform, false);

            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(500, 80);

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "VICTORY!";
            titleText.fontSize = 72;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(0.2f, 0.9f, 0.3f); // Verde brillante
            titleText.enableAutoSizing = false;

            // ═══════════════════════════════════════════════════════════
            // 6. SOTTOTITOLO / MESSAGGIO
            // ═══════════════════════════════════════════════════════════

            GameObject subtitleObj = new GameObject("SubtitleText");
            subtitleObj.transform.SetParent(container.transform, false);

            RectTransform subtitleRect = subtitleObj.AddComponent<RectTransform>();
            subtitleRect.sizeDelta = new Vector2(500, 40);

            TextMeshProUGUI subtitleText = subtitleObj.AddComponent<TextMeshProUGUI>();
            subtitleText.text = "You survived and thrived!";
            subtitleText.fontSize = 24;
            subtitleText.fontStyle = FontStyles.Italic;
            subtitleText.alignment = TextAlignmentOptions.Center;
            subtitleText.color = new Color(0.8f, 0.9f, 0.8f);

            // ═══════════════════════════════════════════════════════════
            // 7. SPAZIATORE
            // ═══════════════════════════════════════════════════════════

            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(container.transform, false);
            RectTransform spacerRect = spacer.AddComponent<RectTransform>();
            spacerRect.sizeDelta = new Vector2(0, 30);
            LayoutElement spacerLayout = spacer.AddComponent<LayoutElement>();
            spacerLayout.minHeight = 30;

            // ═══════════════════════════════════════════════════════════
            // 8. BOTTONE RESTART
            // ═══════════════════════════════════════════════════════════

            GameObject buttonObj = new GameObject("RestartButton");
            buttonObj.transform.SetParent(container.transform, false);

            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(250, 60);

            Image buttonImg = buttonObj.AddComponent<Image>();
            buttonImg.color = new Color(0.2f, 0.6f, 0.3f); // Verde medio

            Button restartButton = buttonObj.AddComponent<Button>();
            restartButton.targetGraphic = buttonImg;
            ColorBlock colors = restartButton.colors;
            colors.normalColor = new Color(0.2f, 0.6f, 0.3f);
            colors.highlightedColor = new Color(0.3f, 0.7f, 0.4f);
            colors.pressedColor = new Color(0.15f, 0.5f, 0.25f);
            colors.disabledColor = new Color(0.3f, 0.3f, 0.3f);
            restartButton.colors = colors;

            // LayoutElement per dimensione fissa
            LayoutElement buttonLayout = buttonObj.AddComponent<LayoutElement>();
            buttonLayout.minWidth = 250;
            buttonLayout.minHeight = 60;
            buttonLayout.preferredWidth = 250;
            buttonLayout.preferredHeight = 60;

            // Testo del bottone
            GameObject buttonTextObj = new GameObject("ButtonText");
            buttonTextObj.transform.SetParent(buttonObj.transform, false);

            RectTransform buttonTextRect = buttonTextObj.AddComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;

            TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = "🎮 PLAY AGAIN";
            buttonText.fontSize = 28;
            buttonText.fontStyle = FontStyles.Bold;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;
            buttonText.raycastTarget = false;

            // ═══════════════════════════════════════════════════════════
            // 9. AGGIUNGI COMPONENTE VictoryUI
            // ═══════════════════════════════════════════════════════════

            VictoryUI victoryUI = canvasObj.AddComponent<VictoryUI>();

            // Assegna riferimenti via SerializedObject
            SerializedObject so = new SerializedObject(victoryUI);
            so.FindProperty("subtitleText").objectReferenceValue = subtitleText;
            so.FindProperty("restartButton").objectReferenceValue = restartButton;
            so.ApplyModifiedPropertiesWithoutUndo();

            // ═══════════════════════════════════════════════════════════
            // 10. COLLEGA A GAME MANAGER
            // ═══════════════════════════════════════════════════════════

            var gameManager = Object.FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                SerializedObject gmSo = new SerializedObject(gameManager);
                gmSo.FindProperty("victoryUI").objectReferenceValue = canvasObj;
                gmSo.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("<color=green>[VictoryUISetup]</color> ✅ Linked to GameManager!");
            }
            else
            {
                Debug.LogWarning("[VictoryUISetup] GameManager not found in scene. " +
                    "Remember to manually assign the VictoryCanvas to GameManager.victoryUI!");
            }

            // ═══════════════════════════════════════════════════════════
            // 11. DISATTIVA (sarà attivato alla vittoria)
            // ═══════════════════════════════════════════════════════════

            canvasObj.SetActive(false);

            // 12. Seleziona e ping
            Selection.activeGameObject = canvasObj;
            EditorGUIUtility.PingObject(canvasObj);

            Debug.Log("<color=green>[VictoryUISetup]</color> ✅ Victory Screen created!\n" +
                "Structure:\n" +
                "  └─ VictoryCanvas (Canvas + VictoryUI)\n" +
                "      └─ DarkBackground (semi-transparent green)\n" +
                "          └─ Container (VerticalLayoutGroup)\n" +
                "              ├─ TitleText ('VICTORY!')\n" +
                "              ├─ SubtitleText ('You survived and thrived!')\n" +
                "              ├─ Spacer\n" +
                "              └─ RestartButton ('PLAY AGAIN')\n\n" +
                "⚠️ The canvas is INACTIVE by default (it will activate on Victory).\n" +
                "⚠️ Remember to SAVE the scene! (Ctrl+S)");
        }
    }
}
