using Agents;
using CombatSystem;
using EventChannelSystem;
using Events;
using HwanLib.GGMLib.SoundSystem;
using Players.Movement;
using UnityEngine;

namespace Players
{
    public class PlayerController : Agent
    {
        public static Transform PlayerTransform { get; private set; }

        [field: SerializeField] public PlayerInputSO PlayerInput { get; private set; }

        [Tooltip("피격/사망 이벤트를 발행할 채널(카메라 쉐이크가 구독). 카메라가 듣는 것과 같은 채널을 넣는다.")]
        [SerializeField] private EventChannelSO playerEvent;

        [Header("사운드")]
        [SerializeField] private EventChannelSO soundChannel;
        [SerializeField] private SoundClipSO hitSound;

        private IInputable _inputable;

        protected override void Awake()
        {
            base.Awake();
            
            PlayerTransform = transform;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            if (PlayerTransform == transform)
                PlayerTransform = null;
        }

        protected override void InitializeComponents()
        {
            base.InitializeComponents();
            
            _inputable = GetModule<IInputable>();
            Debug.Assert(_inputable != null, "플레이어 이동 관련 모듈이 없습니다.");
        }

        // 모든 Awake가 끝난 Start에서 1회 발행. 이 시점엔 UIManager(DefaultExecutionOrder -100)가
        // Awake에서 HUD를 생성·구독해둔 상태라, 늦은 구독으로 이벤트를 놓칠 일이 없다.
        // SerializeField/DI 대신 이 이벤트로 플레이어 모듈 참조를 외부에 공급한다.
        private void Start()
        {
            playerEvent?.RaiseEvent(
                PlayerEvents.PlayerInitEvent
                    .Init(Health, GetModule<IRotateMovement>()
                        , GetModule<MissileFuse>(), gameObject));
        }

        private void OnEnable()
        {
            PlayerInput.OnYawPitchRotationChanged += _inputable.SetYawPitchRotation;
            PlayerInput.OnSuperBoostChange += _inputable.SetSuperBoost;

            // 피격 → 카메라 피격 쉐이크 트리거. (폭발/사망 쉐이크는 PlayerExplodedEvent로 처리된다.)
            OnHit += RaiseHitEvent;
        }

        private void OnDisable()
        {
            PlayerInput.OnYawPitchRotationChanged -= _inputable.SetYawPitchRotation;
            PlayerInput.OnSuperBoostChange -= _inputable.SetSuperBoost;

            OnHit -= RaiseHitEvent;
        }

        private void RaiseHitEvent()
        {
            playerEvent?.RaiseEvent(PlayerEvents.PlayerHitEvent);
            if (soundChannel != null && hitSound != null)
            {
                soundChannel.RaiseEvent(SoundSystemEvents.PlaySoundEvent.Init(transform.position, hitSound));
            }
        }
    }
}