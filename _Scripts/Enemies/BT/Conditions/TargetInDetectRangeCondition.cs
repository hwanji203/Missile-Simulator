using System;
using Unity.Behavior;
using UnityEngine;

namespace Enemies.BT.Conditions
{
    // 타겟이 아직 감지 범위 안에 있나? COMBAT을 빠져나갈지(미사일이 지나가 버렸는지) 판단할 때 사용.
    // false면 좀비가 타겟을 놓친 것 → IDLE로 복귀.
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "TargetInDetectRange", story: "[Enemy] check [TargetGameObject] in detectRange", category: "Conditions", id: "e5d4a7b8c9da402233ddeeff01020304")]
    public partial class TargetInDetectRangeCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;

        public override bool IsTrue()
        {
            if (Enemy.Value == null || TargetGameObject.Value == null)
                return false;

            float dist = Vector3.Distance(Enemy.Value.transform.position, TargetGameObject.Value.transform.position);
            return dist <= Enemy.Value.DetectRadius;
        }
    }
}
