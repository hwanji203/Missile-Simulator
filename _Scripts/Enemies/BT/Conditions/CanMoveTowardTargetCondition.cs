using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;

namespace Enemies.BT.Conditions
{
    // 좀비가 타겟(플레이어)을 향해 "실제로 움직일 수 있는 거리"가 MinMoveDistance 이하이면 false.
    // ChaseWithinLeashAction과 동일하게 타겟 위치를 leash 원 안으로 클램프 + NavMesh에 스냅한 목적지를 구하고,
    // 현재 위치 → 그 목적지 거리를 본다. 이미 leash 가장자리에 붙어 더 다가갈 수 없을 때(추격 무의미) 걸러낸다.
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "CanMoveTowardTarget", story: "[Enemy] can move toward [TargetGameObject]", category: "Conditions", id: "a1e6c4b27f0d4e9a8c35b2f1d6e09a23")]
    public partial class CanMoveTowardTargetCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;

        [Tooltip("향해 갈 수 있는 거리가 이 값 이하이면 false(추격 무의미). ChaseWithinLeash와 같은 기준의 목적지로 계산.")]
        [SerializeReference] public BlackboardVariable<float> MinMoveDistance = new(0.5f);

        public override bool IsTrue()
        {
            if (Enemy.Value == null || TargetGameObject.Value == null)
                return false;

            // 여기서부터는 적이 경계 쪽(바깥쪽)으로 가려는 상황 → 실제로 더 갈 수 있는지 검사
            Vector3 destination = GetCircleIntersection(Enemy.Value, TargetGameObject.Value.transform.position);
            float movable = Vector3.Distance(Enemy.Value.transform.position, destination);  
            return movable > MinMoveDistance.Value;
        }

        private Vector3 GetCircleIntersection(AbstractEnemy enemy, Vector3 targetPos)
        {
            Vector3 center = enemy.LeashCenter;
            float radius = enemy.LeashRadius;

            Vector3 point = enemy.transform.position;
            point.y = 0f; center.y = 0f; targetPos.y = 0f;  // XZ 평면

            // 방향 (정규화)
            Vector3 direction = targetPos - point;
            float dLen = direction.magnitude;
            if (dLen < 0.0001f) return enemy.transform.position;  // 같은 위치
            direction /= dLen;

            // 레이-원 교차 (P + t*d 가 원 위에 있을 조건)
            Vector3 m = point - center;
            float b = Vector3.Dot(m, direction);
            float c = Vector3.Dot(m, m) - radius * radius;

            float disc = b * b - c;
            if (disc < 0f)
                return enemy.transform.position;  // 선이 원과 안 만남

            float sqrtDisc = Mathf.Sqrt(disc);

            // 가까운 교차점부터
            float t = -b - sqrtDisc;
            if (t < 0f) t = -b + sqrtDisc;   // 뒤쪽이면 먼 쪽
            if (t < 0f) return enemy.transform.position;

            Vector3 hitPoint = point + direction * t;     // 원 둘레 위의 교차점
            hitPoint.y = enemy.transform.position.y;
            return hitPoint;
        }
    }
}
