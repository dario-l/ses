using System;
using System.Threading;
using System.Threading.Tasks;
using Ses.Abstracts;

namespace Ses
{
    public interface IEventStreamPersistor : IDisposable
    {
        /// <summary>
        /// Loads events from data source.
        /// </summary>
        /// <param name="streamId">Stream identifier</param>
        /// <param name="fromVersion"></param>
        /// <param name="pessimisticLock">If <c>true</c> then acquires pessimistic lock.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IEvent[]> LoadAsync(Guid streamId, int fromVersion, bool pessimisticLock, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Loads events from data source.
        /// </summary>
        /// <param name="streamId">Stream identifier</param>
        /// <param name="fromVersion"></param>
        /// <param name="pessimisticLock">If <c>true</c> then acquires pessimistic lock.</param>
        /// <returns></returns>
        IEvent[] Load(Guid streamId, int fromVersion, bool pessimisticLock);

        /// <summary>
        /// Deletes stream for a given id.
        /// </summary>
        /// <param name="streamId">Stream identifier</param>
        /// <param name="expectedVersion"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DeleteStreamAsync(Guid streamId, int expectedVersion, CancellationToken cancellationToken = default(CancellationToken));

        void DeleteStream(Guid streamId, int expectedVersion);

        /// <summary>
        /// Adding new snapshot to data source.
        /// </summary>
        /// <param name="streamId">Stream identifier</param>
        /// <param name="payload"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="version"></param>
        /// <param name="contractName"></param>
        /// <returns></returns>
        Task UpdateSnapshotAsync(Guid streamId, int version, string contractName, byte[] payload, CancellationToken cancellationToken = new CancellationToken());

        void UpdateSnapshot(Guid streamId, int version, string contractName, byte[] payload);

        /// <summary>
        /// Fires when event was read from data source.
        /// </summary>
        event OnReadEventHandler OnReadEvent;

        /// <summary>
        /// Fires when snapshot was read from data source.
        /// </summary>
        event OnReadSnapshotHandler OnReadSnapshot;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="streamId"></param>
        /// <param name="commitId"></param>
        /// <param name="expectedVersion"></param>
        /// <param name="events"></param>
        /// <param name="metadata"></param>
        /// <param name="isLockable"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SaveChangesAsync(Guid streamId, Guid commitId, int expectedVersion, EventRecord[] events, byte[] metadata, bool isLockable, CancellationToken cancellationToken = default(CancellationToken));

        void SaveChanges(Guid streamId, Guid commitId, int expectedVersion, EventRecord[] events, byte[] metadata, bool isLockable);
    }

    public delegate IEvent OnReadEventHandler(Guid streamId, string contractName, int version, byte[] payload);
    public delegate IRestoredMemento OnReadSnapshotHandler(Guid streamId, string contractName, int version, byte[] payload);
}