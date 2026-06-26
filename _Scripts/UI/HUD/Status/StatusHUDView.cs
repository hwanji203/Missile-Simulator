using System.Collections.Generic;
using MVP.System.BaseMVP;
using MVP.System.BaseMVP.Form;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD.Status
{
    // 프리팹 안에서 VerticalLayoutGroup 부모 아래 체력바(위)·도화선바(아래) Form을 배치.
    // 도화선바 위 "조기 폭발 마진" 마커는 일회성 위젯이라 Form/바인딩을 늘리지 않고
    // 여기 View에서 RectTransform을 직접 제어한다.
    public class StatusHUDView : BaseView
    {
        [SerializeField] private Color marginColor;
        [SerializeField] private Image margin;

        public override void InitializeView(IReadOnlyList<BaseForm> forms)
        {
            base.InitializeView(forms);
            
            margin.color = marginColor;
            margin.fillAmount = 0f;
        }

        // tipRatio: 현재 퓨즈 fill 비율(마커 우측 끝이 붙는 지점), marginRatio: 마진/도화선 비율(마커 폭).
        public void SetFuseMargin(float tipRatio, float marginRatio)
        {
            margin.fillAmount = (1 - tipRatio) + marginRatio;
        }
    }
}
