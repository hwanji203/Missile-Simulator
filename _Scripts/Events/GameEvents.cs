using Core;
using EventChannelSystem;
using MVP.Forms;
using MVP.Forms.Module.Fade;

namespace Events
{
    public static class GameEvents
    {
        public static readonly GameEndedEvent GameEndedEvent = new GameEndedEvent();
        public static readonly LoadSceneEvent LoadSceneEvent = new LoadSceneEvent();
        public static readonly GameStartEvent GameStartEvent = new GameStartEvent();
    }

    public class GameStartEvent : GameEvent
    {
    }
    
    public class GameEndedEvent : GameEvent
    {
        public ScoreTracker ScoreTracker { get; private set; }
        
        public GameEndedEvent Init(ScoreTracker scoreTracker)
        {
            ScoreTracker = scoreTracker;
            return this;
        }
    }

    public class LoadSceneEvent : GameEvent
    {
        public int BuildIndex { get; private set; }
        public string SceneName { get; private set; }
        public TransitionPreset Preset { get; private set; }

        // 공유 싱글톤 변이. RaiseEvent가 동기라 발행→구독 핸들러 처리가 끝날 때까지 안전.
        public LoadSceneEvent Init(int buildIndex, string sceneName, TransitionPreset preset = null)
        {
            BuildIndex = buildIndex;
            SceneName = sceneName;
            Preset = preset;
            return this;
        }

        public LoadSceneEvent Init(string sceneName, TransitionPreset preset = null) => Init(-1, sceneName, preset);
        public LoadSceneEvent Init(int buildIndex, TransitionPreset preset = null) => Init(buildIndex, null, preset);
    }
}
