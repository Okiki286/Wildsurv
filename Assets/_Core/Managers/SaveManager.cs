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

        // Manual Save
        private const string SAVE_FILENAME = "savegame.json";
        private string SaveFilePath => Path.Combine(Application.persistentDataPath, SAVE_FILENAME);

        // Rolling Auto-Save (3 Slots)
        private const int AUTO_SAVE_SLOT_COUNT = 3;
        private const string AUTO_SAVE_PREFIX = "autosave_";
        private const string AUTO_SAVE_INDEX_KEY = "LastAutoSaveIndex"; // PlayerPrefs key

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Check if a valid save file exists.
        /// Used by GameManager.BootSequence() to determine whether to load or start new game.
        /// </summary>
        /// <returns>True if save file exists, false otherwise</returns>
        public bool HasSaveFile()
        {
            bool exists = File.Exists(SaveFilePath);

            if (exists)
            {
                Debug.Log($"<color=cyan>[SaveManager]</color> Save file found at: {SaveFilePath}");
            }
            else
            {
                Debug.Log($"<color=yellow>[SaveManager]</color> No save file found at: {SaveFilePath}");
            }

            return exists;
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
        /// Performs a rolling auto-save across 3 slots.
        /// Cycles through autosave_0.json, autosave_1.json, autosave_2.json.
        /// Index is persisted via PlayerPrefs to survive game sessions.
        /// </summary>
        /// <returns>The slot index (0-2) that was written, or -1 on failure</returns>
        public int PerformRollingAutoSave()
        {
            try
            {
                // 1. Get current index from PlayerPrefs (default: 0)
                int currentIndex = PlayerPrefs.GetInt(AUTO_SAVE_INDEX_KEY, 0);

                // 2. Generate filename for this slot
                string filename = $"{AUTO_SAVE_PREFIX}{currentIndex}.json";
                string filePath = Path.Combine(Application.persistentDataPath, filename);

                // 3. Collect game state data (same as SaveGame)
                GameSaveData data = new GameSaveData();

                // Save Global State (Time)
                if (DayNightSystem.Instance != null)
                {
                    data.globalState.currentDay = DayNightSystem.Instance.CurrentDayNumber;
                    data.globalState.currentDayPhase = (int)DayNightSystem.Instance.CurrentDayPhase;
                }

                // Save Resources
                if (ResourceSystem.Instance != null)
                {
                    foreach (var res in ResourceSystem.Instance.GetAllResourceData())
                    {
                        float amount = ResourceSystem.Instance.GetResourceAmount(res.ResourceId);
                        data.resources.resourceAmounts.Add(new ResourceAmountEntry { resourceId = res.ResourceId, amount = amount });
                    }
                }

                // Save Workers
                if (WorkerSystem.Instance != null)
                {
                    var allWorkers = WorkerSystem.Instance.AllWorkerInstances;
                    var workerList = new List<WorkerStateData>();

                    foreach (var w in allWorkers)
                    {
                        var state = new WorkerStateData
                        {
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
                            assignedStructureId = "",
                            assignedHomeId = ""
                        };

                        if (w.PhysicalWorker != null)
                        {
                            Vector3 pos = w.PhysicalWorker.transform.position;
                            state.posX = pos.x;
                            state.posY = pos.y;
                            state.posZ = pos.z;
                        }

                        workerList.Add(state);
                    }
                    data.workers = workerList.ToArray();
                }
                else
                {
                    data.workers = new WorkerStateData[0];
                }

                // Save Structures
                if (StructureSystem.Instance != null)
                {
                    var allStructures = StructureSystem.Instance.AllStructures;
                    var structList = new List<StructureStateData>();

                    foreach (var s in allStructures)
                    {
                        structList.Add(new StructureStateData
                        {
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
                            assignedWorkerIds = new string[0]
                        });
                    }
                    data.structures = structList.ToArray();
                }
                else
                {
                    data.structures = new StructureStateData[0];
                }

                // 4. Write to file
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(filePath, json);

                // 5. Calculate next index using modulo (0 → 1 → 2 → 0)
                int nextIndex = (currentIndex + 1) % AUTO_SAVE_SLOT_COUNT;
                PlayerPrefs.SetInt(AUTO_SAVE_INDEX_KEY, nextIndex);
                PlayerPrefs.Save(); // Force write to disk

                Debug.Log($"<color=green>[SaveManager]</color> ✅ Rolling Auto-Save to Slot {currentIndex}: {filePath}");
                Debug.Log($"<color=cyan>[SaveManager]</color> Next auto-save will use Slot {nextIndex}");

                return currentIndex;
            }
            catch (Exception e)
            {
                Debug.LogError($"<color=red>[SaveManager]</color> Rolling Auto-Save Failed: {e.Message}\n{e.StackTrace}");
                return -1;
            }
        }

        /// <summary>
        /// Gets information about all auto-save slots.
        /// Useful for building a Load Game UI that shows all available saves.
        /// </summary>
        /// <returns>Array of tuples (slotIndex, filePath, exists, dayNumber). Day is -1 if file doesn't exist.</returns>
        public (int slotIndex, string filePath, bool exists, int dayNumber)[] GetAutoSaveSlotInfo()
        {
            var slotInfo = new (int, string, bool, int)[AUTO_SAVE_SLOT_COUNT];

            for (int i = 0; i < AUTO_SAVE_SLOT_COUNT; i++)
            {
                string filename = $"{AUTO_SAVE_PREFIX}{i}.json";
                string filePath = Path.Combine(Application.persistentDataPath, filename);
                bool exists = File.Exists(filePath);
                int dayNumber = -1;

                // Try to read day number from file if it exists
                if (exists)
                {
                    try
                    {
                        string json = File.ReadAllText(filePath);
                        GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
                        dayNumber = data.globalState.currentDay;
                    }
                    catch
                    {
                        // If we can't read the file, just mark it as corrupt (day -1)
                        dayNumber = -1;
                    }
                }

                slotInfo[i] = (i, filePath, exists, dayNumber);
            }

            return slotInfo;
        }

        /// <summary>
        /// Loads game state from save file.
        /// Relies on GameManager's BootSequence to ensure all scene objects
        /// have registered themselves before this is called.
        /// </summary>
        /// <param name="customFilename">Optional: Specific filename to load (e.g., "autosave_0.json"). If null, loads default "savegame.json"</param>
        public void LoadGame(string customFilename = null)
        {
            // Determine which file to load
            string fileToLoad;
            if (string.IsNullOrEmpty(customFilename))
            {
                fileToLoad = SaveFilePath; // Default: savegame.json
            }
            else
            {
                fileToLoad = Path.Combine(Application.persistentDataPath, customFilename);
            }

            if (!File.Exists(fileToLoad))
            {
                Debug.Log($"<color=yellow>[SaveManager]</color> No save file found at {fileToLoad}. Starting new game.");
                return;
            }

            try {
                Debug.Log($"<color=cyan>[SaveManager]</color> Loading game from {fileToLoad}...");

                string json = File.ReadAllText(fileToLoad);
                GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

                // [CRITICAL FIX] Clear existing status before loading to prevent duplication
                if (WorkerSystem.Instance != null)
                {
                    WorkerSystem.Instance.ClearAllWorkers();
                }

                if (PopulationSystem.Instance != null)
                {
                    PopulationSystem.Instance.ResetRecruits();
                }

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
