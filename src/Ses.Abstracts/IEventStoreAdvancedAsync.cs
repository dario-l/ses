using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ses.Abstracts
{
    public interface IEventStoreAdvancedAsync
    {
        Task DeleteStreamAsync(
            Guid streamId,
            int expectedVersion,
            CancellationToken cancellationToken = default(CancellationToken));

        Task UpdateSnapshotAsync(
            Guid streamId,
            int currentVersion,
            IMemento snapshot,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}