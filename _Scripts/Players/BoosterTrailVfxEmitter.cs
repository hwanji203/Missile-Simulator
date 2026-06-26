using Effects;
using EventChannelSystem;
using Events;
using ModuleSystem;
using UnityEngine;

namespace Players
{
    // 로켓 뒤 분사 파티클(트레일 역할)을 제어한다.
    // 비행 중엔 3인칭에서만 재생하고, 1인칭(Follow)에선 화면을 가리지 않도록 완전히 끈다.
    // 미사일이 멈추면(PlayerStoppedEvent) 이후 다시 재생하지 않는다.
    public class BoosterTrailVfxEmitter : MonoBehaviour, IModule
    {
        [SerializeField] private GameObject vfxObject;
        [SerializeField] private EventChannelSO playerChannel;

        private IPlayableVFX _vfx;
        private bool _isFirstPerson;  // 기본 false = 3인칭으로 시작
        private bool _stopped;        // 미사일이 멈췄는지(이후 재생 금지)

        public void Initialize(ModuleOwner owner)
        {
            _vfx = vfxObject != null ? vfxObject.GetComponent<IPlayableVFX>() : null;
            RefreshPlayState();
        }

        private void OnEnable()
        {
            playerChannel.AddListener<PlayerViewChangedEvent>(OnViewChanged);
            playerChannel.AddListener<PlayerStoppedEvent>(OnStopped);
        }

        private void OnDisable()
        {
            playerChannel.RemoveListener<PlayerViewChangedEvent>(OnViewChanged);
            playerChannel.RemoveListener<PlayerStoppedEvent>(OnStopped);
        }

        private void OnViewChanged(PlayerViewChangedEvent evt)
        {
            _isFirstPerson = evt.IsFirstPerson;
            RefreshPlayState();
        }

        private void OnStopped(PlayerStoppedEvent _)
        {
            _stopped = true;
            _vfx?.StopVFX();
        }

        // 정지했으면 항상 멈춤. 아니면 1인칭=숨김(Stop), 3인칭=재생(Play).
        private void RefreshPlayState()
        {
            if (_vfx == null) return;
            if (_stopped || _isFirstPerson) _vfx.StopVFX();
            else _vfx.PlayVFX();
        }
    }
}
