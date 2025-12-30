#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using WildernessSurvival.Core.Systems;
using WildernessSurvival.Core.Events;

namespace WildernessSurvival.Editor
{
    /// <summary>
    /// Editor utility to wire AutoSaveSystem to DayNight GameEvents.
    /// Menu: Tools/Wilderness/Wire AutoSave Events
    /// </summary>
    public static class AutoSaveEventWirer
    {
        [MenuItem("Tools/Wilderness/Wire AutoSave Events")]
        private static void WireAutoSaveEvents()
        {
            Debug.Log("<color=cyan>[AutoSaveEventWirer]</color> === Starting Event Wiring ===");

            // Find AutoSaveSystem in scene
            AutoSaveSystem autoSaveSystem = Object.FindFirstObjectByType<AutoSaveSystem>();
            if (autoSaveSystem == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "AutoSaveSystem not found in scene!\n\n" +
                    "Please ensure AutoSaveSystem GameObject exists in the scene.",
                    "OK");
                Debug.LogError("<color=red>[AutoSaveEventWirer]</color> AutoSaveSystem not found!");
                return;
            }

            Debug.Log($"<color=green>[AutoSaveEventWirer]</color> Found AutoSaveSystem: {autoSaveSystem.gameObject.name}");

            // Find OnDayStarted event asset
            string[] dayStartedGuids = AssetDatabase.FindAssets("OnDayStarted t:GameEvent");
            if (dayStartedGuids.Length == 0)
            {
                EditorUtility.DisplayDialog("Error",
                    "OnDayStarted GameEvent not found!\n\n" +
                    "Expected path: Assets/_Core/Events/DayNight/OnDayStarted.asset",
                    "OK");
                Debug.LogError("<color=red>[AutoSaveEventWirer]</color> OnDayStarted GameEvent not found!");
                return;
            }

            string onDayStartedPath = AssetDatabase.GUIDToAssetPath(dayStartedGuids[0]);
            GameEvent onDayStartedEvent = AssetDatabase.LoadAssetAtPath<GameEvent>(onDayStartedPath);

            Debug.Log($"<color=green>[AutoSaveEventWirer]</color> Found OnDayStarted event: {onDayStartedPath}");

            // Find OnNightStarted event asset
            string[] nightStartedGuids = AssetDatabase.FindAssets("OnNightStarted t:GameEvent");
            GameEvent onNightStartedEvent = null;

            if (nightStartedGuids.Length > 0)
            {
                string onNightStartedPath = AssetDatabase.GUIDToAssetPath(nightStartedGuids[0]);
                onNightStartedEvent = AssetDatabase.LoadAssetAtPath<GameEvent>(onNightStartedPath);
                Debug.Log($"<color=green>[AutoSaveEventWirer]</color> Found OnNightStarted event: {onNightStartedPath}");
            }
            else
            {
                Debug.LogWarning("<color=yellow>[AutoSaveEventWirer]</color> OnNightStarted GameEvent not found (optional)");
            }

            // Create GameEventListener component for OnDayStarted
            GameEventListener dayStartedListener = autoSaveSystem.GetComponent<GameEventListener>();
            if (dayStartedListener == null)
            {
                dayStartedListener = autoSaveSystem.gameObject.AddComponent<GameEventListener>();
                Debug.Log("<color=green>[AutoSaveEventWirer]</color> Added GameEventListener component");
            }

            // Wire OnDayStarted event using SerializedObject
            SerializedObject so = new SerializedObject(dayStartedListener);
            so.FindProperty("gameEvent").objectReferenceValue = onDayStartedEvent;
            so.ApplyModifiedProperties();

            // Need to use reflection to access the UnityEvent since it's private
            // Alternative: Use SerializedProperty to add the persistent listener
            so.Update();

            var responseProperty = so.FindProperty("response");

            // Clear existing listeners
            int listenerCount = responseProperty.FindPropertyRelative("m_PersistentCalls.m_Calls").arraySize;
            for (int i = listenerCount - 1; i >= 0; i--)
            {
                responseProperty.FindPropertyRelative("m_PersistentCalls.m_Calls").DeleteArrayElementAtIndex(i);
            }

            // Add new persistent call
            var callsArray = responseProperty.FindPropertyRelative("m_PersistentCalls.m_Calls");
            callsArray.InsertArrayElementAtIndex(0);
            var call = callsArray.GetArrayElementAtIndex(0);

            call.FindPropertyRelative("m_Target").objectReferenceValue = autoSaveSystem;
            call.FindPropertyRelative("m_MethodName").stringValue = "OnDayStarted";
            call.FindPropertyRelative("m_Mode").enumValueIndex = 1; // UnityEventCallState.RuntimeOnly
            call.FindPropertyRelative("m_CallState").enumValueIndex = 2; // PersistentListenerMode.Void

            so.ApplyModifiedProperties();

            Debug.Log("<color=green>[AutoSaveEventWirer]</color> ✅ Wired OnDayStarted → AutoSaveSystem.OnDayStarted()");

            // Optional: Wire OnNightStarted if it exists
            if (onNightStartedEvent != null)
            {
                // For night events, we need a second GameEventListener
                // This is a limitation - Unity doesn't support multiple GameEventListeners easily
                // Alternative: Use GameEvent's built-in listener list
                Debug.LogWarning("<color=yellow>[AutoSaveEventWirer]</color> OnNightStarted wiring requires manual setup in GameEvent Inspector");
            }

            // Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            Debug.Log("<color=cyan>[AutoSaveEventWirer]</color> === Event Wiring Complete ===");

            // Success dialog
            EditorUtility.DisplayDialog("Success!",
                "✅ AutoSave Events Wired Successfully!\n\n" +
                $"OnDayStarted → AutoSaveSystem.OnDayStarted()\n\n" +
                "The auto-save will now trigger when a new day starts.\n\n" +
                "Save the scene to keep changes.",
                "OK");

            // Select AutoSaveSystem in hierarchy
            Selection.activeGameObject = autoSaveSystem.gameObject;
        }
    }
}
#endif
