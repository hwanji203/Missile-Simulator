using CombatSystem;
using EventChannelSystem;
using Events;
using ModuleSystem;
using UnityEngine;

namespace Players
{
    // 파워업 좀비 요격 이벤트를 받아 스킬 카테고리에서 랜덤 1개를 획득시킨다.
    // 실제 "랜덤 미달 선택 → 인벤토리 반영 → 이벤트 발행"은 UpgradeService가 담당.
    public class SkillTriggerModule : MonoBehaviour, IModule
    {
        [SerializeField] private EventChannelSO enemyChannel;

        public ModuleOwner Owner { get; private set; }

        private UpgradeService _upgradeService;

        public void Initialize(ModuleOwner owner)
        {
            Owner = owner;
            _upgradeService = owner.GetModule<UpgradeService>();
        }

        private void OnEnable()
        {
            enemyChannel?.AddListener<ZombiePowerUpEvent>(HandlePowerUp);
        }

        private void OnDisable()
        {
            enemyChannel?.RemoveListener<ZombiePowerUpEvent>(HandlePowerUp);
        }

        private void HandlePowerUp(ZombiePowerUpEvent evt)
        {
            _upgradeService?.AcquireRandom(UpgradeCategory.Skill);
        }
    }
}
