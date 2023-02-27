using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace PresetStyle
{
    public class PresetStyleHierarchy
    {
        static GUIStyle s_GUIStyle;

        [InitializeOnLoadMethod]
        static void DecorateHPresetStyleHierarchy()
        {
            s_GUIStyle = new GUIStyle();
            EditorApplication.delayCall += () =>
            {
                s_GUIStyle.alignment = TextAnchor.MiddleRight;
                s_GUIStyle.normal.textColor = Color.gray;
                s_GUIStyle.fontSize -= 3;
            };
            EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
        }
        private static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect rect)
        {
            //code to edit the hierarchy here        
            var inst = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (inst == null) return;
            var style = inst.GetComponent<PresetStyleClass>();
            if (style == null && !Selection.Contains(inst)) return;

            var textRect = rect;
            textRect.width -= 26;

            if(style != null) EditorGUI.LabelField(textRect, style.Name, s_GUIStyle);

            var buttonRect = rect;
            buttonRect.x = rect.x + rect.width - 23;
            buttonRect.width = 20;
            var isActive = inst.gameObject == Selection.activeGameObject;
            var keyClicked = isActive && Event.current.modifiers == EventModifiers.None && Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.S;
            if (keyClicked || GUI.Button(buttonRect, new GUIContent(style == null? "A" : "S", style == null? "Add Preset Style" : "Edit Preset Style")))
            {
                PresetStyleClassPopup editor = Editor.CreateInstance<PresetStyleClassPopup>();

                rect.position = EditorGUIUtility.GUIToScreenPoint(rect.position);
                rect.x += rect.width - 5;
                rect.y -= 20;
                if(PresetStyleUtility.TryGetParentStyleSheetRoot(inst, out var styleRoot))
                {
                    editor.SetTarget(inst, styleRoot);
                    editor.ShowAsDropDown(rect, new Vector2(400, 46));
                }
                else
                {
                    editor.SetTarget(inst);
                    editor.ShowAsDropDown(rect, new Vector2(400, 23));
                }
            }
        }
    }

    public class PresetStyleClassPopup : EditorWindow
    {
        GameObject m_Target;
        string m_StyleString;

        PresetStyleSheetRoot m_PresetRoot;

        public void SetTarget(GameObject target, PresetStyleSheetRoot root = null)
        {
            m_Target = target;
            m_PresetRoot = root;
            var style = target.GetComponent<PresetStyle.PresetStyleClass>();
            m_StyleString = style == null ? string.Empty : style.Name;
        }

        void OnGUI()
        {
            if (m_Target == null) return;
            GUILayout.BeginHorizontal();
            GUI.SetNextControlName("StyleTextField");
            m_StyleString = GUILayout.TextField(m_StyleString);
            if (GUILayout.Button("Save", GUILayout.MaxWidth(60)) || Event.current.keyCode == KeyCode.Return)
            {
                var style = m_Target.GetComponent<PresetStyle.PresetStyleClass>();

                if (!string.IsNullOrWhiteSpace(m_StyleString))
                {
                    if(style == null) style = Undo.AddComponent<PresetStyle.PresetStyleClass>(m_Target);
                    Undo.RecordObject(style, "Editor");
                    style.Name = m_StyleString;
                    PresetStyleUtility.Apply(new[] { style.gameObject });
                    EditorUtility.SetDirty(style);
                }
                else
                {
                    if(style != null) Undo.DestroyObjectImmediate(style);
                }

                Close();
            }
            if (Event.current.keyCode == KeyCode.Escape) Close();
            EditorGUI.FocusTextInControl("StyleTextField");
            GUILayout.EndHorizontal();

            if(m_PresetRoot != null)
            {
                GUILayout.BeginHorizontal();
                var selectors = new List<string>();
                var current = m_StyleString.Split('.');
                m_PresetRoot.Sheet.CollectSelectors(selectors);
                selectors = selectors.SelectMany(s => s.Split('.')).Distinct().Except(current).ToList();

                foreach (var selector in selectors)
                {
                    if (GUILayout.Button(selector)) 
                    {
                        m_StyleString = string.IsNullOrWhiteSpace(m_StyleString)? selector : string.Join(PresetStyleUtility.SEPERATOR, m_StyleString, selector);
                    }
                }

                GUILayout.EndHorizontal();
            }
        }
    }
}
