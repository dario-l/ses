using System;

namespace Ses.Abstracts
{
    public interface IEventStore : IEventStoreAsync
    {
        IReadOnlyEventStream Load(Guid streamId, int fromVersion, bool pessimisticLock);
        void SaveChanges(Guid streamId, int expectedVersion, IEventStream stream);

        new IEventStoreAdvanced Advanced { get; }
    }
}