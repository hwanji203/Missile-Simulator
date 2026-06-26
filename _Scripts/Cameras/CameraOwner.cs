using ModuleSystem;
using UnityEngine;

namespace Cameras
{
    // 카메라 리그의 ModuleOwner이자 유일한 transform 적용자.
    // 매 LateUpdate에 활성 행동(CameraModeModule이 결정)의 목표 포즈로 카메라를 댐핑시킨다.
    // 전환·거리 변경의 부드러움은 전부 여기 한 곳의 댐핑에서 나온다.
    public class CameraOwner : ModuleOwner
    {
        private CameraModeModule _mode;
        private CameraShakeModule _shake;   // 선택: 있으면 베이스 포즈 위에 흔들림을 덧씌운다.
        private Transform _trm;
        private ICameraBehaviour _prevBehaviour;
        private Vector3 _offset;   // look-at 행동에서 '대상(pivot) 기준' 카메라 오프셋. 댐핑은 여기에만 건다.

        private bool _frozenCaptured;     // 정지 시 월드 포즈를 한 번만 캡처
        private Vector3 _frozenPos;
        private Quaternion _frozenRot;

        protected override void Awake()
        {
            base.Awake();
            _trm = transform;
            _mode = GetModule<CameraModeModule>();
            _shake = GetModule<CameraShakeModule>();
            Debug.Assert(_mode != null, "CameraOwner는 CameraModeModule이 필요합니다.");
        }

        private void LateUpdate()
        {
            if (_mode == null) return;

            // 정지(수중 추락 등): 더 이상 플레이어를 따라가지 않고 정지 시점 월드 포즈에 고정한다.
            // early-return만 하면 리그가 움직이는 부모의 자식일 때 부모를 따라 흘러가므로,
            // 캡처한 월드 좌표/회전을 매 프레임 재적용해 완전히 못 박는다.
            if (_mode.Frozen)
            {
                if (!_frozenCaptured)
                {
                    _frozenPos = _trm.position;
                    _frozenRot = _trm.rotation;
                    _frozenCaptured = true;
                }
                _trm.SetPositionAndRotation(_frozenPos, _frozenRot);
                return;
            }

            ICameraBehaviour behaviour = _mode.ActiveBehaviour;
            if (behaviour == null) return;

            // 행동 인스턴스가 막 바뀐 프레임인지 (예: 팔로우→3인칭 첫 진입)
            bool justEntered = behaviour != _prevBehaviour;
            bool prevWasLookAt = _prevBehaviour is ICameraLookAt; // 직전도 look-at(쿼터뷰/peek 등)이었나
            _prevBehaviour = behaviour;

            CameraPose pose = behaviour.GetDesiredPose();

            // look-at 행동(3인칭): 위치를 '대상(pivot) + 오프셋'으로 잡는다.
            // pivot은 매 프레임 미사일을 그대로 따라가므로(자식처럼) 로켓이 아무리 빨라도 뒤처지지 않고,
            // 댐핑은 오프셋(거리/진입 dolly-out/공전)에만 걸린다. 회전은 실제 위치에서 대상 주시로 재계산.
            if (behaviour is ICameraLookAt lookAt)
            {
                Vector3 center = lookAt.LookTarget;
                Vector3 targetOffset = pose.Position - center;

                // 1인칭(non-look-at)에서 막 3인칭 look-at으로 들어올 때만 오프셋 0(=미사일에 붙음)에서
                // 시작 → 댐핑으로 목표 거리까지 dolly-out. look-at → look-at(예: peek↔쿼터뷰)은
                // 현재 오프셋을 유지해 미사일로 스냅하지 않고 끊김 없이 잇는다.
                if (justEntered && !prevWasLookAt) _offset = Vector3.zero;

                _offset = behaviour.Damping <= 0f
                    ? targetOffset
                    : Vector3.Lerp(_offset, targetOffset,
                        1f - Mathf.Exp(-behaviour.Damping * Time.deltaTime));

                _trm.position = center + _offset;

                Vector3 toTarget = center - _trm.position;
                if (toTarget.sqrMagnitude > 1e-6f)
                    _trm.rotation = Quaternion.LookRotation(toTarget, lookAt.LookUp);

                ApplyShake();
                return;
            }

            // 그 외(1인칭 팔로우 등): 절대 위치/회전 댐핑.
            _trm.position = behaviour.Damping <= 0f
                ? pose.Position
                : Vector3.Lerp(_trm.position, pose.Position,
                    1f - Mathf.Exp(-behaviour.Damping * Time.deltaTime));

            _trm.rotation = behaviour.RotationDamping <= 0f
                ? pose.Rotation
                : Quaternion.Slerp(_trm.rotation, pose.Rotation,
                    1f - Mathf.Exp(-behaviour.RotationDamping * Time.deltaTime));

            ApplyShake();
        }

        // 베이스 포즈가 정해진 뒤, 흔들림을 카메라 로컬 기준으로 덧씌운다.
        // 위치는 로컬 축으로 오프셋, 회전은 전방축(roll) 기준으로 기울인다. 프레임당 한 번만 호출된다.
        private void ApplyShake()
        {
            if (_shake == null) return;

            _shake.Tick(Time.deltaTime);
            if (!_shake.IsShaking) return;

            _trm.position += _trm.rotation * _shake.PositionOffset;
            _trm.rotation *= Quaternion.AngleAxis(_shake.RollAngle, Vector3.forward);
        }
    }
}
