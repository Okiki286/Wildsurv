using UnityEngine;

namespace WildernessSurvival.Utils
{
    /// <summary>
    /// Genera placeholder visivi per testing rapido.
    /// Crea terreno, bonfire, e oggetti base.
    /// </summary>
    public class PlaceholderGenerator : MonoBehaviour
    {
        [Header("=== TERRENO ===")]
        [SerializeField] private bool generateTerrain = true;
        [SerializeField] private Vector2Int terrainSize = new Vector2Int(50, 50);
        [SerializeField] private Color terrainColor = new Color(0.3f, 0.5f, 0.2f);

        [Header("=== BONFIRE ===")]
        [SerializeField] private bool generateBonfire = true;
        [SerializeField] private Vector3 bonfirePosition = Vector3.zero;

        [Header("=== GRID ===")]
        [SerializeField] private bool showGrid = true;
        [SerializeField] private float gridSize = 1f;
        [SerializeField] private Color gridColor = new Color(1f, 1f, 1f, 0.1f);

        [Header("=== DECORAZIONI TEST ===")]
        [SerializeField] private bool generateDecorations = true;
        [SerializeField] private int treeCount = 20;
        [SerializeField] private int rockCount = 15;

        // ============================================
        // GENERAZIONE
        // ============================================

        private void Start()
        {
            if (generateTerrain)
            {
                CreateTerrain();
            }

            if (generateBonfire)
            {
                CreateBonfire();
            }

            if (generateDecorations)
            {
                CreateDecorations();
            }
        }

        private void CreateTerrain()
        {
            // Crea piano terreno
            GameObject terrain = GameObject.CreatePrimitive(PrimitiveType.Plane);
            terrain.name = "Terrain_Placeholder";
            terrain.transform.position = Vector3.zero;
            terrain.transform.localScale = new Vector3(terrainSize.x / 10f, 1f, terrainSize.y / 10f);

            // Materiale
            Renderer renderer = terrain.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = terrainColor;
            renderer.material = mat;

            // Layer
            terrain.layer = LayerMask.NameToLayer("Ground");

            Debug.Log($"[Placeholder] Terreno creato: {terrainSize.x}x{terrainSize.y}");
        }

        private void CreateBonfire()
        {
            // Container
            GameObject bonfire = new GameObject("Bonfire_Core");
            bonfire.transform.position = bonfirePosition;

            // Base (cilindro schiacciato)
            GameObject bonfireBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bonfireBase.name = "Bonfire_Base";
            bonfireBase.transform.SetParent(bonfire.transform);
            bonfireBase.transform.localPosition = Vector3.zero;
            bonfireBase.transform.localScale = new Vector3(2f, 0.3f, 2f);

            // Materiale base
            Renderer baseRenderer = bonfireBase.GetComponent<Renderer>();
            Material baseMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            baseMat.color = new Color(0.3f, 0.2f, 0.1f);
            baseRenderer.material = baseMat;

            // Fuoco (sfera emissiva)
            GameObject fireGlow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fireGlow.name = "Fire_Glow";
            fireGlow.transform.SetParent(bonfire.transform);
            fireGlow.transform.localPosition = new Vector3(0, 0.5f, 0);
            fireGlow.transform.localScale = new Vector3(1f, 1.5f, 1f);

            // Rimuovi collider dal fuoco
            Destroy(fireGlow.GetComponent<Collider>());

            // Materiale emissivo per il fuoco
            Renderer fireRenderer = fireGlow.GetComponent<Renderer>();
            Material fireMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            fireMat.color = new Color(1f, 0.5f, 0.1f);
            fireMat.EnableKeyword("_EMISSION");
            fireMat.SetColor("_EmissionColor", new Color(2f, 1f, 0.3f));
            fireRenderer.material = fireMat;

            // Luce
            GameObject lightObj = new GameObject("Bonfire_Light");
            lightObj.transform.SetParent(bonfire.transform);
            lightObj.transform.localPosition = new Vector3(0, 1f, 0);
            
            Light bonfireLight = lightObj.AddComponent<Light>();
            bonfireLight.type = LightType.Point;
            bonfireLight.color = new Color(1f, 0.6f, 0.2f);
            bonfireLight.intensity = 2f;
            bonfireLight.range = 15f;
            bonfireLight.shadows = LightShadows.Soft;

            // Aggiungi tag
            bonfire.tag = "Bonfire";

            Debug.Log("[Placeholder] Bonfire creato");
        }

        private void CreateDecorations()
        {
            GameObject decorContainer = new GameObject("Decorations");
            float halfX = terrainSize.x / 2f;
            float halfZ = terrainSize.y / 2f;

            // Alberi
            for (int i = 0; i < treeCount; i++)
            {
                Vector3 pos = GetRandomPosition(halfX, halfZ, 5f);
                CreateTree(pos, decorContainer.transform);
            }

            // Rocce
            for (int i = 0; i < rockCount; i++)
            {
                Vector3 pos = GetRandomPosition(halfX, halfZ, 3f);
                CreateRock(pos, decorContainer.transform);
            }

            Debug.Log($"[Placeholder] Decorazioni create: {treeCount} alberi, {rockCount} rocce");
        }

        private Vector3 GetRandomPosition(float halfX, float halfZ, float minDistFromCenter)
        {
            Vector3 pos;
            do
            {
                pos = new Vector3(
                    Random.Range(-halfX + 2, halfX - 2),
                    0,
                    Random.Range(-halfZ + 2, halfZ - 2)
                );
            } while (pos.magnitude < minDistFromCenter);
            
            return pos;
        }

        private void CreateTree(Vector3 position, Transform parent)
        {
            GameObject tree = new GameObject("Tree");
            tree.transform.SetParent(parent);
            tree.transform.position = position;

            // Tronco
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform);
            trunk.transform.localPosition = new Vector3(0, 1f, 0);
            trunk.transform.localScale = new Vector3(0.3f, 1f, 0.3f);

            Renderer trunkRenderer = trunk.GetComponent<Renderer>();
            Material trunkMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            trunkMat.color = new Color(0.4f, 0.25f, 0.1f);
            trunkRenderer.material = trunkMat;

            // Chioma
            GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            foliage.name = "Foliage";
            foliage.transform.SetParent(tree.transform);
            foliage.transform.localPosition = new Vector3(0, 2.5f, 0);
            float scale = Random.Range(1f, 2f);
            foliage.transform.localScale = new Vector3(scale, scale * 1.2f, scale);

            Renderer foliageRenderer = foliage.GetComponent<Renderer>();
            Material foliageMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            foliageMat.color = new Color(
                Random.Range(0.2f, 0.4f),
                Random.Range(0.4f, 0.6f),
                Random.Range(0.1f, 0.2f)
            );
            foliageRenderer.material = foliageMat;

            // Tag per interazione futura
            tree.tag = "Resource";
        }

        private void CreateRock(Vector3 position, Transform parent)
        {
            // Usa cubo deformato come roccia placeholder
            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = "Rock";
            rock.transform.SetParent(parent);
            rock.transform.position = position + Vector3.up * 0.25f;
            
            float scaleX = Random.Range(0.5f, 1.5f);
            float scaleY = Random.Range(0.3f, 0.8f);
            float scaleZ = Random.Range(0.5f, 1.5f);
            rock.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
            rock.transform.rotation = Quaternion.Euler(
                Random.Range(-10f, 10f),
                Random.Range(0f, 360f),
                Random.Range(-10f, 10f)
            );

            Renderer rockRenderer = rock.GetComponent<Renderer>();
            Material rockMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            float gray = Random.Range(0.3f, 0.5f);
            rockMat.color = new Color(gray, gray, gray);
            rockRenderer.material = rockMat;

            rock.tag = "Resource";
        }

        // ============================================
        // DEBUG GIZMOS
        // ============================================

        private void OnDrawGizmos()
        {
            if (!showGrid) return;

            Gizmos.color = gridColor;
            
            float halfX = terrainSize.x / 2f;
            float halfZ = terrainSize.y / 2f;

            // Grid lines X
            for (float x = -halfX; x <= halfX; x += gridSize)
            {
                Gizmos.DrawLine(
                    new Vector3(x, 0.01f, -halfZ),
                    new Vector3(x, 0.01f, halfZ)
                );
            }

            // Grid lines Z
            for (float z = -halfZ; z <= halfZ; z += gridSize)
            {
                Gizmos.DrawLine(
                    new Vector3(-halfX, 0.01f, z),
                    new Vector3(halfX, 0.01f, z)
                );
            }

            // Bonfire position
            if (generateBonfire)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(bonfirePosition, 1f);
            }
        }
    }
}
