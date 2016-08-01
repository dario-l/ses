using System.Collections.Generic;

namespace Ses.Abstracts
{
    public interface IReadOnlyEventStream
    {
        IReadOnlyList<IEvent> Events { get; }
        int CurrentVersion { get; }
    }
}