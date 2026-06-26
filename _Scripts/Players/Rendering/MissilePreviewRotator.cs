using UnityEngine;

namespace Players.Rendering
{
    // 메타(로비) 좌상단 RenderTexture 프리뷰용 미사일 표시 모델을 무한 회전시킨다.
    // 로비는 Time.timeScale=0으로 게임을 멈추므로 unscaled time으로 돌려야 정지하지 않는다.
    public class MissilePreviewRotator : MonoBehaviour
    {
        [Tooltip("회전 축(로컬). 기본 (0,1,0) = 세로축 턴테이블")]
        [SerializeField] private Vector3 rotationAxis = Vector3.up;

        [Tooltip("초당 회전 각도")]
        [SerializeField] private float degreesPerSecond = 30f;

        [Tooltip("켜면 Time.timeScale 무시(로비 일시정지 중에도 회전). 기본 켜짐")]
        [SerializeField] private bool ignoreTimeScale = true;

        private void Update()
        {
            float dt = ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
            transform.Rotate(rotationAxis.normalized, degreesPerSecond * dt, Space.World);
        }
    }
}
