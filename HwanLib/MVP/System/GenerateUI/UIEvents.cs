using EventChannelSystem;

namespace MVP.System.GenerateUI
{
    public static class UIEvents
    {
        public static readonly OpenUIEvent OpenUIEvent = new OpenUIEvent();
        public static readonly CloseUIEvent CloseUIEvent = new CloseUIEvent();
        public static readonly ToggleUIEvent ToggleUIEvent = new ToggleUIEvent();
    }
    
    public class OpenUIEvent : GameEvent
    {
        public UIId   Id;
        public GameEvent Payload;
        
        public OpenUIEvent Init(UIId id, GameEvent evt)
        {
            Id = id;
            Payload = evt;
            return this;
        }
    }

    public class CloseUIEvent : GameEvent
    {
        public UIId Id;
        
        public CloseUIEvent Init(UIId id)
        {
            Id = id;
            return this;
        }
    }

    // 토글 요청. 대상이 열려 있으면 닫고, 닫혀 있으면 연다(UIManager가 IsOpen으로 분기).
    public class ToggleUIEvent : GameEvent
    {
        public UIId Id;
                
        public ToggleUIEvent Init(UIId id)
        {
            Id = id;
            return this;
        }
    }
}
