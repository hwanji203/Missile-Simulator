using System.Collections;
using Events;
using MVP.System.BaseMVP;
using MVP.System.GameState;
using MVP.System.GenerateUI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UI.Popup.Notification.AcquisitionToast
{
    public class AcquisitionPresenter : BasePresenter<AcquisitionModel, AcquisitionView>
    {
        private const string BlockReason = "AcquisitionToast";

        [Tooltip("자동으로 닫히기까지의 시간(초). Space 홀드 중엔 카운트가 멈춘다(realtime).")]
        [SerializeField] private float autoCloseDelay = 1.5f;

        private AcquisitionModel _model;
        private bool _frozen; // Space 홀드로 인한 TimeManager 정지 보유 중인지(누수 방지용 추적)

        // BasePresenter.View는 BaseView 타입이라 구체 View 멤버 접근용 캐스팅 접근자.
        private AcquisitionView ToastView => (AcquisitionView)View;

        public override void InitializePresenter()
        {
            base.InitializePresenter();
            _model = (AcquisitionModel)Model;
            OnClosed += HandleClosed;
        }

        // 트리거(PlayerSkillAcquiredEvent 구독)는 UIOpener가 담당.
        // UIManager가 payload(이벤트)를 실어 호출한다. 이미 열려 있으면 내용만 교체 + 타이머 재시작.
        public override void Open<T>(T payload)
        {
            if (payload is not PlayerSkillAcquiredEvent evt || evt.Skill == null) return;

            if (View.IsOpen)
            {
                ApplySkill(evt);
                StopAllCoroutines();
                StartCoroutine(AutoClose());
                return;
            }

            _model.UpdateData(evt.Skill, evt.Level, evt.IsNew);

            base.Open(payload); // View 열기 + UpdateView(타이틀/설명 갱신)
            ToastView.SetIcon(_model.IconSprite, _model.IconColor);

            UIGate.Block(BlockReason);
            // _pausesGame=false. 정지는 Space 홀드 동안만 — AutoClose가 관리.

            StopAllCoroutines();
            StartCoroutine(AutoClose());
        }

        // 이미 열려 있을 때 내용만 갱신.
        private void ApplySkill(PlayerSkillAcquiredEvent evt)
        {
            _model.UpdateData(evt.Skill, evt.Level, evt.IsNew);
            View.UpdateView();
            ToastView.SetIcon(_model.IconSprite, _model.IconColor);
        }

        // realtime 카운트. Space 홀드 동안엔 카운트를 멈추고 시간도 정지해 천천히 읽게 한다.
        private IEnumerator AutoClose()
        {
            float elapsed = 0f;
            while (elapsed < autoCloseDelay)
            {
                bool held = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;

                // 홀드 시작 → 정지, 해제 → 재개. holder-set이라 중복/누락에 안전.
                if (held && !_frozen)      { TimeManager.Stop(this);   _frozen = true; }
                else if (!held && _frozen) { TimeManager.Resume(this); _frozen = false; }

                if (!held) elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // 닫기 전 정지 잔류 해제(홀드 중엔 elapsed가 안 늘어 여기 도달 시 보통 미홀드지만 방어적으로).
            if (_frozen) { TimeManager.Resume(this); _frozen = false; }

            if (View.IsOpen) Close();
        }

        // 닫힘 애니메이션 완료 후 호출(트랜지션은 SetUpdate(true)라 timeScale=0에서도 재생됨).
        private void HandleClosed(BasePresenter _)
        {
            if (_frozen) { TimeManager.Resume(this); _frozen = false; }
            UIGate.Unblock(BlockReason);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            OnClosed -= HandleClosed;

            // 열린 채 파괴(씬 전환 등) 시 전역 상태 복구.
            if (IsOpen)
            {
                if (_frozen) { TimeManager.Resume(this); _frozen = false; }
                UIGate.Unblock(BlockReason);
            }
        }
    }
}
