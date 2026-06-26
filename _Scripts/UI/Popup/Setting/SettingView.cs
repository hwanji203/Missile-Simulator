using MVP.System.AbstractMVP;

namespace UI.Popup.Setting
{
    /// <summary>
    /// 설정 팝업 화면. 창 열기/닫기 애니메이션·배경 페이드·닫기버튼 구독은
    /// AbstractPopupView가 [SerializeField] windowForm/backgroundForm/closeButtons
    /// 직접 참조로 모두 처리한다. 설정창 고유 화면 로직이 없어 비워둔다
    /// (제네릭 TView 확정 + 인스펙터 배선 지점 역할).
    /// </summary>
    public class SettingView : AbstractPopupView
    {
    }
}
