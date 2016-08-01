using System;
using System.Collections.Generic;
using System.Linq;

namespace Ses.Abstracts
{
    public class ReadOnlyEventStream
    {
        private readonly List<IEvent> _events;

        public ReadOnlyEventStream(Guid id, IEnumerable<IEvent> events, int currentVersion)
        {
            ID = id;
            _events = events.ToList();
            CurrentVersion = currentVersion;
        }

        public Guid ID { get; private set; }
        public IReadOnlyList<IEvent> Events => _events.AsReadOnly();
        public int CurrentVersion { get; private set; }
    }
}