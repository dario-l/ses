using System;

namespace Ses.Subscriptions
{
    public interface IEventStoreSubscriptions : IDisposable
    {
        void RunPooler(Type type);
        void RunStoppedPoolers();
    }
}