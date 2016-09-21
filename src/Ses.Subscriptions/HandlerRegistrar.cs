using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Ses.Abstracts.Subscriptions;

namespace Ses.Subscriptions
{
    internal class HandlerRegistrar
    {
        private static readonly Type asyncHandlerType = typeof(IHandleAsync<>);
        private static readonly Type syncHandlerType = typeof(IHandle<>);
        private readonly Dictionary<Type, HandlerTypeInfo> _types;

        public HandlerRegistrar(IEnumerable<Type> handlerTypes)
        {
            _types = new Dictionary<Type, HandlerTypeInfo>(100);

            foreach (var type in handlerTypes.Where(TypeIsHandler))
            {
                if (_types.ContainsKey(type)) continue;

                var interfaces = type.GetInterfaces();
                var hasAsync = interfaces.Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == asyncHandlerType);
                var hasSync = interfaces.Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == syncHandlerType);

                if (hasAsync == hasSync)
                    throw new Exception($"Type '{type.FullName}' can not have mixed sync and async handlers implemented.");

                _types.Add(type, new HandlerTypeInfo(type, GetEventTypes(type, hasAsync ? asyncHandlerType : syncHandlerType), hasAsync));
            }
        }

        private static Type[] GetEventTypes(Type type, Type handlerType)
        {
            var interfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerType)
                .ToArray();

            var list = new List<Type>(interfaces.Length * 2);
            foreach (var i in interfaces)
            {
                var eventTypes = i.GetGenericArguments();
                list.AddRange(eventTypes);
            }
            return list.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TypeIsHandler(Type type) => type.IsClass && typeof(IHandle).IsAssignableFrom(type);

        public IEnumerable<Type> RegisteredHandlerTypes => _types.Keys;
        public IEnumerable<HandlerTypeInfo> RegisteredHandlerInfos => _types.Values;

        public HandlerTypeInfo GetHandlerInfoFor(Type handlerType)
        {
            HandlerTypeInfo result;
            _types.TryGetValue(handlerType, out result);
            return result;
        }

        public class HandlerTypeInfo
        {
            public HandlerTypeInfo(Type handlerType, Type[] eventTypes, bool isAsync)
            {
                HandlerType = handlerType;
                EventTypes = eventTypes;
                IsAsync = isAsync;
            }

            public bool IsAsync { get; private set; }
            public Type HandlerType { get; private set; }
            public Type[] EventTypes { get; }
            

            public bool ContainsEventType(Type eventType)
            {
                foreach (var t in EventTypes)
                {
                    if (t == eventType) return true;
                }
                return false;
            }
        }
    }
}