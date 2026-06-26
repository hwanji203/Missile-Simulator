using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MVP.UIData;

namespace MVP.System
{
    /// <summary>
    /// Model의 메서드를 규약(interact: void M(UIParam) / update: UIParam M())에 따라
    /// 탐색하고 인스턴스에 바인딩한다. 타입 리플렉션이 아닌 메서드 바인딩만 담당.
    /// </summary>
    public static class MVPBinding
    {
        private const BindingFlags Flags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static bool IsInteract(MethodInfo m) =>
            m.ReturnType == typeof(void)
            && m.GetParameters().Length == 1
            && m.GetParameters()[0].ParameterType == typeof(UIParam)
            && !IsCompilerGenerated(m);

        private static bool IsUpdate(MethodInfo m) =>
            m.ReturnType == typeof(UIParam)
            && m.GetParameters().Length == 0
            && !IsCompilerGenerated(m);

        private static bool IsCompilerGenerated(MethodInfo m) =>
            m.Name.IndexOf('<') >= 0 || m.Name.IndexOf('>') >= 0;

        public static IEnumerable<string> InteractMethodNames(Type modelType) =>
            modelType.GetMethods(Flags).Where(IsInteract).Select(m => m.Name).Distinct();

        public static IEnumerable<string> UpdateMethodNames(Type modelType) =>
            modelType.GetMethods(Flags).Where(IsUpdate).Select(m => m.Name).Distinct();

        public static Action<UIParam> ResolveInteract(object model, string methodName)
        {
            if (model == null || string.IsNullOrEmpty(methodName)) return null;
            MethodInfo m = model.GetType().GetMethods(Flags)
                .FirstOrDefault(x => x.Name == methodName && IsInteract(x));
            return m == null ? null : (Action<UIParam>)m.CreateDelegate(typeof(Action<UIParam>), model);
        }

        public static Func<UIParam> ResolveUpdate(object model, string methodName)
        {
            if (model == null || string.IsNullOrEmpty(methodName)) return null;
            MethodInfo m = model.GetType().GetMethods(Flags)
                .FirstOrDefault(x => x.Name == methodName && IsUpdate(x));
            return m == null ? null : (Func<UIParam>)m.CreateDelegate(typeof(Func<UIParam>), model);
        }
    }
}
