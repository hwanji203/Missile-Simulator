using System;
using System.Collections;
using ObjectPool.Runtime;
using UnityEngine;

namespace Effects
{
    // 풀에서 꺼내져 1회 재생되는 VFX 래퍼. VfxDuration이 지나면 OnVfxEnd로 반납 시점을 알린다.
    public class PoolableVfx : AbstractMonoPoolable
    {
        [SerializeField] private GameObject effectObject;
        private IPlayableVFX _playableVfx;

        public event Action<PoolableVfx> OnVfxEnd;

        private void Awake()
        {
            _playableVfx = effectObject.GetComponent<IPlayableVFX>();
        }

        private void OnValidate()
        {
            if (effectObject == null) return;
            _playableVfx = effectObject.GetComponent<IPlayableVFX>();
            if (_playableVfx == null)
                effectObject = null;
        }

        // 풀에서 Pop될 때 호출. 이전 재생 잔상을 정리한다.
        public override void ResetItem()
        {
            _playableVfx.StopVFX();
            transform.localScale = Vector3.one; // 큰 폭발 후 풀 재사용 시 크기 잔상 방지
        }

        public void PlayVfx(Vector3 position, Quaternion rotation, float scale = 1f)
        {
            transform.SetPositionAndRotation(position, rotation);
            transform.localScale = Vector3.one * scale;
            StartCoroutine(PlayVfxCoroutine());
        }

        private IEnumerator PlayVfxCoroutine()
        {
            _playableVfx.PlayVFX();
            yield return new WaitForSeconds(_playableVfx.VfxDuration);
            OnVfxEnd?.Invoke(this);
        }
    }
}
