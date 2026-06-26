namespace MVP.UIData
{
    public class UICooldownParam : UIParam
    {
        public float Cooldown;
        public float Ratio;

        public UICooldownParam Init(float cooldown, float ratio)
        {
            Cooldown = cooldown;
            Ratio = ratio;
            
            return this;
        }
    }
}