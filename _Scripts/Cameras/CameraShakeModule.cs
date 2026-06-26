using EventChannelSystem;
using Events;
using ModuleSystem;
using UnityEngine;

namespace Cameras
{
    // 카메라 흔들림(쉐이크) 모듈. 카메라 리그(CameraOwner)의 자식 모듈로 둔다.
    // 직접 transform을 건드리지 않고, 매 프레임의 "위치 오프셋 + roll 각도"만 계산해 노출한다.
    // 실제 적용은 유일한 transform 적용자인 CameraOwner가 베이스 포즈 위에 덧씌운다.
    //
    // 트리거는 플레이어 이벤트 채널로 받는다(기존 카메라 모듈들과 동일한 디커플 방식):
    //  - PlayerHitEvent     → 피격(생존) 쉐이크: roll을 조금 왔다갔다 + 약하게 흔들림
    //  - PlayerExplodedEvent → 폭발(사망) 쉐이크: 따로 설정한 세기로 더 크게 흔들림.
    //    이 이벤트는 CameraModeModule이 ThirdPerson 폭발 카메라로 전환하는 바로 그 이벤트라,
    //    폭발 연출과 항상 동시에 울린다(타이머/접촉/사망 어떤 원인의 폭발이든 포함).
    public class CameraShakeModule : MonoBehaviour, IModule
    {
        // 한 번의 흔들림을 정의하는 프로필. 피격용/사망용을 따로 둬서 인스펙터에서 세기를 조절한다.
        [System.Serializable]
        public struct ShakeProfile
        {
            [Tooltip("흔들림 지속 시간(초)")]
            public float duration;

            [Tooltip("위치 흔들림 세기(월드 단위, 카메라 로컬축 기준)")]
            public float positionAmplitude;

            [Tooltip("Roll 흔들림 최대 각도(도) — 화면이 좌우로 왔다갔다 기운다")]
            public float rollAmplitude;

            [Tooltip("흔들림 진동수(클수록 더 빠르게 떨린다)")]
            public float frequency;
        }

        [SerializeField] private EventChannelSO playerEvent;

        [Header("피격 시(생존)")]
        [SerializeField]
        private ShakeProfile hitProfile = new()
        {
            duration = 0.3f, positionAmplitude = 0.15f, rollAmplitude = 6f, frequency = 18f,
        };

        [Header("폭발(사망) 시")]
        [SerializeField]
        private ShakeProfile explodeProfile = new()
        {
            duration = 0.8f, positionAmplitude = 0.5f, rollAmplitude = 15f, frequency = 22f,
        };

        // ── CameraOwner가 매 프레임 읽는 값 ──
        public Vector3 PositionOffset { get; private set; }
        public float RollAngle { get; private set; }
        public bool IsShaking => _timer > 0f;

        private ShakeProfile _current;
        private float _timer;     // 남은 흔들림 시간(초)
        private float _elapsed;   // 시작부터의 경과(노이즈/사인 시간축)
        private float _seedX, _seedY, _seedZ; // 위치 노이즈용 펄린 시드(흔들림마다 새로)

        public void Initialize(ModuleOwner owner)
        {
            if (playerEvent == null) return;
            playerEvent.AddListener<PlayerHitEvent>(OnHit);
            playerEvent.AddListener<PlayerExplodedEvent>(OnExploded);
        }

        private void OnDestroy()
        {
            if (playerEvent == null) return;
            playerEvent.RemoveListener<PlayerHitEvent>(OnHit);
            playerEvent.RemoveListener<PlayerExplodedEvent>(OnExploded);
        }

        private void OnHit(PlayerHitEvent _) => Play(hitProfile);
        private void OnExploded(PlayerExplodedEvent _) => Play(explodeProfile);

        // 흔들림 시작(혹은 재시작). 최신 트리거가 우선이라 사망이 피격을 덮어쓴다.
        private void Play(ShakeProfile profile)
        {
            _current = profile;
            _timer = profile.duration;
            _elapsed = 0f;
            _seedX = Random.value * 100f;
            _seedY = Random.value * 100f;
            _seedZ = Random.value * 100f;
        }

        // CameraOwner가 LateUpdate에서 한 번 호출해 시간을 진행시키고 오프셋/roll을 갱신한다.
        public void Tick(float deltaTime)
        {
            if (_timer <= 0f)
            {
                PositionOffset = Vector3.zero;
                RollAngle = 0f;
                return;
            }

            _timer -= deltaTime;
            _elapsed += deltaTime;

            // 남은 시간 비율로 감쇠(제곱으로 더 부드럽게 잦아듦).
            float env = _current.duration > 0f ? Mathf.Clamp01(_timer / _current.duration) : 0f;
            env *= env;

            // 위치: 축마다 다른 펄린 노이즈(-1..1)로 불규칙하게 떨림.
            float t = _elapsed * _current.frequency;
            float nx = Mathf.PerlinNoise(_seedX, t) * 2f - 1f;
            float ny = Mathf.PerlinNoise(_seedY, t) * 2f - 1f;
            float nz = Mathf.PerlinNoise(_seedZ, t) * 2f - 1f;
            PositionOffset = new Vector3(nx, ny, nz) * (_current.positionAmplitude * env);

            // Roll: 사인 왕복으로 좌우로 왔다갔다 기울인다.
            RollAngle = Mathf.Sin(_elapsed * _current.frequency) * (_current.rollAmplitude * env);
        }
    }
}
