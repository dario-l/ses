using System;
using System.Collections.Generic;
using System.Linq;
using Ses.Abstracts.Subscriptions;

namespace Ses.Subscriptions
{
    internal class HandlerRegistrar
    {
        private readonly IDictionary<Type, IList<Type>> _types;

        public HandlerRegistrar(IEnumerable<Type> handlerTypes)
        {
            _types = new Dictionary<Type, IList<Type>>();
            foreach (var type in handlerTypes.Where(TypeIsHandler))
            {
                if (_types.ContainsKey(type)) continue;
                _types.Add(type, GetEventTypes(type));
            }
        }

        private static IList<Type> GetEventTypes(Type type)
        {
            var interfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandle<>));

            var list = new List<Type>();
            foreach (var eventTypes in interfaces.Select(i => i.GetGenericArguments()))
            {
                list.AddRange(eventTypes);
            }
            return list;
        }

        private static bool TypeIsHandler(Type type)
        {
            return type.IsClass && typeof(IHandle).IsAssignableFrom(type);
        }

        public IEnumerable<Type> RegisteredHandlerTypes => _types.Keys;

        public IEnumerable<Type> GetRegisteredEventTypesFor(Type handlerType)
        {
            return !_types.ContainsKey(handlerType) ? new List<Type>(0) : _types[handlerType];
        }
    }
}