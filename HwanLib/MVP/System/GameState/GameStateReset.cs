using UnityEngine;
using UnityEngine.SceneManagement;

namespace MVP.System.GameState
{
    /// <summary>
    /// 씬 전환 시 <see cref="TimeManager"/>/<see cref="CursorManager"/>의 holder 잔재를 청소한다.
    /// holder(Presenter 등)가 Resume/Lock 없이 파괴되면 시간이 영영 멈추거나 커서가
    /// 자유로 남는 사고가 나므로, 씬 로드마다 강제로 기본 상태로 되돌린다.
    /// </summary>
    internal static class GameStateReset
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Hook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TimeManager.Clear();
            CursorManager.Clear();
        }
    }
}
