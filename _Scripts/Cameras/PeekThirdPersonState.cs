using UnityEngine;

namespace Cameras
{
    // 신규 'peek' 3인칭: 게임 이벤트로 쏙 빠졌다가 일정 시간 뒤 제자리로 돌아온다.
    // 공전 없음. 진입 시 '현재 카메라 시점'(피벗→카메라 방향·거리)을 래치하고,
    // 그 방향 그대로 거리만 distanceCurve(시간) * peekDistance 만큼 더 빠졌다(dolly-out) 돌아온다.
    // → 1인칭으로 튀지 않고 현재 보던 시점 그대로 살짝 멀어졌다 복귀한다.
    // 커브 하나로 '빠짐 → 유지 → 복귀'를 전부 디자인한다. 마지막 키 시간에 도달하면 IsFinished=true
    // → CameraModeModule이 기본 시점(팔로우/쿼터뷰)으로 복귀시킨다.
    // Damping=0(스냅): 부드러움은 전적으로 커브가 담당하고, pivot 추종이라 빠른 로켓에도 안 뒤처진다.
    public class PeekThirdPersonState : MonoBehaviour, IThirdPersonCameraState
    {
        [Tooltip("현재 시점에서 추가로 빠지는 최대 거리(커브 ratio 1일 때).")]
        [SerializeField] private float peekDistance = 7f;

        [Tooltip("x=시간(초), y=추가 후퇴 비율(0=현재 시점, 1=peekDistance만큼 더 빠짐). 빠짐→유지→복귀 모양으로.")]
        [SerializeField] private AnimationCurve distanceCurve = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.4f, 1f), new Keyframe(2f, 1f), new Keyframe(2.4f, 0f));

        private float _time;
        private float _duration;
        private bool _finished;
        private Vector3 _startDir = Vector3.back; // 진입 시 래치한 피벗→카메라 방향(현재 시점)
        private float _startDist;                 // 진입 시 피벗→카메라 거리(현재 시점)
        private Vector3 _lookTarget;

        public Vector3 LookTarget => _lookTarget;
        public Vector3 LookUp => Vector3.up;
        public float Damping => 0f;     // 스냅 — 부드러움은 커브가 담당
        public bool IsFinished => _finished;

        public void Enter(IThirdPersonContext ctx, ThirdPersonSituation situation, bool fresh)
        {
            _time = 0f;
            _finished = false;
            _duration = distanceCurve.length > 0 ? distanceCurve[distanceCurve.length - 1].time : 0f;

            // 현재 카메라 시점(피벗 기준 오프셋)을 래치. 이 방향·거리에서 출발해 그대로 빠졌다 돌아온다.
            Vector3 offset = ctx.CameraPosition - ctx.PivotPosition;
            _startDist = offset.magnitude;
            if (_startDist > 0.01f)
            {
                _startDir = offset / _startDist;
            }
            else
            {
                // 카메라가 피벗에 거의 붙은 경우(예외)엔 수평 '뒤'를 기준으로 폴백.
                Vector3 fwd = ctx.Facing * Vector3.forward;
                Vector3 flat = Vector3.ProjectOnPlane(fwd, Vector3.up);
                if (flat.sqrMagnitude < 0.0001f) flat = Vector3.forward;
                _startDir = -flat.normalized;
                _startDist = 0f;
            }
        }

        public CameraPose Tick(IThirdPersonContext ctx, float deltaTime)
        {
            _time += deltaTime;
            if (_time >= _duration) _finished = true;

            float ratio = distanceCurve.Evaluate(_time);
            _lookTarget = ctx.PivotPosition;

            // 현재 시점 거리 + 커브만큼 추가 후퇴 → 같은 방향으로 dolly-out 했다가 복귀.
            Vector3 pos = _lookTarget + _startDir * (_startDist + ratio * peekDistance);
            Quaternion rot = Quaternion.LookRotation(-_startDir, LookUp); // 실제 회전은 CameraOwner가 재계산(미사용)
            return new CameraPose(pos, rot);
        }
    }
}
