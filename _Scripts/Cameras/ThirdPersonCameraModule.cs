using EventChannelSystem;
using Events;
using ModuleSystem;
using Players;
using Players.Movement;
using UnityEngine;

namespace Cameras
{
    // 3인칭 façade + 컨텍스트 + 라우터.
    // CameraOwner에는 여전히 하나의 ICameraBehaviour/ICameraLookAt로 보이지만,
    // 실제 이동/회전은 상황별로 선택된 자식 state(IThirdPersonCameraState)에 위임한다.
    // 자식 state가 매 프레임 읽을 런타임 값(피벗/회전/공전 입력)은 IThirdPersonContext로 노출한다.
    public class ThirdPersonCameraModule : MonoBehaviour, IModule, ICameraBehaviour, ICameraLookAt, IThirdPersonContext
    {
        [SerializeField] private PlayerInputSO playerInput;
        [Tooltip("플레이어가 PlayerInitEvent를 발행하는 채널과 동일한 에셋을 연결.")]
        [SerializeField] private EventChannelSO _playerChannel;

        private IRotateMovement _rotate;
        private Transform _cameraTrm;  // 카메라 리그(=owner) transform. CameraPosition 게터가 사용.

        private Vector2 _lookDelta;   // 이번 프레임 마우스 이동량(입력 콜백이 갱신, 매 프레임 최신값으로 덮어씀)
        private float _explodeScale = 1f; // 최근 폭발 진입의 후퇴 배수(SetSituation에서 갱신)
        private IThirdPersonCameraState _active;
        private GroundedThirdPersonState _grounded;
        private DashChaseThirdPersonState _dashChase;
        private PeekThirdPersonState _peek;
        private LookBackThirdPersonState _lookBack;

        // ── IThirdPersonContext: 자식 state가 매 프레임 읽는 런타임 값 ──
        public Vector3 PivotPosition => _rotate.PivotPosition;
        public Vector3 CameraPosition => _cameraTrm.position;
        public Quaternion Facing => _rotate.FacingRotation;
        public Vector2 LookDelta => _lookDelta;
        public float ExplodeScale => _explodeScale;

        // ── ICameraBehaviour / ICameraLookAt: 활성 state로 위임 ──
        public float Damping => _active?.Damping ?? 0f;
        public float RotationDamping => 0f; // 회전은 ICameraLookAt 경로(실제 위치에서 대상 주시)로 처리
        public Vector3 LookTarget => _active?.LookTarget ?? _rotate.PivotPosition;
        public Vector3 LookUp => _active?.LookUp ?? Vector3.up;

        // 활성 시한부 state(peek 등)가 끝났는지. CameraModeModule이 폴링해 기본 시점으로 복귀시킨다.
        public bool ActiveStateFinished => _active?.IsFinished ?? false;

        public void Initialize(ModuleOwner owner)
        {
            // state는 이 오브젝트의 자식 컴포넌트로 둔다(인스펙터에서 각각 따로 튜닝).
            _grounded = GetComponentInChildren<GroundedThirdPersonState>(true);
            _dashChase = GetComponentInChildren<DashChaseThirdPersonState>(true);
            _peek = GetComponentInChildren<PeekThirdPersonState>(true);
            _lookBack = GetComponentInChildren<LookBackThirdPersonState>(true);

            _cameraTrm = owner.transform;
            _playerChannel?.AddListener<PlayerInitEvent>(OnPlayerInit);
        }

        // 플레이어 회전 모듈 참조 수령. IThirdPersonContext 게터(PivotPosition/Facing)가 이 참조를 쓴다.
        private void OnPlayerInit(PlayerInitEvent evt) => _rotate = evt.Rotate;

        private void OnDestroy()
        {
            _playerChannel?.RemoveListener<PlayerInitEvent>(OnPlayerInit);
        }

        private void OnEnable()
        {
            if (playerInput != null) playerInput.OnCameraLookChanged += SetLookDelta;
        }

        private void OnDisable()
        {
            if (playerInput != null) playerInput.OnCameraLookChanged -= SetLookDelta;
        }

        // 마우스 이동량을 그 프레임의 최신값으로 보관(누적이 아님 — 입력 없으면 canceled로 0이 들어온다).
        private void SetLookDelta(Vector2 value) => _lookDelta = value;

        // 상황 진입/변경. fresh=true(팔로우→3인칭 첫 진입)면 state가 방향을 새로 래치/타이머를 리셋한다.
        public void SetSituation(ThirdPersonSituation situation, bool fresh, float pullScale = 1f)
        {
            _explodeScale = pullScale;
            _active = situation switch
            {
                ThirdPersonSituation.Stop       => _grounded,
                ThirdPersonSituation.Explode    => _grounded,
                ThirdPersonSituation.SuperBoost => _dashChase,
                ThirdPersonSituation.Peek       => _peek,
                ThirdPersonSituation.LookBack   => _lookBack,
                _                               => _grounded
            };
            _active?.Enter(this, situation, fresh);
        }

        public CameraPose GetDesiredPose()
        {
            if (_active == null) return new CameraPose(transform.position, transform.rotation);
            CameraPose pose = _active.Tick(this, Time.deltaTime);
            _lookDelta = Vector2.zero;   // 이번 프레임 마우스 입력은 한 번만 소비(중복 적용/드리프트 방지)
            return pose;
        }
    }
}
