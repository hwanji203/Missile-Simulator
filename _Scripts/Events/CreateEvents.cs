using System;
using EventChannelSystem;
using ModuleSystem;
using ObjectPool.Runtime;
using UnityEngine;

namespace Events
{
    public static class CreateEvents
    {
        public static readonly ShowPoolingVfxEvent ShowPoolingVfxEvent = new ShowPoolingVfxEvent();
        public static readonly SpawnHeldProjectileEvent SpawnHeldProjectileEvent = new SpawnHeldProjectileEvent();
    }

    public class ShowPoolingVfxEvent : GameEvent
    {
        public PoolItemSO ItemData { get; private set; }
        public Vector3 Position { get; private set; }
        public Quaternion Rotation { get; private set; }
        public float Scale { get; private set; } // 1=원본 크기. 메인 폭발만 반경 배수로 키운다.

        public ShowPoolingVfxEvent InitData(PoolItemSO itemData, Vector3 position, Quaternion rotation, float scale = 1f)
        {
            ItemData = itemData;
            Position = position;
            Rotation = rotation;
            Scale = scale;
            return this;
        }
    }

// 풀에서 투사체를 꺼내 좀비 "손"에 들려두라는 명령(윈드업 시점). ProjectileSpawner가 받는다.
// 발사 속도는 아직 모르므로 넘기지 않고, 스폰된 인스턴스를 OnSpawned 콜백으로 스킬에 돌려준다.
// (Events 어셈블리가 Enemies를 참조하지 않도록 콜백 타입은 IPoolable로 둔다 → 스킬에서 캐스팅.)
    public class SpawnHeldProjectileEvent : GameEvent
    {
        public PoolItemSO ItemData { get; private set; }
        public Transform Anchor { get; private set; }
        public ModuleOwner Attacker { get; private set; }
        public float Damage { get; private set; }
        public float Scale { get; private set; }
        public Action<IPoolable> OnSpawned { get; private set; }

        public SpawnHeldProjectileEvent InitData(PoolItemSO itemData, Transform anchor,
            ModuleOwner attacker, float damage, float scale, Action<IPoolable> onSpawned)
        {
            ItemData = itemData;
            Anchor = anchor;
            Attacker = attacker;
            Damage = damage;
            Scale = scale;
            OnSpawned = onSpawned;
            return this;
        }
    }
}