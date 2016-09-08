using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ses.Abstracts;

namespace Ses
{
    internal class EventStore : IEventStore
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

        private IEvent OnEventRead(Guid streamId, string contractName, int version, byte[] payload)
        {
            var eventType = _settings.ContractsRegistry.GetType(contractName);
            var @event = _settings.Serializer.Deserialize<IEvent>(payload, eventType);
            if (@event == null) throw new InvalidCastException($"Deserialized payload from stream {streamId} is not an IEvent of type {eventType.FullName}.");

            if (_settings.UpConverterFactory != null)
            {
                var upConverter = _settings.UpConverterFactory.CreateInstance(eventType);
                while (upConverter != null)
                {
                    @event = ((dynamic)upConverter).Convert(@event);
                    upConverter = _settings.UpConverterFactory.CreateInstance(@event.GetType());
                }
            }
            return @event;
        }

        private IRestoredMemento OnSnapshotRead(Guid streamId, string contractName, int version, byte[] payload)
        {
            var snapshotType = _settings.ContractsRegistry.GetType(contractName);
            var memento = _settings.Serializer.Deserialize<IMemento>(payload, snapshotType);
            if (memento == null) throw new InvalidCastException($"Deserialized payload from stream {streamId} is not an IMemento of type {snapshotType.FullName}.");

            if (_settings.UpConverterFactory != null)
            {
                var upConverter = _settings.UpConverterFactory.CreateInstance(snapshotType);
                while (upConverter != null)
                {
                    memento = ((dynamic)upConverter).Convert(memento);
                    upConverter = _settings.UpConverterFactory.CreateInstance(memento.GetType());
                }
            }
            return new RestoredMemento(version, memento);
        }

        public async Task<IReadOnlyEventStream> LoadAsync(Guid streamId, bool pessimisticLock, CancellationToken cancellationToken = default(CancellationToken))
        {
            var events = await _settings.Persistor.LoadAsync(streamId, 1, pessimisticLock, cancellationToken);
            if (events == null || events.Count == 0) return null;
            var snapshot = events[0] as IRestoredMemento;
            var currentVersion = snapshot?.Version + (events.Count - 1) ?? events.Count;
            return new ReadOnlyEventStream(events, currentVersion);
        }

        public IReadOnlyEventStream Load(Guid streamId, bool pessimisticLock)
        {
            var events = _settings.Persistor.Load(streamId, 1, pessimisticLock);
            if (events == null || events.Count == 0) return null;
            var snapshot = events[0] as IRestoredMemento;
            var currentVersion = snapshot?.Version + (events.Count - 1) ?? events.Count;
            return new ReadOnlyEventStream(events, currentVersion);
        }

        public void SaveChanges(Guid streamId, int expectedVersion, IEventStream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (expectedVersion < ExpectedVersion.Any) throw new InvalidOperationException($"Expected version {expectedVersion} for stream {streamId} is invalid.");
            if (!stream.Events.Any()) return;
            _settings.Logger.Debug("Saving changes for stream '{0}' with commit '{1}'...", streamId, stream.CommitId);

            var metadata = stream.Metadata != null ? _settings.Serializer.Serialize(stream.Metadata, stream.Metadata.GetType()) : null;
            var events = CreateEventRecords(expectedVersion, stream);

            _settings.Persistor.SaveChanges(streamId, stream.CommitId, expectedVersion, events, metadata, stream.IsLockable);
        }

        public async Task SaveChangesAsync(Guid streamId, int expectedVersion, IEventStream stream, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (expectedVersion < ExpectedVersion.Any) throw new InvalidOperationException($"Expected version {expectedVersion} for stream {streamId} is invalid.");
            if (!stream.Events.Any()) return;
            _settings.Logger.Debug("Saving changes for stream '{0}' with commit '{1}'...", streamId, stream.CommitId);
            await TrySaveChanges(streamId, expectedVersion, stream, cancellationToken);
        }

        private async Task TrySaveChanges(Guid streamId, int expectedVersion, IEventStream stream, CancellationToken cancellationToken)
        {
            // TODO: resolving conflicts
            var metadata = stream.Metadata != null ? _settings.Serializer.Serialize(stream.Metadata, stream.Metadata.GetType()) : null;
            var events = CreateEventRecords(expectedVersion, stream);
#if DEBUG
            LogSavedStream(streamId, expectedVersion, stream.CommitId, events);
#endif
            await _settings.Persistor.SaveChangesAsync(streamId, stream.CommitId, expectedVersion, events, metadata, stream.IsLockable, cancellationToken);
        }

#if DEBUG
        private void LogSavedStream(Guid streamId, int expectedVersion, Guid commitId, IEnumerable<EventRecord> events)
        {
            _settings.Logger.Trace($"Stream {streamId} v.{ExpectedVersion.Parse(expectedVersion)} commit {commitId}");
            foreach (var e in events)
            {
                _settings.Logger.Trace($"\t{e.ContractName} v.{e.Version} [{e.Payload}]");
            }
        }
#endif

        private IEnumerable<EventRecord> CreateEventRecords(int expectedVersion, IEventStream stream)
        {
            if (expectedVersion < 0) expectedVersion = 0;
            var records = new List<EventRecord>();
            foreach (var @event in stream.Events)
            {
                var version = ++expectedVersion;
                var contractName = _settings.ContractsRegistry.GetContractName(@event.GetType());
                var payload = _settings.Serializer.Serialize(@event, @event.GetType());
                records.Add(new EventRecord(version, contractName, payload));
            }
            return records;
        }

        public void Dispose()
        {
            _settings.Persistor.Dispose();
        }
    }
}