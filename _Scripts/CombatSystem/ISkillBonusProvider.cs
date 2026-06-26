using System;

namespace CombatSystem
{
    public interface ISkillBonusProvider
    {
        event Action OnBonusChanged;
        float GetBonus(SkillType type);
    }
}
