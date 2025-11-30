using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace WildernessSurvival.Core.Events
{
    // ============================================
    // EVENTO CON PARAMETRO FLOAT
    // ============================================
    
    [CreateAssetMenu(fileName = "NewFloatEvent", menuName = "Wilderness Survival/Events/Float Event")]
    public class FloatEvent : ScriptableObject
    {
        [TextArea(2, 4)]
        [SerializeField] private string description;
        
        private readonly List<FloatEventListener> listeners = new List<FloatEventListener>();

        public void Raise(float value)
        {
            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                if (listeners[i] != null)
                {
                    listeners[i].OnEventRaised(value);
                }
            }
        }

        public void RegisterListener(FloatEventListener listener)
        {
            if (!listeners.Contains(listener))
                listeners.Add(listener);
        }

        public void UnregisterListener(FloatEventListener listener)
        {
            listeners.Remove(listener);
        }
    }

    public class FloatEventListener : MonoBehaviour
    {
        [SerializeField] private FloatEvent gameEvent;
        [SerializeField] private UnityEvent<float> response;

        private void OnEnable() => gameEvent?.RegisterListener(this);
        private void OnDisable() => gameEvent?.UnregisterListener(this);
        public void OnEventRaised(float value) => response?.Invoke(value);
    }

    // ============================================
    // EVENTO CON PARAMETRO INT
    // ============================================
    
    [CreateAssetMenu(fileName = "NewIntEvent", menuName = "Wilderness Survival/Events/Int Event")]
    public class IntEvent : ScriptableObject
    {
        [TextArea(2, 4)]
        [SerializeField] private string description;
        
        private readonly List<IntEventListener> listeners = new List<IntEventListener>();

        public void Raise(int value)
        {
            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                if (listeners[i] != null)
                {
                    listeners[i].OnEventRaised(value);
                }
            }
        }

        public void RegisterListener(IntEventListener listener)
        {
            if (!listeners.Contains(listener))
                listeners.Add(listener);
        }

        public void UnregisterListener(IntEventListener listener)
        {
            listeners.Remove(listener);
        }
    }

    public class IntEventListener : MonoBehaviour
    {
        [SerializeField] private IntEvent gameEvent;
        [SerializeField] private UnityEvent<int> response;

        private void OnEnable() => gameEvent?.RegisterListener(this);
        private void OnDisable() => gameEvent?.UnregisterListener(this);
        public void OnEventRaised(int value) => response?.Invoke(value);
    }

    // ============================================
    // EVENTO CON PARAMETRO STRING
    // ============================================
    
    [CreateAssetMenu(fileName = "NewStringEvent", menuName = "Wilderness Survival/Events/String Event")]
    public class StringEvent : ScriptableObject
    {
        private readonly List<StringEventListener> listeners = new List<StringEventListener>();

        public void Raise(string value)
        {
            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                if (listeners[i] != null)
                {
                    listeners[i].OnEventRaised(value);
                }
            }
        }

        public void RegisterListener(StringEventListener listener)
        {
            if (!listeners.Contains(listener))
                listeners.Add(listener);
        }

        public void UnregisterListener(StringEventListener listener)
        {
            listeners.Remove(listener);
        }
    }

    public class StringEventListener : MonoBehaviour
    {
        [SerializeField] private StringEvent gameEvent;
        [SerializeField] private UnityEvent<string> response;

        private void OnEnable() => gameEvent?.RegisterListener(this);
        private void OnDisable() => gameEvent?.UnregisterListener(this);
        public void OnEventRaised(string value) => response?.Invoke(value);
    }

    // ============================================
    // EVENTO CON PARAMETRO VECTOR3
    // ============================================
    
    [CreateAssetMenu(fileName = "NewVector3Event", menuName = "Wilderness Survival/Events/Vector3 Event")]
    public class Vector3Event : ScriptableObject
    {
        private readonly List<Vector3EventListener> listeners = new List<Vector3EventListener>();

        public void Raise(Vector3 value)
        {
            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                if (listeners[i] != null)
                {
                    listeners[i].OnEventRaised(value);
                }
            }
        }

        public void RegisterListener(Vector3EventListener listener)
        {
            if (!listeners.Contains(listener))
                listeners.Add(listener);
        }

        public void UnregisterListener(Vector3EventListener listener)
        {
            listeners.Remove(listener);
        }
    }

    public class Vector3EventListener : MonoBehaviour
    {
        [SerializeField] private Vector3Event gameEvent;
        [SerializeField] private UnityEvent<Vector3> response;

        private void OnEnable() => gameEvent?.RegisterListener(this);
        private void OnDisable() => gameEvent?.UnregisterListener(this);
        public void OnEventRaised(Vector3 value) => response?.Invoke(value);
    }
}
