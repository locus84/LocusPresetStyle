using UnityEngine;
using UnityEditor;
using UnityEditor.Presets;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;

namespace PresetStyle
{
    public static class PresetStyleUtility
    {
        static string s_StyleClipboard = string.Empty;

        [InitializeOnLoadMethod]
        static void InitializeCallback()
        {
            PresetStyleSheetRoot.ApplyRecursive = root => new PresetSheetContext(root).Apply(root.gameObject, true);
        }

        public const string SEPERATOR = ".";
        public const char SEPERATOR_CHAR = '.';
        const int EDITOR_ORDER = 1;

        [MenuItem("CONTEXT/Component/Apply Preset Style", true, EDITOR_ORDER)]
        public static bool ApplyComponentValidation(MenuCommand command)
        {
            var component = command.context as Component;
            return component != null && component.GetComponent<PresetStyleClass>() != null;
        }

        [MenuItem("CONTEXT/Component/Apply Preset Style", false, EDITOR_ORDER)]
        public static void ApplyComponent(MenuCommand command)
        {
            var component = command.context as Component;
            if (!TryGetParentStyleSheetRoot(component.gameObject, out var styleRoot)) 
            {
                Debug.LogWarning($"StyleSheetRoot component is not found in GameObject({component.gameObject.name})'s parents!");
                return;
            }
            var context = new PresetSheetContext(styleRoot);
            context.Apply(component);
        }

        [MenuItem("CONTEXT/Component/Analyze Preset Style", true, EDITOR_ORDER)]
        public static bool AnalyzeComponentValidation(MenuCommand command)
        {
            var component = command.context as Component;
            return component != null && component.GetComponent<PresetStyleClass>() != null;
        }

        [MenuItem("CONTEXT/Component/Analyze Preset Style", false, EDITOR_ORDER)]
        public static void AnalyzeComponent(MenuCommand command)
        {
            var component = command.context as Component;
            if (!TryGetParentStyleSheetRoot(component.gameObject, out var styleRoot))    
            {
                Debug.LogWarning($"StyleSheetRoot component is not found in GameObject({component.gameObject.name})'s parents!");
                return;
            }
            var context = new PresetSheetContext(styleRoot);
            var trackInfos = context.Apply(component, true);
            var result = new Dictionary<Component, List<TrackInfo>>();
            if(trackInfos.Count > 0) result.Add(component, trackInfos);

            var trackEditor = EditorWindow.GetWindow<PresetTrackWindow>("Preset Style Analyzer");
            trackEditor.SetTrackInfos(result);
            trackEditor.Show();
        }

        
        [MenuItem("CONTEXT/Component/Overwrite Preset Style", true, EDITOR_ORDER)]
        public static bool OverrideComponentValidation(MenuCommand command)
        {
            var component = command.context as Component;

            //must be a presetstyle
            if (component == null || component.GetComponent<PresetStyleClass>() == null) return false;

            //must be a root
            if (!PresetStyleUtility.TryGetParentStyleSheetRoot(component.gameObject, out var styleRoot)) return false;
            var context = new PresetStyleUtility.PresetSheetContext(styleRoot);
            var trackInfos = context.Apply(component, true);
            return trackInfos.Count > 0;
        }
        
        [MenuItem("CONTEXT/Component/Overwrite Preset Style", false, EDITOR_ORDER)]
        public static void OverrideComponent(MenuCommand command)
        {
            var field = typeof ( Event ).GetField ( "s_Current", BindingFlags.Static | BindingFlags.NonPublic );
            Event current = field.GetValue(null) as Event;
            if (current == null) return;
            var position = current.mousePosition;

            var component = command.context as Component;

            //must be a root
            PresetStyleUtility.TryGetParentStyleSheetRoot(component.gameObject, out var styleRoot);
            var context = new PresetStyleUtility.PresetSheetContext(styleRoot);
            var trackInfos = context.Apply(component, true);

            var selections = trackInfos.Select(info => new GUIContent($"{info.Match}({info.Sheet.name})")).ToArray();
            
            EditorUtility.DisplayCustomMenu(new Rect(position.x, position.y, 0, 0), selections, -1, (userdata, options, selected) => {
                if(selected >= 0)
                {
                    var info = trackInfos[selected];
                    Debug.Log(ReplacePreset(info, component));

                    // var testPreset = AssetDatabase.LoadAssetAtPath<Preset>("Assets/Sample/GettingStarted/Text.preset");
                    // Undo.RegisterCompleteObjectUndo(testPreset, "Editor");
                    // Debug.Log(testPreset.UpdateProperties(component));
                    // Undo.RecordObject(info.Preset, "Editor");
                    // var mod = info.Preset.PropertyModifications;
                    

                    // EditorUtility.SetDirty(info.Preset);
                    // EditorUtility.SetDirty(info.Sheet);
                } 
            }, null );
            return;
        }

        [MenuItem("GameObject/Preset Style/Add Preset Style", true, EDITOR_ORDER)]
        public static bool AddStyleValidation()
        {
            return Selection.gameObjects.Any(go => go.GetComponent<PresetStyleClass>() == null);
        }

        [MenuItem("GameObject/Preset Style/Add Preset Style", false, EDITOR_ORDER)]
        public static void AddStyle(MenuCommand command)
        {
            foreach (var go in Selection.gameObjects)
            {
                if (go.GetComponent<PresetStyleClass>() != null) continue;
                UnityEditor.Undo.AddComponent<PresetStyleClass>(go);
            }
        }

        [MenuItem("GameObject/Preset Style/Copy Preset Style", true, EDITOR_ORDER)]
        public static bool CopyStyleValidation()
        {
            if (Selection.activeGameObject == null) return false;
            return Selection.activeGameObject.GetComponent<PresetStyleClass>() != null;
        }

        [MenuItem("GameObject/Preset Style/Copy Preset Style", false, EDITOR_ORDER)]
        public static void CopyStyle()
        {
            s_StyleClipboard = Selection.activeGameObject.GetComponent<PresetStyleClass>().Name;
        }

        [MenuItem("GameObject/Preset Style/Paste Preset Style", true, EDITOR_ORDER)]
        public static bool PasteStyleValidation() => !string.IsNullOrWhiteSpace(s_StyleClipboard) && Selection.gameObjects.Length > 0;

        [MenuItem("GameObject/Preset Style/Paste Preset Style", false, EDITOR_ORDER)]
        public static void PasteStyle()
        {
            foreach (var go in Selection.gameObjects)
            {
                if (!go.TryGetComponent<PresetStyleClass>(out var style))
                {
                    style = Undo.AddComponent<PresetStyleClass>(go);
                }

                Undo.RecordObject(style, "Editor");
                style.Name = s_StyleClipboard;
                Apply(new [] { go });
            }
        }

        [MenuItem("GameObject/Preset Style/Apply Preset Style", true, EDITOR_ORDER)]
        public static bool ApplyValidation()
        {
            return Selection.gameObjects.Any(go => go.GetComponent<PresetStyleClass>() != null);
        }

        [MenuItem("GameObject/Preset Style/Apply Preset Style", false, EDITOR_ORDER)]
        public static void Apply() => Apply(Selection.gameObjects);

        public static void Apply(IEnumerable<GameObject> gameObjects)
        {
            var sheetContextCache = new Dictionary<PresetStyleSheetRoot, PresetSheetContext>();
            foreach (var go in gameObjects)
            {
                if (!TryGetParentStyleSheetRoot(go, out var styleRoot)) continue;
                if (!sheetContextCache.TryGetValue(styleRoot, out var context))
                {
                    context = new PresetSheetContext(styleRoot);
                    sheetContextCache.Add(styleRoot, context);
                }
                context.Apply(go, false);
            }
        }

        [MenuItem("GameObject/Preset Style/Apply Preset Style Recursive", true, EDITOR_ORDER)]
        public static bool ApplyRecursiveValidation() => Selection.gameObjects.Length > 0;

        [MenuItem("GameObject/Preset Style/Apply Preset Style Recursive", false, EDITOR_ORDER)]
        public static void ApplyRecursive() => ApplyRecursive(Selection.gameObjects);

        public static void ApplyRecursive(IEnumerable<GameObject> gameObjects)
        {
            var styles = new HashSet<PresetStyleClass>();
            
            foreach (var go in gameObjects) styles.UnionWith(go.GetComponentsInChildren<PresetStyleClass>(true));

            var contextDict = new Dictionary<PresetStyleSheetRoot, PresetSheetContext>();

            foreach(var style in styles)
            {
                if (!TryGetParentStyleSheetRoot(style.gameObject, out var styleRoot)) continue;
                
                if(!contextDict.TryGetValue(styleRoot, out var context))
                {
                    context = new PresetSheetContext(styleRoot);
                    contextDict.Add(styleRoot, context);
                }
                
                context.Apply(style.gameObject, false);
            }
        }

        [MenuItem("GameObject/Preset Style/Remove Preset Style", true, EDITOR_ORDER)]
        public static bool RemoveValidation()
        {
            return Selection.gameObjects.Any(go => go.GetComponent<PresetStyleClass>() != null);
        }

        [MenuItem("GameObject/Preset Style/Remove Preset Style", false, EDITOR_ORDER)]
        public static void Remove()
        {
            foreach (var go in Selection.gameObjects)
            {
                var style = go.GetComponent<PresetStyleClass>();
                if(style != null) Undo.DestroyObjectImmediate(style);
            }
        }
        
        [MenuItem("GameObject/Preset Style/Analyze Preset Style", true, EDITOR_ORDER)]
        public static bool AnalyzeGameObjectValidation()
        {
            return Selection.gameObjects.Any(go => go.GetComponent<PresetStyleClass>() != null);
        }

        [MenuItem("GameObject/Preset Style/Analyze Preset Style", false, EDITOR_ORDER)]
        public static void AnalyzeGameObject()
        {
            var result = new Dictionary<Component, List<TrackInfo>>();

            foreach (var go in Selection.gameObjects)
            {
                var style = go.GetComponent<PresetStyleClass>();
                if(style == null) continue;
                if (!TryGetParentStyleSheetRoot(go, out var styleRoot)) 
                {
                    Debug.LogWarning($"StyleSheetRoot component is not found in GameObject({go.name})'s parents!");
                    continue;
                }
                
                var context = new PresetSheetContext(styleRoot);
                foreach(var component in go.GetComponents<Component>())
                {
                    var trackInfos = context.Apply(component, true);
                    if(trackInfos.Count == 0) continue;
                    result.Add(component, trackInfos);
                }
            }

            var trackEditor = EditorWindow.GetWindow<PresetTrackWindow>("Preset Style Analyzer");
            trackEditor.SetTrackInfos(result);
            trackEditor.Show();
        }
        
        [MenuItem("Window/Preset Style/Show Anlayzer Window", false, 300)]
        public static void ShowAnalyzerWindow() => EditorWindow.GetWindow<PresetTrackWindow>("Preset Style Analyzer").Show();

        public static bool TryGetParentStyleSheetRoot(GameObject gameObject, out PresetStyleSheetRoot styleRoot)
        {
            if(!gameObject.TryGetComponent<PresetStyleSheetRoot>(out styleRoot))
            {
                styleRoot = gameObject.GetComponentInParent<PresetStyleSheetRoot>();
            }
            if (styleRoot != null) return true;
            return false;
        }

        public static bool TryValidateSelector(string selector, out string validated)
        {
            if (string.IsNullOrWhiteSpace(selector))
            {
                validated = default;
                return false;
            }

            validated = string.Join(SEPERATOR, selector.Split(SEPERATOR_CHAR).OrderBy(x => x));
            return true;
        }

        public static string ReorderSelector(string selector, out int count)
        {
            if (!selector.Contains(SEPERATOR))
            {
                count = 1;
                return selector;
            } 
            var split = selector.Split(SEPERATOR_CHAR).Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x)).OrderBy(x => x).ToArray();
            count = split.Length;
            return string.Join(SEPERATOR, split);
        }

        public static void ParseSelectorVariantsFromName(string selector, List<string> result, int skipCount)
        {
            result.Clear();
            if (string.IsNullOrWhiteSpace(selector)) return;
            var split = selector.Split(SEPERATOR_CHAR).Skip(skipCount).Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).OrderBy(x => x).ToArray();
            result.AddRange(Enumerable.Range(0, 1 << split.Length).Select(index => string.Join(PresetStyleUtility.SEPERATOR, split.Where((v, i) => (index & (1 << i)) != 0))));
        }
        
        public static bool ReplacePreset(TrackInfo info, Component to)
        {
            if(!info.Preset.CanBeAppliedTo(to)) return false;
            var index = info.Style.Presets.FindIndex(p => info.Preset);
            if(index < 0) return false;

            var newPreset = new Preset(to);
            newPreset.name = to.GetType().FullName;
            newPreset.excludedProperties = info.Preset.excludedProperties;
            Undo.DestroyObjectImmediate(info.Style.Presets[index]);
            info.Style.Presets[index] = newPreset;
            AssetDatabase.AddObjectToAsset(newPreset, info.Sheet);
            Undo.RegisterCreatedObjectUndo(newPreset, "Editor");
            return true;
        }

        public struct TrackInfo
        {
            public PresetStyleSheet Sheet;
            public PresetStyleSheet.PresetStyle Style;
            public string Match;
            public Preset Preset;
            public int Specificity;
        }

        public class TrackInfoComparer : IComparer<TrackInfo>
        {
            public int Compare(TrackInfo x, TrackInfo y) => x.Specificity.CompareTo(y.Specificity);
        }

        public class PresetSheetContext
        {
            private TrackInfoComparer m_Comparer = new TrackInfoComparer();
            private PresetStyleSheetRoot m_RootComponent;
            private Dictionary<string, Dictionary<string, List<TrackInfo>>> m_SheetContext;

            public PresetSheetContext(PresetStyleSheetRoot sheetRoot)
            {
                m_RootComponent = sheetRoot;
                var sheet = sheetRoot.Sheet;
                var appliedHash = new HashSet<PresetStyleSheet>();
                var sheetListToApply = new HashSet<PresetStyleSheet>();

                void _AppendSheet(PresetStyleSheet sheetToAdd, HashSet<PresetStyleSheet> resultSheets)
                {
                    //skip already existing sheet
                    if(!resultSheets.Add(sheetToAdd)) return;

                    if (sheet.ParentSheets != null)
                    {
                        foreach (var parent in sheetToAdd.ParentSheets)
                        {
                            if (parent == null) continue;
                            _AppendSheet(parent, resultSheets);
                        }
                    }
                }

                _AppendSheet(sheet, sheetListToApply);

                m_SheetContext = new Dictionary<string, Dictionary<string, List<TrackInfo>>>();

                foreach(var currentSheet in sheetListToApply)
                {
                    foreach (var style in currentSheet.Styles)
                    {
                        if (string.IsNullOrWhiteSpace(style.Selector)) continue;

                        var orderedSelector = ReorderSelector(style.Selector, out var selctorCount);

                        if (!m_SheetContext.TryGetValue(orderedSelector, out var typeToPresetTuple))
                        {
                            typeToPresetTuple = new Dictionary<string, List<TrackInfo>>();
                            m_SheetContext.Add(orderedSelector, typeToPresetTuple);
                        }

                        foreach (var preset in style.Presets)
                        {
                            if (!typeToPresetTuple.TryGetValue(preset.name, out var tupleList))
                            {
                                tupleList = new List<TrackInfo>();
                                typeToPresetTuple.Add(preset.name, tupleList);
                            }
                            tupleList.Add(new TrackInfo()
                            {
                                Match = orderedSelector,
                                Style = style,
                                Sheet = currentSheet,
                                Preset = preset,
                                Specificity = style.Priority + 10 * selctorCount + 100 * currentSheet.SheetPriority
                            });
                        }
                    }
                }
            }

            public List<TrackInfo> Apply(Component component, bool dry = false)
            {
                var selectors = new List<string>();
                var result = new List<TrackInfo>();
                if (!component.TryGetComponent<PresetStyleClass>(out var subName)) return result;
                ParseSelectorVariantsFromName(subName.Name, selectors, 0);

                for (int i = 0; i < selectors.Count; i++)
                {
                    var selector = selectors[i];
                    if (string.IsNullOrWhiteSpace(selector)) continue;
                    if (!m_SheetContext.TryGetValue(selector, out var presetsDict)) continue;
                    if (!presetsDict.TryGetValue(component.GetType().FullName, out var info)) continue;
                    for (int j = 0; j < info.Count; j++)
                    {
                        if (!info[j].Preset.CanBeAppliedTo(component)) continue;
                        //record apply info
                        result.Add(info[j]);
                    }
                }

                result.Sort(m_Comparer);

                if(!dry && result.Count > 0)
                {
                    Undo.RecordObject(component, "Editor");
                    foreach(var info in result) m_RootComponent.ApplyPreset(component, info.Sheet, info.Preset);
                    EditorUtility.SetDirty(component);
                }

                //this is to analyze which is actually applied
                return result;
            }

            public void Apply(GameObject go, bool recursive)
            {
                _ApplyInternal(go, recursive, new List<string>());
            }

            void _ApplyInternal(GameObject go, bool recursive, List<string> selectorCache, List<TrackInfo> infoCache = null)
            {
                if (go.TryGetComponent<PresetStyleSheetRoot>(out var childRoot) && childRoot != m_RootComponent)
                {
                    if (recursive)
                    {
                        var childContext = new PresetSheetContext(childRoot);
                        childContext._ApplyInternal(go, true, selectorCache);
                    }
                    return;
                }

                if(infoCache == null) infoCache = new List<TrackInfo>();

                if (go.TryGetComponent<PresetStyleClass>(out var subName))
                {
                    ParseSelectorVariantsFromName(subName.Name, selectorCache, 0);

                    for (int i = 0; i < selectorCache.Count; i++)
                    {
                        var selector = selectorCache[i];
                        if (string.IsNullOrWhiteSpace(selector)) continue;
                        if (!m_SheetContext.TryGetValue(selector, out var presetsDict)) continue;
                        var components = go.GetComponents<Component>();
                        foreach (var component in components)
                        {
                            if (!presetsDict.TryGetValue(component.GetType().FullName, out var info)) continue;
                            infoCache.Clear();

                            for (int j = 0; j < info.Count; j++)
                            {
                                var preset = info[j];
                                if (!info[j].Preset.CanBeAppliedTo(component)) continue;
                                infoCache.Add(info[j]);

                            }

                            infoCache.Sort(m_Comparer);

                            if(infoCache.Count > 0)
                            {
                                Undo.RecordObject(component, "Editor");
                                foreach(var infoToApply in infoCache) m_RootComponent.ApplyPreset(component, infoToApply.Sheet, infoToApply.Preset);
                                EditorUtility.SetDirty(component);
                            }
                        }
                    }
                }

                if (recursive)
                {
                    for (int i = 0; i < go.transform.childCount; i++)
                    {
                        var child = go.transform.GetChild(i).gameObject;
                        _ApplyInternal(child, true, selectorCache);
                    }
                }
            }
        }
    }
}