using UnityEngine;

namespace WildernessSurvival.Gameplay.Core
{
    /// <summary>
    /// Helper statico per creare la struttura visiva del WaystoneBeacon.
    /// Genera primitive Unity (nessun asset esterno richiesto).
    /// </summary>
    public static class WaystoneBeaconVisualBuilder
    {
        /// <summary>
        /// Crea la struttura visiva completa del beacon.
        /// </summary>
        /// <param name="parent">Transform padre (il WaystoneBeacon)</param>
        /// <param name="addLight">Se true, aggiunge una Point Light</param>
        public static void BuildVisuals(Transform parent, bool addLight = true)
        {
            // Verifica se già esistono i figli
            if (parent.Find("Monolith") != null)
            {
                Debug.Log("<color=cyan>[WaystoneBeaconVisualBuilder]</color> Visuali già presenti, skip.");
                return;
            }

            // ============================================
            // MONOLITH (Base - Cilindro)
            // ============================================
            
            GameObject monolith = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            monolith.name = "Monolith";
            monolith.transform.SetParent(parent);
            monolith.transform.localPosition = Vector3.zero;
            monolith.transform.localRotation = Quaternion.identity;
            monolith.transform.localScale = new Vector3(1.5f, 2f, 1.5f);

            // Material URP per il monolite (pietra opaca)
            Renderer monolithRenderer = monolith.GetComponent<Renderer>();
            if (monolithRenderer != null)
            {
                monolithRenderer.material = CreateStoneMaterial();
            }

            // ============================================
            // CRYSTAL (Cristallo - Cube ruotato 45°)
            // ============================================
            
            GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crystal.name = "Crystal";
            crystal.transform.SetParent(parent);
            crystal.transform.localPosition = new Vector3(0f, 3f, 0f); // Sopra il monolite
            crystal.transform.localRotation = Quaternion.Euler(0f, 45f, 0f); // Ruotato 45° su Y
            crystal.transform.localScale = new Vector3(0.4f, 0.8f, 0.4f); // Stretto e alto

            // Material URP per il cristallo (glow ciano HDR)
            Renderer crystalRenderer = crystal.GetComponent<Renderer>();
            if (crystalRenderer != null)
            {
                crystalRenderer.material = CreateCrystalMaterial();
            }

            // ============================================
            // RUNE RING (Anello decorativo alla base)
            // ============================================
            
            GameObject runeRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            runeRing.name = "RuneRing";
            runeRing.transform.SetParent(parent);
            runeRing.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            runeRing.transform.localRotation = Quaternion.identity;
            runeRing.transform.localScale = new Vector3(2.2f, 0.1f, 2.2f);

            // Material URP per anello rune
            Renderer runeRenderer = runeRing.GetComponent<Renderer>();
            if (runeRenderer != null)
            {
                runeRenderer.material = CreateRuneRingMaterial();
            }

            // ============================================
            // POINT LIGHT (opzionale)
            // ============================================
            
            if (addLight)
            {
                GameObject lightObj = new GameObject("BeaconLight");
                lightObj.transform.SetParent(parent);
                lightObj.transform.localPosition = new Vector3(0f, 3.5f, 0f); // Sopra il cristallo

                Light pointLight = lightObj.AddComponent<Light>();
                pointLight.type = LightType.Point;
                pointLight.color = new Color(0.4f, 0.7f, 1f); // Azzurro
                pointLight.intensity = 0.5f; // Partenza tenue (giorno)
                pointLight.range = 12f;
                pointLight.shadows = LightShadows.Soft;
            }

            Debug.Log("<color=green>[WaystoneBeaconVisualBuilder]</color> ✓ Visuali del beacon creati (Monolith + Crystal + RuneRing + Light)");
        }

        /// <summary>
        /// Aggiunge/aggiorna solo la luce del beacon.
        /// </summary>
        public static Light EnsureLight(Transform parent)
        {
            Transform existingLight = parent.Find("BeaconLight");
            if (existingLight != null)
            {
                return existingLight.GetComponent<Light>();
            }

            GameObject lightObj = new GameObject("BeaconLight");
            lightObj.transform.SetParent(parent);
            lightObj.transform.localPosition = new Vector3(0f, 3.5f, 0f);

            Light pointLight = lightObj.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = new Color(0.4f, 0.7f, 1f);
            pointLight.intensity = 0.5f;
            pointLight.range = 12f;

            return pointLight;
        }

        /// <summary>
        /// Crea un material URP-first compatibile.
        /// </summary>
        private static Material CreateMaterial(Color baseColor, Color emissionColor, string name)
        {
            Material mat = CreateURPMaterial(name);
            
            // Imposta base color
            SetMaterialBaseColor(mat, baseColor);

            // Imposta emission se presente
            if (emissionColor != Color.clear)
            {
                SetMaterialEmission(mat, emissionColor);
            }

            return mat;
        }

        /// <summary>
        /// Crea material specifico per cristallo con glow ciano HDR.
        /// </summary>
        public static Material CreateCrystalMaterial()
        {
            Material mat = CreateURPMaterial("Waystone_CrystalGlow_URP");
            
            // Base color: ciano chiaro semi-trasparente
            Color baseColor = new Color(0.6f, 0.85f, 1f, 0.9f);
            SetMaterialBaseColor(mat, baseColor);
            
            // Emission ciano HDR (intensità 4)
            Color emissionHDR = new Color(0f, 0.8f, 1f) * 4f;
            SetMaterialEmission(mat, emissionHDR);
            
            // Smoothness alto per effetto cristallo
            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", 0.9f);
            }
            
            Debug.Log("<color=cyan>[WaystoneBeaconVisualBuilder]</color> ✨ Creato materiale cristallo URP con glow ciano HDR");
            return mat;
        }

        /// <summary>
        /// Crea material specifico per pietra opaca.
        /// </summary>
        public static Material CreateStoneMaterial()
        {
            Material mat = CreateURPMaterial("Waystone_Stone_URP");
            
            // Base color: grigio scuro pietra
            Color baseColor = new Color(0.25f, 0.25f, 0.28f, 1f);
            SetMaterialBaseColor(mat, baseColor);
            
            // Smoothness basso per pietra opaca
            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", 0.15f);
            }
            
            // Metallic zero per pietra
            if (mat.HasProperty("_Metallic"))
            {
                mat.SetFloat("_Metallic", 0f);
            }
            
            Debug.Log("<color=gray>[WaystoneBeaconVisualBuilder]</color> 🪨 Creato materiale pietra URP opaco");
            return mat;
        }

        /// <summary>
        /// Crea material per rune ring con emissione tenue.
        /// </summary>
        public static Material CreateRuneRingMaterial()
        {
            Material mat = CreateURPMaterial("Waystone_RuneRing_URP");
            
            // Base color: nero bluastro
            Color baseColor = new Color(0.1f, 0.1f, 0.15f, 1f);
            SetMaterialBaseColor(mat, baseColor);
            
            // Emission tenue ciano
            Color emissionTenue = new Color(0.1f, 0.4f, 0.6f) * 1.5f;
            SetMaterialEmission(mat, emissionTenue);
            
            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", 0.3f);
            }
            
            return mat;
        }

        /// <summary>
        /// Crea material base URP Lit (fallback a Standard se non in URP).
        /// </summary>
        private static Material CreateURPMaterial(string name)
        {
            Shader shader = null;
            
            // Prova shader URP (priorità)
            shader = Shader.Find("Universal Render Pipeline/Lit");
            
            // Fallback: URP Simple Lit
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            }
            
            // Fallback: Built-in Standard (per progetti non-URP)
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }
            
            // Ultimo fallback: shader di default
            if (shader == null)
            {
                Debug.LogWarning($"[WaystoneBeaconVisualBuilder] Nessuno shader trovato! Usando default.");
                return new Material(Shader.Find("Sprites/Default")) { name = name };
            }

            Material mat = new Material(shader);
            mat.name = name;
            
            return mat;
        }

        /// <summary>
        /// Imposta il base color su un materiale (URP o Standard).
        /// </summary>
        private static void SetMaterialBaseColor(Material mat, Color color)
        {
            // URP usa _BaseColor
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }
            // Standard usa _Color
            if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", color);
            }
        }

        /// <summary>
        /// Imposta emission su un materiale (URP o Standard).
        /// </summary>
        private static void SetMaterialEmission(Material mat, Color emissionColor)
        {
            if (!mat.HasProperty("_EmissionColor")) return;
            
            mat.SetColor("_EmissionColor", emissionColor);
            
            // Abilita emission keyword (necessario per URP e Standard)
            mat.EnableKeyword("_EMISSION");
            
            // Per URP, potrebbe servire anche questo flag
            if (mat.HasProperty("_EmissionEnabled"))
            {
                mat.SetFloat("_EmissionEnabled", 1f);
            }
            
            // Assicura che il materiale sia marcato per global illumination
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        }
    }
}

