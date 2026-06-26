using AnimationSystem;
using Agents;
using ModuleSystem;
using UnityEngine;
using UnityEngine.AI;

namespace Enemies
{
    public class NavAgentRenderer : AgentRenderer, IAfterInitModule
    {
        [SerializeField] private AnimParamSO forwardSpeedParam;
        [SerializeField] private AnimParamSO sideSpeedParam;

        [Header("Navigation Agent control")]
        [SerializeField] private bool updateRotation;
        [SerializeField] private bool updatePosition;

        [Header("Force rotation settings")]
        [SerializeField] private bool forceRotation;
        [SerializeField] private float forceRotationSpeed;

        [Header("Animation smoothing")]
        [Tooltip("속도 파라미터가 목표값에 도달하는 데 걸리는 대략적인 시간(초). 클수록 부드럽고 반응이 느려진다.")]
        [SerializeField] private float speedSmoothTime = 0.12f;

        private INavMovement _navMovement;
        private NavMeshAgent _navAgent;
        private Vector2 _velocity;

        public bool IsUpdateRotationByAnimator
        {
            get => !updateRotation; //이게 켜져있으면 NavAgent가 처리한다.
            set
            {
                updateRotation = !value;
                if (_navAgent != null)
                {
                    _navAgent.updateRotation = updateRotation; //갱신한다.
                }
            }
        }

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _navMovement = owner.GetModule<INavMovement>();
            Debug.Assert(_navMovement != null, "NavAgentRenderer는 INavMovement가 필요합니다.");
        }

        public void AfterInit()
        {
            _navAgent = _navMovement.NavMeshAgent; //이건 AfterInit에서 해야해.
            _navAgent.updatePosition = updatePosition;
            _navAgent.updateRotation = updateRotation;
        }

        private void OnAnimatorMove()
        {
            if (_navAgent == null || Time.timeScale == 0) return;

            Vector3 rootPosition = Animator.rootPosition;
            rootPosition.y = _navAgent.nextPosition.y;

            if (NavMesh.SamplePosition(rootPosition, out NavMeshHit hit, 0.4f, NavMesh.AllAreas))
            {
                Owner.transform.position = rootPosition;
                _navAgent.nextPosition = hit.position;
            }

            if (IsUpdateRotationByAnimator)
                Owner.transform.rotation = Animator.rootRotation;
        }

        private void Update()
        {
            SynchronizeAnimatorAndNavAgent();
            ForceRotationControl();
        }

        private void SynchronizeAnimatorAndNavAgent()
        {
            if (_navAgent == null) return;

            Vector2 targetVelocity = CalculateTargetVelocity();

            //프레임레이트와 무관한 지수 감쇠 스무딩. 파라미터가 한 프레임 만에 튀지 않는다.
            float smooth = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(speedSmoothTime, 0.0001f));
            _velocity = Vector2.Lerp(_velocity, targetVelocity, smooth);

            //정지 직전의 미세한 잔값이 2D Simple Directional의 방향을 튀게 하지 않도록 스냅.
            if (targetVelocity == Vector2.zero && _velocity.sqrMagnitude < 0.0025f)
                _velocity = Vector2.zero;

            Animator.SetFloat(forwardSpeedParam.ParamHash, _velocity.y);
            Animator.SetFloat(sideSpeedParam.ParamHash, _velocity.x);

            //루트모션이 NavMesh 밖으로 나가는 등 에이전트와 너무 벌어진 경우에만 위치를 되돌린다.
            Vector3 worldDeltaPosition = _navAgent.nextPosition - Owner.transform.position;
            worldDeltaPosition.y = 0;
            float threshold = _navAgent.radius * 0.5f;
            if (worldDeltaPosition.sqrMagnitude > threshold * threshold)
            {
                Owner.transform.position = Vector3.Lerp(Owner.transform.position, _navAgent.nextPosition, smooth);
            }
        }

        //에이전트가 '가려고 하는' 속도(desiredVelocity)를 speed로 정규화해 블렌드트리 좌표(±1)로 변환.
        //실제 이동 델타를 역측정하던 방식은 루트모션과 피드백 루프를 만들어 속도가 진동했다.
        private Vector2 CalculateTargetVelocity()
        {
            if (_navAgent.isStopped || !_navAgent.hasPath)
                return Vector2.zero;

            Vector3 desired = _navAgent.desiredVelocity;
            desired.y = 0;

            float maxSpeed = Mathf.Max(_navAgent.speed, 0.01f);
            Vector2 target = new Vector2(
                Vector3.Dot(Owner.transform.right, desired),
                Vector3.Dot(Owner.transform.forward, desired)) / maxSpeed;

            //도착 직전엔 남은 거리에 비례해 자연스럽게 감속.
            //pathPending 동안엔 remainingDistance가 0을 반환하므로 감속하지 않는다
            //(BT가 SetDestination을 다시 호출할 때마다 속도가 0으로 떨어지던 원인).
            if (!_navAgent.pathPending)
            {
                float stopping = Mathf.Max(_navAgent.stoppingDistance, 0.01f);
                if (_navAgent.remainingDistance <= stopping)
                    target *= Mathf.Clamp01(_navAgent.remainingDistance / stopping);
            }

            return Vector2.ClampMagnitude(target, 1f);
        }

        private void ForceRotationControl()
        {
            if (!forceRotation || _navAgent == null || _navMovement.IsArrived) return;

            Vector3 desiredDirection = _navAgent.steeringTarget - Owner.transform.position;
            if (desiredDirection.sqrMagnitude < 0.01f) return; //거의 정지면 회전 안함.

            desiredDirection.y = 0;
            Quaternion desiredRotation = Quaternion.LookRotation(desiredDirection);
            Owner.transform.rotation = Quaternion.RotateTowards(
                Owner.transform.rotation, desiredRotation, forceRotationSpeed * Time.deltaTime);
        }
    }
}
