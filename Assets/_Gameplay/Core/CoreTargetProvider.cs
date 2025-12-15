using UnityEngine;

namespace WildernessSurvival.Gameplay.Core
{
    /// <summary>
    /// Static provider per accedere al Core (WaystoneBeacon) da qualsiasi sistema.
    /// Usato principalmente da EnemyController per trovare il target.
    /// </summary>
    public static class CoreTargetProvider
    {
        private static Transform cachedCoreTransform;
        private static WaystoneBeaconController cachedController;
        private static bool hasSearched = false;

        /// <summary>
        /// Ritorna true se esiste un Core valido in scena.
        /// </summary>
        public static bool HasCore
        {
            get
            {
                EnsureCached();
                return cachedCoreTransform != null && cachedController != null && !cachedController.IsDestroyed;
            }
        }

        /// <summary>
        /// Ritorna il Transform del Core (per navigation/targeting).
        /// Ritorna null se il Core non esiste o è distrutto.
        /// </summary>
        public static Transform GetCoreTransform()
        {
            EnsureCached();
            
            // Verifica che il core sia ancora valido
            if (cachedCoreTransform == null || cachedController == null || cachedController.IsDestroyed)
            {
                return null;
            }

            return cachedCoreTransform;
        }

        /// <summary>
        /// Ritorna il controller del Core per accesso diretto.
        /// </summary>
        public static WaystoneBeaconController GetCoreController()
        {
            EnsureCached();
            return cachedController;
        }

        /// <summary>
        /// Ritorna la posizione del Core.
        /// Ritorna Vector3.zero se non esiste.
        /// </summary>
        public static Vector3 GetCorePosition()
        {
            Transform core = GetCoreTransform();
            return core != null ? core.position : Vector3.zero;
        }

        /// <summary>
        /// Calcola distanza dal Core.
        /// Ritorna float.MaxValue se il Core non esiste.
        /// </summary>
        public static float GetDistanceToCore(Vector3 fromPosition)
        {
            Transform core = GetCoreTransform();
            if (core == null) return float.MaxValue;

            return Vector3.Distance(fromPosition, core.position);
        }

        /// <summary>
        /// Verifica se una posizione è dentro il raggio di influenza del Core.
        /// </summary>
        public static bool IsWithinCoreInfluence(Vector3 position)
        {
            EnsureCached();
            
            if (cachedController == null || cachedCoreTransform == null) 
                return false;

            float distance = Vector3.Distance(position, cachedCoreTransform.position);
            return distance <= cachedController.InfluenceRadius;
        }

        /// <summary>
        /// Forza il refresh della cache (chiamare dopo spawn/respawn del Core).
        /// </summary>
        public static void RefreshCache()
        {
            hasSearched = false;
            cachedCoreTransform = null;
            cachedController = null;
            EnsureCached();
        }

        /// <summary>
        /// Registra manualmente il Core (chiamato dal Bootstrap).
        /// </summary>
        public static void RegisterCore(WaystoneBeaconController controller)
        {
            if (controller != null)
            {
                cachedController = controller;
                cachedCoreTransform = controller.transform;
                hasSearched = true;

                #if UNITY_EDITOR
                Debug.Log($"<color=cyan>[CoreTargetProvider]</color> Core registrato: {controller.name}");
                #endif
            }
        }

        private static void EnsureCached()
        {
            // Se abbiamo già cercato e trovato, verifica che sia ancora valido
            if (hasSearched && cachedCoreTransform != null)
            {
                return;
            }

            // Cerca il core
            hasSearched = true;

            // Prima prova via Bootstrap static reference
            if (WaystoneBeaconBootstrap.Beacon != null)
            {
                cachedController = WaystoneBeaconBootstrap.Beacon;
                cachedCoreTransform = cachedController.transform;
                return;
            }

            // Fallback: cerca per tag
            try
            {
                GameObject coreObj = GameObject.FindWithTag("Core");
                if (coreObj != null)
                {
                    cachedCoreTransform = coreObj.transform;
                    cachedController = coreObj.GetComponent<WaystoneBeaconController>();
                    return;
                }
            }
            catch
            {
                // Tag non esiste
            }

            // Fallback: cerca per nome
            GameObject byName = GameObject.Find("WaystoneBeacon");
            if (byName != null)
            {
                cachedCoreTransform = byName.transform;
                cachedController = byName.GetComponent<WaystoneBeaconController>();
            }
        }

        /// <summary>
        /// Reset completo (per scene reload).
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            cachedCoreTransform = null;
            cachedController = null;
            hasSearched = false;
        }
    }
}
