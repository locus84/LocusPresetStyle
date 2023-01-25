using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PresetStyle
{
    [DisallowMultipleComponent]
    public class PresetStyleClass : MonoBehaviour
    {
        void OnValidate() => hideFlags = HideFlags.HideInInspector | HideFlags.DontSaveInBuild;

#if UNITY_EDITOR
        [field: SerializeField]
        public string Name { get; set; } = string.Empty;
#endif
    }
}
