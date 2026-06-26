using UnityEngine;

namespace Enemies
{
    /// <summary>
    /// 터레인/경사면 위에서 모델을 바닥 법선 방향으로 세운다(up = 바닥 법선).
    /// 부모(Animator 모델)의 yaw(바라보는 방향)는 유지하고 pitch/roll만 슬로프에 맞춘다.
    ///
    /// 배치: Animator 모델 아래의 메쉬 래퍼("Body") 자식에 붙인다.
    /// → Animator 트랜스폼은 그대로라 OnAnimatorMove의 rootRotation이 오염되지 않고,
    ///   루트(NavMeshAgent)는 수직을 유지한다. 이 기울임은 순수 비주얼이다.
    /// </summary>
    public class SlopeAligner : MonoBehaviour
    {
        [Header("Ground detection")]
        [SerializeField] private LayerMask groundMask = ~0;      // 바닥 레이어. 에너미 자기 레이어는 제외할 것.
        [SerializeField] private float rayOriginHeight = 1.0f;   // 발 위치에서 위로 띄운 레이 시작 높이
        [SerializeField] private float rayDistance = 3.0f;       // 아래로 쏘는 거리

        [Header("Alignment")]
        [SerializeField] private float maxSlopeAngle = 50f;      // 이 각도 이상은 클램프(과한 기울임 방지)
        [SerializeField] private float alignSharpness = 10f;     // 클수록 빠르게 정렬(프레임 독립 스무딩)
        [SerializeField] private bool align = true;
        [Tooltip("지면 법선을 다시 샘플링(레이캐스트)하는 주기(초). 회전 스무딩은 매 프레임 돈다. 0이면 매 프레임 레이캐스트.")]
        [SerializeField] private float raycastInterval = 0.1f;

        private Transform _parent;   // yaw를 공급하는 모델 트랜스폼

        // 레이캐스트는 주기마다만 하고 결과 법선을 캐시한다(마리당 매 프레임 레이캐스트 방지).
        private Vector3 _groundNormal = Vector3.up;
        private bool _hasGround;
        private float _nextRaycastTime;

        private void Awake()
        {
            _parent = transform.parent;
            Debug.Assert(_parent != null, "SlopeAligner는 모델(Animator) 아래 래퍼 자식에 붙여야 합니다.");
        }

        private void OnEnable()
        {
            // 활성화 시 현재 부모 방향으로 즉시 맞춰서 첫 프레임 튐 방지.
            if (_parent != null) transform.rotation = _parent.rotation;
            _hasGround = false;
            // 여러 마리가 같은 프레임에 몰려서 레이캐스트하지 않도록 시작 위상을 흩는다.
            _nextRaycastTime = Time.time + Random.value * raycastInterval;
        }

        private void LateUpdate()
        {
            // LateUpdate: OnAnimatorMove(루트모션)가 부모 yaw를 다 갱신한 뒤에 기울인다.
            if (Time.time >= _nextRaycastTime)
            {
                SampleGroundNormal();
                _nextRaycastTime = Time.time + raycastInterval;
            }

            Quaternion target = align && _hasGround ? ComputeAlignedRotation() : _parent.rotation;

            // 프레임 독립 지수 스무딩.
            float t = 1f - Mathf.Exp(-alignSharpness * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, t);
        }

        private void SampleGroundNormal()
        {
            Vector3 origin = _parent.position + Vector3.up * rayOriginHeight;

            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
                    rayOriginHeight + rayDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                // 바닥을 못 찾으면 수직(부모 yaw) 유지.
                _hasGround = false;
                return;
            }

            // 너무 가파른 곳은 법선을 up 쪽으로 클램프해서 과하게 눕는 걸 막는다.
            Vector3 normal = hit.normal;
            if (Vector3.Angle(Vector3.up, normal) > maxSlopeAngle)
                normal = Vector3.RotateTowards(Vector3.up, normal, maxSlopeAngle * Mathf.Deg2Rad, 0f);

            _groundNormal = normal;
            _hasGround = true;
        }

        private Quaternion ComputeAlignedRotation()
        {
            // 부모의 forward(yaw)를 슬로프 평면에 투영 → up은 법선, forward는 yaw 유지.
            // 법선은 캐시를 쓰되 yaw는 매 프레임 현재 값을 반영한다(회전 중 랙 방지).
            Vector3 projForward = Vector3.ProjectOnPlane(_parent.forward, _groundNormal);
            if (projForward.sqrMagnitude < 1e-6f)
                return _parent.rotation; // 거의 수직 표면 등 예외

            return Quaternion.LookRotation(projForward.normalized, _groundNormal);
        }
    }
}
