using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ses.Domain
{
    public interface IRepository<TAggregate> where TAggregate : class, IAggregate, new()
    {
        /// <summary>
        /// Creates aggregate instance populated from stream with given id.
        /// </summary>
        /// <param name="streamId">Stream id</param>
        /// <param name="pessimisticLock"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Aggregate instance</returns>
        Task<TAggregate> LoadAsync(Guid streamId, bool pessimisticLock = false, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Save uncommited events to event store.
        /// </summary>
        /// <param name="aggregate">Aggregate with uncommited events.</param>
        /// <param name="commitId"></param>
        /// <param name="cancellationToken"></param>
        Task SaveChangesAsync(TAggregate aggregate, Guid? commitId = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Delete events stream from event store permanently. Use it wisely.
        /// </summary>
        /// <param name="streamId">Stream id to delete.</param>
        /// <param name="expectedVersion"></param>
        /// <param name="cancellationToken"></param>
        Task DeleteAsync(Guid streamId, int expectedVersion, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Creates aggregate instance populated from stream with given id.
        /// </summary>
        /// <param name="streamId">Stream id</param>
        /// <param name="pessimisticLock"></param>
        /// <returns>Aggregate instance</returns>
        TAggregate Load(Guid streamId, bool pessimisticLock = false);

        /// <summary>
        /// Save uncommited events to event store.
        /// </summary>
        /// <param name="aggregate">Aggregate with uncommited events.</param>
        /// <param name="commitId"></param>
        void SaveChanges(TAggregate aggregate, Guid? commitId = null);

        /// <summary>
        /// Delete events stream from event store permanently. Use it wisely.
        /// </summary>
        /// <param name="streamId">Stream id to delete.</param>
        /// <param name="expectedVersion"></param>
        void Delete(Guid streamId, int expectedVersion);
    }
}