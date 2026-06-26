using ModuleSystem;
using UnityEngine;

namespace CombatSystem
{
    public class ActionDataModule : MonoBehaviour, IModule
    {
        public Vector3 HitPoint { get; set; }
        public Vector3 HitNormal { get; set; }
        public ModuleOwner Attacker { get; set; }

        // 마지막으로 받은 데미지량. 죽을 때 래그돌 런치 세기(거리 반비례) 계산에 쓴다.
        public float DamageAmount { get; set; }

        public void Initialize(ModuleOwner owner)
        {
        }
    }
}