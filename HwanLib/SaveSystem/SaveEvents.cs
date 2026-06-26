using EventChannelSystem;

namespace SaveSystem
{
    public static class SaveEvents
    {
        public static readonly StoreDataEvent StoreDataEvent = new StoreDataEvent();
        public static readonly RestoreDataEvent RestoreDataEvent = new RestoreDataEvent();
        public static readonly SyncDataEvent SyncDataEvent = new SyncDataEvent();
    }

    public class StoreDataEvent : GameEvent
    {
        //비워둔다.
    }

    public class RestoreDataEvent : GameEvent
    {
        //이것두 비워둔다.
    }
    
    public class SyncDataEvent : GameEvent
    {
        public int SaveId;
        public string SaveData;
        
        public SyncDataEvent Init(int saveId, string saveData)
        {
            SaveId = saveId;
            SaveData = saveData;
            
            return this;
        }
    }
}