using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Ruccho.Utilities
{
    [Serializable]
    public class ExposedUnityEvent
    {
        [SerializeField] private List<ExposedInvokableCall> calls;

        public void Invoke(IExposedPropertyTable resolver)
        {
            foreach (var call in calls)
            {
                call.Invoke(resolver);
            }
        }

    }
}