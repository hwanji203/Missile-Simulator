using MVP.System.AbstractMVP;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Popup.Notification.AcquisitionToast
{
    public class AcquisitionView : AbstractPopupView
    {
        [SerializeField] private Image icon;

        // 스킬별 아이콘 스프라이트/색을 직접 대입한다.
        public void SetIcon(Sprite sprite, Color color)
        {
            if (icon == null) return;
            icon.sprite = sprite;
            icon.color  = color;
        }
    }
}
