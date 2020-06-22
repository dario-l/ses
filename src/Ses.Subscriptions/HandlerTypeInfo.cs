using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Ses.Subscriptions
{
    public class HandlerTypeInfo
    {
        internal HandlerTypeInfo(Type handlerType, Type[] eventTypes, bool isAsync)
        {
            HandlerType = handlerType;
            EventTypes = new HashSet<Type>(eventTypes);
            IsAsync = isAsync;
        }

        public bool IsAsync { get; }
        public Type HandlerType { get; }
        public HashSet<Type> EventTypes { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool ContainsEventType(Type eventType) => EventTypes.Contains(eventType);
    }
}