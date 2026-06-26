using EventChannelSystem;
using HwanLib.GGMLib.SoundSystem;
using UnityEngine;

namespace MVP.Interaction
{
    /// <summary>
    /// UIManager GO에 한 번만 붙인다. SoundManager로 PlaySoundEvent를 중계한다.
    /// </summary>
    [DisallowMultipleComponent]
    public class UISoundService : MonoBehaviour, ISoundable
    {
        [SerializeField] private EventChannelSO soundChannel;

        private void Awake() => UISound.Register(this);

        public void Play(SoundClipSO clip)
        {
            if (clip == null || soundChannel == null) return;
            soundChannel.RaiseEvent(SoundSystemEvents.PlaySoundEvent.Init(Vector3.zero, clip));
        }
    }
}
