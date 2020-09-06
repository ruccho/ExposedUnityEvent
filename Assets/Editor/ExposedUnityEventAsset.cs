using System.Collections;
using System.Collections.Generic;
using Ruccho.Utilities;
using UnityEngine;

[CreateAssetMenu(fileName = "Event", menuName = "ExposedUnityEvent")]
public class ExposedUnityEventAsset : ScriptableObject
{
    [SerializeField]
    private ExposedUnityEvent exposed;
}
