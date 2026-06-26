using System;
using Unity.Behavior;
using UnityEngine;

namespace Enemies.BT.Conditions
{
    // 좀비가 자기 leash(집) 원의 가장자리에 닿았는지. 추격이 leash에 막혀 더 못 나가는 지점 감지 등에 쓴다.
    // 수평(XZ) 거리로만 판정한다(지형 높낮이 무시) — ChaseWithinLeash의 클램프와 동일 기준.
    // 거리 >= 반경 * EdgeThreshold 이면 "가장자리"로 본다.
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "AtLeashEdge", story: "[Enemy] is at leash edge", category: "Conditions", id: "c3b7e2a14f9d4c6e8a05d2b3f6e1c907")]
    public partial class AtLeashEdgeCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;

        [Tooltip("반경의 몇 비율(0~1) 이상이면 '가장자리'로 볼지. 0.9 = 반경의 90% 지점부터 가장자리로 간주.")]
        [SerializeReference] public BlackboardVariable<float> EdgeThreshold = new(0.8f);

        public override bool IsTrue()
        {
            if (Enemy.Value == null)
            {
                Debug.LogError("AtLeashEdge: Enemy가 할당되지 않았습니다. false 반환.");
                return false;
            }

            float radius = Enemy.Value.LeashRadius;
            if (radius <= 0f) return false; // 티어 미주입 등 — 가장자리 판정 불가

            Vector3 offset = Enemy.Value.transform.position - Enemy.Value.LeashCenter;
            offset.y = 0f; // 수평 거리(지형 높낮이 무시)

            float edge = radius * Mathf.Clamp01(EdgeThreshold.Value);
            return offset.sqrMagnitude >= edge * edge;
            
            
        }
    }
}
