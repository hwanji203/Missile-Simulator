using System;
using System.Collections;
using EventChannelSystem;
using MVP.Forms.Module.Fade;

namespace UI.System.Fade
{
    // Fade UI 오픈 payload. 페이드 중간점(화면을 덮은 시점)에서 실행할 콜백을 담는다.
    // 델리게이트를 담으므로 UIEvents의 풀링 싱글톤과 달리 매 호출 new 인스턴스를 만든다.
    public class FadeRequestEvent : GameEvent
    {
        public Func<IEnumerator> OnMidpoint; // null 허용(무로드/즉시 전환)
        public TransitionPreset Preset;      // null 허용(FadeForm.defaultPreset 사용)

        public FadeRequestEvent(Func<IEnumerator> onMidpoint = null, TransitionPreset preset = null)
        {
            OnMidpoint = onMidpoint;
            Preset = preset;
        }
    }
}
