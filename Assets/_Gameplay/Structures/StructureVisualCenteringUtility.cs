using System.Collections.Generic;
using UnityEngine;

namespace WildernessSurvival.Gameplay.Structures
{
    /// <summary>
    /// Utility condivisa per centrare il VisualRoot di una struttura
    /// in modo deterministico e identico sia per ghost che per runtime.
    /// Include caching per evitare ricalcoli su stesso prefab/rotazione.
    /// </summary>
    public static class StructureVisualCenteringUtility
    {
        // Cache: key = HashCode.Combine(prefabInstanceID, rotationStep)
        private static readonly Dictionary<int, Vector3> _cachedLocalDeltaByPrefabRotation = new Dictionary<int, Vector3>();

        /// <summary>
        /// Ottiene o calcola l'offset locale per centrare il visualRoot.
        /// Usa cache basata su prefab instance ID e rotation step (0..3).
        /// Zero allocations se presente in cache.
        /// IMPORTANTE: Il delta viene calcolato assumendo che visualRoot sia alla baseLocalPos.
        /// </summary>
        public static Vector3 GetOrComputeCenteringLocalDelta(Transform root, Transform visualRoot, GameObject prefab, int rotationStep = 0)
        {
            if (visualRoot == null || root == null || prefab == null) return Vector3.zero;

            // Genera cache key: combine prefab instanceID + rotationStep
            int cacheKey = System.HashCode.Combine(prefab.GetInstanceID(), rotationStep);

            // Controlla cache
            if (_cachedLocalDeltaByPrefabRotation.TryGetValue(cacheKey, out Vector3 cachedDelta))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[StructureVisualCentering] Cache HIT for {prefab.name} rotation {rotationStep}: {cachedDelta}");
#endif
                return cachedDelta;
            }

            // Cache miss: calcola
            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return Vector3.zero;

            // Calcola bounds combinati in world space
            Bounds worldBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                worldBounds.Encapsulate(renderers[i].bounds);
            }

            // Target: posizione world del root (dove vogliamo centrare)
            Vector3 targetWorld = root.position;

            // Delta in world space per centrare i bounds sul target
            Vector3 deltaWorld = targetWorld - worldBounds.center;
            deltaWorld.y = 0f; // Non modificare altezza

            // Converti delta da world a local space del parent di visualRoot
            // Questo gestisce correttamente scale e rotation
            Transform parent = visualRoot.parent;
            Vector3 deltaLocal = parent.InverseTransformVector(deltaWorld);
            deltaLocal.y = 0f;

            // Salva in cache
            _cachedLocalDeltaByPrefabRotation[cacheKey] = deltaLocal;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[StructureVisualCentering] Cache MISS, computed for {prefab.name} rotation {rotationStep}:\n" +
                      $"  World bounds center: {worldBounds.center}\n" +
                      $"  Target world pos: {targetWorld}\n" +
                      $"  Delta world: {deltaWorld}\n" +
                      $"  Delta local (in parent space): {deltaLocal}");
#endif

            return deltaLocal;
        }

        /// <summary>
        /// Calcola l'offset locale per centrare il visualRoot, partendo da una posizione base pulita.
        /// Prima resetta visualRoot alla baseLocalPos, calcola bounds, poi ripristina.
        /// Questo garantisce che il delta sia calcolato correttamente indipendentemente dallo stato attuale.
        /// </summary>
        public static Vector3 ComputeCenteringDeltaFromCleanState(Transform root, Transform visualRoot, Vector3 baseLocalPos, GameObject prefab, int rotationStep = 0)
        {
            if (visualRoot == null || root == null || prefab == null) return Vector3.zero;

            // Genera cache key: combine prefab instanceID + rotationStep
            int cacheKey = System.HashCode.Combine(prefab.GetInstanceID(), rotationStep);

            // Controlla cache
            if (_cachedLocalDeltaByPrefabRotation.TryGetValue(cacheKey, out Vector3 cachedDelta))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[StructureVisualCentering] Cache HIT (clean) for {prefab.name} rotation {rotationStep}: {cachedDelta}");
#endif
                return cachedDelta;
            }

            // Salva posizione corrente
            Vector3 savedLocalPos = visualRoot.localPosition;

            // Resetta alla posizione base per calcolo bounds pulito
            visualRoot.localPosition = baseLocalPos;

            // Calcola bounds dal stato pulito
            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                visualRoot.localPosition = savedLocalPos;
                return Vector3.zero;
            }

            Bounds worldBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                worldBounds.Encapsulate(renderers[i].bounds);
            }

            // Target: posizione world del root
            Vector3 targetWorld = root.position;

            // Delta in world space
            Vector3 deltaWorld = targetWorld - worldBounds.center;
            deltaWorld.y = 0f;

            // Converti a local space
            Transform parent = visualRoot.parent;
            Vector3 deltaLocal = parent.InverseTransformVector(deltaWorld);
            deltaLocal.y = 0f;

            // Ripristina posizione originale (prima di applicare il nuovo delta)
            visualRoot.localPosition = savedLocalPos;

            // Salva in cache
            _cachedLocalDeltaByPrefabRotation[cacheKey] = deltaLocal;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[StructureVisualCentering] Cache MISS (clean), computed for {prefab.name} rotation {rotationStep}:\n" +
                      $"  Base localPos used: {baseLocalPos}\n" +
                      $"  World bounds center: {worldBounds.center}\n" +
                      $"  Target world pos: {targetWorld}\n" +
                      $"  Delta local: {deltaLocal}");
#endif

            return deltaLocal;
        }

        /// <summary>
        /// Applica il centering al visualRoot UNA SOLA VOLTA (usa flag per prevenire double-apply).
        /// Modifica solo X/Z, preserva Y originale.
        /// NOTA: Questo metodo è ADDITIVO - usa ApplyCenteringAbsolute per rotazioni.
        /// </summary>
        public static void ApplyCenteringOnce(Transform visualRoot, Vector3 originalLocalPos, Vector3 deltaLocal, ref bool appliedFlag)
        {
            if (appliedFlag)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[StructureVisualCentering] ApplyCenteringOnce skipped (already applied) for {visualRoot?.name}");
#endif
                return;
            }

            if (visualRoot == null) return;

            Vector3 centeredPos = originalLocalPos;
            centeredPos.x += deltaLocal.x;
            centeredPos.z += deltaLocal.z;
            // Y rimane = originalLocalPos.y

            visualRoot.localPosition = centeredPos;
            appliedFlag = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[StructureVisualCentering] ApplyCenteringOnce APPLIED to {visualRoot.name}:\n" +
                      $"  Original localPos: {originalLocalPos}\n" +
                      $"  Delta local applied: {deltaLocal}\n" +
                      $"  Final localPos: {visualRoot.localPosition}");
#endif
        }

        /// <summary>
        /// Applica il centering in modo ASSOLUTO: baseLocalPos + delta.
        /// Idempotente - chiamate ripetute con stesso baseLocalPos e delta danno sempre lo stesso risultato.
        /// Usa questo metodo per rotazioni del preview per evitare drift.
        /// </summary>
        /// <param name="visualRoot">Transform da centrare</param>
        /// <param name="baseLocalPos">Posizione locale BASE (quella originale del prefab, mai modificata)</param>
        /// <param name="deltaLocal">Delta da applicare (calcolato per la rotazione corrente)</param>
        public static void ApplyCenteringAbsolute(Transform visualRoot, Vector3 baseLocalPos, Vector3 deltaLocal)
        {
            if (visualRoot == null) return;

            Vector3 finalPos = baseLocalPos;
            finalPos.x += deltaLocal.x;
            finalPos.z += deltaLocal.z;
            // Y rimane = baseLocalPos.y

            visualRoot.localPosition = finalPos;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[StructureVisualCentering] ApplyCenteringAbsolute to {visualRoot.name}:\n" +
                      $"  Base localPos: {baseLocalPos}\n" +
                      $"  Delta local: {deltaLocal}\n" +
                      $"  Final localPos: {visualRoot.localPosition}");
#endif
        }

        /// <summary>
        /// Calcola l'offset locale necessario per centrare i bounds dei renderer
        /// sul pivot del parent/root, indipendente da scale e rotation.
        /// DEPRECATO: usa GetOrComputeCenteringLocalDelta per beneficiare della cache.
        /// </summary>
        public static Vector3 ComputeLocalCenteringOffset(Transform visualRoot)
        {
            if (visualRoot == null || visualRoot.parent == null) return Vector3.zero;

            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return Vector3.zero;

            // Calcola bounds combinati in world space
            Bounds worldBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                worldBounds.Encapsulate(renderers[i].bounds);
            }

            // Target: posizione world del parent (dove vogliamo centrare)
            Vector3 targetWorld = visualRoot.parent.position;

            // Delta in world space per centrare i bounds sul target
            Vector3 deltaWorld = targetWorld - worldBounds.center;
            deltaWorld.y = 0f; // Non modificare altezza

            // Converti delta da world a local space del parent
            // Questo gestisce correttamente scale e rotation
            Transform parent = visualRoot.parent;
            Vector3 deltaLocal = parent.InverseTransformVector(deltaWorld);
            deltaLocal.y = 0f;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[StructureVisualCentering] Computed offset for {visualRoot.name}:\n" +
                      $"  World bounds center: {worldBounds.center}\n" +
                      $"  Target world pos: {targetWorld}\n" +
                      $"  Delta world: {deltaWorld}\n" +
                      $"  Delta local (in parent space): {deltaLocal}");
#endif

            return deltaLocal;
        }

        /// <summary>
        /// Applica il centering al visualRoot sommando l'offset alla posizione locale originale.
        /// Modifica solo X/Z, preserva Y originale.
        /// </summary>
        public static void ApplyCentering(Transform visualRoot, Vector3 originalLocalPos)
        {
            if (visualRoot == null) return;

            Vector3 deltaLocal = ComputeLocalCenteringOffset(visualRoot);

            Vector3 centeredPos = originalLocalPos;
            centeredPos.x += deltaLocal.x;
            centeredPos.z += deltaLocal.z;
            // Y rimane = originalLocalPos.y

            visualRoot.localPosition = centeredPos;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[StructureVisualCentering] Applied to {visualRoot.name}:\n" +
                      $"  Original localPos: {originalLocalPos}\n" +
                      $"  Delta local applied: {deltaLocal}\n" +
                      $"  Final localPos: {visualRoot.localPosition}");
#endif
        }

        /// <summary>
        /// Trova il VisualRoot transform in un prefab struttura.
        /// Usa StructureController.visualRoot se disponibile, altrimenti cerca per nome.
        /// </summary>
        public static Transform FindVisualRoot(GameObject structureObj)
        {
            // 1. Prova con StructureController reference
            var controller = structureObj.GetComponent<StructureController>();
            if (controller != null)
            {
                // StructureController ha il campo serializzato visualRoot
                // Ma non possiamo accederlo direttamente se è private
                // Quindi cerchiamo per nome invece
            }

            // 2. Cerca child chiamato "VisualRoot"
            Transform visualRoot = structureObj.transform.Find("VisualRoot");
            if (visualRoot != null) return visualRoot;

            // 3. Fallback: primo child con Renderer
            foreach (Transform child in structureObj.transform)
            {
                if (child.GetComponentInChildren<Renderer>() != null)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
