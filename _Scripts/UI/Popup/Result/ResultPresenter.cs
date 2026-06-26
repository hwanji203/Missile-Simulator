using System.Collections.Generic;
using Core;
using EventChannelSystem;
using Events;
using MVP.System.BaseMVP;
using MVP.System.GenerateUI;
using MVP.UIData;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Popup.Result
{
    public class ResultPresenter : BasePresenter<ResultModel, ResultView>
    {
        [SerializeField] private EventChannelSO gameChannel;

        private ResultModel _model;
        private ResultView  _view;

        public override void InitializePresenter()
        {
            base.InitializePresenter();
            _model = (ResultModel)Model;
            _view  = (ResultView)View;
            
            _view.restartButton.OnFormInteracted += OnRestart; 
            _view.homeButton.OnFormInteracted += OnHome;
        }

        public override void Open<T>(T payload)
        {
            base.Open(payload);

            if (payload is GameEndedEvent gameEndEvt)
            {
                ScoreTracker scoreTracker = gameEndEvt.ScoreTracker;
            
                int total = scoreTracker != null ? scoreTracker.TotalCurrency : 0;
                _model.SetResults(
                    scoreTracker != null ? scoreTracker.GetSnapshot() : new List<TierKillResult>(),
                    total);

                // 시간정지/커서는 _pausesGame=true가 자동 처리.
                _view.ShowRows(_model.TierResults);
                _view.PlayCountUp(_model.TotalCurrency, 1.5f);

                UIGate.Block("Result");
            }
        }

        public override void Close()
        {
            base.Close();
            UIGate.Unblock("Result");
        }

        private void OnRestart(UIParam _)
        {
            // 시간/커서 복원은 씬 리로드 시 GameStateReset.Clear()가 처리.
            gameChannel.RaiseEvent(GameEvents.LoadSceneEvent.Init(SceneManager.GetActiveScene().buildIndex));
        }

        private void OnHome(UIParam _)
        {
            //TODO 씬을 로드할 때 UISceneManager에서 화면, 사운드 Fade 하고 HomeScene 같이 알아보기 쉽게 저장해놓기
            gameChannel.RaiseEvent(GameEvents.LoadSceneEvent.Init(0));
        }
    }
}
