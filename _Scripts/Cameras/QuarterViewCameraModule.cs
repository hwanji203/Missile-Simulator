using EventChannelSystem;
using Events;
using ModuleSystem;
using Players.Movement;
using UnityEngine;

namespace Cameras
{
    // 3인칭 쿼터뷰: yaw/pitch 모두 미사일 진행 방향을 데드존을 두고 따라 공전한다.
    // ICameraLookAt 경로라 미사일이 항상 화면 중앙에 오고, 댐핑은 CameraOwner가 오프셋에만 건다.
    public class QuarterViewCameraModule : MonoBehaviour, IModule, ICameraBehaviour, ICameraLookAt
    {
        [Tooltip("피벗에서 카메라까지 거리")]
        [SerializeField] private float distance = 12f;
        [Tooltip("CameraOwner 오프셋 댐핑 — 진입 dolly-out/공전 변화의 부드러움")]
        [SerializeField] private float damping = 8f;

        [Header("Orbit - 좌우(yaw)")]
        [Tooltip("진행 방향을 따라 공전하는 속도 (클수록 더 빠르게 끌어당김)")]
        [SerializeField] private float orbitDampingYaw = 0.75f;
        [Tooltip("진행 방향에서 좌우로 뒤처질 수 있는 최대 각도(도)")]
        [SerializeField] private float deadzoneYaw = 50f;

        [Header("Orbit - 상하(pitch)")]
        [Tooltip("로켓 수평 비행 시 카메라 기본 앙각(도). 로켓 pitch에 더해진다.")]
        [SerializeField] private float defaultPitchOffset = 30f;
        [Tooltip("상하로 따라 공전하는 속도 (클수록 더 빠르게 끌어당김)")]
        [SerializeField] private float orbitDampingPitch = 0.75f;
        [Tooltip("상하로 뒤처질 수 있는 최대 각도(도)")]
        [SerializeField] private float deadzonePitch = 30f;

        [Tooltip("데드존을 넘었을 때 경계로 복귀하는 속도 (작을수록 더 늘어지게 끌림)")]
        [SerializeField] private float snapDamping = 5f;

        [Tooltip("플레이어가 PlayerInitEvent를 발행하는 채널과 동일한 에셋을 연결.")]
        [SerializeField] private EventChannelSO playerChannel;

        private IRotateMovement _rotate;

        private Transform _cameraTrm;
        private float _camYaw;
        private float _camPitch;
        private float _targetYaw;
        private float _targetPitch;
        private bool _started;

        public float Damping => damping;
        public float RotationDamping => 0f;
        public Vector3 LookTarget => _rotate.PivotPosition;
        public Vector3 LookUp => Vector3.up;

        public void Initialize(ModuleOwner owner)
        {
            _cameraTrm = owner.transform;
            
            playerChannel?.AddListener<PlayerInitEvent>(OnPlayerInit);
        }

        private void OnDestroy()
        {
            playerChannel?.RemoveListener<PlayerInitEvent>(OnPlayerInit);
        }

        // 플레이어 회전 모듈 참조를 받은 뒤 초기 각도를 래치한다(과거 Start 로직).
        private void OnPlayerInit(PlayerInitEvent evt)
        {
            _rotate = evt.Rotate;
            if (_rotate == null) return;

            _targetYaw   = ComputeTargetYaw(0f);
            _camYaw      = _targetYaw;
            _targetPitch = ComputeTargetPitch(defaultPitchOffset);
            _camPitch    = _targetPitch;
            _started     = true;
        }

        public CameraPose GetDesiredPose()
        {
            if (!_started) return new CameraPose(_cameraTrm.position, _cameraTrm.rotation);

            _targetYaw   = ComputeTargetYaw(_targetYaw);
            _targetPitch = ComputeTargetPitch(_targetPitch);

            float dampYaw   = orbitDampingYaw;
            float dampPitch = orbitDampingPitch;

            _camYaw   = ComputeOrbitAngle(_camYaw,   _targetYaw,   dampYaw,   deadzoneYaw);
            _camPitch = ComputeOrbitAngle(_camPitch, _targetPitch, dampPitch, deadzonePitch);

            Quaternion yawRot = Quaternion.Euler(0f, _camYaw, 0f);
            Vector3 dir      = yawRot * (Quaternion.AngleAxis(_camPitch, Vector3.right) * Vector3.back);
            Vector3 position = _rotate.PivotPosition + dir * distance;

            return new CameraPose(position, Quaternion.LookRotation(-dir, Vector3.up));
        }

        // 현재 각도, 타겟 각도, damping, deadzone → 새 각도 (lag 기반 orbit + deadzone)
        private float ComputeOrbitAngle(float current, float target, float damp, float deadzone)
        {
            float lag = Mathf.DeltaAngle(target, current);
            lag = Mathf.Lerp(lag, 0f, 1f - Mathf.Exp(-damp * Time.deltaTime));
            lag = SmoothClampAngle(lag, deadzone);
            return target + lag;
        }

        // 진행 방향(DesiredRotation)의 수평 yaw. 수직에 가까워 투영이 0이면 fallback 유지.
        private float ComputeTargetYaw(float fallback)
        {
            Vector3 fwd = _rotate.DesiredRotation * Vector3.forward;
            fwd.y = 0f;
            return fwd.sqrMagnitude < 1e-4f ? fallback : Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
        }

        // 진행 방향의 월드 pitch(앙각) + defaultPitchOffset. 방향이 없으면 fallback 유지.
        private float ComputeTargetPitch(float fallback)
        {
            Vector3 fwd = _rotate.DesiredRotation * Vector3.forward;
            if (fwd.sqrMagnitude < 1e-4f) return fallback;
            float pitch = -Mathf.Asin(Mathf.Clamp(fwd.y, -1f, 1f)) * Mathf.Rad2Deg;
            return pitch + defaultPitchOffset;
        }

        // 각도가 limit를 넘으면 경계(limit)로 snapDamping 속도로 부드럽게 끌어당긴다
        private float SmoothClampAngle(float angle, float limit)
        {
            if (Mathf.Abs(angle) <= limit) return angle;
            float target = Mathf.Sign(angle) * limit;
            float st     = 1f - Mathf.Exp(-snapDamping * Time.deltaTime);
            return Mathf.Lerp(angle, target, st);
        }
    }
}
