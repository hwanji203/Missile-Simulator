namespace SaveSystem
{
    public interface IRestorable : ISaveable
    {
        void RestoreData(string data);
    }
}