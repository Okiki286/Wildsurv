using UnityEngine;
using Sirenix.OdinInspector;

namespace WildernessSurvival.Core.Data
{
    /// <summary>
    /// Enum per il modello di food economy.
    /// MVP: RecruitOnly (food solo per reclutamento).
    /// Futuro: RecruitAndUpkeep (food anche come costo di mantenimento worker).
    /// </summary>
    public enum FoodModel
    {
        [Tooltip("Food usato solo per reclutare nuovi worker")]
        RecruitOnly = 0,

        [Tooltip("Food usato sia per reclutamento che per upkeep giornaliero")]
        RecruitAndUpkeep = 1
    }

    /// <summary>
    /// ScriptableObject per configurare le regole dell'economia del villaggio.
    /// Centralizza tutti i parametri di bilanciamento per population, food, recruit.
    /// </summary>
    [CreateAssetMenu(fileName = "EconomyRules", menuName = "Wilderness/Data/Economy Rules")]
    public class EconomyRules : ScriptableObject
    {
        // ============================================
        // SINGLETON LOADER
        // ============================================

        private static EconomyRules _instance;
        public static EconomyRules Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<EconomyRules>("EconomyRules");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (_instance == null)
                    {
                        Debug.LogError("[EconomyRules] EconomyRules.asset not found in Resources folder!");
                    }
#endif
                }
                return _instance;
            }
        }

        // ============================================
        // FOOD MODEL CONFIGURATION
        // ============================================

        [TitleGroup("Food Model")]
        [EnumToggleButtons]
        [Tooltip("MVP: RecruitOnly. Quando attivi RecruitAndUpkeep, i worker consumeranno food ogni notte.")]
        [SerializeField] private FoodModel foodModel = FoodModel.RecruitOnly;

        public FoodModel FoodModel => foodModel;
        public bool IsUpkeepEnabled => foodModel == FoodModel.RecruitAndUpkeep;

        // ============================================
        // RECRUITMENT CONFIGURATION
        // ============================================

        [TitleGroup("Recruitment")]
        [BoxGroup("Recruitment/Costs")]
        [Tooltip("Costo base in Food per reclutare il primo worker aggiuntivo")]
        [SerializeField, Min(1)] private int baseRecruitCost = 40;

        [BoxGroup("Recruitment/Costs")]
        [Tooltip("Costo aggiuntivo per ogni worker già presente oltre il primo")]
        [SerializeField, Min(0)] private int costPerExtraWorker = 15;

        [BoxGroup("Recruitment/Limits")]
        [Tooltip("Massimo numero di worker reclutabili per giorno (0 = illimitato)")]
        [SerializeField, Min(0)] private int recruitPerDayCap = 1;

        [BoxGroup("Recruitment/Limits")]
        [Tooltip("Numero di worker iniziali al Day 1")]
        [SerializeField, Min(1)] private int startingWorkerCount = 2;

        public int BaseRecruitCost => baseRecruitCost;
        public int CostPerExtraWorker => costPerExtraWorker;
        public int RecruitPerDayCap => recruitPerDayCap;
        public int StartingWorkerCount => startingWorkerCount;

        // ============================================
        // HOUSING CONFIGURATION
        // ============================================

        [TitleGroup("Housing")]
        [Tooltip("Numero di beds default per ogni House structure")]
        [SerializeField, Min(1)] private int defaultBedsPerHouse = 2;

        public int DefaultBedsPerHouse => defaultBedsPerHouse;

        // ============================================
        // UPKEEP CONFIGURATION (Future-Proof)
        // ============================================

        [TitleGroup("Upkeep (Future)")]
        [InfoBox("Questi parametri sono attivi SOLO quando FoodModel = RecruitAndUpkeep", InfoMessageType.Info)]

        [BoxGroup("Upkeep (Future)/Costs")]
        [Tooltip("Food consumato per worker ogni notte")]
        [SerializeField, Min(1)] private int foodUpkeepPerWorker = 1;

        [BoxGroup("Upkeep (Future)/Penalties")]
        [Tooltip("Moltiplicatore velocità lavoro quando affamati (es. 0.8 = -20%)")]
        [SerializeField, Range(0f, 1f)] private float starvationWorkSpeedMult = 0.8f;

        [BoxGroup("Upkeep (Future)/Penalties")]
        [Tooltip("Se true, non puoi reclutare quando hai worker affamati")]
        [SerializeField] private bool starvationBlocksRecruit = true;

        public int FoodUpkeepPerWorker => foodUpkeepPerWorker;
        public float StarvationWorkSpeedMult => starvationWorkSpeedMult;
        public bool StarvationBlocksRecruit => starvationBlocksRecruit;

        // ============================================
        // HELPER METHODS
        // ============================================

        /// <summary>
        /// Calcola il costo in Food per reclutare un nuovo worker
        /// dato il numero attuale di worker (esclusi quelli iniziali).
        /// Formula: baseRecruitCost + (currentWorkers - startingWorkerCount) * costPerExtraWorker
        /// </summary>
        public int CalculateRecruitCost(int currentWorkerCount)
        {
            int extraWorkers = Mathf.Max(0, currentWorkerCount - startingWorkerCount);
            return baseRecruitCost + (extraWorkers * costPerExtraWorker);
        }

        /// <summary>
        /// Calcola il food totale necessario per upkeep notturno.
        /// Ritorna 0 se FoodModel != RecruitAndUpkeep.
        /// </summary>
        public int CalculateTotalUpkeep(int workerCount)
        {
            if (foodModel != FoodModel.RecruitAndUpkeep) return 0;
            return workerCount * foodUpkeepPerWorker;
        }

        // ============================================
        // EDITOR VALIDATION
        // ============================================

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (baseRecruitCost < 1) baseRecruitCost = 1;
            if (costPerExtraWorker < 0) costPerExtraWorker = 0;
            if (startingWorkerCount < 1) startingWorkerCount = 1;
            if (foodUpkeepPerWorker < 1) foodUpkeepPerWorker = 1;
        }

        [TitleGroup("Debug Preview")]
        [ShowInInspector, ReadOnly]
        private string PreviewRecruitCost3Workers => $"Costo 3° worker: {CalculateRecruitCost(2)} Food";

        [ShowInInspector, ReadOnly]
        private string PreviewRecruitCost5Workers => $"Costo 5° worker: {CalculateRecruitCost(4)} Food";

        [ShowInInspector, ReadOnly]
        private string PreviewUpkeep5Workers => IsUpkeepEnabled
            ? $"Upkeep 5 workers: {CalculateTotalUpkeep(5)} Food/notte"
            : "Upkeep: DISABILITATO (RecruitOnly mode)";
#endif
    }
}
