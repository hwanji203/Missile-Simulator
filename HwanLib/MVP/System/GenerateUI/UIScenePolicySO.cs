using System;
using System.Collections.Generic;
using UnityEngine;

namespace MVP.System.GenerateUI
{
    // 씬별 "시작 시 자동으로 열릴 UI" 정책. Physics 충돌 매트릭스처럼
    // 열=씬, 행=UIId 그리드로 편집한다(Tools/MVP/Scene UI Matrix).
    // 없는 씬/Id는 전부 중립(자동으로 안 열림). 모든 등록 UI는 모든 씬에 닫힌 채 Instantiate된다.
    [CreateAssetMenu(fileName = "UIScenePolicy", menuName = "UI/MVP/Scene UI Policy")]
    public class UIScenePolicySO : ScriptableObject
    {
        // Unity 직렬화 한계로 Dict/HashSet 대신 List<SceneEntry>.
        [Serializable]
        public class SceneEntry
        {
            public string sceneName;
            public List<UIId> openOnStart = new();
        }

        [SerializeField] private List<SceneEntry> entries = new();

        // 런타임 조회. 없는 씬은 빈 목록(전부 중립).
        public IReadOnlyList<UIId> GetOpenOnStart(string sceneName)
        {
            SceneEntry entry = FindEntry(sceneName);
            return entry != null ? entry.openOnStart : Array.Empty<UIId>();
        }

        private SceneEntry FindEntry(string sceneName)
        {
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].sceneName == sceneName)
                    return entries[i];
            return null;
        }

#if UNITY_EDITOR
        // 에디터 전용 쓰기 API (매트릭스 윈도우가 사용).
        public void EditorSet(string sceneName, UIId id, bool openOnStart)
        {
            if (string.IsNullOrEmpty(sceneName) || id == UIId.None) return;

            SceneEntry entry = FindEntry(sceneName);
            if (entry == null)
            {
                if (!openOnStart) return; // 켤 것도 없으면 빈 엔트리 안 만든다.
                entry = new SceneEntry { sceneName = sceneName };
                entries.Add(entry);
            }

            bool has = entry.openOnStart.Contains(id);
            if (openOnStart && !has) entry.openOnStart.Add(id);
            else if (!openOnStart && has) entry.openOnStart.Remove(id);
        }

        public bool EditorGet(string sceneName, UIId id)
        {
            SceneEntry entry = FindEntry(sceneName);
            return entry != null && entry.openOnStart.Contains(id);
        }
#endif
    }
}
