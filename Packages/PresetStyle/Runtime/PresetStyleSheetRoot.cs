using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace PresetStyle
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class PresetStyleSheetRoot : MonoBehaviour
    {
        void OnValidate() => hideFlags = HideFlags.DontSaveInBuild;

#if UNITY_EDITOR
        public static System.Action<GameObject> ApplyRecursive = null; 

        public PresetStyleSheet Sheet;
        public bool AutoApply = false;

        void Update()
        {
            if (Application.isPlaying || !AutoApply) return;

#if UNITY_2021_2_OR_NEWER
            var isPrefabMode = UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(gameObject) != null;
#else
            var isPrefabMode = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetPrefabStage(gameObject) != null;
#endif
            //means in prefab mode
            var isPersistant = UnityEditor.EditorUtility.IsPersistent(this);
            var parentRefresh = GetComponentsInParent<PresetStyleSheetRoot>().Any(sheet => sheet != this && sheet.AutoApply);
            if ((isPrefabMode || !isPersistant) && !parentRefresh) ApplyRecursive?.Invoke(gameObject);
        }

        public virtual void ApplyPreset(Component component, PresetStyleSheet originSheet, UnityEditor.Presets.Preset preset)
        {
            preset.ApplyTo(component);
        }
#endif
    }
}