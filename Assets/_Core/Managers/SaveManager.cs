using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using WildernessSurvival.Core.Systems;
using WildernessSurvival.Gameplay.Resources;
using WildernessSurvival.Gameplay.Workers;
using WildernessSurvival.Gameplay.Structures;

namespace Wilderness.Core
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }
        private const string SAVE_FILENAME = "savegame.json";
        private string SaveFilePath => Path.Combine(Application.persistentDataPath, SAVE_FILENAME);

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SaveGame()
        {
            try {
                GameSaveData data = new GameSaveData();

                // 1. Save Global State (Time)
                if (DayNightSystem.Instance != null) {
                    data.globalState.currentDay = DayNightSystem.Instance.CurrentDayNumber;
                    data.globalState.currentDayPhase = (int)DayNightSystem.Instance.CurrentDayPhase;
                }

                // 2. Save Resources
                if (ResourceSystem.Instance != null) {
                    foreach (var res in ResourceSystem.Instance.GetAllResourceData()) {
                        float amount = ResourceSystem.Instance.GetResourceAmount(res.ResourceId);
                        data.resources.resourceAmounts.Add(new ResourceAmountEntry { resourceId = res.ResourceId, amount = amount });
                    }
                }

                // 3. Save Workers
                if (WorkerSystem.Instance != null) {
                    var allWorkers = WorkerSystem.Instance.AllWorkerInstances;
                    var workerList = new List<WorkerStateData>();

                    foreach (var w in allWorkers) {
                        var state = new WorkerStateData {
                            instanceId = w.InstanceId,
                            customName = w.CustomName,
                            workerDataId = w.Data.WorkerId,
                            currentHealth = w.CurrentHealth,
                            maxHealth = w.MaxHealth,
                            level = w.Level,
                            experience = w.Experience,
                            currentState = (int)w.CurrentState,
                            isAtWorksite = w.IsAtWorksite,
                            currentJobId = w.CurrentJob?.JobId ?? "",
                            assignedStructureId = "",  // Phase 3: StructureController.InstanceId not yet implemented
                            assignedHomeId = ""        // Phase 3: ShelterHome.InstanceId not yet implemented
                        };

                        if (w.PhysicalWorker != null) {
                            Vector3 pos = w.PhysicalWorker.transform.position;
                            state.posX = pos.x;
                            state.posY = pos.y;
                            state.posZ = pos.z;
                        }

                        workerList.Add(state);
                    }
                    data.workers = workerList.ToArray();
                } else {
                    data.workers = new WorkerStateData[0];
                }

                // 4. Save Structures
                if (StructureSystem.Instance != null) {
                    var allStructures = StructureSystem.Instance.AllStructures;
                    var structList = new List<StructureStateData>();

                    foreach (var s in allStructures) {
                        structList.Add(new StructureStateData {
                            instanceId = s.InstanceId,
                            structureDataId = s.Data.StructureId,
                            originCellX = s.OriginCell.x,
                            originCellY = s.OriginCell.y,
                            rotationStep = s.PlacementRotationStep,
                            currentHealth = s.CurrentHealth,
                            currentLevel = s.CurrentLevel,
                            buildProgress = s.BuildProgress,
                            isOperational = s.IsOperational,
                            structureState = (int)s.State,
                            assignedWorkerIds = new string[0]  // Phase 3B placeholder
                        });
                    }
                    data.structures = structList.ToArray();
                } else {
                    data.structures = new StructureStateData[0];
                }

                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SaveFilePath, json);
                Debug.Log($"[SaveManager] Game Saved to: {SaveFilePath}");
            }
            catch (Exception e) { Debug.LogError($"[SaveManager] Save Failed: {e.Message}"); }
        }

        /// <summary>
        /// Loads game state from save file.
        /// Relies on GameManager's BootSequence to ensure all scene objects
        /// have registered themselves before this is called.
        /// </summary>
        public void LoadGame()
        {
            if (!File.Exists(SaveFilePath))
            {
                Debug.Log($"<color=yellow>[SaveManager]</color> No save file found at {SaveFilePath}. Starting new game.");
                return;
            }

            try {
                Debug.Log($"<color=cyan>[SaveManager]</color> Loading game from {SaveFilePath}...");

                string json = File.ReadAllText(SaveFilePath);
                GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

                // 1. Load Global State
                if (DayNightSystem.Instance != null)
                {
                    DayNightSystem.Instance.SetDay(data.globalState.currentDay);
                    Debug.Log($"<color=cyan>[SaveManager]</color> Restored day number: {data.globalState.currentDay}");
                }

                // 2. Load Resources
                if (ResourceSystem.Instance != null) {
                    foreach (var entry in data.resources.resourceAmounts) {
                        ResourceSystem.Instance.SetResourceAmount(entry.resourceId, entry.amount);
                    }
                    Debug.Log($"<color=cyan>[SaveManager]</color> Restored {data.resources.resourceAmounts.Count} resource types");
                }

                // 3. Load Structures (BEFORE Workers - dependency requirement)
                Debug.Log($"<color=lime>[SAVE-DEBUG]</color> Starting Structure Load... Found {data.structures?.Length ?? 0} structures in save data.");

                if (StructureSystem.Instance != null && data.structures != null) {
                    int restoredCount = 0;
                    int skippedCount = 0;

                    foreach (var sData in data.structures) {
                        Debug.Log($"<color=lime>[SAVE-DEBUG]</color> Attempting to restore structure: {sData.structureDataId} at grid ({sData.originCellX}, {sData.originCellY}). InstanceId: {sData.instanceId}");

                        StructureController restored = StructureSystem.Instance.RestoreStructureFromSave(sData);
                        if (restored != null) {
                            restoredCount++;
                        }
                        else
                        {
                            // FIX: Don't error on missing IDs - gracefully skip with warning
                            Debug.LogWarning($"<color=orange>[SAVE-DEBUG]</color> Skipped structure: {sData.structureDataId} (missing from database or invalid data)");
                            skippedCount++;
                        }
                    }

                    Debug.Log($"<color=green>[SaveManager]</color> Structure restoration complete: {restoredCount} restored, {skippedCount} skipped");
                }
                else
                {
                    Debug.LogWarning($"<color=yellow>[SAVE-DEBUG]</color> Structure restore skipped. StructureSystem ready: {StructureSystem.Instance != null}, structures in save: {data.structures != null}");
                }

                // 4. Load Workers
                if (WorkerSystem.Instance != null && data.workers != null) {
                    int restoredCount = 0;
                    foreach (var wData in data.workers) {
                        WorkerInstance restored = WorkerSystem.Instance.RestoreWorkerFromSave(wData);
                        if (restored != null) {
                            restoredCount++;
                        }
                    }
                    Debug.Log($"<color=green>[SaveManager]</color> Restored {restoredCount} workers");
                }

                Debug.Log("<color=green>[SaveManager] ✅ Game Loaded Successfully</color>");
            }
            catch (Exception e) {
                Debug.LogError($"<color=red>[SaveManager]</color> Load Failed: {e.Message}\n{e.StackTrace}");
            }
        }
    }

    [Serializable] public class GameSaveData {
        public GlobalStateData globalState = new GlobalStateData();
        public ResourcesStateData resources = new ResourcesStateData();
        public WorkerStateData[] workers;
        public StructureStateData[] structures;
    }
    [Serializable] public class GlobalStateData { public int currentDay; public int currentDayPhase; }
    [Serializable] public class ResourcesStateData { public List<ResourceAmountEntry> resourceAmounts = new List<ResourceAmountEntry>(); }
    [Serializable] public class ResourceAmountEntry { public string resourceId; public float amount; }
    // Worker save data
    [Serializable]
    public class WorkerStateData
    {
        // Identity
        public string instanceId;
        public string customName;
        public string workerDataId;      // WorkerData asset reference

        // Stats
        public float currentHealth;
        public float maxHealth;
        public int level;
        public float experience;

        // Job (Phase 2 - not yet implemented)
        public string currentJobId;

        // Assignments (Phase 3 - not yet implemented)
        public string assignedStructureId;
        public string assignedHomeId;

        // State
        public int currentState;         // (int)WorkerState
        public bool isAtWorksite;

        // Position
        public float posX, posY, posZ;
    }

    // Structure save data
    [Serializable]
    public class StructureStateData
    {
        // Identity
        public string instanceId;
        public string structureDataId;      // StructureData asset reference

        // Grid Position
        public int originCellX;
        public int originCellY;
        public int rotationStep;            // 0-3 (0=0°, 1=90°, 2=180°, 3=270°)

        // Stats
        public float currentHealth;
        public int currentLevel;
        public float buildProgress;         // 0.0 to 1.0
        public bool isOperational;

        // State
        public int structureState;          // (int)StructureState enum

        // Assignments (Phase 3B - not yet implemented)
        public string[] assignedWorkerIds;
    }
}
