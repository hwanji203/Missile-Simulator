namespace Core
{
    public struct TierKillResult
    {
        public string TierName;
        public int    Kills;
        public int    RewardEach;
        public int    Subtotal => Kills * RewardEach;
    }
}
