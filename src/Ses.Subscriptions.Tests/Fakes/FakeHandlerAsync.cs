using System.Threading.Tasks;
using Ses.Abstracts.Subscriptions;

namespace Ses.Subscriptions.Tests.Fakes
{
    public class FakeHandlerAsync : IHandleAsync<FakeEvent>
    {
        public Task Handle(FakeEvent e, EventEnvelope envelope)
        {
            return Task.FromResult(0);
        }
    }
}