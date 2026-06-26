namespace MVP.UIData
{
    public class UISwapParam : UIParam
    {
        public int ItemEnum;
        public int TargetIndex;

        public UISwapParam Init(int itemEnum, int targetIndex)
        {
            ItemEnum = itemEnum;
            TargetIndex = targetIndex;

            return this;
        }
    }
}