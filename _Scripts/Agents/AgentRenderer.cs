using ModuleSystem;
using UnityEngine;

namespace Agents
{
    [RequireComponent(typeof(Animator))]
    public class AgentRenderer : MonoBehaviour, IModule, IRenderer
    {
        public Animator Animator { get; private set; }
        protected ModuleOwner Owner;

        public virtual void Initialize(ModuleOwner owner)
        {
            Owner = owner;
            Animator = GetComponent<Animator>();
        }

        public void PlayClip(int clipHash, float normalizedTime, float crossFadeDuration, int layerIndex = 0)
        {
            //Play, CrossFade, CrossFadeFixedTime
            Animator.CrossFadeInFixedTime(clipHash, crossFadeDuration, layerIndex, normalizedTime);
        }

        public void SetSpeed(float speed)
        {
            if (Animator != null) Animator.speed = speed;
        }

        public bool IsPlayingClip(int clipHash, int layerIndex = 0)
        {
            if (Animator == null) return false;

            // 현재 상태가 그 클립이면 재생 중. (short/full path 둘 다 비교 — AnimParamSO 이름 규칙 무관하게)
            AnimatorStateInfo current = Animator.GetCurrentAnimatorStateInfo(layerIndex);
            if (current.shortNameHash == clipHash || current.fullPathHash == clipHash)
                return true;

            // CrossFade 전이 중이면 전이 '대상' 상태도 재생 중으로 본다(방금 튼 모션이 아직 전이 중일 수 있음).
            if (Animator.IsInTransition(layerIndex))
            {
                AnimatorStateInfo next = Animator.GetNextAnimatorStateInfo(layerIndex);
                if (next.shortNameHash == clipHash || next.fullPathHash == clipHash)
                    return true;
            }

            return false;
        }
    }
}
