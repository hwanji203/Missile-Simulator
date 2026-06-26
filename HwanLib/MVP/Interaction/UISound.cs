using HwanLib.GGMLib.SoundSystem;

namespace MVP.Interaction
{
    /// <summary>
    /// 전역 UI 사운드 접근점. UISoundService가 미등록이면 무음으로 동작한다.
    /// </summary>
    public static class UISound
    {
        private static UISoundService _service;

        public static void Register(UISoundService service) => _service = service;

        public static void Play(SoundClipSO clip) => _service?.Play(clip);

        /// <summary>custom이 있으면 custom, 없으면 defaultClip 재생.</summary>
        public static void Play(SoundClipSO custom, SoundClipSO defaultClip)
        {
            if (custom != null) Play(custom);
            else Play(defaultClip);
        }
    }
}
