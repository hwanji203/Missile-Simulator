using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;

namespace Enemies.BT.Actions
{
    // 예시의 ChaseToTarget을 leash 제약 버전으로. 타겟을 쫓되 목적지를 leash 원 안으로
    // 클램프 → 좀비가 자기 집 원을 절대 벗어나지 않는다(연쇄 어그로/연쇄 폭발 방지).
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "ChaseWithinLeash", story: "[Enemy] chase [TargetGameObject] within leash", category: "Action/Navigation", id: "b2e1d4c5f6a7488990aabbccddeeff01")]
    public partial class ChaseWithinLeashAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;

        private Vector3 _destination;
        private INavMovement _navMovement;

        protected override Status OnStart()
        {
            if (Enemy.Value == null || TargetGameObject.Value == null || Enemy.Value.NavMovement == null)
                return Status.Failure;
            
            _navMovement = Enemy.Value.NavMovement;
            _destination = GetCircleIntersection(Enemy.Value, TargetGameObject.Value.transform.position);
            _navMovement.SetDestination(_destination);
            return Status.Success;
        }

        private Vector3 GetCircleIntersection(AbstractEnemy enemy, Vector3 targetPos)
        {
            Vector3 center = enemy.LeashCenter;
            Vector3 enemyPos = Enemy.Value.transform.position;

            // XZ 평면 기준
            center.y = 0f;
            enemyPos.y = 0f;
            targetPos.y = 0f;

            float enemyDistFromCenter = Vector3.Distance(enemyPos, center);   // 적이 중심에서 떨어진 거리
            float targetDistFromCenter = Vector3.Distance(targetPos, center); // 타겟이 중심에서 떨어진 거리

            // 타겟이 적보다 중심에 가까움 = 적은 중심 쪽(안쪽)으로 가려는 중 → leash 못 벗어남 → 무조건 OK
            if (targetDistFromCenter <= enemyDistFromCenter)
                return targetPos;
                 
            center = enemy.LeashCenter;
            float radius = enemy.LeashRadius;

            Vector3 P = enemy.transform.position;
            P.y = 0f; center.y = 0f; targetPos.y = 0f;  // XZ 평면

            // 방향 (정규화)
            Vector3 d = targetPos - P;
            float dLen = d.magnitude;
            if (dLen < 0.0001f) return enemy.transform.position;  // 같은 위치
            d /= dLen;

            // 레이-원 교차 (P + t*d 가 원 위에 있을 조건)
            Vector3 m = P - center;
            float b = Vector3.Dot(m, d);
            float c = Vector3.Dot(m, m) - radius * radius;

            float disc = b * b - c;
            if (disc < 0f)
                return enemy.transform.position;  // 선이 원과 안 만남

            float sqrtDisc = Mathf.Sqrt(disc);

            // 가까운 교차점부터
            float t = -b - sqrtDisc;
            if (t < 0f) t = -b + sqrtDisc;   // 뒤쪽이면 먼 쪽
            if (t < 0f) return enemy.transform.position;

            Vector3 hitPoint = P + d * t;     // 원 둘레 위의 교차점
            hitPoint.y = enemy.transform.position.y;
            return hitPoint;
        }
    }
}
