using System.Collections.Generic;

namespace Ses.Abstracts
{
    public class ReadOnlyEventStream : IReadOnlyEventStream
    {
        public ReadOnlyEventStream(IEnumerable<IEvent> events, int committedVersion)
        {
            CommittedEvents = events;
            CommittedVersion = committedVersion;
        }

        public IEnumerable<IEvent> CommittedEvents { get; }
        public int CommittedVersion { get; }
    }
}