using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ses.Abstracts
{
    public interface IEventStoreAsync : IDisposable
    {
        Task<IReadOnlyEventStream> LoadAsync(
            Guid streamId,
            int fromVersion,
            bool pessimisticLock,
            CancellationToken cancellationToken = default(CancellationToken));

        Task SaveChangesAsync(
            Guid streamId,
            int expectedVersion,
            IEventStream stream,
            CancellationToken cancellationToken = default(CancellationToken));

        IEventStoreAdvancedAsync Advanced { get; }
    }
}