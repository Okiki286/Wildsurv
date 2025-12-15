using UnityEngine;
using UnityEditor;

namespace WildernessSurvival.Gameplay.Core.Editor
{
    /// <summary>
    /// Editor tool per creare il prefab WaystoneBeacon con modello placeholder.
    /// </summary>
    public static class WaystoneBeaconPrefabCreator
    {
        private const string PREFAB_PATH = "Assets/_Gameplay/Structures/Waystone/WaystoneBeacon.prefab";
        
        [MenuItem("Tools/Wilderness/Create WaystoneBeacon Prefab")]
        public static void CreateWaystoneBeaconPrefab()
        {
            // Verifica se esiste già
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            if (existingPrefab != null)
            {
                if (!EditorUtility.DisplayDialog("Prefab Esistente", 
                    "WaystoneBeacon.prefab esiste già. Vuoi sovrascriverlo?", 
                    "Sovrascrivi", "Annulla"))
                {
                    return;
                }
            }

            // Crea root GameObject
            GameObject beacon = new GameObject("WaystoneBeacon");
            
            // Imposta tag
            try
            {
                beacon.tag = "Core";
            }
            catch
            {
                Debug.LogWarning("[WaystoneBeaconPrefabCreator] Tag 'Core' non trovato. " +
                    "Aggiungilo in Edit → Project Settings → Tags and Layers");
            }

            // Aggiungi collider principale
            CapsuleCollider collider = beacon.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 2f, 0f);
            collider.radius = 1f;
            collider.height = 4f;

            // === MONOLITH (base di pietra) ===
            GameObject monolith = GameObject.CreatePrimitive(PrimitiveType.Cube);
            monolith.name = "Monolith";
            monolith.transform.SetParent(beacon.transform);
            monolith.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            monolith.transform.localScale = new Vector3(0.8f, 3f, 0.8f);
            // Rimuovi collider primitivo (usiamo quello del parent)
            Object.DestroyImmediate(monolith.GetComponent<Collider>());

            // === CRYSTAL (cristallo in cima) ===
            GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crystal.name = "Crystal";
            crystal.transform.SetParent(beacon.transform);
            crystal.transform.localPosition = new Vector3(0f, 3.5f, 0f);
            crystal.transform.localScale = new Vector3(0.6f, 0.8f, 0.6f);
            Object.DestroyImmediate(crystal.GetComponent<Collider>());

            // === RUNE RING (anello decorativo) ===
            GameObject runeRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            runeRing.name = "RuneRing";
            runeRing.transform.SetParent(beacon.transform);
            runeRing.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            runeRing.transform.localScale = new Vector3(2f, 0.1f, 2f);
            Object.DestroyImmediate(runeRing.GetComponent<Collider>());

            // === LUCE ===
            GameObject lightObj = new GameObject("BeaconLight");
            lightObj.transform.SetParent(beacon.transform);
            lightObj.transform.localPosition = new Vector3(0f, 4f, 0f);
            Light pointLight = lightObj.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = new Color(0.5f, 0.8f, 1f); // Cyan chiaro
            pointLight.intensity = 2f;
            pointLight.range = 10f;

            // Aggiungi controller
            beacon.AddComponent<WaystoneBeaconController>();

            // Assicurati che la cartella esista
            string folderPath = System.IO.Path.GetDirectoryName(PREFAB_PATH);
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                System.IO.Directory.CreateDirectory(folderPath);
                AssetDatabase.Refresh();
            }

            // Salva come prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(beacon, PREFAB_PATH);
            
            // Pulisci l'oggetto temporaneo dalla scena
            Object.DestroyImmediate(beacon);
            
            // Seleziona il prefab creato
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);

            Debug.Log($"<color=green>[WaystoneBeaconPrefabCreator]</color> ✓ Prefab creato: {PREFAB_PATH}");
            EditorUtility.DisplayDialog("Successo!", 
                $"WaystoneBeacon prefab creato:\n{PREFAB_PATH}\n\nTrascinalo nella scena per usarlo.", 
                "OK");
        }

        [MenuItem("Tools/Wilderness/Add WaystoneBeacon to Scene")]
        public static void AddWaystoneBeaconToScene()
        {
            // Cerca prefab esistente
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            
            if (prefab == null)
            {
                if (EditorUtility.DisplayDialog("Prefab Non Trovato",
                    "Il prefab WaystoneBeacon non esiste ancora. Vuoi crearlo ora?",
                    "Crea Prefab", "Annulla"))
                {
                    CreateWaystoneBeaconPrefab();
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
                }
                else
                {
                    return;
                }
            }

            // Verifica se esiste già nella scena
            WaystoneBeaconController existing = Object.FindFirstObjectByType<WaystoneBeaconController>();
            if (existing != null)
            {
                EditorUtility.DisplayDialog("Beacon Esistente",
                    $"Un WaystoneBeacon esiste già nella scena: {existing.gameObject.name}",
                    "OK");
                Selection.activeObject = existing.gameObject;
                return;
            }

            // Istanzia nella scena
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = Vector3.zero;
            
            Selection.activeObject = instance;
            Undo.RegisterCreatedObjectUndo(instance, "Add WaystoneBeacon");

            Debug.Log("<color=green>[WaystoneBeaconPrefabCreator]</color> ✓ WaystoneBeacon aggiunto alla scena.");
        }
    }
}
