using System;
using Unity.Behavior;
using UnityEngine;

namespace Enemies.BT.Conditions
{
    // 플레이어가 멈추거나 터져 위협받은 상태인가? EnemyThreatReceiver가 IsThreatened(Bool)를 켜고,
    // FleeFromTargetAction이 도망을 끝낼 때 끈다. FLEE 분기 진입 조건으로 쓴다.
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "IsThreatened", story: "[IsThreatened]", category: "Conditions", id: "a9f3b71c08e24d6e9c5fb2104e7d3a85")]
    public partial class IsThreatenedCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<bool> IsThreatened;

        public override bool IsTrue() => IsThreatened != null && IsThreatened.Value;
    }
}
