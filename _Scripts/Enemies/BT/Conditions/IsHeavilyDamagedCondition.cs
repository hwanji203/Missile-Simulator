using System;
using Unity.Behavior;
using UnityEngine;

namespace Enemies.BT.Conditions
{
    // 많이 다쳤나? 마지막 데미지 비율(LastDamageFraction = 데미지/최대체력)이 임계값 이상이면 true.
    // FLEE 상태에서 true → CROWL(기어서 도망), false → RUN(뛰어서 도망)으로 분기.
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "IsHeavilyDamaged", story: "[Enemy] is heavily damaged over [Threshold]", category: "Conditions", id: "d4c3f6a7b8c9401122ccddeeff010203")]
    public partial class IsHeavilyDamagedCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<float> Threshold = new(0.5f);

        public override bool IsTrue()
        {
            if (Enemy.Value == null) return false;
            return Enemy.Value.NormalizedHealth <= Threshold.Value;
        }
    }
}
