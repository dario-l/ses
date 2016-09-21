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

        public void Append(Guid commitId, int expectedVersion, List<EventRecord> events, byte[] metadata)
        {
            switch (expectedVersion)
            {
                case ExpectedVersion.Any:
                    AppendWithExpectedVersionAny(commitId, expectedVersion, events, metadata);
                    return;
                case ExpectedVersion.NoStream:
                    AppendToStreamExpectedVersionNoStream(commitId, expectedVersion, events, metadata);
                    return;
                default:
                    AppendToStreamExpectedVersion(commitId, expectedVersion, events, metadata);
                    return;
            }
        }

        private void AppendToStreamExpectedVersion(Guid commitId, int expectedVersion, List<EventRecord> events, byte[] metadata)
        {
            var currentVersion = GetCurrentVersion();
            if (expectedVersion > currentVersion)
            {
                throw new WrongExpectedVersionException($"Expected version {expectedVersion} is lower than appended earlier to stream {StreamId}.");
            }

            if (expectedVersion < currentVersion)
            {
                for (var i = 0; i < events.Count; i++)
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

            if (events.Any(newStreamEvent => Events.Any(x => x.Version == newStreamEvent.Version)))
            {
                throw new WrongExpectedVersionException($"Some events with expected version {expectedVersion} has been appended earlier to stream {StreamId}.");
            }

            AppendEvents(commitId, events, metadata);
        }

        private void AppendToStreamExpectedVersionNoStream(Guid commitId, int expectedVersion, List<EventRecord> events, byte[] metadata)
        {
            if (Events.Count > 0)
            {
                if (events.Count > Events.Count)
                {
                    throw new WrongExpectedVersionException($"Some events with expected version {expectedVersion} has been appended earlier to stream {StreamId}.");
                }

                if (events.Where((@event, index) => Events[index].Version != @event.Version).Any())
                {
                    throw new WrongExpectedVersionException($"Some events with expected version {expectedVersion} has been appended earlier to stream {StreamId}.");
                }
                return;
            }

            AppendEvents(commitId, events, metadata);
        }

        private void AppendWithExpectedVersionAny(Guid commitId, int expectedVersion, List<EventRecord> events, byte[] metadata)
        {
            var newEventIds = new HashSet<int>(events.Select(e => e.Version));
            newEventIds.ExceptWith(Events.Select(x => x.Version));

            if (newEventIds.Count == 0) return; // all events are in
            if (newEventIds.Count != events.Count)
            {
                throw new WrongExpectedVersionException($"Some events with expected version {expectedVersion} has been appended earlier to stream {StreamId}.");
            }

            AppendEvents(commitId, events, metadata);
        }

        private void AppendEvents(Guid commitId, List<EventRecord> events, byte[] metadata)
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