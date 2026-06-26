using EventChannelSystem;
using Events;
using ModuleSystem;
using UnityEngine;

namespace Players.Rendering
{
    // 메인 카메라와 이 transform 기준 로컬 좌표(localFadePoint) 사이 거리를 매 프레임 측정해
    // MissileCameraFade 셰이더의 _ForceFade(0~1)를 두 임계값 사이에서 보간한다.
    //  - 거리 >= outerThreshold : _ForceFade 0 (완전 불투명)
    //  - inner < 거리 < outer   : 가까워질수록 선형으로 투명
    //  - 거리 <= innerThreshold : _ForceFade 1 (완전 소멸)
    public class MissileRenderer : MonoBehaviour, IModule
    {
        private static readonly int ForceFadeID = Shader.PropertyToID("_ForceFade");

        [Header("페이드 대상")]
        [Tooltip("MissileCameraFade 셰이더를 사용하는 렌더러")]
        [SerializeField] private Renderer targetRenderer;

        [Header("표시/숨김 채널")]
        [Tooltip("GameStartEvent를 발행하는 채널 — 게임 시작 시 미사일을 보이게 한다.")]
        [SerializeField] private EventChannelSO gameChannel;
        [Tooltip("PlayerExplodedEvent를 발행하는 채널 — 메인 폭발 시 미사일을 숨긴다.")]
        [SerializeField] private EventChannelSO playerChannel;

        [Header("거리 기준점")]
        [Tooltip("이 transform 기준 로컬 좌표 — 카메라와의 거리를 재는 기준점(기즈모로 표시)")]
        [SerializeField] private Vector3 localFadePoint;

        [Header("임계값")]
        [Tooltip("이 거리부터 흐려지기 시작한다")]
        [SerializeField] private float outerThreshold = 3f;
        [Tooltip("이 거리 이하면 완전히 사라진다 (outerThreshold보다 작아야 함)")]
        [SerializeField] private float innerThreshold = 0.5f;

        [Tooltip("월드 축별 반경 비율 — Y를 1보다 작게 하면 위아래로 납작한 타원체(지구 모양)가 된다")]
        [SerializeField] private Vector3 fadeAxisScale = Vector3.one;

        [Tooltip("한번 페이드된 뒤 밖으로 나갈 때 감지 영역을 키우는 계수 — 클수록 더 멀어져야 페이드가 풀린다(진동 방지 히스테리시스, 1이면 비활성)")]
        [SerializeField] private float outsideDetectionMultiplier = 1.5f;

        private Camera _camera;
        private MaterialPropertyBlock _mpb;
        private float _appliedFade = -1f; // 값이 실제로 바뀔 때만 SetPropertyBlock 하기 위한 캐시
        private bool _faded;              // 현재 페이드 영역 안(페이드 진행 중)인지 — 히스테리시스용
        private bool _bouncing;           // 바운스 시퀀스 진행 중이면 폭발로 렌더러를 끄지 않는다

        public void Initialize(ModuleOwner owner)
        {
            _camera = Camera.main;
            _mpb = new MaterialPropertyBlock();

            ApplyFade(0);

            // 게임 시작 전엔 미사일을 숨겨두고, 시작 시 표시·폭발 시 다시 숨긴다.
            if (targetRenderer != null) targetRenderer.enabled = false;
            gameChannel?.AddListener<GameStartEvent>(OnGameStart);
            playerChannel?.AddListener<PlayerExplodedEvent>(OnExploded);
            playerChannel?.AddListener<PlayerBounceActiveEvent>(OnBounceActive);
        }

        private void OnGameStart(GameStartEvent _)
        {
            if (targetRenderer != null) targetRenderer.enabled = true;
        }

        // 바운스 중엔 로켓이 튕겨다니므로 보여야 한다. 시퀀스가 끝나는(마지막 바운스) 순간에만 숨긴다.
        private void OnExploded(PlayerExplodedEvent _)
        {
            if (_bouncing) return; // 바운스 진행 중이면 메인/착지 폭발로 숨기지 않는다
            if (targetRenderer != null) targetRenderer.enabled = false;
        }

        private void OnBounceActive(PlayerBounceActiveEvent evt)
        {
            _bouncing = evt.Active;
            if (targetRenderer == null) return;
            // 바운스 시작: 메인 폭발이 방금 끈 렌더러를 다시 켜 튕기는 로켓을 보여준다.
            // 바운스 끝: 마지막 바운스 후 렌더러를 끈다.
            targetRenderer.enabled = evt.Active;
        }

        private void OnDestroy()
        {
            gameChannel?.RemoveListener<GameStartEvent>(OnGameStart);
            playerChannel?.RemoveListener<PlayerExplodedEvent>(OnExploded);
            playerChannel?.RemoveListener<PlayerBounceActiveEvent>(OnBounceActive);
        }

        // 카메라는 Update에서 움직이므로(FollowCamera) 그 뒤인 LateUpdate에서 거리를 잰다.
        private void LateUpdate()
        {
            // 숨김 상태(게임 시작 전·폭발 후)면 거리 페이드 계산을 건너뛴다.
            if (targetRenderer == null || !targetRenderer.enabled) return;

            // Initialize 시점에 메인 카메라가 아직 없을 수 있으니 한 번 더 시도
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            Vector3 worldPoint = transform.TransformPoint(localFadePoint);

            // 월드 축별 반경 비율로 나눠 타원체 거리를 만든다(Y<1 이면 위아래로 납작).
            Vector3 delta = _camera.transform.position - worldPoint;
            delta.x /= Mathf.Max(fadeAxisScale.x, 1e-4f);
            delta.y /= Mathf.Max(fadeAxisScale.y, 1e-4f);
            delta.z /= Mathf.Max(fadeAxisScale.z, 1e-4f);
            float dist = delta.magnitude;

            // 페이드 중이면 영역을 계수배 확대 → outer*k 만큼 멀어져야 풀린다(진동 없는 히스테리시스)
            float scale = _faded ? Mathf.Max(outsideDetectionMultiplier, 1f) : 1f;
            float effOuter = outerThreshold * scale;
            float effInner = innerThreshold * scale;

            // effOuter 이상 = 0, effInner 이하 = 1, 사이는 선형
            float denom = Mathf.Max(effOuter - effInner, 1e-4f);
            float fade = Mathf.Clamp01((effOuter - dist) / denom);
            _faded = fade > 0f;

            ApplyFade(fade);
        }

        private void ApplyFade(float fade)
        {
            if (Mathf.Approximately(fade, _appliedFade)) return;
            _appliedFade = fade;

            targetRenderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(ForceFadeID, fade);
            targetRenderer.SetPropertyBlock(_mpb);
        }

        private void OnValidate()
        {
            if (innerThreshold < 0f) innerThreshold = 0f;
            if (outerThreshold < innerThreshold) outerThreshold = innerThreshold;

            // 1 미만이면 히스테리시스가 역전돼 오히려 진동하므로 막는다
            if (outsideDetectionMultiplier < 1f) outsideDetectionMultiplier = 1f;

            // 0 스케일은 거리 계산에서 0 나눗셈이 되므로 최소값으로 막는다
            fadeAxisScale.x = Mathf.Max(fadeAxisScale.x, 1e-3f);
            fadeAxisScale.y = Mathf.Max(fadeAxisScale.y, 1e-3f);
            fadeAxisScale.z = Mathf.Max(fadeAxisScale.z, 1e-3f);
        }

        // 기준점과 두 임계값(타원체)을 씬 뷰에 표시
        private void OnDrawGizmos()
        {
            Vector3 worldPoint = transform.TransformPoint(localFadePoint);

            // 기준점 (스케일 영향 없이)
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(worldPoint, 0.1f);

            // 월드 축 스케일을 행렬에 실어 단위 구를 타원체로 그린다(거리 계산과 동일한 모양)
            Matrix4x4 prev = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(worldPoint, Quaternion.identity, fadeAxisScale);

            // inner = 완전 소멸
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(Vector3.zero, innerThreshold);

            // outer = 페이드 시작(들어올 때)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(Vector3.zero, outerThreshold);

            // outer*k = 페이드 해제(나갈 때) 히스테리시스 경계
            float mult = Mathf.Max(outsideDetectionMultiplier, 1f);
            if (mult > 1f)
            {
                Gizmos.color = new Color(1f, 0.55f, 0f); // 주황
                Gizmos.DrawWireSphere(Vector3.zero, outerThreshold * mult);
            }

            Gizmos.matrix = prev;
        }
    }
}
