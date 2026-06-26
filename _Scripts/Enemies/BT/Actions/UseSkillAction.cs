using System;
using CombatSystem;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Enemies.BT.Actions
{
    // 스킬(좀비는 "던지기")을 발동하고 스킬이 끝날 때까지 Running. 던지기 스킬 본체는 서브시스템 2.
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "UseSkill", story: "[Enemy] use [SkillNumber] to [TargetGameObject]", category: "Action/Combat", id: "4efae18dde7778ee0947c8f5a9df2348")]
    public partial class UseSkillAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<int> SkillNumber;
        [SerializeReference] public BlackboardVariable<GameObject> TargetGameObject;
        
        private ISkillModule _skillModule;
        private bool _isSkillEnd;

        protected override Status OnStart()
        {
            if (Enemy.Value == null || SkillNumber.Value < 0 || Enemy.Value.SkillModule == null)
            {
                Debug.LogError("use skill의 기본 값이 설정되지 않았습니다.");
                return Status.Failure;
            }

            _skillModule = Enemy.Value.SkillModule;

            _isSkillEnd = false;
            _skillModule.OnCurrentSkillEnd += HandleSkillEnd;
            _skillModule.UseSkill(SkillNumber.Value, TargetGameObject.Value);
            return Status.Running;
        }

        private void HandleSkillEnd()
        {
            _skillModule.OnCurrentSkillEnd -= HandleSkillEnd;
            _isSkillEnd = true;
        }

        protected override Status OnUpdate()
        {
            // 공격 도중 위협(도망) 상황이 되면 진행 중인 스킬을 즉시 포기한다 — "다 때려치우고 도망".
            // 진입은 CanUseSkillCondition이 막지만, 이미 Running인 던지기는 여기서만 끊을 수 있다.
            // Success로 끝내면 OnEnd가 StopSkillIfNotFinished로 든 무기 반납·슬롯 해제까지 정리한다.
            if (IsThreatened())
                return Status.Success;

            return _isSkillEnd ? Status.Success : Status.Running; //Fail 아니다.
        }

        private bool IsThreatened()
        {
            return Enemy.Value != null
                   && Enemy.Value.GetVariable(BtVars.IsThreatened, out BlackboardVariable<bool> threatened)
                   && threatened.Value;
        }

        protected override void OnEnd()
        {
            if (_skillModule != null)
            {
                _skillModule.OnCurrentSkillEnd -= HandleSkillEnd;
                _skillModule.StopSkillIfNotFinished(); //공격으로 썼던 것들을 모두 Cleanup해라.
            }
        }
    }
}
