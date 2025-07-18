﻿using Event.Events;
using Event.Unity_Events;
using UnityEngine;
using UnityEngine.Events;

namespace Event.Listeners
{
    [AddComponentMenu("Farming Kit/Events/Bool Event Listener")]
    public class BoolEventListener : ScriptableEventListener<bool>
    {
        [SerializeField]
        protected BoolEvent eventObject;

        private UnityEventBool eventAction = new UnityEventBool();

        [SerializeField]
        private UnityEvent onTrue;

        [SerializeField]
        private UnityEvent onFalse;

        private void Awake()
        {
            eventAction.AddListener((_state) =>
            {
                if (_state == true)
                {
                    onTrue.Invoke();
                }
                else
                    onFalse.Invoke();
            });
        }

        protected override ScriptableEvent<bool> ScriptableEvent
        {
            get
            {
                return eventObject;
            }
        }

        protected override UnityEvent<bool> Action
        {
            get
            {
                return eventAction;
            }
        }
    }
}
