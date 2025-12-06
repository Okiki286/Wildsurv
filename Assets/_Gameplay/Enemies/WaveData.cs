using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

namespace WildernessSurvival.Gameplay.Enemies
{
    /// <summary>
    /// Definisce una ondata di nemici.
    /// Può essere usata come template o singola wave.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWave", menuName = "Wilderness Survival/Data/Wave Definition")]
    public class WaveData : ScriptableObject
    {
        // ============================================
        // IDENTIFICAZIONE
        // ============================================
        
        [TitleGroup("Wave Info")]
        [HorizontalGroup("Wave Info/Row1")]
        [LabelWidth(100)]
        [SerializeField] private string waveId;
        
        [HorizontalGroup("Wave Info/Row1")]
        [LabelWidth(80)]
        [PropertyRange(1, 50)]
        [SerializeField] private int waveNumber = 1;

        [TextArea(1, 2)]
        [SerializeField] private string description;

        // ============================================
        // CONFIGURAZIONE WAVE
        // ============================================

        [TitleGroup("Configurazione")]
        [BoxGroup("Configurazione/Timing")]
        [HorizontalGroup("Configurazione/Timing/Row")]
        [LabelWidth(120)]
        [SuffixLabel("sec", Overlay = true)]
        [Tooltip("Tempo prima dell'inizio della wave")]
        [SerializeField] private float preparationTime = 10f;
        
        [HorizontalGroup("Configurazione/Timing/Row")]
        [LabelWidth(100)]
        [SuffixLabel("sec", Overlay = true)]
        [Tooltip("Durata totale dello spawn")]
        [SerializeField] private float spawnDuration = 30f;

        [BoxGroup("Configurazione/Spawn Points")]
        [ToggleLeft]
        #pragma warning disable CS0414 // Reserved for future random spawn point feature
        [SerializeField] private bool useRandomSpawnPoints = true;
        #pragma warning restore CS0414
        
        [BoxGroup("Configurazione/Spawn Points")]
        [HideIf("useRandomSpawnPoints")]
        [SerializeField] private Transform[] specificSpawnPoints;

        // ============================================
        // NEMICI
        // ============================================

        [TitleGroup("Nemici")]
        [InfoBox("Definisci i gruppi di nemici per questa wave")]
        [TableList(AlwaysExpanded = true, ShowIndexLabels = true)]
        [SerializeField] private WaveEnemyGroup[] enemyGroups;

        // ============================================
        // SCALING
        // ============================================

        [TitleGroup("Scaling Multipliers")]
        [InfoBox("Moltiplicatori applicati a tutti i nemici di questa wave")]
        [BoxGroup("Scaling Multipliers/Stats")]
        [HorizontalGroup("Scaling Multipliers/Stats/Row1")]
        [LabelWidth(100)]
        [PropertyRange(0.5f, 5f)]
        [SerializeField] private float healthMultiplier = 1f;
        
        [HorizontalGroup("Scaling Multipliers/Stats/Row1")]
        [LabelWidth(100)]
        [PropertyRange(0.5f, 3f)]
        [SerializeField] private float damageMultiplier = 1f;

        [HorizontalGroup("Scaling Multipliers/Stats/Row2")]
        [LabelWidth(100)]
        [PropertyRange(0.5f, 2f)]
        [SerializeField] private float speedMultiplier = 1f;
        
        [HorizontalGroup("Scaling Multipliers/Stats/Row2")]
        [LabelWidth(100)]
        [PropertyRange(0.5f, 3f)]
        [SerializeField] private float rewardMultiplier = 1f;

        // ============================================
        // EVENTI SPECIALI
        // ============================================

        [TitleGroup("Eventi Speciali")]
        [BoxGroup("Eventi Speciali/Boss")]
        [ToggleLeft]
        [SerializeField] private bool hasBoss = false;
        
        [BoxGroup("Eventi Speciali/Boss")]
        [ShowIf("hasBoss")]
        [SerializeField] private EnemyData bossEnemy;
        
        [BoxGroup("Eventi Speciali/Boss")]
        [ShowIf("hasBoss")]
        [LabelWidth(120)]
        [Tooltip("Spawn boss dopo X% della wave")]
        [PropertyRange(0, 100)]
        #pragma warning disable CS0414 // Reserved for future boss spawn timing feature
        [SerializeField] private float bossSpawnAtPercent = 80f;
        #pragma warning restore CS0414

        [BoxGroup("Eventi Speciali/Hazards")]
        [ToggleLeft]
        #pragma warning disable CS0414 // Reserved for future environmental hazard feature
        [SerializeField] private bool hasEnvironmentalHazard = false;
        
        [BoxGroup("Eventi Speciali/Hazards")]
        [ShowIf("hasEnvironmentalHazard")]
        [EnumToggleButtons]
        [SerializeField] private HazardType hazardType = HazardType.None;
        #pragma warning restore CS0414

        // ============================================
        // RICOMPENSE WAVE
        // ============================================

        [TitleGroup("Ricompense Completamento")]
        [TableList]
        [SerializeField] private WaveReward[] completionRewards;

        // ============================================
        // PROPERTIES
        // ============================================

        public string WaveId => waveId;
        public int WaveNumber => waveNumber;
        public string Description => description;
        public float PreparationTime => preparationTime;
        public float SpawnDuration => spawnDuration;
        public WaveEnemyGroup[] EnemyGroups => enemyGroups;
        public float HealthMultiplier => healthMultiplier;
        public float DamageMultiplier => damageMultiplier;
        public float SpeedMultiplier => speedMultiplier;
        public float RewardMultiplier => rewardMultiplier;
        public bool HasBoss => hasBoss;
        public EnemyData BossEnemy => bossEnemy;
        public WaveReward[] CompletionRewards => completionRewards;

        // ============================================
        // METODI
        // ============================================

        /// <summary>
        /// Calcola il numero totale di nemici nella wave
        /// </summary>
        public int GetTotalEnemyCount()
        {
            int total = 0;
            if (enemyGroups != null)
            {
                foreach (var group in enemyGroups)
                {
                    total += group.count;
                }
            }
            if (hasBoss) total++;
            return total;
        }

        /// <summary>
        /// Calcola difficoltà stimata
        /// </summary>
        public float GetDifficultyRating()
        {
            float rating = 0f;
            
            if (enemyGroups != null)
            {
                foreach (var group in enemyGroups)
                {
                    if (group.enemyData != null)
                    {
                        float enemyRating = group.enemyData.BaseHealth * 0.01f + 
                                           group.enemyData.AttackDamage * 0.1f;
                        rating += enemyRating * group.count;
                    }
                }
            }
            
            rating *= healthMultiplier * damageMultiplier;
            
            if (hasBoss) rating *= 1.5f;
            
            return rating;
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(waveId))
            {
                waveId = $"wave_{waveNumber:D2}";
            }
        }

        [TitleGroup("Debug")]
        [Button("📊 Wave Summary", ButtonSizes.Medium)]
        private void DebugSummary()
        {
            Debug.Log($"=== Wave {waveNumber}: {waveId} ===\n" +
                $"Total Enemies: {GetTotalEnemyCount()}\n" +
                $"Difficulty Rating: {GetDifficultyRating():F1}\n" +
                $"Duration: {spawnDuration}s\n" +
                $"Has Boss: {hasBoss}");
            
            if (enemyGroups != null)
            {
                Debug.Log("Enemy Groups:");
                foreach (var group in enemyGroups)
                {
                    string name = group.enemyData != null ? group.enemyData.DisplayName : "NULL";
                    Debug.Log($"  - {name} x{group.count}");
                }
            }
        }
        #endif
    }

    // ============================================
    // ENUMS
    // ============================================

    public enum HazardType
    {
        None,
        Fog,
        Storm,
        Earthquake,
        BloodMoon,
        Eclipse
    }

    // ============================================
    // STRUCTS
    // ============================================

    [System.Serializable]
    public struct WaveEnemyGroup
    {
        [HorizontalGroup("Row", 0.4f)]
        [HideLabel]
        [Tooltip("Tipo di nemico")]
        public EnemyData enemyData;
        
        [HorizontalGroup("Row", 0.15f)]
        [HideLabel]
        [Tooltip("Quantità")]
        [PropertyRange(1, 100)]
        public int count;
        
        [HorizontalGroup("Row", 0.2f)]
        [HideLabel]
        [SuffixLabel("sec delay")]
        [Tooltip("Ritardo spawn dal gruppo precedente")]
        public float spawnDelay;
        
        [HorizontalGroup("Row", 0.25f)]
        [HideLabel]
        [SuffixLabel("sec interval")]
        [Tooltip("Intervallo tra ogni spawn")]
        public float spawnInterval;
    }

    [System.Serializable]
    public struct WaveReward
    {
        [HorizontalGroup("Row", 0.6f)]
        [HideLabel]
        [ValueDropdown("GetResourceIds")]
        public string resourceId;
        
        [HorizontalGroup("Row", 0.4f)]
        [HideLabel]
        [SuffixLabel("x", Overlay = true)]
        public int amount;

        #if UNITY_EDITOR
        private static IEnumerable<string> GetResourceIds()
        {
            return new List<string> { "warmwood", "food", "shards", "bronze_ore", "iron_ore" };
        }
        #endif
    }
}
