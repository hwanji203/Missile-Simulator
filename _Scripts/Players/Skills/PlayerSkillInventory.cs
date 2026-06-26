using System;
using System.Collections.Generic;
using CombatSystem;
using ModuleSystem;
using UnityEngine;

namespace Players
{
    public enum SkillAddResult { Added, Upgraded, MaxLevel }

    public class PlayerSkillInventory : MonoBehaviour, IModule, ISkillBonusProvider
    {
        public event Action OnBonusChanged;
        public event Action<PlayerSkillSO, int, SkillAddResult> OnSkillChanged;

        public ModuleOwner Owner { get; private set; }

        private readonly Dictionary<SkillType, (PlayerSkillSO skill, int level)> _skills = new();

        public void Initialize(ModuleOwner owner)
        {
            Owner = owner;
        }

        public SkillAddResult AddOrUpgrade(PlayerSkillSO skill)
        {
            if (_skills.TryGetValue(skill.skillType, out var entry))
            {
                if (skill.category == UpgradeCategory.Skill && entry.level >= skill.maxLevel)
                    return SkillAddResult.MaxLevel;

                int newLevel = entry.level + 1;
                _skills[skill.skillType] = (skill, newLevel);
                OnSkillChanged?.Invoke(skill, newLevel, SkillAddResult.Upgraded);
                OnBonusChanged?.Invoke();
                return SkillAddResult.Upgraded;
            }

            _skills[skill.skillType] = (skill, 1);
            OnSkillChanged?.Invoke(skill, 1, SkillAddResult.Added);
            OnBonusChanged?.Invoke();
            return SkillAddResult.Added;
        }

        public float GetBonus(SkillType type)
        {
            if (!_skills.TryGetValue(type, out var entry)) return 0f;
            float[] values = entry.skill.perLevelValues;
            if (values == null || values.Length == 0) return 0f;
            float total = 0f;
            // 레벨이 배열 길이를 넘으면 마지막 값을 반복 가산(스탯 무한 레벨). 캡 스킬은 길이를 안 넘어 동작 불변.
            for (int i = 0; i < entry.level; i++)
                total += values[Mathf.Min(i, values.Length - 1)];
            return total;
        }

        // 스킬군 전용: 현재 레벨의 perLevelValues 절대값을 조회한다(간격·강도 등).
        // GetBonus(레벨까지 누적 합산, 스탯군)와 달리 "현재 레벨 한 칸의 값"만 돌려준다.
        // 레벨 0(미획득)이면 0. 레벨이 배열 길이를 넘으면 마지막 값으로 클램프.
        public float GetLevelValue(SkillType type)
        {
            if (!_skills.TryGetValue(type, out var entry)) return 0f;
            float[] values = entry.skill.perLevelValues;
            if (values == null || values.Length == 0) return 0f;
            return values[Mathf.Clamp(entry.level - 1, 0, values.Length - 1)];
        }

        public int GetLevel(SkillType type)
            => _skills.TryGetValue(type, out var entry) ? entry.level : 0;
    }
}
