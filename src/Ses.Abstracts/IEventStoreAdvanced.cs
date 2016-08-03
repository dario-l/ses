using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ses.Abstracts
{
    public interface IEventStoreAdvanced
    {
        Task DeleteStream(
            Guid streamId,
            int expectedVersion,
            CancellationToken cancellationToken = default(CancellationToken));

        Task AddSnapshot(
            Guid streamId,
            int currentVersion,
            IMemento snapshot,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}