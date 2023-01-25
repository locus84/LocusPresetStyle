using UnityEngine;
using UnityEditor;
using UnityEditor.Presets;
using System.Collections.Generic;
using System.Linq;
using System;

namespace PresetStyle
{
    public static class PresetStyleUtility
    {
        static string s_StyleClipboard = string.Empty;

        [InitializeOnLoadMethod]
        static void InitializeCallback()
        {
            PresetStyleSheetRoot.ApplyRecursive = go => ApplyRecursive(new [] { go });
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
            if (!TryGetParentStyleSheetRoot(component.gameObject, out var styleRoot)) return;
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
            if (!TryGetParentStyleSheetRoot(component.gameObject, out var styleRoot)) return;
            var context = new PresetSheetContext(styleRoot);
            var trackInfos = context.Apply(component, true);
            var result = new Dictionary<Component, List<TrackInfo>>();
            if(trackInfos.Count > 0) result.Add(component, trackInfos);

            var trackEditor = EditorWindow.GetWindow<PresetTrackWindow>("Preset Style Analyzer");
            trackEditor.SetTrackInfos(result);
            trackEditor.Show();
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
            foreach (var go in gameObjects)
            {
                if (!TryGetParentStyleSheetRoot(go, out var styleRoot)) continue;
                var context = new PresetSheetContext(styleRoot);
                context.Apply(go, true);
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
                if (!TryGetParentStyleSheetRoot(go, out var styleRoot)) continue;
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
            Debug.LogError($"StyleSheetRoot component is not found in GameObject({gameObject.name})'s parents!");
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

        public static string ReorderSelector(string selector)
        {
            if (!selector.Contains(SEPERATOR)) return selector;
            var split = selector.Split(SEPERATOR_CHAR).Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x)).OrderBy(x => x);
            return string.Join(SEPERATOR, split);
        }

        public static void ParseSelectorVariantsFromName(string selector, List<string> result, int skipCount)
        {
            result.Clear();
            if (string.IsNullOrWhiteSpace(selector)) return;
            var split = selector.Split(SEPERATOR_CHAR).Skip(skipCount).Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).OrderBy(x => x).ToArray();
            result.AddRange(Enumerable.Range(0, 1 << split.Length).Select(index => string.Join(PresetStyleUtility.SEPERATOR, split.Where((v, i) => (index & (1 << i)) != 0))));
        }

        public struct TrackInfo
        {
            public PresetStyleSheet Sheet;
            public string Match;
            public Preset Preset;
        }

        public class PresetSheetContext
        {

            private PresetStyleSheetRoot m_RootComponent;
            private Dictionary<string, Dictionary<string, List<(PresetStyleSheet sheet, Preset preset)>>> m_SheetContext;

            public PresetSheetContext(PresetStyleSheetRoot sheetRoot)
            {
                m_RootComponent = sheetRoot;
                var sheet = sheetRoot.Sheet;
                var appliedHash = new HashSet<PresetStyleSheet>();
                var sheetListToApply = new List<PresetStyleSheet>();

                void _AppendSheet(PresetStyleSheet sheet, List<PresetStyleSheet> resultSheets)
                {
                    if (sheet.ParentSheets != null)
                    {
                        foreach (var parent in sheet.ParentSheets)
                        {
                            if (parent == null) continue;
                            _AppendSheet(parent, resultSheets);
                        }
                    }
                    resultSheets.Add(sheet);
                }

                _AppendSheet(sheet, sheetListToApply);

                m_SheetContext = new Dictionary<string, Dictionary<string, List<(PresetStyleSheet sheet, Preset preset)>>>();

                for (int i = 0; i < sheetListToApply.Count; i++)
                {
                    var currentSheet = sheetListToApply[i];
                    foreach (var style in currentSheet.Styles)
                    {
                        if (string.IsNullOrWhiteSpace(style.Selector)) continue;

                        var orderedSelector = ReorderSelector(style.Selector);

                        if (!m_SheetContext.TryGetValue(orderedSelector, out var typeToPresetTuple))
                        {
                            typeToPresetTuple = new Dictionary<string, List<(PresetStyleSheet sheet, Preset preset)>>();
                            m_SheetContext.Add(orderedSelector, typeToPresetTuple);
                        }

                        foreach (var preset in style.Presets)
                        {
                            if (!typeToPresetTuple.TryGetValue(preset.name, out var tupleList))
                            {
                                tupleList = new List<(PresetStyleSheet sheet, Preset preset)>();
                                typeToPresetTuple.Add(preset.name, tupleList);
                            }
                            tupleList.Add((currentSheet, preset));
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
                    var isDirty = false;
                    for (int j = 0; j < info.Count; j++)
                    {
                        if (!info[j].preset.CanBeAppliedTo(component)) continue;
                        if (!dry && !isDirty)
                        {
                            Undo.RecordObject(component, "Editor");
                            isDirty = true;
                        }

                        //if dry, skip actual apply
                        if (!dry) m_RootComponent.ApplyPreset(component, info[j].sheet, info[j].preset);

                        //record apply info
                        result.Add(new TrackInfo()
                        {
                            Match = selector,
                            Sheet = info[j].sheet,
                            Preset = info[j].preset
                        });
                    }
                    if (isDirty) EditorUtility.SetDirty(component);
                }

                //this is to analyze which is actually applied
                return result;
            }

            public void Apply(GameObject go, bool recursive)
            {
                _ApplyInternal(go, recursive, new List<string>());
            }

            void _ApplyInternal(GameObject go, bool recursive, List<string> selectorCache)
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
                            var isDirty = false;
                            for (int j = 0; j < info.Count; j++)
                            {
                                var preset = info[j];
                                if (!info[j].preset.CanBeAppliedTo(component)) continue;
                                if (!isDirty)
                                {
                                    Undo.RecordObject(component, "Editor");
                                    isDirty = true;
                                }
                                m_RootComponent.ApplyPreset(component, info[j].sheet, info[j].preset);
                            }
                            if (isDirty) EditorUtility.SetDirty(component);
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