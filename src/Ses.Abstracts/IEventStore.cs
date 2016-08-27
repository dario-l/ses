using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ses.Abstracts
{
    public interface IEventStore : IDisposable
    {
        Task<IReadOnlyEventStream> Load(
            Guid streamId,
            bool pessimisticLock,
            CancellationToken cancellationToken = default(CancellationToken));

        Task SaveChanges(
            Guid streamId,
            int expectedVersion,
            IEventStream stream,
            CancellationToken cancellationToken = default(CancellationToken));

        IEventStoreAdvanced Advanced { get; }
    }
}