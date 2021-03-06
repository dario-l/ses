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

        public virtual bool ConflictsWith(Type eventToCheck, Type[] previousEventTypes)
        {
            if (eventToCheck == null) throw new ArgumentNullException(nameof(eventToCheck));
            if (previousEventTypes == null) throw new ArgumentNullException(nameof(previousEventTypes));
            // If type not registered assume the worst and say it conflicts
            if (!_conflictRegister.ContainsKey(eventToCheck)) return true;

            var any1 = false;
            foreach (var previousType in previousEventTypes)
            {
                if (_conflictRegister.ContainsKey(eventToCheck))
                {
                    var any2 = false;
                    foreach (var registeredType in _conflictRegister[eventToCheck])
                    {
                        if (registeredType != previousType) continue;
                        any2 = true;
                        break;
                    }
                    if (!any2) continue;
                }
                any1 = true;
                break;
            }
            return any1;
        }

        public void RegisterConflicts(Type eventDefinition, params Type[] conflictsWith)
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