using System;
using System.Linq;
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

        public Task<IReadOnlyEventStream> Load(Guid streamId, bool pessimisticLock, CancellationToken cancellationToken = default(CancellationToken))
        {
            return InternalLoad(streamId, 0, pessimisticLock, cancellationToken);
        }

        private async Task<IReadOnlyEventStream> InternalLoad(Guid streamId, int fromVersion, bool pessimisticLock, CancellationToken cancellationToken)
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
            await TrySaveChanges(streamId, expectedVersion, stream, 0, cancellationToken);
        }

        private async Task<int> TrySaveChanges(Guid streamId, int expectedVersion, IEventStream stream, int tryCommitCounter, CancellationToken cancellationToken)
        {
            try
            {
                await _settings.Persistor.SaveChanges(streamId, stream.CommitId, expectedVersion, stream.Events, stream.Metadata);
            }
            catch (StreamConcurrencyException e)
            {
                if (tryCommitCounter >= 3)
                {
                    _settings.Logger.Error("Retrying committing stream '{0}' excided limit and throwing concurrency exception.", streamId);
                    throw;
                }
                tryCommitCounter++;
                _settings.Logger.Debug("Retrying committing stream '{0}' for {1} time...", streamId, tryCommitCounter);
                var previousEvents = await InternalLoad(streamId, e.ConflictedEventVersion - 1, false, cancellationToken);
                var calculatedVersion = previousEvents.CommittedVersion;
                var previousEventTypes = previousEvents.CommittedEvents.Select(x => x.GetType()).ToList();
                var resolved = _settings.ConcurrencyConflictResolver.ConflictsWith(e.ConflictedEventType, previousEventTypes);
                if (!resolved) throw;
                return await TrySaveChanges(streamId, expectedVersion + calculatedVersion, stream, tryCommitCounter, cancellationToken);
            }
            return tryCommitCounter;
        }
    }
}