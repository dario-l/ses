using System;
using System.Collections.Generic;
using System.Linq;

namespace Ses.InMemory
{
    internal class InMemoryStream
    {
        private bool _isDeleted;

        public InMemoryStream(Guid streamId)
        {
            StreamId = streamId;
            Events = new List<InMemoryEventRecord>();
        }

        public List<InMemoryEventRecord> Events { get; }
        public Guid StreamId { get; }

        public int? GetCurrentVersion()
        {
            return _isDeleted ? (int?)null : Events.LastOrDefault()?.Version ?? 0;
        }

        public void AppendToStream(Guid commitId, int expectedVersion, EventRecord[] events, byte[] metadata)
        {
            switch (expectedVersion)
            {
                case ExpectedVersion.Any:
                    AppendToStreamExpectedVersionAny(commitId, expectedVersion, events, metadata);
                    return;
                case ExpectedVersion.NoStream:
                    AppendToStreamExpectedVersionNoStream(commitId, expectedVersion, events, metadata);
                    return;
                default:
                    AppendToStreamExpectedVersion(commitId, expectedVersion, events, metadata);
                    return;
            }
        }

        private void AppendToStreamExpectedVersion(Guid commitId, int expectedVersion, EventRecord[] events, byte[] metadata)
        {
            // Need to do optimistic concurrency check...
            var currentVersion = GetCurrentVersion();
            if (expectedVersion > currentVersion)
            {
                throw new WrongExpectedVersionException($"Expected version {expectedVersion} is lower than appended earlier to stream {StreamId}.");
            }

            if (expectedVersion < currentVersion)
            {
                // expectedVersion < currentVersion, Idempotency test
                for (var i = 0; i < events.Length; i++)
                {
                    var index = expectedVersion + i + 1;
                    if (index >= Events.Count)
                    {
                        throw new WrongExpectedVersionException($"Some events with expected version {expectedVersion} has been appended earlier to stream {StreamId}.");
                    }
                    if (Events[index].Version != events[i].Version)
                    {
                        throw new WrongExpectedVersionException($"Some events with expected version {expectedVersion} has been appended earlier to stream {StreamId}.");
                    }
                }
                return;
            }

            // expectedVersion == currentVersion)
            if (events.Any(newStreamEvent => Events.Any(x => x.Version == newStreamEvent.Version)))
            {
                throw new WrongExpectedVersionException($"Some events with expected version {expectedVersion} has been appended earlier to stream {StreamId}.");
            }

            AppendEvents(commitId, events, metadata);
        }

        private void AppendToStreamExpectedVersionNoStream(Guid commitId, int expectedVersion, EventRecord[] events, byte[] metadata)
        {
            if (Events.Count > 0)
            {
                //Already committed events, do idempotency check
                if (events.Length > Events.Count)
                {
                    throw new WrongExpectedVersionException($"Some events with expected version {expectedVersion} has been appended earlier to stream {StreamId}.");
                }

                if (events.Where((@event, index) => Events[index].Version != @event.Version).Any())
                {
                    throw new WrongExpectedVersionException($"Some events with expected version {expectedVersion} has been appended earlier to stream {StreamId}.");
                }
                return;
            }

            // None of the events were written previously...
            AppendEvents(commitId, events, metadata);
        }

        private void AppendToStreamExpectedVersionAny(Guid commitId, int expectedVersion, EventRecord[] events, byte[] metadata)
        {
            // idemponcy check - how many newEvents have already been written?
            var newEventIds = new HashSet<int>(events.Select(e => e.Version));
            newEventIds.ExceptWith(Events.Select(x => x.Version));

            if (newEventIds.Count == 0)
            {
                // All events have already been written, we're idempotent
                return;
            }

            if (newEventIds.Count != events.Length)
            {
                // Some of the events have already been written, bad request
                throw new WrongExpectedVersionException($"Some events with expected version {expectedVersion} has been appended earlier to stream {StreamId}.");
            }

            // None of the events were written previously...
            AppendEvents(commitId, events, metadata);
        }

        private void AppendEvents(Guid commitId, EventRecord[] events, byte[] metadata)
        {
            foreach (var record in events)
            {
                Events.Add(new InMemoryEventRecord(record.Version, record.ContractName, record.Payload, commitId, metadata));
            }
        }

        public void DeleteAllEvents()
        {
            Events.Clear();
            _isDeleted = true;
        }
    }
}