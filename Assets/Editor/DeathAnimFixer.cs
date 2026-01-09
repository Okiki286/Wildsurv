using UnityEditor;
using UnityEngine;

namespace WildernessSurvival.Editor
{
    public class DeathAnimFixer
    {
        [MenuItem("Wilderness/Fix Death Animations")]
        public static void FixDeathAnimations()
        {
            string[] animPaths = {
                "Assets/KayKit/Characters/Animations/Animations/Rig_Medium/General/Death_A.anim",
                "Assets/KayKit/Characters/Animations/Animations/Rig_Medium/General/Death_A_Pose.anim",
                "Assets/KayKit/Characters/Animations/Animations/Rig_Large/General/Death_A.anim",
                "Assets/KayKit/Characters/Animations/Animations/Rig_Large/General/Death_A_Pose.anim"
            };

            int fixedCount = 0;

            foreach (string path in animPaths)
            {
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip == null) continue;

                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                
                // Death_A (la caduta) -> Non deve loopare
                // Death_A_Pose (a terra) -> Deve loopare
                bool shouldLoop = path.Contains("Pose");
                
                if (settings.loopTime != shouldLoop)
                {
                    settings.loopTime = shouldLoop;
                    AnimationUtility.SetAnimationClipSettings(clip, settings);
                    EditorUtility.SetDirty(clip);
                    Debug.Log($"[DeathAnimFixer] Set loopTime={shouldLoop} for {clip.name}");
                    fixedCount++;
                }
            }

            if (fixedCount > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"[DeathAnimFixer] Successfully fixed {fixedCount} animation clips.");
            }
            else
            {
                Debug.Log("[DeathAnimFixer] All clips already correctly configured.");
            }
        }
    }
}
