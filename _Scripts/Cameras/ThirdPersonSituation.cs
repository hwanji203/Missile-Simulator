namespace Cameras
{
    // 3인칭이 되는 상황. 상황마다 카메라 거리(등)만 달라진다.
    public enum ThirdPersonSituation
    {
        Stop,        // 미사일이 멈춤(접촉 또는 타이머 종료 중 먼저)
        Explode,     // 폭발 — 더 멀리 빠짐
        SuperBoost,  // 슈퍼 부스트 돌진
        Peek,        // 게임 이벤트로 쏙 나왔다가 일정 시간 뒤 다시 들어가는 시한부 연출
        LookBack     // Shift 홀드 — 비행 중 로켓 앞으로 가 뒤를 돌아본다(마우스로 시점 회전)
    }
}
