﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ses.InMemory
{
    internal class InMemoryPersistor : IEventStreamPersistor
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private readonly ConcurrentDictionary<Guid, InMemoryStream> _streams = new ConcurrentDictionary<Guid, InMemoryStream>();
        private readonly ConcurrentDictionary<Guid, InMemorySnapshot> _snapshots = new ConcurrentDictionary<Guid, InMemorySnapshot>();

        public void UpdateSnapshot(Guid streamId, int version, string contractName, byte[] payload)
        {
            throw new NotImplementedException();
        }

        public EventRecord[] Load(Guid streamId, int fromVersion, bool pessimisticLock)
        {
            if (pessimisticLock) throw new NotImplementedException("Pessimistic lock is not implemented.");
            _lock.EnterReadLock();

            try
            {
                if (!_streams.TryGetValue(streamId, out var stream))
                {
                    return new EventRecord[0];
                }

                var events = new List<EventRecord>(20);
                if (_snapshots.TryGetValue(streamId, out var snapshot) && snapshot.Version >= fromVersion)
                {
                    // ReSharper disable once PossibleNullReferenceException
                    events.Add(EventRecord.Snapshot(snapshot.ContractName, snapshot.Version, snapshot.Payload));
                }

                if (snapshot == null)
                {
                    foreach (var @event in stream.Events)
                    {
                        if (@event.Version < fromVersion) continue;
                        events.Add(CreateEventObject(@event));
                    }
                }
                else
                {
                    foreach (var @event in stream.Events)
                    {
                        if (@event.Version <= snapshot.Version) continue;
                        events.Add(CreateEventObject(@event));
                    }
                }
                return events.ToArray();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public Task<EventRecord[]> LoadAsync(Guid streamId, int fromVersion, bool pessimisticLock, CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(Load(streamId, fromVersion, pessimisticLock));
        }

        private static EventRecord CreateEventObject(InMemoryEventRecord arg)
        {
            return EventRecord.Event(arg.ContractName, arg.Version, arg.EventData);
        }

        public Task DeleteStreamAsync(Guid streamId, int expectedVersion, CancellationToken cancellationToken = new CancellationToken())
        {
            DeleteStream(streamId, expectedVersion);
            return Task.FromResult(0);
        }

        public void DeleteStream(Guid streamId, int expectedVersion)
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

                if (!_streams.TryGetValue(streamId, out var inMemoryStream)) return;

                inMemoryStream.DeleteAllEvents();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public Task UpdateSnapshotAsync(Guid streamId, int version, string contractName, byte[] payload, CancellationToken cancellationToken = new CancellationToken())
        {
            _lock.EnterWriteLock();
            try
            {
                if (!_snapshots.TryGetValue(streamId, out var snapshot))
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

        public Task SaveChangesAsync(Guid streamId, Guid commitId, int expectedVersion, EventRecord[] events, byte[] metadata, bool isLockable, CancellationToken cancellationToken = new CancellationToken())
        {
            SaveChanges(streamId, commitId, expectedVersion, events, metadata, isLockable);
            return Task.FromResult(0);
        }

        public void SaveChanges(Guid streamId, Guid commitId, int expectedVersion, EventRecord[] events, byte[] metadata, bool isLockable)
        {
            if (isLockable) throw new NotImplementedException("Pessimistic lock is not implemented.");

            _lock.EnterWriteLock();
            try
            {
                SaveChangesInternal(streamId, commitId, expectedVersion, events, metadata);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public int GetStreamVersion(Guid streamId)
        {
            _lock.EnterReadLock();

            try
            {
                if (!_streams.TryGetValue(streamId, out var stream))
                {
                    return -1;
                }

                return stream.Events.OrderByDescending(x => x.Version).FirstOrDefault()?.Version ?? 0;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public Task<int> GetStreamVersionAsync(Guid streamId, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(GetStreamVersion(streamId));
        }

        private void SaveChangesInternal(Guid streamId, Guid commitId, int expectedVersion, EventRecord[] events, byte[] metadata)
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

        public void Dispose()
        {
            _lock.Dispose();
        }
    }
}
