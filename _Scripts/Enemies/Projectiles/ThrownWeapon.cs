using System.Collections.Generic;
using CombatSystem;
using EventChannelSystem;
using HwanLib.GGMLib.SoundSystem;
using UnityEngine;

namespace Enemies.Projectiles
{
    // 던져지는 무기 투사체. 하나의 프리팹에 여러 무기 메쉬를 자식으로 두고,
    // 발사 때마다 랜덤으로 하나만 활성화한다(Saw/RoadSign/Rake/Pipe/Wrench… 확장 자유).
    // 플레이어 로켓에 맞으면 티어 비례 데미지를 준다.
    public class ThrownWeapon : ThrownProjectile
    {
        [Header("무기 비주얼(자식). 발사 때 랜덤 1개만 켠다.")]
        [SerializeField] private List<GameObject> weaponVisuals = new();

        [Header("플레이어 명중 시 튕김")]
        [Tooltip("반사 후 속도 배율(1=속도 유지, <1=충돌감 위해 감속).")]
        [SerializeField, Range(0f, 1.5f)] private float bounceSpeedMultiplier = 0.8f;

        [Header("Whiz 효과음")]
        [SerializeField] private EventChannelSO soundChannel;
        [SerializeField] private SoundClipSO whizSound;
        [SerializeField] private float whizDistance = 8f;

        private float _damage;
        private bool _whizPlayed;

        public override void ResetItem()
        {
            base.ResetItem();
            _whizPlayed = false;
            DisableAllVisuals();
        }

        protected override void Update()
        {
            base.Update();

            if (!IsActive || IsGrounded || _whizPlayed) return;

            if (Players.PlayerController.PlayerTransform != null && soundChannel != null && whizSound != null)
            {
                float dist = Vector3.Distance(transform.position, Players.PlayerController.PlayerTransform.position);
                if (dist <= whizDistance)
                {
                    _whizPlayed = true;
                    soundChannel.RaiseEvent(SoundSystemEvents.PlaySoundEvent.Init(transform.position, whizSound));
                }
            }
        }

        protected override void OnPrepared(in ProjectilePrepData data)
        {
            _damage = data.Damage;
            PickRandomVisual();
        }

        protected override bool OnHitTarget(IDamageable target, Vector3 hitPoint, Vector3 travelDir, Vector3 surfaceNormal)
        {
            target?.ApplyDamage(new DamageData
            {
                DamageAmount = _damage,
                HitPoint = hitPoint,
                HitNormal = travelDir, // 진행 방향 → 로켓 넉백/반응 방향
                Attacker = Attacker,
                IsCritical = false,
            });
            BounceOffTarget(surfaceNormal, bounceSpeedMultiplier); // 데미지 후 튕겨 날아감
            return false; // 즉시 반납 X — 튕겨서 지면에 떨어진 뒤 groundedLifetime 타이머로 반납
        }

        private void PickRandomVisual()
        {
            DisableAllVisuals();
            if (weaponVisuals.Count == 0) return;

            int index = Random.Range(0, weaponVisuals.Count);
            if (weaponVisuals[index] != null)
                weaponVisuals[index].SetActive(true);
        }

        private void DisableAllVisuals()
        {
            foreach (GameObject visual in weaponVisuals)
                if (visual != null) visual.SetActive(false);
        }
    }
}
