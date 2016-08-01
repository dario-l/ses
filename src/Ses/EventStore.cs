using System;
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
            var convertedEvent = _settings.UpConverter.Convert(@event);
            return convertedEvent;
        }

        private IEvent OnSnapshotRead(Guid streamId, string snapshotContract, byte[] snapshotData)
        {
            var snapshotType = _settings.ContractsRegistry.GetType(snapshotContract);
            var memento = _settings.Serializer.Deserialize<IMemento>(snapshotData, snapshotType);
            if (memento == null) throw new InvalidCastException($"Deserialized payload from stream {streamId} is not an IMemento of type {snapshotType.FullName}.");
            var convertedSnapshot = _settings.UpConverter.Convert(memento);
            return convertedSnapshot;
        }

        public async Task<IReadOnlyEventStream> Load(Guid id, bool pessimisticLock = false)
        {
            var events = await _settings.Persistor.Load(id, pessimisticLock);
            var snapshot = events[0] as IMemento;
            var currentVersion = snapshot?.Version + events.Count ?? events.Count;
            return new ReadOnlyEventStream(id, events, currentVersion);
        }

        public Task SaveChanges(IEventStream stream)
        {
            throw new NotImplementedException();
        }
    }
}