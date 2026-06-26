using AnimationSystem;
using CombatSystem;
using EventChannelSystem;
using Events;
using UnityEngine;

namespace Enemies.Projectiles
{
    // 가끔 무기 대신 던져지는 "파워업 좀비" 투사체. 미사일이 맞추면 데미지 대신 파워업 이벤트를 발행한다.
    // (구 링 시스템을 대체하는 보상. 실제 파워업 효과 연결은 추후 — 지금은 이벤트만.)
    // 추가로, 로켓에 맞으면 시체를 래그돌로 전환해 잠시 날아가다 풀로 반납한다.
    // 로켓을 못 맞히고 땅에 떨어져도 그 자리에서 래그돌로 무너진다(작은 힘으로 크럼플).
    public class ThrownPowerupZombie : ThrownProjectile
    {
        [SerializeField] private AnimParamSO fallingAnim;
        [SerializeField] private EventChannelSO enemyChannel;
        [SerializeField] private Animator animator;

        [Header("느린 비행(요격용)")]
        [Tooltip("던져진 속도에 곱하는 비율(<1이면 느리게 날아가 플레이어가 요격하기 쉬움). " +
                 "중력도 함께 보정해 궤도(라인) 모양은 그대로 두고 통과 속도만 느려진다 — 같은 호를 슬로모션으로 날아간다.")]
        [SerializeField, Range(0.1f, 1f)] private float launchSpeedMultiplier = 0.5f;

        [Header("로켓 명중 시 래그돌")]
        [Tooltip("시체를 띄우는 힘. RagdollModule의 데미지 비례 발사 속도에 그대로 들어간다.")]
        [SerializeField] private float corpseLaunchForce = 25f;
        [Tooltip("래그돌 시체를 풀로 반납하기까지의 시간(초). 로켓 명중·땅 착지 래그돌 공통.")]
        [SerializeField] private float corpseDespawnDelay = 4f;

        [Header("땅 착지 시 래그돌")]
        [Tooltip("로켓을 못 맞히고 땅에 떨어졌을 때 무너지는 힘. 작게 줘서 튀지 않고 그 자리에 크럼플된다.")]
        [SerializeField] private float groundCorpseForce = 4f;

        private RagdollModule _ragdoll;
        private bool _ragdolled;
        private float _corpseElapsed;

        protected override void Awake()
        {
            base.Awake();

            // ModuleOwner 없이 래그돌 모듈을 단편적으로만 사용한다(앵커 = 이 투사체 루트).
            _ragdoll = GetComponentInChildren<RagdollModule>();
            _ragdoll?.Initialize(this);

            animator.Play(fallingAnim.ParamHash);
        }

        // 풀에서 다시 꺼낼 때: 래그돌을 비행 상태로 되돌리고 낙하 애니메이션을 재생한다.
        public override void ResetItem()
        {
            _ragdoll?.ResetRagdoll(); // 메인 Rb를 비키네매틱으로 먼저 복구한 뒤
            base.ResetItem();         // base가 속도/플래그를 초기화한다

            _ragdolled = false;
            _corpseElapsed = 0f;
            if (animator != null) animator.CrossFade(fallingAnim.ParamHash, 0);
        }

        private float Multiplier => Mathf.Clamp(launchSpeedMultiplier, 0.1f, 1f);

        // 파워업 좀비는 "같은 궤도를 그대로, 더 느리게" 날아간다.
        // 속도 m배 + 중력 m²배 → 공간 경로는 라인과 완전히 동일하고 통과 시간만 1/m배 길어진다.
        protected override Vector3 GetFlightVelocity(Vector3 velocity) => velocity * Multiplier;
        protected override float FlightGravityScale => Multiplier * Multiplier;

        protected override bool OnHitTarget(IDamageable target, Vector3 hitPoint, Vector3 travelDir, Vector3 surfaceNormal)
        {
            if (enemyChannel != null)
            {
                ZombiePowerUpEvent evt = EnemyEvents.ZombiePowerUpEvent.InitData(hitPoint);
                enemyChannel.RaiseEvent(evt);
            }

            if (_ragdoll == null) return true; // 래그돌 리그가 없으면 기존처럼 즉시 반납

            // 로켓 표면 법선으로 반사한 방향으로 시체를 띄운다(튕김). 세기는 corpseLaunchForce 고정.
            // 반납은 미루고 corpseDespawnDelay 뒤에 직접 반납한다.
            Vector3 bounceDir = Vector3.Reflect(travelDir, surfaceNormal);
            BeginCorpse(bounceDir, corpseLaunchForce, hitPoint);
            return false;
        }

        // 로켓을 못 맞히고 땅에 떨어졌을 때: 그 자리에서 래그돌로 무너진다(작은 힘으로 크럼플).
        // base.OnTriggerEnter/OnCollisionEnter의 LandOnGround가 _grounded를 켜고 이 훅을 부른다.
        protected override void OnHitObstacle()
        {
            if (_ragdoll == null || _ragdolled) return;

            // 떨어지던 방향으로 살짝(속도가 없으면 아래로). EnableRagdoll이 upwardBias를 더해 자연스럽게 무너진다.
            Vector3 dir = Rb.linearVelocity.sqrMagnitude > 1e-4f
                ? Rb.linearVelocity.normalized
                : Vector3.down;
            BeginCorpse(dir, groundCorpseForce, transform.position);
        }

        // 시체(래그돌) 전환 공통 처리: 본을 깨워 발사하고 corpseDespawnDelay 타이머를 시작한다.
        private void BeginCorpse(Vector3 direction, float force, Vector3 point)
        {
            _ragdolled = true;
            _corpseElapsed = 0f;
            _ragdoll.EnableRagdoll(direction, force, point);
        }

        protected override void Update()
        {
            if (_ragdolled)
            {
                _corpseElapsed += Time.deltaTime;
                if (_corpseElapsed >= corpseDespawnDelay)
                    ReturnToPool();
                return;
            }

            base.Update();
        }
    }
}
