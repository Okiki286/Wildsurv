using UnityEngine;
using System;
using Sirenix.OdinInspector;
using WildernessSurvival.Core.Data;
using WildernessSurvival.Core.Events;
using WildernessSurvival.Gameplay.Workers;
using WildernessSurvival.Gameplay.Structures.Housing;
using WildernessSurvival.Gameplay.Resources;

namespace WildernessSurvival.Core.Systems
{
    /// <summary>
    /// Reason codes for why recruitment is blocked.
    /// </summary>
    public enum RecruitBlockReason
    {
        None = 0,
        NotEnoughFood,
        NoHousingCapacity,
        DailyCapReached,
        StarvingWorkers,
        SystemNotReady
    }

    /// <summary>
    /// Sistema che gestisce la popolazione del villaggio.
    /// - Calcola housing capacity (somma beds delle case operative)
    /// - Gestisce recruit queue con costo Food
    /// - Spawna worker al DayStarted
    /// - Future-proof per upkeep system
    /// </summary>
    public class PopulationSystem : MonoBehaviour
    {
        // ============================================
        // SINGLETON
        // ============================================

        public static PopulationSystem Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            Instance = null;
        }

        // ============================================
        // CONFIGURATION
        // ============================================

        [TitleGroup("Events")]
        [SerializeField] private GameEvent onDayStarted;
        [SerializeField] private GameEvent onNightStarted;

        [TitleGroup("Recruitment")]
        [Tooltip("WorkerData per i nuovi recruit. Se null, tenta Resources.Load('Workers/Villager').")] 
        [SerializeField] private WorkerData recruitWorkerData;

        [TitleGroup("Debug")]
        [SerializeField] private bool debugMode = true;

        // ============================================
        // RUNTIME STATE
        // ============================================

        [TitleGroup("Runtime Status")]
        [ShowInInspector, ReadOnly]
        private int queuedRecruits = 0;

        [ShowInInspector, ReadOnly]
        private int recruitsSpawnedToday = 0;

        [ShowInInspector, ReadOnly]
        private bool starvingToday = false;

        // ============================================
        // PROPERTIES
        // ============================================

        public int WorkerCount => WorkerSystem.Instance != null ? WorkerSystem.Instance.WorkerInstanceCount : 0;
        public int QueuedRecruits => queuedRecruits;
        public bool HasQueuedRecruits => queuedRecruits > 0;
        public int RecruitsSpawnedToday => recruitsSpawnedToday;
        public bool IsStarving => starvingToday;

        /// <summary>
        /// Calcola la capacità totale di housing dalle case operative.
        /// </summary>
        public int HousingCapacity
        {
            get
            {
                int capacity = 0;

                // Trova tutte le ShelterHome operative
                var shelters = FindObjectsByType<ShelterHome>(FindObjectsSortMode.None);
                for (int i = 0; i < shelters.Length; i++)
                {
                    if (shelters[i] != null && shelters[i].IsOperational)
                    {
                        capacity += shelters[i].Capacity;
                    }
                }

                return capacity;
            }
        }

        /// <summary>
        /// Posti disponibili per nuovi worker.
        /// </summary>
        public int AvailableHousingSlots => Mathf.Max(0, HousingCapacity - WorkerCount - queuedRecruits);

        // ============================================
        // EVENTS
        // ============================================

        /// <summary>Invocato quando un worker viene spawnato dalla queue.</summary>
        public event Action<WorkerInstance> OnWorkerRecruited;

        /// <summary>Invocato quando la capacità housing cambia (struttura costruita/distrutta).</summary>
        public event Action<int> OnHousingCapacityChanged;

        /// <summary>Invocato quando lo stato starving cambia.</summary>
        public event Action<bool> OnStarvationStateChanged;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            if (onDayStarted != null)
                onDayStarted.AddListener(HandleDayStarted);

            if (onNightStarted != null)
                onNightStarted.AddListener(HandleNightStarted);
        }

        private void OnDisable()
        {
            if (onDayStarted != null)
                onDayStarted.RemoveListener(HandleDayStarted);

            if (onNightStarted != null)
                onNightStarted.RemoveListener(HandleNightStarted);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ============================================
        // PUBLIC API - RECRUITMENT
        // ============================================

        /// <summary>
        /// Verifica se il player può reclutare un nuovo worker.
        /// </summary>
        /// <param name="blockReason">Motivo del blocco se ritorna false.</param>
        /// <returns>True se può reclutare.</returns>
        public bool CanRecruit(out RecruitBlockReason blockReason)
        {
            blockReason = RecruitBlockReason.None;

            // System check
            if (WorkerSystem.Instance == null || ResourceSystem.Instance == null)
            {
                blockReason = RecruitBlockReason.SystemNotReady;
                return false;
            }

            var rules = EconomyRules.Instance;
            if (rules == null)
            {
                blockReason = RecruitBlockReason.SystemNotReady;
                return false;
            }

            // Daily cap check
            if (rules.RecruitPerDayCap > 0 && recruitsSpawnedToday >= rules.RecruitPerDayCap)
            {
                blockReason = RecruitBlockReason.DailyCapReached;
                return false;
            }

            // Starvation check (solo se upkeep attivo)
            if (rules.IsUpkeepEnabled && rules.StarvationBlocksRecruit && starvingToday)
            {
                blockReason = RecruitBlockReason.StarvingWorkers;
                return false;
            }

            // Housing capacity check
            if (AvailableHousingSlots <= 0)
            {
                blockReason = RecruitBlockReason.NoHousingCapacity;
                return false;
            }

            // Food check
            int cost = RecruitCost();
            int currentFood = Mathf.FloorToInt(ResourceSystem.Instance.GetResourceAmount("food"));
            if (currentFood < cost)
            {
                blockReason = RecruitBlockReason.NotEnoughFood;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Overload semplificato senza reason.
        /// </summary>
        public bool CanRecruit()
        {
            return CanRecruit(out _);
        }

        /// <summary>
        /// Calcola il costo attuale per reclutare un worker.
        /// </summary>
        public int RecruitCost()
        {
            var rules = EconomyRules.Instance;
            if (rules == null) return 999;

            // Include queued recruits nel conteggio
            int effectiveWorkerCount = WorkerCount + queuedRecruits;
            return rules.CalculateRecruitCost(effectiveWorkerCount);
        }

        /// <summary>
        /// Richiede un nuovo recruit. Paga Food e mette in coda.
        /// Il worker verrà spawnato al prossimo DayStarted.
        /// </summary>
        /// <returns>True se la richiesta è stata accettata.</returns>
        public bool RequestRecruit()
        {
            if (!CanRecruit(out RecruitBlockReason reason))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (debugMode)
                {
                    Debug.Log($"<color=orange>[Population]</color> RequestRecruit BLOCKED: {reason}");
                }
#endif
                return false;
            }

            int cost = RecruitCost();

            // Pay food
            var foodCost = new ResourceCost[] { new ResourceCost { resourceId = "food", amount = cost } };
            if (!ResourceSystem.Instance.PayCost(foodCost))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (debugMode)
                {
                    Debug.LogWarning($"<color=red>[Population]</color> PayCost failed for {cost} food!");
                }
#endif
                return false;
            }

            // Add to queue
            queuedRecruits++;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
            {
                Debug.Log($"<color=green>[Population]</color> RequestRecruit OK: cost={cost} food, queue={queuedRecruits}");
            }
#endif

            return true;
        }

        /// <summary>
        /// Ottiene il testo descrittivo per il motivo di blocco recruit.
        /// </summary>
        public string GetBlockReasonText(RecruitBlockReason reason)
        {
            switch (reason)
            {
                case RecruitBlockReason.NotEnoughFood:
                    return $"Not enough Food (need {RecruitCost()})";
                case RecruitBlockReason.NoHousingCapacity:
                    return "No housing available";
                case RecruitBlockReason.DailyCapReached:
                    return "Daily recruit limit reached";
                case RecruitBlockReason.StarvingWorkers:
                    return "Workers are starving!";
                case RecruitBlockReason.SystemNotReady:
                    return "System not ready";
                default:
                    return "";
            }
        }

        // ============================================
        // PUBLIC API - UPKEEP (Future-Proof Stub)
        // ============================================

        /// <summary>
        /// Consuma food per upkeep notturno.
        /// STUB: in MVP (FoodModel.RecruitOnly) non fa nulla.
        /// Quando attivi RecruitAndUpkeep, implementa shortage logic.
        /// </summary>
        public void ConsumeFoodUpkeep()
        {
            var rules = EconomyRules.Instance;
            if (rules == null) return;

            // MVP: RecruitOnly mode - skip upkeep
            if (!rules.IsUpkeepEnabled)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (debugMode)
                {
                    Debug.Log("<color=gray>[Population]</color> ConsumeFoodUpkeep: skipped (RecruitOnly mode)");
                }
#endif
                return;
            }

            // ===== FUTURE: RecruitAndUpkeep implementation =====
            int totalUpkeep = rules.CalculateTotalUpkeep(WorkerCount);
            if (totalUpkeep <= 0) return;

            int currentFood = Mathf.FloorToInt(ResourceSystem.Instance.GetResourceAmount("food"));

            if (currentFood >= totalUpkeep)
            {
                // Pay full upkeep
                var upkeepCost = new ResourceCost[] { new ResourceCost { resourceId = "food", amount = totalUpkeep } };
                ResourceSystem.Instance.PayCost(upkeepCost);
                SetStarvingState(false);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (debugMode)
                {
                    Debug.Log($"<color=cyan>[Population]</color> Upkeep paid: {totalUpkeep} food for {WorkerCount} workers");
                }
#endif
            }
            else
            {
                // Pay what we can, set starving
                if (currentFood > 0)
                {
                    var partialCost = new ResourceCost[] { new ResourceCost { resourceId = "food", amount = currentFood } };
                    ResourceSystem.Instance.PayCost(partialCost);
                }
                SetStarvingState(true);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (debugMode)
                {
                    Debug.LogWarning($"<color=red>[Population]</color> STARVATION! Need {totalUpkeep} food, have {currentFood}. Workers are starving.");
                }
#endif
            }
        }

        private void SetStarvingState(bool starving)
        {
            if (starvingToday == starving) return;

            starvingToday = starving;
            OnStarvationStateChanged?.Invoke(starving);
        }

        /// <summary>
        /// Ottiene il moltiplicatore velocità lavoro corrente.
        /// Ritorna 1.0 se non starving, altrimenti StarvationWorkSpeedMult.
        /// </summary>
        public float GetWorkSpeedMultiplier()
        {
            var rules = EconomyRules.Instance;
            if (rules == null) return 1f;

            if (rules.IsUpkeepEnabled && starvingToday)
            {
                return rules.StarvationWorkSpeedMult;
            }
            return 1f;
        }

        // ============================================
        // DAY/NIGHT HANDLERS
        // ============================================

        private void HandleDayStarted()
        {
            // Reset daily counters
            recruitsSpawnedToday = 0;

            // Clear starvation (can eat again during day)
            if (starvingToday)
            {
                SetStarvingState(false);
            }

            // Spawn queued recruits
            SpawnQueuedRecruits();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
            {
                Debug.Log($"<color=yellow>[Population]</color> DayStarted: workers={WorkerCount}, capacity={HousingCapacity}, queue={queuedRecruits}");
            }
#endif
        }

        private void HandleNightStarted()
        {
            // Consume food upkeep (stub in MVP)
            ConsumeFoodUpkeep();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugMode)
            {
                Debug.Log($"<color=blue>[Population]</color> NightStarted: starving={starvingToday}");
            }
#endif
        }

        private void SpawnQueuedRecruits()
        {
            if (queuedRecruits <= 0) return;
            if (WorkerSystem.Instance == null) return;

            var rules = EconomyRules.Instance;
            int maxSpawnThisDay = rules != null && rules.RecruitPerDayCap > 0
                ? rules.RecruitPerDayCap
                : int.MaxValue;

            int spawnedThisCall = 0;

            while (queuedRecruits > 0 && recruitsSpawnedToday < maxSpawnThisDay)
            {
                // Check housing (in case structures were destroyed overnight)
                if (AvailableHousingSlots + 1 <= 0) // +1 because we're about to spawn
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (debugMode)
                    {
                        Debug.LogWarning($"<color=orange>[Population]</color> Cannot spawn recruit: no housing available!");
                    }
#endif
                    break;
                }

                // Get worker data from WorkerSystem
                var workerData = GetDefaultWorkerData();
                if (workerData == null)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogError("[Population] No WorkerData available for spawning!");
#endif
                    break;
                }

                // Spawn near Waystone
                Vector3 spawnPos = GetRecruitSpawnPosition();
                WorkerInstance newWorker = SpawnWorkerAtPosition(workerData, spawnPos);

                if (newWorker != null)
                {
                    queuedRecruits--;
                    recruitsSpawnedToday++;
                    spawnedThisCall++;

                    OnWorkerRecruited?.Invoke(newWorker);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (debugMode)
                    {
                        Debug.Log($"<color=green>[Population]</color> DayStarted spawn worker: name={newWorker.CustomName} workers={WorkerCount}/{HousingCapacity}");
                    }
#endif
                }
                else
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogError("[Population] Failed to spawn worker!");
#endif
                    break;
                }
            }

            if (spawnedThisCall > 0 && queuedRecruits > 0)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (debugMode)
                {
                    Debug.Log($"<color=yellow>[Population]</color> {queuedRecruits} recruits still queued (daily cap reached)");
                }
#endif
            }
        }

        private WorkerData GetDefaultWorkerData()
        {
            // 1. Try direct reference (most robust)
            if (recruitWorkerData != null)
            {
                return recruitWorkerData;
            }

            // 2. Try WorkerSystem's available types
            if (WorkerSystem.Instance != null)
            {
                var types = WorkerSystem.Instance.GetType().GetField("availableWorkerTypes", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (types != null)
                {
                    var list = types.GetValue(WorkerSystem.Instance) as System.Collections.Generic.List<WorkerData>;
                    if (list != null && list.Count > 0)
                    {
                        return list[0];
                    }
                }
            }

            // 3. Fallback to Resources.Load
            var loaded = Resources.Load<WorkerData>("Workers/Villager");
            if (loaded != null)
            {
                return loaded;
            }

            // 4. FAIL - log detailed error
            Debug.LogError("[Population] CRITICAL: No WorkerData available for recruitment!\n" +
                "Fix options:\n" +
                "  1. Assign 'recruitWorkerData' in PopulationSystem Inspector\n" +
                "  2. Add WorkerData to WorkerSystem's 'availableWorkerTypes'\n" +
                "  3. Create 'Assets/_Core/Resources/Workers/Villager.asset'");
            return null;
        }

        private Vector3 GetRecruitSpawnPosition()
        {
            // Try to find Waystone
            var waystone = FindFirstObjectByType<WildernessSurvival.Gameplay.Core.WaystoneBeaconController>();
            if (waystone != null)
            {
                // Spawn near waystone with random offset
                Vector3 center = waystone.transform.position;
                Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * 3f;
                randomOffset.y = 0;
                Vector3 targetPos = center + randomOffset;

                // Snap to NavMesh
                if (UnityEngine.AI.NavMesh.SamplePosition(targetPos, out UnityEngine.AI.NavMeshHit hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    return hit.position;
                }
                return center;
            }

            // Fallback to world origin
            return Vector3.zero;
        }

        private WorkerInstance SpawnWorkerAtPosition(WorkerData data, Vector3 position)
        {
            if (data == null || WorkerSystem.Instance == null) return null;

            // Use WorkerSystem's CreateWorkerInstance but override spawn position
            // This is a workaround - ideally WorkerSystem.CreateWorkerInstance would accept position
            WorkerInstance worker = WorkerSystem.Instance.CreateWorkerInstance(data);

            // Move to spawn position
            if (worker != null && worker.PhysicalWorker != null)
            {
                worker.PhysicalWorker.transform.position = position;
            }

            return worker;
        }

        // ============================================
        // DEBUG TOOLS
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug Actions")]
        [ButtonGroup("Debug Actions/Row1")]
        [Button("Test Recruit", ButtonSizes.Medium)]
        [GUIColor(0.4f, 0.8f, 0.4f)]
        private void DebugTestRecruit()
        {
            if (Application.isPlaying)
            {
                if (CanRecruit(out RecruitBlockReason reason))
                {
                    RequestRecruit();
                }
                else
                {
                    Debug.Log($"[Population] Cannot recruit: {GetBlockReasonText(reason)}");
                }
            }
        }

        [ButtonGroup("Debug Actions/Row1")]
        [Button("Force Spawn Queue", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.8f, 0.4f)]
        private void DebugForceSpawnQueue()
        {
            if (Application.isPlaying && queuedRecruits > 0)
            {
                SpawnQueuedRecruits();
            }
        }

        [ButtonGroup("Debug Actions/Row2")]
        [Button("Add 100 Food", ButtonSizes.Medium)]
        private void DebugAddFood()
        {
            if (Application.isPlaying && ResourceSystem.Instance != null)
            {
                ResourceSystem.Instance.AddResource("food", 100);
                Debug.Log("[Population] Added 100 food");
            }
        }

        [ButtonGroup("Debug Actions/Row2")]
        [Button("Print Status", ButtonSizes.Medium)]
        private void DebugPrintStatus()
        {
            CanRecruit(out RecruitBlockReason reason);
            Debug.Log($"=== POPULATION STATUS ===\n" +
                $"Workers: {WorkerCount}\n" +
                $"Housing Capacity: {HousingCapacity}\n" +
                $"Available Slots: {AvailableHousingSlots}\n" +
                $"Queued Recruits: {queuedRecruits}\n" +
                $"Recruits Today: {recruitsSpawnedToday}\n" +
                $"Recruit Cost: {RecruitCost()} food\n" +
                $"Can Recruit: {reason == RecruitBlockReason.None} ({reason})\n" +
                $"Starving: {starvingToday}");
        }

        [ButtonGroup("Debug Actions/Row3")]
        [Button("Toggle Starvation", ButtonSizes.Medium)]
        [GUIColor(1f, 0.5f, 0.5f)]
        private void DebugToggleStarvation()
        {
            if (Application.isPlaying)
            {
                SetStarvingState(!starvingToday);
                Debug.Log($"[Population] Starvation set to: {starvingToday}");
            }
        }
#endif
    }
}
