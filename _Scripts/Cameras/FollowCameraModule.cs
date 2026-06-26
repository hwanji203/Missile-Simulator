using EventChannelSystem;
using Events;
using ModuleSystem;
using Players.Movement;
using UnityEngine;

namespace Cameras
{
    // 1인칭 팔로우: 미사일 뒤를 데드존을 두고 공전(orbit)하며 따라간다.
    // 기존 FollowCamera 로직을 그대로 옮기되, transform을 직접 쓰지 않고
    // 목표 포즈만 계산해 돌려준다(실제 적용/댐핑은 CameraOwner).
    public class FollowCameraModule : MonoBehaviour, IModule, ICameraBehaviour
    {
        [SerializeField] private float damping = 15;
        [Tooltip("카메라 roll 최대 각도(도) — 이 값으로 클램프")]
        [SerializeField] private float maxCameraRoll = 20f;
        [Tooltip("roll이 0(수평)으로 복귀하려는 힘 (클수록 빨리 수평으로 돌아옴)")]
        [SerializeField] private float rollReturnSpeed = 3f;

        [Header("Orbit - 좌우(yaw)")]
        [Tooltip("좌우로 뒤를 따라 공전하는 속도 (클수록 더 많이/빠르게 끌어당김)")]
        [SerializeField] private float orbitDampingYaw = 0.75f;
        [Tooltip("좌우로 뒤처질 수 있는 최대 각도(도)")]
        [SerializeField] private float deadzoneYaw = 52.5f;

        [Header("Orbit - 상하(pitch)")]
        [Tooltip("상하로 뒤를 따라 공전하는 속도 (클수록 더 많이/빠르게 끌어당김)")]
        [SerializeField] private float orbitDampingPitch = 1.25f;
        [Tooltip("상하로 뒤처질 수 있는 최대 각도(도)")]
        [SerializeField] private float deadzonePitch = 40f;

        [Tooltip("데드존을 넘었을 때 경계로 복귀하는 속도 (작을수록 더 스무스하게/늘어지게 끌림)")]
        [SerializeField] private float snapDamping = 5f;
        [Tooltip("입력 없을 때 미사일 뒤로 빠르게 따라붙는 속도 (yaw/pitch 공통)")]
        [SerializeField] private float orbitDampingNoInput = 6f;

        [Tooltip("플레이어가 PlayerInitEvent를 발행하는 채널과 동일한 에셋을 연결.")]
        [SerializeField] private EventChannelSO _playerChannel;

        private IRotateMovement _rotateMovement;

        private Transform _cameraTrm;   // 실제 카메라(=owner 루트) transform. 이 모듈은 자식이라 자기 transform이 아님
        private Quaternion _rotationOffset;
        private Vector3 _positionOffset;
        private Vector3 _orbitDir;   // 피벗→카메라 방향 (transform에서 되읽지 않고 직접 관리)
        private float _camRoll;      // 카메라에 적용되는 roll (0으로 복귀하려는 힘 + 클램프)
        private float _prevRoll;     // 직전 프레임 미사일 roll (변화량 계산용)
        private bool _rollInit;
        private bool _started;
        private bool _snapNext;                 // 다음 포즈에서 회전을 즉시 스냅(뒤돌아보기 해제 복귀용)
        private float _effectiveRotationDamping; // 이번 프레임에 CameraOwner가 쓸 회전 댐핑(스냅 요청 시 0)

        // 원래 FollowCamera처럼 위치는 항상 직접 대입(스냅). orbit 자체가 이미 부드러우므로 위치 댐핑은 없다.
        public float Damping => 0f;
        public float RotationDamping => _effectiveRotationDamping;

        // 다음 GetDesiredPose에서 회전을 Lerp 없이 즉시 스냅하도록 요청(Shift 뒤돌아보기 해제 → 팔로우 즉시 복귀).
        public void RequestSnap() => _snapNext = true;

        public void Initialize(ModuleOwner owner)
        {
            _cameraTrm = owner.transform;
            _playerChannel?.AddListener<PlayerInitEvent>(OnPlayerInit);
        }

        private void OnDestroy()
        {
            _playerChannel?.RemoveListener<PlayerInitEvent>(OnPlayerInit);
        }

        // 플레이어 회전 모듈 참조를 받은 뒤 초기 오프셋을 잡는다(과거 Start 로직).
        // PlayerInitEvent는 모든 Awake 이후(플레이어 Start)에 발행되므로 _cameraTrm은 준비돼 있다.
        private void OnPlayerInit(PlayerInitEvent evt)
        {
            _rotateMovement = evt.Rotate;
            if (_rotateMovement == null) return;

            _rotationOffset = Quaternion.Inverse(_rotateMovement.FacingRotation) * _cameraTrm.rotation;
            _positionOffset = _cameraTrm.position - _rotateMovement.PivotPosition;
            _orbitDir = _positionOffset.normalized;
            _effectiveRotationDamping = damping;
            _started = true;
        }

        public CameraPose GetDesiredPose()
        {
            if (!_started) return new CameraPose(_cameraTrm.position, _cameraTrm.rotation);
            // 스냅 요청이 있으면 이번 프레임만 회전 댐핑 0(즉시). CameraOwner가 GetDesiredPose 직후 RotationDamping을 읽는다.
            _effectiveRotationDamping = _snapNext ? 0f : damping;
            _snapNext = false;
            return new CameraPose(ComputePosition(), ComputeRotation());
        }

        private Vector3 ComputePosition()
        {
            Vector3 pivot = _rotateMovement.PivotPosition;
            float distance = _positionOffset.magnitude;

            // 분해 기준 프레임: 전방은 DesiredRotation을 따르되 up은 월드 up으로 고정해 roll을 제거한다.
            Vector3 desiredFwd = _rotateMovement.DesiredRotation * Vector3.forward;
            Quaternion frame = Quaternion.LookRotation(desiredFwd, Vector3.up);

            // _orbitDir을 이 프레임 로컬 공간으로 변환 → yaw(좌우) / pitch(상하) 두 각도로 분해
            Vector3 local = Quaternion.Inverse(frame) * _orbitDir;
            float horiz = Mathf.Sqrt(local.x * local.x + local.z * local.z);
            float yaw   = Mathf.Atan2(local.x, -local.z) * Mathf.Rad2Deg;
            float pitch = Mathf.Atan2(local.y, horiz) * Mathf.Rad2Deg;

            // 1) 데드존 안: 각 축을 0(정렬)으로 Damping — 입력 없을 땐 빠르게 뒤로 따라붙는다
            // 1) 데드존 안: 각 축을 0(정렬)으로 Damping — 입력 없을 땐 빠르게 뒤로 따라붙는다
            bool hasInput = _rotateMovement.HasRotationInput;
            float dampYaw   = hasInput ? orbitDampingYaw   : orbitDampingNoInput;
            float dampPitch = hasInput ? orbitDampingPitch : orbitDampingNoInput;
            yaw   = Mathf.Lerp(yaw,   0f, 1f - Mathf.Exp(-dampYaw   * Time.deltaTime));
            pitch = Mathf.Lerp(pitch, 0f, 1f - Mathf.Exp(-dampPitch * Time.deltaTime));

            // 2) 데드존 밖: 경계로 snapDamping으로 부드럽게 끌어당김
            yaw   = SmoothClampAngle(yaw,   deadzoneYaw);
            pitch = SmoothClampAngle(pitch, deadzonePitch);

            // yaw/pitch → 로컬 방향 재조립 후 다시 월드로
            float yawR = yaw * Mathf.Deg2Rad;
            float pitchR = pitch * Mathf.Deg2Rad;
            Vector3 newLocal = new Vector3(
                Mathf.Cos(pitchR) * Mathf.Sin(yawR),
                Mathf.Sin(pitchR),
                -Mathf.Cos(pitchR) * Mathf.Cos(yawR));
            _orbitDir = frame * newLocal;

            return pivot + _orbitDir * distance;
        }

        // 각도가 limit를 넘으면 경계(limit)로 snapDamping 속도로 부드럽게 끌어당긴다
        private float SmoothClampAngle(float angle, float limit)
        {
            if (Mathf.Abs(angle) <= limit) return angle;

            float target = Mathf.Sign(angle) * limit;
            float st = 1f - Mathf.Exp(-snapDamping * Time.deltaTime);
            return Mathf.Lerp(angle, target, st);
        }

        private Quaternion ComputeRotation()
        {
            Quaternion targetRotation = _rotateMovement.DesiredRotation * _rotationOffset;
            return ReduceRoll(targetRotation);
        }

        private Quaternion ReduceRoll(Quaternion rotation)
        {
            // 바라보는 방향(전방)은 유지하고, 그 전방축 기준으로 카메라 roll을 적용한다
            Vector3 forward = rotation * Vector3.forward;
            Quaternion noRoll = Quaternion.LookRotation(forward, Vector3.up);

            // 미사일의 연속 roll 값을 직접 받아 그 '변화량'만큼만 카메라를 기울인다.
            float missileRoll = _rotateMovement.RollAngle;
            if (!_rollInit) { _prevRoll = missileRoll; _rollInit = true; }
            float deltaRoll = missileRoll - _prevRoll;
            _prevRoll = missileRoll;

            // 미사일이 90°로 정착(복귀) 중일 땐 그 roll 변화에 영향받지 않음
            if (!_rotateMovement.IsRollSettling)
                _camRoll += deltaRoll;

            // 매 프레임 0(수평)으로 복귀하려는 힘을 받고, 최대값으로 클램프
            _camRoll = Mathf.Lerp(_camRoll, 0f, 1f - Mathf.Exp(-rollReturnSpeed * Time.deltaTime));
            _camRoll = Mathf.Clamp(_camRoll, -maxCameraRoll, maxCameraRoll);

            return noRoll * Quaternion.AngleAxis(_camRoll, Vector3.forward);
        }
    }
}
