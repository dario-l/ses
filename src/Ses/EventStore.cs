using System;
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
            _settings.Persistor.OnReadEvent += OnEventRead;
            _settings.Persistor.OnReadSnapshot += OnSnapshotRead;
        }

        public IEventStoreAdvanced Advanced { get; }

        public IReadOnlyEventStream Load(Guid streamId, int fromVersion, bool pessimisticLock)
        {
            var events = _settings.Persistor.Load(streamId, fromVersion, pessimisticLock);
            if (events == null || events.Length == 0) return null;
            var snapshot = events[0] as IRestoredMemento;
            var currentVersion = CalculateCurrentVersion(fromVersion, events, snapshot);
            return new ReadOnlyEventStream(events, currentVersion);
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
                records[i] = new EventRecord(version, contractName, payload);
            }
            return records;
        }

        private IEvent OnEventRead(Guid streamId, string contractName, int version, byte[] payload)
        {
            var eventType = _settings.ContractsRegistry.GetType(contractName);
            var @event = _settings.Serializer.Deserialize<IEvent>(payload, eventType);
            if (@event == null)
                throw new InvalidCastException($"Deserialized payload from stream {streamId} is not an IEvent of type {eventType.FullName}.");

            if (_settings.UpConverterFactory == null) return @event;
            var upConverter = _settings.UpConverterFactory.CreateInstance(eventType);
            while (upConverter != null)
            {
                @event = ((dynamic)upConverter).Convert((dynamic)@event);
                upConverter = _settings.UpConverterFactory.CreateInstance(@event.GetType());
            }
            return @event;
        }

        private IRestoredMemento OnSnapshotRead(Guid streamId, string contractName, int version, byte[] payload)
        {
            var snapshotType = _settings.ContractsRegistry.GetType(contractName);
            var memento = _settings.Serializer.Deserialize<IMemento>(payload, snapshotType);
            if (memento == null)
                throw new InvalidCastException($"Deserialized payload from stream {streamId} is not an IMemento of type {snapshotType.FullName}.");

            if (_settings.UpConverterFactory == null) return new RestoredMemento(version, memento);
            var upConverter = _settings.UpConverterFactory.CreateInstance(snapshotType);
            while (upConverter != null)
            {
                memento = ((dynamic)upConverter).Convert((dynamic)memento);
                upConverter = _settings.UpConverterFactory.CreateInstance(memento.GetType());
            }
            return new RestoredMemento(version, memento);
        }

        public void Dispose()
        {
            _settings.Persistor.Dispose();
        }
    }
}