using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ruccho.Utilities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[Serializable]
public class UnityEventString : UnityEvent<string>{}
public class EventContainer : MonoBehaviour, IExposedPropertyTable
{
    [SerializeField] private UnityEventString ue;
    [SerializeField] private ExposedUnityEvent eue;
    
    // Start is called before the first frame update
    void Start()
    {
        eue.Invoke(this);
        /*
        var d = CreateDelegateTest();
        d.DynamicInvoke("aaa", 0);
        */
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Sample(GameObject e)
    {
        
    }


    [SerializeField, HideInInspector] private List<ExposedReferenceEntry> exposedReferences;
    public void SetReferenceValue(PropertyName id, Object value)
    {
        var entry = exposedReferences.FirstOrDefault(e => e.id == id);
        if (entry != default)
        {
            entry.value = value;
        }
        else
        {
            exposedReferences.Add(new ExposedReferenceEntry(id, value));
        }
    }

    public Object GetReferenceValue(PropertyName id, out bool idValid)
    {
        var entry = exposedReferences.FirstOrDefault(e => e.id == id);
        if (entry != default)
        {
            idValid = true;
            return entry.value;
        }
        else
        {
            idValid = false;
            return null;
        }
    }

    public void ClearReferenceValue(PropertyName id)
    {
        var entry = exposedReferences.FirstOrDefault(e => e.id == id);
        if (entry != default)
        {
            exposedReferences.Remove(entry);
        }
    }

    [Serializable]
    public class ExposedReferenceEntry
    {
        public PropertyName id;
        public Object value;

        public ExposedReferenceEntry(PropertyName id, Object value)
        {
            this.id = id;
            this.value = value;
        }
    }
}
