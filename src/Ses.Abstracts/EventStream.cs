using System;
using System.Collections.Generic;

namespace Ses.Abstracts
{
    public class EventStream : IEventStream
    {
        public EventStream(Guid commitId, IEnumerable<IEvent> events)
        {
            CommitId = commitId;
            Events = events;
        }

        public Guid CommitId { get; }
        public IEnumerable<IEvent> Events { get; }
        public IDictionary<string, object> Metadata { get; set; }
    }
}