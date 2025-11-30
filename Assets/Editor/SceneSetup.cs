using UnityEngine;
using UnityEngine.Rendering.Universal;
using WildernessSurvival.Core.Managers;
using WildernessSurvival.Core.Systems;
using WildernessSurvival.Gameplay.Resources;
using WildernessSurvival.Utils;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WildernessSurvival.Editor
{
    /// <summary>
    /// Configura automaticamente la scena principale con tutti i sistemi.
    /// </summary>
    public static class SceneSetup
    {
        #if UNITY_EDITOR
        [MenuItem("Tools/Wilderness Survival/Setup Game Scene")]
        public static void SetupGameScene()
        {
            Debug.Log("<color=cyan>=== SETUP SCENA GIOCO ===</color>");

            // 1. Crea container principale
            GameObject gameRoot = CreateOrFind("--- GAME SYSTEMS ---");
            
            // 2. GameManager
            GameObject gmObj = CreateOrFind("GameManager", gameRoot.transform);
            if (!gmObj.TryGetComponent<GameManager>(out _))
            {
                gmObj.AddComponent<GameManager>();
                Debug.Log("[Setup] GameManager aggiunto");
            }

            // 3. DayNightSystem
            GameObject dnObj = CreateOrFind("DayNightSystem", gameRoot.transform);
            if (!dnObj.TryGetComponent<DayNightSystem>(out _))
            {
                dnObj.AddComponent<DayNightSystem>();
                Debug.Log("[Setup] DayNightSystem aggiunto");
            }

            // 4. ResourceSystem
            GameObject rsObj = CreateOrFind("ResourceSystem", gameRoot.transform);
            if (!rsObj.TryGetComponent<ResourceSystem>(out _))
            {
                rsObj.AddComponent<ResourceSystem>();
                Debug.Log("[Setup] ResourceSystem aggiunto");
            }

            // 5. LightingManager
            GameObject lmObj = CreateOrFind("LightingManager", gameRoot.transform);
            if (!lmObj.TryGetComponent<DayNightLightingManager>(out _))
            {
                lmObj.AddComponent<DayNightLightingManager>();
                Debug.Log("[Setup] DayNightLightingManager aggiunto");
            }

            // 6. Setup Camera
            SetupCamera();

            // 7. Setup Lighting
            SetupLighting();

            // 8. Placeholder Generator
            GameObject placeholderObj = CreateOrFind("PlaceholderGenerator", gameRoot.transform);
            if (!placeholderObj.TryGetComponent<PlaceholderGenerator>(out _))
            {
                placeholderObj.AddComponent<PlaceholderGenerator>();
                Debug.Log("[Setup] PlaceholderGenerator aggiunto");
            }

            // 9. Event Bus (container per eventi)
            CreateOrFind("--- EVENTS ---");

            Debug.Log("<color=green>=== SETUP COMPLETATO ===</color>");
            Debug.Log("Prossimi passi:\n" +
                "1. Crea ScriptableObjects risorse in _Content/Data/Resources/\n" +
                "2. Assegna risorse al ResourceSystem\n" +
                "3. Crea eventi in _Content/Data/Events/\n" +
                "4. Premi Play per testare!");
        }

        [MenuItem("Tools/Wilderness Survival/Create Resource ScriptableObjects")]
        public static void CreateResourceSOs()
        {
            string path = "Assets/_Content/Data/Resources";
            
            // Assicurati che la cartella esista
            if (!AssetDatabase.IsValidFolder(path))
            {
                System.IO.Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }

            // Warmwood
            CreateResourceSO(path, "Warmwood", "warmwood", 
                "Legna speciale che brucia più a lungo", 
                1, ResourceRarity.Common, 10, 8f, 25, 50);

            // Food
            CreateResourceSO(path, "Food", "food",
                "Cibo per nutrire i villager",
                1, ResourceRarity.Common, 5, 5f, 30, 60);

            // Shards
            CreateResourceSO(path, "Shards", "shards",
                "Frammenti cristallini di potere",
                1, ResourceRarity.Uncommon, 25, 2f, 20, 40);

            AssetDatabase.Refresh();
            Debug.Log("<color=green>[Setup] ScriptableObjects risorse creati in " + path + "</color>");
        }

        [MenuItem("Tools/Wilderness Survival/Create Game Events")]
        public static void CreateGameEvents()
        {
            string path = "Assets/_Content/Data/Events";

            if (!AssetDatabase.IsValidFolder(path))
            {
                System.IO.Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }

            // Eventi base
            CreateGameEventSO(path, "OnDayStarted", "Evento quando inizia il giorno");
            CreateGameEventSO(path, "OnNightStarted", "Evento quando inizia la notte");
            CreateGameEventSO(path, "OnDayEnding", "Evento 30 sec prima della fine del giorno");
            CreateGameEventSO(path, "OnNightEnding", "Evento 30 sec prima della fine della notte");
            CreateGameEventSO(path, "OnGameInitialized", "Evento quando il gioco è inizializzato");
            CreateGameEventSO(path, "OnGamePaused", "Evento quando il gioco va in pausa");
            CreateGameEventSO(path, "OnGameResumed", "Evento quando il gioco riprende");
            CreateGameEventSO(path, "OnGameOver", "Evento Game Over");

            // Eventi tipizzati
            CreateIntEventSO(path, "OnDayNumberChanged", "Passa il numero del giorno corrente");
            CreateFloatEventSO(path, "OnTimeProgressChanged", "Progresso 0-1 della fase corrente");
            CreateStringEventSO(path, "OnResourceChanged", "Passa l'ID della risorsa cambiata");
            CreateStringEventSO(path, "OnResourceInsufficient", "Passa l'ID della risorsa insufficiente");

            AssetDatabase.Refresh();
            Debug.Log("<color=green>[Setup] GameEvents creati in " + path + "</color>");
        }

        // ============================================
        // HELPER METHODS
        // ============================================

        private static GameObject CreateOrFind(string name, Transform parent = null)
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null)
            {
                obj = new GameObject(name);
                if (parent != null)
                {
                    obj.transform.SetParent(parent);
                }
            }
            return obj;
        }

        private static void SetupCamera()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }

            // Aggiungi controller isometrico
            if (!mainCam.TryGetComponent<IsometricCameraController>(out _))
            {
                mainCam.gameObject.AddComponent<IsometricCameraController>();
            }

            // Posizione iniziale
            mainCam.transform.position = new Vector3(0, 15, -15);
            mainCam.transform.rotation = Quaternion.Euler(45, 0, 0);

            // Ortografica per isometrico
            mainCam.orthographic = true;
            mainCam.orthographicSize = 10f;
            mainCam.nearClipPlane = 0.1f;
            mainCam.farClipPlane = 100f;

            // URP Camera Data
            if (!mainCam.TryGetComponent<UniversalAdditionalCameraData>(out _))
            {
                mainCam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }

            Debug.Log("[Setup] Camera configurata");
        }

        private static void SetupLighting()
        {
            // Trova o crea luce direzionale
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            Light directional = null;
            
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    directional = light;
                    break;
                }
            }

            if (directional == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                directional = lightObj.AddComponent<Light>();
                directional.type = LightType.Directional;
            }

            // Configura
            directional.transform.rotation = Quaternion.Euler(50, -30, 0);
            directional.color = new Color(1f, 0.95f, 0.8f);
            directional.intensity = 1f;
            directional.shadows = LightShadows.Soft;

            // Collega al LightingManager
            DayNightLightingManager lm = Object.FindFirstObjectByType<DayNightLightingManager>();
            if (lm != null)
            {
                SerializedObject so = new SerializedObject(lm);
                so.FindProperty("mainLight").objectReferenceValue = directional;
                so.ApplyModifiedProperties();
            }

            Debug.Log("[Setup] Lighting configurato");
        }

        private static void CreateResourceSO(string path, string name, string id, 
            string desc, int tier, ResourceRarity rarity, int value, 
            float productionPerMin, int workerBonus, int heroBonus)
        {
            string fullPath = $"{path}/Resource_{name}.asset";
            
            if (AssetDatabase.LoadAssetAtPath<ResourceData>(fullPath) != null)
            {
                Debug.Log($"[Setup] {name} già esistente, skip");
                return;
            }

            ResourceData resource = ScriptableObject.CreateInstance<ResourceData>();
            
            // Usa reflection o SerializedObject per impostare i campi privati
            SerializedObject so = new SerializedObject(resource);
            so.FindProperty("resourceId").stringValue = id;
            so.FindProperty("displayName").stringValue = name;
            so.FindProperty("description").stringValue = desc;
            so.FindProperty("tier").intValue = tier;
            so.FindProperty("rarity").enumValueIndex = (int)rarity;
            so.FindProperty("baseValue").intValue = value;
            so.FindProperty("baseProductionPerMinute").floatValue = productionPerMin;
            so.FindProperty("workerBonusPercent").intValue = workerBonus;
            so.FindProperty("heroBonusPercent").intValue = heroBonus;
            so.ApplyModifiedProperties();

            AssetDatabase.CreateAsset(resource, fullPath);
            Debug.Log($"[Setup] Creato: {name}");
        }

        private static void CreateGameEventSO(string path, string name, string desc)
        {
            string fullPath = $"{path}/{name}.asset";
            if (AssetDatabase.LoadAssetAtPath<Core.Events.GameEvent>(fullPath) != null) return;

            var evt = ScriptableObject.CreateInstance<Core.Events.GameEvent>();
            SerializedObject so = new SerializedObject(evt);
            so.FindProperty("description").stringValue = desc;
            so.ApplyModifiedProperties();

            AssetDatabase.CreateAsset(evt, fullPath);
        }

        private static void CreateIntEventSO(string path, string name, string desc)
        {
            string fullPath = $"{path}/{name}.asset";
            if (AssetDatabase.LoadAssetAtPath<Core.Events.IntEvent>(fullPath) != null) return;

            var evt = ScriptableObject.CreateInstance<Core.Events.IntEvent>();
            SerializedObject so = new SerializedObject(evt);
            so.FindProperty("description").stringValue = desc;
            so.ApplyModifiedProperties();

            AssetDatabase.CreateAsset(evt, fullPath);
        }

        private static void CreateFloatEventSO(string path, string name, string desc)
        {
            string fullPath = $"{path}/{name}.asset";
            if (AssetDatabase.LoadAssetAtPath<Core.Events.FloatEvent>(fullPath) != null) return;

            var evt = ScriptableObject.CreateInstance<Core.Events.FloatEvent>();
            AssetDatabase.CreateAsset(evt, fullPath);
        }

        private static void CreateStringEventSO(string path, string name, string desc)
        {
            string fullPath = $"{path}/{name}.asset";
            if (AssetDatabase.LoadAssetAtPath<Core.Events.StringEvent>(fullPath) != null) return;

            var evt = ScriptableObject.CreateInstance<Core.Events.StringEvent>();
            AssetDatabase.CreateAsset(evt, fullPath);
        }
        #endif
    }
}
