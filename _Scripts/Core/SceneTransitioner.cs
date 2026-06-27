using System.Collections;
using EventChannelSystem;
using Events;
using MVP.System.GenerateUI;
using MVP.Utility;
using UI.System.Fade;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    // 씬 전환의 진입점. 비주얼은 전혀 모르고 구독→발행만 한다.
    // gameChannel의 LoadSceneEvent를 구독 → 중간점에 씬 로드를 수행하는
    // FadeRequestEvent를 만들어 uiChannel로 OpenUIEvent(UIId.Fade)를 발행한다.
    public class SceneTransitioner : LightSingleton<SceneTransitioner>
    {
        [SerializeField] private EventChannelSO gameChannel;
        [SerializeField] private EventChannelSO uiChannel;

        protected override void Initialize()
        {
            gameChannel.AddListener<LoadSceneEvent>(HandleLoadScene);
        }

        private void OnDestroy()
        {
            gameChannel?.RemoveListener<LoadSceneEvent>(HandleLoadScene);
        }

        // 싱글톤 이벤트 필드는 지역 변수로 캡처 후 코루틴 클로저에 사용 — 후속 발행 변이 방지.
        private void HandleLoadScene(LoadSceneEvent e)
        {
            string sceneName = e.SceneName;
            int idx = e.BuildIndex;
            var preset = e.Preset;
            IEnumerator Midpoint() => LoadOp(sceneName, idx);
            uiChannel.RaiseEvent(UIEvents.OpenUIEvent.Init(UIId.Fade, new FadeRequestEvent(Midpoint, preset)));
        }

        private IEnumerator LoadOp(string name, int idx)
        {
            AsyncOperation op = string.IsNullOrEmpty(name)
                ? SceneManager.LoadSceneAsync(idx)
                : SceneManager.LoadSceneAsync(name);

            while (!op.isDone)
                yield return null;
        }
    }
}
