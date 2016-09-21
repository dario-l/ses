using System;
using System.Collections.Generic;
using System.Linq;
using Ses.Abstracts.Subscriptions;

namespace Ses.Subscriptions
{
    internal class HandlerRegistrar
    {
        private static readonly Type asyncHandlerType = typeof(IHandleAsync<>);
        private static readonly Type syncHandlerType = typeof(IHandle<>);
        private readonly IDictionary<Type, HandlerTypeInfo> _types;

        public HandlerRegistrar(IEnumerable<Type> handlerTypes)
        {
            _types = new Dictionary<Type, HandlerTypeInfo>();

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

        private static IList<Type> GetEventTypes(Type type, Type handlerType)
        {
            var interfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerType);

            var list = new List<Type>();
            foreach (var eventTypes in interfaces.Select(i => i.GetGenericArguments()))
            {
                list.AddRange(eventTypes);
            }
            return list;
        }

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
            public HandlerTypeInfo(Type handlerType, IList<Type> events, bool isAsync)
            {
                HandlerType = handlerType;
                Events = events;
                IsAsync = isAsync;
            }

            public Type HandlerType { get; private set; }
            public IList<Type> Events { get; private set; }
            public bool IsAsync { get; private set; }
        }
    }
}