using System;

namespace Ses.Abstracts
{
    public interface IEventStoreAdvanced : IEventStoreAdvancedAsync
    {
        void DeleteStream(Guid streamId, int expectedVersion);
        void UpdateSnapshot(Guid streamId, int currentVersion, IMemento snapshot);
        int GetStreamVersion(Guid streamId);
    }
}