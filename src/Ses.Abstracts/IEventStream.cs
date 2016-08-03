using System;
using System.Collections.Generic;

namespace Ses.Abstracts
{
    public interface IEventStream
    {
        Guid CommitId { get; }
        IEnumerable<IEvent> Events { get; }
        IDictionary<string, object> Metadata { get; set; }
    }
}