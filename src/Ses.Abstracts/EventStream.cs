using System;
using System.Collections.Generic;

namespace Ses.Abstracts
{
    public class EventStream : ReadOnlyEventStream
    {
        public EventStream(Guid id, Guid commitId) : base(id, new List<IEvent>(), 0)
        {
            CommitId = commitId;
        }

        public Guid CommitId { get; private set; }
    }
}