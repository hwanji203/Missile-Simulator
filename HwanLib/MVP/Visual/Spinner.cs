using DG.Tweening;
using UnityEngine;

namespace MVP.Visual
{
    /// <summary>
    /// RectTransform을 일정 속도로 무한 Z회전시키는 로딩 인디케이터.
    /// 아무 GO에 부착하면 기본값으로 동작. InteractionFeedback과 무관.
    /// </summary>
    public class Spinner : MonoBehaviour
    {
        [SerializeField] private float degPerSec = 180f;
        [SerializeField] private bool clockwise = true;
        [SerializeField] private bool ignoreTimeScale = true;

        private Tween _tween;

        private void OnEnable()
        {
            Play();
        }

        private void OnDisable()
        {
            _tween?.Kill();
            _tween = null;
        }

        private void Play()
        {
            _tween?.Kill();

            float speed = Mathf.Max(1f, degPerSec);
            float dir = clockwise ? -1f : 1f; // UI에서 Z- 가 시계방향
            float durationFor360 = 360f / speed;

            _tween = transform
                .DOLocalRotate(new Vector3(0f, 0f, dir * 360f), durationFor360, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(ignoreTimeScale);
        }
    }
}
