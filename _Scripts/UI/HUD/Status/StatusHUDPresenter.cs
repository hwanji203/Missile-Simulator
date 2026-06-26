using CombatSystem;
using EventChannelSystem;
using Events;
using MVP.System.BaseMVP;
using Players;
using UnityEngine;

namespace UI.HUD.Status
{
    // 체력바 + 로켓 도화선(MissileFuse) 시간바를 한 레이아웃으로 묶은 통합 HUD.
    // 생성형 UI라 씬 오브젝트(플레이어)를 인스펙터로 배선할 수 없다.
    // 에셋인 EventChannelSO만 직렬화해두고, 이벤트 채널로 데이터를 받아간다.
    //  - 체력 : PlayerInitEvent로 HealthModule 참조를 받아 OnHealthChanged 구독(변할 때만 갱신).
    //  - 도화선: MissileFuse가 매 프레임 발행하는 FuseTimeChangedEvent 페이로드로 갱신(모듈 참조 없음).
    // (플레이어가 발행하는 것과 동일한 채널 에셋을 연결할 것.)
    public class StatusHUDPresenter : BasePresenter<StatusHUDModel, StatusHUDView>
    {
        [SerializeField] private EventChannelSO playerChannel;
        [SerializeField] private PlayerDefaultStatusSO playerDefaultStatus;

        private HealthModule   _healthModule;
        private StatusHUDModel  _model;
        private StatusHUDView  _view;

        public override void InitializePresenter()
        {
            base.InitializePresenter();
            _model = (StatusHUDModel)Model;
            _view = (StatusHUDView)View;

            playerChannel?.AddListener<PlayerInitEvent>(HandlePlayerInit);
            playerChannel?.AddListener<FuseTimeChangedEvent>(HandleFuseTimeChange);
        }

        // 플레이어 초기화 1회: HealthModule 참조 확보 + 변경 구독 + 초기값 즉시 반영.
        private void HandlePlayerInit(PlayerInitEvent evt)
        {
            _healthModule = evt.Health;
            if (_healthModule == null) return;

            _healthModule.OnHealthChanged += HandleHealthChanged;
            HandleHealthChanged();
        }

        // 체력 변화 시 모델 갱신 후 화면 반영.
        private void HandleHealthChanged()
        {
            _model.SetHealth(_healthModule.CurrentHealth, _healthModule.MaxHealth, playerDefaultStatus.DefaultHp);
            Refresh();
        }

        // 도화선 시간(매 프레임) 갱신 후 화면 반영. 마진 마커는 Form 바인딩을 거치지 않고 View에 직접 전달.
        private void HandleFuseTimeChange(FuseTimeChangedEvent evt)
        {
            _model.SetTime(evt.CurrentFuseTime, evt.MaxFuseTime
                , evt.IsInfinite, playerDefaultStatus.DefaultFuseTime, evt.SpareTime);
            Refresh();

            float tip    = evt.IsInfinite ? 1f : evt.MaxFuseTime > 0f ? evt.CurrentFuseTime / evt.MaxFuseTime : 0f;
            float spareRatio = evt.MaxFuseTime > 0f ? Mathf.Clamp01(evt.SpareTime / evt.MaxFuseTime) : 0f;
            if (View != null && View.IsOpen) _view.SetFuseMargin(tip, spareRatio);
        }

        // 열려 있을 때만 모든 Form을 다시 그린다.
        private void Refresh()
        {
            if (View != null && View.IsOpen) View.UpdateView();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            playerChannel?.RemoveListener<PlayerInitEvent>(HandlePlayerInit);
            playerChannel?.RemoveListener<FuseTimeChangedEvent>(HandleFuseTimeChange);
            if (_healthModule != null)
                _healthModule.OnHealthChanged -= HandleHealthChanged;
        }
    }
}
