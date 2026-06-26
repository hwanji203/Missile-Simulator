using MVP.System.AbstractMVP.SaveMVP;
using MVP.System.GenerateUI;
using Players;
using UnityEngine;
using UnityEngine.Audio;

namespace UI.Popup.Setting
{
    public class SettingPresenter : AbstractSaveablePresenter<SettingModel, SettingView>
    {
        [SerializeField] private AudioMixer    audioMixer;
        [SerializeField] private PlayerInputSO playerInput;

        public override bool CanOpen => !UIGate.IsBlocked;

        private SettingModel _model;

        // 시간정지/커서는 BasePresenter._pausesGame=true가 자동 처리(holder-set).
        public override void InitializePresenter()
        {
            base.InitializePresenter();

            _model = (SettingModel)Model;

            _model.AudioMixer  = audioMixer;
            _model.PlayerInput = playerInput;
            _model.ApplyAll();
        }
    }
}
