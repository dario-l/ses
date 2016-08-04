using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ses.Abstracts;

namespace Ses.InMemory
{
    internal class InMemoryPersistor : IEventStreamPersistor
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private readonly ConcurrentDictionary<Guid, InMemoryStream> _streams = new ConcurrentDictionary<Guid, InMemoryStream>();
        private readonly ConcurrentDictionary<Guid, InMemorySnapshot> _snapshots = new ConcurrentDictionary<Guid, InMemorySnapshot>();

        public event OnReadEventHandler OnReadEvent;
        public event OnReadSnapshotHandler OnReadSnapshot;

        public Task<IList<IEvent>> Load(Guid streamId, int fromVersion, bool pessimisticLock, CancellationToken cancellationToken = new CancellationToken())
        {
            if (pessimisticLock) throw new NotImplementedException("Pessimistic lock is not implemented.");
            _lock.EnterReadLock();

            try
            {
                InMemoryStream stream;
                if (!_streams.TryGetValue(streamId, out stream))
                {
                    return Task.FromResult((IList<IEvent>)null);
                }

                var events = new List<IEvent>();
                InMemorySnapshot snapshot;
                if (_snapshots.TryGetValue(streamId, out snapshot) && snapshot.Version >= fromVersion)
                {
                    // ReSharper disable once PossibleNullReferenceException
                    events.Add(OnReadSnapshot(streamId, snapshot.ContractName, snapshot.Version, snapshot.Payload));
                }

                events.AddRange(snapshot != null
                    ? stream.Events.Where(x => x.Version > snapshot.Version).Select(e => CreateEventObject(streamId, e))
                    : stream.Events.Where(x => x.Version >= fromVersion).Select(e => CreateEventObject(streamId, e)));
                return Task.FromResult((IList<IEvent>)events);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        private IEvent CreateEventObject(Guid streamId, InMemoryEventRecord arg)
        {
            // ReSharper disable once PossibleNullReferenceException
            return OnReadEvent(streamId, arg.ContractName, arg.Version, arg.EventData);
        }

        public Task DeleteStream(Guid streamId, int expectedVersion, CancellationToken cancellationToken = new CancellationToken())
        {
            _lock.EnterWriteLock();
            try
            {
                if (!_streams.ContainsKey(streamId) && expectedVersion > 0)
                {
                    throw new WrongExpectedVersionException($"Stream {streamId} not found.");
                }
                if (expectedVersion > 0 && _streams[streamId].Events.Last().Version != expectedVersion)
                {
                    throw new WrongExpectedVersionException($"Expected version {expectedVersion} is different than appended earlier to stream {streamId}.");
                }

                InMemoryStream inMemoryStream;
                if (!_streams.TryGetValue(streamId, out inMemoryStream)) return Task.FromResult(0);

                inMemoryStream.DeleteAllEvents();
                return Task.FromResult(0);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public Task AddSnapshot(Guid streamId, int version, string contractName, byte[] payload, CancellationToken cancellationToken = new CancellationToken())
        {
            _lock.EnterWriteLock();
            try
            {
                InMemorySnapshot snapshot;
                if (!_snapshots.TryGetValue(streamId, out snapshot))
                {
                    snapshot = new InMemorySnapshot(version, contractName, payload);
                    _snapshots.TryAdd(streamId, snapshot);
                }
                else
                {
                    snapshot.Update(version, payload);
                }

                return Task.FromResult(0);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public Task SaveChanges(Guid streamId, Guid commitId, int expectedVersion, IEnumerable<EventRecord> events, byte[] metadata, CancellationToken cancellationToken = new CancellationToken())
        {
            _lock.EnterWriteLock();
            try
            {
                SaveChangesInternal(streamId, commitId, expectedVersion, events, metadata);
                return Task.FromResult(0);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        private void SaveChangesInternal(Guid streamId, Guid commitId, int expectedVersion, IEnumerable<EventRecord> events, byte[] metadata)
        {
            InMemoryStream inMemoryStream;
            if (expectedVersion == ExpectedVersion.NoStream || expectedVersion == ExpectedVersion.Any)
            {
                if (!_streams.TryGetValue(streamId, out inMemoryStream))
                {
                    inMemoryStream = new InMemoryStream(streamId);
                    _streams.TryAdd(streamId, inMemoryStream);
                }

                inMemoryStream.Append(commitId, expectedVersion, events.ToList(), metadata);
                return;
            }

            if (!_streams.TryGetValue(streamId, out inMemoryStream))
            {
                throw new WrongExpectedVersionException($"Append to stream {streamId} expected version {expectedVersion} mismatch. Stream doesn't exists.");
            }
            inMemoryStream.Append(commitId, expectedVersion, events.ToList(), metadata);
        }
    }
}
