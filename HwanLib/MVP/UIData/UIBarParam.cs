namespace MVP.UIData
{
    public class UIBarParam : UIParam
    {
        public float MaxValue;
        public float CurrentValue;

        public UIBarParam Init(float maxValue, float currentValue)
        {
            CurrentValue = currentValue;
            MaxValue = maxValue;
            return this;
        }
    }
}