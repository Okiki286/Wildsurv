using UnityEngine;

namespace WildernessSurvival.Gameplay.Core
{
    /// <summary>
    /// Bootstrap che garantisce l'esistenza del WaystoneBeacon in scena.
    /// Attaccare a un GameObject persistente (es. GameManager o "--- MANAGERS ---").
    /// </summary>
    public class WaystoneBeaconBootstrap : MonoBehaviour
    {
        [Header("Configurazione")]
        [Tooltip("Nome del GameObject da cercare/creare")]
        [SerializeField] private string beaconName = "WaystoneBeacon";
        
        [Tooltip("Tag da assegnare al beacon (deve esistere in Project Settings)")]
        [SerializeField] private string beaconTag = "Core";
        
        [Tooltip("Posizione iniziale del beacon se viene creato")]
        [SerializeField] private Vector3 spawnPosition = Vector3.zero;

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;

        // Cache del beacon creato/trovato
        private static WaystoneBeaconController cachedBeacon;

        /// <summary>
        /// Reset static per domain reload in Editor.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            cachedBeacon = null;
        }

        /// <summary>
        /// Accesso rapido al beacon (per altri sistemi).
        /// </summary>
        public static WaystoneBeaconController Beacon => cachedBeacon;

        private void Awake()
        {
            EnsureBeaconExists();
        }

        /// <summary>
        /// Cerca o crea il WaystoneBeacon in scena.
        /// </summary>
        private void EnsureBeaconExists()
        {
            // 1. Prima cerca per tag (più veloce)
            GameObject existingByTag = null;
            try
            {
                existingByTag = GameObject.FindWithTag(beaconTag);
            }
            catch
            {
                // Tag non esiste ancora - ignora
                if (debugMode)
                {
                    Debug.LogWarning($"<color=orange>[WaystoneBeaconBootstrap]</color> Tag '{beaconTag}' non trovato. Crealo in Edit → Project Settings → Tags and Layers");
                }
            }

            if (existingByTag != null)
            {
                CacheBeacon(existingByTag);
                return;
            }

            // 2. Cerca per nome
            GameObject existingByName = GameObject.Find(beaconName);
            if (existingByName != null)
            {
                CacheBeacon(existingByName);
                return;
            }

            // 3. Non trovato: NON creare più automaticamente
            // CreateBeacon();  // DISABILITATO - usare un prefab manuale
            Debug.LogWarning("<color=orange>[WaystoneBeaconBootstrap]</color> WaystoneBeacon non trovato in scena. " +
                "Aggiungi manualmente un prefab WaystoneBeacon oppure un GameObject con tag 'Core' o nome 'WaystoneBeacon'.");
        }

        private void CacheBeacon(GameObject beaconObj)
        {
            cachedBeacon = beaconObj.GetComponent<WaystoneBeaconController>();
            
            if (cachedBeacon == null)
            {
                // Aggiungi controller se manca
                cachedBeacon = beaconObj.AddComponent<WaystoneBeaconController>();
            }

            if (debugMode)
            {
                Debug.Log($"<color=cyan>[WaystoneBeaconBootstrap]</color> Beacon esistente trovato: {beaconObj.name}");
            }
        }

        private void CreateBeacon()
        {
            // Crea GameObject
            GameObject beacon = new GameObject(beaconName);
            beacon.transform.position = spawnPosition;

            // Assegna tag (se valido)
            try
            {
                beacon.tag = beaconTag;
            }
            catch
            {
                Debug.LogWarning($"<color=orange>[WaystoneBeaconBootstrap]</color> Impossibile assegnare tag '{beaconTag}'. " +
                    "Aggiungi il tag in Edit → Project Settings → Tags and Layers");
            }

            // Aggiungi collider per rilevamento
            CapsuleCollider collider = beacon.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 2f, 0f);
            collider.radius = 1f;
            collider.height = 4f;

            // Crea visuals
            WaystoneBeaconVisualBuilder.BuildVisuals(beacon.transform, addLight: true);

            // Aggiungi controller
            cachedBeacon = beacon.AddComponent<WaystoneBeaconController>();

            if (debugMode)
            {
                Debug.Log($"<color=green>[WaystoneBeaconBootstrap]</color> ✓ WaystoneBeacon CREATO a posizione {spawnPosition}");
            }
            else
            {
                // Log minimo anche senza debug mode (evento importante)
                Debug.Log("<color=green>[WaystoneBeaconBootstrap]</color> WaystoneBeacon creato automaticamente.");
            }
        }

        /// <summary>
        /// Forza la ricreazione del beacon (per testing).
        /// </summary>
        public void ForceRecreate()
        {
            if (cachedBeacon != null)
            {
                #if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(cachedBeacon.gameObject);
                }
                else
                #endif
                {
                    Destroy(cachedBeacon.gameObject);
                }
                cachedBeacon = null;
            }

            CreateBeacon();
        }

        #if UNITY_EDITOR
        [ContextMenu("🔄 Force Recreate Beacon")]
        private void DebugForceRecreate()
        {
            ForceRecreate();
        }
        #endif
    }
}
