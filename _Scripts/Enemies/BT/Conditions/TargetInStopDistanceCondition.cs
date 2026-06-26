using System;
using Unity.Behavior;
using UnityEngine;

namespace Enemies.BT.Conditions
{
    // 타겟이 정지(=던지기 준비) 거리 안에 들어왔나? StopDistance를 던지기 사거리로 쓴다.
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "TargetInStopDistance", story: "[Enemy] check [TargetGameObject] in stopDistance", category: "Conditions", id: "a6c81d2ca3b780f08d9d429bc07cbf99")]
    public partial class TargetInStopDistanceCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;

        public override bool IsTrue()
        {
            if (Enemy.Value == null || TargetGameObject.Value == null)
            {
                Debug.LogError("condition에 Enemy 또는 TargetGameObject가 할당되지 않았습니다. 항상 false반환");
                return false;
            }

            float stopDistance = Enemy.Value.StopDistance;
            float targetDistance = Vector3.Distance(Enemy.Value.transform.position, TargetGameObject.Value.transform.position);

            return targetDistance <= stopDistance;
        }
    }
}
