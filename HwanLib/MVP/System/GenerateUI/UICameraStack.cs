using MVP.System.BaseMVP;
using UnityEngine;

namespace MVP.System.GenerateUI
{
    public static class UICameraStack
    {
        // WorldPosition Canvas에만 worldCamera를 부여. 일반 Overlay UI는 무시.
        public static void AssignWorldCamera(BasePresenter presenter, Camera worldCamera)
        {
            if (!presenter.IsWorldPosition) return;
            presenter.transform.GetChild(0).GetComponent<Canvas>().worldCamera = worldCamera;
        }
    }
}
