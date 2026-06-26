using UnityEngine;

namespace Cameras
{
    // 3인칭 하위 state가 매 프레임 읽는 런타임 컨텍스트.
    // ThirdPersonCameraModule이 구현해 IRotateMovement·입력을 한 번만 물고 자식 state에 노출한다.
    // → 자식 state는 입력 구독/주입 없이 순수 포즈 계산만 한다.
    public interface IThirdPersonContext
    {
        Vector3 PivotPosition { get; }   // 미사일 피벗(주시 대상)
        Vector3 CameraPosition { get; }  // 카메라(리그)의 현재 월드 위치 — peek가 '현재 시점'을 기준으로 빠질 때 사용
        Quaternion Facing { get; }       // 미사일이 실제 보는 회전
        Vector2 LookDelta { get; }       // 이번 프레임 마우스 이동량 (x=yaw, y=pitch). 둘러보기/시점 회전 입력

        // 폭발 진입 시 카메라 후퇴 강도 배수(1=기본). GroundedThirdPersonState가 Explode 거리에 적용.
        float ExplodeScale { get; }
    }

    // 3인칭의 하위 상태(자식 컴포넌트). 자기 포즈를 계산하고, 시한부면 종료를 알린다.
    // CameraOwner는 이 값들을 ThirdPersonCameraModule(façade)을 통해 ICameraBehaviour/ICameraLookAt로 받는다.
    public interface IThirdPersonCameraState
    {
        // 상황 진입. fresh=true(팔로우→3인칭 첫 진입)면 방향을 새로 래치/타이머를 리셋한다.
        void Enter(IThirdPersonContext ctx, ThirdPersonSituation situation, bool fresh);

        // 매 프레임 목표 포즈 계산(타이머/거리/공전 적분 포함). 같은 프레임에 LookTarget/LookUp도 갱신한다.
        CameraPose Tick(IThirdPersonContext ctx, float deltaTime);

        Vector3 LookTarget { get; }   // CameraOwner가 '실제 위치'에서 이 점을 주시 → 대상이 항상 화면 중앙
        Vector3 LookUp { get; }       // 주시 시 상단(up) 기준
        float Damping { get; }        // CameraOwner 오프셋 댐핑 (<=0이면 스냅)
        bool IsFinished { get; }      // 시한부 state 종료 시 true (영구 state는 항상 false)
    }
}
