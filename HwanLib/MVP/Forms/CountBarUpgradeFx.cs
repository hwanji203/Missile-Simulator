using System.Collections.Generic;
using Coffee.UIExtensions;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace MVP.Forms
{
    // CountBarForm의 새 칸(fill)이 켜질 때 "벽을 뚫고 튀어나오는" 연출을 재생한다.
    // 렌더링은 Form이, 연출은 이 컴포넌트가 담당(접근 B 분리). 같은 GameObject에 붙는다.
    [RequireComponent(typeof(CountBarForm))]
    public class CountBarUpgradeFx : MonoBehaviour
    {
        [Header("등장모션")]
        [Tooltip("등장 시작 스케일 배율(정상=1)")]
        [SerializeField] private float startScale = 2.5f;
        [SerializeField] private float popDuration = 0.35f;
        [SerializeField] private Ease popEase = Ease.OutBack;

        [Header("파티클")]
        [Tooltip("UI Particle 프리팹. 비우면 파티클 단계만 건너뜀")]
        [SerializeField] private UIParticle particlePrefab;
        [Tooltip("파티클 인스턴스 자동 파괴까지 시간(초)")]
        [SerializeField] private float particleLifetime = 2f;

        [Header("주변반응(흔들림)")]
        [Tooltip("미지정 시 바 루트(this) RectTransform 사용")]
        [SerializeField] private RectTransform shakeTarget;
        [SerializeField] private float shakeDuration = 0.3f;
        [SerializeField] private float shakeStrength = 12f;

        [Header("+N 시차")]
        [Tooltip("같은 프레임에 여러 칸이 켜질 때 칸 사이 지연(초)")]
        [SerializeField] private float staggerInterval = 0.06f;

        private CountBarForm _form;
        private int _lastBurstFrame = -1;  // 같은 프레임 +N 판별
        private int _burstIndex;           // 그 프레임 내 몇 번째 칸인지
        private readonly List<Sequence> _active = new List<Sequence>();

        private void Awake()
        {
            _form = GetComponent<CountBarForm>();
            if (shakeTarget == null) shakeTarget = transform as RectTransform;
        }

        private void OnEnable()
        {
            if (_form != null) _form.SlotFilled += OnSlotFilled;
        }

        private void OnDisable()
        {
            if (_form != null) _form.SlotFilled -= OnSlotFilled;
            foreach (Sequence s in _active) s.Kill();
            _active.Clear();
        }

        private void OnSlotFilled(RectTransform fill)
        {
            // 같은 프레임에 여러 칸이 켜지면(+N) 칸마다 시차를 준다.
            if (Time.frameCount != _lastBurstFrame)
            {
                _lastBurstFrame = Time.frameCount;
                _burstIndex = 0;
            }
            float delay = _burstIndex * staggerInterval;
            _burstIndex++;

            Image fillImage = fill.GetComponent<Image>();

            // 시차 대기 동안 보이지 않도록 즉시 숨김(큰 스케일 + 알파0).
            fill.localScale = Vector3.one * startScale;
            SetAlpha(fillImage, 0f);

            Sequence seq = DOTween.Sequence().SetUpdate(true);
            if (delay > 0f) seq.AppendInterval(delay);
            seq.AppendCallback(() => SpawnParticle(fill));
            seq.Append(fill.DOScale(Vector3.one, popDuration).SetEase(popEase));
            if (fillImage != null) seq.Join(fillImage.DOFade(1f, popDuration));
            if (shakeTarget != null)
                seq.Join(shakeTarget.DOShakeAnchorPos(shakeDuration, shakeStrength));

            _active.Add(seq);
            seq.OnComplete(() => _active.Remove(seq));
        }

        // 착지 위치에 UI Particle 1회 재생 후 자동 파괴. 프리팹 미연결이면 스킵.
        private void SpawnParticle(RectTransform at)
        {
            if (particlePrefab == null || at == null) return;
            UIParticle p = Instantiate(particlePrefab, shakeTarget != null ? shakeTarget : transform);
            p.transform.position = at.position;
            p.Play();
            Destroy(p.gameObject, particleLifetime);
        }

        private static void SetAlpha(Image img, float a)
        {
            if (img == null) return;
            Color c = img.color;
            c.a = a;
            img.color = c;
        }
    }
}
