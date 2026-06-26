namespace Players.Movement
{
    // 폭발 등으로 미사일의 이동/회전을 멈춰야 할 때 호출되는 정지 인터페이스.
    public interface IStoppableMovement
    {
        void StopMovement();
    }
}
