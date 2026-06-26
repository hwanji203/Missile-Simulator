using System;
using CombatSystem;
using Events;
using EventChannelSystem;
using HwanLib.GGMLib.SoundSystem;
using ObjectPool.Runtime;
using Players.Skills;
using UnityEngine;

namespace Players
{
    // 발동 시: 주변 범위(AoE) 데미지 + 폭발 이벤트 발행.
    // 발동 시점(타이머/접촉 조건)과 이동 정지는 MissileFuse가 담당한다.
    public class ExplosionSkill : MonoBehaviour, ISkill
    {
        public event Action OnSkillEnd;
        [field: SerializeField] public SkillDataSO SkillData { get; private set; }
        [SerializeField] private PoolItemSO explosionPoolItem;

        [Header("스킬 훅(옵션)")]
        [Tooltip("폭발 직전 흡입. 비우면 흡입 없이 즉시 폭발.")]
        [SerializeField] private SuckEffect suckEffect;
        [Tooltip("폭발 직후 점프 재폭발. 비우면 연쇄 없음.")]
        [SerializeField] private BounceEffect bounceEffect;

        [Header("사운드")]
        [SerializeField] private EventChannelSO soundChannel;
        [SerializeField] private SoundClipSO explosionSound;

        private MissileSkillModule _skillModule;
        private AbstractDamageCaster _damageCaster;
        private float _lastUsingTime;

        public bool IsUsing { get; private set; }

        public float NormalizedCooldown
            => Mathf.Approximately(SkillData.cooldown, 0f)
                ? 1f
                : Mathf.Clamp01((Time.time - _lastUsingTime) / SkillData.cooldown);

        public void InitializeSkill(ISkillModule skillModule)
        {
            _skillModule = skillModule as MissileSkillModule;
            Debug.Assert(_skillModule != null, "ExplosionSkill은 MissileSkillModule의 자식이어야 합니다.");

            _damageCaster = GetComponentInChildren<AbstractDamageCaster>();
            Debug.Assert(_damageCaster != null, $"데미지 캐스터가 있어야 데미지를 줄 수 있습니다. : {gameObject}");
            _damageCaster.InitCaster(skillModule.Owner); //딜러(오너)를 캐스터에 주입

            suckEffect?.Init(skillModule.Owner);
            // _damageCaster는 AbstractDamageCaster로 선언돼 있으나 미사일 폭발 캐스터는 ExplosionDamageCaster다.
            // playerChannel: 각 바운스 착지 폭발이 PlayerExplodedEvent를 발행해 종료 타이머 리셋 + 카메라 흔들림.
            bounceEffect?.Init(skillModule.Owner, _damageCaster as ExplosionDamageCaster,
                _skillModule.CreateChannel, _skillModule.PlayerChannel);

            IsUsing = false;
        }

        public bool CanUseSkill(GameObject target = null) => NormalizedCooldown >= 1f && !IsUsing;

        public void UseSkill(GameObject target = null)
        {
            IsUsing = true;
            Vector3 center = transform.position;

            // 폭발 직전 흡입(레벨0이거나 훅 없으면 즉시 콜백). 흡입이 끝난 뒤 실제 폭발한다.
            if (suckEffect != null)
                suckEffect.RunBeforeExplode(center, DoExplode);
            else
                DoExplode();
        }

        // 실제 폭발 본체: 메인 범위 데미지 + 상태성 이벤트(1회) + VFX. (Bounce 훅은 Task 4에서 연결)
        private void DoExplode()
        {
            // 폭발 반경 배수: 파티클 확대·카메라 후퇴가 공유. 캐스터가 ExplosionDamageCaster가 아니면 1.
            float scale = (_damageCaster as ExplosionDamageCaster)?.ExplosionScale ?? 1f;

            if (soundChannel != null && explosionSound != null)
            {
                soundChannel.RaiseEvent(SoundSystemEvents.PlaySoundEvent.Init(transform.position, explosionSound));
            }

            // 1) 폭발 위치 기준 범위 데미지
            _damageCaster.CastDamage(transform.position, transform.forward, SkillData);

            // 2) 폭발 이벤트 발행 (플레이어 채널) — 상태성 이벤트는 메인 폭발에서만 1회. scale로 카메라 후퇴 강화.
            if (_skillModule.PlayerChannel != null)
            {
                PlayerExplodedEvent explodedEvent =
                    PlayerEvents.PlayerExplodedEvent.InitData(transform.position, gameObject, scale);
                _skillModule.PlayerChannel.RaiseEvent(explodedEvent);
            }

            // 3) 폭발 VFX는 플레이어 종속이 아닌 풀(월드 공간)에서 꺼내 재생한다. (생성 채널)
            //    파티클 크기를 폭발 반경 배수(scale)만큼 키운다.
            if (_skillModule.CreateChannel != null)
            {
                Vector3 explosionPos = transform.position;
                ShowPoolingVfxEvent vfxEvent =
                    CreateEvents.ShowPoolingVfxEvent.InitData(explosionPoolItem, explosionPos, Quaternion.identity, scale);
                _skillModule.CreateChannel.RaiseEvent(vfxEvent);
            }

            // 4) 폭발 직후 연쇄(레벨0이거나 훅 없으면 무동작). 추가 폭발은 상태성 이벤트를 내지 않는다.
            bounceEffect?.RunAfterExplode(transform.position);

            StopSkill();
        }

        public void StopSkill()
        {
            IsUsing = false;
            _lastUsingTime = Time.time;
            OnSkillEnd?.Invoke();
        }
    }
}
