using System;
using System.Threading.Tasks;

namespace Ses.Subscriptions
{
    public interface IEventStoreSubscriptions : IDisposable
    {
        void RunStoppedPoller(Type type, bool force = false);
        void RunStoppedPollers();
        PollerInfo[] GetPollers();
        void Start();
        Task StartAsync();
    }
}