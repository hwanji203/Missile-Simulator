using System;
using CombatSystem;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Enemies.BT.Actions
{
    // 피격 처리: 마지막 공격자(ActionData.Attacker)를 타겟으로 잡고 그쪽을 바라본다.
    // HIT 상태 진입 시 호출 → 이후 도망(FLEE)에서 "공격자 반대 방향"을 계산할 수 있게 한다.
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "ProcessHit", story: "[Enemy] process hit from [TargetGameObject]", category: "Action/Combat", id: "6f43ca9dbb0d3cb93810c77be4d7b30d")]
    public partial class ProcessHitAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;

        protected override Status OnStart()
        {
            if (Enemy.Value == null || Enemy.Value.ActionData == null)
                return Status.Failure;

            ActionDataModule actionData = Enemy.Value.ActionData;
            if (actionData.Attacker == null)
                return Status.Failure; // 폭발 등 공격자 없는 피해면 ProcessHit는 건너뛴다.

            TargetGameObject.Value = actionData.Attacker.gameObject;

            RotateToTarget();
            return Status.Success;
        }

        private void RotateToTarget()
        {
            Vector3 direction = (TargetGameObject.Value.transform.position - Enemy.Value.transform.position);
            direction.y = 0;
            if (direction.sqrMagnitude < 0.0001f) return;
            Enemy.Value.transform.rotation = Quaternion.LookRotation(direction.normalized);
        }

        protected override Status OnUpdate() => Status.Success;
    }
}
