using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace MVP.Forms
{
    public enum FromDirection { None, Top, Bottom, Left, Right }

    [RequireComponent(typeof(LayoutElement))]
    public class UITransition : MonoBehaviour
    {
        [SerializeField] private float showDuration = 0.25f;
        [SerializeField] private float hideDuration = 0.2f;

        [Header("Auto — 오브젝트가 켜질 때 PlayShow 자동 재생")]
        [SerializeField] private bool playOnEnable;

        [Header("Delay — Transition 시작 전 대기(초)")]
        [SerializeField] private float showDelay;

        [Header("Fade — CanvasGroup alpha 0↔1")]
        [SerializeField] private bool fadeEnabled;

        [Header("Size — localScale sizeFrom↔1")]
        [SerializeField] private bool  sizeEnabled = true;
        [SerializeField] private float sizeFrom    = 0f;
        [SerializeField] private Ease  showEase    = Ease.OutBack;
        [SerializeField] private Ease  hideEase    = Ease.InBack;

        [Header("Move — 방향에서 제자리로 슬라이드")]
        [SerializeField] private bool          moveEnabled;
        [SerializeField] private FromDirection moveDirection = FromDirection.Bottom;
        [SerializeField] private float         moveDistance  = 100f;
        [SerializeField] private Ease          moveShowEase  = Ease.OutCubic;
        [SerializeField] private Ease          moveHideEase  = Ease.InCubic;
        [Tooltip("켜면 hide가 진입 반대 방향으로 빠져 단방향 통과. 끄면 들어온 자리로 되돌아감(기본)")]
        [SerializeField] private bool          hideReverse;

        [Header("Width — RectTransform sizeDelta.x 트윈 (가로 고정폭 전제)")]
        [SerializeField] private bool  widthEnabled;
        [SerializeField] private float widthValue;                  // 절대 px
        [SerializeField] private bool  widthValueIsTarget;          // true=목표값(펼친 폭), false=시작값
        [SerializeField] private Ease  widthShowEase = Ease.OutCubic;
        [SerializeField] private Ease  widthHideEase = Ease.InCubic;

        public float ShowDuration => showDuration;
        public float HideDuration => hideDuration;

        private CanvasGroup   _group;
        private LayoutElement _layout;
        private RectTransform _rect;
        private Vector3       _originPos;
        private float         _originWidth;
        private float         _origAnchorMinX, _origAnchorMaxX;   // 트윈 전 가로 앵커 (hide 완료 시 복원용)
        private bool          _widthCaptured;
        private Sequence      _seq;

        private void Awake()
        {
            _originPos = transform.localPosition;
            _layout = GetComponent<LayoutElement>();
            if (fadeEnabled)
                _group = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            if (widthEnabled)
            {
                _rect = transform as RectTransform;
                if (_rect != null)
                {
                    _origAnchorMinX = _rect.anchorMin.x;
                    _origAnchorMaxX = _rect.anchorMax.x;
                }
            }
        }

        // playOnEnable=true면 오브젝트가 켜질 때마다 자동으로 등장 연출 재생.
        // (AbstractPopupView처럼 외부에서 PlayShow를 직접 호출하는 경우엔 false로 둘 것 — 중복 재생 방지)
        private void OnEnable()
        {
            if (playOnEnable) PlayShow();
        }

        // widthValueIsTarget=false → (start=widthValue, end=origin)
        // widthValueIsTarget=true  → (start=origin, end=widthValue)
        private float WidthStart => widthValueIsTarget ? _originWidth : widthValue;
        private float WidthEnd   => widthValueIsTarget ? widthValue   : _originWidth;

        private void SetWidth(float w) => _rect.sizeDelta = new Vector2(w, _rect.sizeDelta.y);

        // 자연 폭(실제 렌더 폭)을 한 번만 캡처. sizeDelta.x는 stretch 앵커에서 절대 폭이 아니므로 rect.width 사용.
        private void CaptureOriginWidthOnce()
        {
            if (_widthCaptured) return;
            _originWidth   = _rect.rect.width;
            _widthCaptured = true;
        }

        // 가시적 위치/폭을 유지한 채 가로 앵커(x)만 바꾼다. min==max==0.5면 sizeDelta.x == 절대 폭이 되어 폭 트윈이 정확해짐.
        private void SetHorizontalAnchor(float min, float max)
        {
            if (!(_rect.parent is RectTransform parent))
            {
                var amf = _rect.anchorMin; amf.x = min; _rect.anchorMin = amf;
                var axf = _rect.anchorMax; axf.x = max; _rect.anchorMax = axf;
                return;
            }

            float parentW    = parent.rect.width;
            float worldLeft  = _rect.anchorMin.x * parentW + _rect.offsetMin.x;
            float worldRight = _rect.anchorMax.x * parentW + _rect.offsetMax.x;

            var aMin = _rect.anchorMin; aMin.x = min; _rect.anchorMin = aMin;
            var aMax = _rect.anchorMax; aMax.x = max; _rect.anchorMax = aMax;

            var oMin = _rect.offsetMin; oMin.x = worldLeft  - min * parentW; _rect.offsetMin = oMin;
            var oMax = _rect.offsetMax; oMax.x = worldRight - max * parentW; _rect.offsetMax = oMax;
        }

        // 이동/스케일/폭 트윈은 transform·sizeDelta를 직접 건드리므로, 레이아웃 그룹 자식이면
        // 그룹이 매 프레임 값을 덮어써 연출과 충돌한다. 해당 연출이 있을 땐 레이아웃 통제에서 제외.
        private void ApplyIgnoreLayoutIfNeeded()
        {
            if (_layout != null && (moveEnabled || sizeEnabled || widthEnabled))
                _layout.ignoreLayout = true;
        }

        public void PlayShow(Action onComplete = null)
        {
            KillSeq();
            ApplyIgnoreLayoutIfNeeded();
            if (sizeEnabled)  transform.localScale    = Vector3.one * sizeFrom;
            if (fadeEnabled && _group != null) _group.alpha = 0f;
            if (moveEnabled)  transform.localPosition = _originPos + DirectionOffset();
            if (widthEnabled && _rect != null)
            {
                CaptureOriginWidthOnce();        // 자연 폭은 앵커 모으기 전에 캡처
                SetHorizontalAnchor(0.5f, 0.5f); // 절대 폭 트윈 가능하도록 가로 앵커 중앙으로
                SetWidth(WidthStart);
            }

            // "from" 상태는 위에서 즉시 적용됐으므로, 딜레이 동안 숨은 채 대기한 뒤 등장한다.
            _seq = DOTween.Sequence().SetUpdate(true);
            if (showDelay > 0f) _seq.SetDelay(showDelay);
            if (sizeEnabled)
                _seq.Join(transform.DOScale(Vector3.one, showDuration).SetEase(showEase));
            if (fadeEnabled && _group != null)
                _seq.Join(_group.DOFade(1f, showDuration).SetEase(Ease.Linear));
            if (moveEnabled)
                _seq.Join(transform.DOLocalMove(_originPos, showDuration).SetEase(moveShowEase));
            if (widthEnabled && _rect != null)
                _seq.Join(DOTween.To(() => _rect.sizeDelta.x, SetWidth, WidthEnd, showDuration).SetEase(widthShowEase));
            if (onComplete != null) _seq.OnComplete(() => onComplete());
            _seq.Play();
        }

        // onComplete: SetActive(false) + OnViewClosed 발행을 여기서 처리.
        public void PlayHide(Action onComplete = null)
        {
            KillSeq();
            ApplyIgnoreLayoutIfNeeded();
            if (widthEnabled && _rect != null)
            {
                CaptureOriginWidthOnce();
                SetHorizontalAnchor(0.5f, 0.5f); // show 없이 hide만 불려도 폭 트윈이 정확하도록 보장
            }
            _seq = DOTween.Sequence().SetUpdate(true);
            if (sizeEnabled)
                _seq.Join(transform.DOScale(Vector3.one * sizeFrom, hideDuration).SetEase(hideEase));
            if (fadeEnabled && _group != null)
                _seq.Join(_group.DOFade(0f, hideDuration).SetEase(Ease.Linear));
            if (moveEnabled)
            {
                // hideReverse=true면 진입 반대쪽으로 빠져나가 단방향 통과, false면 들어온 자리로 복귀.
                Vector3 hideTarget = _originPos + (hideReverse ? -DirectionOffset() : DirectionOffset());
                _seq.Join(transform.DOLocalMove(hideTarget, hideDuration).SetEase(moveHideEase));
            }
            if (widthEnabled && _rect != null)
                _seq.Join(DOTween.To(() => _rect.sizeDelta.x, SetWidth, WidthStart, hideDuration).SetEase(widthHideEase));
            _seq.OnComplete(() =>
            {
                // 숨겨질 때만 원래 앵커(예: 0,1 stretch)로 복원
                if (widthEnabled && _rect != null) SetHorizontalAnchor(_origAnchorMinX, _origAnchorMaxX);
                onComplete?.Invoke();
            });
            _seq.Play();
        }

        private Vector3 DirectionOffset() => moveDirection switch
        {
            FromDirection.Top    => new Vector3(0,  moveDistance, 0),
            FromDirection.Bottom => new Vector3(0, -moveDistance, 0),
            FromDirection.Left   => new Vector3(-moveDistance, 0, 0),
            FromDirection.Right  => new Vector3( moveDistance, 0, 0),
            _                    => Vector3.zero,
        };

        private void KillSeq()
        {
            if (_seq != null && _seq.IsActive()) { _seq.Complete(); _seq.Kill(); }
        }

        private void OnDestroy() => KillSeq();
    }
}
