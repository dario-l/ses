using System;
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
            var @event = _settings.Serializer.Deserialize<IMemento>(eventData, eventType);
            if (@event == null) throw new InvalidCastException($"Deserialized payload from stream {streamId} is not an IEvent of type {eventType.FullName}.");
            var upConverter = _settings.UpConverterFactory.CreateInstance(eventType);
            return upConverter == null ? @event : ((dynamic)upConverter).Convert(@event);
        }

        private IEvent OnSnapshotRead(Guid streamId, string snapshotContract, byte[] snapshotData)
        {
            var snapshotType = _settings.ContractsRegistry.GetType(snapshotContract);
            var memento = _settings.Serializer.Deserialize<IMemento>(snapshotData, snapshotType);
            if (memento == null) throw new InvalidCastException($"Deserialized payload from stream {streamId} is not an IMemento of type {snapshotType.FullName}.");
            var upConverter = _settings.UpConverterFactory.CreateInstance(snapshotType);
            return upConverter == null ? memento : ((dynamic)upConverter).Convert(memento);
        }

        public async Task<IReadOnlyEventStream> Load(Guid streamId, bool pessimisticLock, CancellationToken cancellationToken = default(CancellationToken))
        {
            var events = await _settings.Persistor.Load(streamId, pessimisticLock);
            var snapshot = events[0] as IMemento;
            var currentVersion = snapshot?.Version + events.Count ?? events.Count;
            return new ReadOnlyEventStream(events, currentVersion);
        }

        public Task SaveChanges(Guid streamId, int expectedVersion, IEventStream stream, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task DeleteStream(Guid streamId, int expectedVersion, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }
    }
}