using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;

namespace Enemies.BT.Actions
{
    // 타겟 반대 방향으로 도망쳐 감지 범위 밖으로 빠져나간다. 목숨 걸고 도망이라 leash는 무시.
    // FLEE 상태에서 CROWL/RUN 애니메이션과 함께 호출.
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "FleeFromTarget", story: "[Enemy] flee from [TargetGameObject]", category: "Action/Navigation", id: "c3d2e5f6a7b8490011bbccddeeff0102")]
    public partial class FleeFromTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;
        [SerializeReference] public BlackboardVariable<float> EndDistance = new(1000f);
        // 현재 목적지 방향 vs 이상적 도망 방향의 허용 편차 (도). 초과 시 목적지 재계산.
        [SerializeReference] public BlackboardVariable<float> PathDeviation = new(45f);

        private INavMovement _navMovement;
        private Vector3 _fleeDestination;

        protected override Status OnStart()
        {
            if (Enemy.Value == null || TargetGameObject.Value == null || Enemy.Value.NavMovement == null)
                return Status.Failure;

            _navMovement = Enemy.Value.NavMovement;
            return TrySetFarthestFleeDestination() ? Status.Running : Status.Failure;
        }

        protected override Status OnUpdate()
        {
            if (TargetGameObject.Value == null)
                return Status.Success; // 도망칠 대상이 사라짐

            float dist = Vector3.Distance(Enemy.Value.transform.position, TargetGameObject.Value.transform.position);
            if (dist > EndDistance.Value)
                return Status.Success; // 충분히 멀어짐

            if (_navMovement.IsArrived)
                return Status.Success;

            // 플레이어 이동으로 도망 방향이 크게 틀어졌을 때만 재계산
            if (IsPathDiverged())
                TrySetFarthestFleeDestination();

            return Status.Running;
        }

        private bool TrySetFarthestFleeDestination()
        {
            Vector3 dir = Enemy.Value.transform.position - TargetGameObject.Value.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
                dir = Enemy.Value.transform.forward;
            dir.Normalize();

            Vector3 farTarget = Enemy.Value.transform.position + dir * EndDistance.Value;
            if (!NavMesh.SamplePosition(farTarget, out NavMeshHit hit, EndDistance.Value, NavMesh.AllAreas))
                return false;

            _fleeDestination = hit.position;
            _navMovement.SetDestination(_fleeDestination);
            return true;
        }

        // 현재 목적지 방향과 이상적 도망 방향의 각도 편차 확인
        private bool IsPathDiverged()
        {
            Vector3 toDestDir = _fleeDestination - Enemy.Value.transform.position;
            toDestDir.y = 0f;
            if (toDestDir.sqrMagnitude < 1f)
                return true; // 목적지에 거의 도달

            Vector3 idealDir = Enemy.Value.transform.position - TargetGameObject.Value.transform.position;
            idealDir.y = 0f;
            if (idealDir.sqrMagnitude < 0.0001f)
                return false;

            return Vector3.Angle(idealDir.normalized, toDestDir.normalized) > PathDeviation.Value;
        }
    }
}
