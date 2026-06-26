using Effects;
using EventChannelSystem;
using Events;
using ModuleSystem;
using Players.Movement;
using UnityEngine;

namespace Players
{
    // 슈퍼 부스트 활성/비활성 전이를 감지해 자식 VFX를 직접 Play/Stop한다.
    // VFX 오브젝트는 Visual 하위에 두어 pivot 중심 회전을 그대로 따라가게 한다.
    public class SuperBoostVfxEmitter : MonoBehaviour, IModule
    {
        [SerializeField] private GameObject vfxObject;
        [SerializeField] private PlayerInputSO playerInput;
        [SerializeField] private EventChannelSO playerChannel;
        [SerializeField] private AssetHashSO fireScreenVfx;

        private IPlayableVFX _vfx;
        private bool _wasSuperBoosting;

        public void Initialize(ModuleOwner owner)
        {
            _vfx = vfxObject != null ? vfxObject.GetComponent<IPlayableVFX>() : null;
        }

        private void OnEnable()
        {
            playerChannel.AddListener<PlayerSuperBoostEvent>(HandleSuperBoost);
            playerChannel.AddListener<PlayerStoppedEvent>(HandlePlayerStop);
        }

        private void OnDisable()
        {
            playerChannel.RemoveListener<PlayerSuperBoostEvent>(HandleSuperBoost);
            playerChannel.RemoveListener<PlayerStoppedEvent>(HandlePlayerStop);
        }

        private void HandlePlayerStop(PlayerStoppedEvent _)
        {
            if (_wasSuperBoosting)
            {
                _vfx.StopVFX();
                playerChannel.RaiseEvent(
                    PlayerEvents.PlayerScreenVfxEvent.InitData(fireScreenVfx.AssetHash, false));
            }
        }

        private void HandleSuperBoost(PlayerSuperBoostEvent evt)
        {
            if (!_wasSuperBoosting && !evt.IsStarted)
            {
                bool keyDown = evt.IsPressed;
                playerChannel.RaiseEvent(
                    PlayerEvents.PlayerScreenVfxEvent.InitData(fireScreenVfx.AssetHash, keyDown));
            }
        
            if (evt.IsStarted)
            {
                _vfx.PlayVFX();
                _wasSuperBoosting = true;
            }
        }
    }
}
