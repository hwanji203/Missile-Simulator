using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace HwanLib.UI.Layouts
{
    /// <summary>
    /// VerticalLayoutGroup을 확장해, 플레이 중 레이아웃 변화(위치/크기)를 DOTween으로
    /// 부드럽게 적용한다. 에디터(비플레이)에서는 순정 VLG와 100% 동일하게 동작한다.
    ///
    /// 동작: Unity 레이아웃 rebuild 한 사이클 안에서
    ///  - SetLayoutHorizontal(): base가 자식 x/width를 target으로 바꾸기 "직전"의
    ///    현재 실제 값(=날아가던 중간값)을 스냅샷.
    ///  - SetLayoutVertical(): base가 자식 y/height까지 채워 full target 완성 → 그 값을
    ///    target으로 보고, 자식을 스냅샷 값으로 되돌린 뒤 target까지 트윈.
    /// H/V가 같은 프레임 rebuild라서 중간 스냅은 화면에 보이지 않는다.
    /// </summary>
    [AddComponentMenu("Layout/DoTween Vertical Layout Group")]
    public class DoTweenVerticalLayoutGroup : VerticalLayoutGroup
    {
        [SerializeField] private float duration = 0.25f;
        [SerializeField] private Ease ease = Ease.OutCubic;

        // SetLayoutHorizontal 직전(= base가 덮어쓰기 전)의 자식 현재 실제 값.
        private readonly Dictionary<RectTransform, Vector2> _startPos = new();
        private readonly Dictionary<RectTransform, Vector2> _startSize = new();

        // OnEnable 직후 첫 레이아웃은 트윈 없이 즉시 스냅.
        private bool _snapNext;

        protected override void OnEnable()
        {
            base.OnEnable();
            _snapNext = true;
        }

        protected override void OnDisable()
        {
            KillAllChildTweens();
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            KillAllChildTweens();
            base.OnDestroy();
        }

        public override void SetLayoutHorizontal()
        {
            if (!Application.isPlaying)
            {
                base.SetLayoutHorizontal();
                return;
            }

            // base가 x/width를 target으로 바꾸기 전에 현재 실제 값을 스냅샷.
            SnapshotCurrent();
            base.SetLayoutHorizontal();
        }

        public override void SetLayoutVertical()
        {
            if (!Application.isPlaying)
            {
                base.SetLayoutVertical();
                return;
            }

            // base가 y/height까지 채우면 자식은 full target 위치/크기에 놓인다.
            base.SetLayoutVertical();

            if (_snapNext)
            {
                // 켜진 직후 첫 레이아웃: target 그대로 두고(이미 base가 배치함) 스냅.
                _snapNext = false;
                _startPos.Clear();
                _startSize.Clear();
                return;
            }

            ApplyTweens();
        }

        private void SnapshotCurrent()
        {
            _startPos.Clear();
            _startSize.Clear();

            var children = rectChildren;
            for (int i = 0; i < children.Count; i++)
            {
                var rt = children[i];
                _startPos[rt] = rt.anchoredPosition;
                _startSize[rt] = rt.sizeDelta;
            }
        }

        private void ApplyTweens()
        {
            var children = rectChildren;
            for (int i = 0; i < children.Count; i++)
            {
                var rt = children[i];

                // base가 이미 옮겨놓은 현재 값이 곧 최종 target.
                Vector2 targetPos = rt.anchoredPosition;
                Vector2 targetSize = rt.sizeDelta;

                // 이번 스냅샷에 없던(활성 중 새로 추가된) 자식은 target 그대로 둔다.
                if (!_startPos.TryGetValue(rt, out var startPos))
                    continue;
                _startSize.TryGetValue(rt, out var startSize);

                // 날아가던 트윈 중단 → 현재 실제 위치(=스냅샷)에서 새 target으로 갈아탄다.
                rt.DOKill();
                rt.anchoredPosition = startPos;
                rt.sizeDelta = startSize;

                rt.DOAnchorPos(targetPos, duration).SetEase(ease).SetUpdate(true);
                rt.DOSizeDelta(targetSize, duration).SetEase(ease).SetUpdate(true);
            }

            _startPos.Clear();
            _startSize.Clear();
        }

        private void KillAllChildTweens()
        {
            var children = rectChildren;
            if (children != null)
            {
                for (int i = 0; i < children.Count; i++)
                {
                    var rt = children[i];
                    if (rt != null) rt.DOKill();
                }
            }
            _startPos.Clear();
            _startSize.Clear();
        }
    }
}
