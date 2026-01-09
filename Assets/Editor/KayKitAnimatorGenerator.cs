using UnityEngine;
using UnityEditor;
using UnityEditor.Animations; // Necessario per manipolare i Controller via codice

namespace WildernessSurvival.EditorTools
{
    public class KayKitAnimatorGenerator : EditorWindow
    {
        private string controllerName = "KayKit_Worker_Controller";
        private string targetFolder = "Assets/_Gameplay/Workers/Animations"; // Dove salvare il file

        [MenuItem("Tools/KayKit Auto-Setup/Generate Animator Controller")]
        public static void ShowWindow()
        {
            GetWindow<KayKitAnimatorGenerator>("Animator Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("Generatore Automatico Animator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            controllerName = EditorGUILayout.TextField("Nome Controller", controllerName);
            targetFolder = EditorGUILayout.TextField("Cartella Output", targetFolder);

            GUILayout.Space(20);

            if (GUILayout.Button("⚡ Genera Controller & Blend Tree", GUILayout.Height(40)))
            {
                CreateController();
            }
        }

        private void CreateController()
        {
            // 1. Assicurati che la cartella esista
            if (!AssetDatabase.IsValidFolder(targetFolder))
            {
                string guid = AssetDatabase.CreateFolder("Assets/_Gameplay/Workers", "Animations");
                targetFolder = AssetDatabase.GUIDToAssetPath(guid);
            }

            string path = $"{targetFolder}/{controllerName}.controller";

            // 2. Crea il Controller
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);

            // 3. Aggiungi i Parametri (ESATTAMENTE come da tuo report)
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsWorking", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
            controller.AddParameter("HasWeapon", AnimatorControllerParameterType.Bool);

            Debug.Log("✅ Parametri aggiunti (Speed, IsMoving, ecc.)");

            // 4. Trova le clip KayKit nel progetto
            AnimationClip idleClip = FindClip("Idle_A");
            AnimationClip walkClip = FindClip("Walking_A");
            AnimationClip runClip = FindClip("Running_A");
            // Opzionale: Cerca una clip di lavoro generica
            AnimationClip chopClip = FindClip("Chop") ?? FindClip("Attack");

            if (idleClip == null || walkClip == null || runClip == null)
            {
                Debug.LogError("❌ Impossibile trovare alcune clip (Idle_A, Walking_A, Running_A). Assicurati di aver importato KayKit!");
                return;
            }

            // 5. Crea il Blend Tree "Locomotion"
            AnimatorStateMachine rootSm = controller.layers[0].stateMachine;
            AnimatorState blendTreeState = rootSm.AddState("Locomotion");
            blendTreeState.motion = CreateLocomotionBlendTree(controller, idleClip, walkClip, runClip);

            // 6. Crea lo stato "Working" (Base)
            if (chopClip != null)
            {
                AnimatorState workState = rootSm.AddState("Working");
                workState.motion = chopClip;

                // Transizione Semplice: Se IsWorking è true -> Vai a Working
                var toWork = blendTreeState.AddTransition(workState);
                toWork.AddCondition(AnimatorConditionMode.If, 0, "IsWorking");
                toWork.duration = 0.1f;

                // Transizione Ritorno: Se IsWorking è false -> Torna a Locomotion
                var toLocomotion = workState.AddTransition(blendTreeState);
                toLocomotion.AddCondition(AnimatorConditionMode.IfNot, 0, "IsWorking");
                toLocomotion.duration = 0.25f;
            }

            Debug.Log($"<color=green>✅ SUCCESS! Animator creato in: {path}</color>");
            Selection.activeObject = controller; // Evidenzialo nel progetto
        }

        private BlendTree CreateLocomotionBlendTree(AnimatorController controller, AnimationClip idle, AnimationClip walk, AnimationClip run)
        {
            BlendTree tree;
            controller.CreateBlendTreeInController("Locomotion_Tree", out tree);

            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = "Speed"; // Collega al parametro Float creato prima
            tree.useAutomaticThresholds = false; // Manuale come richiesto

            // Aggiungi i nodi con le soglie specificate nel report
            tree.AddChild(idle, 0.0f);   // Fermo
            tree.AddChild(walk, 1.5f);   // Passo normale
            tree.AddChild(run, 3.5f);    // Corsa (NavMesh Speed)

            return tree;
        }

        private AnimationClip FindClip(string partialName)
        {
            // Cerca asset di tipo AnimationClip che contengano il nome
            string[] guids = AssetDatabase.FindAssets($"t:AnimationClip {partialName}");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                // Filtro extra per essere sicuri che sia KayKit (opzionale)
                if (path.Contains("KayKit") || path.Contains("Adventurer"))
                {
                    return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                }
            }

            // Fallback: ritorna il primo trovato se non specifico KayKit
            if (guids.Length > 0)
                return AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guids[0]));

            return null;
        }
    }
}