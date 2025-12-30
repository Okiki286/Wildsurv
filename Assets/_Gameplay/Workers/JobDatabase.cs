using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using WildernessSurvival.Gameplay.Structures;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// Database Singleton per i WorkerJobData.
    /// Fornisce lookup rapido dei job per WorkerRole.
    /// </summary>
    [CreateAssetMenu(fileName = "JobDatabase", menuName = "Wilderness Survival/Job Database")]
    public class JobDatabase : ScriptableObject
    {
        // ============================================
        // SINGLETON
        // ============================================

        private static JobDatabase _instance;
        public static JobDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Resources.Load<JobDatabase>("JobDatabase");
                    if (_instance == null)
                    {
                        Debug.LogWarning("[JobDatabase] No JobDatabase found in Resources folder. Create one at Resources/JobDatabase.asset");
                    }
                }
                return _instance;
            }
        }

        // ============================================
        // JOB DATA
        // ============================================

        [TitleGroup("Job Data")]
        [SerializeField]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true)]
        [Tooltip("Lista di tutti i job disponibili")]
        private List<WorkerJobData> allJobs = new List<WorkerJobData>();
        public List<WorkerJobData> AllJobs => allJobs;

        [TitleGroup("Default Job")]
        [SerializeField]
        [Required("Assegna il WorkerJobData di default (Villager)")]
        [Tooltip("Job di default quando un worker non è assegnato")]
        private WorkerJobData defaultVillagerJob;
        public WorkerJobData DefaultVillagerJob => defaultVillagerJob;

        // ============================================
        // CACHE
        // ============================================

        private Dictionary<WorkerRole, WorkerJobData> _jobLookup;
        private bool _cacheInitialized = false;

        // ============================================
        // PROPERTIES
        // ============================================

        [ShowInInspector, ReadOnly]
        [TitleGroup("Status")]
        public bool IsConfigured => defaultVillagerJob != null && allJobs.Count > 0;

        [ShowInInspector, ReadOnly]
        public int JobCount => allJobs?.Count ?? 0;

        // ============================================
        // INITIALIZATION
        // ============================================

        private void OnEnable()
        {
            InitializeCache();
        }

        /// <summary>
        /// Inizializza la cache di lookup per role.
        /// </summary>
        public void InitializeCache()
        {
            _jobLookup = new Dictionary<WorkerRole, WorkerJobData>();

            if (allJobs == null) return;

            foreach (var job in allJobs)
            {
                if (job == null) continue;

                if (!_jobLookup.ContainsKey(job.Role))
                {
                    _jobLookup[job.Role] = job;
                }
                else
                {
                    Debug.LogWarning($"[JobDatabase] Duplicate role {job.Role} found. Using first: {_jobLookup[job.Role].JobName}");
                }
            }

            _cacheInitialized = true;
            Debug.Log($"<color=green>[JobDatabase]</color> Cache initialized with {_jobLookup.Count} jobs.");
        }

        // ============================================
        // LOOKUP METHODS
        // ============================================

        /// <summary>
        /// Ottiene il WorkerJobData per un dato ruolo.
        /// </summary>
        public WorkerJobData GetJobData(WorkerRole role)
        {
            if (!_cacheInitialized)
            {
                InitializeCache();
            }

            if (_jobLookup != null && _jobLookup.TryGetValue(role, out var jobData))
            {
                return jobData;
            }

            Debug.LogWarning($"[JobDatabase] No job found for role: {role}");
            return null;
        }

        /// <summary>
        /// Ottiene il job di default (Villager).
        /// </summary>
        public WorkerJobData GetDefaultJob()
        {
            return defaultVillagerJob;
        }

        /// <summary>
        /// Ottiene il WorkerJobData tramite string ID.
        /// </summary>
        public WorkerJobData GetJobData(string jobId)
        {
            if (string.IsNullOrEmpty(jobId)) return null;
            if (allJobs == null) return null;

            // Simple search (can be optimized with cache if needed)
            for (int i = 0; i < allJobs.Count; i++)
            {
                if (allJobs[i] != null && allJobs[i].JobId == jobId)
                {
                    return allJobs[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Verifica se esiste un job per il ruolo specificato.
        /// </summary>
        public bool HasJobForRole(WorkerRole role)
        {
            if (!_cacheInitialized)
            {
                InitializeCache();
            }

            return _jobLookup != null && _jobLookup.ContainsKey(role);
        }

        // ============================================
        // EDITOR TOOLS
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Editor Tools")]
        [Button("🔄 Refresh Cache", ButtonSizes.Medium)]
        private void EditorRefreshCache()
        {
            InitializeCache();
        }

        [Button("Print All Jobs", ButtonSizes.Medium)]
        private void EditorPrintJobs()
        {
            Debug.Log($"=== JOB DATABASE ({allJobs?.Count ?? 0} jobs) ===");

            if (allJobs != null)
            {
                foreach (var job in allJobs)
                {
                    if (job != null)
                    {
                        string visualStatus = job.HasValidVisualSet ? "MeshSwap" : (job.UseLegacySystem ? "Legacy" : "NONE");
                        Debug.Log($"  [{job.Role}] {job.JobName} - Visual: [{visualStatus}]");
                    }
                }
            }

            Debug.Log($"Default Job: {(defaultVillagerJob != null ? defaultVillagerJob.JobName : "NOT SET")}");
        }

        [Button("🔍 Auto-Find Jobs in Project", ButtonSizes.Medium), GUIColor(0.4f, 0.8f, 1f)]
        private void EditorAutoFindJobs()
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("t:WorkerJobData");
            allJobs.Clear();

            foreach (var guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var job = UnityEditor.AssetDatabase.LoadAssetAtPath<WorkerJobData>(path);
                if (job != null && !allJobs.Contains(job))
                {
                    allJobs.Add(job);
                }
            }

            Debug.Log($"[JobDatabase] Found {allJobs.Count} WorkerJobData assets.");
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
