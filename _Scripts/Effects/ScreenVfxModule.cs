using System.Collections.Generic;
using System.Linq;
using EventChannelSystem;
using Events;
using UnityEngine;

namespace Effects
{
    // 카메라(또는 그 하위)에 붙어, 자식 IPlayableVFX(화면 효과)를 AssetHash로 매핑하고
    // 채널의 PlayerScreenVfxEvent를 받아 해당 효과를 Play/Stop한다.
    public class ScreenVfxModule : MonoBehaviour
    {
        [SerializeField] private EventChannelSO playerChannel;
        private Dictionary<int, IPlayableVFX> _playableDict;

        private void Awake()
            => _playableDict = GetComponentsInChildren<IPlayableVFX>().ToDictionary(v => v.VfxHash);

        private void OnEnable()
        {
            playerChannel.AddListener<PlayerScreenVfxEvent>(Handle);
        }

        private void OnDisable()
        {
            playerChannel.RemoveListener<PlayerScreenVfxEvent>(Handle);
        }

        private void Handle(PlayerScreenVfxEvent evt)
        {
            if (!_playableDict.TryGetValue(evt.VfxHash, out IPlayableVFX vfx))
            {
                Debug.LogWarning($"Screen VFX with hash : {evt.VfxHash} not found");
                return;
            }
            if (evt.Play) {vfx.PlayVFX();}
            else vfx.StopVFX();
        }
    }
}