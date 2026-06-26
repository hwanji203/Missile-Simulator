using UnityEngine;

namespace Players.Movement
{
    public interface IRotateMovement
    {
        public Quaternion FacingRotation { get; }   // 비주얼이 실제로 바라보는 방향
        public Quaternion DesiredRotation { get; }   // 비주얼이 실제로 바라보는 방향
        public Vector3 PivotPosition { get; }
        public bool IsRollSettling { get; }          // yaw 멈추고 가장 가까운 90°로 정착 중인지
        public bool HasRotationInput { get; }        // 회전 입력(yaw 또는 pitch)이 있는지
        public float RollAngle { get; }              // 누적 roll 각도(연속, 쿼터니언 추출 없이 그대로)

        public void UpdateDesiredRotation(Vector3 scaledRotationInput);
    }
}
