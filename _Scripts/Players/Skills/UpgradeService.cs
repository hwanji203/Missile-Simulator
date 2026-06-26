using System;
using CombatSystem;
using EventChannelSystem;
using Events;
using ModuleSystem;
using UnityEngine;

namespace Players
{
    // 스탯(공중 아이템)과 스킬(날아가는 좀비) 두 획득 경로가 공유하는 업그레이드 진입점.
    // 카테고리별로 아직 maxLevel 미만인 항목 중 랜덤 1개를 인벤토리에 반영하고,
    // 같은 PlayerSkillAcquiredEvent를 발행해 획득 UI(토스트)를 동일하게 탄다.
    public class UpgradeService : MonoBehaviour, IModule
    {
        [SerializeField] private EventChannelSO playerChannel;
        [Tooltip("스탯+스킬 통합 풀. 각 항목의 category로 경로가 갈린다.")]
        [SerializeField] private PlayerSkillSO[] pool;

        public ModuleOwner Owner { get; private set; }

        private PlayerSkillInventory _inventory;

        public void Initialize(ModuleOwner owner)
        {
            Owner = owner;
            _inventory = owner.GetModule<PlayerSkillInventory>();
        }

        // category에 해당하고 아직 maxLevel 미만인 것 중 랜덤 1개를 획득.
        // 성공 시 true + PlayerSkillAcquiredEvent 발행. 가능 항목 없으면 false.
        public bool AcquireRandom(UpgradeCategory category)
        {
            if (pool == null || _inventory == null) return false;

            PlayerSkillSO[] available = Array.FindAll(
                pool, s => s != null && s.category == category
                           && (s.category != UpgradeCategory.Skill
                               || _inventory.GetLevel(s.skillType) < s.maxLevel));

            if (available.Length == 0) return false;

            PlayerSkillSO picked = available[UnityEngine.Random.Range(0, available.Length)];
            return Acquire(picked);
        }

        // 지정한 항목을 직접 획득(스탯 아이템처럼 스폰 시점에 대상이 정해진 경로).
        // 성공 시 true + PlayerSkillAcquiredEvent 발행. MaxLevel이면 false.
        public bool Acquire(PlayerSkillSO picked)
        {
            if (picked == null || _inventory == null) return false;

            SkillAddResult result = _inventory.AddOrUpgrade(picked);
            if (result == SkillAddResult.MaxLevel) return false;

            playerChannel?.RaiseEvent(
                PlayerEvents.PlayerSkillAcquiredEvent.InitData(
                    picked,
                    _inventory.GetLevel(picked.skillType),
                    result == SkillAddResult.Added));
            return true;
        }
    }
}
