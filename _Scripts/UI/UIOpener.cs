using EventChannelSystem;
using Events;
using MVP.System.GenerateUI;
using Players;
using UnityEngine;

namespace UI
{
    // 입력·게임플레이 트리거를 한곳에서 구독해 UIManager 채널(Open/Close/Toggle)로 라우팅한다.
    // UIManager를 직접 참조하지 않고 uiChannel로만 통신해 결합도를 낮춘다.
    public class UIOpener : MonoBehaviour
    {
        [SerializeField] private EventChannelSO uiChannel; // UIManager가 구독하는 채널
        [SerializeField] private EventChannelSO playerChannel; // 게임플레이 이벤트 채널
        [SerializeField] private PlayerInputSO playerInput; // 입력(Escape 토글) 소스
        [SerializeField] private EventChannelSO gameChannel;

        private void OnEnable()
        {
            playerInput.OnToggleSettingRequested += HandleToggleSetting;

            playerChannel.AddListener<PlayerSkillAcquiredEvent>(HandleSkillAcquired);
            playerChannel.AddListener<PlayerSuperBoostEvent>(HandleSuperBoost);
            
            gameChannel.AddListener<GameEndedEvent>(GameEndedHandler);
            gameChannel.AddListener<GameStartEvent>(GameStartHandler);
        }

        private void OnDisable()
        {
            playerInput.OnToggleSettingRequested -= HandleToggleSetting;

            playerChannel.RemoveListener<PlayerSkillAcquiredEvent>(HandleSkillAcquired);
            playerChannel.RemoveListener<PlayerSuperBoostEvent>(HandleSuperBoost);
            
            gameChannel.RemoveListener<GameEndedEvent>(GameEndedHandler);
        }

        // Escape → 설정창 토글.
        private void HandleToggleSetting()
            => uiChannel?.RaiseEvent(UIEvents.ToggleUIEvent.Init(UIId.Setting));

        // 스킬 획득 → 토스트 열기(이벤트를 payload로 전달, 내용 갱신은 프레젠터가 담당).
        private void HandleSkillAcquired(PlayerSkillAcquiredEvent evt)
            => uiChannel?.RaiseEvent(UIEvents.OpenUIEvent.Init(UIId.AcquisitionToast, evt));

        private void GameStartHandler(GameStartEvent evt)
        {
            uiChannel?.RaiseEvent(UIEvents.OpenUIEvent.Init(UIId.StatusHUD, evt));
        }

        // 슈퍼 부스트 시작 → 경고 UI 열기.
        private void HandleSuperBoost(PlayerSuperBoostEvent evt)
        {
            // if (evt.IsPressed)
            //     uiChannel?.RaiseEvent(UIEvents.OpenUIEvent.Init(UIId.SuperBoostWarning, null));
            // else if (evt.IsCanceled)
            //     uiChannel?.RaiseEvent(UIEvents.CloseUIEvent.Init(UIId.SuperBoostWarning));
        }

        private void GameEndedHandler(GameEndedEvent evt)
        {
            uiChannel?.RaiseEvent(UIEvents.OpenUIEvent.Init(UIId.Result, evt));
        }
    }
}
