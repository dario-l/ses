using System;
using System.Collections.Generic;

namespace Ses.Abstracts
{
    public interface IEventStream
    {
        bool IsLockable { get; }
        Guid CommitId { get; }
        IEvent[] Events { get; }
        Dictionary<string, object> Metadata { get; set; }
    }
}