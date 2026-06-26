using System.Collections;
using AnimationSystem;
using CombatSystem;
using Enemies.Projectiles;
using Enemies.Spawning;
using EventChannelSystem;
using Events;
using HwanLib.GGMLib.SoundSystem;
using ObjectPool.Runtime;
using UnityEngine;

namespace Enemies.EnemySkills
{
    // 좀비의 "무기 던지기" 공격. BT의 UseSkillAction이 발동한다.
    //  1) UseSkill: 던지기 애니메이션 재생 + 윈드업 동안 LineRenderer 포물선 미리보기(예측 리드 조준).
    //  2) 애니메이션 이벤트 DamageCast 시점: 투사체를 발사(이벤트 채널 경유 풀 스폰).
    //  3) 애니메이션 이벤트 AnimationEnd 시점: StopSkill → 쿨다운 시작, BT에 종료 통보.
    // 쿨다운/데미지는 좀비 티어가 정한다(ZombieEnemy.ThrowCooldown/ThrowDamage).
    public class ThrowSkill : AbstractEnemySkill
    {
        [Header("발사")]
        [Tooltip("던지는 원점(손 본 등). 비우면 좀비 루트.")]
        [SerializeField] private Transform throwOrigin;
        [Tooltip("무기 비행 시간 N(초). 던진 뒤 정확히 이 시간에 예측 위치를 통과한다. 작을수록 빠르고 평평, 클수록 느리고 높은 호.")]
        [SerializeField] private float flightTime = 0.8f;
        // [Tooltip("타깃 리드 예측에 쓰는 최대 시간(초). 감속 투사체(파워업 좀비)는 실제 비행시간이 몇 배로 길어지는데, " +
        //          "그 시간을 전부 리드하면 조준점이 멀어져 감속이 상쇄된다(거리/시간이 같이 늘어 체감 속도 그대로). 리드를 이 값에서 자른다.")]
        // [SerializeField] private float maxLeadTime = 1.2f;
        [Tooltip("켜면 타깃이 '직진'이 아니라 '현재 도는 방향으로 계속 돈다'고 가정해 곡선 리드한다(짧은 리드에 유리).")]
        [SerializeField] private bool leadTurnRate = true;
        [Tooltip("리드 구간 동안 허용하는 최대 예측 회전각(도). 곡선 과예측 폭주를 막는다.")]
        [SerializeField, Range(0f, 180f)] private float maxLeadTurn = 60f;
        [Tooltip("최소 발사 각도(도). 음수면 아래로도 던질 수 있다.")]
        [SerializeField, Range(-89f, 89f)] private float minLaunchAngle = 10f;
        [Tooltip("무기 대신 '파워업 좀비'를 던질 확률.")]
        [SerializeField, Range(0f, 1f)] private float powerupZombieChance = 0.15f;
        [Tooltip("티어 데미지에 추가로 곱하는 스킬 배수.")]
        [SerializeField] private float damageMultiplier = 1f;
        [Tooltip("'회전 시작' 애니 이벤트 이후 던질 방향으로 회전하는 시간(초). 이 시간이 끝나면 던진다.")]
        [SerializeField] private float aimRotateDuration = 0.3f;
        [Tooltip("좀비 티어 스케일에 추가로 곱하는 투사체 크기 배수.")]
        [SerializeField] private float projectileScale = 1f;

        [Header("스폰(이벤트 경유)")]
        [SerializeField] private EventChannelSO createChannel;     // SpawnHeldProjectileEvent 발행
        [SerializeField] private PoolItemSO weaponPoolItem;
        [SerializeField] private PoolItemSO powerupZombiePoolItem;

        [Header("사운드")]
        [SerializeField] private EventChannelSO soundChannel;
        [SerializeField] private SoundClipSO zombieAttackSound;

        [Header("연출")]
        [SerializeField] private AnimParamSO throwAnim;
        [SerializeField] private float crossFade = 0.1f;
        [SerializeField] private float maxAnimDelay = 2f;

        private ZombieEnemy _zombie;
        private GameObject _target;
        private bool _released;
        private bool _subscribed;
        private ThrownProjectile _held;   // 윈드업~발사 사이 손에 들고 있는 투사체
        private float _flightTime;        // 이번 발사에 쓰는 고정 비행시간 N
        private Coroutine _attackDelayRoutine; // 던지기 애니 재생 전 랜덤 지연 코루틴(중단 시 취소해야 함)

        // 타깃 yaw 회전 속도 추정(속도 방향 변화율). 곡선 리드 예측에 쓴다.
        private const float TurnRateSmoothing = 0.35f; // 각속도 추정 지수 평활 계수
        private Rigidbody _targetRb;      // 타깃 속도/회전 추정용 캐시
        private Vector3 _prevTargetVelDir;// 직전 표본의 수평 속도 방향
        private float _targetYawRate;     // 추정 yaw 각속도(rad/s, 부호=회전 방향)
        private float _sampleDt;          // 마지막 갱신 이후 누적 시간(물리 스텝 보정)
        private bool _hasPrevVel;         // 첫 표본 확보 여부

        // 조준 단계(StartAim 이벤트 ~ aimRotateDuration 동안 회전 → 끝나면 발사).
        private bool _aiming;             // 회전 중인지
        private bool _hasLockedAim;       // 조준 지점을 확정했는지
        private float _aimElapsed;        // 회전 경과 시간
        private Vector3 _lockedAimPoint;  // StartAim 시점에 확정한 예측 조준 지점(월드)
        private Quaternion _aimStartRot;  // 회전 시작 시 좀비 회전
        private Quaternion _aimTargetRot; // 조준 지점을 바라보는 목표 회전(수평)

        // 쿨다운은 티어가 정한다(없으면 SkillData 기본).
        protected override float Cooldown => _zombie != null ? _zombie.ThrowCooldown : base.Cooldown;

        public override void InitializeSkill(ISkillModule skillModule)
        {
            base.InitializeSkill(skillModule);
            _zombie = OwnerEnemy as ZombieEnemy;
            if (throwOrigin == null) throwOrigin = OwnerEnemy.transform;
        }

        // 스포너 공격 제한: 코디네이터가 있으면 슬롯이 있어야 던질 수 있다(없으면 = 제한 없음).
        private EnemyAttackCoordinator Coordinator => _zombie != null ? _zombie.AttackCoordinator : null;

        public override bool CanUseSkill(GameObject target = null)
        {
            return target != null && !IsUsing && NormalizedCooldown >= 1f
                   && (Coordinator == null || Coordinator.HasFreeSlot);
        }

        public override void UseSkill(GameObject target = null)
        {
            base.UseSkill(target); // IsUsing = true

            // 슬롯 점유(스포너 공격 제한). CanUseSkill 통과 후 같은 프레임 경합으로 실패할 수 있다 →
            // 조용히 종료(StopSkill이 OnSkillEnd를 발행해 BT가 Running에 갇히지 않는다. 쿨다운은 한 번 돈다 — 감수).
            if (Coordinator != null && !Coordinator.TryAcquire(this))
            {
                StopSkill();
                return;
            }

            _target = target;
            _released = false;
            _aiming = false;        // 스킬 시작 직후엔 회전하지 않는다(StartAim 이벤트 전까지).
            _hasLockedAim = false;
            _aimElapsed = 0f;

            // 타깃 회전 속도 추정 초기화(StartAim 전까지 윈드업 동안 표본을 쌓는다).
            _targetRb = target != null ? target.GetComponentInParent<Rigidbody>() : null;
            _targetYawRate = 0f;
            _hasPrevVel = false;
            _sampleDt = 0f;

            _attackDelayRoutine = StartCoroutine(AttackDelay());
        }

        private IEnumerator AttackDelay()
        {
            float randomDelay = Random.Range(0, maxAnimDelay);
            yield return new WaitForSeconds(randomDelay);

            _attackDelayRoutine = null;

            if (throwAnim != null)
                Renderer?.PlayClip(throwAnim.ParamHash, 0f, crossFade);

            Subscribe();
        }

        // 무기/파워업 좀비를 이 시점에 결정해 풀에서 꺼내 손에 들린다(= 곧 던질 바로 그 인스턴스).
        private void SpawnHeldItem()
        {
            if (createChannel == null) return;

            bool throwZombie = powerupZombiePoolItem != null
                               && Random.value < powerupZombieChance;
            // 좀비도 무기와 완전히 같은 궤적(같은 N)으로 던진다. 감속은 투사체의 launchSpeedMultiplier가
            // "같은 호를 슬로모션으로" 만들어 담당한다(높이 따로 안 올림).
            _flightTime = flightTime;
            PoolItemSO item = throwZombie ? powerupZombiePoolItem : weaponPoolItem;
            if (item == null) return;

            float damage = (_zombie != null ? _zombie.ThrowDamage : 0f) * damageMultiplier;
            float scale = (OwnerEnemy != null ? OwnerEnemy.transform.localScale.x : 1f) * projectileScale;

            SpawnHeldProjectileEvent evt = CreateEvents.SpawnHeldProjectileEvent.InitData(
                item, throwOrigin, OwnerEnemy, damage, scale, OnHeldSpawned);
            createChannel.RaiseEvent(evt);
        }

        // 스포너가 동기적으로 돌려주는 인스턴스를 받아 둔다(IPoolable → ThrownProjectile 캐스팅).
        private void OnHeldSpawned(IPoolable spawned)
        {
            _held = spawned as ThrownProjectile;
        }

        // 애니메이션 이벤트(StartAimTrigger) → 이 순간 던질 조준 지점을 확정(락)하고 그 방향으로 회전 시작.
        // 실제 발사는 회전을 마치는 aimRotateDuration(n)초 뒤 Update에서. 조준을 일찍 확정하므로
        // 타깃이 그 사이 이동하면 약간의 오차가 생긴다(설계상 감수).
        private void HandleStartAim()
        {
            if (_released || _aiming) return;
            if (_target == null || OwnerEnemy == null) return;

            _lockedAimPoint = PredictAimPoint(_target);
            _hasLockedAim = true;

            _aimStartRot = OwnerEnemy.transform.rotation;
            _aimTargetRot = ComputeFacing(_lockedAimPoint);
            _aimElapsed = 0f;
            _aiming = true;
        }

        // 손에 든 투사체를 실제로 발사. 회전은 StartAim 때 예측으로 이미 맞춰뒀지만(시각용),
        // 던지기 직전 이 순간 타깃 위치를 한 번 더 예측해 그 점에 정확히 N초 뒤 도달하는 속도로 쏜다.
        // (회전과 발사 방향이 약간 어긋나도 명중은 속도벡터로 정직하게 한다.)
        // 경로 라인 미리보기는 발사된 투사체가 직접 그린다.
        private void HandleRelease()
        {
            if (_released) return;
            _released = true;
            _aiming = false;

            if (_held == null) return; // 스폰 실패 등 — 던질 게 없으면 그냥 종료

            // 조준을 확정하지 못했으면(StartAim 전에 끝남 등) 던지지 않고 손에 든 채 반납(고아 방지).
            if (!_hasLockedAim)
            {
                _held.ReturnHeldToPool();
                _held = null;
                return;
            }

            // 던지기 직전 재예측(타깃이 사라졌으면 StartAim 때 락한 지점으로 폴백).
            Vector3 aimPoint = _target != null ? PredictAimPoint(_target) : _lockedAimPoint;
            Vector3 velocity = ComputeVelocity(throwOrigin.position, aimPoint, _flightTime);

            if (soundChannel != null && zombieAttackSound != null)
            {
                soundChannel.RaiseEvent(SoundSystemEvents.PlaySoundEvent.Init(transform.position, zombieAttackSound));
            }

            _held.LaunchFromHold(velocity, _flightTime);
            _held = null;
        }

        // 던지기 애니메이션 끝 → 스킬 종료.
        private void HandleAnimEnd()
        {
            if (!_released) HandleRelease(); // DamageCast 이벤트가 없을 때의 안전망
            StopSkill();
        }

        public override void StopSkill()
        {
            // 아직 던지기 애니가 재생되기 전(랜덤 윈드업 지연) 중단되면 그 코루틴부터 취소한다.
            // 안 그러면 지연이 끝난 뒤 코루틴이 깨어나 공격 모션을 재생하고 다시 Subscribe해버려
            // "도망쳐야 할 좀비가 공격 모션을 하는" 버그가 난다.
            if (_attackDelayRoutine != null)
            {
                StopCoroutine(_attackDelayRoutine);
                _attackDelayRoutine = null;
            }

            Coordinator?.Release(this); // 공격 슬롯 반납(점유 안 했어도 멱등이라 안전)
            Unsubscribe();
            _released = true;
            _aiming = false;
            ReturnHeldIfAny(); // 윈드업 중 중단되면 손에 든 걸 반납
            base.StopSkill(); // IsUsing=false, _lastUseTime=now, OnSkillEnd
        }

        private void Update()
        {
            TrackTargetTurnRate(); // 윈드업~조준 동안 타깃 yaw 각속도를 계속 추정

            if (!_aiming || OwnerEnemy == null) return;

            // 확정한 조준 지점 방향으로 aimRotateDuration 동안 정확히 회전 완료(Slerp 정규화).
            // 발사는 회전과 무관하게 DamageCast 이벤트가 트리거한다(여긴 회전만).
            _aimElapsed += Time.deltaTime;
            float t = aimRotateDuration > 0f ? Mathf.Clamp01(_aimElapsed / aimRotateDuration) : 1f;
            OwnerEnemy.transform.rotation = Quaternion.Slerp(_aimStartRot, _aimTargetRot, t);

            if (t >= 1f) _aiming = false; // 회전 완료 — 멈춘다(던지지 않음).
        }

        private void OnDisable()
        {
            // 비활성화로 StopSkill을 못 타는 경우의 안전망.
            Coordinator?.Release(this);
            ReturnHeldIfAny();
        }

        private void ReturnHeldIfAny()
        {
            if (_held == null) return;
            _held.ReturnHeldToPool();
            _held = null;
        }

        // 고정 조준 지점을 수평으로 바라보는 회전(좌우만). 방향이 거의 0이면 현재 회전 유지.
        private Quaternion ComputeFacing(Vector3 aimPoint)
        {
            Vector3 dir = aimPoint - OwnerEnemy.transform.position;
            dir.y = 0f;
            return dir.sqrMagnitude < 0.0001f
                ? OwnerEnemy.transform.rotation
                : Quaternion.LookRotation(dir.normalized);
        }

        // 고정 비행시간 N이라 리드 시간이 거리에 의존하지 않는다 → 수렴 반복 없이 1회 예측이면 끝(50마리에 가볍다).
        // 리드 시간 = 실제 비행 소요(감속 슬로모션 좀비는 GetFlightDuration으로 1/m배 길어짐).
        // 그 시간만큼 이동한 타깃 위치를 조준 지점으로 삼는다(직진이면 직선, 돌고 있으면 수평 원호).
        // 회피 여지를 주던 랜덤 오차는 제거 — 정확도 최우선.
        private Vector3 PredictAimPoint(GameObject target)
        {
            // UseSkill에서 같은 타깃으로 캐시해 둔 Rigidbody를 재사용한다(GetComponentInParent 반복 방지).
            Vector3 targetVel = _targetRb != null ? _targetRb.linearVelocity : Vector3.zero;

            float leadTime = _held != null ? _held.GetFlightDuration(_flightTime) : _flightTime;
            return PredictTargetPosition(target.transform.position, targetVel, _targetYawRate, leadTime);
        }

        // 타깃의 t초 뒤 위치 예측. 수직 성분은 직선 외삽, 수평 성분은 추정 yaw 각속도로 등각속도 원호.
        // 수평 변위 = h·t·[ sinθ/θ·fwd + (1−cosθ)/θ·p ],  θ=yawRate·t,  p=fwd를 +90° 회전한 단위벡터.
        // (θ→0이면 자동으로 직선 h·t·fwd으로 수렴.) θ는 maxLeadTurn으로 클램프해 곡선 과예측을 막는다.
        private Vector3 PredictTargetPosition(Vector3 pos, Vector3 vel, float yawRate, float t)
        {
            Vector3 horiz = new Vector3(vel.x, 0f, vel.z);
            float hSpeed = horiz.magnitude;
            Vector3 yOffset = Vector3.up * (vel.y * t);

            // 끄거나, 거의 안 돌거나, 멈춰 있거나, 리드가 없으면 직선 외삽.
            if (!leadTurnRate || hSpeed < 1e-4f || Mathf.Abs(yawRate) < 1e-4f || t < 1e-4f)
                return pos + horiz * t + yOffset;

            Vector3 fwd = horiz / hSpeed;
            float maxRad = maxLeadTurn * Mathf.Deg2Rad;
            float theta = Mathf.Clamp(yawRate * t, -maxRad, maxRad);     // 이번 리드 동안의 총 회전각
            Vector3 p = Quaternion.AngleAxis(90f, Vector3.up) * fwd;     // yawRate 부호와 동일한 회전계

            float a = Mathf.Sin(theta) / theta;
            float b = (1f - Mathf.Cos(theta)) / theta;
            Vector3 horizDisp = hSpeed * t * (a * fwd + b * p);
            return pos + horizDisp + yOffset;
        }

        // 타깃 속도 방향의 변화율로 yaw 각속도를 추정한다. 물리 스텝마다만 속도가 갱신되므로
        // 방향이 실제로 바뀐 표본에서만 누적 시간(_sampleDt) 기준으로 계산하고, 한동안 안 바뀌면 0으로 수렴.
        private void TrackTargetTurnRate()
        {
            if (!IsUsing || _targetRb == null) return;

            _sampleDt += Time.deltaTime;

            Vector3 v = _targetRb.linearVelocity;
            v.y = 0f;
            if (v.sqrMagnitude < 1e-4f) return; // 거의 멈춤 — 방향 의미 없음
            Vector3 dir = v.normalized;

            if (!_hasPrevVel)
            {
                _prevTargetVelDir = dir;
                _hasPrevVel = true;
                _sampleDt = 0f;
                return;
            }

            if ((dir - _prevTargetVelDir).sqrMagnitude > 1e-8f && _sampleDt > 1e-5f)
            {
                // 방향이 바뀐 = 물리 스텝이 실제로 일어난 표본에서만 각속도 갱신.
                float rate = Vector3.SignedAngle(_prevTargetVelDir, dir, Vector3.up) * Mathf.Deg2Rad / _sampleDt;
                _targetYawRate = Mathf.Lerp(_targetYawRate, rate, TurnRateSmoothing);
                _prevTargetVelDir = dir;
                _sampleDt = 0f;
            }
            else if (_sampleDt > 0.2f)
            {
                // 방향이 한동안 안 바뀜 = 직진 중 → 추정 각속도를 0으로 끌어내린다.
                _targetYawRate = Mathf.Lerp(_targetYawRate, 0f, TurnRateSmoothing);
                _sampleDt = 0f;
            }
        }

        private Vector3 ComputeVelocity(Vector3 origin, Vector3 aimPoint, float t)
        {
            float n = Mathf.Max(0.01f, t);
            Vector3 v = (aimPoint - origin) / n - 0.5f * Physics.gravity * n;

            Vector3 horiz = new Vector3(v.x, 0f, v.z);
            float hSpeed = horiz.magnitude;
            if (hSpeed < 1e-4f) return v;

            float currentAngle = Mathf.Atan2(v.y, hSpeed) * Mathf.Rad2Deg;
            if (currentAngle >= minLaunchAngle) return v;

            float speed = v.magnitude;
            float rad = minLaunchAngle * Mathf.Deg2Rad;
            Vector3 hDir = horiz / hSpeed;
            return hDir * (speed * Mathf.Cos(rad)) + Vector3.up * (speed * Mathf.Sin(rad));
        }


        private void Subscribe()
        {
            if (_subscribed || OwnerEnemy == null || OwnerEnemy.Trigger == null) return;
            OwnerEnemy.Trigger.OnStartAim += HandleStartAim;  // 조준 확정 + 회전 시작
            OwnerEnemy.Trigger.OnDamageCast += HandleRelease; // 실제 던지기(회전과 무관)
            OwnerEnemy.Trigger.OnAnimationEnd += HandleAnimEnd;
            OwnerEnemy.Trigger.OnSpawnItem += SpawnHeldItem;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || OwnerEnemy == null || OwnerEnemy.Trigger == null) return;
            OwnerEnemy.Trigger.OnStartAim -= HandleStartAim;
            OwnerEnemy.Trigger.OnDamageCast -= HandleRelease;
            OwnerEnemy.Trigger.OnAnimationEnd -= HandleAnimEnd;
            OwnerEnemy.Trigger.OnSpawnItem -= SpawnHeldItem;
            _subscribed = false;
        }
    }
}
