namespace SaveSystem
{
    public interface IStorable : ISaveable
    {
        string StoreData();
    }
}