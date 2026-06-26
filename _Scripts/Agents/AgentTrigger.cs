using System;
using ModuleSystem;
using UnityEngine;

namespace Agents
{
    // 애니메이션 이벤트 → C# 이벤트 다리. 애니메이션 클립의 Animation Event가
    // AnimationEndTrigger / DamageCastTrigger를 호출하도록 연결한다.
    public class AgentTrigger : MonoBehaviour, IModule
    {
        public event Action OnAnimationEnd;
        public event Action OnDamageCast;
        public event Action OnStartAim;
        public event Action OnSpawnItem;

        public void Initialize(ModuleOwner owner)
        {
            //당장 할게 없다.
        }

        private void AnimationEndTrigger() => OnAnimationEnd?.Invoke();
        private void DamageCastTrigger() => OnDamageCast?.Invoke();
        private void StartAimTrigger() => OnStartAim?.Invoke();
        private void SpawnItemTrigger() => OnSpawnItem?.Invoke();
    }
}
