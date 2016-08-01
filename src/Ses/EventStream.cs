using System;
using System.Collections.Generic;
using Ses.Abstracts;

namespace Ses
{
    public class EventStream : ReadOnlyEventStream, IEventStream
    {
        public EventStream(Guid id, Guid commitId) : base(id, new List<IEvent>(), 0)
        {
            CommitId = commitId;
        }

        public Guid CommitId { get; private set; }
    }
}