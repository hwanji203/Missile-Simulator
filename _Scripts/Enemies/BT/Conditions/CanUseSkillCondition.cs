using System;
using Unity.Behavior;
using UnityEngine;

namespace Enemies.BT.Conditions
{
    // 스킬(던지기)을 쓸 수 있나? — 쿨다운 + 사거리 검사는 스킬 본체(CanUseSkill)가 한다.
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "CanUseSkill", story: "[Enemy] can use [SkillNumber] to [TargetGameObject]", category: "Conditions", id: "45cb4e4a706f68612b56d89c500cc575")]
    public partial class CanUseSkillCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<int> SkillNumber;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;

        public override bool IsTrue()
        {
            if (Enemy.Value == null || SkillNumber.Value < 0 || TargetGameObject.Value == null)
            {
                Debug.LogError("Can use skill 의 컨디션 조건이 잘못되었습니다.");
                return false;
            }

            // 위협받아 도망 중(IsThreatened)이면 공격하지 않는다. 도망 타겟과 공격 타겟이 같은
            // TargetGameObject를 공유하므로, 가드가 없으면 도망 중 던지기가 튀어나와 멈춰버린다.
            if (Enemy.Value.GetVariable(BtVars.IsThreatened, out BlackboardVariable<bool> threatened)
                && threatened.Value)
                return false;

            return Enemy.Value.SkillModule.CanUseSkill(SkillNumber.Value, TargetGameObject.Value);
        }
    }
}
