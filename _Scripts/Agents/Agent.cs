using System;
using CombatSystem;
using ModuleSystem;
using UnityEngine.Events;

namespace Agents
{
    // CombatAgent(데미지→HealthModule 다리)를 그대로 물려받는 적 계열 베이스.
    // 여기에 더해 적 쪽은 피격 정보(HitPoint/Normal/Attacker/Amount)를 ActionDataModule에 캐싱해
    // 죽을 때 래그돌 방향·세기 계산에 쓰고, 인스펙터 배선용 UnityEvent와 HandleHitEvent 훅을 제공한다.
    public abstract class Agent : ModuleOwner, IDamageable
    {
        public event Action OnHit;
        public event Action OnDeath;
        public bool IsDead { get; private set; }
        public HealthModule Health { get; private set; }
        
        public ActionDataModule ActionData { get; private set; }

        protected override void InitializeComponents()
        {
            base.InitializeComponents();
            Health = GetModule<HealthModule>();
            ActionData = GetModule<ActionDataModule>();
        }

        protected override void AfterInitComponents()
        {
            base.AfterInitComponents();
            if (Health != null)
                Health.OnDeath += HandleDeath;
            // CombatAgent의 C# 이벤트를 적 전용 훅과 인스펙터 UnityEvent로 흘려보낸다.
            OnHit += HandleHitEvent;
        }

        protected virtual void OnDestroy()
        {
            if (Health != null)
                Health.OnDeath -= HandleDeath;
            OnHit -= HandleHitEvent;
        }

        protected virtual void HandleHitEvent() { }

        public void ApplyDamage(DamageData damageData)
        {
            if (IsDead) return;

            // 피격 정보를 캐싱해 두고(래그돌이 사망 시 사용), 나머지 다리 처리는 CombatAgent에 위임.
            if (ActionData != null)
            {
                ActionData.HitPoint = damageData.HitPoint;
                ActionData.HitNormal = damageData.HitNormal;
                ActionData.Attacker = damageData.Attacker;
                ActionData.DamageAmount = damageData.DamageAmount;
            }

            OnHit?.Invoke();

            if (Health != null)
                Health.ApplyDamage(damageData.DamageAmount);
        }

        protected virtual void HandleDeath()
        {
            IsDead = true;
            OnDeath?.Invoke();
        }
    }
}
