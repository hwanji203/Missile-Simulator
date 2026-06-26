using EventChannelSystem;
using Events;
using ObjectPool.Runtime;
using UnityEngine;

namespace Enemies.Projectiles
{
    // 채널의 SpawnHeldProjectileEvent를 받아 풀에서 투사체를 꺼내 좀비 손에 들려두고,
    // 투사체가 끝나면(명중/지형/수명/미발사 반납) 풀로 반납한다. (Effects.PoolingVfxSpawner과 동일 패턴)
    public class ProjectileSpawner : MonoBehaviour
    {
        [SerializeField] private EventChannelSO channel;
        [SerializeField] private PoolManagerSO poolManager;

        private void OnEnable()
        {
            if (channel != null)
                channel.AddListener<SpawnHeldProjectileEvent>(HandleSpawnHeld);
        }

        private void OnDisable()
        {
            if (channel != null)
                channel.RemoveListener<SpawnHeldProjectileEvent>(HandleSpawnHeld);
        }

        // 윈드업: 풀에서 꺼내 좀비 손에 들려두고, 인스턴스를 콜백으로 스킬에 돌려준다.
        // 반납 구독은 여기서 한 번만 걸어 둔다(손에서 반납하든, 발사 후 반납하든 동일 경로).
        private void HandleSpawnHeld(SpawnHeldProjectileEvent evt)
        {
            ThrownProjectile projectile = poolManager.Pop<ThrownProjectile>(evt.ItemData);
            if (projectile == null) return;

            projectile.OnReturnToPool += HandleReturn;
            projectile.HoldInHand(evt.Anchor,
                new ProjectilePrepData(evt.Attacker, evt.Damage, evt.Scale));
            evt.OnSpawned?.Invoke(projectile);
        }

        private void HandleReturn(ThrownProjectile projectile)
        {
            projectile.OnReturnToPool -= HandleReturn;
            poolManager.Push(projectile);
        }
    }
}
