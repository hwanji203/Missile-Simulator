using DG.Tweening;
using UnityEngine;

namespace MVP.Visual
{
    /// <summary>
    /// 하이라이트 RectTransform을 좌→우로 주기적으로 스윕하는 광택 연출.
    /// 마스크/머티리얼 구조(Mask + 하이라이트 자식)는 Heat 프리팹 참고 — 이 스크립트는 하이라이트의 X 이동만 담당.
    /// highlight는 부모(마스크) 폭 기준 startX→endX 로 이동. 아무 GO(부모=마스크)에 부착.
    /// </summary>
    public class ShinyImage : MonoBehaviour
    {
        [SerializeField] private RectTransform highlight;     // 스윕할 하이라이트(미지정 시 자기 자신)
        [SerializeField] private float startX = -200f;
        [SerializeField] private float endX = 200f;
        [SerializeField] private float sweepDuration = 0.6f;  // 한 번 쓸고 지나가는 시간
        [SerializeField] private float interval = 2f;         // 스윕 사이 대기 시간
        [SerializeField] private Ease ease = Ease.InOutSine;
        [SerializeField] private bool ignoreTimeScale = true;

        private Sequence _seq;

        private void Awake()
        {
            if (highlight == null) highlight = transform as RectTransform;
        }

        private void OnEnable()
        {
            Play();
        }

        private void OnDisable()
        {
            _seq?.Kill();
            _seq = null;
        }

        private void Play()
        {
            _seq?.Kill();
            if (highlight == null) return;

            SetX(startX);

            _seq = DOTween.Sequence()
                .Append(highlight.DOAnchorPosX(endX, sweepDuration).SetEase(ease))
                .AppendCallback(() => SetX(startX))
                .AppendInterval(interval)
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(ignoreTimeScale);
        }

        private void SetX(float x)
        {
            Vector2 p = highlight.anchoredPosition;
            p.x = x;
            highlight.anchoredPosition = p;
        }
    }
}
