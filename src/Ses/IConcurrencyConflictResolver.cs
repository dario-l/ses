using System;
using System.Collections.Generic;

namespace Ses
{
    public interface IConcurrencyConflictResolver
    {
        bool ConflictsWith(Type eventToCheck, IEnumerable<Type> previousEvents);
        void RegisterConflictList(Type eventDefinition, List<Type> conflictsWith);
    }
}