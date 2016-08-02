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
        /// <param name="pessimisticLock">If <c>true</c> then acquires pessimistic lock.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IList<IEvent>> Load(Guid streamId, bool pessimisticLock, CancellationToken cancellationToken = default(CancellationToken));

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
        /// <param name="snapshot">Snaphot instance</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task AddSnapshot(Guid streamId, IMemento snapshot, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Fires when event was read from data source.
        /// </summary>
        Func<Guid, string, byte[], IEvent> OnReadEvent { get; set; }

        /// <summary>
        /// Fires when snapshot was read from data source.
        /// </summary>
        Func<Guid, string, byte[], IEvent> OnReadSnapshot { get; set; }
    }
}