using System;
using System.Collections.Generic;
using Ses.Abstracts;

namespace Ses
{
    public class EventStream : IEventStream
    {
        public EventStream(Guid commitId)
        {
            CommitId = commitId;
        }

        public Guid CommitId { get; }
        public IEvent[] Events { get; }
        public IDictionary<string, object> Metadata { get; set; }
    }
}