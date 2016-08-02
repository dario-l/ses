using System;
using System.Collections.Generic;

namespace Ses
{
    public interface IConcurrencyConflictResolver : IConcurrencyConflictResolverRegister
    {
        bool ConflictsWith(Type eventToCheck, IEnumerable<Type> previousEvents);
    }

    public interface IConcurrencyConflictResolverRegister
    {
        void RegisterConflictList(Type eventDefinition, params Type[] conflictsWith);
    }
}