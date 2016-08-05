using System;
using System.Collections.Generic;

namespace Ses.Abstracts
{
    public class EventStream : IEventStream
    {
        public EventStream(Guid commitId, IEnumerable<IEvent> events, bool isLockable = false)
        {
            CommitId = commitId;
            Events = events;
            IsLockable = isLockable;
        }

        public bool IsLockable { get; }
        public Guid CommitId { get; }
        public IEnumerable<IEvent> Events { get; }
        public IDictionary<string, object> Metadata { get; set; }
    }
}