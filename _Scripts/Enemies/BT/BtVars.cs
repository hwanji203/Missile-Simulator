namespace Enemies.BT
{
    public static class BtVars
    {
        public const string TargetGameObject = "TargetGameObject";
        public const string Enemy = "Enemy";
        public const string StateChannel = "StateChannel";
        public const string AnimationChannel = "AnimationChannel";

        // 플레이어가 멈추거나 터져 위협받은 상태(EnemyThreatReceiver가 켜고, FleeFromTargetAction이 끝날 때 끈다).
        // 그래프에 같은 이름(IsThreatened)의 Bool 블랙보드 변수를 만들어 둬야 한다.
        public const string IsThreatened = "IsThreatened";
    }
}
