using CombatSystem;
using EventChannelSystem;
using Events;
using ModuleSystem;
using UnityEngine;

namespace Players
{
    // 공중 스탯 아이템 픽업 이벤트를 받아 스탯 카테고리에서 랜덤 1개를 획득시킨다.
    // SkillTriggerModule(좀비→스킬)의 스탯 버전. 실제 처리는 UpgradeService가 담당.
    public class StatPickupTriggerModule : MonoBehaviour, IModule
    {
        [SerializeField] private EventChannelSO playerChannel;

        public ModuleOwner Owner { get; private set; }

        private UpgradeService _upgradeService;

        public void Initialize(ModuleOwner owner)
        {
            Owner = owner;
            _upgradeService = owner.GetModule<UpgradeService>();
        }

        private void OnEnable()
        {
            playerChannel?.AddListener<StatItemPickedUpEvent>(HandlePickup);
        }

        private void OnDisable()
        {
            playerChannel?.RemoveListener<StatItemPickedUpEvent>(HandlePickup);
        }

        private void HandlePickup(StatItemPickedUpEvent evt)
        {
            // 스폰 시 정해진 스탯이 있으면 그걸 확정 지급, 없으면 랜덤 폴백.
            if (evt.Stat != null)
                _upgradeService?.Acquire(evt.Stat);
            else
                _upgradeService?.AcquireRandom(UpgradeCategory.Stat);
        }
    }
}
