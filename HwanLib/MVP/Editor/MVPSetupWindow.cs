using System;
using System.Collections.Generic;
using System.Linq;
using MVP.Interaction;
using MVP.System;
using MVP.System.BaseMVP;
using MVP.System.BaseMVP.Form;
using UnityEditor;
using UnityEngine;

namespace MVP.Editor
{
    public class MVPSetupWindow : EditorWindow
    {
        private GameObject _root;
        private GameObject _selected;
        private readonly List<GameObject> _selection = new();   // 현재 선택 집합(단일 클릭이면 1개)
        private readonly List<GameObject> _visibleRows = new();  // 직전 Repaint에 그려진 행 순서(Shift 범위용)
        private readonly Dictionary<int, bool> _foldouts = new();
        private string _search = "";
        private int _rowIndex;
        private Vector2 _treeScroll;
        private Vector2 _detailScroll;
        private float _splitWidth = -1f;
        private bool _draggingSplitter;
        private float _dragStartMouseX;
        private float _splitWidthAtDragStart;
        private InteractionFeedbackProfile _profile;
        private Type[] _allFormTypes;
        private GUIStyle _richLabelStyle;

        // 기능 1 — 스크립트 생성
        private bool _showGenerator = true;
        private string _genCategory = "";
        private string _genName = "";
        private string _genExclude;

        // 기능 3 — Form 부착 검색
        private string _formAttachSearch = "";
        private Vector2 _attachScroll;

        // 기능 4 — 루트 GO 영속화
        private const string RootRefPrefsKey = "MVP.Setup.RootRef";

        [MenuItem("Tools/MVP/Setup")]
        public static void Open() => GetWindow<MVPSetupWindow>("MVP Setup");

        private void OnEnable()
        {
            _richLabelStyle = null;
            _allFormTypes = TypeCache.GetTypesDerivedFrom<BaseForm>()
                .Where(t => !t.IsAbstract)
                .OrderBy(t => t.Name)
                .ToArray();

            _genExclude = MVPScriptGenerator.LoadExcludeKeywordsRaw();
            RestoreRoot();

            if (_profile == null)
            {
                string guid = AssetDatabase.FindAssets("t:InteractionFeedbackProfile").FirstOrDefault();
                if (guid != null)
                    _profile = AssetDatabase.LoadAssetAtPath<InteractionFeedbackProfile>(
                        AssetDatabase.GUIDToAssetPath(guid));
            }
        }

        private void OnGUI()
        {
            if (_splitWidth < 0) _splitWidth = Mathf.Max(100f, position.width * 0.35f);

            DrawGeneratorSection();

            EditorGUI.BeginChangeCheck();
            _root = (GameObject)EditorGUILayout.ObjectField("루트 GO", _root, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck())
            {
                _foldouts.Clear();
                _selected = null;
                _selection.Clear();
                _search = "";
                SaveRootRef();
            }

            if (_root == null)
            {
                EditorGUILayout.HelpBox("루트 GameObject를 지정하세요.", MessageType.Info);
                return;
            }

            DrawBulkFeedbackSection();
            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(_splitWidth)))
                {
                    _search = GUILayout.TextField(_search, EditorStyles.toolbarSearchField);

                    _treeScroll = EditorGUILayout.BeginScrollView(_treeScroll);
                    _rowIndex = 0;
                    if (Event.current.type == EventType.Repaint) _visibleRows.Clear();
                    if (string.IsNullOrEmpty(_search))
                        DrawNode(_root.transform, 0);
                    else
                        DrawSearchResults();
                    EditorGUILayout.EndScrollView();
                }

                DrawSplitter();

                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                {
                    _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
                    DrawDetail();
                    EditorGUILayout.EndScrollView();
                }
            }
        }

        // ── 기능 1: M/P/V 스크립트 생성 ───────────────────────────────────────────

        private void DrawGeneratorSection()
        {
            _showGenerator = EditorGUILayout.Foldout(_showGenerator, "MVP 생성", true);
            if (!_showGenerator) return;

            using (new EditorGUI.IndentLevelScope())
            {
                _genCategory = EditorGUILayout.TextField("분류", _genCategory);
                _genName = EditorGUILayout.TextField("이름", _genName);

                EditorGUI.BeginChangeCheck();
                _genExclude = EditorGUILayout.TextField(
                    new GUIContent("네임스페이스 제외(,)", "쉼표로 구분. 경로 세그먼트 중 이 키워드는 네임스페이스에서 제외"),
                    _genExclude);
                if (EditorGUI.EndChangeCheck())
                    MVPScriptGenerator.SaveExcludeKeywordsRaw(_genExclude);

                bool ready = !string.IsNullOrWhiteSpace(_genCategory)
                             && !string.IsNullOrWhiteSpace(_genName);
                using (new EditorGUI.DisabledScope(!ready))
                {
                    if (GUILayout.Button("생성"))
                        RunGenerate();
                }
            }
            EditorGUILayout.Space(4);
        }

        private void RunGenerate()
        {
            MVPScriptGenerator.Result result = MVPScriptGenerator.Generate(
                _genCategory, _genName, MVPScriptGenerator.ParseKeywords(_genExclude));

            if (result.Success)
            {
                Debug.Log($"[MVPSetup] {result.Message}");
                _genName = "";
                GUIUtility.keyboardControl = 0;
            }
            else
            {
                EditorUtility.DisplayDialog("MVP 생성 실패", result.Message, "확인");
            }
        }

        // ── 기능 4: 루트 GO 영속화 ────────────────────────────────────────────────

        // GlobalObjectId로 저장 → 프리팹 에셋·씬 오브젝트·프리팹 스테이지 인스턴스 모두 복원.
        // (창 닫기/리컴파일은 모두 복원. 씬·스테이지 오브젝트는 해당 씬/스테이지가 열려 있어야 복원됨.)
        private void RestoreRoot()
        {
            if (_root != null) return;
            string raw = EditorPrefs.GetString(RootRefPrefsKey, "");
            if (string.IsNullOrEmpty(raw)) return;
            if (!GlobalObjectId.TryParse(raw, out GlobalObjectId id)) return;

            _root = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as GameObject;
        }

        private void SaveRootRef()
        {
            if (_root == null)
            {
                EditorPrefs.DeleteKey(RootRefPrefsKey);
                return;
            }

            GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(_root);
            if (id.identifierType == 0) // 0 = Null/유효하지 않음
                EditorPrefs.DeleteKey(RootRefPrefsKey);
            else
                EditorPrefs.SetString(RootRefPrefsKey, id.ToString());
        }

        // ── 일괄 Feedback 부착 ────────────────────────────────────────────────────

        private void DrawBulkFeedbackSection()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _profile = (InteractionFeedbackProfile)EditorGUILayout.ObjectField(
                    _profile, typeof(InteractionFeedbackProfile), false, GUILayout.ExpandWidth(true));

                bool profileReady = _profile != null;
                using (new EditorGUI.DisabledScope(!profileReady))
                {
                    if (GUILayout.Button("Feedback 일괄 부착", GUILayout.Width(120)))
                        BulkAttachFeedback();
                }
            }
        }

        private void BulkAttachFeedback()
        {
            int count = 0;
            foreach (Transform t in _root.GetComponentsInChildren<Transform>(true))
            {
                GameObject go = t.gameObject;
                // IInteractable Form이 있는 GO만 대상
                BaseForm form = go.GetComponent<BaseForm>();
                if (form is not IInteractable) continue;

                InteractionFeedback feedback = go.GetComponent<InteractionFeedback>();
                if (feedback == null)
                {
                    feedback = Undo.AddComponent<InteractionFeedback>(go);
                    count++;
                }

                // profile 미설정이면 덮어쓰기
                SerializedObject so = new(feedback);
                SerializedProperty prop = so.FindProperty("profile");
                if (prop.objectReferenceValue == null)
                {
                    prop.objectReferenceValue = _profile;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(go);
                }
            }
            Debug.Log($"[MVPSetup] InteractionFeedback 일괄 부착: {count}개 추가됨 (profile={_profile.name})");
        }

        private void DrawSplitter()
        {
            // 폭을 Width(5)로 못 박는다. ExpandHeight만 주면 가로로도 늘어나(stretchWidth) HorizontalScope의
            // 남는 폭을 스플리터가 빨아들여 두꺼운 막대가 된다(오른쪽 패널 몫을 가져감).
            Rect splitterRect = GUILayoutUtility.GetRect(
                5f, 5f, GUILayout.Width(5f), GUILayout.ExpandHeight(true));
            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);

            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(splitterRect, new Color(0.13f, 0.13f, 0.13f, 1f));

            switch (Event.current.type)
            {
                case EventType.MouseDown when splitterRect.Contains(Event.current.mousePosition):
                    _draggingSplitter = true;
                    _dragStartMouseX = Event.current.mousePosition.x;
                    _splitWidthAtDragStart = _splitWidth;
                    Event.current.Use();
                    break;
                case EventType.MouseDrag when _draggingSplitter:
                    _splitWidth = Mathf.Clamp(
                        _splitWidthAtDragStart + (Event.current.mousePosition.x - _dragStartMouseX),
                        80f, position.width - 120f);
                    Repaint();
                    Event.current.Use();
                    break;
                case EventType.MouseUp when _draggingSplitter:
                    _draggingSplitter = false;
                    Event.current.Use();
                    break;
            }
        }

        // ── Tree ─────────────────────────────────────────────────────────────────

        private const float IndentWidth = 14f;

        private void DrawNode(Transform t, int depth)
        {
            GameObject go = t.gameObject;
            bool hasChildren = t.childCount > 0;
            int id = go.GetInstanceID();
            if (!_foldouts.ContainsKey(id)) _foldouts[id] = true;

            Rect row = GUILayoutUtility.GetRect(0f, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
            DrawRowBackground(row, go);

            Rect arrowRect = new(row.x + depth * IndentWidth, row.y, IndentWidth, row.height);
            Rect labelRect = new(arrowRect.xMax, row.y, row.xMax - arrowRect.xMax, row.height);

            if (hasChildren)
                _foldouts[id] = EditorGUI.Foldout(arrowRect, _foldouts[id], GUIContent.none);

            HandleRowSelect(labelRect, go);
            EditorGUI.LabelField(labelRect, new GUIContent(BuildNodeLabelRich(go), BuildNodeLabel(go)), RichStyle);

            if (hasChildren && _foldouts[id])
                foreach (Transform child in t)
                    DrawNode(child, depth + 1);
        }

        private void DrawSearchResults()
        {
            bool any = false;
            foreach (Transform t in _root.GetComponentsInChildren<Transform>(true))
            {
                GameObject go = t.gameObject;
                BaseForm form = go.GetComponent<BaseForm>();
                if (form == null) continue;

                bool matchName = go.name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0;
                bool matchType = form.GetType().Name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0;
                if (!matchName && !matchType) continue;

                any = true;
                Rect row = GUILayoutUtility.GetRect(0f, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
                DrawRowBackground(row, go);

                Rect labelRect = new(row.x + 4f, row.y, row.width - 4f, row.height);
                HandleRowSelect(labelRect, go);
                EditorGUI.LabelField(labelRect, new GUIContent(BuildNodeLabelRich(go), BuildNodeLabel(go)), RichStyle);
            }

            if (!any)
                EditorGUILayout.HelpBox("일치하는 Form 오브젝트 없음", MessageType.None);
        }

        private void DrawRowBackground(Rect row, GameObject go)
        {
            if (Event.current.type == EventType.Repaint)
            {
                _visibleRows.Add(go);
                if (_selection.Contains(go))
                    EditorGUI.DrawRect(row, new Color(0.24f, 0.49f, 0.91f, 0.3f));
                else if (_rowIndex % 2 == 1)
                    EditorGUI.DrawRect(row, new Color(0f, 0f, 0f, 0.07f));
            }
            _rowIndex++;
        }

        private void HandleRowSelect(Rect rect, GameObject go)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                if (e.control || e.command)            // Ctrl(맥은 Cmd): 토글
                {
                    if (!_selection.Remove(go)) _selection.Add(go);
                    _selected = go;
                }
                else if (e.shift && _selected != null)  // Shift: 앵커~현재 범위
                {
                    SelectRange(_selected, go);
                }
                else                                    // 그냥 클릭: 단일
                {
                    _selection.Clear();
                    _selection.Add(go);
                    _selected = go;
                }
                e.Use();
                Repaint();
            }
        }

        // _visibleRows(화면 순서)에서 앵커~타깃 구간을 선택한다. 둘 중 하나라도 안 보이면 단일 폴백.
        private void SelectRange(GameObject anchor, GameObject target)
        {
            int a = _visibleRows.IndexOf(anchor);
            int b = _visibleRows.IndexOf(target);
            _selection.Clear();
            if (a < 0 || b < 0)
            {
                _selection.Add(target);
                return;
            }
            if (a > b) (a, b) = (b, a);
            for (int i = a; i <= b; ++i) _selection.Add(_visibleRows[i]);
        }

        private static string BuildNodeLabel(GameObject go)
        {
            string label = go.name;
            BaseForm form = go.GetComponent<BaseForm>();
            if (form != null)
            {
                label += $"  {form.GetType().Name}";
                if (form is IInteractable)
                {
                    bool bound = !string.IsNullOrEmpty(form.InteractMethod);
                    label += bound ? $" I:{form.InteractMethod}" : " I:!";
                }
                if (form is IUpdatable)
                {
                    bool bound = !string.IsNullOrEmpty(form.UpdateMethod);
                    label += bound ? $" U:{form.UpdateMethod}" : " U:!";
                }
            }
            if (go.GetComponent<InteractionFeedback>() != null) label += "  [F]";
            return label;
        }

        private static string BuildNodeLabelRich(GameObject go)
        {
            string label = $"<b>{go.name}</b>";
            BaseForm form = go.GetComponent<BaseForm>();
            if (form != null)
            {
                label += $"  <color=#8DB4D9>{form.GetType().Name}</color>";
                if (form is IInteractable)
                {
                    bool bound = !string.IsNullOrEmpty(form.InteractMethod);
                    label += bound
                        ? $" <color=#7EC8A0>I:{form.InteractMethod}</color>"
                        : " <color=#FF6B61>I:!</color>";
                }
                if (form is IUpdatable)
                {
                    bool bound = !string.IsNullOrEmpty(form.UpdateMethod);
                    label += bound
                        ? $" <color=#E8C07A>U:{form.UpdateMethod}</color>"
                        : " <color=#FF6B61>U:!</color>";
                }
            }
            if (go.GetComponent<InteractionFeedback>() != null) label += "  <color=#D4C567>[F]</color>";
            return label;
        }

        private GUIStyle RichStyle
        {
            get
            {
                if (_richLabelStyle != null) return _richLabelStyle;
                _richLabelStyle = new GUIStyle(EditorStyles.label) { richText = true };
                return _richLabelStyle;
            }
        }

        // ── Detail ───────────────────────────────────────────────────────────────

        private void DrawDetail()
        {
            _selection.RemoveAll(go => go == null);

            if (_selection.Count == 0)
            {
                EditorGUILayout.HelpBox("왼쪽 트리에서 GO를 선택하세요.", MessageType.None);
                return;
            }

            if (_selection.Count == 1)
            {
                _selected = _selection[0];
                DrawSingleDetail();
                return;
            }

            DrawBulkDetail();
        }

        private void DrawSingleDetail()
        {
            EditorGUILayout.LabelField(_selected.name, EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            BaseForm form = GetSingleForm(_selected);

            DrawFormSection(form);

            if (form is IInteractable)
            {
                EditorGUILayout.Space(4);
                DrawFeedbackSection();
            }

            if (form is IInteractable || form is IUpdatable)
            {
                EditorGUILayout.Space(4);
                DrawBindingSection(form);
            }
        }

        // ── 일괄 모드 (2개+ 선택) ──────────────────────────────────────────────────

        private void DrawBulkDetail()
        {
            EditorGUILayout.LabelField($"{_selection.Count}개 선택됨", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            List<BaseForm> forms = new();
            bool anyMissing = false;
            foreach (GameObject go in _selection)
            {
                BaseForm f = GetSingleForm(go);
                forms.Add(f);
                if (f == null) anyMissing = true;
            }

            DrawBulkFormSection(forms, anyMissing);

            bool sameType = forms.Count > 0 && forms[0] != null
                            && forms.All(f => f != null && f.GetType() == forms[0].GetType());
            if (!sameType)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(
                    "Form 타입이 섞여 있어 필드·바인딩 일괄 편집은 불가합니다. Form 부착/제거만 가능합니다.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.Space(4);
            DrawBulkFields(forms);

            BaseForm sample = forms[0];
            if (sample is IInteractable || sample is IUpdatable)
            {
                EditorGUILayout.Space(4);
                DrawBulkBinding(forms);
            }
        }

        private void DrawBulkFormSection(List<BaseForm> forms, bool anyMissing)
        {
            EditorGUILayout.LabelField("── Form ────────────────────", EditorStyles.boldLabel);

            // Form 없는 GO가 하나라도 있으면 일괄 부착 검색 제공.
            if (anyMissing)
            {
                _formAttachSearch = EditorGUILayout.TextField("Form 검색", _formAttachSearch);

                Type[] matches = string.IsNullOrEmpty(_formAttachSearch)
                    ? _allFormTypes
                    : _allFormTypes
                        .Where(t => t.Name.IndexOf(_formAttachSearch, StringComparison.OrdinalIgnoreCase) >= 0)
                        .ToArray();

                if (matches.Length == 0)
                {
                    EditorGUILayout.HelpBox("일치하는 Form 타입 없음", MessageType.None);
                }
                else
                {
                    _attachScroll = EditorGUILayout.BeginScrollView(_attachScroll, GUILayout.MaxHeight(160));
                    foreach (Type t in matches)
                    {
                        if (GUILayout.Button($"{t.Name}  (Form 없는 곳에 일괄 부착)", EditorStyles.miniButton))
                        {
                            BulkAddForm(t);
                            _formAttachSearch = "";
                            GUIUtility.keyboardControl = 0;
                            GUIUtility.ExitGUI();
                        }
                    }
                    EditorGUILayout.EndScrollView();
                }
            }

            // Form 있는 GO가 하나라도 있으면 일괄 제거 버튼.
            if (forms.Any(f => f != null))
            {
                if (GUILayout.Button("선택 Form 일괄 제거"))
                {
                    BulkRemoveForms(forms);
                    GUIUtility.ExitGUI();
                }
            }
        }

        private void BulkAddForm(Type formType)
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Bulk Add Form");
            int group = Undo.GetCurrentGroup();
            foreach (GameObject go in _selection)
            {
                if (GetSingleForm(go) != null) continue;   // 이미 있으면 건너뜀
                Undo.AddComponent(go, formType);
                EditorUtility.SetDirty(go);
            }
            Undo.CollapseUndoOperations(group);
        }

        private void BulkRemoveForms(List<BaseForm> forms)
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Bulk Remove Form");
            int group = Undo.GetCurrentGroup();
            foreach (BaseForm f in forms)
            {
                if (f == null) continue;
                GameObject go = f.gameObject;
                Undo.DestroyObjectImmediate(f);
                EditorUtility.SetDirty(go);
            }
            Undo.CollapseUndoOperations(group);
        }

        private void DrawBulkFields(List<BaseForm> forms)
        {
            EditorGUILayout.LabelField($"{forms[0].GetType().Name} (일괄)", EditorStyles.boldLabel);

            SerializedObject so = new(forms.Cast<UnityEngine.Object>().ToArray());
            EditorGUI.BeginChangeCheck();

            SerializedProperty prop = so.GetIterator();
            bool enterChildren = true;
            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (prop.name is "m_Script" or "interactMethod" or "updateMethod") continue;
                EditorGUILayout.PropertyField(prop, true);   // 값이 다른 필드는 Unity가 자동 "—" 표시
            }

            if (EditorGUI.EndChangeCheck())
            {
                so.ApplyModifiedProperties();
                foreach (BaseForm f in forms) EditorUtility.SetDirty(f);
            }
        }

        private void DrawBulkBinding(List<BaseForm> forms)
        {
            EditorGUILayout.LabelField("── 바인딩 (일괄) ───────────", EditorStyles.boldLabel);

            Type modelType = ResolveModelType(forms[0].gameObject);
            if (modelType == null)
            {
                EditorGUILayout.HelpBox(
                    "부모 계층에 BasePresenter<TModel,TView> 없음 — Model 메서드를 찾을 수 없습니다.",
                    MessageType.Warning);
                return;
            }
            if (!forms.All(f => ResolveModelType(f.gameObject) == modelType))
            {
                EditorGUILayout.HelpBox("선택 Form들의 Model이 달라 바인딩 일괄이 불가합니다.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField($"Model: {modelType.Name}", EditorStyles.miniLabel);

            BaseForm sample = forms[0];
            if (sample is IInteractable)
            {
                string current = CommonValue(forms, f => f.InteractMethod);
                DrawMethodPopup(current == null ? "Interact (혼합)" : "Interact",
                    current, MVPBinding.InteractMethodNames(modelType),
                    v =>
                    {
                        Undo.RecordObjects(forms.ToArray(), "Set Interact");
                        foreach (BaseForm f in forms) { f.EditorSetInteractMethod(v); EditorUtility.SetDirty(f); }
                    });
            }

            if (sample is IUpdatable)
            {
                string current = CommonValue(forms, f => f.UpdateMethod);
                DrawMethodPopup(current == null ? "Update (혼합)" : "Update",
                    current, MVPBinding.UpdateMethodNames(modelType),
                    v =>
                    {
                        Undo.RecordObjects(forms.ToArray(), "Set Update");
                        foreach (BaseForm f in forms) { f.EditorSetUpdateMethod(v); EditorUtility.SetDirty(f); }
                    });
            }
        }

        // 모든 form의 값이 같으면 그 값, 다르면 null(혼합).
        private static string CommonValue(List<BaseForm> forms, Func<BaseForm, string> get)
        {
            string first = get(forms[0]) ?? "";
            foreach (BaseForm f in forms)
                if ((get(f) ?? "") != first) return null;
            return first;
        }

        private void DrawFormSection(BaseForm form)
        {
            EditorGUILayout.LabelField("── Form ────────────────────", EditorStyles.boldLabel);

            int formCount = _selected.GetComponents<BaseForm>().Length;
            if (formCount > 1)
                EditorGUILayout.HelpBox($"Form이 {formCount}개 감지됨. 첫 번째만 표시합니다.", MessageType.Warning);

            if (form == null)
                DrawFormAttachSearch();
            else
                DrawFormFields(form);
        }

        // 기능 3: Form 부착 검색
        private void DrawFormAttachSearch()
        {
            _formAttachSearch = EditorGUILayout.TextField("Form 검색", _formAttachSearch);

            Type[] matches = string.IsNullOrEmpty(_formAttachSearch)
                ? _allFormTypes
                : _allFormTypes
                    .Where(t => t.Name.IndexOf(_formAttachSearch, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToArray();

            if (matches.Length == 0)
            {
                EditorGUILayout.HelpBox("일치하는 Form 타입 없음", MessageType.None);
                return;
            }

            _attachScroll = EditorGUILayout.BeginScrollView(_attachScroll, GUILayout.MaxHeight(160));
            foreach (Type t in matches)
            {
                if (GUILayout.Button(t.Name, EditorStyles.miniButton))
                {
                    Undo.AddComponent(_selected, t);
                    EditorUtility.SetDirty(_selected);
                    _formAttachSearch = "";
                    GUIUtility.keyboardControl = 0;
                }
            }
            EditorGUILayout.EndScrollView();
        }

        // 기능 2: Form 직렬화 필드 편집 (+ 제거)
        private void DrawFormFields(BaseForm form)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(form.GetType().Name);
                if (GUILayout.Button("제거", GUILayout.Width(48)))
                {
                    Undo.DestroyObjectImmediate(form);
                    EditorUtility.SetDirty(_selected);
                    return;
                }
            }

            SerializedObject so = new(form);
            EditorGUI.BeginChangeCheck();

            SerializedProperty prop = so.GetIterator();
            bool enterChildren = true;
            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (prop.name is "m_Script" or "interactMethod" or "updateMethod") continue;
                EditorGUILayout.PropertyField(prop, true);
            }

            if (EditorGUI.EndChangeCheck())
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(form);
            }
        }

        private void DrawFeedbackSection()
        {
            EditorGUILayout.LabelField("── InteractionFeedback ─────", EditorStyles.boldLabel);
            InteractionFeedback feedback = _selected.GetComponent<InteractionFeedback>();

            if (feedback == null)
            {
                _profile = (InteractionFeedbackProfile)EditorGUILayout.ObjectField(
                    "Profile", _profile, typeof(InteractionFeedbackProfile), false);
                if (GUILayout.Button("부착"))
                {
                    feedback = Undo.AddComponent<InteractionFeedback>(_selected);
                    if (_profile != null)
                    {
                        SerializedObject so = new(feedback);
                        so.FindProperty("profile").objectReferenceValue = _profile;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                    EditorUtility.SetDirty(_selected);
                }
            }
            else
            {
                SerializedObject so = new(feedback);
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(so.FindProperty("profile"), new GUIContent("Profile"));
                if (EditorGUI.EndChangeCheck()) so.ApplyModifiedProperties();

                if (GUILayout.Button("제거"))
                {
                    Undo.DestroyObjectImmediate(feedback);
                    EditorUtility.SetDirty(_selected);
                }
            }
        }

        private void DrawBindingSection(BaseForm form)
        {
            EditorGUILayout.LabelField("── 바인딩 ──────────────────", EditorStyles.boldLabel);
            Type modelType = ResolveModelType(_selected);

            if (modelType == null)
            {
                EditorGUILayout.HelpBox(
                    "부모 계층에 BasePresenter<TModel,TView> 없음 — Model 메서드를 찾을 수 없습니다.",
                    MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField($"Model: {modelType.Name}", EditorStyles.miniLabel);

            if (form is IInteractable)
                DrawMethodPopup("Interact", form.InteractMethod,
                    MVPBinding.InteractMethodNames(modelType),
                    v =>
                    {
                        Undo.RecordObject(form, "Set Interact");
                        form.EditorSetInteractMethod(v);
                        EditorUtility.SetDirty(form);
                    });

            if (form is IUpdatable)
                DrawMethodPopup("Update", form.UpdateMethod,
                    MVPBinding.UpdateMethodNames(modelType),
                    v =>
                    {
                        Undo.RecordObject(form, "Set Update");
                        form.EditorSetUpdateMethod(v);
                        EditorUtility.SetDirty(form);
                    });
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static BaseForm GetSingleForm(GameObject go)
        {
            BaseForm[] forms = go.GetComponents<BaseForm>();
            return forms.Length > 0 ? forms[0] : null;
        }

        private static void DrawMethodPopup(string label, string current, IEnumerable<string> names, Action<string> apply)
        {
            List<string> choices = new() { "None" };
            choices.AddRange(names);
            int index = Mathf.Max(0, choices.IndexOf(string.IsNullOrEmpty(current) ? "None" : current));
            int newIndex = EditorGUILayout.Popup(label, index, choices.ToArray());
            string selected = newIndex <= 0 ? "" : choices[newIndex];
            if (selected != (current ?? "")) apply(selected);
        }

        private static Type ResolveModelType(GameObject go)
        {
            BasePresenter presenter = go.GetComponentInParent<BasePresenter>(true);
            if (presenter == null) return null;
            return MVPTypeUtil.GetModelView(presenter.GetType()).model;
        }

    }
}
