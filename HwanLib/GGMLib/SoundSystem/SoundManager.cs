using System.Collections.Generic;
using EventChannelSystem;
using ObjectPool.Runtime;
using UnityEngine;

namespace HwanLib.GGMLib.SoundSystem
{
    public class SoundManager : MonoBehaviour
    {
        [SerializeField] private PoolManagerSO poolManagerSO;
        [SerializeField] private PoolItemSO soundItemSO;

        [field: SerializeField] public EventChannelSO SoundEventChannel { get; private set; }

        private readonly Dictionary<SoundClipSO, SoundPlayer> _soundPlayerDict = new();

        private void Awake()
        {
            SoundEventChannel.AddListener<PlaySoundEvent>(HandlePlaySoundEvent);
            SoundEventChannel.AddListener<StopSoundEvent>(HandleStopSoundEvent);
        }

        private void OnDestroy()
        {
            SoundEventChannel.RemoveListener<PlaySoundEvent>(HandlePlaySoundEvent);
            SoundEventChannel.RemoveListener<StopSoundEvent>(HandleStopSoundEvent);
        }

        private void HandlePlaySoundEvent(PlaySoundEvent evt)
        {
            SoundPlayer soundPlayer = poolManagerSO.Pop<SoundPlayer>(soundItemSO);
            soundPlayer.transform.position = evt.Position;
            soundPlayer.PlaySound(evt.ClipData);
            soundPlayer.OnSoundFinished += HandleSoundFinish;

            if (evt.LoopKey != null && evt.ClipData.isLoop)
            {
                if (_soundPlayerDict.TryGetValue(evt.LoopKey, out SoundPlayer prev))
                {
                    prev.ForceStopSound();
                    poolManagerSO.Push(prev);
                    _soundPlayerDict.Remove(evt.LoopKey);
                }
                _soundPlayerDict.Add(evt.LoopKey, soundPlayer);
            }
            else if (evt.LoopKey == null && evt.ClipData.isLoop)
            {
                Debug.LogWarning($"[SoundManager] Loop 클립에는 LoopKey가 필요합니다. ({evt.ClipData.name})");
            }
        }

        private void HandleSoundFinish(SoundPlayer soundPlayer)
        {
            soundPlayer.OnSoundFinished -= HandleSoundFinish;
            poolManagerSO.Push(soundPlayer);
        }

        private void HandleStopSoundEvent(StopSoundEvent evt)
        {
            if (_soundPlayerDict.TryGetValue(evt.LoopKey, out SoundPlayer soundPlayer))
            {
                soundPlayer.ForceStopSound();
                soundPlayer.OnSoundFinished -= HandleSoundFinish;
                poolManagerSO.Push(soundPlayer);
                _soundPlayerDict.Remove(evt.LoopKey);
            }
        }
    }
}
