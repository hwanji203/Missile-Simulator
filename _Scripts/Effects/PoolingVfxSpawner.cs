using EventChannelSystem;
using Events;
using ObjectPool.Runtime;
using UnityEngine;

namespace Effects
{
    // 채널의 ShowPoolingVfxEvent를 받아 풀에서 VFX를 꺼내 재생하고, 재생이 끝나면 풀로 반납한다.
    // (플레이어 종속이 아닌 월드 공간 VFX. 폭발 등에 사용)
    public class PoolingVfxSpawner : MonoBehaviour
    {
        [SerializeField] private EventChannelSO channel;
        [SerializeField] private PoolManagerSO poolManager;

        private void OnEnable()
        {
            if (channel != null)
                channel.AddListener<ShowPoolingVfxEvent>(HandlePlayPoolingVfx);
        }

        private void OnDisable()
        {
            if (channel != null)
                channel.RemoveListener<ShowPoolingVfxEvent>(HandlePlayPoolingVfx);
        }

        private void HandlePlayPoolingVfx(ShowPoolingVfxEvent evt)
        {
            PoolableVfx vfx = poolManager.Pop<PoolableVfx>(evt.ItemData);
            if (vfx == null) return;

            vfx.OnVfxEnd += HandleVfxEnd;
            vfx.PlayVfx(evt.Position, evt.Rotation, evt.Scale);
        }

        private void HandleVfxEnd(PoolableVfx vfx)
        {
            vfx.OnVfxEnd -= HandleVfxEnd;
            poolManager.Push(vfx);
        }
    }
}
