using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ses.Abstracts;

namespace Ses
{
    public class EventStore : IEventStore
    {
        private readonly IEventStoreSettings _settings;

        public EventStore(IEventStoreSettings settings)
        {
            _settings = settings;
            _settings.Persistor.OnReadEvent += OnEventRead;
            _settings.Persistor.OnReadSnapshot += OnSnapshotRead;
        }

        private IEvent OnEventRead(Guid streamId, string eventContract, byte[] eventData)
        {
            var eventType = _settings.ContractsRegistry.GetType(eventContract);
            var @event = _settings.Serializer.Deserialize<IEvent>(eventData, eventType);
            if (@event == null) throw new InvalidCastException($"Deserialized payload from stream {streamId} is not an IEvent of type {eventType.FullName}.");

            if (_settings.UpConverterFactory == null) return @event;

            var upConverter = _settings.UpConverterFactory.CreateInstance(eventType);
            while (upConverter != null)
            {
                @event = ((dynamic)upConverter).Convert(@event);
                upConverter = _settings.UpConverterFactory.CreateInstance(@event.GetType());
            }

            return @event;
        }

        private IEvent OnSnapshotRead(Guid streamId, string snapshotContract, byte[] snapshotData)
        {
            var snapshotType = _settings.ContractsRegistry.GetType(snapshotContract);
            var memento = _settings.Serializer.Deserialize<IMemento>(snapshotData, snapshotType);
            if (memento == null) throw new InvalidCastException($"Deserialized payload from stream {streamId} is not an IMemento of type {snapshotType.FullName}.");

            if (_settings.UpConverterFactory == null) return memento;

            var upConverter = _settings.UpConverterFactory.CreateInstance(snapshotType);
            while (upConverter != null)
            {
                memento = ((dynamic)upConverter).Convert(memento);
                upConverter = _settings.UpConverterFactory.CreateInstance(memento.GetType());
            }

            return memento;
        }

        public Task<IReadOnlyEventStream> Load(Guid streamId, bool pessimisticLock, CancellationToken cancellationToken = default(CancellationToken))
        {
            return LoadInternal(streamId, 1, pessimisticLock, cancellationToken);
        }

        private async Task<IReadOnlyEventStream> LoadInternal(Guid streamId, int fromVersion, bool pessimisticLock, CancellationToken cancellationToken)
        {
            var events = await _settings.Persistor.Load(streamId, fromVersion, pessimisticLock, cancellationToken);
            var snapshot = events[0] as IMemento;
            var currentVersion = snapshot?.Version + events.Count ?? events.Count;
            return new ReadOnlyEventStream(events, currentVersion);
        }

        public async Task DeleteStream(Guid streamId, int expectedVersion, CancellationToken cancellationToken = new CancellationToken())
        {
            await _settings.Persistor.DeleteStream(streamId, expectedVersion, cancellationToken);
        }

        public async Task SaveChanges(Guid streamId, int expectedVersion, IEventStream stream, CancellationToken cancellationToken = default(CancellationToken))
        {
            _settings.Logger.Debug("Saving changes for stream '{0}' with commit '{1}'...", streamId, stream.CommitId);
            await TrySaveChanges(streamId, expectedVersion, stream, cancellationToken);
        }

        private async Task TrySaveChanges(Guid streamId, int expectedVersion, IEventStream stream, CancellationToken cancellationToken)
        {
            // TODO: resolving conflicts
            var metadata = _settings.Serializer.Serialize(stream.Metadata, stream.Metadata.GetType());
            var events = CreateEventRecords(expectedVersion, stream);
            await _settings.Persistor.SaveChanges(streamId, stream.CommitId, expectedVersion, events, metadata, cancellationToken);
        }

        private EventRecord[] CreateEventRecords(int expectedVersion, IEventStream stream)
        {
            var records = new List<EventRecord>();
            foreach (var @event in stream.Events)
            {
                var version = ++expectedVersion;
                var contractName = _settings.ContractsRegistry.GetContractName(@event.GetType());
                var payload = _settings.Serializer.Serialize(@event, @event.GetType());
                records.Add(new EventRecord(version, contractName, payload));
            }
            return records.ToArray();
        }
    }
}