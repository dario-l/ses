using System;
using System.Collections.Generic;
using System.Linq;
using Ses.Abstracts;

namespace Ses.Conflicts
{
    public class ConcurrencyConflictResolver : IConcurrencyConflictResolver
    {
        private readonly Dictionary<Type, List<Type>> _conflictRegister;

        public ConcurrencyConflictResolver()
        {
            _conflictRegister = new Dictionary<Type, List<Type>>();
        }

        public bool ConflictsWith(Type eventToCheck, IEnumerable<Type> previousEvents)
        {
            // If type not registered assume the worst and say it conflicts
            return !_conflictRegister.ContainsKey(eventToCheck)
                || previousEvents.Any(previousEvent => _conflictRegister[eventToCheck].Any(et => et == previousEvent));
        }

        public void RegisterConflictList(Type eventDefinition, List<Type> conflictsWith)
        {
            if (!eventDefinition.IsSubclassOf(typeof(IEvent))) throw new ArgumentException("eventDefinition must be of type IEvent");
            if (conflictsWith.Any(c => c.IsSubclassOf(typeof(IEvent)) == false)) throw new ArgumentException("all conflicts with type must be of type IEvent");
            if (_conflictRegister.ContainsKey(eventDefinition))
                _conflictRegister.Remove(eventDefinition);

            _conflictRegister.Add(eventDefinition, conflictsWith);
        }
    }
}