using UnityEngine;
using UnityEditor;
using System.IO;

namespace WildernessSurvival.Editor
{
    /// <summary>
    /// Crea automaticamente la struttura cartelle del progetto.
    /// Usa: Menu Unity → Tools → Setup Project Structure
    /// </summary>
    public static class ProjectSetup
    {
        [MenuItem("Tools/Wilderness Survival/Setup Project Structure")]
        public static void CreateFolderStructure()
        {
            string[] folders = new string[]
            {
                // Core Systems
                "Assets/_Core",
                "Assets/_Core/Systems",
                "Assets/_Core/Managers",
                "Assets/_Core/Events",
                "Assets/_Core/Utils",
                "Assets/_Core/StateMachines",
                
                // Gameplay
                "Assets/_Gameplay",
                "Assets/_Gameplay/Workers",
                "Assets/_Gameplay/Structures",
                "Assets/_Gameplay/Enemies",
                "Assets/_Gameplay/Combat",
                "Assets/_Gameplay/Resources",
                "Assets/_Gameplay/TechTree",
                
                // Content (Data-Driven)
                "Assets/_Content",
                "Assets/_Content/Data",
                "Assets/_Content/Data/Resources",
                "Assets/_Content/Data/Structures",
                "Assets/_Content/Data/Enemies",
                "Assets/_Content/Data/Waves",
                "Assets/_Content/Data/Tech",
                "Assets/_Content/Prefabs",
                "Assets/_Content/Prefabs/Structures",
                "Assets/_Content/Prefabs/Enemies",
                "Assets/_Content/Prefabs/Workers",
                "Assets/_Content/Prefabs/Projectiles",
                "Assets/_Content/Prefabs/VFX",
                "Assets/_Content/Materials",
                "Assets/_Content/Animations",
                
                // UI
                "Assets/_UI",
                "Assets/_UI/HUD",
                "Assets/_UI/Panels",
                "Assets/_UI/Prefabs",
                
                // Scenes
                "Assets/Scenes",
                
                // Third Party
                "Assets/ThirdParty",
                
                // Editor Extensions
                "Assets/Editor"
            };

            foreach (string folder in folders)
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                    Debug.Log($"[ProjectSetup] Creata cartella: {folder}");
                }
            }

            AssetDatabase.Refresh();
            Debug.Log("<color=green>[ProjectSetup] Struttura progetto creata con successo!</color>");
        }

        [MenuItem("Tools/Wilderness Survival/Open Documentation")]
        public static void OpenDocumentation()
        {
            Debug.Log("=== WILDERNESS SURVIVAL CAMP - QUICK START ===\n" +
                "1. Crea ScriptableObjects in _Content/Data/\n" +
                "2. GameManager gestisce Day/Night cycle\n" +
                "3. ResourceSystem gestisce tutte le risorse\n" +
                "4. Usa GameEvents per comunicazione tra sistemi\n" +
                "===============================================");
        }
    }
}
