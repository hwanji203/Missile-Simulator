using System;
using Agents;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Enemies.BT.Actions
{
    // 애니메이션이 끝났다는 신호(AgentTrigger.OnAnimationEnd — 클립의 Animation Event)가 올 때까지 대기.
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "WaitForAnimation", story: "[Enemy] wait for animation", category: "Action/Animation", id: "f437fca2c175bc64dc335a47942fb03e")]
    public partial class WaitForAnimationAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;

        private Agents.AgentTrigger _agentTrigger;
        private bool _isAnimationEnd;

        protected override Status OnStart()
        {
            if (Enemy.Value == null || Enemy.Value.Trigger == null)
                return Status.Failure;

            _isAnimationEnd = false;
            _agentTrigger = Enemy.Value.Trigger;
            _agentTrigger.OnAnimationEnd += HandleAnimationEnd;
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            return _isAnimationEnd ? Status.Success : Status.Running;
        }

        protected override void OnEnd()
        {
            if (_agentTrigger != null)
                _agentTrigger.OnAnimationEnd -= HandleAnimationEnd;
        }

        private void HandleAnimationEnd() => _isAnimationEnd = true;
    }
}
