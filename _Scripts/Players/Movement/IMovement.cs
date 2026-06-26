namespace Players.Movement
{
    public interface IMovement
    {
        public float CurrentSpeed { get; } // 현재 실제 이동 속력 (m/s)
        public bool IsSuperBoosting { get; } // 슈퍼 부스트(취소 불가 직선 돌진) 활성 중인지
        
        public bool TryStartSuperBoost();
    }
}
