using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ses.Abstracts;

namespace Ses
{
    internal partial class EventStore
    {
        IEventStoreAdvancedAsync IEventStoreAsync.Advanced => Advanced;

        public async Task<IReadOnlyEventStream> LoadAsync(Guid streamId, int fromVersion, bool pessimisticLock, CancellationToken cancellationToken = default(CancellationToken))
        {
            var records = await _settings.Persistor.LoadAsync(streamId, fromVersion, pessimisticLock, cancellationToken);
            if (records == null || records.Length == 0) return null;
            return CreateReadOnlyStream(streamId, fromVersion, records);
        }

        private static int CalculateCurrentVersion(int fromVersion, IEvent[] events, IRestoredMemento snapshot)
        {
            return snapshot?.Version + (events.Length - 1) + (fromVersion - 1) ?? events.Length + (fromVersion - 1);
        }

        public async Task SaveChangesAsync(Guid streamId, int expectedVersion, IEventStream stream, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (expectedVersion < ExpectedVersion.Any)
                throw new InvalidOperationException($"Expected version {expectedVersion} for stream {streamId} is invalid.");
            if (stream.Events.Length == 0) return;
            await TrySaveChangesAsync(streamId, expectedVersion, stream, cancellationToken);
        }

        private async Task TrySaveChangesAsync(Guid streamId, int expectedVersion, IEventStream stream, CancellationToken cancellationToken)
        {
            _settings.Logger.Trace("Saving changes for stream '{0}' with commit '{1}'...", streamId, stream.CommitId);

            var metadata = stream.Metadata != null ? _settings.Serializer.Serialize(stream.Metadata, stream.Metadata.GetType()) : null;

            var version = expectedVersion;

            var tryCounter = 0;
            while (true)
            {
                try
                {
                    var events = CreateEventRecords(version, stream);

                    await _settings.Persistor.SaveChangesAsync(
                        streamId,
                        stream.CommitId,
                        version,
                        events,
                        metadata,
                        stream.IsLockable,
                        cancellationToken);

                    return;
                }
                catch (WrongExpectedVersionException e)
                {
                    if (expectedVersion <= ExpectedVersion.NoStream || _settings.ConcurrencyConflictResolver == null) throw;

                    if (tryCounter >= 3)
                    {
                        _settings.Logger.Error("Retrying committing stream '{0}' excided limit and throwing concurrency exception.", streamId);
                        throw;
                    }
                    tryCounter++;
                    _settings.Logger.Debug("Retrying committing stream '{0}' for {1} time...", streamId, tryCounter);
                    var previousStream = await LoadAsync(streamId, e.ConflictedVersion - 1, false, cancellationToken);
                    var previousEventTypes = previousStream.CommittedEvents.Select(x => x.GetType()).ToArray();
                    var conflictedEventType = _settings.ContractsRegistry.GetType(e.ConflictedContractName);

                    if (_settings.ConcurrencyConflictResolver.ConflictsWith(conflictedEventType, previousEventTypes)) throw;

                    version = previousStream.CommittedVersion + stream.Events.Length;
                }
            }
        }
    }
}