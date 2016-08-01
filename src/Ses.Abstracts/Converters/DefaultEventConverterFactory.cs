using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ses.Abstracts.Converters
{
    public class DefaultEventConverterFactory : IEventConverterFactory
    {
        private readonly IDictionary<Type, Type> _converters;

        public DefaultEventConverterFactory(params Assembly[] assemblies)
        {
            _converters = new Dictionary<Type, Type>();
            foreach (var assembly in assemblies)
            {
                foreach (var converterType in assembly.GetTypes().Where(IsConverter))
                {
                    var eventType =
                        converterType.GetInterfaces()
                            .First(x => x.IsGenericType && typeof(IUpconvertEvent).IsAssignableFrom(x))
                            .GetGenericArguments()
                            .First();
                    _converters.Add(eventType, converterType);
                }
            }
        }

        private static bool IsConverter(Type type)
        {
            return typeof(IUpconvertEvent).IsAssignableFrom(type);
        }

        public IUpconvertEvent CreateInstance(Type eventType)
        {
            Type converter;
            return (!_converters.TryGetValue(eventType, out converter) ? null : Activator.CreateInstance(converter)) as IUpconvertEvent;
        }
    }
}