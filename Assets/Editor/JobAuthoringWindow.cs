#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Collections.Generic;
using System.Linq;
using System;
using WildernessSurvival.Gameplay.Workers;
using WildernessSurvival.Gameplay.Structures;

namespace WildernessSurvival.Editor
{
    /// <summary>
    /// Odin EditorWindow per authoring e management dei Worker Jobs.
    /// Permette di:
    /// - Generare/aggiornare WorkerJobData per ogni WorkerRole
    /// - Sincronizzare JobDatabase
    /// - Assegnare job di default ai WorkerData
    /// - Assegnare required jobs alle strutture
    /// - Browse e edit jobs in split panel (left list / right inspector)
    /// - Preview workers in scene con job applicato
    /// </summary>
    public class JobAuthoringWindow : OdinEditorWindow
    {
        // ============================================
        // MENU ITEM
        // ============================================

        [MenuItem("Tools/Wilderness/Job Authoring Window")]
        private static void OpenWindow()
        {
            var window = GetWindow<JobAuthoringWindow>();
            window.titleContent = new GUIContent("Job Authoring");
            window.minSize = new Vector2(900, 600);
            window.Show();
        }

        // ============================================
        // CONFIGURATION
        // ============================================

        [TitleGroup("Configuration")]
        [FolderPath(AbsolutePath = false, RequireExistingPath = false)]
        [InfoBox("Cartella dove verranno salvati i WorkerJobData generati. Default: Assets/_GameData/Jobs")]
        [SerializeField]
        private string jobsFolder = "Assets/_GameData/Jobs";

        [TitleGroup("Configuration")]
        [Required("Assegna il JobDatabase da aggiornare")]
        [AssetsOnly]
        [SerializeField]
        private JobDatabase jobDatabase;

        [TitleGroup("Configuration")]
        [Tooltip("Job di default (Villager) da assegnare ai worker che non hanno job")]
        [AssetsOnly]
        [SerializeField]
        private WorkerJobData defaultVillagerJob;

        [TitleGroup("Configuration")]
        [Tooltip("Prefab worker da usare per preview in scene")]
        [AssetsOnly]
        [SerializeField]
        private GameObject workerPreviewPrefab;

        [TitleGroup("Configuration")]
        [Tooltip("Palette colori standard per ruoli (opzionale)")]
        [AssetsOnly]
        [SerializeField]
        private JobRolePalette rolePalette;

        // ============================================
        // STRUCTURE JOB RULES
        // ============================================

        [TitleGroup("Structure Job Assignment Rules")]
        [InfoBox("Regole per assegnare automaticamente i required jobs alle strutture in base al nome.", InfoMessageType.Info)]
        [ListDrawerSettings(DefaultExpandedState = true, ShowIndexLabels = true, DraggableItems = true)]
        [SerializeField]
        private List<StructureJobRule> structureJobRules = new List<StructureJobRule>
        {
            new StructureJobRule { nameContains = "Sawmill", requiredJob = null },
            new StructureJobRule { nameContains = "Mine", requiredJob = null },
            new StructureJobRule { nameContains = "Farm", requiredJob = null },
            new StructureJobRule { nameContains = "Tower", requiredJob = null },
            new StructureJobRule { nameContains = "Wall", requiredJob = null }
        };

        // ============================================
        // JOB BROWSER
        // ============================================

        [HorizontalGroup("Browser", 0.35f)]
        [VerticalGroup("Browser/Left")]
        [TitleGroup("Browser/Left/Jobs List")]
        [ShowInInspector]
        [LabelText("Seleziona Job")]
        [ValueDropdown("allJobs", IsUniqueList = true)]
        [OnValueChanged("OnJobSelected")]
        private WorkerJobData selectedJobFromDropdown;

        [VerticalGroup("Browser/Left")]
        [ListDrawerSettings(
            ShowIndexLabels = false,
            DraggableItems = false,
            HideAddButton = true,
            HideRemoveButton = true,
            OnTitleBarGUI = "DrawJobListToolbar"
        )]
        [ShowInInspector]
        [InfoBox("Usa il dropdown sopra per selezionare un job, oppure fai doppio-click qui.", InfoMessageType.None)]
        private List<WorkerJobData> allJobs = new List<WorkerJobData>();

        private void OnJobSelected()
        {
            if (selectedJobFromDropdown != null)
            {
                selectedJob = selectedJobFromDropdown;
#if UNITY_EDITOR
                Debug.Log($"<color=cyan>[JobAuthoringWindow]</color> Selected job: {selectedJob.JobName}");
#endif
            }
        }

        [HorizontalGroup("Browser")]
        [VerticalGroup("Browser/Right")]
        [TitleGroup("Browser/Right/Selected Job Inspector")]
        [ShowInInspector]
        [InlineEditor(InlineEditorObjectFieldModes.CompletelyHidden)]
        [HideLabel]
        [EnableIf("@selectedJob != null")]
        private WorkerJobData selectedJob;

        // ============================================
        // VISUAL SETTINGS (ADVANCED EDITOR)
        // ============================================

        // Placeholder per creare il gruppo Visual Settings richiesto dai button
        [VerticalGroup("Browser/Right")]
        [TitleGroup("Browser/Right/Visual Settings")]
        [HideInInspector]
        private bool visualSettingsGroupPlaceholder;

        // ============================================
        // PREVIEW
        // ============================================

        [VerticalGroup("Browser/Right")]
        [TitleGroup("Browser/Right/Scene Preview")]
        [ShowInInspector, ReadOnly]
        [InfoBox("Nessun preview attivo", InfoMessageType.None, VisibleIf = "@previewInstance == null")]
        [InfoBox("Preview attivo in scena", InfoMessageType.Info, VisibleIf = "@previewInstance != null")]
        private GameObject previewInstance;

        // ============================================
        // LIFECYCLE
        // ============================================

        protected override void OnEnable()
        {
            base.OnEnable();
            RefreshJobList();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            CleanupPreview();
        }

        // ============================================
        // JOB LIST TOOLBAR
        // ============================================

        private void DrawJobListToolbar()
        {
            if (GUILayout.Button("Refresh", GUILayout.Width(70)))
            {
                RefreshJobList();
            }
        }

        // ============================================
        // JOB GENERATION & SYNC
        // ============================================

        [VerticalGroup("Browser/Left")]
        [ButtonGroup("Browser/Left/Actions")]
        [Button("Generate/Update Jobs", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
        [InfoBox("Genera o aggiorna WorkerJobData per ogni WorkerRole nell'enum. Crea asset mancanti nella cartella Jobs.", InfoMessageType.Info)]
        private void GenerateOrUpdateWorkerJobData()
        {
            if (string.IsNullOrEmpty(jobsFolder))
            {
                EditorUtility.DisplayDialog("Error", "Jobs folder path is empty!", "OK");
                return;
            }

            // Crea la cartella se non esiste
            if (!AssetDatabase.IsValidFolder(jobsFolder))
            {
                string parentFolder = System.IO.Path.GetDirectoryName(jobsFolder).Replace('\\', '/');
                string folderName = System.IO.Path.GetFileName(jobsFolder);
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }

            int created = 0;
            int updated = 0;

            // Itera su tutti i valori dell'enum WorkerRole
            foreach (WorkerRole role in Enum.GetValues(typeof(WorkerRole)))
            {
                if (role == WorkerRole.None) continue;

                string assetName = $"Job_{role}.asset";
                string assetPath = $"{jobsFolder}/{assetName}";

                WorkerJobData existingJob = AssetDatabase.LoadAssetAtPath<WorkerJobData>(assetPath);

                if (existingJob == null)
                {
                    // Crea nuovo job
                    WorkerJobData newJob = ScriptableObject.CreateInstance<WorkerJobData>();

                    // Usa reflection per settare i campi privati
                    var jobIdField = typeof(WorkerJobData).GetField("jobId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var jobNameField = typeof(WorkerJobData).GetField("jobName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var roleField = typeof(WorkerJobData).GetField("role", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    jobIdField?.SetValue(newJob, role.ToString().ToLower());
                    jobNameField?.SetValue(newJob, role.ToString());
                    roleField?.SetValue(newJob, role);

                    // Applica colore dalla palette se disponibile
                    if (rolePalette != null)
                    {
                        Color roleColor = rolePalette.GetColor(role, Color.white);
                        var visualSetField = typeof(WorkerJobData).GetField("visualSet", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (visualSetField != null)
                        {
                            var visualSet = visualSetField.GetValue(newJob) as WorkerVisualSet;
                            if (visualSet != null && visualSet.roleColorTint == Color.white)
                            {
                                visualSet.roleColorTint = roleColor;
                                Debug.Log($"[JobAuthoringWindow] Applied palette color {roleColor} to {role}");
                            }
                        }
                    }

                    AssetDatabase.CreateAsset(newJob, assetPath);
                    created++;

                    Debug.Log($"[JobAuthoringWindow] Created WorkerJobData: {assetPath}");

                    // Se è None (Villager) e non abbiamo defaultVillagerJob, assegnalo
                    if (role == WorkerRole.None && defaultVillagerJob == null)
                    {
                        defaultVillagerJob = newJob;
                        EditorUtility.SetDirty(this);
                    }
                }
                else
                {
                    // Aggiorna job esistente se necessario
                    var roleField = typeof(WorkerJobData).GetField("role", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var currentRole = (WorkerRole)roleField?.GetValue(existingJob);

                    if (currentRole != role)
                    {
                        roleField?.SetValue(existingJob, role);
                        EditorUtility.SetDirty(existingJob);
                        updated++;
                        Debug.Log($"[JobAuthoringWindow] Updated role for: {assetPath}");
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RefreshJobList();

            EditorUtility.DisplayDialog(
                "Job Generation Complete",
                $"Created: {created}\nUpdated: {updated}\n\nTotal jobs: {allJobs.Count}",
                "OK"
            );
        }

        [VerticalGroup("Browser/Left")]
        [ButtonGroup("Browser/Left/Actions")]
        [Button("Sync Job Database", ButtonSizes.Large), GUIColor(0.4f, 1f, 0.8f)]
        [InfoBox("Sincronizza il JobDatabase con tutti i WorkerJobData trovati nel progetto.", InfoMessageType.Info)]
        private void SyncJobDatabase()
        {
            if (jobDatabase == null)
            {
                EditorUtility.DisplayDialog("Error", "JobDatabase is not assigned!", "OK");
                return;
            }

            // Trova tutti i WorkerJobData nel progetto
            string[] guids = AssetDatabase.FindAssets("t:WorkerJobData");
            List<WorkerJobData> foundJobs = new List<WorkerJobData>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                WorkerJobData job = AssetDatabase.LoadAssetAtPath<WorkerJobData>(path);
                if (job != null)
                {
                    foundJobs.Add(job);
                }
            }

            // Usa reflection per accedere al campo privato allJobs
            var allJobsField = typeof(JobDatabase).GetField("allJobs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (allJobsField != null)
            {
                allJobsField.SetValue(jobDatabase, foundJobs);
            }

            // Assegna defaultVillagerJob se presente
            if (defaultVillagerJob != null)
            {
                var defaultJobField = typeof(JobDatabase).GetField("defaultVillagerJob", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                defaultJobField?.SetValue(jobDatabase, defaultVillagerJob);
            }

            EditorUtility.SetDirty(jobDatabase);
            AssetDatabase.SaveAssets();

            Debug.Log($"[JobAuthoringWindow] Synced JobDatabase with {foundJobs.Count} jobs.");

            EditorUtility.DisplayDialog(
                "Sync Complete",
                $"JobDatabase synced with {foundJobs.Count} jobs.",
                "OK"
            );
        }

        // ============================================
        // WORKER & STRUCTURE ASSIGNMENT
        // ============================================

        [VerticalGroup("Browser/Left")]
        [ButtonGroup("Browser/Left/Batch")]
        [Button("Assign Default Job to Workers", ButtonSizes.Medium)]
        private void AssignDefaultJobToAllWorkers()
        {
            if (defaultVillagerJob == null)
            {
                EditorUtility.DisplayDialog("Error", "Default Villager Job is not assigned!", "OK");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:WorkerData");
            int assigned = 0;

            // Ottieni il role dal defaultVillagerJob
            WorkerRole villagerRole = defaultVillagerJob.Role;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                WorkerData workerData = AssetDatabase.LoadAssetAtPath<WorkerData>(path);

                if (workerData != null)
                {
                    // Usa reflection per accedere al campo privato defaultRole
                    var defaultRoleField = typeof(WorkerData).GetField("defaultRole", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (defaultRoleField != null)
                    {
                        var currentRole = (WorkerRole)defaultRoleField.GetValue(workerData);

                        // Assegna solo se è None (non ha un role impostato)
                        if (currentRole == WorkerRole.None)
                        {
                            defaultRoleField.SetValue(workerData, villagerRole);
                            EditorUtility.SetDirty(workerData);
                            assigned++;
                            Debug.Log($"[JobAuthoringWindow] Assigned role {villagerRole} to WorkerData: {workerData.name}");
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();

            Debug.Log($"[JobAuthoringWindow] Assigned default role to {assigned} WorkerData assets.");

            EditorUtility.DisplayDialog(
                "Workers Updated",
                $"Assigned default role ({villagerRole}) to {assigned} WorkerData that had None role.",
                "OK"
            );
        }

        [VerticalGroup("Browser/Left")]
        [ButtonGroup("Browser/Left/Batch")]
        [Button("Assign Required Jobs to Structures", ButtonSizes.Medium)]
        private void AssignRequiredJobToStructures()
        {
            if (structureJobRules == null || structureJobRules.Count == 0)
            {
                EditorUtility.DisplayDialog("Info", "No structure job rules defined.", "OK");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:StructureData");
            int assigned = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                StructureData structureData = AssetDatabase.LoadAssetAtPath<StructureData>(path);

                if (structureData != null)
                {
                    bool matched = false;

                    foreach (var rule in structureJobRules)
                    {
                        if (string.IsNullOrEmpty(rule.nameContains)) continue;
                        if (rule.requiredJob == null) continue;

                        if (structureData.name.IndexOf(rule.nameContains, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            // Ottieni il role dal WorkerJobData
                            WorkerRole jobRole = rule.requiredJob.Role;

                            // Usa reflection per accedere al campo privato allowedRoles in StructureData
                            var allowedRolesField = typeof(StructureData).GetField("allowedRoles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                            if (allowedRolesField != null)
                            {
                                // Sovrascrivi con il role del job (puoi usare |= per aggiungere invece di sovrascrivere)
                                allowedRolesField.SetValue(structureData, jobRole);

                                Debug.Log($"[JobAuthoringWindow] Matched structure '{structureData.name}' with rule '{rule.nameContains}' → set AllowedRoles to {jobRole}");
                                matched = true;
                                assigned++;
                            }

                            break;
                        }
                    }

                    if (matched)
                    {
                        EditorUtility.SetDirty(structureData);
                    }
                }
            }

            AssetDatabase.SaveAssets();

            Debug.Log($"[JobAuthoringWindow] Assigned jobs to {assigned} structures based on rules.");

            EditorUtility.DisplayDialog(
                "Structure Assignment Complete",
                $"Matched {assigned} structures with job rules.\n\nNote: StructureData uses AllowedRoles (flags), not a single RequiredJob field.",
                "OK"
            );
        }

        // ============================================
        // JOB BROWSER
        // ============================================

        private void RefreshJobList()
        {
            allJobs.Clear();

            string[] guids = AssetDatabase.FindAssets("t:WorkerJobData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                WorkerJobData job = AssetDatabase.LoadAssetAtPath<WorkerJobData>(path);
                if (job != null)
                {
                    allJobs.Add(job);
                }
            }

            // Ordina per Role
            allJobs = allJobs.OrderBy(j => j.Role).ToList();

            Debug.Log($"[JobAuthoringWindow] Refreshed job list: {allJobs.Count} jobs found.");
        }

        [VerticalGroup("Browser/Left")]
        [ShowInInspector]
        [DisplayAsString]
        [HideLabel]
        private string JobListInfo => $"Jobs: {allJobs.Count}";

        // ============================================
        // VISUAL SETTINGS ACTIONS
        // ============================================

        [VerticalGroup("Browser/Right/Visual Settings")]
        [Button("Apply Role Color from Palette", ButtonSizes.Medium), GUIColor(0.8f, 0.6f, 1f)]
        [EnableIf("@selectedJob != null && rolePalette != null")]
        private void ApplyRoleColorToSelectedJob()
        {
            if (selectedJob == null)
            {
                EditorUtility.DisplayDialog("Error", "No job selected!", "OK");
                return;
            }

            if (rolePalette == null)
            {
                EditorUtility.DisplayDialog("Error", "Job Role Palette is not assigned!", "OK");
                return;
            }

            // Ottieni colore dalla palette
            Color roleColor = rolePalette.GetColor(selectedJob.Role, Color.white);

            // Usa reflection per accedere al VisualSet privato
            var visualSetField = typeof(WorkerJobData).GetField("visualSet", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (visualSetField != null)
            {
                var visualSet = visualSetField.GetValue(selectedJob) as WorkerVisualSet;
                if (visualSet != null)
                {
                    visualSet.roleColorTint = roleColor;
                    EditorUtility.SetDirty(selectedJob);
                    AssetDatabase.SaveAssets();

                    Debug.Log($"<color=cyan>[JobAuthoringWindow]</color> Applied color {roleColor} to {selectedJob.JobName} from palette.");

                    EditorUtility.DisplayDialog(
                        "Color Applied",
                        $"Applied role color for {selectedJob.Role} to {selectedJob.JobName}.\n\nColor: RGB({roleColor.r:F2}, {roleColor.g:F2}, {roleColor.b:F2})",
                        "OK"
                    );
                }
            }
        }

        [VerticalGroup("Browser/Right")]
        [ButtonGroup("Browser/Right/Visual Settings/Actions")]
        [Button("Refresh Preview Visual", ButtonSizes.Medium), GUIColor(0.4f, 1f, 0.8f)]
        [EnableIf("@selectedJob != null && previewInstance != null")]
        private void RefreshPreviewVisual()
        {
            if (previewInstance == null)
            {
                EditorUtility.DisplayDialog("Error", "No preview instance in scene!", "OK");
                return;
            }

            if (selectedJob == null)
            {
                EditorUtility.DisplayDialog("Error", "No job selected!", "OK");
                return;
            }

            var visualController = previewInstance.GetComponent<WorkerVisualController>();
            if (visualController != null)
            {
                visualController.ApplyJobVisual(selectedJob);
                Debug.Log($"<color=cyan>[JobAuthoringWindow]</color> Refreshed preview visual for {selectedJob.JobName}");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Preview instance doesn't have WorkerVisualController!", "OK");
            }
        }

        // ============================================
        // SCENE PREVIEW
        // ============================================

        [VerticalGroup("Browser/Right")]
        [ButtonGroup("Browser/Right/Scene Preview/Actions")]
        [Button("Spawn Preview in Scene", ButtonSizes.Large), GUIColor(0.4f, 1f, 0.4f)]
        [EnableIf("@selectedJob != null && workerPreviewPrefab != null")]
        private void SpawnPreviewInScene()
        {
            if (workerPreviewPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Worker Preview Prefab is not assigned!", "OK");
                return;
            }

            if (selectedJob == null)
            {
                EditorUtility.DisplayDialog("Error", "No job selected!", "OK");
                return;
            }

            // Cleanup vecchio preview
            CleanupPreview();

            // Spawn nuovo preview
            previewInstance = (GameObject)PrefabUtility.InstantiatePrefab(workerPreviewPrefab);
            previewInstance.name = $"Preview_{selectedJob.JobName}";
            previewInstance.transform.position = Vector3.zero;

            // Force-init MeshController (Awake non viene chiamato in Editor)
            var meshController = previewInstance.GetComponent<WorkerMeshController>();
            if (meshController != null)
            {
                meshController.Initialize();
            }

            // Cerca WorkerVisualController
            var visualController = previewInstance.GetComponent<WorkerVisualController>();
            if (visualController != null)
            {
                // Applica direttamente il job visual senza WorkerInstance
                visualController.ApplyJobVisual(selectedJob);

                Debug.Log($"[JobAuthoringWindow] Spawned preview for job: {selectedJob.JobName}");
            }
            else
            {
                Debug.LogWarning($"[JobAuthoringWindow] Preview prefab doesn't have WorkerVisualController. Preview spawned but visuals won't be applied.");
            }

            Selection.activeGameObject = previewInstance;
        }

        [VerticalGroup("Browser/Right")]
        [ButtonGroup("Browser/Right/Scene Preview/Actions")]
        [Button("Destroy Preview", ButtonSizes.Medium), GUIColor(1f, 0.4f, 0.4f)]
        [EnableIf("@previewInstance != null")]
        private void DestroyPreviewInScene()
        {
            CleanupPreview();
        }

        private void CleanupPreview()
        {
            if (previewInstance != null)
            {
                DestroyImmediate(previewInstance);
                previewInstance = null;
                Debug.Log("[JobAuthoringWindow] Preview destroyed.");
            }
        }

        // ============================================
        // VALIDATION SYSTEM
        // ============================================

        [FoldoutGroup("Validation")]
        [InfoBox("Valida tutti i dati del gioco (Jobs, Workers, Structures) per trovare configurazioni mancanti o incoerenti.", InfoMessageType.Info)]
        [TableList(IsReadOnly = true, AlwaysExpanded = true)]
        [ShowInInspector]
        private List<ValidationEntry> validationResults = new List<ValidationEntry>();

        [FoldoutGroup("Validation")]
        [ShowInInspector, ReadOnly]
        [DisplayAsString]
        private string ValidationSummary => validationResults.Count > 0
            ? $"⚠ {validationResults.Count} warnings found"
            : "✓ No issues found";

        [FoldoutGroup("Validation")]
        [Button("Validate All Game Data", ButtonSizes.Large), GUIColor(1f, 0.8f, 0.4f)]
        private void ValidateAllGameData()
        {
            validationResults.Clear();
            int totalWarnings = 0;

            Debug.Log("<color=yellow>[JobAuthoringWindow]</color> Starting validation...");

            // ═══════════════════════════════════════════════════════════
            // VALIDATE WORKER JOB DATA
            // ═══════════════════════════════════════════════════════════
            string[] jobGuids = AssetDatabase.FindAssets("t:WorkerJobData");
            foreach (string guid in jobGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                WorkerJobData job = AssetDatabase.LoadAssetAtPath<WorkerJobData>(path);

                if (job != null)
                {
                    // Check VisualSet
                    if (job.VisualSet == null)
                    {
                        validationResults.Add(new ValidationEntry
                        {
                            category = "Job",
                            severity = "Error",
                            message = $"'{job.name}': VisualSet is null",
                            context = job
                        });
                        totalWarnings++;
                    }
                    else if (!job.HasValidVisualSet)
                    {
                        validationResults.Add(new ValidationEntry
                        {
                            category = "Job",
                            severity = "Warning",
                            message = $"'{job.name}': VisualSet has no valid meshes",
                            context = job
                        });
                        totalWarnings++;
                    }

                    // Check JobName
                    if (string.IsNullOrEmpty(job.JobName))
                    {
                        validationResults.Add(new ValidationEntry
                        {
                            category = "Job",
                            severity = "Warning",
                            message = $"'{job.name}': JobName is empty",
                            context = job
                        });
                        totalWarnings++;
                    }

                    // Check Animator
                    if (job.VisualSet != null && job.VisualSet.animatorController == null)
                    {
                        validationResults.Add(new ValidationEntry
                        {
                            category = "Job",
                            severity = "Info",
                            message = $"'{job.name}': No animator controller assigned",
                            context = job
                        });
                    }
                }
            }

            // ═══════════════════════════════════════════════════════════
            // VALIDATE WORKER DATA
            // ═══════════════════════════════════════════════════════════
            string[] workerGuids = AssetDatabase.FindAssets("t:WorkerData");
            foreach (string guid in workerGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                WorkerData workerData = AssetDatabase.LoadAssetAtPath<WorkerData>(path);

                if (workerData != null)
                {
                    // Check defaultRole usando reflection
                    var defaultRoleField = typeof(WorkerData).GetField("defaultRole", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (defaultRoleField != null)
                    {
                        var defaultRole = (WorkerRole)defaultRoleField.GetValue(workerData);

                        // Warning se è None (a meno che non sia intenzionale per Villager generico)
                        if (defaultRole == WorkerRole.None && !workerData.name.ToLower().Contains("villager"))
                        {
                            validationResults.Add(new ValidationEntry
                            {
                                category = "Worker",
                                severity = "Warning",
                                message = $"'{workerData.name}': defaultRole is None (not assigned)",
                                context = workerData
                            });
                            totalWarnings++;
                        }
                    }

                    // Check prefab
                    var prefabField = typeof(WorkerData).GetField("prefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (prefabField != null)
                    {
                        var prefab = prefabField.GetValue(workerData) as GameObject;
                        if (prefab == null)
                        {
                            validationResults.Add(new ValidationEntry
                            {
                                category = "Worker",
                                severity = "Error",
                                message = $"'{workerData.name}': Prefab is not assigned",
                                context = workerData
                            });
                            totalWarnings++;
                        }
                    }
                }
            }

            // ═══════════════════════════════════════════════════════════
            // VALIDATE STRUCTURE DATA
            // ═══════════════════════════════════════════════════════════
            string[] structureGuids = AssetDatabase.FindAssets("t:StructureData");
            foreach (string guid in structureGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                StructureData structureData = AssetDatabase.LoadAssetAtPath<StructureData>(path);

                if (structureData != null)
                {
                    // Check allowedRoles usando reflection
                    var allowedRolesField = typeof(StructureData).GetField("allowedRoles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var workerSlotsField = typeof(StructureData).GetField("workerSlots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var requiresBuilderField = typeof(StructureData).GetField("requiresBuilder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (allowedRolesField != null && workerSlotsField != null)
                    {
                        var allowedRoles = (WorkerRole)allowedRolesField.GetValue(structureData);
                        var workerSlots = (int)workerSlotsField.GetValue(structureData);

                        // Warning se ha worker slots ma AllowedRoles è None
                        if (workerSlots > 0 && allowedRoles == WorkerRole.None)
                        {
                            validationResults.Add(new ValidationEntry
                            {
                                category = "Structure",
                                severity = "Warning",
                                message = $"'{structureData.name}': Has {workerSlots} worker slots but allowedRoles is None",
                                context = structureData
                            });
                            totalWarnings++;
                        }
                    }

                    // Check prefab
                    var prefabField = typeof(StructureData).GetField("prefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (prefabField != null)
                    {
                        var prefab = prefabField.GetValue(structureData) as GameObject;
                        if (prefab == null)
                        {
                            validationResults.Add(new ValidationEntry
                            {
                                category = "Structure",
                                severity = "Error",
                                message = $"'{structureData.name}': Prefab is not assigned",
                                context = structureData
                            });
                            totalWarnings++;
                        }
                    }
                }
            }

            // ═══════════════════════════════════════════════════════════
            // SUMMARY
            // ═══════════════════════════════════════════════════════════
            Debug.Log($"<color=yellow>[JobAuthoringWindow]</color> Validation complete: {totalWarnings} issues found.");

            if (totalWarnings == 0)
            {
                EditorUtility.DisplayDialog(
                    "Validation Complete",
                    "✓ No issues found!\n\nAll Jobs, Workers, and Structures are properly configured.",
                    "OK"
                );
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Validation Complete",
                    $"⚠ Found {totalWarnings} issues.\n\nCheck the Validation panel for details.\n\nClick on entries to select problematic assets.",
                    "OK"
                );
            }
        }

        [FoldoutGroup("Validation")]
        [Button("Clear Validation Results", ButtonSizes.Small)]
        private void ClearValidationResults()
        {
            validationResults.Clear();
            Debug.Log("[JobAuthoringWindow] Validation results cleared.");
        }
    }

    // ============================================
    // HELPER CLASSES
    // ============================================

    [Serializable]
    public class ValidationEntry
    {
        [TableColumnWidth(80, Resizable = false)]
        [LabelText("Category")]
        public string category;

        [TableColumnWidth(70, Resizable = false)]
        [LabelText("Severity")]
        public string severity;

        [TableColumnWidth(300, Resizable = true)]
        [LabelText("Message")]
        public string message;

        [TableColumnWidth(150, Resizable = false)]
        [LabelText("Asset")]
        [AssetsOnly]
        public UnityEngine.Object context;
    }

    // ============================================
    // HELPER CLASSES
    // ============================================

    [Serializable]
    public class StructureJobRule
    {
        [HorizontalGroup("Rule")]
        [LabelWidth(100)]
        [Tooltip("Nome (o parte del nome) della struttura")]
        public string nameContains;

        [HorizontalGroup("Rule")]
        [LabelWidth(100)]
        [Tooltip("Job richiesto da assegnare")]
        [AssetsOnly]
        public WorkerJobData requiredJob;
    }
}
#endif
