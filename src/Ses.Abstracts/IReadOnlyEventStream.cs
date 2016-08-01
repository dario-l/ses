using System;
using System.Collections.Generic;

namespace Ses.Abstracts
{
    public interface IReadOnlyEventStream
    {
        Guid ID { get; }
        IReadOnlyList<IEvent> Events { get; }
        int CurrentVersion { get; }
    }
}