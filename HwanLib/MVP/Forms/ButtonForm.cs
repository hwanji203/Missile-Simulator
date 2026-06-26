using MVP.System.AbstractMVP.Form;

namespace MVP.Forms
{
    /// <summary>
    /// 의미(클릭)만 가진 버튼 Form. 비주얼 연출은 InteractionFeedback이 전담한다.
    /// (구 FadeButtonForm의 대체 — 알파 페이드 책임이 Feedback으로 이동)
    /// </summary>
    public class ButtonForm : AbstractClickForm
    {
    }
}
