using System;
using MVP.System.BaseMVP;

namespace MVP.System
{
    public static class MVPTypeUtil
    {
        /// <summary>
        /// presenterType의 상속 사슬을 거슬러 올라가 BasePresenter&lt;TModel,TView&gt;를 찾고
        /// 그 제네릭 인자 (Model, View) 타입을 반환한다. 제네릭 베이스가 없으면 (null, null).
        /// </summary>
        public static (Type model, Type view) GetModelView(Type presenterType)
        {
            for (Type t = presenterType; t != null; t = t.BaseType)
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(BasePresenter<,>))
                {
                    Type[] args = t.GetGenericArguments();
                    return (args[0], args[1]);
                }
            }

            return (null, null);
        }
    }
}
