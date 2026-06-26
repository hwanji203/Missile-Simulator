using EventChannelSystem;
using Events;
using HwanLib.GGMLib.SoundSystem;
using MVP.System.BaseMVP;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UI.HUD.Title
{
    public class TitlePresenter : BasePresenter<TitleModel, TitleView>
    {
        [SerializeField] private EventChannelSO gameChannel;
        [Header("사운드")]
        [SerializeField] private EventChannelSO soundChannel;
        [SerializeField] private SoundClipSO titleBgm;
        
        public override void Open<T>(T payload)
        {
            base.Open(payload);
            if (soundChannel != null && titleBgm != null)
            {
                soundChannel.RaiseEvent(SoundSystemEvents.PlaySoundEvent.Init(Vector3.zero, titleBgm, titleBgm));
            }
        }

        public override void Close()
        {
            if (soundChannel != null && titleBgm != null)
            {
                soundChannel.RaiseEvent(SoundSystemEvents.StopSoundEvent.Init(titleBgm));
            }
            base.Close();
        }

        protected override void OnDestroy()
        {
            if (IsOpen && soundChannel != null && titleBgm != null)
            {
                soundChannel.RaiseEvent(SoundSystemEvents.StopSoundEvent.Init(titleBgm));
            }
            base.OnDestroy();
        }

        private void Update()
        {
            if (!IsOpen)
                return;

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                gameChannel.RaiseEvent(GameEvents.LoadSceneEvent.Init(1));
                if (IsOpen && soundChannel != null && titleBgm != null)
                {
                    soundChannel.RaiseEvent(SoundSystemEvents.StopSoundEvent.Init(titleBgm));
                }
            }
        }
    }
}
