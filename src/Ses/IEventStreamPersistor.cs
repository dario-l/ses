using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ses.Abstracts;

namespace Ses
{
    public interface IEventStreamPersistor
    {
        /// <summary>
        /// Loads events from persistance instance.
        /// </summary>
        /// <param name="streamId">Stream identifier</param>
        /// <param name="pessimisticLock">If <c>true</c> then acquires pessimistic lock.</param>
        /// <returns></returns>
        Task<IList<IEvent>> Load(Guid streamId, bool pessimisticLock);

        /// <summary>
        /// Deletes stream for a given id.
        /// </summary>
        /// <param name="streamId">Stream identifier</param>
        /// <returns></returns>
        Task DeleteStream(Guid streamId);

        /// <summary>
        /// Adding new snapshot to persistance.
        /// </summary>
        /// <param name="streamId">Stream identifier</param>
        /// <param name="snapshot">Snaphot instance</param>
        /// <returns></returns>
        Task AddSnapshot(Guid streamId, IMemento snapshot);

        /// <summary>
        /// Fires when event was read from persistance.
        /// </summary>
        Func<Guid, string, byte[], IEvent> OnEventRead { get; set; }

        /// <summary>
        /// Fires when snapshot was read from persistance.
        /// </summary>
        Func<Guid, string, byte[], IEvent> OnSnapshotRead { get; set; }
    }
}