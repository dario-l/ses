using System;

namespace Ses.Subscriptions
{
    public interface IEventStoreSubscriptions : IDisposable
    {
        void RunStoppedPooler(Type type, bool force = false);
        void RunStoppedPoolers();
        Type[] GetPoolerTypes();
    }
}