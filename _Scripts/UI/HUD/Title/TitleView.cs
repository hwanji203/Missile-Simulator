using MVP.System.BaseMVP;
using UnityEngine;
using DG.Tweening;   // DOTween 사용을 위해 추가

namespace UI.HUD.Title
{
    public class TitleView : BaseView
    {
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("깜빡임 설정")]
        [SerializeField] private float minAlpha = 0.3f;  // 가장 흐려질 때 값
        [SerializeField] private float maxAlpha = 1f;    // 가장 진해질 때 값
        [SerializeField] private float duration = 1f;    // 한 번 변하는 데 걸리는 시간(초)

        private Tween blinkTween;   // 반복 애니메이션을 저장해 둘 변수 (나중에 멈추려고)

        public override void OpenView()
        {
            base.OpenView();

            // 시작 alpha를 최소값으로 맞춤
            canvasGroup.alpha = minAlpha;

            // maxAlpha까지 올라갔다가, 다시 내려오기를 무한 반복
            blinkTween = canvasGroup
                .DOFade(maxAlpha, duration)        // maxAlpha까지 서서히 변함
                .SetLoops(-1, LoopType.Yoyo)       // -1 = 무한 반복, Yoyo = 올라갔다 내려갔다
                .SetEase(Ease.InOutSine);          // 부드럽게 (선택 사항)
        }

        public override void CloseView()
        {
            // 반복 애니메이션이 살아있으면 멈추고 정리함
            if (blinkTween != null && blinkTween.IsActive())
            {
                blinkTween.Kill();   // 트윈을 죽임(멈춤)
                blinkTween = null;
            }

            base.CloseView();
        }
    }
}