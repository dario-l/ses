using System;

namespace Ses.Conflicts
{
    public interface IConcurrencyConflictResolver : IConcurrencyConflictResolverRegister
    {
        bool ConflictsWith(Type eventToCheck, Type[] previousEventTypes);
    }

    public interface IConcurrencyConflictResolverRegister
    {
        void RegisterConflictList(Type eventDefinition, params Type[] conflictsWith);
    }
}