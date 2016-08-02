using System.Collections.Generic;

namespace Ses.Abstracts
{
    public interface IReadOnlyEventStream
    {
        IEnumerable<IEvent> CommittedEvents { get; }
        int CommittedVersion { get; }
    }
}