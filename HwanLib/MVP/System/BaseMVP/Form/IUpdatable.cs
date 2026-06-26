using System;
using MVP.UIData;

namespace MVP.System.BaseMVP.Form
{
    public interface IUpdatable
    {
        /// <summary>Presenter가 Model의 UIParam M() 델리게이트를 주입한다.</summary>
        void BindUpdateSource(Func<UIParam> source);

        /// <summary>주입된 소스로 현재 값을 끌어와 비주얼 갱신.</summary>
        void UpdateForm();
    }
}
