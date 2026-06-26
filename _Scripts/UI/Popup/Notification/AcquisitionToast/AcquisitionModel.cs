using MVP.System.BaseMVP;
using MVP.UIData;
using Players;
using UnityEngine;

namespace UI.Popup.Notification.AcquisitionToast
{
    public class AcquisitionModel : IModel
    {
        private string _title = string.Empty;
        private string _desc  = string.Empty;

        // Sprite/색은 UIParam이 없어 Presenter가 읽어 View에 직접 대입한다.
        public Sprite IconSprite { get; private set; }
        public Color  IconColor  { get; private set; } = Color.white;

        // 이벤트 데이터를 저장하고 자신을 반환한다. desc는 신규/강화 정보를 합성한다.
        public AcquisitionModel UpdateData(PlayerSkillSO skill, int level, bool isNew)
        {
            string suffix = $"Lv.{(level >= skill.maxLevel ? "MAX" : level)}";
            _title = string.IsNullOrEmpty(skill.title) ? skill.skillName : $"{skill.title} {suffix}";

            _desc = skill.desc;

            IconSprite = skill.iconSprite;
            IconColor  = skill.iconColor;
            return this;
        }

        // Form 바인딩용 Update 메서드(UIParam 반환).
        private UIParam UpdateTitle() => UIParams.UIStringParam.Init(_title);
        private UIParam UpdateDesc()  => UIParams.UIStringParam.Init(_desc);
    }
}
