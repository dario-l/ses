using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ses.Abstracts;

namespace Ses
{
    public interface IEventStreamPersistor
    {
        /// <summary>
        /// Loads events from data source.
        /// </summary>
        /// <param name="streamId">Stream identifier</param>
        /// <param name="fromVersion"></param>
        /// <param name="pessimisticLock">If <c>true</c> then acquires pessimistic lock.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IList<IEvent>> Load(Guid streamId, int fromVersion, bool pessimisticLock, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Deletes stream for a given id.
        /// </summary>
        /// <param name="streamId">Stream identifier</param>
        /// <param name="expectedVersion"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DeleteStream(Guid streamId, int expectedVersion, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Adding new snapshot to data source.
        /// </summary>
        /// <param name="streamId">Stream identifier</param>
        /// <param name="payload"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="version"></param>
        /// <param name="contractName"></param>
        /// <returns></returns>
        Task AddSnapshot(Guid streamId, int version, string contractName, byte[] payload, CancellationToken cancellationToken = new CancellationToken());

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
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SaveChanges(Guid streamId, Guid commitId, int expectedVersion, IEnumerable<EventRecord> events, byte[] metadata, CancellationToken cancellationToken = default(CancellationToken));
    }

    public delegate Task<IEvent> OnReadEventHandler(Guid streamId, string contractName, int version, byte[] payload);
    public delegate Task<IRestoredMemento> OnReadSnapshotHandler(Guid streamId, string contractName, int version, byte[] payload);
}