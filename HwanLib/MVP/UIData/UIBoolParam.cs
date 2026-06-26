namespace MVP.UIData
{
    public class UIBoolParam : UIParam
    {
        public bool Value;

        public UIBoolParam Init(bool value)
        {
            Value = value;
            return this;
        }
    }
}