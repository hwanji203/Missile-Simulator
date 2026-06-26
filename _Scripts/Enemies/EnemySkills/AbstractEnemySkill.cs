using System;
using Agents;
using CombatSystem;
using UnityEngine;

namespace Enemies.EnemySkills
{
    public abstract class AbstractEnemySkill : MonoBehaviour, ISkill
    {
        public event Action OnSkillEnd;
        [field: SerializeField] public SkillDataSO SkillData { get; private set; }

        protected AbstractEnemy OwnerEnemy;
        protected float LastUseTime;
        protected IRenderer Renderer;

        public bool IsUsing { get; private set; }

        // 쿨다운 길이(초). 기본은 SkillData. 티어별 쿨다운이 필요한 스킬(ThrowSkill)은 오버라이드한다.
        protected virtual float Cooldown => SkillData != null ? SkillData.cooldown : 0f;

        public virtual float NormalizedCooldown
            => Mathf.Approximately(Cooldown, 0f)
            ? 1f : Mathf.Clamp01((Time.time - LastUseTime) / Cooldown);

        public virtual void InitializeSkill(ISkillModule skillModule)
        {
            OwnerEnemy = skillModule.Owner as AbstractEnemy;
            Debug.Assert(OwnerEnemy != null, $"적 공격 스킬은 반드시 적 공격 컴포넌트의 자식이어야 합니다. {gameObject}");
            Renderer = OwnerEnemy.GetModule<IRenderer>(); //렌더링 모듈 가져오고
            LastUseTime = -int.MaxValue;
        }

        public abstract bool CanUseSkill(GameObject target = null);

        public virtual void UseSkill(GameObject target = null)
        {
            IsUsing = true;
        }

        public virtual void StopSkill()
        {
            IsUsing = false;
            LastUseTime = Time.time;
            OnSkillEnd?.Invoke();
        }
    }
}
