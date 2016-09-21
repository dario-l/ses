using System;
using System.Collections.Generic;

namespace Ses.Abstracts
{
    public class EventStream : IEventStream
    {
        public EventStream(Guid commitId, IEvent[] events, bool isLockable = false)
        {
            CommitId = commitId;
            Events = events;
            IsLockable = isLockable;
        }

        public bool IsLockable { get; }
        public Guid CommitId { get; }
        public IEvent[] Events { get; }
        public Dictionary<string, object> Metadata { get; set; }
    }
}