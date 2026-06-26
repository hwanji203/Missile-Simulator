using MVP.System.AbstractMVP;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Popup.Meta
{
    // 헤드리스 뷰 — 메타 진행 저장은 UI 없이 동작.
    // 향후 메인 화면 구현 시 이 클래스를 확장.
    public class MetaProgressView : AbstractPopupView
    {
        public override void OpenView()
        {
            base.OpenView();
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(transition.GetComponent<RectTransform>());
        }
    }
}
