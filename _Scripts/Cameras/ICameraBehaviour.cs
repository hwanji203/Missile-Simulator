using UnityEngine;

namespace Cameras
{
    // 카메라가 한 프레임에 있어야 할 위치/회전.
    public readonly struct CameraPose
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;

        public CameraPose(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }

    // 카메라 행동(1인칭 팔로우 / 3인칭 등)은 transform을 직접 건드리지 않고
    // "원하는 목표 포즈"만 계산해 돌려준다. 실제 적용/댐핑은 CameraOwner가 단독으로 한다.
    public interface ICameraBehaviour
    {
        CameraPose GetDesiredPose();
        float Damping { get; } // 0 이하면 즉시 스냅
        float RotationDamping { get; } // 0 이하면 즉시 스냅
    }

    // 이 행동은 회전을 GetDesiredPose의 값이 아니라, '댐핑이 적용된 실제 카메라 위치'에서
    // LookTarget을 바라보도록 CameraOwner가 재계산해 적용한다.
    // → 위치가 목표보다 뒤처져 있어도 대상이 항상 화면 중앙에 오므로 공전 시 흔들림이 없다.
    public interface ICameraLookAt
    {
        Vector3 LookTarget { get; }
        Vector3 LookUp { get; }   // 대상을 바라볼 때 쓸 상단(up) 기준. 보통 월드 up, 돌진 추격 등에선 회전시킨 up.
    }
}
