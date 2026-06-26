using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Enemies.BT.Actions
{
    // 타겟을 비운다. 미사일이 지나가 버려 COMBAT을 벗어날 때 호출 → 다음에 다시 FindTarget으로 재탐지.
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "ClearTarget", story: "Clear [TargetGameObject]", category: "Action/Combat", id: "f6e5b8c9dadb403344eeff0102030405")]
    public partial class ClearTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;

        protected override Status OnStart()
        {
            TargetGameObject.Value = null;
            return Status.Success;
        }
    }
}
