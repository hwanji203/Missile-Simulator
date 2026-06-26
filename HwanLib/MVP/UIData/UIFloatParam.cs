namespace MVP.UIData
{
    public class UIFloatParam : UIParam
    {
        public float Value;

        public UIFloatParam Init(float value)
        {
            Value = value;
            return this;
        }
    }
}