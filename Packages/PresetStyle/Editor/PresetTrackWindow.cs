using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using TrackInfo = PresetStyle.PresetStyleUtility.TrackInfo;

namespace PresetStyle
{
    public class PresetTrackWindow : EditorWindow
    {
        private bool m_AutoRefresh = true;
        Dictionary<Component, List<TrackInfo>> m_TrackInfos;

        public void SetTrackInfos(Dictionary<Component, List<TrackInfo>> trackInfos)
        {
            m_TrackInfos = trackInfos;
            m_AutoRefresh = false;
        }

        void CollectTrackInfos()
        {
            var result = new Dictionary<Component, List<TrackInfo>>();

            foreach (var go in Selection.gameObjects)
            {
                var style = go.GetComponent<PresetStyleClass>();
                if(style == null) continue;
                if (!PresetStyleUtility.TryGetParentStyleSheetRoot(go, out var styleRoot)) continue;
                var context = new PresetStyleUtility.PresetSheetContext(styleRoot);
                foreach(var component in go.GetComponents<Component>())
                {
                    var trackInfos = context.Apply(component, true);
                    if(trackInfos.Count == 0) continue;
                    result.Add(component, trackInfos);
                }
            }

            m_TrackInfos = result;
        }

        void OnSelectionChange()
        {
            Repaint();
        }

        void OnGUI()
        {
            m_AutoRefresh = GUILayout.Toggle(m_AutoRefresh, "Auto Refresh By Selection");
            EditorGUILayout.Separator();
            if(m_AutoRefresh) CollectTrackInfos();

            if(m_TrackInfos == null || m_TrackInfos.Count == 0) return;

            foreach(var trackInfo in m_TrackInfos)
            {
                EditorGUILayout.Separator();
                EditorGUILayout.ObjectField(trackInfo.Key, trackInfo.Key.GetType(), true);
                for(int i = 0; i < trackInfo.Value.Count; i++)
                {
                    var info = trackInfo.Value[i];
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{nameof(TrackInfo.Match)} : {info.Match}", GUILayout.MinWidth(10));
                    EditorGUILayout.ObjectField(info.Sheet, info.Sheet.GetType(), false);
                    EditorGUILayout.ObjectField(info.Preset, info.Preset.GetType(), false);
                    GUILayout.EndHorizontal();
                }
            }
        }
    }
}

