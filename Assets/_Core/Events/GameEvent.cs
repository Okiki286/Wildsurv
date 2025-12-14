using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;

namespace WildernessSurvival.Core.Events
{
    /// <summary>
    /// Evento base senza parametri.
    /// Permette comunicazione disaccoppiata tra sistemi.
    /// Supporta sia GameEventListener (inspector) che System.Action (runtime).
    /// </summary>
    [CreateAssetMenu(fileName = "NewGameEvent", menuName = "Wilderness Survival/Events/Game Event")]
    public class GameEvent : ScriptableObject
    {
        [TextArea(2, 4)]
        [SerializeField] private string description;

        private readonly List<GameEventListener> listeners = new List<GameEventListener>();
        private readonly List<Action> actionListeners = new List<Action>();

        #if UNITY_EDITOR
        [SerializeField] private bool debugMode = false;
        #endif

        public void Raise()
        {
            #if UNITY_EDITOR
            if (debugMode)
            {
                Debug.Log($"<color=yellow>[GameEvent]</color> {name} raised - {listeners.Count} component listeners, {actionListeners.Count} action listeners");
            }
            #endif

            // Itera al contrario per sicurezza se un listener si rimuove
            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                if (listeners[i] != null)
                {
                    listeners[i].OnEventRaised();
                }
            }

            // Invoke action listeners
            for (int i = actionListeners.Count - 1; i >= 0; i--)
            {
                actionListeners[i]?.Invoke();
            }
        }

        public void RegisterListener(GameEventListener listener)
        {
            if (!listeners.Contains(listener))
            {
                listeners.Add(listener);
            }
        }

        public void UnregisterListener(GameEventListener listener)
        {
            if (listeners.Contains(listener))
            {
                listeners.Remove(listener);
            }
        }

        /// <summary>
        /// Registra un callback Action (per sottoscrizioni runtime/codice)
        /// </summary>
        public void AddListener(Action callback)
        {
            if (callback != null && !actionListeners.Contains(callback))
            {
                actionListeners.Add(callback);
            }
        }

        /// <summary>
        /// Rimuove un callback Action
        /// </summary>
        public void RemoveListener(Action callback)
        {
            if (callback != null)
            {
                actionListeners.Remove(callback);
            }
        }
    }

    /// <summary>
    /// Componente che ascolta un GameEvent e invoca UnityEvent.
    /// Attaccare a qualsiasi GameObject che deve reagire all'evento.
    /// </summary>
    public class GameEventListener : MonoBehaviour
    {
        [Tooltip("Evento da ascoltare")]
        [SerializeField] private GameEvent gameEvent;
        
        [Tooltip("Azioni da eseguire quando l'evento viene sollevato")]
        [SerializeField] private UnityEvent response;

        private void OnEnable()
        {
            if (gameEvent != null)
            {
                gameEvent.RegisterListener(this);
            }
        }

        private void OnDisable()
        {
            if (gameEvent != null)
            {
                gameEvent.UnregisterListener(this);
            }
        }

        public void OnEventRaised()
        {
            response?.Invoke();
        }
    }
}
