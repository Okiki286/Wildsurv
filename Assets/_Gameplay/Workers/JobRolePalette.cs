using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using WildernessSurvival.Gameplay.Structures;

namespace WildernessSurvival.Gameplay.Workers
{
    /// <summary>
    /// Palette centralizzata dei colori standard per ogni WorkerRole.
    /// Usata dal JobAuthoringWindow e dai WorkerVisualSet per garantire coerenza visiva.
    /// </summary>
    [CreateAssetMenu(fileName = "JobRolePalette", menuName = "Wilderness Survival/Jobs/Job Role Palette")]
    public class JobRolePalette : ScriptableObject
    {
        // ============================================
        // ROLE COLOR ENTRY
        // ============================================

        [System.Serializable]
        public class RoleColorEntry
        {
            [HorizontalGroup("Entry", 100)]
            [LabelWidth(60)]
            [EnumToggleButtons]
            [Tooltip("Ruolo worker")]
            public WorkerRole role;

            [HorizontalGroup("Entry")]
            [LabelWidth(60)]
            [ColorPalette]
            [Tooltip("Colore identificativo del ruolo")]
            public Color color = Color.white;

            [HorizontalGroup("Entry", 150)]
            [LabelWidth(80)]
            [Tooltip("Nome leggibile (opzionale)")]
            public string displayName;
        }

        // ============================================
        // PALETTE DATA
        // ============================================

        [TitleGroup("Color Palette")]
        [InfoBox("Definisci i colori standard per ogni ruolo worker. Questi colori saranno applicati automaticamente ai Job quando creati.", InfoMessageType.Info)]
        [ListDrawerSettings(DefaultExpandedState = true, ShowIndexLabels = false, DraggableItems = true)]
        [SerializeField]
        private List<RoleColorEntry> roleColors = new List<RoleColorEntry>
        {
            new RoleColorEntry { role = WorkerRole.None, color = new Color(0.8f, 0.8f, 0.8f), displayName = "Villager" },
            new RoleColorEntry { role = WorkerRole.Gatherer, color = new Color(0.4f, 0.8f, 0.4f), displayName = "Gatherer" },
            new RoleColorEntry { role = WorkerRole.Builder, color = new Color(0.8f, 0.5f, 0.2f), displayName = "Builder" },
            new RoleColorEntry { role = WorkerRole.Guard, color = new Color(0.8f, 0.2f, 0.2f), displayName = "Guard" },
            new RoleColorEntry { role = WorkerRole.Scout, color = new Color(0.3f, 0.6f, 0.9f), displayName = "Scout" },
            new RoleColorEntry { role = WorkerRole.Crafter, color = new Color(0.7f, 0.4f, 0.8f), displayName = "Crafter" },
            new RoleColorEntry { role = WorkerRole.Researcher, color = new Color(0.2f, 0.7f, 0.9f), displayName = "Researcher" }
        };

        public List<RoleColorEntry> RoleColors => roleColors;

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Ottiene il colore associato a un ruolo.
        /// Se non trovato, restituisce il fallback.
        /// </summary>
        public Color GetColor(WorkerRole role, Color fallback = default)
        {
            if (roleColors == null || roleColors.Count == 0)
                return fallback != default ? fallback : Color.white;

            foreach (var entry in roleColors)
            {
                if (entry.role == role)
                    return entry.color;
            }

            return fallback != default ? fallback : Color.white;
        }

        /// <summary>
        /// Verifica se un ruolo ha un colore definito nella palette.
        /// </summary>
        public bool HasColor(WorkerRole role)
        {
            if (roleColors == null) return false;

            foreach (var entry in roleColors)
            {
                if (entry.role == role)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Imposta il colore per un ruolo.
        /// Se il ruolo esiste già, aggiorna il colore. Altrimenti, aggiunge una nuova entry.
        /// </summary>
        public void SetColor(WorkerRole role, Color color, string displayName = "")
        {
            if (roleColors == null)
                roleColors = new List<RoleColorEntry>();

            // Cerca entry esistente
            foreach (var entry in roleColors)
            {
                if (entry.role == role)
                {
                    entry.color = color;
                    if (!string.IsNullOrEmpty(displayName))
                        entry.displayName = displayName;
                    return;
                }
            }

            // Aggiungi nuova entry
            roleColors.Add(new RoleColorEntry
            {
                role = role,
                color = color,
                displayName = displayName
            });
        }

        // ============================================
        // EDITOR HELPERS
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug")]
        [Button("Print Palette", ButtonSizes.Medium)]
        private void DebugPrintPalette()
        {
            if (roleColors == null || roleColors.Count == 0)
            {
                Debug.LogWarning("[JobRolePalette] Palette is empty!");
                return;
            }

            string output = "=== JOB ROLE PALETTE ===\n";
            foreach (var entry in roleColors)
            {
                output += $"• {entry.role} ({entry.displayName}): RGB({entry.color.r:F2}, {entry.color.g:F2}, {entry.color.b:F2})\n";
            }

            Debug.Log(output);
        }

        [TitleGroup("Debug")]
        [Button("Add Missing Roles", ButtonSizes.Medium)]
        [InfoBox("Aggiunge automaticamente i ruoli mancanti dalla palette con colori di default.")]
        private void AddMissingRoles()
        {
            var allRoles = System.Enum.GetValues(typeof(WorkerRole));
            int added = 0;

            foreach (WorkerRole role in allRoles)
            {
                if (!HasColor(role))
                {
                    // Colore di default basato su hash del nome ruolo
                    Color defaultColor = GenerateColorFromRole(role);
                    SetColor(role, defaultColor, role.ToString());
                    added++;
                }
            }

            if (added > 0)
            {
                UnityEditor.EditorUtility.SetDirty(this);
                Debug.Log($"<color=green>[JobRolePalette]</color> Added {added} missing roles to palette.");
            }
            else
            {
                Debug.Log("[JobRolePalette] All roles already have colors assigned.");
            }
        }

        private Color GenerateColorFromRole(WorkerRole role)
        {
            // Genera un colore pseudo-random ma consistente basato sul ruolo
            int hash = role.GetHashCode();
            UnityEngine.Random.InitState(hash);
            return new Color(
                UnityEngine.Random.Range(0.3f, 0.9f),
                UnityEngine.Random.Range(0.3f, 0.9f),
                UnityEngine.Random.Range(0.3f, 0.9f)
            );
        }
#endif
    }
}
