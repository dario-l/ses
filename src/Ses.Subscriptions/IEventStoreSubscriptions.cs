using System;

namespace Ses.Subscriptions
{
    public interface IEventStoreSubscriptions : IDisposable
    {
        void RunStoppedPoller(Type type, bool force = false);
        void RunStoppedPollers();
        PollerInfo[] GetPollers();
    }
}