#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace WildernessSurvival.UI.RewardFeel.Editor
{
    /// <summary>
    /// Editor utility per creare il prefab UI_ShardPopup.
    /// Menu: Tools > Wilderness > Create Shard Popup Prefab
    /// </summary>
    public static class ShardPopupPrefabCreator
    {
        private const string PREFAB_PATH = "Assets/_UI/Prefabs/RewardFeel/UI_ShardPopup.prefab";
        private const string PREFAB_FOLDER = "Assets/_UI/Prefabs/RewardFeel";

        [MenuItem("Tools/Wilderness/Create Shard Popup Prefab")]
        public static void CreateShardPopupPrefab()
        {
            // Crea cartella se non esiste
            if (!AssetDatabase.IsValidFolder("Assets/_UI/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets/_UI", "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder(PREFAB_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets/_UI/Prefabs", "RewardFeel");
            }

            // Crea root GameObject
            GameObject root = new GameObject("UI_ShardPopup");
            RectTransform rootRect = root.AddComponent<RectTransform>();
            CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();

            // Configurazione root
            rootRect.sizeDelta = new Vector2(120f, 40f);
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);

            // Container per animazione scale
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
            layout.spacing = 4f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Testo "+X"
            GameObject textObj = new GameObject("AmountText");
            textObj.transform.SetParent(container.transform, false);
            TextMeshProUGUI amountText = textObj.AddComponent<TextMeshProUGUI>();
            RectTransform textRect = textObj.GetComponent<RectTransform>();

            amountText.text = "+10";
            amountText.fontSize = 28f;
            amountText.fontStyle = FontStyles.Bold;
            amountText.alignment = TextAlignmentOptions.Center;
            amountText.color = new Color(1f, 0.85f, 0.2f); // Gold color
            amountText.enableAutoSizing = false;
            amountText.overflowMode = TextOverflowModes.Overflow;

            textRect.sizeDelta = new Vector2(60f, 36f);

            // Icona shard
            GameObject iconObj = new GameObject("ShardIcon");
            iconObj.transform.SetParent(container.transform, false);
            Image shardIcon = iconObj.AddComponent<Image>();
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();

            iconRect.sizeDelta = new Vector2(28f, 28f);
            shardIcon.color = new Color(0.6f, 0.4f, 1f); // Purple placeholder

            // Riordina: testo prima, icona dopo
            textObj.transform.SetAsFirstSibling();

            // Aggiungi componente ShardPopupView
            ShardPopupView popupView = root.AddComponent<ShardPopupView>();

            // Usa SerializedObject per assegnare i campi privati
            SerializedObject so = new SerializedObject(popupView);
            so.FindProperty("amountText").objectReferenceValue = amountText;
            so.FindProperty("shardIcon").objectReferenceValue = shardIcon;
            so.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            so.FindProperty("container").objectReferenceValue = containerRect;

            // Parametri di default
            so.FindProperty("duration").floatValue = 0.9f;
            so.FindProperty("riseDistance").floatValue = 80f;
            so.FindProperty("popInScale").floatValue = 0.5f;
            so.FindProperty("popPeakScale").floatValue = 1.15f;
            so.FindProperty("popInDuration").floatValue = 0.12f;
            so.FindProperty("fadeStartPercent").floatValue = 0.6f;
            so.FindProperty("randomOffsetRange").vector2Value = new Vector2(20f, 10f);

            so.ApplyModifiedPropertiesWithoutUndo();

            // Disattiva per pooling
            root.SetActive(false);

            // Salva come prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            Object.DestroyImmediate(root);

            // Seleziona il prefab creato
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);

            Debug.Log($"<color=green>[ShardPopupPrefabCreator]</color> Prefab created at: {PREFAB_PATH}");
            Debug.Log("<color=yellow>NOTA:</color> Assegna manualmente lo sprite shard all'icona nel prefab!");
        }

        [MenuItem("Tools/Wilderness/Create Reward Feel System Object")]
        public static void CreateRewardFeelSystemObject()
        {
            // Trova Canvas esistente o creane uno
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Nessun Canvas trovato in scena. Crea prima un Canvas.");
                return;
            }

            // Crea RewardFeelSystem container
            GameObject systemObj = new GameObject("RewardFeelSystem");
            systemObj.transform.SetParent(canvas.transform, false);
            RectTransform rect = systemObj.AddComponent<RectTransform>();

            // Full stretch
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Aggiungi componenti
            ShardPopupSpawner spawner = systemObj.AddComponent<ShardPopupSpawner>();
            ShardSfxPlayer sfxPlayer = systemObj.AddComponent<ShardSfxPlayer>();

            // Carica prefab se esiste
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            if (prefab != null)
            {
                SerializedObject so = new SerializedObject(spawner);
                so.FindProperty("popupPrefab").objectReferenceValue = prefab.GetComponent<ShardPopupView>();
                so.FindProperty("poolSize").intValue = 24;
                so.FindProperty("worldYOffset").floatValue = 1.5f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning($"Prefab non trovato a {PREFAB_PATH}. Esegui prima 'Create Shard Popup Prefab'.");
            }

            Selection.activeObject = systemObj;
            EditorGUIUtility.PingObject(systemObj);

            Debug.Log("<color=green>[RewardFeel]</color> RewardFeelSystem creato sotto Canvas.");
            Debug.Log("<color=yellow>TODO:</color> Assegna AudioClip per SFX shards, e aggiungi ShardHUDPulse al container shards nell'HUD.");
        }
    }
}
#endif
