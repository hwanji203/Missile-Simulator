namespace MVP.UIData
{
    public class UIIntParam : UIParam
    {
        public int Value;

        public UIIntParam Init(int value)
        {
            Value = value;
            return this;
        }
    }
}