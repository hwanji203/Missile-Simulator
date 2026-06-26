using System.Collections.Generic;
using DG.Tweening;
using MVP.System.AbstractMVP.Form;
using MVP.UIData;
using UnityEngine;
using UnityEngine.UI;

namespace MVP.Forms
{
    // 값이 줄어들 때 "깎인 구간"을 고스트(흰색) 슬라이더가 잠시 보여주다가 Lerp로 따라 내려오는 게이지.
    // 메인 슬라이더는 즉시 스냅, 고스트는 ghostDelay 뒤 ghostDuration 동안 내려온다. 회복/증가 시엔 둘 다 즉시.
    // 연타로 맞으면 트윈을 Kill로 초기화하지 않고, 각 히트의 목표값을 큐에 쌓아 "대기→내려오기" 구간을
    // 순차 재생한다(Append처럼). 앞 구간이 끝나야 다음 구간이 시작되어 계단식으로 내려온다.
    // 바인딩은 SliderForm과 동일하게 UIFloatParam(0~1) 하나 — 기존 UpdateXxxRatio 메서드를 그대로 쓰면 된다.
    public class GhostSliderForm : AbstractVisualForm
    {
        [Tooltip("메인 뒤에 깔리는 고스트 슬라이더(흰색 fill). 메인과 같은 0~1 범위여야 한다.")]
        [SerializeField] private Image ghostImage;
        [Tooltip("줄어든 뒤 고스트가 따라 내려오기 시작할 때까지의 대기(초).")]
        [SerializeField] private float ghostDelay = 0.15f;
        [Tooltip("고스트가 새 값까지 내려오는 데 걸리는 시간(초).")]
        [SerializeField] private float ghostDuration = 0.4f;

        private Image _image;
        private Tween _ghostTween;
        private readonly Queue<float> _pending = new Queue<float>(); // 순차 재생 대기 중인 히트 목표값들

        private Image MainImage => _image != null ? _image : _image = GetComponent<Image>();

        protected override void UpdateVisual(UIParam data)
        {
            float value = ((UIFloatParam)data).Value;
            Image main = MainImage;

            if (Mathf.Approximately(value, main.fillAmount))
                return;
            
            bool decreased = value < main.fillAmount;
            main.fillAmount = value;

            if (ghostImage == null) return;
            
            if (decreased)
            {
                _pending.Enqueue(value); // 이번 히트 목표값을 큐에 적재
                if (_ghostTween == null) PlayNext(); // 재생 중이 아니면 즉시 처리 시작
            }
            else
            {
                // 회복/초기화: 큐와 진행 중 트윈을 모두 비우고 즉시 동기화
                _pending.Clear();
                _ghostTween?.Kill();
                _ghostTween = null;
                ghostImage.fillAmount = value;
            }
        }

        // 큐에서 다음 목표값을 꺼내 한 구간(대기→내려오기)을 재생. 완료되면 이어서 다음 구간을 처리한다.
        private void PlayNext()
        {
            if (_pending.Count == 0)
            {
                _ghostTween = null;
                return;
            }

            float target = _pending.Dequeue();
            _ghostTween = ghostImage.DOFillAmount(target, ghostDuration)
                .SetDelay(ghostDelay)
                .SetEase(Ease.OutQuad)
                .OnComplete(PlayNext);
        }

        private void OnDestroy()
        {
            _ghostTween?.Kill();
            _pending.Clear();
        }
    }
}
