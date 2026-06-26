using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace MVP.Visual
{
    /// <summary>
    /// 대상 Graphic의 알파 또는 localScale를 min↔max로 주기 반복하는 독립 연출.
    /// InteractionFeedback과 무관. 아무 GO에 부착하면 기본값으로 동작.
    /// </summary>
    public class ImagePulse : MonoBehaviour
    {
        private enum PulseMode { Alpha, Scale }

        [SerializeField] private PulseMode mode = PulseMode.Alpha;
        [SerializeField] private float cycleDuration = 1f; // min→max 1방향 시간(초)
        [SerializeField] private float min = 0.4f;
        [SerializeField] private float max = 1f;
        [SerializeField] private Ease ease = Ease.InOutSine;
        [SerializeField] private bool ignoreTimeScale = true;

        private Graphic _graphic;
        private Tween _tween;

        private void Awake()
        {
            _graphic = GetComponent<Graphic>();
        }

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

            if (mode == PulseMode.Alpha)
            {
                if (_graphic == null) return;
                SetAlpha(min);
                _tween = _graphic.DOFade(max, cycleDuration);
            }
            else // Scale
            {
                transform.localScale = Vector3.one * min;
                _tween = transform.DOScale(Vector3.one * max, cycleDuration);
            }

            _tween.SetEase(ease)
                  .SetLoops(-1, LoopType.Yoyo)
                  .SetUpdate(ignoreTimeScale);
        }

        private void SetAlpha(float a)
        {
            if (_graphic == null) return;
            Color c = _graphic.color;
            c.a = a;
            _graphic.color = c;
        }
    }
}
