using System;
using System.Collections.Generic;
using System.Linq;

namespace Ses.Abstracts
{
    public class EventStream : IEventStream
    {
        public EventStream(Guid commitId, IEnumerable<IEvent> events = null)
        {
            CommitId = commitId;
            Events = events?.ToList() ?? new List<IEvent>();
            Metadata = new Dictionary<string, object>();
        }

        public Guid CommitId { get; }
        public IList<IEvent> Events { get; }
        public IDictionary<string, object> Metadata { get; }

        public void Append(IEnumerable<IEvent> events)
        {
            foreach (var @event in events)
            {
                Events.Add(@event);
            }
        }
    }
}