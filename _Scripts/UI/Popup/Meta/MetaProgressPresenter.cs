using EventChannelSystem;
using Events;
using HwanLib.GGMLib.SoundSystem;
using MVP.Forms;
using MVP.System.AbstractMVP.SaveMVP;
using MVP.System.BaseMVP;
using MVP.UIData;
using UnityEngine;

namespace UI.Popup.Meta
{
    public class MetaProgressPresenter
        : AbstractSaveablePresenter<MetaProgressModel, MetaProgressView>
    {
        [SerializeField] private EventChannelSO gameChannel;
        [SerializeField] private MetaBuffSO buffSO;
        [SerializeField] private ButtonForm[] shouldRefreshButtons;

        [Header("호버 정보(우상단)")]
        [SerializeField] private MetaStatHoverInfo[] hoverItems;
        [SerializeField] private TextForm hoverText;
        [SerializeField] private TextForm descriptionText;

        [Header("사운드")]
        [SerializeField] private EventChannelSO soundChannel;
        [SerializeField] private SoundClipSO selectUibgm;

        private MetaProgressModel _model;

        public override void InitializePresenter()
        {
            base.InitializePresenter();
            _model = (MetaProgressModel)Model;

            _model.BuffSO = buffSO;
            
            gameChannel?.AddListener<GameEndedEvent>(OnGameEnded);

            foreach (var shouldRefreshButton in shouldRefreshButtons)
                shouldRefreshButton.OnFormInteracted += Refresh;

            if (hoverItems != null)
                foreach (var item in hoverItems)
                    if (item != null) item.HoverChanged += OnStatHover;

            hoverText.Text = "";
            descriptionText.Text = "";
            
            // 닫기 버튼은 View.CloseView()를 직접 부르므로 Presenter.Close() 오버라이드를 타지 않는다.
            // 모든 닫힘 경로가 수렴하는 OnClosed에 cleanup을 걸어야 버튼 클릭에서도 실행된다.
            OnClosed += HandleClosed;
        }

        // 시간정지/커서는 _pausesGame=true가 자동 처리.
        public override void Open<T>(T payload)
        {
            base.Open(payload);
            
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            soundChannel.RaiseEvent(SoundSystemEvents.PlaySoundEvent.Init(selectUibgm, selectUibgm));
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            gameChannel?.RemoveListener<GameEndedEvent>(OnGameEnded);
            OnClosed -= HandleClosed;

            foreach (var shouldRefreshButton in shouldRefreshButtons)
                shouldRefreshButton.OnFormInteracted -= Refresh;

            if (hoverItems != null)
                foreach (var item in hoverItems)
                    if (item != null) item.HoverChanged -= OnStatHover;
        }

        private void Refresh(UIParam _)
            => View.UpdateView();

        // 스탯 호버 진입/이탈 → 우상단에 "이름 + 현재값(+증가분)"과 설명. 이탈 시 둘 다 비움.
        private void OnStatHover(MetaStatHoverInfo item, bool entered)
        {
            if (hoverText != null)
                hoverText.Text = entered
                    ? $"{item.DisplayName}  {_model.GetStatHoverText(item.Stat)}"
                    : string.Empty;

            if (descriptionText != null)
                descriptionText.Text = entered ? item.Description : string.Empty;
        }

        // 닫힘 완료(버튼/프로그래밍 양쪽) 시 실행. 게임 시작 이벤트 발행.
        private void HandleClosed(BasePresenter _)
        {
            soundChannel.RaiseEvent(SoundSystemEvents.StopSoundEvent.Init(selectUibgm));
            gameChannel.RaiseEvent(GameEvents.GameStartEvent);
        }
        
        [ContextMenu("AddGold")]
        public void AddGold()
        {
            _model.AddCurrency(100);
            View.UpdateView();
        }

        private void OnGameEnded(GameEndedEvent evt)
        {
            _model.AddCurrency(evt.ScoreTracker != null ? evt.ScoreTracker.TotalCurrency : 0);
            SaveManually();
        }
        
#if UNITY_EDITOR
        [ContextMenu("Clear")]
        private void Clear()
        {
            _model.ClearData();
        }
#endif
    }
}
