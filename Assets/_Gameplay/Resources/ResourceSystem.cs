using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using WildernessSurvival.Core.Events;

namespace WildernessSurvival.Gameplay.Resources
{
    /// <summary>
    /// Sistema centrale per la gestione di tutte le risorse.
    /// Singleton accessibile da qualsiasi sistema.
    /// VERSIONE FINALE - Compatibile 100% con ResourceData esistente
    /// </summary>
    public class ResourceSystem : MonoBehaviour
    {
        // ============================================
        // SINGLETON
        // ============================================

        public static ResourceSystem Instance { get; private set; }

        // ============================================
        // CONFIGURAZIONE
        // ============================================

        [TitleGroup("Configurazione Risorse")]
        [InfoBox("Trascina qui tutte le ResourceData disponibili nel gioco")]
        [AssetList(Path = "_Content/Data/Resources", AutoPopulate = true)]
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "displayName")]
        [Tooltip("Tutte le definizioni risorse disponibili nel gioco")]
        [SerializeField] private ResourceData[] availableResources;

        [TitleGroup("Risorse Iniziali")]
        [TableList(AlwaysExpanded = true, ShowIndexLabels = false)]
        [Tooltip("Risorse iniziali per nuova partita")]
        [SerializeField] private StartingResource[] startingResources;

        [TitleGroup("Eventi")]
        [FoldoutGroup("Eventi/Resource Events")]
        [Tooltip("Evento quando una risorsa cambia quantità")]
        [SerializeField] private StringEvent onResourceChanged;

        [FoldoutGroup("Eventi/Resource Events")]
        [Tooltip("Evento quando risorse insufficienti")]
        [SerializeField] private StringEvent onResourceInsufficient;

        [TitleGroup("Debug")]
        [ToggleLeft]
        [SerializeField] private bool debugMode = true;

        // ============================================
        // RUNTIME DATA
        // ============================================

        // Dizionario resourceId → quantità attuale
        [ShowInInspector]
        [TitleGroup("Runtime - Inventario")]
        [DictionaryDrawerSettings(KeyLabel = "Resource ID", ValueLabel = "Amount")]
        [ReadOnly]
        private Dictionary<string, float> resourceAmounts = new Dictionary<string, float>();

        // Cache resourceId → ResourceData per lookup veloce
        private Dictionary<string, ResourceData> resourceLookup = new Dictionary<string, ResourceData>();

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[ResourceSystem] Duplicato rilevato, distruggo questo.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeSystem();
        }

        private void InitializeSystem()
        {
            // Popola la cache di lookup
            resourceLookup.Clear();
            resourceAmounts.Clear();

            if (availableResources == null || availableResources.Length == 0)
            {
                Debug.LogError("[ResourceSystem] Nessuna risorsa configurata!");
                return;
            }

            foreach (var resource in availableResources)
            {
                if (resource == null) continue;

                string id = resource.ResourceId; // ✅ Property corretta

                if (resourceLookup.ContainsKey(id))
                {
                    Debug.LogWarning($"[ResourceSystem] ID duplicato: {id}");
                    continue;
                }

                resourceLookup[id] = resource;
                resourceAmounts[id] = 0f;

                if (debugMode)
                {
                    Debug.Log($"[ResourceSystem] Registrata risorsa: {id} ({resource.DisplayName})"); // ✅ Property corretta
                }
            }

            // Applica risorse iniziali
            ApplyStartingResources();

            Debug.Log($"<color=green>[ResourceSystem] Inizializzato con {resourceLookup.Count} risorse</color>");
        }

        private void ApplyStartingResources()
        {
            if (startingResources == null) return;

            foreach (var starting in startingResources)
            {
                if (starting.resource != null)
                {
                    AddResource(starting.resource.ResourceId, starting.amount); // ✅ Property corretta
                }
            }
        }

        // ============================================
        // API PUBBLICA - GESTIONE RISORSE
        // ============================================

        /// <summary>
        /// Aggiunge una quantità di risorsa
        /// </summary>
        public bool AddResource(string resourceId, float amount)
        {
            if (!resourceAmounts.ContainsKey(resourceId))
            {
                Debug.LogWarning($"[ResourceSystem] Risorsa sconosciuta: {resourceId}");
                return false;
            }

            var data = resourceLookup[resourceId];
            float newAmount = resourceAmounts[resourceId] + amount;

            // Applica cap se definito
            if (data.HasMaxStorage) // ✅ Property corretta
            {
                newAmount = Mathf.Min(newAmount, data.MaxStorage); // ✅ Property corretta
            }

            resourceAmounts[resourceId] = newAmount;

            if (debugMode)
            {
                Debug.Log($"<color=cyan>[ResourceSystem]</color> +{amount} {data.DisplayName} → Totale: {newAmount}"); // ✅ Property corretta
            }

            // Notifica cambio
            onResourceChanged?.Raise(resourceId);

            return true;
        }

        /// <summary>
        /// Rimuove una quantità di risorsa. Ritorna false se insufficiente.
        /// </summary>
        public bool RemoveResource(string resourceId, float amount)
        {
            if (!resourceAmounts.ContainsKey(resourceId))
            {
                Debug.LogWarning($"[ResourceSystem] Risorsa sconosciuta: {resourceId}");
                return false;
            }

            if (resourceAmounts[resourceId] < amount)
            {
                if (debugMode)
                {
                    Debug.Log($"<color=red>[ResourceSystem]</color> Insufficiente {resourceId}: richiesto {amount}, disponibile {resourceAmounts[resourceId]}");
                }
                onResourceInsufficient?.Raise(resourceId);
                return false;
            }

            resourceAmounts[resourceId] -= amount;

            if (debugMode)
            {
                Debug.Log($"<color=orange>[ResourceSystem]</color> -{amount} {resourceLookup[resourceId].DisplayName} → Totale: {resourceAmounts[resourceId]}"); // ✅ Property corretta
            }

            onResourceChanged?.Raise(resourceId);
            return true;
        }

        /// <summary>
        /// Verifica se c'è abbastanza risorsa
        /// </summary>
        public bool HasResource(string resourceId, float amount)
        {
            if (!resourceAmounts.ContainsKey(resourceId)) return false;
            return resourceAmounts[resourceId] >= amount;
        }

        /// <summary>
        /// Ottiene la quantità attuale di una risorsa
        /// </summary>
        public float GetResourceAmount(string resourceId)
        {
            if (resourceAmounts.TryGetValue(resourceId, out float amount))
            {
                return amount;
            }
            return 0f;
        }

        /// <summary>
        /// Ottiene i dati di una risorsa
        /// </summary>
        public ResourceData GetResourceData(string resourceId)
        {
            if (resourceLookup.TryGetValue(resourceId, out ResourceData data))
            {
                return data;
            }
            return null;
        }

        /// <summary>
        /// Verifica se si possono pagare più risorse insieme
        /// </summary>
        public bool CanAfford(ResourceCost[] costs)
        {
            if (costs == null) return true;

            foreach (var cost in costs)
            {
                if (!HasResource(cost.resourceId, cost.amount))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Paga un costo multiplo. Ritorna false se non può permetterselo.
        /// </summary>
        public bool PayCost(ResourceCost[] costs)
        {
            if (!CanAfford(costs))
            {
                return false;
            }

            foreach (var cost in costs)
            {
                RemoveResource(cost.resourceId, cost.amount);
            }
            return true;
        }

        /// <summary>
        /// Ottiene tutte le risorse e quantità (per UI)
        /// </summary>
        public Dictionary<string, float> GetAllResources()
        {
            return new Dictionary<string, float>(resourceAmounts);
        }

        /// <summary>
        /// Ottiene tutte le ResourceData disponibili
        /// </summary>
        public ResourceData[] GetAllResourceData()
        {
            return availableResources;
        }

        // ============================================
        // COMPATIBILITY CON STRUCTURECOST[]
        // ============================================

        /// <summary>
        /// Verifica se si possono pagare costi da StructureData (namespace Structures)
        /// </summary>
        public bool CanAfford(WildernessSurvival.Gameplay.Structures.StructureCost[] costs)
        {
            if (costs == null || costs.Length == 0) return true;

            foreach (var cost in costs)
            {
                if (!HasResource(cost.resourceId, cost.amount))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Paga un costo da StructureData. Ritorna false se non può permetterselo.
        /// </summary>
        public bool PayCost(WildernessSurvival.Gameplay.Structures.StructureCost[] costs)
        {
            if (!CanAfford(costs))
            {
                return false;
            }

            foreach (var cost in costs)
            {
                RemoveResource(cost.resourceId, cost.amount);
            }
            return true;
        }

        // ============================================
        // PRODUZIONE RISORSE (chiamato da strutture)
        // ============================================

        /// <summary>
        /// Produce risorse basate su worker/hero assegnati.
        /// Chiamato ogni tick dal sistema di produzione.
        /// </summary>
        public void ProduceResource(string resourceId, int workerCount, int heroCount, float deltaTime)
        {
            var data = GetResourceData(resourceId);
            if (data == null) return;

            // Calcola produzione per questo frame
            float productionPerMinute = data.CalculateProduction(workerCount, heroCount); // ✅ Metodo esistente
            float productionThisFrame = (productionPerMinute / 60f) * deltaTime;

            AddResource(resourceId, productionThisFrame);
        }

        // ============================================
        // DEBUG & TESTING (ODIN)
        // ============================================

        [TitleGroup("Debug Actions")]
        [ButtonGroup("Debug Actions/Row1")]
        [Button("Add 100 Warmwood", ButtonSizes.Medium)]
        [GUIColor(0.4f, 0.8f, 0.4f)]
        private void DebugAddWarmwood()
        {
            AddResource("warmwood", 100);
        }

        [ButtonGroup("Debug Actions/Row1")]
        [Button("Add 50 Food", ButtonSizes.Medium)]
        [GUIColor(0.4f, 0.6f, 0.8f)]
        private void DebugAddFood()
        {
            AddResource("food", 50);
        }

        [ButtonGroup("Debug Actions/Row1")]
        [Button("Add 25 Shard", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.4f, 0.8f)]
        private void DebugAddShard()
        {
            AddResource("shard", 25);
        }

        [ButtonGroup("Debug Actions/Row2")]
        [Button("Reset All Resources", ButtonSizes.Medium)]
        [GUIColor(1f, 0.5f, 0.5f)]
        private void DebugResetResources()
        {
            foreach (var key in new List<string>(resourceAmounts.Keys))
            {
                resourceAmounts[key] = 0f;
            }
            ApplyStartingResources();
            Debug.Log("[ResourceSystem] Risorse resettate");
        }

        [ButtonGroup("Debug Actions/Row2")]
        [Button("Print Inventory", ButtonSizes.Medium)]
        private void DebugPrintAll()
        {
            Debug.Log("=== RESOURCE INVENTORY ===");
            foreach (var kvp in resourceAmounts)
            {
                var data = resourceLookup[kvp.Key];
                Debug.Log($"  {data.DisplayName}: {kvp.Value:F1}"); // ✅ Property corretta
            }
        }
    }

    // ============================================
    // STRUCT DI SUPPORTO
    // ============================================

    [System.Serializable]
    public struct StartingResource
    {
        [HorizontalGroup("Row", 0.7f)]
        [HideLabel]
        public ResourceData resource;

        [HorizontalGroup("Row")]
        [HideLabel]
        [SuffixLabel("x", Overlay = true)]
        public float amount;
    }

    [System.Serializable]
    public struct ResourceCost
    {
        [HorizontalGroup("Row", 0.7f)]
        [HideLabel]
        public string resourceId;

        [HorizontalGroup("Row")]
        [HideLabel]
        public float amount;
    }
}