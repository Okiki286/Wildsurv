#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using WildernessSurvival.Core.Data;
using WildernessSurvival.Core.Systems;

namespace WildernessSurvival.Core.Editor
{
    /// <summary>
    /// Editor tool per setup del sistema Population.
    /// Crea asset EconomyRules e GameObject PopulationSystem.
    /// </summary>
    public static class PopulationSetupTool
    {
        private const string ECONOMY_RULES_PATH = "Assets/_Core/Resources/EconomyRules.asset";
        private const string RESOURCES_FOLDER = "Assets/_Core/Resources";

        [MenuItem("Tools/Wilderness/Population/Create Economy Rules Asset")]
        public static void CreateEconomyRulesAsset()
        {
            // Ensure Resources folder exists
            if (!AssetDatabase.IsValidFolder(RESOURCES_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets/_Core", "Resources");
            }

            // Check if asset already exists
            var existing = AssetDatabase.LoadAssetAtPath<EconomyRules>(ECONOMY_RULES_PATH);
            if (existing != null)
            {
                Debug.Log($"[PopulationSetup] EconomyRules already exists at {ECONOMY_RULES_PATH}");
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                return;
            }

            // Create new asset
            var asset = ScriptableObject.CreateInstance<EconomyRules>();
            AssetDatabase.CreateAsset(asset, ECONOMY_RULES_PATH);
            AssetDatabase.SaveAssets();

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);

            Debug.Log($"<color=green>[PopulationSetup]</color> Created EconomyRules at {ECONOMY_RULES_PATH}");
        }

        [MenuItem("Tools/Wilderness/Population/Create Population System")]
        public static void CreatePopulationSystem()
        {
            // Check if already exists
            var existing = Object.FindFirstObjectByType<PopulationSystem>();
            if (existing != null)
            {
                Debug.Log("[PopulationSetup] PopulationSystem already exists in scene");
                Selection.activeObject = existing.gameObject;
                return;
            }

            // Create new GameObject
            var go = new GameObject("PopulationSystem");
            var system = go.AddComponent<PopulationSystem>();

            // Try to assign GameEvents
            var onDayStarted = AssetDatabase.LoadAssetAtPath<WildernessSurvival.Core.Events.GameEvent>(
                "Assets/_Core/Events/DayNight/OnDayStarted.asset");
            var onNightStarted = AssetDatabase.LoadAssetAtPath<WildernessSurvival.Core.Events.GameEvent>(
                "Assets/_Core/Events/DayNight/OnNightStarted.asset");

            if (onDayStarted != null || onNightStarted != null)
            {
                var so = new SerializedObject(system);

                if (onDayStarted != null)
                    so.FindProperty("onDayStarted").objectReferenceValue = onDayStarted;

                if (onNightStarted != null)
                    so.FindProperty("onNightStarted").objectReferenceValue = onNightStarted;

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            Selection.activeObject = go;
            EditorGUIUtility.PingObject(go);

            Debug.Log("<color=green>[PopulationSetup]</color> Created PopulationSystem in scene. Remember to save the scene!");

            if (onDayStarted == null || onNightStarted == null)
            {
                Debug.LogWarning("[PopulationSetup] Could not find DayNight GameEvent assets. Assign them manually.");
            }
        }

        [MenuItem("Tools/Wilderness/Population/Complete Setup")]
        public static void CompleteSetup()
        {
            CreateEconomyRulesAsset();
            CreatePopulationSystem();

            Debug.Log("<color=green>[PopulationSetup]</color> Complete setup done!");
            Debug.Log("Next steps:");
            Debug.Log("  1. Configure EconomyRules values in Inspector");
            Debug.Log("  2. Add RecruitUI component to a UI Button");
            Debug.Log("  3. Ensure ShelterHome structures have correct Capacity");
        }
    }
}
#endif
