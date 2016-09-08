using System.Threading.Tasks;
using Ses.Abstracts.Subscriptions;

namespace Ses.Subscriptions.Tests.Fakes
{
    public class FakeHandlerMixed : IHandleAsync<FakeEvent>, IHandle<FakeEventAnother>
    {
        public Task Handle(FakeEvent e, EventEnvelope envelope)
        {
            return Task.FromResult(0);
        }

        public void Handle(FakeEventAnother e, EventEnvelope envelope)
        {
            
        }
    }
}