using ModuleSystem;
using UnityEngine;

namespace Players.Movement
{
    // Root는 회전시키지 않고, 비물리 자식인 Visual을 피벗 기준으로 회전시킨다 (물리와 충돌 없음).
    public class MissileRotation : MonoBehaviour, IModule, IRotateMovement
        , IStoppableMovement, IContactFreezable
    {
        [SerializeField] private float damping = 6.25f;
        [SerializeField] private float rotationMultiplier = 30f;
        [Tooltip("yaw 회전 속도(deg/s) 대비 roll이 도는 속력 비율 (계속 누적 회전)")]
        [SerializeField] private float rollSpeed = 2f;
        [Tooltip("roll 속력 가감속 / 90° 정착 속도 (클수록 민첩)")]
        [SerializeField] private float rollDamping = 5f;
        [SerializeField] private Transform rootTrm;
        [Tooltip("pivot을 따로 설정하기 위해선 root를 직접 돌려선 안된다.")]
        [SerializeField] private Transform visualTrm;
        [SerializeField] private float cameraDistance = 3.5f;

        public Quaternion FacingRotation => visualTrm.rotation;

        public Vector3 PivotPosition =>
            rootTrm.TransformPoint(PivotLocalPosition);
        private Vector3 PivotLocalPosition => visualTrm.localPosition + visualTrm.localRotation * _pivotLocalOffset;
        public Quaternion DesiredRotation { get; private set; }
        public bool IsRollSettling => _settling;
        public bool HasRotationInput => _hasInput;
        public float RollAngle => _roll;
        
        private float _yaw;
        private float _pitch;
        private float _roll;          // 누적 roll 각도 (계속 회전, 360 넘김)
        private float _rollVel;       // 현재 roll 각속도 (deg/s)
        private float _rollTarget;    // 정지 시 정착할 가장 가까운 90° 배수 목표
        private bool _settling;       // yaw 정지 후 90° 정착 중인지
        private bool _hasInput;       // 이번 프레임 회전 입력(yaw 또는 pitch) 여부
        private IMovement _movement;  // 부스터 활성 동안 회전 차단용
        private Rigidbody _rb;        // 정지 후 이동 방향을 읽기 위한 Root의 Rigidbody
        private Vector3 _pivotLocalOffset; // Visual 원점에서 회전 피벗까지의 로컬 오프셋
        private bool _stopped;
        private bool _frozen;          // 땅/적 충돌 후 회전 자체를 완전히 멈춤

        public void Initialize(ModuleOwner owner)
        {
            _movement = owner.GetModule<IMovement>();
            _rb = owner.GetComponent<Rigidbody>();

            // 회전 피벗을 Root 로컬좌표로 한 번 계산 (카메라 오프셋 고정 전제)
            Transform camTrm = Camera.main.transform;
            Vector3 camLocal = rootTrm.InverseTransformPoint(camTrm.position);
            DesiredRotation = Quaternion.LookRotation(visualTrm.forward, camTrm.up);

            _pivotLocalOffset = new Vector3(0f, 0f, camLocal.z + cameraDistance);
        }

        public void StopMovement()
        {
            _stopped = true;
        }

        // 땅/적과 충돌하는 순간 호출: 이후 추락 회전·입력 회전 등 모든 회전을 중단한다.
        public void Freeze()
        {
            _frozen = true;
        }

        public void UpdateDesiredRotation(Vector3 scaledRotationInput)
        {
            // 슈퍼 부스트 활성 중(키 홀드와 무관하게 실제 돌진 중)에는 회전 전체를 차단한다.
            // yaw/pitch/roll 정착 포함 모두 동결 — DesiredRotation 현재값 유지.
            if (_movement.IsSuperBoosting || _stopped)
            {
                _hasInput = false;
                return;
            }

            _hasInput = Mathf.Abs(scaledRotationInput.x) > 0.001f || Mathf.Abs(scaledRotationInput.y) > 0.001f;

            // 월드 기준 yaw축 회전 속도 (deg/s)
            float yawSpeed = scaledRotationInput.x * rotationMultiplier;

            _yaw   += yawSpeed * Time.deltaTime;
            _pitch -= scaledRotationInput.y * rotationMultiplier * Time.deltaTime;
            _pitch = Mathf.Clamp(_pitch, -89, 75f);

            UpdateRoll(scaledRotationInput.x, yawSpeed);

            DesiredRotation = Quaternion.Euler(_pitch, _yaw, _roll);
        }

        private void Update()
        {
            // 충돌로 동결되면 어떤 회전도 하지 않는다(추락 회전 포함).
            if (_frozen) return;

            // 정지 후에는 실제 이동(속도) 방향을 바라보도록 계속 회전시킨다.
            if (_stopped)
                UpdateFallRotation();

            ApplyRotation();
        }

        // 멈춘 뒤(중력 추락 등) 현재 속도 방향을 DesiredRotation으로 삼는다.
        private void UpdateFallRotation()
        {
            Vector3 velocity = _rb.linearVelocity;
            if (velocity.sqrMagnitude < 0.0001f) return; // 거의 멈췄으면 기존 방향 유지

            Quaternion worldLook = Quaternion.LookRotation(velocity.normalized, Vector3.up);
            DesiredRotation = worldLook;
        }

        // yaw 속도에 비례한 속력으로 roll을 계속 누적 회전시키고,
        // yaw가 멈추면 진행 방향의 "다음 90° 배수"로 부드럽게 정착시킨다.
        // (미사일은 90°마다 동일 형상이라 90 단위 정착이 자연스럽고, 반대로는 돌지 않는다.)
        private void UpdateRoll(float yawInput, float yawSpeed)
        {
            const float inputDeadzone = 0.001f;

            if (Mathf.Abs(yawInput) > inputDeadzone)
            {
                // 회전 중: 목표 속력으로 가감속하며 계속 누적
                float targetRollVel = -yawSpeed * rollSpeed;
                _rollVel = Mathf.Lerp(_rollVel, targetRollVel, 1f - Mathf.Exp(-rollDamping * Time.deltaTime));
                _roll += _rollVel * Time.deltaTime;
                _settling = false;
                return;
            }

            // 정지: 진입 시 가장 가까운 90° 배수를 목표로 고정 (진행 방향 등 무관)
            if (!_settling)
            {
                _settling = true;
                _rollTarget = Mathf.Round(_roll / 90f) * 90f;
            }

            // 점점 느려져 90° 배수에 정착
            _roll = Mathf.SmoothDamp(_roll, _rollTarget, ref _rollVel, 1f / Mathf.Max(rollDamping, 0.0001f));
        }

        // 비물리 자식인 Visual을 피벗 기준으로 회전시킨다.
        private void ApplyRotation()
        {
            if (Quaternion.Angle(visualTrm.localRotation, DesiredRotation) < 0.01f) return;

            float t = 1f - Mathf.Exp(-damping * Time.deltaTime);
            // 회전 전(prevRot) 기준 피벗을 먼저 캐시해야 보정이 성립한다.
            Vector3 pivotLocal = PivotLocalPosition;
            Quaternion newRot = Quaternion.Slerp(visualTrm.localRotation, DesiredRotation, t);

            // 피벗 점이 회전 동안 제자리에 있도록 Visual 로컬 위치를 보정 → 피벗 기준 회전
            visualTrm.localRotation = newRot;
            visualTrm.localPosition = pivotLocal - newRot * _pivotLocalOffset;
        }

        // 회전 피벗(축)을 씬 뷰에 표시 (플레이 중, 오브젝트 선택 시)
        private void OnDrawGizmosSelected()
        {
            if (rootTrm == null || visualTrm == null) return;

            Vector3 pivot = PivotPosition;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pivot, 0.3f);

            Gizmos.color = Color.green;                                              // yaw 축
            Gizmos.DrawLine(pivot - visualTrm.up * 2f, pivot + visualTrm.up * 2f);
            Gizmos.color = Color.red;                                                // pitch 축
            Gizmos.DrawLine(pivot - visualTrm.right * 2f, pivot + visualTrm.right * 2f);
        }
    }
}
