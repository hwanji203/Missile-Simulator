using System;
using CombatSystem;
using DG.Tweening;
using EventChannelSystem;
using Events;
using ModuleSystem;
using ObjectPool.Runtime;
using UnityEngine;

namespace Enemies.Projectiles
{
    // 던질 것을 손에 들릴 때 넘기는 데이터(스폰러 → 투사체). 발사 속도는 발사 시점에 직접 받는다.
    public readonly struct ProjectilePrepData
    {
        public readonly ModuleOwner Attacker;
        public readonly float Damage;
        public readonly float Scale;   // 좀비 티어 스케일 → 무기 크기

        public ProjectilePrepData(ModuleOwner attacker, float damage, float scale)
        {
            Attacker = attacker;
            Damage = damage;
            Scale = scale;
        }
    }

    // 좀비가 던지는 투사체의 추상 베이스(풀링 대상).
    //  1) 윈드업: HoldInHand로 손 본에 부착(kinematic·중력off·콜라이더off).
    //  2) 발사: LaunchFromHold로 분리 → 물리/콜라이더 켜고 속도 부여(포물선).
    //  3) 타깃(플레이어 로켓) 트리거 명중 → 데미지/파워업 + VFX.
    //     지면은 솔리드 콜라이더(자식 전용 레이어)가 OnCollisionEnter로 받아 튕기고 굴러 정착.
    //  4) 착지 후 groundedLifetime, 착지 전 maxLifetime(맵 밖 안전망)이 지나면 반납.
    [RequireComponent(typeof(Rigidbody))]
    public abstract class ThrownProjectile : AbstractMonoPoolable
    {
        [SerializeField] private EventChannelSO gameChannel;
        [SerializeField] protected LayerMask targetMask;        // 플레이어(로켓) 레이어
        [SerializeField] protected LayerMask obstacleMask;      // 지형/지면 레이어
        [SerializeField] protected float deadYPos = 0;          // y값이 이 값 이하일 때 반납
        [SerializeField] protected float groundedLifetime = 3f; // 착지 후 이 시간 뒤 반납
        [SerializeField] protected float spinSpeed = 540f;      // deg/sec, 텀블 연출

        [Header("명중 이펙트")]
        [SerializeField] protected EventChannelSO createChannel; // 풀링 VFX 채널
        [SerializeField] protected PoolItemSO hitVfx;            // 피격 이펙트(풀)

        [Header("손에 들기 / 비행 콜라이더")]
        [Tooltip("비행 중에만 켜지는 콜라이더(감지 트리거 + 바닥 솔리드). 손에 든 동안 꺼진다. 래그돌 본 콜라이더는 넣지 말 것.")]
        [SerializeField] protected Collider[] flightColliders;
        [SerializeField] protected Vector3 gripLocalPosition;   // 손 본 부착 시 로컬 위치
        [SerializeField] protected Vector3 gripLocalEuler;      // 손 본 부착 시 로컬 회전(오일러)

        [Header("소환 스케일")]
        [Tooltip("켜면 어떤 크기의 좀비 손에 들려도/던져진 뒤에도 월드 크기를 프리팹 원본으로 고정(티어 스케일 무시, 커지는 연출 없음). 끄면 0→목표로 DOScale.")]
        [SerializeField] protected bool fixedScale;
        [Tooltip("소환 시 0→목표 스케일로 커지는 시간(초).")]
        [SerializeField] protected float growDuration = 0.2f;

        [Header("비행 경로 라인")]
        [Tooltip("발사 순간 예측 포물선을 그려 둔다. 비우면 안 그린다. (월드 공간 LineRenderer)\n" +
                 "땅에 착지하거나 타깃에 명중하는 순간 지운다(비행 중에는 계속 보인다).")]
        [SerializeField] protected ThrowIndicator pathLine;

        public event Action<ThrownProjectile> OnReturnToPool;

        protected Rigidbody Rb;
        protected ModuleOwner Attacker;

        protected bool IsActive => _active;
        protected bool IsGrounded => _grounded;

        private bool _active;
        private bool _grounded;
        private bool _retired;          // 반납 1회 보장(비행 중/손에 든 상태 공통)
        private bool _targetDisabled;   // 한 번 튕긴 뒤 타깃(로켓) 재상호작용 차단(튕김 1회 제한)
        private float _groundedElapsed;
        private float _scale = 1f;      // 던질 때의 월드 스케일(부착 동안 보정)
        private Transform _homeParent;  // 풀 루트(Pool은 Push 때 reparent를 안 하므로 기억해 둔다)
        private RigidbodyInterpolation _flightInterpolation; // 비행 시 복원할 보간 모드(prefab 설정)
        private Tween _scaleTween;      // 소환 시 0→목표 커지는 연출(재사용 시 Kill)
        private Vector3 _originScale;

        protected virtual void Awake()
        {
            Rb = GetComponent<Rigidbody>();
            _homeParent = transform.parent;
            _flightInterpolation = Rb.interpolation;
            _originScale = transform.localScale;
        }

        // ── 풀 수명 ───────────────────────────────────────────────
        public override void ResetItem()
        {
            if (Rb == null) Rb = GetComponent<Rigidbody>();
            _scaleTween?.Kill(); // 이전 생애의 커지는 연출이 남아 있으면 정리
            _scaleTween = null;
            _active = _grounded = _retired = _targetDisabled = false;
            _groundedElapsed = 0f;
            pathLine?.Hide(); // 풀에서 다시 꺼낼 때 지난 경로 라인 제거(안전망)
            EnterHeldPhysics(); // 꺼내자마자 안전한 정지 상태로
            
            gameChannel.AddListener<GameEndedEvent>(ReturnToPool);
        }

        // ── 손에 들기 → 발사 ─────────────────────────────────────
        // 윈드업: 손 본에 부착(아직 발사 X).
        public virtual void HoldInHand(Transform anchor, in ProjectilePrepData data)
        {
            if (Rb == null) Rb = GetComponent<Rigidbody>();
            Attacker = data.Attacker;
            _scale = Mathf.Max(0.0001f, data.Scale);

            EnterHeldPhysics();
            SetFlightColliders(false);
            AttachTo(anchor);

            _active = _grounded = _retired = _targetDisabled = false;
            _groundedElapsed = 0f;

            OnPrepared(data);
        }

        // 발사: 손에서 분리(월드 위치 유지) → 물리/콜라이더 켜고 속도 부여.
        // flightTime은 라인이 그릴 예측 포물선의 길이(스킬의 고정 비행 시간)다 — 실제 발사 속도로 동결한다.
        public virtual void LaunchFromHold(Vector3 velocity, float flightTime)
        {
            DetachToHome();
            Rb.position = transform.position;
            // 감속 투사체(파워업 좀비)는 실제 비행시간이 maxLifetime보다 길 수 있다 → 공중에서 수명 반납되지 않게 보정.
            // 실제 비행 속도/중력은 서브클래스가 조정할 수 있다(예: 파워업 좀비 감속).
            // 속도를 m배·중력을 m²배 하면 "같은 공간 경로"를 1/m배 느리게 통과하므로,
            // 경로 라인은 원본 velocity/Physics.gravity로 그려도 실제 비행과 정확히 겹친다.
            BeginFlight(GetFlightVelocity(velocity));
            // 발사 위치 기준 예측 포물선을 1회 그려 고정한다(월드 공간이라 비행 중에도 제자리).
            // 라인은 비행 내내 보이다가 착지(LandOnGround)나 타깃 명중(OnTriggerEnter) 때 지운다.
            pathLine?.Show(transform.position, velocity, Physics.gravity, flightTime, deadYPos);
        }

        // 경로 라인에 쓰인 원본 속도를 실제 비행 속도로 변환하는 훅(기본 = 그대로).
        protected virtual Vector3 GetFlightVelocity(Vector3 velocity) => velocity;

        // 비행 중 적용할 중력 배율(기본 1배). 속도 m배 + 중력 m²배 = 같은 경로를 느리게.
        protected virtual float FlightGravityScale => 1f;

        // 솔버(m=1 기준)가 구한 비행 시간을 실제 비행 소요 시간으로 변환한다(기본 = 그대로).
        // 느려지면 1/m배 길어진다. 라인 클리어 타이밍·발사 측 예측 리드가 함께 쓴다.
        public virtual float GetFlightDuration(float flightTime) => flightTime;


        // 던지지 못하고 끝났을 때(좀비 경직/사망) 풀로 반납.
        public void ReturnHeldToPool()
        {
            DetachToHome();
            ReturnToPool();
        }

        // 서브클래스가 데미지·비주얼 등을 준비(부착 시점).
        protected virtual void OnPrepared(in ProjectilePrepData data) { }

        // ── 비행 / 명중 ──────────────────────────────────────────
        protected virtual void Update()
        {
            if (!_active) return;

            if (_grounded)
            {
                // 착지 후엔 groundedLifetime만 적용(maxLifetime 무시 — 이미 안착).
                if ((_groundedElapsed += Time.deltaTime) >= groundedLifetime) ReturnToPool();
                return;
            }
            pathLine?.TrimToPosition(transform.position); // 이미 지나친 구간 제거
            if (transform.position.y < deadYPos) ReturnToPool();
        }

        // 비행 중 중력 직접 적용. FlightGravityScale로 호의 곡률을 바꿔 라인 궤도와 정확히 일치시킨다.
        // (착지 후엔 내장 중력이 복원되므로 여기서 빠진다.)
        protected virtual void FixedUpdate()
        {
            if (!_active || _grounded) return;
            Rb.AddForce(Physics.gravity * FlightGravityScale, ForceMode.Acceleration);
        }

        // 트리거: 타깃(플레이어 로켓) 명중 → 데미지/파워업 + VFX.
        // 지면은 보통 솔리드 콜라이더가 OnCollisionEnter로 받지만, 솔리드 콜라이더 없이 트리거만
        // 가진 투사체(예: 파워업 좀비 — 강체 바운스 없이 착지 즉시 래그돌)는 여기서 지면도 감지한다.
        protected virtual void OnTriggerEnter(Collider other)
        {
            if (!_active || _grounded) return;
            int otherLayer = other.gameObject.layer;

            // 트리거로 지면을 감지하는 투사체: 솔리드 충돌이 없으니 트리거 겹침을 착지로 본다.
            if ((obstacleMask.value & (1 << otherLayer)) != 0)
            {
                LandOnGround();
                return;
            }

            if (_targetDisabled) return; // 이미 튕겼으면 로켓과 재상호작용 안 함
            if ((targetMask.value & (1 << otherLayer)) == 0) return;
            
            _targetDisabled = true;

            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Vector3 travelDir = Rb.linearVelocity.sqrMagnitude > 1e-4f
                ? Rb.linearVelocity.normalized
                : transform.forward;
            // 트리거라 진짜 충돌 법선이 없으므로 로켓 중심→무기 방향을 외향 표면 법선으로 근사.
            Vector3 surfaceNormal = transform.position - other.bounds.center;
            surfaceNormal = surfaceNormal.sqrMagnitude > 1e-6f ? surfaceNormal.normalized : -travelDir;

            pathLine?.Hide(); // 타깃에 닿는 순간 예측 경로 라인은 의미가 끝나므로 지운다.
            PlayHitVfx(hitPoint);
            IDamageable target = other.GetComponentInParent<IDamageable>();
            if (OnHitTarget(target, hitPoint, travelDir, surfaceNormal)) // 반납 여부는 서브클래스가 결정
                ReturnToPool();
        }

        // 솔리드 콜라이더가 지면에 충돌 → 착지 처리(PhysicMaterial로 튕기고 굴러 정착).
        protected virtual void OnCollisionEnter(Collision collision)
        {
            if (!_active || _grounded) return;
            if ((obstacleMask.value & (1 << collision.gameObject.layer)) == 0) return;

            LandOnGround();
        }

        // 지면에 닿았을 때 공통 처리: 착지 타이머 시작 + 경로 라인 제거 + 서브클래스 훅.
        // 솔리드(OnCollisionEnter)·트리거(OnTriggerEnter) 어느 경로로 들어와도 동일하게 처리한다.
        private void LandOnGround()
        {
            _grounded = true;
            _groundedElapsed = 0f;
            Rb.useGravity = true; // 비행용 직접 중력에서 내장 중력으로 복귀(정상 튕김/구르기)
            pathLine?.Hide();     // 땅에 떨어지는 순간 경로 라인을 끈다.
            OnHitObstacle();
        }

        // 타깃 명중 처리(무기 = 데미지 / 파워업 좀비 = 파워업 이벤트).
        // 반환 = 즉시 반납할지. 시체를 남기거나 튕겨낼 때는 false 후 직접 처리.
        protected abstract bool OnHitTarget(IDamageable target, Vector3 hitPoint, Vector3 travelDir, Vector3 surfaceNormal);

        // 지면에 처음 닿았을 때 훅(착지 사운드 등).
        protected virtual void OnHitObstacle() { }

        // 외향 표면 법선 기준으로 비행 속도를 반사해 튕겨낸다.
        // 로켓은 넉백 면역 + 감지가 트리거라 PhysX 반발이 없으므로 직접 반사한다.
        // 호출 후엔 타깃 재상호작용을 차단(튕김 1회 제한) → 중력으로 떨어져 지면 물리로 정착한다.
        protected void BounceOffTarget(Vector3 surfaceNormal, float speedMultiplier = 1f)
        {
            Rb.linearVelocity = Vector3.Reflect(Rb.linearVelocity, surfaceNormal) * speedMultiplier;
        }

        protected void PlayHitVfx(Vector3 point)
        {
            if (createChannel == null || hitVfx == null) return;
            createChannel.RaiseEvent(
                CreateEvents.ShowPoolingVfxEvent.InitData(hitVfx, point, Quaternion.identity));
        }
        
        private void ReturnToPool(GameEndedEvent _)
            => ReturnToPool();

        protected void ReturnToPool()
        {
            if (_retired) return;
            
            gameChannel.RemoveListener<GameEndedEvent>(ReturnToPool);
            _retired = true;
            _active = false;
            pathLine?.Hide(); // 착지·명중 없이 수명(maxLifetime)으로 반납될 때를 위한 안전망
            OnReturnToPool?.Invoke(this);
        }

        // ── 물리 / 부착 상태 헬퍼 ────────────────────────────────
        private void BeginFlight(Vector3 velocity)
        {
            Rb.isKinematic = false;
            // 비행 중에는 FixedUpdate에서 직접 중력을 적용한다(FlightGravityScale 배율 반영).
            // 착지하면 OnCollisionEnter에서 내장 중력으로 되돌려 정상 물리로 튕기고 굴러 정착한다.
            Rb.useGravity = false;
            Rb.interpolation = _flightInterpolation; // 손에 든 동안 껐던 보간을 비행용으로 복원
            Rb.linearVelocity = velocity;
            Rb.angularVelocity = UnityEngine.Random.onUnitSphere * (spinSpeed * Mathf.Deg2Rad);

            SetFlightColliders(true);
            _groundedElapsed = 0f;
            _grounded = _retired = _targetDisabled = false;
            _active = true;
        }

        // 손에 든/대기 상태의 정지 물리. 손 본 애니메이션에 끌려다닐 때 interpolation이 켜져 있으면
        // 렌더 위치가 한 박자 늦게 보간되어 흔들리므로 끈다(발사 시 BeginFlight가 복원).
        private void EnterHeldPhysics()
        {
            if (!Rb.isKinematic)
            {
                Rb.linearVelocity = Vector3.zero;
                Rb.angularVelocity = Vector3.zero;
            }
            Rb.isKinematic = true;
            Rb.useGravity = false;
            Rb.interpolation = RigidbodyInterpolation.None;
        }

        // 목표 "월드" 스케일. fixedScale이면 티어 스케일(_scale) 무시하고 프리팹 원본 크기 고정.
        private Vector3 DesiredWorldScale => fixedScale ? _originScale : Vector3.one * _scale;

        private void AttachTo(Transform anchor)
        {
            if (anchor == null)
            {
                ApplySpawnScale(DesiredWorldScale);
                return;
            }

            transform.SetParent(anchor, false);
            transform.localPosition = gripLocalPosition;
            transform.localRotation = Quaternion.Euler(gripLocalEuler);
            // 손 본 계층 스케일을 나눠 월드 크기가 목표값이 되도록.
            ApplySpawnScale(DesiredWorldScale / Mathf.Max(0.0001f, anchor.lossyScale.x));
        }

        // 소환 시 스케일 적용: fixedScale이면 즉시 고정(커지는 연출 없음), 아니면 0→목표로 DOScale.
        private void ApplySpawnScale(Vector3 target)
        {
            _scaleTween?.Kill();
            _scaleTween = null;

            if (growDuration <= 0f)
            {
                transform.localScale = target;
                return;
            }

            transform.localScale = Vector3.zero;
            _scaleTween = transform.DOScale(target, growDuration).SetEase(Ease.OutBack);
        }

        private void DetachToHome()
        {
            _scaleTween?.Kill(); // 커지는 도중 발사/반납되면 연출 중단(목표 스케일로 고정).
            _scaleTween = null;
            transform.SetParent(_homeParent, true); // 월드 위치 유지한 채 분리
            // 발사 후에도 목표 월드 크기 유지(fixedScale이면 원본 크기 그대로 — 기존엔 티어 스케일로 되돌리는 버그).
            float homeScale = _homeParent != null ? Mathf.Max(0.0001f, _homeParent.lossyScale.x) : 1f;
            transform.localScale = DesiredWorldScale / homeScale;
        }

        private void SetFlightColliders(bool on)
        {
            if (flightColliders == null) return;
            foreach (Collider c in flightColliders)
                if (c != null) c.enabled = on;
        }
    }
}
