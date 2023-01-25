using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace PresetStyle
{
    [CreateAssetMenu]
    public class PresetStyleSheet : ScriptableObject
    {
#if UNITY_EDITOR
        public List<PresetStyleSheet> ParentSheets;
        public List<PresetStyle> Styles = new List<PresetStyle>();

        [System.Serializable]
        public class PresetStyle
        {
            public string Selector;
            public List<UnityEditor.Presets.Preset> Presets = new List<UnityEditor.Presets.Preset>();
        }

        public void CollectSelectors(List<string> results)
        {
            foreach(var parents in ParentSheets) parents.CollectSelectors(results);
            foreach(var style in Styles) results.Add(style.Selector);
        }
#endif
    }
}
