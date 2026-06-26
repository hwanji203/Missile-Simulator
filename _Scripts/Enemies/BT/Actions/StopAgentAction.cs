using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Enemies.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "StopAgent", story: "Stop [Enemy]", category: "Action/Navigation", id: "03ebf02a3d03e440a55e7bb861d1cd3f")]
    public partial class StopAgentAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;

        protected override Status OnStart()
        {
            // 시작할 때 한 번 시도하고, 결과를 그대로 OnUpdate 로직에 맡김
            return TryStop();
        }

        protected override Status OnUpdate()
        {
            // 아직 준비가 안 됐으면 매 프레임 다시 시도함
            return TryStop();
        }

        // 멈추기를 시도하는 공통 함수 (한 군데로 합침)
        private Status TryStop()
        {
            // Enemy 또는 이동 컴포넌트가 아직 없으면 → 계속 시도(Running)
            if (Enemy.Value == null || Enemy.Value.NavMovement == null)
                return Status.Running;

            // 준비가 됐으면 멈추고 성공 반환
            Enemy.Value.NavMovement.StopImmediately();
            return Status.Success;
        }
    }
}