using System;

namespace Ses.Conflicts
{
    public interface IConcurrencyConflictResolver : IConcurrencyConflictResolverRegister
    {
        bool ConflictsWith(Type eventToCheck, Type[] previousEventTypes);
    }

    public interface IConcurrencyConflictResolverRegister
    {
        void RegisterConflicts(Type eventDefinition, params Type[] conflictsWith);
    }
}