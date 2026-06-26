namespace CombatSystem
{
    public enum SkillType
    {
        // 스탯(공중 아이템)
        HpUp,
        ExplosionRange,
        ExplosionDamage,
        FuseSpareTime,
        // 스킬(날아가는 좀비)
        Grenade,
        Bounce,
        EnemySuck,
        SuperBoost
    }

    public enum UpgradeCategory { Stat, Skill }
}
