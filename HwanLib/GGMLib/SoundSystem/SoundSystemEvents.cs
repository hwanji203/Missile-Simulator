using EventChannelSystem;
using UnityEngine;

namespace HwanLib.GGMLib.SoundSystem
{
    public static class SoundSystemEvents
    {
        public static readonly PlaySoundEvent PlaySoundEvent = new PlaySoundEvent();
        public static readonly StopSoundEvent StopSoundEvent = new StopSoundEvent();
    }

    public class PlaySoundEvent : GameEvent
    {
        public Vector3 Position;
        public SoundClipSO ClipData;
        /// <summary>null = 일회성. non-null = 루프 식별 키 (이전 int ChannelNumber 대체).</summary>
        public SoundClipSO LoopKey;

        public PlaySoundEvent Init(SoundClipSO clipData, SoundClipSO loopKey = null)
        {
            ClipData = clipData;
            LoopKey = loopKey;
            return this;
        }
        
        public PlaySoundEvent Init(Vector3 position, SoundClipSO clipData, SoundClipSO loopKey = null)
        {
            Position = position;
            ClipData = clipData;
            LoopKey = loopKey;
            return this;
        }
    }

    public class StopSoundEvent : GameEvent
    {
        public SoundClipSO LoopKey;

        public StopSoundEvent Init(SoundClipSO loopKey)
        {
            LoopKey = loopKey;
            return this;
        }
    }
}
