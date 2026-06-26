using System;
using AnimationSystem;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Enemies.BT.Actions
{
    // AnimationChannel 브랜치가 호출 — 전달된 AnimParamSO 상태를 CrossFade로 재생.
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "PlayClip", story: "[Enemy] play [clip] at [Layer] and [Position]", category: "Action/Animation", id: "7e1abd7b1dd164fa4543244e8c89bda9")]
    public partial class PlayClipAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractEnemy> Enemy;
        [SerializeReference] public BlackboardVariable<AnimParamSO> Clip;
        [SerializeReference] public BlackboardVariable<int> Layer;
        [SerializeReference] public BlackboardVariable<float> Position;

        [SerializeReference] public BlackboardVariable<float> CrossDuration = new(0.2f);

        protected override Status OnStart()
        {
            if (Enemy.Value == null || Enemy.Value.AgentRenderer == null || Clip.Value == null)
                return Status.Failure;

            Enemy.Value.AgentRenderer.PlayClip(Clip.Value.ParamHash,
                Position.Value, CrossDuration.Value, Layer.Value);

            return Status.Success;
        }
    }
}
