using EventChannelSystem;
using UnityEngine;
using Enemies;

namespace Events
{
    public static class EnemyEvents
    {
        public static readonly ZombiePowerUpEvent ZombiePowerUpEvent = new ZombiePowerUpEvent();
        public static readonly ZombieKilledEvent ZombieKilledEvent = new ZombieKilledEvent();
    }

    // 미사일이 '날아오는 좀비'(파워업 좀비 투사체)를 맞췄을 때 발행. 플레이어 파워업의 트리거.
    // (구 링 시스템 대체. 실제 파워업 효과 연결은 추후 — 지금은 이벤트만 발행한다.)
    public class ZombiePowerUpEvent : GameEvent
    {
        public Vector3 Position { get; private set; }

        public ZombiePowerUpEvent InitData(Vector3 position)
        {
            Position = position;
            return this;
        }
    }

    public class ZombieKilledEvent : GameEvent
    {
        public ZombieTierSO Tier { get; private set; }

        public ZombieKilledEvent Init(ZombieTierSO tier)
        {
            Tier = tier;
            return this;
        }
    }
}
