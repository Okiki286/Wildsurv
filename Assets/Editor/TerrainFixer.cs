using UnityEngine;
using UnityEditor;

namespace WildernessSurvival.Utils
{
    /// <summary>
    /// Tool per fixare il terreno e renderlo compatibile con BuildMode
    /// </summary>
    public static class TerrainFixer
    {
        [MenuItem("Wilderness/Fix/🔧 Fix Terrain for BuildMode")]
        public static void FixTerrain()
        {
            Debug.Log("<color=cyan>========================================</color>");
            Debug.Log("<color=cyan>[TerrainFixer] Inizializzazione fix...</color>");
            Debug.Log("<color=cyan>========================================</color>");

            bool terrainWasFixed = false;

            // 1. Cerca tutti gli oggetti terreno nella scena
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            foreach (GameObject obj in allObjects)
            {
                // Cerca oggetti che sembrano terreno
                if (obj.name.Contains("Terrain") ||
                    obj.name.Contains("Ground") ||
                    obj.name.Contains("Plane") ||
                    obj.name.Contains("Floor"))
                {
                    terrainWasFixed |= FixTerrainObject(obj);
                }
            }

            if (!terrainWasFixed)
            {
                Debug.LogWarning("[TerrainFixer] Nessun terreno trovato! Creo un Plane di test...");
                CreateTestPlane();
            }

            Debug.Log("<color=green>========================================</color>");
            Debug.Log("<color=green>[TerrainFixer] ✅ FIX COMPLETATO!</color>");
            Debug.Log("<color=green>========================================</color>");
            Debug.Log("\n<color=yellow>🎮 Ora premi PLAY e premi B per testare il BuildMode!</color>\n");
        }

        private static bool FixTerrainObject(GameObject obj)
        {
            bool wasFixed = false;
            Debug.Log($"<color=yellow>[TerrainFixer]</color> Analizzo: {obj.name}");

            // 1. Verifica Layer
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer == -1)
            {
                Debug.LogError("[TerrainFixer] Layer 'Ground' non esiste! Crealo manualmente in Edit > Project Settings > Tags and Layers");
                return false;
            }

            if (obj.layer != groundLayer)
            {
                obj.layer = groundLayer;
                Debug.Log($"  ✅ Layer impostato a 'Ground' (Layer {groundLayer})");
                wasFixed = true;
            }

            // 2. Verifica Collider
            Collider existingCollider = obj.GetComponent<Collider>();

            if (existingCollider == null)
            {
                // Nessun collider - aggiungiamolo
                MeshFilter meshFilter = obj.GetComponent<MeshFilter>();

                if (meshFilter != null)
                {
                    // Ha una mesh - usa MeshCollider
                    MeshCollider meshCol = obj.AddComponent<MeshCollider>();
                    meshCol.sharedMesh = meshFilter.sharedMesh;
                    meshCol.convex = false;
                    Debug.Log($"  ✅ Aggiunto MeshCollider");
                    wasFixed = true;
                }
                else
                {
                    // Nessuna mesh - usa BoxCollider
                    Renderer renderer = obj.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        BoxCollider boxCol = obj.AddComponent<BoxCollider>();
                        boxCol.size = renderer.bounds.size;
                        boxCol.center = renderer.bounds.center - obj.transform.position;
                        Debug.Log($"  ✅ Aggiunto BoxCollider");
                        wasFixed = true;
                    }
                    else
                    {
                        // Nessun renderer - crea BoxCollider default
                        BoxCollider boxCol = obj.AddComponent<BoxCollider>();
                        boxCol.size = new Vector3(50, 1, 50);
                        Debug.Log($"  ✅ Aggiunto BoxCollider (default size)");
                        wasFixed = true;
                    }
                }
            }
            else
            {
                Debug.Log($"  ℹ️ Collider già presente: {existingCollider.GetType().Name}");
            }

            // 3. Verifica Tag (opzionale ma consigliato)
            if (!obj.CompareTag("Untagged"))
            {
                Debug.Log($"  ℹ️ Tag attuale: {obj.tag}");
            }

            if (wasFixed)
            {
                EditorUtility.SetDirty(obj);
                Debug.Log($"<color=green>  ✅ {obj.name} FIXATO!</color>");
            }
            else
            {
                Debug.Log($"  ✓ {obj.name} già configurato correttamente");
            }

            return wasFixed;
        }

        private static void CreateTestPlane()
        {
            // Crea un Plane di test
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "TestGround_BuildMode";
            plane.transform.position = Vector3.zero;
            plane.transform.localScale = new Vector3(5, 1, 5); // 50x50 units

            // Imposta layer
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer != -1)
            {
                plane.layer = groundLayer;
            }

            // Material verde per riconoscerlo
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.2f, 0.6f, 0.2f);
            plane.GetComponent<Renderer>().material = mat;

            Debug.Log("<color=green>[TerrainFixer]</color> Creato TestGround_BuildMode (50x50m)");
            Debug.Log("  ✅ Layer: Ground");
            Debug.Log("  ✅ Collider: MeshCollider");

            Selection.activeGameObject = plane;
            EditorGUIUtility.PingObject(plane);
        }

        [MenuItem("Wilderness/Fix/📊 Check Terrain Status")]
        public static void CheckTerrainStatus()
        {
            Debug.Log("=== TERRAIN STATUS CHECK ===");

            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer == -1)
            {
                Debug.LogError("❌ Layer 'Ground' non esiste!");
                Debug.Log("   Soluzione: Crea manualmente in Edit > Project Settings > Tags and Layers");
                return;
            }

            Debug.Log($"✅ Layer 'Ground' esiste (Layer {groundLayer})");

            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int terrainCount = 0;
            int validTerrains = 0;

            foreach (GameObject obj in allObjects)
            {
                if (obj.name.Contains("Terrain") ||
                    obj.name.Contains("Ground") ||
                    obj.name.Contains("Plane") ||
                    obj.name.Contains("Floor"))
                {
                    terrainCount++;

                    bool hasCorrectLayer = obj.layer == groundLayer;
                    bool hasCollider = obj.GetComponent<Collider>() != null;

                    if (hasCorrectLayer && hasCollider)
                    {
                        validTerrains++;
                        Debug.Log($"✅ {obj.name}: Layer OK, Collider OK");
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ {obj.name}: Layer={hasCorrectLayer}, Collider={hasCollider}");
                    }
                }
            }

            Debug.Log($"\n=== SUMMARY ===");
            Debug.Log($"Terreni trovati: {terrainCount}");
            Debug.Log($"Terreni validi: {validTerrains}");

            if (validTerrains == 0)
            {
                Debug.LogWarning("⚠️ Nessun terreno valido! Esegui 'Fix Terrain for BuildMode'");
            }
        }

        [MenuItem("Wilderness/Fix/🧪 Create Test Plane")]
        public static void CreateTestPlaneManual()
        {
            CreateTestPlane();
        }
    }
}