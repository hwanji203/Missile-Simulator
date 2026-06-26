using UnityEngine;

namespace Cameras
{
    // Shift 홀드 '뒤돌아보기' 3인칭: 비행 중 로켓의 '앞쪽'(진행방향)으로 카메라를 두고 로켓을 뒤돌아본다.
    // → 화면엔 로켓의 앞부분 + 뒤에서 날아오는 것들이 보인다(기본 시점 = 평소 추격의 yaw 180°).
    // 기준 방향은 매 프레임 로켓의 현재 facing을 추종(래치하지 않음)하므로 로켓이 돌아도 항상 '실제 뒤'를 본다.
    // 마우스로 yaw/pitch 오프셋을 누적(각각 한계각으로 클램프)하며, 재진입해도 그 각도를 유지한다.
    // Damping=0(스냅): Shift 홀드/해제 시 Lerp 없이 즉시 전환된다.
    public class LookBackThirdPersonState : MonoBehaviour, IThirdPersonCameraState
    {
        [SerializeField] private float distance = 8f;

        [Tooltip("로켓 위로 들어올리는 기본 각도(도)")]
        [SerializeField] private float elevationAngle = 15f;

        [Header("마우스 시점 회전")]
        [Tooltip("마우스 이동량당 회전 각도(deg/마우스 단위)")]
        [SerializeField] private float mouseSensitivity = 0.15f;
        [Tooltip("좌우(yaw) 회전 한계각(도) — ±이 값으로 클램프")]
        [SerializeField] private float yawLimit = 80f;
        [Tooltip("상하(pitch) 회전 한계각(도) — ±이 값으로 클램프")]
        [SerializeField] private float pitchLimit = 60f;

        private float _yaw;     // 누적 yaw 오프셋(재진입해도 유지)
        private float _pitch;   // 누적 pitch 오프셋(재진입해도 유지)
        private Vector3 _lookTarget;

        public Vector3 LookTarget => _lookTarget;
        public Vector3 LookUp => Vector3.up;
        public float Damping => 0f;     // 즉시(스냅) — Shift 홀드/해제 시 Lerp 없음
        public bool IsFinished => false;

        // 누적 각도는 재진입해도 유지하므로 Enter에서 리셋하지 않는다.
        public void Enter(IThirdPersonContext ctx, ThirdPersonSituation situation, bool fresh) { }

        public CameraPose Tick(IThirdPersonContext ctx, float deltaTime)
        {
            _yaw = Mathf.Clamp(_yaw + ctx.LookDelta.x * mouseSensitivity, -yawLimit, yawLimit);
            _pitch = Mathf.Clamp(_pitch + ctx.LookDelta.y * mouseSensitivity, -pitchLimit, pitchLimit);

            _lookTarget = ctx.PivotPosition;

            // 기준: 로켓 앞쪽(진행방향) + elevation → 카메라가 로켓 앞에서 뒤를 본다.
            Vector3 fwd = (ctx.Facing * Vector3.forward).normalized;
            if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward;
            float e = elevationAngle * Mathf.Deg2Rad;
            Vector3 dir = fwd * Mathf.Cos(e) + Vector3.up * Mathf.Sin(e);

            // 마우스 yaw(월드 up 둘레) → 마우스 pitch(현재 dir에 수직인 축 둘레) 순으로 오프셋 적용.
            dir = Quaternion.AngleAxis(_yaw, Vector3.up) * dir;
            Vector3 pitchAxis = Vector3.Cross(Vector3.up, dir);
            if (pitchAxis.sqrMagnitude > 1e-6f)
                dir = Quaternion.AngleAxis(_pitch, pitchAxis.normalized) * dir;

            Vector3 pos = _lookTarget + dir * distance;
            Quaternion rot = Quaternion.LookRotation(-dir, LookUp); // 실제 회전은 CameraOwner가 재계산(미사용)
            return new CameraPose(pos, rot);
        }
    }
}
