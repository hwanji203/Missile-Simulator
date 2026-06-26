using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;

namespace Enemies.BT.Actions
{
    // 예시의 MoveToNextPoint(웨이포인트 순찰)를 대체. 좀비는 자기 leash 원 안에서
    // 랜덤한 NavMesh 지점을 골라 이동한다(최소 이동거리 보장 → 제자리 안 맴돌게).
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "MoveToRandomLeashPoint", story: "[Enemy] move to random point in leash", category: "Action/Navigation", id: "a1f0c3d4e5b647889900aabbccddeeff")]
    public partial class MoveToRandomLeashPointAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<float> MinMoveDistance = new(2f);

        private const int SampleAttempts = 8;
        private INavMovement _navMovement;

        protected override Status OnStart()
        {
            if (Enemy.Value == null || Enemy.Value.NavMovement == null)
                return Status.Failure;

            _navMovement = Enemy.Value.NavMovement;
            Vector3 center = Enemy.Value.LeashCenter;
            float radius = Enemy.Value.LeashRadius;

            for (int i = 0; i < SampleAttempts; i++)
            {
                Vector2 rnd = UnityEngine.Random.insideUnitCircle * radius;
                Vector3 candidate = center + new Vector3(rnd.x, 0f, rnd.y);

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas)
                    && Vector3.Distance(hit.position, Enemy.Value.transform.position) >= MinMoveDistance.Value)
                {
                    _navMovement.SetDestination(hit.position);
                    return Status.Running;
                }
            }

            return Status.Failure; // 적당한 지점을 못 찾음 → MOVE 셀렉터의 fallback으로
        }

        protected override Status OnUpdate()
        {
            return _navMovement.IsArrived ? Status.Success : Status.Running;
        }
    }
}
