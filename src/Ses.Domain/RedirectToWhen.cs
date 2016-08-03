using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Ses.Domain
{
    /// <summary>
    /// Simple helper, that looks up and calls the proper overload of
    /// On(SpecificEventType event). Reflection information is cached statically once per type.
    /// </summary>
    internal static class RedirectToWhen
    {
        static readonly MethodInfo internalPreserveStackTraceMethod =
            typeof(Exception).GetMethod("InternalPreserveStackTrace", BindingFlags.Instance | BindingFlags.NonPublic);

        private const string methodName = "On";

        static class Cache<T>
        {
            public static readonly IDictionary<Type, MethodInfo> Dict = typeof(T)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.Name == methodName && m.GetParameters().Length == 1)
                .ToDictionary(m => m.GetParameters().First().ParameterType, m => m);
        }

        [DebuggerNonUserCode]
        public static void InvokeEventOptional<T>(T instance, object @event)
        {
            MethodInfo info;
            var type = @event.GetType();
            if (!Cache<T>.Dict.TryGetValue(type, out info)) return;

            try
            {
                info.Invoke(instance, new[] { @event });
            }
            catch (TargetInvocationException ex)
            {
                internalPreserveStackTraceMethod?.Invoke(ex.InnerException, new object[0]);
                throw ex.InnerException;
            }
        }
    }
}