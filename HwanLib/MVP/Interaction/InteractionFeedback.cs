using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MVP.Interaction
{
    /// <summary>
    /// 상호작용 연출 전담 컴포넌트 (Heat Element들의 복붙 패턴을 단일화).
    /// 포인터/패드 포커스 이벤트 → FeedbackStateMachine → 프로퍼티 트웨인(알파/스케일/색조) + 사운드 훅.
    /// Form(의미 흐름)과는 같은 GameObject의 CanvasGroup.interactable로만 만난다.
    /// 무설정 부착만으로 동작하며, 느낌 조정은 공유 프로파일 SO에서 한다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public class InteractionFeedback : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        IPointerClickHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        [SerializeField] private InteractionFeedbackProfile profile;

        [Tooltip("색조(tint)를 적용할 대상. 비워두면 색조는 생략된다.")]
        [SerializeField] private Graphic[] tintTargets;

        private readonly FeedbackStateMachine _fsm = new();
        private readonly List<Tween> _tweens = new();

        private CanvasGroup _canvasGroup;
        private Selectable _selectable;
        private bool _warnedNoProfile;

        public bool IsInteractable => _fsm.IsInteractable;

        private InteractionFeedbackProfile Profile
        {
            get
            {
                if (profile != null) return profile;
                if (_warnedNoProfile == false)
                {
                    _warnedNoProfile = true;
                    Debug.LogWarning($"[MVP] {name}: InteractionFeedbackProfile 미할당 — 코드 기본값으로 동작합니다.", this);
                }
                return InteractionFeedbackProfile.Fallback;
            }
        }

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _selectable = GetComponent<Selectable>();
            ApplyInstant(_fsm.Current);
        }

        private void OnDisable()
        {
            KillTweens();
            ApplyInstant(_fsm.Current);
        }

        /// <summary>Form/Presenter가 호출하는 비활성화 수렴점. CanvasGroup·Selectable과 동기화된다.</summary>
        public void SetInteractable(bool value)
        {
            _fsm.SetInteractable(value);
            _canvasGroup.interactable = value;
            if (_selectable != null) _selectable.interactable = value;
            TransitionTo(_fsm.Current);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_fsm.IsInteractable == false) return;
            
            _fsm.SetHovered(true);
            TransitionTo(_fsm.Current);
            UISound.Play(Profile.hoverClip);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _fsm.SetHovered(false);
            TransitionTo(_fsm.Current);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            _fsm.SetPressed(true);
            TransitionTo(_fsm.Current);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            _fsm.SetPressed(false);
            TransitionTo(_fsm.Current);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_fsm.IsInteractable == false || eventData.button != PointerEventData.InputButton.Left) return;

            UISound.Play(Profile.clickClip);
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (_fsm.IsInteractable == false) return;

            _fsm.SetSelected(true);
            TransitionTo(_fsm.Current);
            UISound.Play(Profile.hoverClip);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _fsm.SetSelected(false);
            TransitionTo(_fsm.Current);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (_fsm.IsInteractable == false) return;

            UISound.Play(Profile.clickClip);
        }

        private void TransitionTo(FeedbackState state)
        {
            if (gameObject.activeInHierarchy == false)
            {
                ApplyInstant(state);
                return;
            }

            InteractionFeedbackProfile.StateStyle style = Profile.Get(state);
            KillTweens();

            _tweens.Add(_canvasGroup.DOFade(style.alpha, style.duration).SetEase(style.ease).SetUpdate(true));
            _tweens.Add(transform.DOScale(style.scale, style.duration).SetEase(style.ease).SetUpdate(true));

            if (tintTargets == null) return;
            foreach (Graphic target in tintTargets)
            {
                if (target == null) continue;
                _tweens.Add(target.DOColor(style.tint, style.duration).SetEase(style.ease).SetUpdate(true));
            }
        }

        private void ApplyInstant(FeedbackState state)
        {
            if (_canvasGroup == null) return; // Awake 이전(OnDisable 등) 호출 방어

            InteractionFeedbackProfile.StateStyle style = Profile.Get(state);
            _canvasGroup.alpha = style.alpha;
            transform.localScale = Vector3.one * style.scale;

            if (tintTargets == null) return;
            foreach (Graphic target in tintTargets)
            {
                if (target == null) continue;
                target.color = style.tint;
            }
        }

        private void KillTweens()
        {
            foreach (Tween tween in _tweens)
                tween?.Kill();
            _tweens.Clear();
        }
    }
}
