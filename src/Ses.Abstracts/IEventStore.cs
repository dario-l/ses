using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ses.Abstracts
{
    public interface IEventStore : IDisposable
    {
        Task<IReadOnlyEventStream> LoadAsync(
            Guid streamId,
            bool pessimisticLock,
            CancellationToken cancellationToken = default(CancellationToken));

        Task SaveChangesAsync(
            Guid streamId,
            int expectedVersion,
            IEventStream stream,
            CancellationToken cancellationToken = default(CancellationToken));

        IReadOnlyEventStream Load(Guid streamId, bool pessimisticLock);

        void SaveChanges(Guid streamId, int expectedVersion, IEventStream stream);

        IEventStoreAdvanced Advanced { get; }
    }
}