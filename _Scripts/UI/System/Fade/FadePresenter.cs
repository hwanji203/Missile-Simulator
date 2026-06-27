using System.Collections;
using MVP.System.BaseMVP;
using MVP.System.GenerateUI;
using UnityEngine;
using UnityEngine.Audio;

namespace UI.System.Fade
{
    public class FadePresenter : BasePresenter<FadeModel, FadeView>
    {
        [Header("Audio Fade")]
        [Tooltip("씬 전환 중 페이드시킬 믹서. 비우면 사운드 페이드 안 함(영상만).")]
        [SerializeField] private AudioMixer mixer;
        [Tooltip("믹서에 노출(Expose)된 볼륨 파라미터 이름.")]
        [SerializeField] private string volumeParam = "Master";
        [Tooltip("사운드 페이드 아웃/인 각 방향 시간(초). 영상 페이드 절반 시간 이하 권장.")]
        [SerializeField, Min(0f)] private float audioFadeDuration = 0.4f;
        [Tooltip("무음으로 간주할 dB(보통 -80).")]
        [SerializeField] private float silentDb = -80f;

        // 씬 전환 연출이므로 씬 리셋 닫기 루프에서 제외.
        public override bool PersistsAcrossSceneReset => true;

        // 페이드 진행 중 재진입 방지.
        private bool _busy;
        public override bool CanOpen => !_busy;

        // 현재 돌고 있는 사운드 페이드 코루틴. 새 페이드 시작 전 이전 게 살아있으면 끊는다.
        private Coroutine _audioFadeCo;
        
        public override void Open<T>(T payload)
        {
            base.Open(payload); // IsOpen=true, View.OpenView (캔버스 활성)
        
            var req = payload as FadeRequestEvent;
            _busy = true;
            StartCoroutine(RunSequence(req));
            
            UIGate.Block("Fade");
        }

        public override void Close()
        {
            base.Close();
            
            UIGate.Unblock("Fade");
        }

        private IEnumerator RunSequence(FadeRequestEvent req)
        {
            var view = (FadeView)View;
            var preset = req?.Preset;

            // 현재 Master 볼륨(dB) 캡처 → 복구 기준. 믹서/파라미터 없으면 사운드 페이드 생략.
            float baseDb = 0f;
            bool hasMixer = mixer != null && mixer.GetFloat(volumeParam, out baseDb);

            // 덮는 동안 소리 빼기(영상과 병렬).
            if (hasMixer) StartAudioFade(baseDb, silentDb);
            yield return view.Fade.FadeOut(preset);

            // 중간점: 씬 로드 등 콜백을 완료까지 대기 → 깜빡임 방지.
            // (스피너 토글은 FadeForm이 FadeOut 끝/FadeIn 시작에서 내부적으로 처리.)
            if (req?.OnMidpoint != null)
                yield return req.OnMidpoint();

            float mid = view.Fade.MiddleDelayOf(preset);
            if (mid > 0)
                yield return new WaitForSecondsRealtime(mid);

            // 걷는 동안 소리 복귀(영상과 병렬).
            if (hasMixer) StartAudioFade(silentDb, baseDb);
            yield return view.Fade.FadeIn(preset);

            // 영상은 끝났는데 사운드 페이드가 아직 돌고 있으면: 끊고 기준값으로 딱 스냅.
            if (hasMixer)
            {
                StopAudioFade();
                mixer.SetFloat(volumeParam, baseDb);
            }

            _busy = false;
            Close(); // → OnViewClosed → OnClosed → UIManager 정리
        }

        // 사운드 페이드를 시작하되, 이전 게 아직 살아있으면 끊고 시작 dB로 딱 스냅한 뒤 새로 시작.
        // (이전 페이드의 목표 = 이번 페이드의 시작이라 fromDb로 스냅하면 자연스럽게 이어진다.)
        private void StartAudioFade(float fromDb, float toDb)
        {
            if (_audioFadeCo != null)
            {
                StopCoroutine(_audioFadeCo);
                mixer.SetFloat(volumeParam, fromDb);
            }
            _audioFadeCo = StartCoroutine(FadeAudio(fromDb, toDb, audioFadeDuration));
        }

        // 돌고 있는 사운드 페이드가 있으면 중단(목표값 스냅은 호출부에서).
        private void StopAudioFade()
        {
            if (_audioFadeCo != null)
            {
                StopCoroutine(_audioFadeCo);
                _audioFadeCo = null;
            }
        }

        // dB를 직선 보간하면 뚝 끊기므로 선형 진폭으로 보간 후 dB로 변환. timeScale=0에서도 동작하도록 unscaled.
        private IEnumerator FadeAudio(float fromDb, float toDb, float dur)
        {
            if (dur <= 0f) { mixer.SetFloat(volumeParam, toDb); yield break; }

            float fromLin = DbToLinear(fromDb);
            float toLin   = DbToLinear(toDb);
            float time = 0f;

            while (time < dur)
            {
                float k = Mathf.Clamp01(time / dur);
                mixer.SetFloat(volumeParam, LinearToDb(Mathf.Lerp(fromLin, toLin, k)));
                yield return null;
                time += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            }

            mixer.SetFloat(volumeParam, toDb);
            _audioFadeCo = null; // 자연 종료: 핸들 정리(이후 StopAudioFade는 no-op).
        }

        private static float DbToLinear(float db) => Mathf.Pow(10f, db / 20f);
        private static float LinearToDb(float linear) => Mathf.Log10(Mathf.Max(linear, 0.0001f)) * 20f;
    }
}
