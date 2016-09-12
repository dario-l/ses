using System;
using System.Collections.Generic;

namespace Ses.Abstracts.Subscriptions
{
    public class EventEnvelope
    {
        public EventEnvelope(Guid streamId, Guid commitId, DateTime createdAtUtc, long sequenceId, int version, IEvent @event, IDictionary<string, object> metadata)
        {
            StreamId = streamId;
            CommitId = commitId;
            CreatedAtUtc = createdAtUtc;
            SequenceId = sequenceId;
            Version = version;
            Event = @event;
            Metadata = metadata;
        }

        public DateTime CreatedAtUtc { get; private set; }
        public Guid StreamId { get; private set; }
        public Guid CommitId { get; private set; }
        public IDictionary<string, object> Metadata { get; private set; }
        public long SequenceId { get; private set; }
        public int Version { get; private set; }
        public IEvent Event { get; private set; }
    }
}