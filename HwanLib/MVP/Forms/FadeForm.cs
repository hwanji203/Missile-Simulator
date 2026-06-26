using System.Collections;
using System.Collections.Generic;
using MVP.Forms.Module.Fade;
using MVP.System.BaseMVP;
using MVP.System.BaseMVP.Form;
using UnityEngine;
using UnityEngine.UI;

namespace MVP.Forms
{
    // 씬 전환 모션의 비주얼 Form. 데이터 바인딩은 없고(IInitializable로 targetImage만 준비),
    // Presenter가 FadeOut/FadeIn 코루틴을 명령형으로 호출한다. (Easy Transition Fade/TransitionEffect 로직 이전)
    // 효과별 분기 없이 preset(머티리얼)만 교체하고 _Cutoff를 보간한다.
    [RequireComponent(typeof(Image))]
    public class FadeForm : BaseForm, IInitializable
    {
        [Tooltip("preset 미전달 시 사용할 기본 전환 프리셋(Fade 등).")]
        [SerializeField] private TransitionPreset defaultPreset;

        private static readonly int CutoffID   = Shader.PropertyToID("_Cutoff");
        private static readonly int RectSizeID = Shader.PropertyToID("_RectSize");
        private static readonly int AngleID    = Shader.PropertyToID("_Angle");

        private Image _targetImage;

        // preset당 머티리얼 인스턴스 1개(지연 생성). OnDestroy에서 전부 Destroy.
        private readonly Dictionary<TransitionPreset, Material> _instances = new();

        // BasePresenter.InitializePresenter가 자동 호출. 머티리얼 인스턴스는 첫 사용 시 생성.
        public void Initialize()
        {
            _targetImage = GetComponent<Image>();
        }

        // 0(투명) → 1(덮음). preset의 머티리얼로 교체 후 보간.
        public IEnumerator FadeOut(TransitionPreset preset)
        {
            preset ??= defaultPreset;
            Material inst = GetInstance(preset);
            _targetImage.material = inst;
            ApplyRectSize(inst);
            // 방향성 효과: 나갈 때는 머티리얼에 구워진 기준 _Angle로 복원(이전 FadeIn에서 +π 했을 수 있음).
            if (preset.flipAngleOnIn)
                inst.SetFloat(AngleID, preset.material.GetFloat(AngleID));
            yield return AnimateCutoff(inst, 0f, 1f, preset.duration / 2f, preset.easingCurve);
        }

        // 1(덮음) → 0(투명). FadeOut과 같은 preset 인스턴스 사용.
        public IEnumerator FadeIn(TransitionPreset preset)
        {
            preset ??= defaultPreset;
            Material inst = GetInstance(preset);
            // 방향성 효과: 들어올 때 _Angle을 +π 해서 되돌아가지 않고 같은 방향으로 계속 닦아 나간다.
            if (preset.flipAngleOnIn)
                inst.SetFloat(AngleID, preset.material.GetFloat(AngleID) + Mathf.PI);
            yield return AnimateCutoff(inst, 1f, 0f, preset.duration / 2f, preset.easingCurve);
        }

        // 아웃과 인 사이 대기(초).
        public float MiddleDelayOf(TransitionPreset preset) => (preset ?? defaultPreset).middleDelay;

        // preset당 머티리얼 인스턴스를 캐시(지연 생성).
        private Material GetInstance(TransitionPreset preset)
        {
            if (!_instances.TryGetValue(preset, out Material inst))
            {
                inst = new Material(preset.material);
                _instances[preset] = inst;
            }
            return inst;
        }

        // 화면을 덮기 직전(캔버스 활성·레이아웃 후) 현재 rect로 _RectSize 갱신. (원본 패리티)
        private void ApplyRectSize(Material inst)
        {
            Rect rect = _targetImage.rectTransform.rect;
            inst.SetVector(RectSizeID, new Vector4(rect.width, rect.height, 0, 0));
        }

        // _Cutoff를 이징 커브 따라 보간. timeScale=0에서도 동작하도록 unscaled time 사용.
        private IEnumerator AnimateCutoff(Material inst, float startValue, float targetValue, float animDuration, AnimationCurve curve)
        {
            inst.SetFloat(CutoffID, startValue);
            float time = 0f;

            while (time < animDuration)
            {
                float t = Mathf.Clamp01(time / animDuration);
                float curvedT = curve.Evaluate(t);
                inst.SetFloat(CutoffID, Mathf.Lerp(startValue, targetValue, curvedT));

                yield return null;

                // 씬 로드 랙 스파이크로 애니메이션이 건너뛰지 않게 dt를 캡.
                time += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            }

            // 정확한 최종값 보장.
            inst.SetFloat(CutoffID, Mathf.Lerp(startValue, targetValue, curve.Evaluate(1f)));
        }

        private void OnDestroy()
        {
            foreach (Material inst in _instances.Values)
                if (inst != null) Destroy(inst);
            _instances.Clear();
        }
    }
}
