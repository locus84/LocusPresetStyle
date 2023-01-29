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

        [Range(-100, 100)]
        public int SheetPriority;
        public List<PresetStyle> Styles = new List<PresetStyle>();

        [System.Serializable]
        public class PresetStyle
        {
            public string Selector;

            [Range(-100, 100)]
            public int Priority;
            public List<UnityEditor.Presets.Preset> Presets = new List<UnityEditor.Presets.Preset>();
        }

        public void CollectSelectors(List<string> results, HashSet<PresetStyleSheet> processed = null)
        {
            if(processed == null) processed = new HashSet<PresetStyleSheet>();
            if(!processed.Add(this)) return;
            foreach(var style in Styles) results.Add(style.Selector);
            foreach(var parents in ParentSheets) parents.CollectSelectors(results, processed);
        }
#endif
    }
}
