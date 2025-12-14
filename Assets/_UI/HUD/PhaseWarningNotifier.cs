using UnityEngine;
using WildernessSurvival.Core.Events;
using Sirenix.OdinInspector;

namespace WildernessSurvival.UI
{
    /// <summary>
    /// Notifica il giocatore quando sta per cambiare fase (Day/Night).
    /// MVP: stampa in console. Estendibile per UI toast/banner.
    /// </summary>
    public class PhaseWarningNotifier : MonoBehaviour
    {
        // ============================================
        // EVENTS
        // ============================================

        [TitleGroup("Game Events")]
        [Tooltip("Evento sollevato 30s prima della fine del giorno")]
        [SerializeField] private GameEvent onDayEnding;

        [Tooltip("Evento sollevato 30s prima della fine della notte")]
        [SerializeField] private GameEvent onNightEnding;

        // ============================================
        // SETTINGS
        // ============================================

        [TitleGroup("Settings")]
        [Tooltip("Messaggio mostrato quando sta arrivando la notte")]
        [SerializeField] private string nightIncomingMessage = "Night incoming!";

        [Tooltip("Messaggio mostrato quando sta arrivando l'alba")]
        [SerializeField] private string dawnSoonMessage = "Dawn soon!";

        [TitleGroup("Debug")]
        [SerializeField] private bool logToConsole = true;

        // ============================================
        // STATE
        // ============================================

        private bool isInitialized = false;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            isInitialized = true;
        }

        private void OnEnable()
        {
            if (!isInitialized) return;

            if (onDayEnding != null)
            {
                onDayEnding.AddListener(OnDayEnding);
            }

            if (onNightEnding != null)
            {
                onNightEnding.AddListener(OnNightEnding);
            }
        }

        private void OnDisable()
        {
            if (onDayEnding != null)
            {
                onDayEnding.RemoveListener(OnDayEnding);
            }

            if (onNightEnding != null)
            {
                onNightEnding.RemoveListener(OnNightEnding);
            }
        }

        // ============================================
        // EVENT HANDLERS
        // ============================================

        private void OnDayEnding()
        {
            ShowWarning(nightIncomingMessage, WarningType.NightIncoming);
        }

        private void OnNightEnding()
        {
            ShowWarning(dawnSoonMessage, WarningType.DawnSoon);
        }

        // ============================================
        // DISPLAY
        // ============================================

        private void ShowWarning(string message, WarningType type)
        {
            // Console log (MVP)
            if (logToConsole)
            {
                string color = type == WarningType.NightIncoming ? "orange" : "cyan";
                string icon = type == WarningType.NightIncoming ? "!" : "*";
                Debug.Log($"<color={color}>[{icon}]</color> <b>{message}</b>");
            }

            // TODO: Integrate with UI system when available
            // Example integration points:
            // - ToastUI.Show(message);
            // - HUDBanner.Display(message, duration);
            // - GameHUD.ShowWarningBanner(message);
        }

        private enum WarningType
        {
            NightIncoming,
            DawnSoon
        }

        // ============================================
        // PUBLIC API (for external UI integration)
        // ============================================

        /// <summary>
        /// Evento C# per integrare con altri sistemi UI
        /// </summary>
        public static event System.Action<string> OnWarningTriggered;

        private void NotifyExternalSystems(string message)
        {
            OnWarningTriggered?.Invoke(message);
        }

        // ============================================
        // DEBUG
        // ============================================

#if UNITY_EDITOR
        [TitleGroup("Debug")]
        [ButtonGroup("Debug/Row")]
        [Button("Test Night Warning", ButtonSizes.Medium)]
        [GUIColor(1f, 0.6f, 0.3f)]
        private void DebugTestNightWarning()
        {
            ShowWarning(nightIncomingMessage, WarningType.NightIncoming);
        }

        [ButtonGroup("Debug/Row")]
        [Button("Test Dawn Warning", ButtonSizes.Medium)]
        [GUIColor(0.3f, 0.8f, 1f)]
        private void DebugTestDawnWarning()
        {
            ShowWarning(dawnSoonMessage, WarningType.DawnSoon);
        }
#endif
    }
}
