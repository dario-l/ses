using System;

namespace Ses.Subscriptions
{
    public interface IEventStoreSubscriptions : IDisposable
    {
        void RunStoppedPooler(Type type);
        void RunStoppedPoolers();
    }
}