using System.Collections;
using CombatSystem;
using Effects;
using EventChannelSystem;
using Events;
using HwanLib.GGMLib.SoundSystem;
using ModuleSystem;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Players.Movement
{
    // 미사일이 실제로 바라보는 방향(Visual의 forward)으로 전진시킨다.
    public class MissileMovement : MonoBehaviour, IModule, IMovement
        , IStoppableMovement, IContactFreezable
    {
        [SerializeField] private Rigidbody rb;
        [SerializeField] private float moveSpeed = 10f;
        [Tooltip("속도가 떨어질 때 감속 부드러움 (작을수록 천천히 떨어진다). 가속은 즉시.")]
        [SerializeField] private float decelDamping = 2f;

        [Header("슈퍼 부스트 (취소 불가 직선 돌진)")]
        [Tooltip("슈퍼 부스트 시 moveSpeed에 곱해지는 배율")]
        [SerializeField] private float superMultiplier = 6f;
        [Tooltip("슈퍼 부스트 발동/카메라 3인칭 트리거를 발행할 이벤트 채널 (다른 모듈과 같은 에셋 공유)")]
        [SerializeField] private EventChannelSO playerEvent;
        [SerializeField] private AssetHashSO windScreenVfxHash;

        [Header("사운드")]
        [SerializeField] private EventChannelSO soundChannel;
        [SerializeField] private SoundClipSO boostSound;
        [SerializeField] private SoundClipSO superBoostSound;

        private IInputable _input;
        private IRotateMovement _rotation;
        private float _currentSpeed;
        private bool _stopped;
        private bool _superBoosting;
        private Vector3 _lockedDir;   // 슈퍼 부스트 발동 순간 고정된 진행 방향
        private PlayerSkillInventory _inventory;

        public float CurrentSpeed => rb.linearVelocity.magnitude;

        public bool IsSuperBoosting => _superBoosting;

        public void Initialize(ModuleOwner owner)
        {
            _input = owner.GetModule<IInputable>();
            _rotation = owner.GetModule<IRotateMovement>();
            _inventory = owner.GetModule<PlayerSkillInventory>();
        }

        private void Start()
        {
            playerEvent.RaiseEvent(PlayerEvents.PlayerScreenVfxEvent
                .InitData(windScreenVfxHash.AssetHash, true));
            StartCoroutine(CheckPlayerVelocityZero());

            if (soundChannel != null && boostSound != null)
            {
                soundChannel.RaiseEvent(SoundSystemEvents.PlaySoundEvent.Init(transform.position, boostSound, boostSound));
            }
        }

        private void OnDestroy()
        {
            if (soundChannel != null)
            {
                if (boostSound != null)
                {
                    soundChannel.RaiseEvent(SoundSystemEvents.StopSoundEvent.Init(boostSound));
                }
                if (superBoostSound != null)
                {
                    soundChannel.RaiseEvent(SoundSystemEvents.StopSoundEvent.Init(superBoostSound));
                }
            }
        }

        private IEnumerator CheckPlayerVelocityZero()
        {
            float velocityZeroTime = 0f;
            float targetTime = 1f;
            float checkInterval = 0.1f;

            // 1초 동안 누적될 때까지 반복 수행
            while (velocityZeroTime < targetTime)
            {
                yield return new WaitForSeconds(checkInterval);

                // Rigidbody의 속도 크기가 거의 0인지 확인 (미세한 움직임 고려)
                if (rb != null && rb.linearVelocity.sqrMagnitude < 0.001f)
                {
                    // 멈춰있다면 경과한 시간만큼 누적
                    velocityZeroTime += checkInterval;
                }
                else
                {
                    // 움직였다면 누적 시간 초기화 (연속 1초여야 하므로)
                    velocityZeroTime = 0f;
                }
            }

            // 1초 동안 무사히 멈춰있었다면 이벤트 호출
            playerEvent.RaiseEvent(PlayerEvents.PlayerVelocityZero);
        }

        public void StopMovement()
        {
            // 조종(추진)을 멈추고 속도를 0으로 만든 뒤 중력에만 맡긴다(폭발/접촉 공통).
            // 폭발 시엔 회전을 얼리지 않으므로, 0이 된 속도가 중력으로 아래로 자라며
            // MissileRotation이 그 속도 방향(아래)을 바라보게 된다. 슈퍼 부스트도 여기서 종료.
            float lastSpeed = _currentSpeed;
            playerEvent.RaiseEvent(PlayerEvents.PlayerScreenVfxEvent
                .InitData(windScreenVfxHash.AssetHash, false));
            
            if (soundChannel != null)
            {
                if (boostSound != null)
                {
                    soundChannel.RaiseEvent(SoundSystemEvents.StopSoundEvent.Init(boostSound));
                }
                if (superBoostSound != null)
                {
                    soundChannel.RaiseEvent(SoundSystemEvents.StopSoundEvent.Init(superBoostSound));
                }
            }

            _stopped = true;
            _superBoosting = false;
            _currentSpeed = 0f;
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(Random.onUnitSphere * lastSpeed / 2, ForceMode.Impulse);
            rb.useGravity = true;
        }

        // 스페이스 홀드로 발동. 한 번 켜지면 취소 불가 — 땅에 닿아 StopMovement될 때까지 유지.
        public bool TryStartSuperBoost()
        {
            if (_superBoosting || _stopped) return false;

            _superBoosting = true;
            _lockedDir = (_rotation.FacingRotation * Vector3.forward).normalized;

            if (soundChannel != null)
            {
                if (boostSound != null)
                {
                    soundChannel.RaiseEvent(SoundSystemEvents.StopSoundEvent.Init(boostSound));
                }
                if (superBoostSound != null)
                {
                    soundChannel.RaiseEvent(SoundSystemEvents.PlaySoundEvent.Init(transform.position, superBoostSound, superBoostSound));
                }
            }

            playerEvent?.RaiseEvent(PlayerEvents.PlayerSuperBoostEvent
                .InitData(false, false, true));

            return true;
        }

        private void FixedUpdate()
        {
            if (_stopped) return;
            
            if (_superBoosting)
            {
                // 발동 순간 방향으로 고정해 초고속 직선 돌진. 매 스텝 하드셋이라 충돌 임펄스에 안 밀린다.
                // superMultiplier는 base, SuperBoost 스킬 레벨 보너스가 가산된다.
                float mult = superMultiplier + (_inventory != null ? _inventory.GetBonus(SkillType.SuperBoost) : 0f);
                rb.linearVelocity = _lockedDir * (moveSpeed * mult);
                return;
            }

            float target = moveSpeed;
            // 올라갈 땐 즉시(확), 떨어질 땐 decelDamping으로 천천히
            if (target >= _currentSpeed)
                _currentSpeed = target;
            else
                _currentSpeed = Mathf.Lerp(_currentSpeed, target, 1f - Mathf.Exp(-decelDamping * Time.fixedDeltaTime));

            // FacingRotation * forward == Visual.forward (실제 바라보는 방향)
            rb.linearVelocity = _rotation.FacingRotation * Vector3.forward * _currentSpeed;
        }
        
        public void Freeze()
        {
            rb.linearVelocity = Vector3.zero;
        }
    }
}
