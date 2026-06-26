using System;
using System.IO;
using System.Linq;
using MVP.System.GenerateUI;
using UnityEditor;
using UnityEngine;

namespace MVP.Editor
{
    /// <summary>
    /// 씬별 "시작 시 자동으로 열릴 UI" 매트릭스 편집기.
    /// 열 = Build Settings의 enabled 씬, 행 = UIId(None 제외). 셀 = 중립 ☐ / 시작시켜짐 ☑.
    /// </summary>
    public class UISceneMatrixWindow : EditorWindow
    {
        private const string DefaultAssetPath = "Assets/HwanLib/MVP/System/GenerateUI/UIScenePolicy.asset";
        private const float RowHeaderWidth = 180f;
        private const float CellWidth = 120f;

        private UIScenePolicySO _policy;
        private Vector2 _scroll;

        [MenuItem("Tools/MVP/Scene UI Matrix")]
        public static void Open() => GetWindow<UISceneMatrixWindow>("Scene UI Matrix");

        private void OnEnable() => FindPolicy();

        private void FindPolicy()
        {
            string guid = AssetDatabase.FindAssets("t:UIScenePolicySO").FirstOrDefault();
            _policy = guid != null
                ? AssetDatabase.LoadAssetAtPath<UIScenePolicySO>(AssetDatabase.GUIDToAssetPath(guid))
                : null;
        }

        private void OnGUI()
        {
            if (_policy == null)
            {
                DrawNoPolicy();
                return;
            }

            DrawToolbar();
            EditorGUILayout.Space(4);

            string[] scenes = EnabledSceneNames();
            if (scenes.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "Build Settings에 enabled된 씬이 없습니다. File > Build Settings에서 씬을 추가하세요.",
                    MessageType.Warning);
                return;
            }

            UIId[] ids = ((UIId[])Enum.GetValues(typeof(UIId))).Where(id => id != UIId.None).ToArray();
            if (ids.Length == 0)
            {
                EditorGUILayout.HelpBox("등록된 UIId가 없습니다. MVP 생성기로 UI를 만들면 행이 등장합니다.", MessageType.Info);
                return;
            }

            DrawMatrix(scenes, ids);
        }

        private void DrawNoPolicy()
        {
            EditorGUILayout.HelpBox("UIScenePolicy 에셋을 찾지 못했습니다.", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("새로 생성", GUILayout.Height(28)))
                    CreatePolicy();
                if (GUILayout.Button("다시 찾기", GUILayout.Height(28)))
                    FindPolicy();
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUILayout.ObjectField(_policy, typeof(UIScenePolicySO), false);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("다시 찾기", EditorStyles.toolbarButton))
                    FindPolicy();
            }
        }

        private void DrawMatrix(string[] scenes, UIId[] ids)
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            // 헤더 행: 빈 코너 + 씬 이름들.
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("UIId \\ Scene", EditorStyles.boldLabel, GUILayout.Width(RowHeaderWidth));
                foreach (string scene in scenes)
                    GUILayout.Label(scene, EditorStyles.boldLabel, GUILayout.Width(CellWidth));
            }

            foreach (UIId id in ids)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(id.ToString(), GUILayout.Width(RowHeaderWidth));
                    foreach (string scene in scenes)
                    {
                        bool cur = _policy.EditorGet(scene, id);
                        bool next = EditorGUILayout.Toggle(cur, GUILayout.Width(CellWidth));
                        if (next != cur)
                        {
                            Undo.RecordObject(_policy, "Edit Scene UI Matrix");
                            _policy.EditorSet(scene, id, next);
                            EditorUtility.SetDirty(_policy);
                        }
                    }
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(6);
            if (GUILayout.Button("저장 (AssetDatabase.SaveAssets)"))
                AssetDatabase.SaveAssets();
        }

        private static string[] EnabledSceneNames() =>
            EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => Path.GetFileNameWithoutExtension(s.path))
                .ToArray();

        private void CreatePolicy()
        {
            var asset = CreateInstance<UIScenePolicySO>();
            string dir = Path.GetDirectoryName(DefaultAssetPath);
            if (!AssetDatabase.IsValidFolder(dir))
                Directory.CreateDirectory(dir);
            AssetDatabase.CreateAsset(asset, DefaultAssetPath);
            AssetDatabase.SaveAssets();
            _policy = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}
