using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using WildernessSurvival.UI;

namespace WildernessSurvival.Editor
{
    /// <summary>
    /// Editor tool to automatically add UIButtonSound to all buttons in the scene.
    /// </summary>
    public static class AudioSetupTool
    {
        [MenuItem("Tools/Wilderness/Auto-Setup UI Sounds")]
        public static void AutoSetupUISounds()
        {
            // Find ALL buttons in the scene, including inactive ones
            Button[] allButtons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            int addedCount = 0;

            foreach (Button button in allButtons)
            {
                // Check if it already has UIButtonSound
                if (button.GetComponent<UIButtonSound>() == null)
                {
                    Undo.AddComponent<UIButtonSound>(button.gameObject);
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                Debug.Log($"<color=green>[AudioSetupTool]</color> Added UIButtonSound to {addedCount} button(s).");
                EditorUtility.DisplayDialog(
                    "UI Sound Setup Complete",
                    $"Added UIButtonSound component to {addedCount} button(s).\n\nYou can Undo (Ctrl+Z) if needed.",
                    "OK"
                );
            }
            else
            {
                Debug.Log("<color=yellow>[AudioSetupTool]</color> All buttons already have UIButtonSound attached.");
                EditorUtility.DisplayDialog(
                    "UI Sound Setup",
                    "All buttons already have UIButtonSound attached.\nNo changes made.",
                    "OK"
                );
            }
        }
    }
}
