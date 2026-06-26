using UnityEngine;

namespace Cameras
{
    // 정지/폭발 3인칭: 진입 시 미사일이 보던 수평 '뒤'를 1회 래치하고, 그 뒤 + elevation 위에서 주시한다.
    // 이후 미사일이 회전해도 카메라는 휩쓸리지 않고, yaw 입력으로만 월드 up 둘레를 공전한다.
    // 정지/폭발은 거리만 다르다(같은 로직, 다른 거리). 영구 — 스스로 끝나지 않는다.
    public class GroundedThirdPersonState : MonoBehaviour, IThirdPersonCameraState
    {
        [Header("상황별 거리")]
        [SerializeField] private float stopDistance = 6f;
        [SerializeField] private float explodeDistance = 10f;
        [Tooltip("폭발 반경 배수에 따라 후퇴 거리를 더 늘리는 계수. 0=고정, 1=파티클과 동일 비율")]
        [SerializeField] private float explodePullFactor = 1f;

        [Header("포즈")]
        [Tooltip("미사일 위로 올라가는 각도(도)")]
        [SerializeField] private float elevationAngle = 30f;

        [Header("둘러보기 (yaw 공전)")]
        [Tooltip("마우스 이동량당 공전 각도(deg/마우스 단위)")]
        [SerializeField] private float orbitYawSensitivity = 0.15f;

        [Tooltip("거리/오프셋 댐핑 (CameraOwner가 사용)")]
        [SerializeField] private float damping = 5f;

        private float _targetDistance;
        private float _distance;
        private float _orbitYaw;
        private Vector3 _baseBack = Vector3.back;
        private Vector3 _lookTarget;

        public Vector3 LookTarget => _lookTarget;
        public Vector3 LookUp => Vector3.up;
        public float Damping => damping;
        public bool IsFinished => false;

        public void Enter(IThirdPersonContext ctx, ThirdPersonSituation situation, bool fresh)
        {
            if (situation == ThirdPersonSituation.Explode)
                // 폭발이 클수록(ExplodeScale↑) 더 멀리 빠진다. explodePullFactor로 강도 조절.
                _targetDistance = explodeDistance * (1f + (ctx.ExplodeScale - 1f) * explodePullFactor);
            else
                _targetDistance = stopDistance;

            if (fresh)
            {
                _distance = _targetDistance;
                _orbitYaw = 0f;
                LatchDirection(ctx);
            }
        }

        // 현재 미사일이 보는 수평 방향의 '뒤'를 1회 고정. 이후 미사일이 돌아도 카메라는 안 휩쓸린다.
        private void LatchDirection(IThirdPersonContext ctx)
        {
            Vector3 fwd = ctx.Facing * Vector3.forward;
            Vector3 flat = Vector3.ProjectOnPlane(fwd, Vector3.up);
            if (flat.sqrMagnitude < 0.0001f) flat = Vector3.forward;
            _baseBack = -flat.normalized;
        }

        public CameraPose Tick(IThirdPersonContext ctx, float deltaTime)
        {
            _orbitYaw += ctx.LookDelta.x * orbitYawSensitivity;
            _distance = Mathf.Lerp(_distance, _targetDistance, 1f - Mathf.Exp(-damping * deltaTime));

            _lookTarget = ctx.PivotPosition;
            Vector3 back = Quaternion.AngleAxis(_orbitYaw, Vector3.up) * _baseBack;
            float e = elevationAngle * Mathf.Deg2Rad;
            Vector3 dir = back * Mathf.Cos(e) + Vector3.up * Mathf.Sin(e);

            Vector3 pos = _lookTarget + dir * _distance;
            Quaternion rot = Quaternion.LookRotation(-dir, LookUp); // 실제 회전은 CameraOwner가 재계산(미사용)
            return new CameraPose(pos, rot);
        }
    }
}
