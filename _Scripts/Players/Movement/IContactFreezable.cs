namespace Players.Movement
{
    // 땅/적 등과 충돌하는 순간, 더 이상 어떤 변환(회전 등)도 하지 않도록 동결시키는 인터페이스.
    // 정지(IStoppableMovement)와 달리, 충돌 접촉 시점에 한 번 호출되어 이후 갱신을 완전히 멈춘다.
    public interface IContactFreezable
    {
        void Freeze();
    }
}
