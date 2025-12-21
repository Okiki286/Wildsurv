#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace WildernessSurvival.Core.Systems.Editor
{
    /// <summary>
    /// Editor utility per creare prefab e setup del sistema Economy Feedback.
    /// Versione Screen Space Overlay - popup sempre orizzontali.
    /// </summary>
    public static class EconomyFeedbackPrefabCreator
    {
        private const string PREFAB_PATH = "Assets/_Core/Prefabs/EconomyFeedback/WorldResourcePopup.prefab";
        private const string PREFAB_FOLDER = "Assets/_Core/Prefabs/EconomyFeedback";

        [MenuItem("Tools/Wilderness/Economy Feedback/Create World Resource Popup Prefab")]
        public static void CreateWorldResourcePopupPrefab()
        {
            // Crea cartelle se non esistono
            if (!AssetDatabase.IsValidFolder("Assets/_Core/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets/_Core", "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder(PREFAB_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets/_Core/Prefabs", "EconomyFeedback");
            }

            // Root con RectTransform (per Screen Space UI)
            GameObject root = new GameObject("WorldResourcePopup");
            RectTransform rootRect = root.AddComponent<RectTransform>();

            // Configura RectTransform per ancoraggio centrale
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(200f, 60f);

            CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();

            // Container per scale animation
            GameObject container = new GameObject("Container");
            container.transform.SetParent(root.transform, false);
            RectTransform containerRect = container.AddComponent<RectTransform>();
            HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
            ContentSizeFitter fitter = container.AddComponent<ContentSizeFitter>();

            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.sizeDelta = Vector2.zero;
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 8f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Icona risorsa (prima del testo)
            GameObject iconObj = new GameObject("ResourceIcon");
            iconObj.transform.SetParent(container.transform, false);
            Image iconImage = iconObj.AddComponent<Image>();
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();

            iconRect.sizeDelta = new Vector2(40f, 40f);
            iconImage.color = Color.white;

            // Testo "+X"
            GameObject textObj = new GameObject("AmountText");
            textObj.transform.SetParent(container.transform, false);
            TextMeshProUGUI amountText = textObj.AddComponent<TextMeshProUGUI>();
            RectTransform textRect = textObj.GetComponent<RectTransform>();

            amountText.text = "+10";
            amountText.fontSize = 36f;
            amountText.fontStyle = FontStyles.Bold;
            amountText.alignment = TextAlignmentOptions.Center;
            amountText.color = Color.white;
            amountText.enableAutoSizing = false;
            amountText.overflowMode = TextOverflowModes.Overflow;

            textRect.sizeDelta = new Vector2(100f, 50f);

            // Aggiungi componente WorldResourcePopup
            WorldResourcePopup popup = root.AddComponent<WorldResourcePopup>();

            // Usa SerializedObject per assegnare i campi privati
            SerializedObject so = new SerializedObject(popup);
            so.FindProperty("amountText").objectReferenceValue = amountText;
            so.FindProperty("iconImage").objectReferenceValue = iconImage;
            so.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            so.FindProperty("container").objectReferenceValue = containerRect;

            // Parametri di default (Screen Space) - tuned per mobile UX
            so.FindProperty("duration").floatValue = 1.8f;
            so.FindProperty("riseDistancePixels").floatValue = 100f;
            so.FindProperty("popInDuration").floatValue = 0.12f;
            so.FindProperty("popPeakScale").floatValue = 1.25f;
            so.FindProperty("fadeStartPercent").floatValue = 0.7f;
            so.FindProperty("gainColor").colorValue = new Color(0.3f, 1f, 0.3f);
            so.FindProperty("spendColor").colorValue = new Color(1f, 0.4f, 0.4f);

            so.ApplyModifiedPropertiesWithoutUndo();

            // Disattiva per pooling
            root.SetActive(false);

            // Salva come prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            Object.DestroyImmediate(root);

            // Seleziona il prefab creato
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);

            Debug.Log($"<color=green>[EconomyFeedback]</color> Prefab created at: {PREFAB_PATH}");
            Debug.Log("<color=yellow>IMPORTANT - Outline Setup:</color>");
            Debug.Log("  1. Open the prefab and select AmountText");
            Debug.Log("  2. In TextMeshPro component, click Material Preset dropdown");
            Debug.Log("  3. Create/assign a preset with Outline enabled (Width ~0.2, Color black)");
            Debug.Log("  4. This ensures outline persists without runtime material modification");
        }

        [MenuItem("Tools/Wilderness/Economy Feedback/Create Economy Feedback System")]
        public static void CreateEconomyFeedbackSystem()
        {
            // Crea nuovo GameObject
            GameObject systemObj = new GameObject("EconomyFeedbackSystem");

            // Aggiungi componente
            EconomyFeedbackSystem system = systemObj.AddComponent<EconomyFeedbackSystem>();

            // Carica prefab se esiste
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            if (prefab != null)
            {
                SerializedObject so = new SerializedObject(system);
                so.FindProperty("popupPrefab").objectReferenceValue = prefab.GetComponent<WorldResourcePopup>();
                so.FindProperty("poolSize").intValue = 32;
                so.FindProperty("worldYOffset").floatValue = 1.8f;
                so.FindProperty("coalesceWindow").floatValue = 0.15f;
                so.FindProperty("sfxCooldown").floatValue = 0.08f;
                so.FindProperty("sfxVolume").floatValue = 0.5f;
                so.FindProperty("hideProductionPopups").boolValue = true;
                so.FindProperty("minDeltaForPopup").intValue = 1;

                // Stacking parameters (pixel-based)
                so.FindProperty("stackWindow").floatValue = 0.3f;
                so.FindProperty("stackVerticalSpacingPixels").floatValue = 50f;
                so.FindProperty("stackHorizontalSpacingPixels").floatValue = 25f;
                so.FindProperty("bucketPrecision").floatValue = 2f;

                // Canvas sorting
                so.FindProperty("canvasSortingOrder").intValue = 5000;

                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning($"Prefab non trovato a {PREFAB_PATH}. Esegui prima 'Create World Resource Popup Prefab'.");
            }

            Selection.activeObject = systemObj;
            EditorGUIUtility.PingObject(systemObj);

            Debug.Log("<color=green>[EconomyFeedback]</color> EconomyFeedbackSystem creato in scena.");
            Debug.Log("<color=yellow>TODO:</color> Assegna AudioClip per gainClip e spendClip.");
        }

        [MenuItem("Tools/Wilderness/Economy Feedback/Setup Complete System")]
        public static void SetupCompleteSystem()
        {
            // 1. Crea prefab
            CreateWorldResourcePopupPrefab();

            // 2. Crea sistema in scena
            CreateEconomyFeedbackSystem();

            Debug.Log("<color=green>[EconomyFeedback]</color> Setup completo! Ricorda di:");
            Debug.Log("  1. Assegnare AudioClip per SFX (gainClip, spendClip)");
            Debug.Log("  2. Verificare che ResourceDisplayUI.Instance sia presente in scena");
            Debug.Log("  3. Il sistema crea automaticamente un Canvas Screen Space Overlay a runtime");
        }
    }
}
#endif
