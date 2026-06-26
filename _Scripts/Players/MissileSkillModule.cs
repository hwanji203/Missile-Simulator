using System;
using System.Collections.Generic;
using System.Linq;
using CombatSystem;
using EventChannelSystem;
using ModuleSystem;
using UnityEngine;

namespace Players
{
    // 미사일용 가벼운 스킬 관리자.
    // 자식의 ISkill들을 skillIndex로 모아두고, 외부(충돌 트리거 등)의 요청으로 발동시킨다.
    public class MissileSkillModule : MonoBehaviour, ISkillModule, IModule
    {
        // 스킬이 폭발 등의 게임 이벤트를 발행할 때 사용하는 채널.
        [field: SerializeField] public EventChannelSO CreateChannel { get; private set; }
        [field: SerializeField] public EventChannelSO PlayerChannel { get; private set; }

        public ModuleOwner Owner { get; private set; }
        public event Action OnCurrentSkillEnd;

        private Dictionary<int, ISkill> _skillDict;
        private ISkill _currentSkill;

        public void Initialize(ModuleOwner owner)
        {
            Owner = owner;

            _skillDict = GetComponentsInChildren<ISkill>()
                .ToDictionary(skill => skill.SkillData.skillIndex);

            foreach (ISkill skill in _skillDict.Values)
                skill.InitializeSkill(this);
        }

        public bool CanUseSkill(int skillIndex, GameObject target = null)
        {
            if (_currentSkill is { IsUsing: true })
                return false;

            return _skillDict.TryGetValue(skillIndex, out ISkill skill) && skill.CanUseSkill(target);
        }

        public void UseSkill(int skillIndex, GameObject target = null)
        {
            if (!_skillDict.TryGetValue(skillIndex, out ISkill skill))
                return;

            if (_currentSkill != null)
                _currentSkill.OnSkillEnd -= HandleCurrentSkillEnd;

            _currentSkill = skill;
            _currentSkill.OnSkillEnd += HandleCurrentSkillEnd;
            skill.UseSkill(target);
        }

        private void HandleCurrentSkillEnd()
        {
            _currentSkill.OnSkillEnd -= HandleCurrentSkillEnd;
            InvokeSkillEnd();
            _currentSkill = null;
        }

        public void InvokeSkillEnd() => OnCurrentSkillEnd?.Invoke();

        public void StopSkillIfNotFinished()
        {
            if (_currentSkill != null)
            {
                _currentSkill.StopSkill();
                _currentSkill = null;
            }
        }
    }
}
