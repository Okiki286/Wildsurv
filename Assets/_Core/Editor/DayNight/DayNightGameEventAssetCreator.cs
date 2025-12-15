#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using WildernessSurvival.Core.Events;
using System.IO;

namespace WildernessSurvival.Core.Editor.DayNight
{
    /// <summary>
    /// Editor utility per creare i GameEvent assets per il ciclo Day/Night.
    /// </summary>
    public static class DayNightGameEventAssetCreator
    {
        private const string EVENTS_FOLDER = "Assets/_Core/Events/DayNight";
        
        private static readonly string[] EVENT_NAMES = new string[]
        {
            "OnDayStarted",
            "OnDayEnding",
            "OnNightStarted",
            "OnNightEnding"
        };

        [MenuItem("Tools/Wilderness Survival/DayNight/Create DayNight GameEvents")]
        public static void CreateDayNightGameEvents()
        {
            // 1. Verifica/crea la cartella
            EnsureFolderExists(EVENTS_FOLDER);
            
            int createdCount = 0;
            int existingCount = 0;
            Object lastCreated = null;

            // 2. Crea gli asset mancanti
            foreach (string eventName in EVENT_NAMES)
            {
                string assetPath = $"{EVENTS_FOLDER}/{eventName}.asset";
                
                // Controlla se l'asset esiste già
                GameEvent existingAsset = AssetDatabase.LoadAssetAtPath<GameEvent>(assetPath);
                
                if (existingAsset != null)
                {
                    Debug.Log($"<color=cyan>[DayNight]</color> Asset già esistente: <b>{assetPath}</b>");
                    existingCount++;
                    lastCreated = existingAsset;
                }
                else
                {
                    // Crea nuovo GameEvent
                    GameEvent newEvent = ScriptableObject.CreateInstance<GameEvent>();
                    AssetDatabase.CreateAsset(newEvent, assetPath);
                    
                    Debug.Log($"<color=green>[DayNight]</color> ✓ Creato: <b>{assetPath}</b>");
                    createdCount++;
                    lastCreated = newEvent;
                }
            }

            // 3. Salva tutti gli asset
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 4. Log riassuntivo
            Debug.Log($"<color=yellow>[DayNight GameEvents]</color> Completato! " +
                      $"Creati: <b>{createdCount}</b>, Già esistenti: <b>{existingCount}</b>");

            // 5. Seleziona la cartella o l'ultimo asset creato nel Project
            Object folderObj = AssetDatabase.LoadAssetAtPath<Object>(EVENTS_FOLDER);
            if (folderObj != null)
            {
                Selection.activeObject = folderObj;
                EditorGUIUtility.PingObject(folderObj);
            }
            else if (lastCreated != null)
            {
                Selection.activeObject = lastCreated;
                EditorGUIUtility.PingObject(lastCreated);
            }
        }

        private static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                Debug.Log($"<color=cyan>[DayNight]</color> Cartella esistente: <b>{folderPath}</b>");
                return;
            }

            // Crea le cartelle in modo ricorsivo
            string[] folders = folderPath.Split('/');
            string currentPath = folders[0]; // "Assets"

            for (int i = 1; i < folders.Length; i++)
            {
                string nextPath = $"{currentPath}/{folders[i]}";
                
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                    Debug.Log($"<color=green>[DayNight]</color> ✓ Creata cartella: <b>{nextPath}</b>");
                }
                
                currentPath = nextPath;
            }
        }
    }
}
#endif
