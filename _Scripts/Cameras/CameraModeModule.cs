using EventChannelSystem;
using Events;
using ModuleSystem;
using Players;
using UnityEngine;

namespace Cameras
{
    // 기본 시점 선택지. CameraModeModule의 defaultView 인스펙터 필드로 고른다.
    public enum CameraViewMode
    {
        Follow,      // 1인칭 팔로우(미사일 뒤 데드존 orbit)
        QuarterView  // 3인칭 쿼터뷰(고정 pitch로 내려다보며 진행 방향을 따라 공전)
    }

    // 카메라의 두뇌: 트리거(정지/폭발/슈퍼부스트/peek)를 듣고 활성 행동을 전환한다.
    // 기본 시점은 defaultView로 선택(팔로우/쿼터뷰). 트리거가 오면 3인칭으로,
    // 시한부 state(peek)가 끝나면 자동으로 기본 시점 복귀.
    // Shift 홀드(LookBack)는 비행 중(=기본 시점일 때)만 즉시 뒤돌아보기로 전환, 해제 시 즉시 기본 시점 복귀.
    public class CameraModeModule : MonoBehaviour, IModule
    {
        [SerializeField] private EventChannelSO playerEvent;
        [SerializeField] private PlayerInputSO playerInput;
        [Tooltip("기본 시점. 상황 연출이 끝나면 이 시점으로 복귀한다")]
        [SerializeField] private CameraViewMode defaultView = CameraViewMode.QuarterView;

        private FollowCameraModule _follow;
        private QuarterViewCameraModule _quarterView;
        private ThirdPersonCameraModule _thirdPerson;

        public ICameraBehaviour ActiveBehaviour { get; private set; }

        // 켜지면 CameraOwner가 추적을 멈추고 현재 위치에 고정한다(수중 추락 등 비폭발 종료 연출).
        public bool Frozen { get; private set; }

        // defaultView가 가리키는 기본 시점 행동. 쿼터뷰 모듈이 프리팹에 없으면 팔로우로 폴백.
        private ICameraBehaviour DefaultBehaviour =>
            defaultView == CameraViewMode.QuarterView && _quarterView != null ? _quarterView : _follow;

        public void Initialize(ModuleOwner owner)
        {
            _follow = owner.GetModule<FollowCameraModule>();
            _quarterView = owner.GetModule<QuarterViewCameraModule>();
            _thirdPerson = owner.GetModule<ThirdPersonCameraModule>();
            ActiveBehaviour = DefaultBehaviour;

            if (playerInput != null) playerInput.OnViewToggleRequested += OnViewToggle;

            if (playerEvent == null) return;
            playerEvent.AddListener<PlayerStoppedEvent>(OnStopped);
            playerEvent.AddListener<PlayerExplodedEvent>(OnExploded);
            playerEvent.AddListener<PlayerSuperBoostEvent>(OnSuperBoost);
            playerEvent.AddListener<PlayerPeekRequestedEvent>(OnPeek);
            playerEvent.AddListener<PlayerSkillAcquiredEvent>(OnSkillAcquired);
            playerEvent.AddListener<CameraFreezeEvent>(OnCameraFreeze);
        }

        private void OnDestroy()
        {
            if (playerInput != null) playerInput.OnViewToggleRequested -= OnViewToggle;

            if (playerEvent == null) return;
            playerEvent.RemoveListener<PlayerStoppedEvent>(OnStopped);
            playerEvent.RemoveListener<PlayerExplodedEvent>(OnExploded);
            playerEvent.RemoveListener<PlayerSuperBoostEvent>(OnSuperBoost);
            playerEvent.RemoveListener<PlayerPeekRequestedEvent>(OnPeek);
            playerEvent.RemoveListener<PlayerSkillAcquiredEvent>(OnSkillAcquired);
            playerEvent.RemoveListener<CameraFreezeEvent>(OnCameraFreeze);
        }

        // Shift 누름마다 1인칭(Follow) ↔ 3인칭(QuarterView)을 즉시 토글한다(백뷰 대체).
        private void OnViewToggle()
        {
            defaultView = defaultView == CameraViewMode.Follow
                ? CameraViewMode.QuarterView
                : CameraViewMode.Follow;

            bool toFirstPerson = defaultView == CameraViewMode.Follow;
            ReturnToDefault(snapFollow: toFirstPerson);   // 1인칭으로 갈 땐 회전을 Lerp 없이 즉시 스냅
            playerEvent?.RaiseEvent(
                PlayerEvents.PlayerViewChangedEvent.InitData(toFirstPerson));
        }

        // 시한부 state(peek)가 커브를 다 돌면 기본 시점으로 자동 복귀.
        private void Update()
        {
            if (ReferenceEquals(ActiveBehaviour, _thirdPerson) &&
                _thirdPerson != null && _thirdPerson.ActiveStateFinished)
                ReturnToDefault(snapFollow: false);
        }

        private void ReturnToDefault(bool snapFollow)
        {
            ICameraBehaviour target = DefaultBehaviour;
            if (snapFollow && ReferenceEquals(target, _follow)) _follow?.RequestSnap();
            ActiveBehaviour = target;
        }

        private void OnStopped(PlayerStoppedEvent _)             => EnterThirdPerson(ThirdPersonSituation.Stop);
        private void OnCameraFreeze(CameraFreezeEvent _)         => Frozen = true;
        private void OnExploded(PlayerExplodedEvent evt)         => EnterThirdPerson(ThirdPersonSituation.Explode, evt.Scale);

        private void OnSuperBoost(PlayerSuperBoostEvent evt)
        {
            if (evt.IsStarted)
                EnterThirdPerson(ThirdPersonSituation.SuperBoost);
        }

        private void OnPeek(PlayerPeekRequestedEvent _)           => EnterThirdPerson(ThirdPersonSituation.Peek);

        // 스킬/스탯 획득 시 카메라가 잠깐 뒤로 빠졌다 복귀(Peek). 스탯·스킬 모두 이 이벤트로 흐른다.
        private void OnSkillAcquired(PlayerSkillAcquiredEvent _) => EnterThirdPerson(ThirdPersonSituation.Peek);

        // 외부(예: 추후 슈퍼부스트 이동 로직)에서 직접 3인칭을 켜는 공개 훅.
        public void EnterThirdPerson(ThirdPersonSituation situation, float pullScale = 1f)
        {
            if (_thirdPerson == null) return;
            bool fresh = ActiveBehaviour is not ThirdPersonCameraModule; // 팔로우→3인칭 첫 진입인지
            _thirdPerson.SetSituation(situation, fresh, pullScale);
            ActiveBehaviour = _thirdPerson;
        }
    }
}
