using System.Collections.Generic;
using UnityEngine;

namespace Enemies
{
    // 에너미에 부착해 두면 Show()/Hide()로 아웃라인 머티리얼을 토글한다.
    // sharedMaterials 배열 끝에 outlineMaterial을 추가/제거하는 방식.
    // outlineMaterial = Assets/_Shaders/EnemyOutline/EnemyOutline.shader 로 만든 머티리얼.
    public class EnemyOutline : MonoBehaviour
    {
        [SerializeField] private Material outlineMaterial;
        [SerializeField] private SkinnedMeshRenderer[] renderers;

        private readonly List<(SkinnedMeshRenderer r, Material[] orig)> _saved = new();
        private bool _shown;

        private void Awake()
        {
            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        }

        public void Show()
        {
            if (_shown || outlineMaterial == null) return;
            _shown = true;
            _saved.Clear();
            foreach (var r in renderers)
            {
                if (r == null) continue;
                var orig = r.sharedMaterials;
                _saved.Add((r, orig));
                Material[] next = new Material[orig.Length + 1];
                orig.CopyTo(next, 0);
                next[orig.Length] = outlineMaterial;
                r.sharedMaterials = next;
            }
        }

        public void Hide()
        {
            if (!_shown) return;
            _shown = false;
            foreach (var (r, orig) in _saved)
                if (r != null) r.sharedMaterials = orig;
            _saved.Clear();
        }

        private void OnDisable() => Hide();
    }
}
