using System;
using System.Collections.Generic;

namespace Ses.Abstracts
{
    public interface IEventStream
    {
        Guid CommitId { get; }
        IList<IEvent> Events { get; }
        IDictionary<string, object> Metadata { get; }
    }
}