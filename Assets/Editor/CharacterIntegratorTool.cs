#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.IO;

namespace WildernessSurvival.EditorTools
{
    /// <summary>
    /// Editor Tool per sostituire la mesh visuale di un Worker e generare automaticamente l'Animator Controller.
    /// </summary>
    public class CharacterIntegratorTool : OdinEditorWindow
    {
        private const string GENERATED_ANIMATIONS_PATH = "Assets/_Gameplay/Workers/Animations/Generated";
        
        [MenuItem("Tools/Wilderness Survival/Character Integrator")]
        private static void OpenWindow()
        {
            var window = GetWindow<CharacterIntegratorTool>();
            window.titleContent = new GUIContent("Character Integrator");
            window.minSize = new Vector2(450, 400);
            window.Show();
        }

        // ══════════════════════════════════════════════════════════════
        // INPUT SECTION
        // ══════════════════════════════════════════════════════════════
        
        [BoxGroup("Input")]
        [Title("Target Prefabs")]
        [Tooltip("Il prefab del worker da modificare (es. Worker_Base_Mobile)")]
        [Required("Seleziona il prefab del worker da modificare")]
        [AssetsOnly]
        public GameObject targetWorkerPrefab;
        
        [BoxGroup("Input")]
        [Tooltip("Il modello FBX del nuovo personaggio (es. TT_demo_police)")]
        [Required("Seleziona il modello 3D del personaggio")]
        [AssetsOnly]
        public GameObject newCharacterModel;

        // ══════════════════════════════════════════════════════════════
        // ANIMATIONS SECTION
        // ══════════════════════════════════════════════════════════════
        
        [BoxGroup("Animations")]
        [Title("Animation Clips")]
        [Tooltip("Clip di animazione Idle")]
        [Required("Seleziona l'animazione Idle")]
        public AnimationClip idleClip;
        
        [BoxGroup("Animations")]
        [Tooltip("Clip di animazione Run/Walk")]
        [Required("Seleziona l'animazione Run")]
        public AnimationClip runClip;
        
        [BoxGroup("Animations")]
        [Tooltip("Clip di animazione Work/Attack/Interaction")]
        [Required("Seleziona l'animazione Work")]
        public AnimationClip workClip;

        // ══════════════════════════════════════════════════════════════
        // OUTPUT INFO
        // ══════════════════════════════════════════════════════════════
        
        [BoxGroup("Output")]
        [Title("Generated Assets")]
        [ShowInInspector, ReadOnly]
        [LabelText("Output Folder")]
        private string OutputFolder => GENERATED_ANIMATIONS_PATH;
        
        [BoxGroup("Output")]
        [ShowInInspector, ReadOnly]
        [LabelText("Controller Name")]
        private string ControllerName => newCharacterModel != null 
            ? $"Animator_{newCharacterModel.name}.controller" 
            : "Animator_[CharacterName].controller";

        // ══════════════════════════════════════════════════════════════
        // MAIN ACTION
        // ══════════════════════════════════════════════════════════════
        
        [Button(ButtonSizes.Large), GUIColor(0.4f, 0.8f, 0.4f)]
        [PropertySpace(20)]
        [Title("Execute Integration")]
        public void SwapVisualsAndSetupAnimator()
        {
            // Validazione input
            if (!ValidateInputs())
                return;
            
            try
            {
                // A. Sostituzione Mesh
                string prefabPath = AssetDatabase.GetAssetPath(targetWorkerPrefab);
                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                
                if (prefabRoot == null)
                {
                    EditorUtility.DisplayDialog("Errore", "Impossibile caricare il prefab!", "OK");
                    return;
                }
                
                // Trova e rimuovi il vecchio visuale
                Transform oldVisuals = FindVisualsChild(prefabRoot.transform);
                if (oldVisuals != null)
                {
                    Debug.Log($"[CharacterIntegrator] Rimozione vecchio visuale: {oldVisuals.name}");
                    DestroyImmediate(oldVisuals.gameObject);
                }
                
                // Istanzia il nuovo modello come figlio
                GameObject newVisuals = (GameObject)PrefabUtility.InstantiatePrefab(newCharacterModel, prefabRoot.transform);
                if (newVisuals == null)
                {
                    // Fallback: istanziazione normale
                    newVisuals = Instantiate(newCharacterModel, prefabRoot.transform);
                }
                
                // Configura il nuovo visuale
                newVisuals.name = "Visuals";
                newVisuals.transform.localPosition = Vector3.zero;
                newVisuals.transform.localRotation = Quaternion.identity;
                newVisuals.transform.localScale = Vector3.one;
                
                Debug.Log($"[CharacterIntegrator] Nuovo visuale aggiunto: {newVisuals.name}");
                
                // B. Creazione Animator Controller
                AnimatorController controller = CreateAnimatorController();
                
                if (controller == null)
                {
                    EditorUtility.DisplayDialog("Errore", "Impossibile creare l'Animator Controller!", "OK");
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                    return;
                }
                
                // Assegna il controller all'Animator del nuovo modello
                Animator animator = newVisuals.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = newVisuals.AddComponent<Animator>();
                }
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                
                // D. Collega l'Animator al WorkerController (FIX FOOT SLIDING!)
                var workerController = prefabRoot.GetComponent<WildernessSurvival.Gameplay.Workers.WorkerController>();
                if (workerController != null)
                {
                    // Usa reflection per settare il campo privato 'animator'
                    var animatorField = typeof(WildernessSurvival.Gameplay.Workers.WorkerController)
                        .GetField("animator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (animatorField != null)
                    {
                        animatorField.SetValue(workerController, animator);
                        Debug.Log($"[CharacterIntegrator] WorkerController.animator collegato a Visuals!");
                    }
                }
                
                Debug.Log($"[CharacterIntegrator] Animator Controller assegnato: {controller.name}");
                
                // C. Salvataggio del Prefab
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                EditorUtility.DisplayDialog("Successo", 
                    $"Integrazione completata!\n\n" +
                    $"• Visuale sostituito con: {newCharacterModel.name}\n" +
                    $"• Controller creato: {ControllerName}\n" +
                    $"• Prefab salvato: {prefabPath}", 
                    "OK");
                
                Debug.Log($"[CharacterIntegrator] ✅ Integrazione completata per {targetWorkerPrefab.name}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CharacterIntegrator] Errore durante l'integrazione: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("Errore", $"Si è verificato un errore:\n{ex.Message}", "OK");
            }
        }

        // ══════════════════════════════════════════════════════════════
        // HELPER METHODS
        // ══════════════════════════════════════════════════════════════
        
        private bool ValidateInputs()
        {
            if (targetWorkerPrefab == null)
            {
                EditorUtility.DisplayDialog("Validazione", "Seleziona il prefab del worker target!", "OK");
                return false;
            }
            
            if (newCharacterModel == null)
            {
                EditorUtility.DisplayDialog("Validazione", "Seleziona il modello del nuovo personaggio!", "OK");
                return false;
            }
            
            if (idleClip == null || runClip == null || workClip == null)
            {
                EditorUtility.DisplayDialog("Validazione", "Tutte le animazioni (Idle, Run, Work) sono obbligatorie!", "OK");
                return false;
            }
            
            // Verifica che il target sia un prefab
            if (PrefabUtility.GetPrefabAssetType(targetWorkerPrefab) == PrefabAssetType.NotAPrefab)
            {
                EditorUtility.DisplayDialog("Validazione", "Il target deve essere un prefab!", "OK");
                return false;
            }
            
            return true;
        }
        
        private Transform FindVisualsChild(Transform parent)
        {
            // Prima cerca per nome "Visuals"
            Transform visuals = parent.Find("Visuals");
            if (visuals != null)
                return visuals;
            
            // Fallback: cerca il primo figlio con MeshRenderer o SkinnedMeshRenderer
            foreach (Transform child in parent)
            {
                if (child.GetComponent<MeshRenderer>() != null || 
                    child.GetComponent<SkinnedMeshRenderer>() != null ||
                    child.GetComponentInChildren<MeshRenderer>() != null ||
                    child.GetComponentInChildren<SkinnedMeshRenderer>() != null)
                {
                    return child;
                }
            }
            
            return null;
        }
        
        private AnimatorController CreateAnimatorController()
        {
            // Assicura che la cartella esista
            EnsureDirectoryExists(GENERATED_ANIMATIONS_PATH);
            
            string controllerPath = $"{GENERATED_ANIMATIONS_PATH}/Animator_{newCharacterModel.name}.controller";
            
            // Crea il controller
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            
            // Aggiungi parametri
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsWorking", AnimatorControllerParameterType.Bool);
            
            // Ottieni il layer base
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            
            // ═══════════════════════════════════════════════════════════
            // CREA STATI
            // ═══════════════════════════════════════════════════════════
            
            // Stato IDLE (Default)
            AnimatorState idleState = stateMachine.AddState("Idle", new Vector3(250, 100, 0));
            idleState.motion = idleClip;
            stateMachine.defaultState = idleState;
            
            // Stato RUN
            AnimatorState runState = stateMachine.AddState("Run", new Vector3(500, 100, 0));
            runState.motion = runClip;
            
            // Stato WORK
            AnimatorState workState = stateMachine.AddState("Work", new Vector3(375, -50, 0));
            workState.motion = workClip;
            
            // ═══════════════════════════════════════════════════════════
            // CREA TRANSIZIONI
            // ═══════════════════════════════════════════════════════════
            
            // Idle -> Run (quando Speed > 0.1)
            AnimatorStateTransition idleToRun = idleState.AddTransition(runState);
            idleToRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            idleToRun.hasExitTime = false;
            idleToRun.duration = 0.15f;
            
            // Run -> Idle (quando Speed < 0.1)
            AnimatorStateTransition runToIdle = runState.AddTransition(idleState);
            runToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            runToIdle.hasExitTime = false;
            runToIdle.duration = 0.15f;
            
            // AnyState -> Work (quando IsWorking == true)
            AnimatorStateTransition anyToWork = stateMachine.AddAnyStateTransition(workState);
            anyToWork.AddCondition(AnimatorConditionMode.If, 0, "IsWorking");
            anyToWork.hasExitTime = false;
            anyToWork.duration = 0.1f;
            anyToWork.canTransitionToSelf = false;
            
            // Work -> Idle (quando IsWorking == false)
            AnimatorStateTransition workToIdle = workState.AddTransition(idleState);
            workToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsWorking");
            workToIdle.hasExitTime = false;
            workToIdle.duration = 0.15f;
            
            // Salva il controller
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[CharacterIntegrator] Animator Controller creato: {controllerPath}");
            
            return controller;
        }
        
        private void EnsureDirectoryExists(string path)
        {
            // Converti il path Unity in path di sistema
            string fullPath = Path.Combine(Application.dataPath.Replace("/Assets", ""), path);
            
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                AssetDatabase.Refresh();
                Debug.Log($"[CharacterIntegrator] Cartella creata: {path}");
            }
        }

        // ══════════════════════════════════════════════════════════════
        // UTILITY BUTTONS
        // ══════════════════════════════════════════════════════════════
        
        [Button("Open Output Folder"), PropertyOrder(100)]
        [FoldoutGroup("Utilities")]
        private void OpenOutputFolder()
        {
            EnsureDirectoryExists(GENERATED_ANIMATIONS_PATH);
            EditorUtility.RevealInFinder(GENERATED_ANIMATIONS_PATH);
        }
        
        [Button("Clear All Fields"), PropertyOrder(101)]
        [FoldoutGroup("Utilities")]
        private void ClearAllFields()
        {
            targetWorkerPrefab = null;
            newCharacterModel = null;
            idleClip = null;
            runClip = null;
            workClip = null;
        }
        
        [Button("Select Worker Prefabs Folder"), PropertyOrder(102)]
        [FoldoutGroup("Utilities")]
        private void SelectWorkerPrefabsFolder()
        {
            Object folder = AssetDatabase.LoadAssetAtPath<Object>("Assets/_Gameplay/Workers/Prefabs");
            if (folder != null)
            {
                Selection.activeObject = folder;
                EditorGUIUtility.PingObject(folder);
            }
        }
    }
}
#endif
