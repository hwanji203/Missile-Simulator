using MVP.System.BaseMVP;

namespace MVP.System.AbstractMVP.SaveMVP
{
    public interface ISaveableModel : IModel
    {
        string StoreData();
        void RestoreData(string data);
    }
}
