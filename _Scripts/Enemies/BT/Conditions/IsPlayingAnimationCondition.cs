using System;
using AnimationSystem;
using Unity.Behavior;
using UnityEngine;

namespace Enemies.BT.Conditions
{
    // 좀비가 특정 애니메이션(AnimParamSO 해시)을 지금 재생 중인지. AnimationChannel/PlayClip으로 튼 모션을
    // BT에서 확인할 때 사용(예: 던지기 모션 재생 중인지 → 끝날 때까지 대기/중복 발동 방지).
    // CrossFade 전이 중이면 전이 대상도 재생 중으로 본다(IRenderer.IsPlayingClip 참고).
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "IsPlayingAnimation", story: "[Enemy] is playing [Clip]", category: "Conditions", id: "d4c8f3b25a0e4d7f9b16e3c4a7f2d018")]
    public partial class IsPlayingAnimationCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<AnimParamSO> Clip;
        [SerializeReference] public BlackboardVariable<int> Layer = new(0);

        public override bool IsTrue()
        {
            if (Enemy.Value == null || Clip.Value == null || Enemy.Value.AgentRenderer == null)
                return false;

            return Enemy.Value.AgentRenderer.IsPlayingClip(Clip.Value.ParamHash, Layer.Value);
        }
    }
}
