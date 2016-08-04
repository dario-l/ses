using System;
using System.Collections.Generic;
using System.Linq;
using Ses.Abstracts;

namespace Ses.Conflicts
{
    public class DefaultConcurrencyConflictResolver : IConcurrencyConflictResolver
    {
        private readonly Dictionary<Type, List<Type>> _conflictRegister;

        public DefaultConcurrencyConflictResolver()
        {
            _conflictRegister = new Dictionary<Type, List<Type>>();
        }

        public virtual bool ConflictsWith(Type eventToCheck, IEnumerable<Type> previousEvents)
        {
            if (eventToCheck == null) throw new ArgumentNullException(nameof(eventToCheck));
            if (previousEvents == null) throw new ArgumentNullException(nameof(previousEvents));
            // If type not registered assume it not conflicts
            return _conflictRegister.ContainsKey(eventToCheck)
                && previousEvents.Any(previousEvent => _conflictRegister[eventToCheck].Any(et => et == previousEvent));
        }

        public void RegisterConflictList(Type eventDefinition, params Type[] conflictsWith)
        {
            if (eventDefinition == null) throw new ArgumentNullException(nameof(eventDefinition));
            if (conflictsWith == null) throw new ArgumentNullException(nameof(conflictsWith));
            if (conflictsWith.Length == 0) throw new ArgumentException("Conflicting types array can not be empty.", nameof(conflictsWith));
            if (!typeof(IEvent).IsAssignableFrom(eventDefinition)) throw new ArgumentException("eventDefinition must be of type IEvent");
            if (conflictsWith.Any(c => !typeof(IEvent).IsAssignableFrom(c))) throw new ArgumentException("all conflicts with type must be of type IEvent");
            if (_conflictRegister.ContainsKey(eventDefinition))
                _conflictRegister.Remove(eventDefinition);

            _conflictRegister.Add(eventDefinition, conflictsWith.ToList());
        }
    }
}