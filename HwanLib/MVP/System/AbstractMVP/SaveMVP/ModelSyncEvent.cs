using EventChannelSystem;

namespace MVP.System.AbstractMVP.SaveMVP
{
    public static class SaveMVPEvents
    {
        public static readonly ModelSyncEvent ModelSyncEvent = new ModelSyncEvent();
    }
    
    public class ModelSyncEvent : GameEvent
    {
        public string SaveKey { get; private set; }
        public string Data { get; private set; }

        public ModelSyncEvent Init(string saveKey, string data)
        {
            SaveKey = saveKey;
            Data = data;
            return this;
        }
    }
}
