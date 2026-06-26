using CombatSystem;
using Enemies.BT;
using Enemies.Spawning;
using Events;
using EventChannelSystem;
using HwanLib.GGMLib.SoundSystem;
using UnityEngine;

namespace Enemies
{
    // 로켓 게임의 좀비 적 구체 클래스. 예시의 AbstractEnemy 구조(Agent + Nav + Sensor + SkillModule + BT)를
    // 그대로 따르되, 로켓 전용 요소를 얹는다:
    //  - 티어(소/중/대)로 크기↔체력↔리시 반경 일괄 세팅
    //  - 죽으면 래그돌(폭발 거리 반비례 충격) + 디졸브로 사라짐
    //  - 살아남으면 OnSurviveDamage로 AI에게 도망 트리거(서브시스템 3에서 BT가 소비)
    public class ZombieEnemy : AbstractEnemy
    {
        [Tooltip("스폰러가 티어를 주입하기 전, 단독 테스트용 기본 티어.")]
        [SerializeField] private ZombieTierSO defaultTier;

        [SerializeField] private EventChannelSO enemyChannel;

        [Tooltip("Visual 아래 Body 래퍼. 티어의 bodyY로 로컬 Y 위치를 맞춘다.")]
        [SerializeField] private Transform body;

        [Tooltip("크기가 클수록 애니메이션을 반비례(1/scale)로 느리게 한다. 이 값이 하한(너무 느려지지 않게). 1.0이 상한.")]
        [SerializeField, Range(0.1f, 1f)] private float minAnimSpeed = 0.6f;

        private ZombieTierSO _tier;
        private RagdollModule _ragdoll;

        // 프리팹이 인스펙터에서 설정한 NavMeshAgent 기본 정지 거리(티어 스케일 적용 전 원본).
        // ApplyTier가 이 값에 티어 스케일을 곱해 정지 거리를 키운다.
        private float _baseStoppingDistance;

        // 이동/체력 조회 프로퍼티(LeashCenter/LeashRadius/NormalizedHealth/LastDamageFraction)는
        // 이제 AbstractEnemy에 있다 — 모든 BT 노드가 AbstractEnemy 하나로 통일되도록.

        // 던지기 공격 파라미터(티어가 정한다). ThrowSkill이 발사 직전에 읽는다.
        public float ThrowCooldown { get; private set; } = 2.5f;
        public float ThrowDamage { get; private set; } = 10f;

        // 스포너가 주입하는 공격 제한 관리자(없으면 제한 없이 자유 공격 — 직접 배치/테스트용).
        public EnemyAttackCoordinator AttackCoordinator { get; set; }

        [Header("사운드")]
        [SerializeField] private EventChannelSO soundChannel;
        [SerializeField] private SoundClipSO zombieIdleSound;
        [SerializeField] private SoundClipSO zombieDeathSound;
        [SerializeField] private float idleSoundMinInterval = 5f;
        [SerializeField] private float idleSoundMaxInterval = 15f;

        private Coroutine _idleSoundCoroutine;

        private void OnEnable()
        {
            if (_idleSoundCoroutine != null)
            {
                StopCoroutine(_idleSoundCoroutine);
            }
            _idleSoundCoroutine = StartCoroutine(ZombieIdleSoundLoop());
        }

        private void OnDisable()
        {
            if (_idleSoundCoroutine != null)
            {
                StopCoroutine(_idleSoundCoroutine);
                _idleSoundCoroutine = null;
            }
        }

        private System.Collections.IEnumerator ZombieIdleSoundLoop()
        {
            while (!IsDead)
            {
                float delay = Random.Range(idleSoundMinInterval, idleSoundMaxInterval);
                yield return new WaitForSeconds(delay);

                if (IsDead) break;

                if (soundChannel != null && zombieIdleSound != null)
                {
                    soundChannel.RaiseEvent(SoundSystemEvents.PlaySoundEvent.Init(transform.position, zombieIdleSound));
                }
            }
        }

        protected override void InitializeComponents()
        {
            base.InitializeComponents();
            _ragdoll = GetModule<RagdollModule>();

            // 티어 스케일을 곱하기 전에 프리팹 원본 정지 거리를 한 번만 캐시한다
            // (티어를 다시 주입해도 _baseStoppingDistance가 변하지 않아 누적되지 않는다).
            if (NavMovement?.NavMeshAgent != null)
                _baseStoppingDistance = NavMovement.NavMeshAgent.stoppingDistance;
        }

        protected override void AfterInitComponents()
        {
            base.AfterInitComponents();
            ApplyTier();
        }

        // 스폰러(서브시스템 4)가 호출해 티어를 주입한다. 이미 초기화됐으면 즉시 반영.
        public void AssignTier(ZombieTierSO tier)
        {
            _tier = tier;
            ApplyTier();
        }

        private void ApplyTier()
        {
            ZombieTierSO tier = _tier != null ? _tier : defaultTier;
            if (tier == null) return;

            transform.localScale = Vector3.one * tier.scale;
            Health?.SetMaxHealth(tier.maxHealth);

            // 크기가 클수록 애니메이션을 반비례로 느리게(루트모션 보폭이 과하게 빨라 보이지 않게).
            // minAnimSpeed가 하한, 1.0이 상한 → 작은 적(scale<1)은 빨라지지 않는다.
            AgentRenderer?.SetSpeed(Mathf.Clamp(1f / tier.scale, minAnimSpeed, 1f));

            // 크기가 클수록 애니메이션(루트 모션) 보폭도 커져 목적지를 지나친다 →
            // NavMeshAgent 정지 거리도 티어 스케일에 비례해 키워 오버슈트/진동을 줄인다.
            if (NavMovement?.NavMeshAgent != null)
                NavMovement.NavMeshAgent.stoppingDistance = _baseStoppingDistance * tier.scale;

            // 티어별 Body 로컬 Y 위치(발이 바닥에 닿도록 높이 보정).
            if (body != null)
            {
                Vector3 bodyPos = body.localPosition;
                bodyPos.y = tier.bodyY;
                body.localPosition = bodyPos;
            }

            ThrowCooldown = tier.throwCooldown;
            ThrowDamage = tier.throwDamage;
            StopDistance = tier.detectRadius;
            DetectRadius = tier.detectRadius;

            // 리시 원: 중심 = 스폰 위치, 반경 = 티어값. (LeashCenter/LeashRadius는 AbstractEnemy 소유)
            SetLeash(transform.position, tier.leashRadius);
        }

        protected override void HandleDeath()
        {
            base.HandleDeath(); // IsDead = true, OnDeath 발사
            StateChannel?.SendEventMessage(EnemyState.DEATH);

            if (_idleSoundCoroutine != null)
            {
                StopCoroutine(_idleSoundCoroutine);
                _idleSoundCoroutine = null;
            }

            if (soundChannel != null && zombieDeathSound != null)
            {
                soundChannel.RaiseEvent(SoundSystemEvents.PlaySoundEvent.Init(transform.position, zombieDeathSound));
            }

            // 마지막 피격 정보(ActionData)로 래그돌 충격을 만든다. 폭발 거리 반비례 데미지라
            // 가까울수록 DamageAmount가 커져 더 멀리 날아간다.
            DamageData lastDamage = new DamageData
            {
                DamageAmount = ActionData != null ? ActionData.DamageAmount : 0f,
                HitPoint = ActionData != null ? ActionData.HitPoint : transform.position,
                HitNormal = ActionData != null ? ActionData.HitNormal : Vector3.up,
            };

            _ragdoll?.EnableRagdoll(lastDamage);

            ZombieTierSO activeTier = _tier != null ? _tier : defaultTier;
            enemyChannel?.RaiseEvent(EnemyEvents.ZombieKilledEvent.Init(activeTier));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(LeashCenter, LeashRadius);
        }

#if UNITY_EDITOR
        // 임시 테스트: 플레이 모드에서 컴포넌트 우클릭 → "TEST: 치명타로 죽이기".
        // 폭발/레이어/마스크 배선과 무관하게 피격→사망→래그돌만 격리해서 본다. 확인 후 지워도 됨.
        [ContextMenu("TEST: 치명타로 죽이기")]
        private void TestKill()
        {
            float lethal = Health != null && Health.MaxHealth > 0f ? Health.MaxHealth : 9999f;
            ApplyDamage(new DamageData
            {
                DamageAmount = lethal,
                HitPoint = transform.position + Vector3.up,           // 대략 몸통 높이
                HitNormal = (Vector3.up + transform.forward).normalized, // 위+앞으로 날아가게
            });
        }
#endif
    }
}
