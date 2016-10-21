using System;
using System.Collections.Generic;
using System.Linq;
using Ses.Abstracts;

namespace Ses
{
    internal partial class EventStore : IEventStore
    {
        private readonly IEventStoreSettings _settings;

        public EventStore(IEventStoreSettings settings)
        {
            Advanced = new EventStoreAdvanced(settings);
            _settings = settings;
        }

        public IEventStoreAdvanced Advanced { get; }

        public IReadOnlyEventStream Load(Guid streamId, int fromVersion, bool pessimisticLock)
        {
            var records = _settings.Persistor.Load(streamId, fromVersion, pessimisticLock);
            if (records == null || records.Length == 0) return null;
            return CreateReadOnlyStream(streamId, fromVersion, records);
        }

        private IReadOnlyEventStream CreateReadOnlyStream(Guid streamId, int fromVersion, EventRecord[] records)
        {
            var snapshotRecord = records[0];
            var snapshot = snapshotRecord.Kind == EventRecord.RecordKind.Snapshot
                ? DeserializeSnapshot(streamId, snapshotRecord.ContractName, snapshotRecord.Version, snapshotRecord.Payload)
                : null;

            var events = DeserializeEvents(streamId, records, snapshot);
            var currentVersion = CalculateCurrentVersion(fromVersion, events, snapshot);
            return new ReadOnlyEventStream(events, currentVersion);
        }

        private IEvent[] DeserializeEvents(Guid streamId, EventRecord[] records, IRestoredMemento snapshot)
        {
            var events = new List<IEvent>(records.Length);
            if (snapshot != null) { events.Add(snapshot); }
            var start = snapshot == null ? 0 : 1;
            for (var i = start; i < records.Length; i++)
            {
                var record = records[i];
                events.Add(DeserializeEvent(streamId, record.ContractName, record.Version, record.Payload));
            }
            return events.ToArray();
        }

        public void SaveChanges(Guid streamId, int expectedVersion, IEventStream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (expectedVersion < ExpectedVersion.Any)
                throw new InvalidOperationException($"Expected version {expectedVersion} for stream {streamId} is invalid.");
            if (stream.Events.Length == 0) return;
            TrySaveChanges(streamId, expectedVersion, stream);
        }

        private void TrySaveChanges(Guid streamId, int expectedVersion, IEventStream stream)
        {
            _settings.Logger.Trace("Saving changes for stream '{0}' with commit '{1}'...", streamId, stream.CommitId);

            var metadata = stream.Metadata != null ? _settings.Serializer.Serialize(stream.Metadata, stream.Metadata.GetType()) : null;

            var tryCounter = 0;
            while (true)
            {
                try
                {
                    var events = CreateEventRecords(expectedVersion, stream);

                    _settings.Persistor.SaveChanges(
                        streamId,
                        stream.CommitId,
                        expectedVersion,
                        events,
                        metadata,
                        stream.IsLockable);

                    return;
                }
                catch (WrongExpectedVersionException e)
                {
                    if (_settings.ConcurrencyConflictResolver == null) throw;

                    if (tryCounter >= 3)
                    {
                        _settings.Logger.Error("Retrying committing stream '{0}' excided limit and throwing concurrency exception.", streamId);
                        throw;
                    }
                    tryCounter++;
                    _settings.Logger.Debug("Retrying committing stream '{0}' for {1} time...", streamId, tryCounter);
                    var previousStream = Load(streamId, e.ConflictedVersion - 1, false);
                    var previousEventTypes = previousStream.CommittedEvents.Select(x => x.GetType()).ToArray();
                    var conflictedEventType = _settings.ContractsRegistry.GetType(e.ConflictedContractName);

                    if (_settings.ConcurrencyConflictResolver.ConflictsWith(conflictedEventType, previousEventTypes)) throw;

                    expectedVersion = previousStream.CommittedVersion + stream.Events.Length;
                }
            }
        }

        private EventRecord[] CreateEventRecords(int expectedVersion, IEventStream stream)
        {
            if (expectedVersion < 0) expectedVersion = 0;
            var records = new EventRecord[stream.Events.Length];
            for(var i = 0; i < stream.Events.Length; i++)
            {
                var @event = stream.Events[i];
                var eventType = @event.GetType();
                var version = ++expectedVersion;
                var contractName = _settings.ContractsRegistry.GetContractName(eventType);
                var payload = _settings.Serializer.Serialize(@event, eventType);
                records[i] = EventRecord.Event(contractName, version, payload);
            }
            return records;
        }

        private IEvent DeserializeEvent(Guid streamId, string contractName, int version, byte[] payload)
        {
            var eventType = _settings.ContractsRegistry.GetType(contractName);
            var @event = _settings.Serializer.Deserialize<IEvent>(payload, eventType);
            if (@event == null)
                throw new InvalidCastException($"Deserialized payload with version {version} from stream {streamId} is not an IEvent of type {eventType.FullName}.");

            return _settings.UpConverterFactory == null ? @event : UpConvert(eventType, @event);
        }

        private IRestoredMemento DeserializeSnapshot(Guid streamId, string contractName, int version, byte[] payload)
        {
            var snapshotType = _settings.ContractsRegistry.GetType(contractName);
            var memento = _settings.Serializer.Deserialize<IMemento>(payload, snapshotType);
            if (memento == null)
                throw new InvalidCastException($"Deserialized payload from stream {streamId} is not an IMemento of type {snapshotType.FullName}.");

            return _settings.UpConverterFactory == null
                ? new RestoredMemento(version, memento)
                : new RestoredMemento(version, UpConvert(snapshotType, memento));
        }

        private T UpConvert<T>(Type eventType, T @event) where T : IEvent
        {
            var upConverter = _settings.UpConverterFactory.CreateInstance(eventType);
            while (upConverter != null)
            {
                @event = ((dynamic)upConverter).Convert((dynamic)@event);
                upConverter = _settings.UpConverterFactory.CreateInstance(@event.GetType());
            }
            return @event;
        }

        public void Dispose()
        {
            _settings.Persistor.Dispose();
        }
    }
}