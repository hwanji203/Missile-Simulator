using System;
using ModuleSystem;
using UnityEngine;

namespace CombatSystem
{
    public class HealthModule : MonoBehaviour, IModule
    {
        public event Action OnDeath;
        public event Action OnHealthChanged;

        [SerializeField] private float maxHealth;
        [SerializeField] private float currentHealth;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float Normalized => maxHealth > 0f ? currentHealth / maxHealth : 0f;

        private float _baseMaxHealth;
        private ISkillBonusProvider _bonusProvider;

        public void Initialize(ModuleOwner owner)
        {
            _baseMaxHealth = maxHealth;
            _bonusProvider = owner.GetModule<ISkillBonusProvider>();
            if (_bonusProvider != null)
                _bonusProvider.OnBonusChanged += RefreshMaxHealth;
            currentHealth = maxHealth;
        }

        private void OnDestroy()
        {
            if (_bonusProvider != null)
                _bonusProvider.OnBonusChanged -= RefreshMaxHealth;
        }

        private void RefreshMaxHealth()
        {
            float prev = maxHealth;
            maxHealth = _baseMaxHealth + (_bonusProvider?.GetBonus(SkillType.HpUp) ?? 0f);
            float delta = maxHealth - prev;
            if (delta > 0f) currentHealth = Mathf.Min(currentHealth + delta, maxHealth);
            OnHealthChanged?.Invoke();
        }

        // 적 티어 주입처럼 런타임에 최대 체력을 바꿀 때 사용. 현재 체력도 가득 채운다.
        public void SetMaxHealth(float value)
        {
            _baseMaxHealth = value;
            RefreshMaxHealth();
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke();
        }

        public void ApplyDamage(float damageAmount)
        {
            currentHealth -= damageAmount;
            if (currentHealth <= 0)
            {
                currentHealth = 0;
                OnDeath?.Invoke();
            }
            OnHealthChanged?.Invoke();
        }
    }
}
