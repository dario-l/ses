using System.Collections.Generic;
using System.Linq;

namespace Ses.Abstracts
{
    public class ReadOnlyEventStream : IReadOnlyEventStream
    {
        private readonly List<IEvent> _events;

        public ReadOnlyEventStream(IEnumerable<IEvent> events, int committedVersion)
        {
            _events = events.ToList();
            CommittedVersion = committedVersion;
        }

        public IEnumerable<IEvent> CommittedEvents => _events.AsReadOnly();
        public int CommittedVersion { get; }
    }
}