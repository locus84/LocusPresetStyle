using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEditor.Presets;
using UnityEditorInternal;

namespace PresetStyle
{
    [CustomEditor(typeof(PresetStyleSheet), true)]
    public class PresetStyleSheetInspector : Editor
    {
        SerializedProperty m_ParentList;
        ReorderableList m_StyleList;
        bool m_IsAssemblyReloading = false;
        Dictionary<int, Editor> m_CachedEditors = new Dictionary<int, Editor>();

        void OnEnable()
        {
            m_ParentList = serializedObject.FindProperty(nameof(PresetStyleSheet.ParentSheets));
            m_StyleList = new ReorderableList(serializedObject, serializedObject.FindProperty(nameof(PresetStyleSheet.Styles)));
            m_StyleList.drawHeaderCallback = (Rect rect) => { EditorGUI.LabelField(rect, nameof(PresetStyleSheet.Styles)); };
            m_StyleList.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = m_StyleList.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += 1;
                rect.height -= 2;
                var selector = m_StyleList.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative(nameof(PresetStyleSheet.PresetStyle.Selector));
                EditorGUI.PropertyField(rect, selector);
            };

            m_StyleList.elementHeightCallback += i => EditorGUIUtility.singleLineHeight + 2;
            m_StyleList.onAddCallback += list => 
            {
                ReorderableList.defaultBehaviours.DoAddButton(list);
                var newProp = list.serializedProperty.GetArrayElementAtIndex(list.count - 1);
                newProp.FindPropertyRelative(nameof(PresetStyleSheet.PresetStyle.Selector)).stringValue = string.Empty;
                newProp.FindPropertyRelative(nameof(PresetStyleSheet.PresetStyle.Presets)).ClearArray();
            };
            m_StyleList.onRemoveCallback += list => 
            {
                var to = list.serializedProperty.serializedObject.targetObject;
                var styles = (to as PresetStyleSheet).Styles;
                foreach(var preset in styles[list.index].Presets) Undo.DestroyObjectImmediate(preset);
                ReorderableList.defaultBehaviours.DoRemoveButton(list);
            };
            
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }
        

        void OnDisable()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;

            //we don't need to do anything if assembly is reloading
            if(m_IsAssemblyReloading) return;

            foreach(var kv in m_CachedEditors)
            {
                if(kv.Value == null) continue;
                if(kv.Value.target == null) continue;
                DestroyImmediate(kv.Value);
            }
            m_CachedEditors.Clear();
        }

        private void OnBeforeAssemblyReload() => m_IsAssemblyReloading = true;

        public void SetExpendAll(bool expend)
        {
            foreach (var kv in m_CachedEditors)
            {
                var editor = kv.Value;
                var fi = editor.GetType().GetField("m_InternalEditor", BindingFlags.NonPublic | BindingFlags.Instance);
                var internalEditor = fi.GetValue(editor) as Editor;
                UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(internalEditor.target, expend);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_ParentList);
            m_StyleList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();

            var selectedIndex = m_StyleList.index;
            var styles = (target as PresetStyleSheet).Styles;

            if (selectedIndex < 0 || selectedIndex >= styles.Count) return;

            var selectedStyle = styles[selectedIndex];
            if (selectedStyle.Presets == null) selectedStyle.Presets = new List<Preset>();


            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Collapse All")) SetExpendAll(false);
            if (GUILayout.Button("Expend All")) SetExpendAll(true);
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();
            DropAreaGUI(selectedStyle.Selector, selectedStyle.Presets);

            //copy presets to prevent modifications during loop
            var presets = new List<Preset>(selectedStyle.Presets);
            
            for (int i = 0; i < presets.Count; i++)
            {
                var preset = presets[i];
                if (preset == null)
                {
                    selectedStyle.Presets.RemoveAt(i);
                    EditorUtility.SetDirty(target);
                    GUIUtility.ExitGUI();
                    return;
                }

                var needToAdd = !m_CachedEditors.TryGetValue(preset.GetInstanceID(), out var editor);
                UnityEditor.Editor.CreateCachedEditor(preset, null, ref editor);
                if(needToAdd) m_CachedEditors.Add(preset.GetInstanceID(), editor);
                
                GUILayout.BeginVertical("box");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Include All"))
                {
                    Undo.RecordObject(preset, "Editor");
                    preset.excludedProperties = new string[0];
                }
                if (GUILayout.Button("Exclude All"))
                {
                    Undo.RecordObject(preset, "Editor");
                    ExcludeProperties(preset);
                }
                if (GUILayout.Button("Reverse"))
                {
                    Undo.RecordObject(preset, "Editor");
                    ReverseProperties(preset);
                }

                if (GUILayout.Button("\u2191", GUILayout.MaxWidth(20)))
                {
                    if (i > 0)
                    {
                        Undo.RecordObject(target, "Editor");
                        var prev = selectedStyle.Presets[i - 1];
                        selectedStyle.Presets[i - 1] = preset;
                        selectedStyle.Presets[i] = prev;
                        EditorUtility.SetDirty(target);
                        GUIUtility.ExitGUI();
                        return;
                    }
                }

                if (GUILayout.Button("\u2193", GUILayout.MaxWidth(20)))
                {
                    if (i < selectedStyle.Presets.Count - 1)
                    {
                        Undo.RecordObject(target, "Editor");
                        var next = selectedStyle.Presets[i + 1];
                        selectedStyle.Presets[i + 1] = preset;
                        selectedStyle.Presets[i] = next;
                        GUIUtility.ExitGUI();
                        return;
                    }
                }
                if (GUILayout.Button("X", GUILayout.MaxWidth(20)))
                {
                    Undo.RecordObject(target, "Editor");
                    selectedStyle.Presets.RemoveAt(i);
                    m_CachedEditors.Remove(preset.GetInstanceID());
                    Undo.DestroyObjectImmediate(preset);
                    EditorUtility.SetDirty(target);
                    GUIUtility.ExitGUI();
                    return;
                }
                GUILayout.EndHorizontal();
                editor.serializedObject.Update();                
                editor.OnInspectorGUI();
                editor.serializedObject.ApplyModifiedProperties();
                GUILayout.EndVertical();
            }

            serializedObject.ApplyModifiedProperties();
        }

        public void DropAreaGUI(string selector, List<Preset> presets)
        {
            Event evt = Event.current;
            Rect drop_area = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(drop_area, $"Selector : {selector}\nDrag and drop component(s) here.");

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!drop_area.Contains(evt.mousePosition))
                        return;

                    DragAndDrop.visualMode = DragAndDrop.objectReferences.Where(obj => obj is Component).Any() ?
                        DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (Object dragged_object in DragAndDrop.objectReferences)
                        {
                            if (!(dragged_object is Component)) continue;
                            if (dragged_object is PresetStyleSheetRoot) continue;

                            Undo.RecordObject(target, "Editor");

                            var newPreset = new Preset(dragged_object);
                            newPreset.name = dragged_object.GetType().FullName;
                            bool added = false;

                            for (int i = 0; i < presets.Count; i++)
                            {
                                if (presets[i] == null)
                                {
                                    presets.RemoveAt(i--);
                                    continue;
                                }
                                if (presets[i].GetPresetType() == newPreset.GetPresetType())
                                {
                                    m_CachedEditors.Remove(presets[i].GetInstanceID());
                                    newPreset.excludedProperties = presets[i].excludedProperties;
                                    Undo.DestroyObjectImmediate(presets[i]);
                                    presets[i] = newPreset;
                                    added = true;
                                    break;
                                }
                            }
                            if (!added)
                            {
                                ExcludeProperties(newPreset);
                                presets.Add(newPreset);
                            } 
                            AssetDatabase.AddObjectToAsset(newPreset, target);
                            Undo.RegisterCreatedObjectUndo(newPreset, "Editor");
                        }
                    }
                break;
            }
        }

        static void ExcludeProperties(Preset preset)
        {
            preset.excludedProperties = preset.PropertyModifications
                    .Select(p => GetRootProperty(p.propertyPath))
                    .Union(preset.excludedProperties)
                    .Distinct()
                    .ToArray();
        }
        
        static void ReverseProperties(Preset preset)
        {
            preset.excludedProperties = preset.PropertyModifications
                    .Select(p => GetRootProperty(p.propertyPath))
                    .Distinct()
                    .ToArray();
        }

        static string GetRootProperty(string propertyPath)
        {
            var split = propertyPath.IndexOf('.');
            if (split != -1)
                return propertyPath.Substring(0, split);
            return propertyPath;
        }
    }
}