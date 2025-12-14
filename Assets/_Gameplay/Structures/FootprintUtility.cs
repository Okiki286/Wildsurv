using System.Collections.Generic;
using UnityEngine;

namespace WildernessSurvival.Gameplay.Structures
{
    /// <summary>
    /// Utility statica per gestire footprint delle strutture con supporto rotazione.
    ///
    /// Convenzioni:
    /// - Le celle locali partono da (0,0) e vanno a (width-1, height-1)
    /// - OriginCell (anchor) è la cella bottom-left del footprint nel mondo
    /// - La rotazione avviene attorno al centro logico del footprint
    /// - rotationSteps: 0=0°, 1=90°, 2=180°, 3=270° (orario)
    ///
    /// Formule di rotazione per una cella (x,z) in un footprint (w x h):
    /// - 0° (step 0): (x, z) -> bounding box invariato (w x h)
    /// - 90° (step 1): (x, z) -> (z, w-1-x) -> bounding box diventa (h x w)
    /// - 180° (step 2): (x, z) -> (w-1-x, h-1-z) -> bounding box invariato (w x h)
    /// - 270° (step 3): (x, z) -> (h-1-z, x) -> bounding box diventa (h x w)
    /// </summary>
    public static class FootprintUtility
    {
        // ============================================
        // CORE ROTATION API
        // ============================================

        /// <summary>
        /// Ruota una singola cella locale secondo lo step di rotazione.
        /// </summary>
        /// <param name="localCell">Cella locale (0-indexed)</param>
        /// <param name="originalWidth">Larghezza originale del footprint</param>
        /// <param name="originalHeight">Altezza originale del footprint</param>
        /// <param name="rotationSteps">Step di rotazione (0-3)</param>
        /// <returns>Cella locale ruotata</returns>
        public static Vector2Int RotateLocalCell(Vector2Int localCell, int originalWidth, int originalHeight, int rotationSteps)
        {
            // Normalizza rotationSteps a 0..3
            rotationSteps = ((rotationSteps % 4) + 4) % 4;

            int x = localCell.x;
            int z = localCell.y;
            int w = originalWidth;
            int h = originalHeight;

            switch (rotationSteps)
            {
                case 0: // 0° - invariato
                    return new Vector2Int(x, z);

                case 1: // 90° orario
                    return new Vector2Int(z, w - 1 - x);

                case 2: // 180°
                    return new Vector2Int(w - 1 - x, h - 1 - z);

                case 3: // 270° orario (= 90° antiorario)
                    return new Vector2Int(h - 1 - z, x);

                default:
                    return localCell;
            }
        }

        /// <summary>
        /// Ruota una lista di celle locali secondo lo step di rotazione.
        /// </summary>
        /// <param name="localCells">Lista di celle locali (0-indexed)</param>
        /// <param name="originalWidth">Larghezza originale del footprint</param>
        /// <param name="originalHeight">Altezza originale del footprint</param>
        /// <param name="rotationSteps">Step di rotazione (0-3)</param>
        /// <returns>Lista di celle locali ruotate</returns>
        public static List<Vector2Int> GetRotatedCells(List<Vector2Int> localCells, int originalWidth, int originalHeight, int rotationSteps)
        {
            var rotatedCells = new List<Vector2Int>(localCells.Count);

            foreach (var cell in localCells)
            {
                rotatedCells.Add(RotateLocalCell(cell, originalWidth, originalHeight, rotationSteps));
            }

            return rotatedCells;
        }

        /// <summary>
        /// Genera tutte le celle locali di un footprint rettangolare.
        /// </summary>
        /// <param name="width">Larghezza in celle</param>
        /// <param name="height">Altezza in celle</param>
        /// <returns>Lista di celle (0,0) fino a (width-1, height-1)</returns>
        public static List<Vector2Int> GenerateRectangularFootprint(int width, int height)
        {
            var cells = new List<Vector2Int>(width * height);

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    cells.Add(new Vector2Int(x, z));
                }
            }

            return cells;
        }

        /// <summary>
        /// Calcola la dimensione del bounding box dopo la rotazione.
        /// </summary>
        /// <param name="originalSize">Dimensione originale (width, height)</param>
        /// <param name="rotationSteps">Step di rotazione (0-3)</param>
        /// <returns>Dimensione ruotata</returns>
        public static Vector2Int GetRotatedSize(Vector2Int originalSize, int rotationSteps)
        {
            // Normalizza
            rotationSteps = ((rotationSteps % 4) + 4) % 4;

            // 90° e 270° scambiano X e Y
            if (rotationSteps % 2 == 1)
            {
                return new Vector2Int(originalSize.y, originalSize.x);
            }

            return originalSize;
        }

        // ============================================
        // HIGH-LEVEL API (per StructureData)
        // ============================================

        /// <summary>
        /// Calcola il footprint completo di una struttura con rotazione applicata.
        /// Ritorna sia le celle ruotate che la dimensione del bounding box risultante.
        /// </summary>
        /// <param name="data">StructureData della struttura</param>
        /// <param name="rotationSteps">Step di rotazione (0-3)</param>
        /// <returns>Tuple con (celle locali ruotate, dimensione bounding box)</returns>
        public static (List<Vector2Int> cells, Vector2Int size) GetRotatedFootprint(StructureData data, int rotationSteps)
        {
            if (data == null)
            {
                Debug.LogError("[FootprintUtility] StructureData is null!");
                return (new List<Vector2Int>(), Vector2Int.zero);
            }

            Vector2Int originalSize = data.GridSize;
            int width = originalSize.x;
            int height = originalSize.y;

            // Genera le celle locali del footprint base
            var localCells = GenerateRectangularFootprint(width, height);

            // Ruota le celle
            var rotatedCells = GetRotatedCells(localCells, width, height, rotationSteps);

            // Calcola la nuova dimensione del bounding box
            Vector2Int rotatedSize = GetRotatedSize(originalSize, rotationSteps);

            return (rotatedCells, rotatedSize);
        }

        /// <summary>
        /// Calcola le celle world occupate da una struttura data origine e rotazione.
        /// </summary>
        /// <param name="data">StructureData della struttura</param>
        /// <param name="originCell">Cella di origine (bottom-left nel mondo)</param>
        /// <param name="rotationSteps">Step di rotazione (0-3)</param>
        /// <returns>Lista di celle world occupate</returns>
        public static List<Vector2Int> GetWorldOccupiedCells(StructureData data, Vector2Int originCell, int rotationSteps)
        {
            var (localCells, _) = GetRotatedFootprint(data, rotationSteps);

            var worldCells = new List<Vector2Int>(localCells.Count);

            foreach (var localCell in localCells)
            {
                // Aggiungi l'offset dell'origine per ottenere la cella world
                worldCells.Add(new Vector2Int(originCell.x + localCell.x, originCell.y + localCell.y));
            }

            return worldCells;
        }

        /// <summary>
        /// Calcola le celle world occupate da una struttura usando i suoi dati runtime.
        /// </summary>
        /// <param name="controller">StructureController della struttura</param>
        /// <returns>Lista di celle world occupate</returns>
        public static List<Vector2Int> GetWorldOccupiedCells(StructureController controller)
        {
            if (controller == null || controller.Data == null)
            {
                Debug.LogWarning("[FootprintUtility] Controller or Data is null!");
                return new List<Vector2Int>();
            }

            return GetWorldOccupiedCells(controller.Data, controller.OriginCell, controller.PlacementRotationStep);
        }

        // ============================================
        // VALIDATION API
        // ============================================

        /// <summary>
        /// Verifica se una cella world è occupata da un set di celle.
        /// </summary>
        /// <param name="cellToCheck">Cella da verificare</param>
        /// <param name="occupiedCells">Set di celle occupate</param>
        /// <returns>True se la cella è occupata</returns>
        public static bool IsCellInFootprint(Vector2Int cellToCheck, IEnumerable<Vector2Int> occupiedCells)
        {
            foreach (var cell in occupiedCells)
            {
                if (cell == cellToCheck)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Verifica se due footprint si sovrappongono.
        /// </summary>
        public static bool FootprintsOverlap(IEnumerable<Vector2Int> footprint1, IEnumerable<Vector2Int> footprint2)
        {
            // Converti il secondo footprint in HashSet per O(1) lookup
            var set2 = new HashSet<Vector2Int>(footprint2);

            foreach (var cell in footprint1)
            {
                if (set2.Contains(cell))
                    return true;
            }

            return false;
        }

        // ============================================
        // DEBUG
        // ============================================

#if UNITY_EDITOR
        /// <summary>
        /// Disegna il footprint con Gizmos (per debug).
        /// </summary>
        public static void DrawFootprintGizmos(Vector2Int originCell, StructureData data, int rotationSteps, float gridSize, Color color)
        {
            if (data == null) return;

            Gizmos.color = color;

            var worldCells = GetWorldOccupiedCells(data, originCell, rotationSteps);

            foreach (var cell in worldCells)
            {
                Vector3 worldPos = new Vector3(cell.x * gridSize, 0.1f, cell.y * gridSize);
                Gizmos.DrawWireCube(worldPos, new Vector3(gridSize * 0.95f, 0.1f, gridSize * 0.95f));
            }
        }

        /// <summary>
        /// Log dettagliato del footprint per debug.
        /// </summary>
        public static void DebugLogFootprint(StructureData data, Vector2Int originCell, int rotationSteps)
        {
            if (data == null)
            {
                Debug.Log("[FootprintUtility] Data is null");
                return;
            }

            var (localCells, size) = GetRotatedFootprint(data, rotationSteps);
            var worldCells = GetWorldOccupiedCells(data, originCell, rotationSteps);

            Debug.Log($"[FootprintUtility] {data.DisplayName}\n" +
                      $"  Original Size: {data.GridSize}\n" +
                      $"  Rotation Steps: {rotationSteps} ({rotationSteps * 90}°)\n" +
                      $"  Rotated Size: {size}\n" +
                      $"  Origin Cell: {originCell}\n" +
                      $"  Local Cells ({localCells.Count}): {string.Join(", ", localCells)}\n" +
                      $"  World Cells ({worldCells.Count}): {string.Join(", ", worldCells)}");
        }
#endif
    }
}
