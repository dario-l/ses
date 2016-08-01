using System.Collections.Generic;
using System.Linq;

namespace Ses.Abstracts
{
    public class ReadOnlyEventStream : IReadOnlyEventStream
    {
        private readonly List<IEvent> _events;

        public ReadOnlyEventStream(IEnumerable<IEvent> events, int currentVersion)
        {
            _events = events.ToList();
            CurrentVersion = currentVersion;
        }

        public IReadOnlyList<IEvent> Events => _events.AsReadOnly();
        public int CurrentVersion { get; }
    }
}