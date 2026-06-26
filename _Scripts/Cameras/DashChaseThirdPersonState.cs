using UnityEngine;

namespace Cameras
{
    // 슈퍼 부스트 3인칭: 진입 시 3D 돌진 방향과 로켓 up을 평탄화 없이 1회 래치한다.
    // 돌진 축 기준 꽁무니 뒤 + elevation에 앵커링하되(위/아래 어디로 돌진해도 꽁무니를 봄),
    // yaw 공전은 월드 up 둘레(수평 턴테이블)로 돈다 → 돌진 축이 기울어도 기운 원(계란 모양)이 안 된다.
    // 영구 — 스스로 끝나지 않는다(착지 등 외부 전환으로만 빠져나감).
    public class DashChaseThirdPersonState : MonoBehaviour, IThirdPersonCameraState
    {
        [SerializeField] private float distance = 8f;

        [Tooltip("돌진 축 뒤에서 들어올리는 각도(도)")]
        [SerializeField] private float elevationAngle = 30f;

        [Tooltip("마우스 이동량당 공전 각도(deg/마우스 단위)")]
        [SerializeField] private float orbitYawSensitivity = 0.15f;

        [Tooltip("오프셋 댐핑 (CameraOwner가 진입 dolly-out에 사용)")]
        [SerializeField] private float damping = 5f;

        private float _orbitYaw;
        private Vector3 _baseDir = Vector3.forward; // 진입 시 래치한 3D 돌진 방향
        private Vector3 _baseUp = Vector3.up;       // 진입 시 래치한 로켓 up
        private Vector3 _lookTarget;

        public Vector3 LookTarget => _lookTarget;
        public Vector3 LookUp => Vector3.up;
        public float Damping => damping;
        public bool IsFinished => false;

        public void Enter(IThirdPersonContext ctx, ThirdPersonSituation situation, bool fresh)
        {
            if (fresh)
            {
                _orbitYaw = 0f;
                _baseDir = (ctx.Facing * Vector3.forward).normalized;
                _baseUp = (ctx.Facing * Vector3.up).normalized;
            }
        }

        public CameraPose Tick(IThirdPersonContext ctx, float deltaTime)
        {
            _orbitYaw += ctx.LookDelta.x * orbitYawSensitivity;
            _lookTarget = ctx.PivotPosition;

            float e = elevationAngle * Mathf.Deg2Rad;
            Vector3 d0 = -_baseDir * Mathf.Cos(e) + _baseUp * Mathf.Sin(e); // yaw=0: 3D 꽁무니 뒤
            Vector3 dir = Quaternion.AngleAxis(_orbitYaw, Vector3.up) * d0;

            Vector3 pos = _lookTarget + dir * distance;
            Quaternion rot = Quaternion.LookRotation(-dir, LookUp); // 실제 회전은 CameraOwner가 재계산(미사용)
            return new CameraPose(pos, rot);
        }
    }
}
