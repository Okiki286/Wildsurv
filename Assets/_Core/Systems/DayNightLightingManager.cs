using UnityEngine;
using UnityEngine.Rendering;
using WildernessSurvival.Core.Events;

namespace WildernessSurvival.Core.Systems
{
    /// <summary>
    /// Gestisce l'illuminazione e i colori ambientali per il ciclo giorno/notte.
    /// Crea transizioni fluide tra le fasi.
    /// </summary>
    public class DayNightLightingManager : MonoBehaviour
    {
        // ============================================
        // RIFERIMENTI
        // ============================================

        [Header("=== LUCI ===")]
        [Tooltip("Luce direzionale principale (Sole/Luna)")]
        [SerializeField] private Light mainLight;
        
        [Tooltip("Luce del Bonfire (sempre attiva ma più intensa di notte)")]
        [SerializeField] private Light bonfireLight;

        [Header("=== PRESET GIORNO ===")]
        [SerializeField] private Color dayLightColor = new Color(1f, 0.95f, 0.8f);
        [SerializeField] private float dayLightIntensity = 1.2f;
        [SerializeField] private Color dayAmbientColor = new Color(0.7f, 0.75f, 0.8f);
        [SerializeField] private Color daySkyColor = new Color(0.5f, 0.7f, 1f);
        [SerializeField] private Color dayFogColor = new Color(0.8f, 0.85f, 0.9f);
        [SerializeField] private float dayFogDensity = 0.01f;

        [Header("=== PRESET TRAMONTO ===")]
        [SerializeField] private Color sunsetLightColor = new Color(1f, 0.6f, 0.3f);
        [SerializeField] private float sunsetLightIntensity = 0.8f;
        [SerializeField] private Color sunsetAmbientColor = new Color(0.6f, 0.4f, 0.3f);
        [SerializeField] private Color sunsetSkyColor = new Color(1f, 0.5f, 0.2f);

        [Header("=== PRESET NOTTE ===")]
        [SerializeField] private Color nightLightColor = new Color(0.3f, 0.35f, 0.5f);
        [SerializeField] private float nightLightIntensity = 0.3f;
        [SerializeField] private Color nightAmbientColor = new Color(0.1f, 0.1f, 0.2f);
        [SerializeField] private Color nightSkyColor = new Color(0.05f, 0.05f, 0.15f);
        [SerializeField] private Color nightFogColor = new Color(0.1f, 0.1f, 0.15f);
        [SerializeField] private float nightFogDensity = 0.03f;

        [Header("=== BONFIRE ===")]
        [SerializeField] private Color bonfireColor = new Color(1f, 0.6f, 0.2f);
        [SerializeField] private float bonfireDayIntensity = 0.5f;
        [SerializeField] private float bonfireNightIntensity = 2f;
        [SerializeField] private float bonfireFlickerSpeed = 3f;
        [SerializeField] private float bonfireFlickerAmount = 0.2f;

        [Header("=== TRANSIZIONE ===")]
        [SerializeField] private float transitionSpeed = 2f;

        [Header("=== DEBUG ===")]
        [SerializeField] private bool debugMode = false;

        // ============================================
        // RUNTIME
        // ============================================

        private LightingPreset currentPreset;
        private LightingPreset targetPreset;
        private float transitionProgress = 1f;
        private float bonfireBaseIntensity;

        // ============================================
        // STRUCT PRESET
        // ============================================

        private struct LightingPreset
        {
            public Color lightColor;
            public float lightIntensity;
            public Color ambientColor;
            public Color skyColor;
            public Color fogColor;
            public float fogDensity;
            public float bonfireIntensity;
        }

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Start()
        {
            // Auto-find lights se non assegnate
            if (mainLight == null)
            {
                mainLight = FindFirstObjectByType<Light>();
            }

            // Inizializza con preset giorno
            currentPreset = GetDayPreset();
            targetPreset = currentPreset;
            ApplyPreset(currentPreset);

            // Setup bonfire
            if (bonfireLight != null)
            {
                bonfireLight.color = bonfireColor;
                bonfireBaseIntensity = bonfireDayIntensity;
            }
        }

        private void Update()
        {
            // Transizione smooth
            if (transitionProgress < 1f)
            {
                transitionProgress += Time.deltaTime * transitionSpeed;
                transitionProgress = Mathf.Clamp01(transitionProgress);

                LightingPreset lerped = LerpPreset(currentPreset, targetPreset, transitionProgress);
                ApplyPreset(lerped);

                if (transitionProgress >= 1f)
                {
                    currentPreset = targetPreset;
                }
            }

            // Flicker bonfire
            UpdateBonfireFlicker();
        }

        // ============================================
        // PRESET GETTERS
        // ============================================

        private LightingPreset GetDayPreset()
        {
            return new LightingPreset
            {
                lightColor = dayLightColor,
                lightIntensity = dayLightIntensity,
                ambientColor = dayAmbientColor,
                skyColor = daySkyColor,
                fogColor = dayFogColor,
                fogDensity = dayFogDensity,
                bonfireIntensity = bonfireDayIntensity
            };
        }

        private LightingPreset GetSunsetPreset()
        {
            return new LightingPreset
            {
                lightColor = sunsetLightColor,
                lightIntensity = sunsetLightIntensity,
                ambientColor = sunsetAmbientColor,
                skyColor = sunsetSkyColor,
                fogColor = Color.Lerp(dayFogColor, nightFogColor, 0.5f),
                fogDensity = Mathf.Lerp(dayFogDensity, nightFogDensity, 0.5f),
                bonfireIntensity = Mathf.Lerp(bonfireDayIntensity, bonfireNightIntensity, 0.5f)
            };
        }

        private LightingPreset GetNightPreset()
        {
            return new LightingPreset
            {
                lightColor = nightLightColor,
                lightIntensity = nightLightIntensity,
                ambientColor = nightAmbientColor,
                skyColor = nightSkyColor,
                fogColor = nightFogColor,
                fogDensity = nightFogDensity,
                bonfireIntensity = bonfireNightIntensity
            };
        }

        private LightingPreset LerpPreset(LightingPreset a, LightingPreset b, float t)
        {
            return new LightingPreset
            {
                lightColor = Color.Lerp(a.lightColor, b.lightColor, t),
                lightIntensity = Mathf.Lerp(a.lightIntensity, b.lightIntensity, t),
                ambientColor = Color.Lerp(a.ambientColor, b.ambientColor, t),
                skyColor = Color.Lerp(a.skyColor, b.skyColor, t),
                fogColor = Color.Lerp(a.fogColor, b.fogColor, t),
                fogDensity = Mathf.Lerp(a.fogDensity, b.fogDensity, t),
                bonfireIntensity = Mathf.Lerp(a.bonfireIntensity, b.bonfireIntensity, t)
            };
        }

        // ============================================
        // APPLY
        // ============================================

        private void ApplyPreset(LightingPreset preset)
        {
            // Main light
            if (mainLight != null)
            {
                mainLight.color = preset.lightColor;
                mainLight.intensity = preset.lightIntensity;
            }

            // Ambient
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = preset.ambientColor;

            // Skybox color (se usi skybox procedurale)
            // RenderSettings.skybox.SetColor("_Tint", preset.skyColor);

            // Fog
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = preset.fogColor;
            RenderSettings.fogDensity = preset.fogDensity;

            // Bonfire base intensity
            bonfireBaseIntensity = preset.bonfireIntensity;
        }

        private void UpdateBonfireFlicker()
        {
            if (bonfireLight == null) return;

            // Flicker usando Perlin noise per effetto naturale
            float flicker = Mathf.PerlinNoise(Time.time * bonfireFlickerSpeed, 0f);
            flicker = (flicker - 0.5f) * 2f * bonfireFlickerAmount;

            bonfireLight.intensity = bonfireBaseIntensity * (1f + flicker);
        }

        // ============================================
        // API PUBBLICA (chiamata da DayNightSystem via Eventi)
        // ============================================

        /// <summary>
        /// Transizione al giorno
        /// </summary>
        public void TransitionToDay()
        {
            targetPreset = GetDayPreset();
            transitionProgress = 0f;

            if (debugMode)
            {
                Debug.Log("<color=yellow>[Lighting]</color> Transizione a GIORNO");
            }
        }

        /// <summary>
        /// Transizione alla notte
        /// </summary>
        public void TransitionToNight()
        {
            targetPreset = GetNightPreset();
            transitionProgress = 0f;

            if (debugMode)
            {
                Debug.Log("<color=blue>[Lighting]</color> Transizione a NOTTE");
            }
        }

        /// <summary>
        /// Transizione al tramonto (pre-notte)
        /// </summary>
        public void TransitionToSunset()
        {
            targetPreset = GetSunsetPreset();
            transitionProgress = 0f;

            if (debugMode)
            {
                Debug.Log("<color=orange>[Lighting]</color> Transizione a TRAMONTO");
            }
        }

        /// <summary>
        /// Imposta preset istantaneamente (no transizione)
        /// </summary>
        public void SetPresetInstant(bool isDay)
        {
            currentPreset = isDay ? GetDayPreset() : GetNightPreset();
            targetPreset = currentPreset;
            transitionProgress = 1f;
            ApplyPreset(currentPreset);
        }

        // ============================================
        // DEBUG
        // ============================================

        #if UNITY_EDITOR
        [ContextMenu("Debug: Set Day")]
        private void DebugSetDay() => TransitionToDay();

        [ContextMenu("Debug: Set Night")]
        private void DebugSetNight() => TransitionToNight();

        [ContextMenu("Debug: Set Sunset")]
        private void DebugSetSunset() => TransitionToSunset();
        #endif
    }
}
