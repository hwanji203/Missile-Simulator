using MVP.System.AbstractMVP.Form;
using MVP.System.BaseMVP;
using MVP.UIData;
using UnityEngine;

namespace MVP.Forms
{
    // 바인딩된 배율(UIFloatParam)에 따라 RectTransform의 가로폭을 키운다.
    //  - 기준폭 W0 = Initialize 시점의 저작된 sizeDelta.x (배율 1일 때의 크기).
    //  - UpdateVisual(mult) → sizeDelta.x = W0 × mult.
    // 체력 최대치 증가(메타 시작/인게임 HpUp)에 맞춰 체력바를 통째로 키우는 용도.
    // 주의: 가로폭이 레이아웃 그룹에 의해 강제되지 않는 RectTransform에 붙여야 sizeDelta.x가 먹는다.
    public class BarWidthScaleForm : AbstractVisualForm, IInitializable
    {
        private RectTransform _rt;
        private float _baseWidth;

        public void Initialize()
        {
            _rt = GetComponent<RectTransform>();
            _baseWidth = _rt.sizeDelta.x;
        }

        protected override void UpdateVisual(UIParam data)
        {
            if (_rt == null) return;
            float mult = ((UIFloatParam)data).Value;
            if (float.IsNaN(mult)) return;
            _rt.sizeDelta = new Vector2(_baseWidth * mult, _rt.sizeDelta.y);
        }
    }
}
